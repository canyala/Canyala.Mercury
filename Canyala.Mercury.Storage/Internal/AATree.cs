/*

  MIT License
 
  Copyright (c) 2022 Canyala Innovation (Martin Fredriksson)

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;

using Canyala.Lagoon.Contracts;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

using Canyala.Mercury.Storage.Extensions;

[assembly: InternalsVisibleToAttribute("Canyala.Mercury.Test")]

namespace Canyala.Mercury.Storage.Internal;

/// <summary>
/// Provides a heap based AA (Arne Andersson) tree implementation suitable as a building
/// block for various heap based collection implementations.
/// </summary>
internal sealed class AATree
{
    const int HEADER_ROOT = 0;
    const int HEADER_REFCOUNT = HEADER_ROOT + sizeof(long);
    const int HEADER_ITEMCOUNT = HEADER_REFCOUNT + sizeof(long);
    const int HEADER_LENGTH = HEADER_ITEMCOUNT + sizeof(long);

    const int NODE_LEVEL = 0;
    const int NODE_LEFT_OFFSET = NODE_LEVEL + sizeof(int);
    const int NODE_RIGHT_OFFSET = NODE_LEFT_OFFSET + sizeof(long);
    const int NODE_DATA = NODE_RIGHT_OFFSET + sizeof(long);

    int NODE_LENGTH { get { return NODE_DATA + sizeof(long) * _dataFieldCount; } }

    const long NULL = 0L;

    private readonly Heap _heap;
    private readonly long _headerOffset;
    private readonly int _dataFieldCount;
    private readonly Func<long, long, long> _fnCompare;

    // Recursive locks are supported to allow enumerations that contain reads.
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    /// <summary>
    /// Creates a new AATree in a heap.
    /// </summary>
    /// <param name="heap"></param>
    /// <param name="fnCompare"></param>
    /// <param name="dataFieldCount"></param>
    public AATree(Heap heap, Func<long,long,long> fnCompare, int dataFieldCount=1)
    {
        Contract.Requires(dataFieldCount >= 1, "Number of data fields must be greater than 0.");

        _heap = heap;
        _fnCompare = fnCompare;
        _dataFieldCount = dataFieldCount;
        _headerOffset = _heap.Alloc(HEADER_LENGTH);

        var writer = _heap.Writer(_headerOffset);
        writer.Write(NULL);
        writer.Write(1L);
        writer.Write(0L);
    }

    /// <summary>
    /// Creates or recreates an AATree in a heap from a named root.
    /// </summary>
    /// <param name="heap"></param>
    /// <param name="name"></param>
    /// <param name="fnCompare"></param>
    /// <param name="dataFieldCount"></param>
    public AATree(Heap heap, string name, Func<long, long, long> fnCompare, int dataFieldCount = 1)
    {
        Contract.Requires(dataFieldCount >= 1, "Number of data fields must be greater than 0.");

        _heap = heap;
        _fnCompare = fnCompare;
        _dataFieldCount = dataFieldCount;
        _headerOffset = _heap.GetRoot(name);

        if (_headerOffset == 0)
        {
            // create a new named tree
            _headerOffset = _heap.Alloc(HEADER_LENGTH);
            _heap.SetRoot(name, _headerOffset);

            var writer = _heap.Writer(_headerOffset);
            writer.Write(NULL);
            writer.Write(2L); // Named, self referential
            writer.Write(0L);
        } 
        else
        {
            // resurrect a named tree
            IncreaseReferenceCount(); // We increase on behalf of this new proxy object.
        }
    }

    /// <summary>
    /// Recreates an AATree from an offset in heap.
    /// </summary>
    /// <param name="heap"></param>
    /// <param name="offset"></param>
    /// <param name="compare"></param>
    /// <param name="dataFieldCount"></param>
    public AATree(Heap heap, long offset, Func<long, long, long> compare, int dataFieldCount = 1)
    {
        Contract.Requires(dataFieldCount >= 1, "Number of data fields must be greater than 0.");

        _heap = heap;
        _headerOffset = offset;
        _dataFieldCount = dataFieldCount;
        _fnCompare = compare;

        IncreaseReferenceCount(); // We increase on behalf of this new proxy object.
    }

    /// <summary>
    /// The offset of an AATree in it's heap.
    /// </summary>
    public long Offset 
        { get { return _headerOffset; } }

    /// <summary>
    /// Increase the reference count.
    /// </summary>
    /// <returns>The increased count.</returns>
    public long IncreaseReferenceCount()
    {
        try
        {
            _lock.EnterWriteLock();

            var reader = _heap.Reader(_headerOffset, HEADER_REFCOUNT);
            var refCount = reader.ReadInt64() + 1L;

            var writer = _heap.Writer(_headerOffset, HEADER_REFCOUNT);
            writer.Write(refCount);

            return refCount;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Decrease the reference count.
    /// If receiving 0, Destroy() should be invoked.
    /// </summary>
    /// <returns>The decreased count.</returns>
    public long DecreaseReferenceCount()
    {
        try
        {
            _lock.EnterWriteLock();

            var reader = _heap.Reader(_headerOffset, HEADER_REFCOUNT);
            var refCount = reader.ReadInt64();

            if (refCount == 0)
                throw new InvalidOperationException("Decrease on a 0 refcount.");

            refCount = refCount -  1L;
            var writer = _heap.Writer(_headerOffset, HEADER_REFCOUNT);
            writer.Write(refCount);

            return refCount;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Destroys an AATree. 
    /// A suitable action after receiving 0 from DecreaseReferenceCount().
    /// </summary>
    /// <param name="freeData"></param>
    public void Destroy(Action<long[]> freeData)
    {
        Clear(freeData);
        _heap.Free(_headerOffset);
    }

    /// <summary>
    /// Retrieve the data in an AATree node.
    /// </summary>
    /// <param name="offset">The offset to an AATree node.</param>
    /// <returns>The node data as an array of long.</returns>
    public long[] GetData(long offset)
    { 
        try
        {
            _lock.EnterReadLock();

            return _heap.Reader(offset, NODE_DATA).ReadInt64s(_dataFieldCount); 
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// If this tree is read only, relies on the underlying storage.
    /// </summary>
    public bool IsReadOnly { get { return _heap.IsReadOnly; } }

    /// <summary>
    /// Makes a new AATree node.
    /// </summary>
    /// <param name="data">The data for the node.</param>
    /// <returns>The offset of the node in the heap.</returns>
    private long MakeNode(long[] data)
    {
        Contract.Requires(_dataFieldCount == data.Length, "dataFieldCount != data.Length");

        var reader = _heap.Reader(_headerOffset, HEADER_ITEMCOUNT);
        var oldCount = reader.ReadInt64();

        var writer = _heap.Writer(_headerOffset, HEADER_ITEMCOUNT);
        writer.Write(oldCount + 1);

        var nodeOffset = _heap.Alloc(NODE_LENGTH);
        writer = _heap.Writer(nodeOffset);

        writer.Write(1);
        writer.Write(NULL);
        writer.Write(NULL);
        writer.Write(data);

        return nodeOffset;
    }

    /// <summary>
    /// Skew's an AATree node.
    /// </summary>
    /// <param name="nodeOffset">The offset of the node in the heap.</param>
    /// <returns>The offset of the possibly relocated node.</returns>
    private long Skew(long nodeOffset)
    {
        if (nodeOffset == NULL)
            return NULL;

        var nodeReader = _heap.Reader(nodeOffset);
        var nodeLevel = nodeReader.ReadInt32();
        var nodeLeftOffset = nodeReader.ReadInt64();

        if (nodeLeftOffset != NULL)
        {
            var nodeLeftReader = _heap.Reader(nodeLeftOffset);
            var nodeLeftLevel = nodeLeftReader.ReadInt32();

            if (nodeLeftLevel == nodeLevel)
            {
                nodeLeftReader.Seek(NODE_RIGHT_OFFSET);
                var nodeLeftRight = nodeLeftReader.ReadInt64();

                var nodeWriter = _heap.Writer(nodeOffset, NODE_LEFT_OFFSET);
                nodeWriter.Write(nodeLeftRight);

                var nodeLeftWriter = _heap.Writer(nodeLeftOffset, NODE_RIGHT_OFFSET);
                nodeLeftWriter.Write(nodeOffset);

                return nodeLeftOffset;
            }
        }
      
        return nodeOffset;
    }

    /// <summary>
    /// Split's an AATree node.
    /// </summary>
    /// <param name="nodeOffset">The offset of the node in the heap.</param>
    /// <returns>The possibly relocated node offset.</returns>
    private long Split(long nodeOffset)
    {
        if (nodeOffset == NULL)
            return NULL;

        var nodeReader = _heap.Reader(nodeOffset, NODE_LEVEL);
        var nodeLevel = nodeReader.ReadInt32();

        nodeReader.Seek(NODE_RIGHT_OFFSET);
        var nodeRightOffset = nodeReader.ReadInt64();

        if (nodeRightOffset == NULL)
            return nodeOffset;

        var nodeRightReader = _heap.Reader(nodeRightOffset, NODE_RIGHT_OFFSET);
        var nodeRightRightOffset = nodeRightReader.ReadInt64();

        if (nodeRightRightOffset == NULL)
            return nodeOffset;

        var nodeRightRightReader = _heap.Reader(nodeRightRightOffset, NODE_LEVEL);
        var nodeRightRightLevel = nodeRightRightReader.ReadInt32();

        if (nodeRightRightLevel != nodeLevel)
            return nodeOffset;

        nodeRightReader.Seek(NODE_LEVEL);
        var nodeRightLevel = nodeRightReader.ReadInt32();
        var nodeRightLeftOffset = nodeRightReader.ReadInt64();

        var nodeWriter = _heap.Writer(nodeOffset, NODE_RIGHT_OFFSET);
        nodeWriter.Write(nodeRightLeftOffset);

        var nodeRightWriter = _heap.Writer(nodeRightOffset, NODE_LEVEL);
        nodeRightWriter.Write(nodeRightLevel + 1);
        nodeRightWriter.Write(nodeOffset);

        return nodeRightOffset;
    }

    public long Insert(Func<long,long> compareTo, Action<long[]> initData)
    {
        try
        {
            _lock.EnterWriteLock();

            var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);

            var rootOffset = headerReader.ReadInt64();
            var insertOffset = InsertNode(rootOffset, compareTo, initData, out long locatedOffset);

            if (insertOffset == rootOffset)
                return locatedOffset;

            var headerWriter = _heap.Writer(_headerOffset, HEADER_ROOT);
            headerWriter.Write(rootOffset = insertOffset);

            return locatedOffset;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private long InsertNode(long nodeOffset, Func<long,long> compareTo, Action<long[]> initData, out long locatedOffset)
    {
        if (nodeOffset == NULL)
        {
            var data = new long[_dataFieldCount];
            initData(data);

            locatedOffset = MakeNode(data);
            return locatedOffset;
        }

        var nodeReader = _heap.Reader(nodeOffset, NODE_DATA);
        var nodeData = nodeReader.ReadInt64s(_dataFieldCount);
        var direction = compareTo(nodeData[0]);

        if (direction < 0)
        {
            nodeReader.Seek(NODE_LEFT_OFFSET);
            var nodeLeftOffset = nodeReader.ReadInt64();
            var insertedOffset = InsertNode(nodeLeftOffset, compareTo, initData, out locatedOffset);
            var nodeWriter = _heap.Writer(nodeOffset, NODE_LEFT_OFFSET);
            nodeWriter.Write(insertedOffset);
        }
        else if (direction > 0)
        {
            nodeReader.Seek(NODE_RIGHT_OFFSET);
            var nodeRightOffset = nodeReader.ReadInt64();
            var insertedOffset = InsertNode(nodeRightOffset, compareTo, initData, out locatedOffset);
            var nodeWriter = _heap.Writer(nodeOffset, NODE_RIGHT_OFFSET);
            nodeWriter.Write(insertedOffset);
        }
        else
        {
            if (nodeData.Length > 1)
            {
                initData(nodeData);
                var nodeWriter = _heap.Writer(nodeOffset, NODE_DATA);
                nodeWriter.Write(nodeData);
            }

            return locatedOffset = nodeOffset;
        }

        nodeOffset = Skew(nodeOffset);
        nodeOffset = Split(nodeOffset);

        return nodeOffset;
    }

    public bool Remove(Func<long,long> compareTo, Action<long[]> freeData)
    {
        try
        {
            _lock.EnterWriteLock();

            var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
            var rootOffset = headerReader.ReadInt64();

            if (rootOffset == NULL)
                return false;

            var nodeOffset = RemoveNode(rootOffset, compareTo, freeData);

            if (nodeOffset != rootOffset)
            {
                var headerWriter = _heap.Writer(_headerOffset, HEADER_ROOT);
                headerWriter.Write(nodeOffset);
            }

            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private long RemoveNode(long nodeOffset, Func<long,long> compareTo, Action<long[]> freeData)
    {
        var nodeReader = _heap.Reader(nodeOffset);

        var nodeLevel = nodeReader.ReadInt32();
        var nodeLeftOffset = nodeReader.ReadInt64();
        var nodeRightOffset = nodeReader.ReadInt64();
        var nodeData = nodeReader.ReadInt64s(_dataFieldCount);

        var direction = compareTo(nodeData[0]);

        if (direction > 0)
        {
            if (nodeRightOffset != NULL)
            {
                var removeNode = RemoveNode(nodeRightOffset, compareTo, freeData);

                if (removeNode != nodeRightOffset)
                {
                    var nodeWriter = _heap.Writer(nodeOffset, NODE_RIGHT_OFFSET);
                    nodeWriter.Write(nodeRightOffset = removeNode);
                }
            }
        }
        else if (direction < 0)
        {
            if (nodeLeftOffset != NULL)
            {
                var removeNode = RemoveNode(nodeLeftOffset, compareTo, freeData);

                if (removeNode != nodeLeftOffset)
                {
                    var nodeWriter = _heap.Writer(nodeOffset, NODE_LEFT_OFFSET);
                    nodeWriter.Write(nodeLeftOffset = removeNode);
                }
            }
        }
        else
        {
            if (nodeLeftOffset == NULL && nodeRightOffset == NULL)
            {
                var reader = _heap.Reader(_headerOffset, HEADER_ITEMCOUNT);
                var oldCount = reader.ReadInt64();

                var writer = _heap.Writer(_headerOffset, HEADER_ITEMCOUNT);
                writer.Write(oldCount - 1);

                freeData(nodeData);
                _heap.Free(nodeOffset);
                return NULL;
            }
            else if (nodeLeftOffset == NULL)
            {
                var nextData = NextData(nodeRightOffset, NODE_LEFT_OFFSET);
                var removeNode = RemoveNode(nodeRightOffset, data => _fnCompare(nextData[0], data), data => {});
                var nodeWriter = _heap.Writer(nodeOffset);

                if (removeNode != nodeRightOffset)
                {
                    nodeWriter.Seek(NODE_RIGHT_OFFSET);
                    nodeWriter.Write(nodeRightOffset = removeNode);
                }

                freeData(nodeData);
                nodeWriter.Seek(NODE_DATA);
                nodeWriter.Write(nodeData = nextData);
            }
            else
            {
                var prevData = NextData(nodeLeftOffset, NODE_RIGHT_OFFSET);
                var removeNode = RemoveNode(nodeLeftOffset, data => _fnCompare(prevData[0], data), data => {});
                var nodeWriter = _heap.Writer(nodeOffset);

                if (removeNode != nodeLeftOffset)
                {
                    nodeWriter.Seek(NODE_LEFT_OFFSET);
                    nodeWriter.Write(nodeLeftOffset = removeNode);
                }

                freeData(nodeData);
                nodeWriter.Seek(NODE_DATA);
                nodeWriter.Write(nodeData = prevData);
            }
        }

        var nodeLeftLevel = 0;
        if (nodeLeftOffset != NULL)
        {
            var nodeLeftReader = _heap.Reader(nodeLeftOffset, NODE_LEVEL);
            nodeLeftLevel = nodeLeftReader.ReadInt32();
        }

        var nodeRightLevel = 0;
        if (nodeRightOffset != NULL)
        {
            var nodeRightReader = _heap.Reader(nodeRightOffset, NODE_LEVEL);
            nodeRightLevel = nodeRightReader.ReadInt32();
        }

        var shouldBe = Math.Min(nodeLeftLevel, nodeRightLevel) + 1;

        if (shouldBe < nodeLevel)
        {
            var nodeWriter = _heap.Writer(nodeOffset, NODE_LEVEL);
            nodeWriter.Write(nodeLevel = shouldBe);

            if (shouldBe < nodeRightLevel)
            {
                var nodeRightWriter = _heap.Writer(nodeRightOffset, NODE_LEVEL);
                nodeRightWriter.Write(nodeRightLevel = shouldBe);
            }
        }

        var skewOffset = Skew(nodeOffset);

        if (skewOffset != nodeOffset)
        {
            nodeOffset = skewOffset;
            nodeReader = _heap.Reader(nodeOffset = skewOffset, NODE_RIGHT_OFFSET);
            nodeRightOffset = nodeReader.ReadInt64();
        }

        if (nodeRightOffset != NULL)
        {
            skewOffset = Skew(nodeRightOffset);

            var nodeRightReader = _heap.Reader(nodeRightOffset);

            if (skewOffset != nodeRightOffset)
            {
                var nodeWriter = _heap.Writer(nodeOffset, NODE_RIGHT_OFFSET);
                nodeWriter.Write(nodeRightOffset = skewOffset);
                nodeRightReader = _heap.Reader(nodeRightOffset);
            }

            nodeRightReader.Seek(NODE_RIGHT_OFFSET);
            var nodeRightRightOffset = nodeRightReader.ReadInt64();

            if (nodeRightRightOffset != NULL)
            {
                skewOffset = Skew(nodeRightRightOffset);

                if (skewOffset != nodeRightRightOffset)
                {
                    var nodeRightWriter = _heap.Writer(nodeRightOffset, NODE_RIGHT_OFFSET);
                    nodeRightWriter.Write(nodeRightRightOffset = skewOffset);
                }
            }
        }

        var splitOffset = Split(nodeOffset);

        if (splitOffset != nodeOffset)
        {
            nodeReader = _heap.Reader(nodeOffset = splitOffset, NODE_RIGHT_OFFSET);
            nodeRightOffset = nodeReader.ReadInt64();
        }

        if (nodeRightOffset != NULL)
        {
            splitOffset = Split(nodeRightOffset);

            if (splitOffset != nodeRightOffset)
            {
                var nodeWriter = _heap.Writer(nodeOffset, NODE_RIGHT_OFFSET);
                nodeWriter.Write(nodeRightOffset = splitOffset);
            }
        }

        return nodeOffset;
    }

    private long[] NextData(long nodeOffset, int leftOrRightOffset)
    {
        var nodeReader = _heap.Reader(nodeOffset, leftOrRightOffset);
        var nextOffset = nodeReader.ReadInt64();

        while (nextOffset != NULL)
        {
            nodeReader = _heap.Reader(nextOffset, leftOrRightOffset);
            nextOffset = nodeReader.ReadInt64();
        }

        nodeReader.Seek(NODE_DATA);
        var data = nodeReader.ReadInt64s(_dataFieldCount);
        return data;
    }

    public long[]? Search(Func<long,long> compareTo)
    {
        try
        {
            _lock.EnterReadLock();

            var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
            var rootOffset = headerReader.ReadInt64();
            return SearchNode(rootOffset, compareTo);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private long[]? SearchNode(long nodeOffset, Func<long,long> compareTo)
    {
        if (nodeOffset == NULL)
            return null;

        var nodeReader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);
        var nodeLeftOffset = nodeReader.ReadInt64();
        var nodeRightOffset = nodeReader.ReadInt64();
        var nodeData = nodeReader.ReadInt64s(_dataFieldCount);

        long direction = compareTo(nodeData[0]);

        if (direction < 0)
            return SearchNode(nodeLeftOffset, compareTo);
        else if (direction > 0)
            return SearchNode(nodeRightOffset, compareTo);
        else
            return nodeData;
    }

    public long[]? Min()
    {
        var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
        var nodeOffset = headerReader.ReadInt64();

        if (nodeOffset == NULL)
            return null;

        var nodeReader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);
        var nodeLeftOffset = nodeReader.ReadInt64();
        var nodeRightOffset = nodeReader.ReadInt64();
        var nodeData = nodeReader.ReadInt64s(_dataFieldCount);

        while (nodeLeftOffset != NULL)
        {
            nodeReader = _heap.Reader(nodeLeftOffset, NODE_LEFT_OFFSET);
            nodeLeftOffset = nodeReader.ReadInt64();
            nodeRightOffset = nodeReader.ReadInt64();
            nodeData = nodeReader.ReadInt64s(_dataFieldCount);
        }

        return nodeData;
    }

    public long[]? Max()
    {
        var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
        var nodeOffset = headerReader.ReadInt64();

        if (nodeOffset == NULL)
            return null;

        var nodeReader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);
        var nodeLeftOffset = nodeReader.ReadInt64();
        var nodeRightOffset = nodeReader.ReadInt64();
        var nodeData = nodeReader.ReadInt64s(_dataFieldCount);

        while (nodeRightOffset != NULL)
        {
            nodeReader = _heap.Reader(nodeRightOffset, NODE_LEFT_OFFSET);
            nodeLeftOffset = nodeReader.ReadInt64();
            nodeRightOffset = nodeReader.ReadInt64();
            nodeData = nodeReader.ReadInt64s(_dataFieldCount);
        }

        return nodeData;
    }

    private long FindOffset(Func<long, long> compareTo)
    {
        var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
        var rootOffset = headerReader.ReadInt64();
        return FindOffset(rootOffset, compareTo);
    }

    private long FindOffset(long nodeOffset, Func<long, long> compareTo)
    {
        if (nodeOffset == NULL)
            return NULL;

        var nodeReader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);
        var nodeLeftOffset = nodeReader.ReadInt64();
        var nodeRightOffset = nodeReader.ReadInt64();
        var nodeData = nodeReader.ReadInt64s(_dataFieldCount);

        long direction = compareTo(nodeData[0]);

        if (direction < 0)
            return FindOffset(nodeLeftOffset, compareTo);
        else if (direction > 0)
            return FindOffset(nodeRightOffset, compareTo);
        else
            return nodeOffset;
    }

    public long Count()
    {
        try
        {
            _lock.EnterReadLock();

            var reader = _heap.Reader(_headerOffset, HEADER_ITEMCOUNT);
            var count = reader.ReadInt64();

#if VALIDATE
            reader.Seek(HEADER_ROOT);
            var root = reader.ReadInt64();
            var rootCount = Count(root);

            Contract.Assume(count == rootCount, "Internal logic: Computed and counted node counts differ.");
#endif

            return count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private long Count(long nodeOffset)
    {
        if (nodeOffset == NULL)
            return 0L;

        var nodeReader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);
        var ltOffset = nodeReader.ReadInt64();
        var rtOffset = nodeReader.ReadInt64();

        return 1L + Count(ltOffset) + Count(rtOffset);
    }

    /// <summary>
    /// Enumerates a tree in forward or reverse order
    /// </summary>
    /// <param name="ascendingOrder"></param>
    /// <returns></returns>
    public IEnumerable<long[]> Enumerate(bool ascendingOrder=true)
    {
        var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
        return Enumerate(headerReader.ReadInt64(), ascendingOrder).AsReadLocked(_lock, this);
    }

    private IEnumerable<long[]> Enumerate(long nodeOffset, bool ascendingOrder)
    {
        if (nodeOffset == NULL)
            yield break;

        var nodeReader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);

        var nodeLeftOffset = nodeReader.ReadInt64();
        var nodeRightOffset = nodeReader.ReadInt64();
        var nodeData = nodeReader.ReadInt64s(_dataFieldCount);

        if (ascendingOrder)
        {
            if (nodeLeftOffset != NULL)
                foreach (var data in Enumerate(nodeLeftOffset, ascendingOrder))
                    yield return data;

            yield return nodeData;

            if (nodeRightOffset != NULL)
                foreach (var data in Enumerate(nodeRightOffset, ascendingOrder))
                    yield return data;
        }
        else
        {
            if (nodeRightOffset != NULL)
                foreach (var data in Enumerate(nodeRightOffset, ascendingOrder))
                    yield return data;

            yield return nodeData;

            if (nodeLeftOffset != NULL)
                foreach (var data in Enumerate(nodeLeftOffset, ascendingOrder))
                    yield return data;
        }
    }

    /// <summary>
    /// Enumerate starting at a specific element
    /// </summary>
    /// <param name="compareTo"></param>
    /// <param name="ascendingOrder"></param>
    /// <returns></returns>
    public IEnumerable<long[]> Enumerate(Func<long, long> compareTo, bool ascendingOrder, bool inclusive)
    {
        var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
        return Enumerate(headerReader.ReadInt64(), compareTo, ascendingOrder, inclusive).AsReadLocked(_lock, this);
    }

    public IEnumerable<long[]> Enumerate(Func<long,long> compareToLo, Func<long,long> compareToHi, bool ascending, bool inclusive)
    {
        return EnumerateInternal(compareToLo, compareToHi, ascending, inclusive).AsReadLocked(_lock, this);
    }

    private IEnumerable<long[]> EnumerateInternal(Func<long,long> compareLo, Func<long,long> compareHi, bool ascending, bool inclusive)
    {
        Predicate<long> lowBreaker = item => compareLo(item) >= 0;
        Predicate<long> lowEqual = item => compareLo(item) == 0;

        Predicate<long> highBreaker = item => compareHi(item) <= 0;
        Predicate<long> highEqual = item => compareHi(item) == 0;

        var breaker = ascending ?  highBreaker : lowBreaker;
        var starter = ascending ? compareLo : compareHi;
        var equal = ascending ? highEqual : lowEqual;

        var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
        foreach (var data in Enumerate(headerReader.ReadInt64(), starter, ascending, inclusive))
        {
            if (breaker(data[0]))
            {
                if (inclusive || equal(data[0]))
                    yield return data;

                yield break;
            }

            yield return data;
        }
    }

    private IEnumerable<long[]> Enumerate(long nodeOffset, Func<long, long> compareTo, bool ascendingOrder, bool inclusive)
    {
        if (nodeOffset == NULL)
            yield break;

        var nodeReader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);

        var nodeLeftOffset = nodeReader.ReadInt64();
        var nodeRightOffset = nodeReader.ReadInt64();
        var nodeData = nodeReader.ReadInt64s(_dataFieldCount);
        var compare = compareTo(nodeData[0]);

        if (ascendingOrder)
        {
            if (compare < 0 && nodeLeftOffset != NULL)
                foreach (var data in Enumerate(nodeLeftOffset, compareTo, ascendingOrder, inclusive))
                    yield return data;

            bool yieldNodeData = inclusive || compare <= 0;

            if (nodeRightOffset != NULL)
            {
                foreach (var data in Enumerate(nodeRightOffset, compareTo, ascendingOrder, inclusive))
                {
                    if (yieldNodeData)
                    {
                        yieldNodeData = false;
                        if (compareTo(data[0]) < 0)
                            yield return nodeData;
                    }

                    yield return data;
                }
            }

            if (yieldNodeData)
                yield return nodeData;
        }
        else
        {
            if (compare > 0 && nodeRightOffset != NULL)
                foreach (var data in Enumerate(nodeRightOffset, compareTo, ascendingOrder, inclusive))
                    yield return data;

            bool yieldNodeData = inclusive || compare >= 0;

            if (nodeLeftOffset != NULL)
            {
                foreach (var data in Enumerate(nodeLeftOffset, compareTo, ascendingOrder, inclusive))
                {
                    if (yieldNodeData)
                    {
                        yieldNodeData = false;
                        if (compareTo(data[0]) > 0)
                            yield return nodeData;
                    }

                    yield return data;
                }
            }

            if (yieldNodeData)
                yield return nodeData;
        }
    }

   
    public void Clear(Action<long[]> freeData, Predicate<long>? keepReference=null)
    {
        try
        {
            _lock.EnterWriteLock();

            keepReference ??= data => false;

            var headerReader = _heap.Reader(_headerOffset, HEADER_ROOT);
            var rootOffset = headerReader.ReadInt64();

            if (rootOffset != NULL)
            {
                var nodeOffset = ClearNode(rootOffset, freeData, keepReference);

                if (nodeOffset != rootOffset)
                {
                    var headerWriter = _heap.Writer(_headerOffset, HEADER_ROOT);
                    headerWriter.Write(nodeOffset);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private long ClearNode(long nodeOffset, Action<long[]> freeData, Predicate<long> keepReference)
    {
        if (nodeOffset == NULL)
            return NULL;

        var reader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);
        var lOffset = reader.ReadInt64();
        var rOffset = reader.ReadInt64();
        var data = reader.ReadInt64();

        while (!keepReference(data))
        {
            nodeOffset = RemoveNode(nodeOffset, d => _fnCompare(data, d), freeData);

            if (nodeOffset == NULL)
                return NULL;

            reader = _heap.Reader(nodeOffset, NODE_LEFT_OFFSET);
            lOffset = reader.ReadInt64();
            rOffset = reader.ReadInt64();
            data = reader.ReadInt64();
        }

        if (lOffset != NULL)
        {
            var clearNode = ClearNode(lOffset, freeData, keepReference);

            if (clearNode != lOffset)
            {
                var writer = _heap.Writer(nodeOffset, NODE_LEFT_OFFSET);
                writer.Write(lOffset = clearNode);
            }
        }

        if (rOffset != NULL)
        {
            var clearNode = ClearNode(rOffset, freeData, keepReference);

            if (clearNode != rOffset)
            {
                var writer = _heap.Writer(nodeOffset, NODE_RIGHT_OFFSET);
                writer.Write(rOffset = clearNode);
            }
        }

        return nodeOffset;
    }
}
