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


using Canyala.Lagoon.Core.Extensions;

using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Storage.Allocators;

/// <summary>
/// Provides a generic allocator that uses reference counting
/// to avoid duplication.
/// </summary>
/// <typeparam name="T">The type that the allocator allocates.</typeparam>
public sealed class SingletonAllocator<T> : Allocator<T>
{
    private readonly AATree _index;
    private readonly Heap _objects;
    private readonly ISerializer _serializer = Serializer.SerializerFor(typeof(T));

    /// <summary>
    /// Creates singleton allocator.
    /// </summary>
    /// <remarks>
    /// A singleton allocator makes sure that space is reused, no duplicates
    /// will be stored and reference counting is used to accomplish it.
    /// </remarks>
    /// <param name="index">The heap to store the index in.</param>
    /// <param name="objects">The heap to store objects in, defaults to the index heap.</param>
    public SingletonAllocator(Heap index, Heap objects)
    {
        long Compare(long a, long b)
        {
            if (this[a] is IComparable<T> comparable)
                return comparable.CompareTo(this[b]);

            throw new InvalidCastException($"Type {nameof(T)} does not support required {nameof(IComparable<T>)}");
        }

        _index = new AATree(index, GetType().ReadableName() + ".Index", Compare);
        _objects = objects;
    }

    /// <summary>
    /// Creates singleton allocator.
    /// </summary>
    /// <remarks>
    /// A singleton allocator makes sure that space is reused, no duplicates
    /// will be stored and reference counting is used to accomplish it.
    /// </remarks>
    /// <param name="index">The heap to store both the index and objects in.</param>
    public SingletonAllocator(Heap index) : this(index, index)
    {
        ;
    }


    /// <summary>
    /// Allocates and stores an item in a heap.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The offset of the item in the heap.</returns>
    public override long Alloc(T item)
    {
        Action<long[]> init = data => { if (data[0] == 0) data[0] = AllocSingleton(item);  };

        if (!(item is IComparable<T> comparableItem))
            throw new InvalidCastException($"{nameof(SingletonAllocator<T>)} : Type {nameof(T)} does not support {nameof(IComparable<T>)}");

        var offsets = _index.GetData(_index.Insert(data => comparableItem.CompareTo(this[data]), init));
        UInt32 references = _objects.Reader(offsets[0]).ReadUInt32();
        _objects.Writer(offsets[0]).Write(references + 1);
        return offsets[0];
    }

    /// <summary>
    /// Access the value of an item from the heap.
    /// </summary>
    /// <param name="offset">The offset of the item in the heap.</param>
    /// <returns>The value of item.</returns>
    public override T DeReference(long offset)
    {
        var buffer = _objects[offset];
        var valueBuffer = new byte[buffer.Length - sizeof(UInt32)];
        Array.Copy(buffer, sizeof(UInt32), valueBuffer, 0, valueBuffer.Length);
        return (T) _serializer.Deserialize(valueBuffer);
    }

    /// <summary>
    /// Free the memory of a singleton item if the number of references reach zero.
    /// </summary>
    /// <param name="offset"></param>
    public override void Free(long offset)
    {
        var reader = _objects.Reader(offset);
        UInt32 references = reader.ReadUInt32() - 1;

        if (references == 0)
        {
            FreeSingleton(offset);
            return;
        }

        var writer = _objects.Writer(offset);
        writer.Write(references);
    }

    #region - Internal -

    private long AllocSingleton(T item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        var buffer = _serializer.Serialize(item);
        // We allocate +sizeof(Uint32) to make room for a count.
        var offset = _objects.Alloc(sizeof(UInt32) + buffer.Length);
        var writer = _objects.Writer(offset);
        writer.Write((uint) 0);
        writer.Write(buffer);

        return offset;
    }

    private void FreeSingleton(long offset)
    {
        if (this[offset] is IComparable<T> comparableItem)
            _index.Remove(data => comparableItem.CompareTo(this[data]), offsets => _objects.Free(offsets[0]));
    }

    #endregion
}
