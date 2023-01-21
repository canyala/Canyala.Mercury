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
/// Provides a generic allocator suitable for
/// items fitting in 64-bit fields (e.g. value-types). 
/// </summary>
/// <remarks>
/// Fast but expensive. Consider using lists for storing many items.
/// </remarks>
/// <typeparam name="T">The type that the allocator allocates.</typeparam>
public sealed class NullAllocator<T> : Allocator<T>
{
    /// <summary>
    /// Allocates and stores an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The offset (e.g. value) of the item in the heap.</returns>
    public override long Alloc(T item) 
    {
        if (item is float @float)
            return BitConverter.DoubleToInt64Bits(@float);

        if (item is double @double)
            return BitConverter.DoubleToInt64Bits(@double);

        return Convert.ToInt64(item); 
    }

    /// <summary>
    /// Access the value of an item from the heap.
    /// </summary>
    /// <param name="offset">The offset of the item in the heap.</param>
    /// <returns>The value of item.</returns>
    public override T DeReference(long offset) 
    { 
        if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
            return (T)(object) BitConverter.Int64BitsToDouble(offset);

        return (T) Convert.ChangeType(offset, typeof(T)); 
    }

    /// <summary>
    /// Release the space occupied by the item at offset.
    /// </summary>
    /// <param name="offset">The offset.</param>
    public override void Free(long offset) 
        {}
}
