//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using Canyala.Lagoon.Extensions;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Allocators;
using Canyala.Mercury.Storage.Strategies;

namespace Canyala.Mercury.Storage
{
    /// <summary>
    /// Provides an environment for heaps and allocators.
    /// </summary>
    /// <remarks>
    /// A mercury storage environment is a persisted memory implementation
    /// that provides an alternative to database storage.
    /// </remarks>
    public sealed class Environment : IDisposable
    {
        private readonly Dictionary<Type, Heap> _heaps;
        private readonly Dictionary<Type, Allocator> _allocators;
        private readonly Func<Type, Heap> _heapFactory;
        private readonly List<Object> _anonymousRoots;
        private readonly Strategy _strategy;

        /// <summary>
        /// Provides the collection of heaps managed by the environment.
        /// </summary>
        public IEnumerable<Heap> Heaps 
            { get { return _heaps.Values.Distinct(); } }

        /// <summary>
        /// Provides the collection of allocators managed by the environment.
        /// </summary>
        public IEnumerable<Allocator> Allocators 
            { get { return _allocators.Values; } }

        /// <summary>
        /// Provides the collection of named roots in the environment.
        /// </summary>
        public IEnumerable<string> Roots
            { get { return Heaps.SelectMany(heap => heap.Roots); } }

        /// <summary>
        /// Creates an environment.
        /// </summary>
        /// <param name="heapFactory">A factory method for heap creation. Defaults to a single, memory based heap.</param>
        private Environment(Strategy strategy = null)
        {
            _strategy = strategy ?? Strategy.SinglestoreInMemory(1048576);
            _heapFactory = _strategy.HeapFactory;
            _allocators = new Dictionary<Type, Allocator>();
            _heaps = new Dictionary<Type, Heap>();
            _anonymousRoots = new List<Object>();
        }

        public static Environment Create(Strategy strategy = null)
            { return new Environment(strategy); }

        /// <summary>
        /// Maps a specific type to a heap.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The heap for type.</returns>
        public Heap Heap(Type type)
        {
            Heap heap;
            if (_heaps.TryGetValue(type, out heap))
                return heap;

            heap = _heapFactory(type);
            _heaps.Add(type, heap);

            return heap;
        }

        /// <summary>
        /// Maps a generic type to a heap.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>The heap for type T.</returns>
        public Heap Heap<T>()
            { return Heap(typeof(T)); }

        /// <summary>
        /// Maps a generic type to an allocator.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The allocator for type T</returns>
        public Allocator<T> Allocator<T>()
        {
            var type = typeof(T);

            Allocator allocator;
            if (_allocators.TryGetValue(type, out allocator))
                return (Allocator<T>) allocator;

            if (type.IsPrimitive)
                allocator = new NullAllocator<T>();
            else if (typeof(Storage.Object).IsAssignableFrom(type))
                allocator = new ReferenceAllocator<T>(this);
            else if (type == typeof(string))
                allocator = new SingletonAllocator<T>(Heap<SingletonAllocator<T>>(), Heap<T>());
            else
                allocator = new ValueAllocator<T>(Heap<T>());

            _allocators.Add(type, allocator);

            return (Allocator<T>) allocator;
        }

        public Strategy Strategy { get { return _strategy; } }

        /// <summary>
        /// Counts the number of used blocks in all heaps.
        /// </summary>
        /// <returns>The total number of used blocks.</returns>
        public long CountUsedBlocks()
            { return Heaps.Sum(heap => heap.CountUsedBlocks()); }

        /// <summary>
        /// Counts the number of free blocks in all heaps.
        /// </summary>
        /// <returns>The total number of used blocks.</returns>
        public long CountFreeBlocks()
            { return Heaps.Sum(heap => heap.CountFreeBlocks()); }

        internal void AddAnonymousRoot(Object obj)
            { _anonymousRoots.Add(obj); }

        /// <summary>
        /// Garbage collects all heaps.
        /// </summary>
        public void GarbageCollect()
            { Heaps.Do(heap => heap.GarbageCollect()); }

        public void Dispose()
        {
            Heaps.Do(heap => heap.Stream.Dispose());
        }
    }
}
