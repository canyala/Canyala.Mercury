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
        {
            _environment = environment;
        }

        /// <summary>
        /// Allocates and stores an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The offset of the item in the heap.</returns>
        public override long Alloc(T item)
        {
            if (item is Storage.Object storageObject)
            {
                storageObject.AddReference();
                return storageObject.Offset;
            }

            throw new InvalidCastException($"Type {typeof(T).Name} must be derived from {typeof(Storage.Object).Name}");
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

            if (constructor is null)
                throw new MissingMethodException($"{type.FullName} has no dereference constructor");

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
                heapObject?.Release();   // here we release corresponding to the free operation
            }
        }
    }
}
