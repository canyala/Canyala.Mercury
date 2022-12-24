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
    /// Provides a unit container for allocators.
    /// </summary>
    public class Allocator
    {
    }

    /// <summary>
    /// Provides an abstraction for a generic allocator.
    /// </summary>
    /// <typeparam name="T">The type that the allocator allocates.</typeparam>
    public abstract class Allocator<T> : Allocator
    {
        /// <summary>
        /// Allocates and stores an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The offset of the item in the heap.</returns>
        public abstract long Alloc(T item);

        /// <summary>
        /// Access the value of an item from the heap.
        /// </summary>
        /// <param name="offset">The offset of the item in the heap.</param>
        /// <returns>The value of item.</returns>
        public abstract T DeReference(long offset);

        /// <summary>
        /// Access the value of an item from the heap.
        /// </summary>
        /// <param name="offset">The offset of the item in the heap.</param>
        /// <returns>The value of item.</returns>
        public T this [long offset] 
            { get { return DeReference(offset); } }

        /// <summary>
        /// Release the space occupied by the item at offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public abstract void Free(long offset);
    }
}
