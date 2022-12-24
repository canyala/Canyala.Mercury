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

using Canyala.Lagoon.Contracts;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;


using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Storage.Allocators
{
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
        public SingletonAllocator(Heap index, Heap objects=null)
        {
            _index = new AATree(index, GetType().ReadableName() + ".Index", Compare);
            _objects = objects ?? index;
        }

        private long Compare(long a, long b)
            { return (this[a] as IComparable<T>).CompareTo(this[b]); }

        /// <summary>
        /// Allocates and stores an item in a heap.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The offset of the item in the heap.</returns>
        public override long Alloc(T item)
        {
            var comparableItem = item as IComparable<T>;
            Contract.Requires(comparableItem != null, "SingletonAllocator of T requires that T is an IComparable of T.");

            Action<long[]> init = data => { if (data[0] == 0) data[0] = AllocSingleton(item);  };

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
            var comparableItem = this[offset] as IComparable<T>;
            _index.Remove(data => comparableItem.CompareTo(this[data]), offsets => _objects.Free(offsets[0]));
        }

        #endregion
    }
}
