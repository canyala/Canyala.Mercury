//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Canyala.Mercury.Storage
{
    /// <summary>
    /// Provides an interface for objects that are persisted in a heap.
    /// </summary>
    /// <remarks>
    /// Classes implementing 'Object' is full class citizens in the world
    /// of heaps. This means that they live in persistent storage and they 
    /// will be managed as reference types in the heap, not as value types. 
    /// A similar situation exists between value types and reference types 
    /// in the CLR managed heap; the persistent world
    /// of heaps takes the concept a bit further.
    /// </remarks>
    public abstract class Object : IDisposable
    {
        /// <summary>
        /// Provides the heap offset of an Object.
        /// </summary>
        internal abstract long Offset { get; }
        
        internal abstract void AddReference();
        internal abstract void Release();

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    Release();
                }
            }
            finally
            {
                disposed = true;
            }
        }

        ~Object()
        {
            Dispose(false);
        }
    }
}
