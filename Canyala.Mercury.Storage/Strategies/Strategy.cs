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
using System.Threading.Tasks;

using Canyala.Lagoon.Extensions;

using Canyala.Mercury.Storage.Strategies.Internal;

namespace Canyala.Mercury.Storage
{
    /// <summary>
    /// Provides a storage strategy factory for common scenarios.
    /// </summary>
    public abstract class Strategy
    {
        public Func<Type, Heap> HeapFactory { get; protected set; }

        public abstract void Remove();

        /// <summary>
        /// Creates a factory method closure that creates multiple in memory heaps.
        /// </summary>
        /// <param name="heapSize">The maximum size of the heaps created.</param>
        /// <returns>A heap factory method.</returns>
        public static Strategy PolystoreInMemory(int heapSize)
            { return new PolystoreInMemory(heapSize); }

        /// <summary>
        /// Creates a factory method closure that creates a single in memory heap.
        /// </summary>
        /// <param name="heapSize">The maximum size of the heap.</param>
        /// <returns>A heap factory method.</returns>
        public static Strategy SinglestoreInMemory(int heapSize)
            { return new SinglestoreInMemory(heapSize); }

        /// <summary>
        /// Creates a factory method closure that creates a single file based heap.
        /// </summary>
        /// <param name="heapSize">The maximum size of the heap.</param>
        /// <param name="filePath">The path to the single file based heap.</param>
        /// <returns>A heap factory method.</returns>
        public static Strategy SinglestoreInFile(int heapSize, string filePath)
            { return new SinglestoreInFile(heapSize, filePath); }
    }
}
