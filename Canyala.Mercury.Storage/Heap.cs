/*

  MIT License
 
  Copyright (c) 2011-2023 Canyala Innovation (Martin Fredriksson)

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all
  copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.

*/

using System.Diagnostics;
using System.Text;

using Canyala.Lagoon.Core.Contracts;
using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.IO;

using Canyala.Mercury.Storage.Extensions;

namespace Canyala.Mercury.Storage;

/// <summary>
/// Provides a thread safe stream based heap with automatic optimistic block reclaim and
/// full garbage collection capabilities. Full GC is not automatic but invoked explicitly.
/// The underlying stream is required to support 'Seek', 'Read' & 'Write'.
/// </summary>
/// 
/// <remarks>
/// A heap does not cache anything by itself, caching should be provided by
/// the underlying stream. To maximize performance it might be a good idea 
/// to support multiple cache areas based on access heuristics.
/// 
/// A heap is organized as follows:
/// 
/// Offset 0 [Header: Size] = byte offset to total sizeof heap.
///          [Header: Free] = byte offset to head free node or null.
///          [Header: Root] = byte offset to client bootstrap allocation or null.
///          [Free:   Size] = size of next + data area in number of bytes (size is negative for free blocks, used by GC)
///          [Free:   Next] = offset to next free block. (borrows data space).
///          [Free:   Prev] = offset to prev free block. (borrows data space).
///          [Free:   Data] = data space.
///          [Used:   Size] = size of data in allocation.
///          [Used:   Data] = data of allocation.
///          .
///          .
///          .
///          
/// Consecutive block nodes are found by adding node size and data size to a block offset.
/// This is used by the garbage collection algorithm to walk the heap.
/// </remarks>
public sealed class Heap
{
    #region Heap Offsets

    const long HEADER_TOTAL_SIZE = sizeof(long) * 0;
    const long HEADER_FREE_LIST = sizeof(long) * 1;
    const long HEADER_ROOT_LIST = sizeof(long) * 2;
    const long HEADER_LENGTH = sizeof(long) * 3;

    const long FREE_SIZE = sizeof(long) * 0;
    const long FREE_NEXT = sizeof(long) * 1;
    const long FREE_PREV = sizeof(long) * 2;

    const long USED_SIZE = sizeof(long) * 0;
    const long USED_DATA = sizeof(long) * 1;

    private const long NULL = 0;

    #endregion

    #region Internal state

    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    private readonly Stream _stream;
    private readonly long _size;

    #endregion

    public const long MinimumHeapSize = (HEADER_LENGTH + sizeof(long) * 3);

    /// <summary>
    /// Gets the underlying stream for the heap.
    /// </summary>
    internal Stream Stream => _stream;

    /// <summary>
    /// Create a heap from a stream
    /// </summary>
    /// <param name="stream">Stream providing access to actual storage.</param>
    /// <param name="size">Maximum size for underlying storage. Not max alloc.</param>
    /// <param name="chnk">Data allocation chunk size, good guess equals better performance.</param>
    /// <returns>A heap</returns>
    public Heap(Stream stream, long size)
    {
        Contract.Requires(size >= MinimumHeapSize , "Heap creation size is below minimum size.");
        Contract.Requires(stream.Length == 0, "Heap creation requires an empty stream.");
        Contract.Requires(stream.CanSeek, "Heap requires a seekable stream.");

        var writer = new BinaryWriter(_stream = stream);

        writer.Seek(HEADER_TOTAL_SIZE);

        writer.Write(_size = size);
        writer.Write(HEADER_LENGTH);
        writer.Write(NULL);

        writer.Write(-(_size - HEADER_LENGTH - sizeof(long)));
        writer.Write(NULL); // Initial next offset
        writer.Write(NULL); // Initial prev offset
    }

    /// <summary>
    /// Recreate a heap from a stream.
    /// </summary>
    /// <param name="stream">Stream providing access to actual storage.</param>
    public Heap(Stream stream)
    {
        Contract.Requires(stream.Length > 0, "Heap recreation requires a non empty stream.");

        var reader = new BinaryReader(_stream = stream);
        reader.Seek(HEADER_TOTAL_SIZE);
        _size = reader.ReadInt64();
    }

    /// <summary>
    /// A heap is read only if the underlying stream can not be written to.
    /// </summary>
    public bool IsReadOnly { get { return !_stream.CanWrite; } }

    /// <summary>
    /// Retrieves the offset of a named root.
    /// </summary>
    /// <remarks>
    /// Implemented with a basic sequential search that makes it fast for few roots
    /// which usually should be the case.
    /// </remarks>
    /// <param name="name">The name of the root.</param>
    /// <returns>The offset or 0L if name not found.</returns>
    public long GetRoot(string name)
    {
        _lock.EnterReadLock();

        try
        {
            return InternalGetRoot(name);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private long InternalGetRoot(string name)
    {
        var reader = new BinaryReader(_stream, Encoding.UTF8);
        reader.Seek(HEADER_ROOT_LIST);
        long next = reader.ReadInt64();

        while (next != NULL)
        {
            reader.Seek(next);
            next = reader.ReadInt64();
            var offset = reader.ReadInt64();
            var label = reader.ReadString();

            if (label == name)
                return offset;
        }

        return NULL;
    }

    public IEnumerable<string> Roots
    {
        get
        {
            return _internalRoots.AsReadLocked(_lock, this);
        }
    }

    private IEnumerable<string> _internalRoots
    {
        get
        {
            var reader = new BinaryReader(_stream, Encoding.UTF8);

            reader.Seek(HEADER_ROOT_LIST);
            long next = reader.ReadInt64();

            while (next != NULL)
            {
                reader.Seek(next);
                next = reader.ReadInt64();
                var offset = reader.ReadInt64();
                var label = reader.ReadString();
                yield return label;
            }
        }
    }

    /// <summary>
    /// Sets the offset of a named root.
    /// </summary>
    /// <param name="name">Root name.</param>
    /// <param name="offset">Root offset.</param>
    public void SetRoot(string name, long offset)
    {
        _lock.EnterWriteLock();

        try
        {
            if (InternalGetRoot(name) != NULL)
                throw new ArgumentException("Named offset already defined.");

            var reader = new BinaryReader(_stream);

            reader.Seek(HEADER_ROOT_LIST);
            var root = reader.ReadInt64();

            var buffer = name.AsBytes();
            var record = InternalAlloc(buffer.Length + sizeof(long) * 2);

            var writer = new BinaryWriter(_stream);

            writer.Seek(record);
            writer.Write(root);
            writer.Write(offset);
            writer.Write(buffer);

            writer.Seek(HEADER_ROOT_LIST);
            writer.Write(record);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Allocate bytes in the heap.
    /// </summary>
    /// <param name="noOfBytes">The number of bytes to allocate.</param>
    /// <returns>The offset to the allocated bytes.</returns>
    public long Alloc(long noOfBytes)
    {
        _lock.EnterWriteLock();

        try
        {
            var offset = InternalAlloc(noOfBytes);
            Contract.Assume(InternalSizeOf(offset) > 0, "Bad size");
            ValidateHeap();

            return offset;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private long InternalAlloc(long bytesToAlloc)
    {
        //
        // We need to make sure that we have room for next & prev in a free block.
        // 

        if (bytesToAlloc < sizeof(long) * 2)  
            bytesToAlloc = sizeof(long) * 2;

        var reader = new BinaryReader(_stream);

        reader.Seek(HEADER_FREE_LIST);
        var freeList = reader.ReadInt64();
        var free = freeList;

        if (free == 0)
            throw new InsufficientMemoryException("Canyala.Mercury.Heap.Alloc");
             
        reader.Seek(free);
        var size = reader.ReadInt64();
        var next = reader.ReadInt64();
        var prev = reader.ReadInt64();

        Contract.Assume(size < 0, "First free block has a size indicating it is a used block.");

        size = -size;

        while (size < bytesToAlloc)
        {
            free = next;

            if (free == 0)
                throw new InsufficientMemoryException("Canyala.Mercury.Heap.Alloc");

            reader.Seek(free);
            size = reader.ReadInt64();
            next = reader.ReadInt64();
            prev = reader.ReadInt64();

            Contract.Assume(size < 0, "Illegal size in free list");

            size = -size;
        }

        var writer = new BinaryWriter(_stream);
        var largeEnoughToPartitionSize = bytesToAlloc + sizeof(long) * 3;
        var nextFreeBlock = next;

        if (size >= largeEnoughToPartitionSize)
        {
            var partitionedSize = size - bytesToAlloc - sizeof(long);  
            nextFreeBlock = free + bytesToAlloc + sizeof(long);

            writer.Seek(nextFreeBlock);
            writer.Write(-partitionedSize);
            writer.Write(next);
            writer.Write(prev);

            if (prev != NULL)
            {
                writer.Seek(prev + FREE_NEXT);
                writer.Write(nextFreeBlock);
            }

            if (next != NULL)
            {
                writer.Seek(next + FREE_PREV);
                writer.Write(nextFreeBlock);
            }
        }
        else
        {
            if (prev != NULL)
            {
                writer.Seek(prev + FREE_NEXT);
                writer.Write(next);
            }

            if (next != NULL)
            {
                writer.Seek(next + FREE_PREV);
                writer.Write(prev);
            }

            bytesToAlloc = size;
        }

        if (free == freeList)
        {
            Contract.Assume(prev == 0, "First free assumes that block has a logical previous of zero.");
            writer.Seek(HEADER_FREE_LIST);
            writer.Write(nextFreeBlock);
        }

        writer.Seek(free);
        writer.Write(bytesToAlloc);

        return free + sizeof(long);
    }

    /// <summary>
    /// Free the bytes allocated at offset.
    /// Attempts to garbage collect consecutive blocks.
    /// </summary>
    /// <param name="offset">The offset</param>
    public void Free(long offset)
    {
        if (offset == 0)
            throw new NullReferenceException();

        _lock.EnterWriteLock();

        try
        {
            var blockOffset = offset - sizeof(long);
            var reader = new BinaryReader(_stream);
            reader.Seek(blockOffset);
            var size = reader.ReadInt64();

            var writer = new BinaryWriter(_stream);
            writer.Seek(blockOffset);
            writer.Write(-size);

            IncludeInFreeList(blockOffset);
            InternalGarbageCollect(blockOffset);

            ValidateHeap();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Dump the heap to a stream for debugging and analysis purposes.
    /// </summary>
    /// <param name="writer">A text stream to write to.</param>
    public void Dump(StreamWriter writer, Action<StreamWriter,BinaryReader,long>? dataBlockDumper=null)
    {
    }

    private void DefaultDataBlockDumper(StreamWriter writer, BinaryReader reader, long size)
    {
    }

    /// <summary>
    /// Dump the heap to a file for debugging and analysis purposes.
    /// </summary>
    /// <param name="fileName">A file to write to.</param>
    public void Dump(string fileName, Action<StreamWriter,BinaryReader,long>? dataBlockDumper=null)
        { using (var writer = new StreamWriter(fileName)) Dump(writer, dataBlockDumper); }

    /// <summary>
    /// Heap set buffer at offset.
    /// </summary>
    /// <param name="offset">The offset, must be a valid offset.</param>
    /// <param name="buffer">The buffer, must not be larger than allocated, throws IndexOutOfRange.</param>
    public void Set(long offset, byte[] buffer)
    {
        if (offset == 0)
            throw new NullReferenceException();

        _lock.EnterWriteLock();

        try
        {
            var reader = new BinaryReader(_stream);
            reader.Seek(offset - sizeof(long));
            var size = reader.ReadInt64();

            if (buffer.Length > size)
                throw new IndexOutOfRangeException("Mercury.Heap.HeapSet");

            var writer = new BinaryWriter(_stream);

            writer.Seek(offset);
            writer.Write(buffer, 0, buffer.Length);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Get buffer at offset.
    /// </summary>
    /// <param name="offset">The offset</param>
    /// <returns>A buffer, size might be aligned on chunck and therefore larger than allocated.</returns>
    /// <exception cref="NullReferenceExcption">is thrown if offset is 0L.</exception>
    public byte[] Get(long offset)
    {
        if (offset == 0)
            throw new NullReferenceException();

        _lock.EnterReadLock();

        try
        {
            var reader = new BinaryReader(_stream);
            reader.Seek(offset - sizeof(long));

            return reader.ReadBytes((int)reader.ReadInt64());
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Creates a binary reader via a sub stream in the heap.
    /// The sub stream provides relative stream access and safety
    /// mechanisms.
    /// </summary>
    /// <param name="offset">Heap offset</param>
    /// <param name="position">The relative position to start reading at.</param>
    /// <returns>A binary reader</returns>
    public BinaryReader Reader(long offset, int position=0)
    {
        if (offset == 0)
            throw new NullReferenceException();

        var subStream = new SubStream(_stream, offset, SizeOf(offset));
        var reader = new BinaryReader(subStream);
        reader.Seek(position);
        return reader;
    }

    /// <summary>
    /// Creates a binary writer via a sub stream of the heap stream.
    /// The sub stream provides relative stream access and safety
    /// mechanisms.
    /// </summary>
    /// <param name="offset">Heap offset</param>
    /// <param name="position">The relative position to start reading at.</param>
    /// <returns>A binary writer</returns>
    public BinaryWriter Writer(long offset, int position=0)
    {
        if (offset == 0)
            throw new NullReferenceException();

        var subStream = new SubStream(_stream, offset, SizeOf(offset));
        var writer = new BinaryWriter(subStream);
        writer.Seek(position);
        return writer;
    }

    /// <summary>
    /// Indexing operator
    /// </summary>
    /// <param name="offset">Allocated offset</param>
    /// <returns>byte array</returns>
    public byte[] this [long offset]
    {
        get { return Get(offset); }
        set { Set(offset, value); }
    }

    /// <summary>
    /// Walks the heap and counts the number of free blocks.
    /// WARNING: Potentially time consuming.
    /// </summary>
    /// <returns>The number of free blocks.</returns>
    public long CountFreeBlocks()
    {
        _lock.EnterReadLock();

        try
        {
            int count = InternalCountFreeBlocksInFreeList();
            Contract.Assume(count == InternalCountFreeBlocks(), "Internal consistency failure.");
            return count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private int InternalCountFreeBlocksInFreeList()
    {
        var reader = new BinaryReader(_stream);

        reader.Seek(HEADER_FREE_LIST);
        var free = reader.ReadInt64();
        int count = 0;

        while (free > 0)
        {
            count++;
            reader.Seek(free + FREE_NEXT);
            free = reader.ReadInt64();
        }
        return count;
    }

    /// <summary>
    /// Walks the heap and counts the number of used blocks.
    /// WARNING: Potentially time consuming.
    /// </summary>
    /// <returns></returns>
    public long CountUsedBlocks()
    {
        _lock.EnterReadLock();

        try
        {
            return InternalCountUsedBlocks();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private long InternalCountUsedBlocks()
    {
        var count = 0L;
        var blockOffset = HEADER_LENGTH;
        var reader = new BinaryReader(_stream);

        while (blockOffset < _size)
        {
            reader.Seek(blockOffset);
            var blockSize = reader.ReadInt64();

            if (blockSize < 0)
                blockOffset = blockOffset + -blockSize + sizeof(long);
            else
            {
                blockOffset = blockOffset + blockSize + sizeof(long);
                count++;
            }
        }

        Contract.Assume(blockOffset == _size, "blockOffset beyond heap length");

        return count;
    }

    [Conditional("VALIDATE")]
    private void ValidateHeap()
    {
        var lastSize = 0L;
        var blockOffset = HEADER_LENGTH;
        var reader = new BinaryReader(_stream);

        while (blockOffset < _stream.Length)
        {
            reader.Seek(blockOffset);
            lastSize = reader.ReadInt64();
            var blockLength = Math.Abs(lastSize) + sizeof(long);
            blockOffset += blockLength;
        }

        Contract.Assume(_size >= _stream.Length, "Bad stream length");
        Contract.Assume(blockOffset == _size, "BlockOffset beyond end of stream.");

        var heapWalkCount = InternalCountFreeBlocks();
        var freeListCount = InternalCountFreeBlocksInFreeList();

        Contract.Assume(heapWalkCount == freeListCount, "Free list count differs from heap walk count.");
    }

    private long InternalCountFreeBlocks()
    {
        var count = 0L;
        var blockOffset = HEADER_LENGTH;
        var reader = new BinaryReader(_stream);

        while (blockOffset < _stream.Length)
        {
            reader.Seek(blockOffset);
            var blockSize = reader.ReadInt64();

            if (blockSize < 0)
            {
                blockOffset = blockOffset + -blockSize + sizeof(long);
                count++;
            }
            else
            {
                blockOffset = blockOffset + blockSize + sizeof(long);
            }
        }

        Contract.Assume(blockOffset == _size, "blockOffset beyond heap size");

        return count;
    }

    /// <summary>
    /// Walks the used block chain in order to validate an offset.
    /// WARNING: Potentially time consuming.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <returns><code>true</code> if offset is valid otherwize <code>false</code>.</returns>
    public bool IsValid(long offset)
    {
        _lock.EnterReadLock();

        try
        {
            return InternalIsValid(offset);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Validates an offset.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <returns></returns>
    private bool InternalIsValid(long offset)
    {
        if (offset == 0)
            return false;

        if (offset < 0)
            return false;

        if (offset >= _size)
            return false;

        var usedOffset = offset - sizeof(long);

        var reader = new BinaryReader(_stream);
        var blockOffset = HEADER_LENGTH;

        while (blockOffset < Math.Min(_stream.Length, _size) && blockOffset <= usedOffset)
        {
            reader.Seek(blockOffset);
            var blockSize = reader.ReadInt64();

            if (blockSize > 0)
            {
                if (usedOffset == blockOffset)
                    return true;

                blockOffset += blockSize + sizeof(long);
            }
            else
            {
                blockOffset += -blockSize + sizeof(long);
            }
        }

        return false;
    }

    /// <summary>
    /// Retrieves the size of an allocated block.
    /// Size may be larger than bytes allocated depending on heap implementation.
    /// </summary>
    /// <param name="offset">The block offset.</param>
    /// <returns>The sise of the block in bytes.</returns>
    public int SizeOf(long offset)
    {
        if (offset == 0)
            throw new NullReferenceException();

        _lock.EnterReadLock();

        try
        {
            return InternalSizeOf(offset);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// SizeOf for internal use, no thread safety.
    /// </summary>
    /// <param name="offset">An allocation offset.</param>
    /// <returns>The allocation size.</returns>
    private int InternalSizeOf(long offset)
    {
        var reader = new BinaryReader(_stream);
        reader.Seek(offset - sizeof(long));
        var size = reader.ReadInt64();

        if (size < sizeof(long) * 2 || size > _size - HEADER_LENGTH - sizeof(long))
        {
            if (InternalIsValid(offset))
                throw new InvalidOperationException("Illegal block size {0}".Args(size));
            else
                throw new ObjectDisposedException("Unallocated block offset.");
        }

        return (int)size;
    }

    /// <summary>
    /// Garbage collects the heap by concatenating adjacent free blocks.
    /// </summary>
    /// <remarks>
    /// Potentially time consuming.
    /// </remarks>
    public void GarbageCollect()
    {
        _lock.EnterUpgradeableReadLock();

        try
        {
            InternalGarbageCollect();
            ValidateHeap();
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Include an unallocated block (negative size) in the free list.
    /// </summary>
    /// <param name="blockOffset">Offset to an unallocated block.</param>
    private void IncludeInFreeList(long blockOffset)
    {
        var reader = new BinaryReader(_stream);
        reader.Seek(HEADER_FREE_LIST);
        var freeList = reader.ReadInt64();

        Contract.Assume(freeList != blockOffset, "offset must not be freelist");

        var writer = new BinaryWriter(_stream);
        writer.Seek(blockOffset + FREE_NEXT);
        writer.Write(freeList);
        writer.Write(NULL);

        if (freeList != NULL)
        {
            writer.Seek(freeList + FREE_PREV);
            writer.Write(blockOffset);
        }

        writer.Seek(HEADER_FREE_LIST);
        writer.Write(blockOffset);
    }

    /// <summary>
    /// Exclude an unallocated block from the free list.
    /// </summary>
    /// <param name="blockOffset"></param>
    private void ExcludeFromFromFreeList(long blockOffset)
    {
        var reader = new BinaryReader(_stream);
        reader.Seek(blockOffset);

        var size = reader.ReadInt64();
        var next = reader.ReadInt64();
        var prev = reader.ReadInt64();

        var writer = new BinaryWriter(_stream);

        if (prev != NULL)
        {
            writer.Seek(prev + FREE_NEXT);
            writer.Write(next);
        }

        if (next != NULL)
        {
            writer.Seek(next + FREE_PREV);
            writer.Write(prev);
        }

        reader.Seek(HEADER_FREE_LIST);
        var freeList = reader.ReadInt64();

        if (blockOffset == freeList)
        {
            writer.Seek(HEADER_FREE_LIST);
            writer.Write(next);
        }
    }

    private void InternalGarbageCollect()
    {
        var blockOffset = HEADER_LENGTH;
        var reader = new BinaryReader(_stream);

        while (blockOffset < _stream.Length)
        {
            reader.Seek(blockOffset);
            var blockSize = reader.ReadInt64();

            if (blockSize < 0)
                blockOffset = blockOffset + InternalGarbageCollect(blockOffset) + sizeof(long);
            else
                blockOffset = blockOffset + blockSize + sizeof(long);
        }

        Contract.Assume(blockOffset == _size, "blockOffset beyond heap size");
    }

    private long InternalGarbageCollect(long offset)
    {
        var reader = new BinaryReader(_stream);
        reader.Seek(offset);

        var size = reader.ReadInt64();
        Contract.Requires(size < 0, "Illegal size");
        size = -size;

        var nextOffset = offset + size + sizeof(long);

        if (nextOffset >= _stream.Length)
            return size;

        reader.Seek(nextOffset);
        var nextSize = reader.ReadInt64();

        if (nextSize < 0)
        {
            ExcludeFromFromFreeList(offset);

            while (nextSize < 0)
            {
                ExcludeFromFromFreeList(nextOffset);

                nextSize = -nextSize + sizeof(long);
                nextOffset = nextOffset + nextSize;
                size += nextSize;

                if (nextOffset < _size)
                {
                    reader.Seek(nextOffset);
                    nextSize = reader.ReadInt64();
                }
                else
                {
                    break;
                }
            }

            var writer = new BinaryWriter(_stream);
            writer.Seek(offset);
            writer.Write(-size);

            IncludeInFreeList(offset);
        }

        return size;
    }
}
