//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Storage.Allocators
{
    /// <summary>
    /// Provides a generic allocator suitable for
    /// items fitting in 64-bit fields. 
    /// </summary>
    /// <remarks>Fast but expensive. Consider using lists for storing many items.</remarks>
    /// <typeparam name="T">The type that the allocator allocates.</typeparam>
    public sealed class NullAllocator<T> : Allocator<T>
    {
        /// <summary>
        /// Allocates and stores an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The offset of the item in the heap.</returns>
        public override long Alloc(T item) 
        {
            if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                return BitConverter.DoubleToInt64Bits((double)(object)item);

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
}
