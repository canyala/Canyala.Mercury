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
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Storage.Allocators;

/// <summary>
/// Provides a generic allocator that stores actual objects.
/// </summary>
/// <typeparam name="T">The type that the allocator allocates.</typeparam>
public sealed class ValueAllocator<T> : Allocator<T>
{
    private readonly Heap _objects;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Creates a value allocator.
    /// </summary>
    /// <param name="objects">The heap for object storage.</param>
    public ValueAllocator(Heap objects)
    {
        _objects = objects;
        _serializer = Serializer.SerializerFor(typeof(T));
    }

    /// <summary>
    /// Allocates and stores an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The offset of the item in the heap.</returns>
    public override long Alloc(T item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        var buffer = _serializer.Serialize(item);
        var dataOffset = _objects.Alloc(buffer.Length);
        _objects[dataOffset] = buffer;
        return dataOffset;
    }

    /// <summary>
    /// Access the value of an item stored in a heap.
    /// </summary>
    /// <param name="offset">The offset of the item in the heap.</param>
    /// <returns>The value of item.</returns>
    public override T DeReference(long offset)
    {
        var buffer = _objects[offset];
        return (T)_serializer.Deserialize(buffer);
    }

    /// <summary>
    /// Release the space occupied by the item at offset.
    /// </summary>
    /// <param name="offset">The offset.</param>
    public override void Free(long offset)
        { _objects.Free(offset); }
}
