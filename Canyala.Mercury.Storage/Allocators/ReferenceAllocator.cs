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
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Storage.Allocators
{
    /// <summary>
    /// Provides a generic allocator that stores actual objects.
    /// </summary>
    /// <typeparam name="T">The type that the allocator allocates.</typeparam>
    public sealed class ReferenceAllocator<T> : Allocator<T>
    {
        private readonly Environment _environment;

        /// <summary>
        /// Create a reference allocator for heap objects.
        /// </summary>
        /// <param name="environment">The environment for the allocator.</param>
        public ReferenceAllocator(Environment environment)
            { _environment = environment; }

        /// <summary>
        /// Allocates and stores an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The offset of the item in the heap.</returns>
        public override long Alloc(T item)
        {
            var heapObject = item as Object;
            heapObject.AddReference();
            return heapObject.Offset;
        }

        /// <summary>
        /// Access the value of an item stored in a heap.
        /// </summary>
        /// <param name="offset">The offset of the item in the heap.</param>
        /// <returns>The value of item.</returns>
        public override T DeReference(long offset)
        {
            var type = typeof(T);
            var argumentTypes = new Type[] { typeof(Environment), typeof(long) };
            var constructor = type.GetConstructor(BindingFlags.Instance|BindingFlags.Public, null, argumentTypes, null);
            return (T) constructor.Invoke(new object[] { _environment, offset });
        }

        /// <summary>
        /// Release the space occupied by the item at offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public override void Free(long offset)
        {
            // Creating the dereferenced object creates an additional reference that
            // is removed in the dispose() call following the USING statement
            using (var heapObject = DeReference(offset) as Object)
            {
                heapObject.Release();   // here we release corresponding to the free operation
            }
        }
    }
}
