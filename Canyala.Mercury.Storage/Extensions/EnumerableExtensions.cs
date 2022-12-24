//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Canyala.Lagoon.Extensions;

namespace Canyala.Mercury.Storage.Extensions
{
    /// <summary>
    /// Provides threading extensions for enumerables.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Implements an enumerator wrapper for locking.
        /// </summary>
        /// <typeparam name="T">Enumerated type.</typeparam>
        private class ReaderWriterLockSlimReadLockEnumerator<T> : IEnumerator<T>, IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;
            private readonly IEnumerator<T> _enumerator;
            private readonly object _object;

            public ReaderWriterLockSlimReadLockEnumerator(ReaderWriterLockSlim @lock, IEnumerator<T> enumerator, object @object)
            {
                _lock = @lock;
                _enumerator = enumerator;
                _lock.EnterUpgradeableReadLock();
                _object = @object;
            }

            public void Dispose()
            {
                var disposable = _enumerator as IDisposable;
                if (disposable != null) disposable.Dispose();
                _lock.ExitUpgradeableReadLock();
                GC.SuppressFinalize(this);
            }

            public T Current
                { get { return _enumerator.Current; } }

            object System.Collections.IEnumerator.Current
                { get { return Current; } }

            public bool MoveNext()
                { return _enumerator.MoveNext(); }

            public void Reset()
                { _enumerator.Reset(); }

            ~ReaderWriterLockSlimReadLockEnumerator()
            { 
                throw new InvalidOperationException("ReaderWriterLockSlimReadLockEnumerator for type {0} must be explicitly dipsosed.".Args(_object.GetType().Name)); 
            }
        }

        /// <summary>
        /// Implements an enumerable wrapper for locking.
        /// </summary>
        /// <typeparam name="T">Enumerable trype.</typeparam>
        private class ReaderWriterLockSlimReadLockEnumerable<T> : IEnumerable<T>
        {
            private readonly ReaderWriterLockSlim _lock;
            private readonly IEnumerable<T> _enumerable;
            private readonly object _object;

            public ReaderWriterLockSlimReadLockEnumerable(ReaderWriterLockSlim @lock, IEnumerable<T> enumerable, object @object)
            {
                _lock = @lock;
                _enumerable = enumerable;
                _object = @object;
            }

            public IEnumerator<T> GetEnumerator()
                { return new ReaderWriterLockSlimReadLockEnumerator<T>(_lock, _enumerable.GetEnumerator(), _object); }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                { return GetEnumerator(); }
        }

        /// <summary>
        /// Wraps an IEnumerable of type 'T' with a read lock on the underlying IEnumerable of type 'T'/>
        /// </summary>
        /// <remarks>
        /// The read lock is managed by adhering to the construct/dispose pattern that is
        /// used by <code>foreach</code>. If <code>GetEnumerator()</code> is used, the enumerator
        /// must be disposed of in a controlled fashion or there will be an everlasting read lock
        /// that causes dead locks. You must not keep references of type <code>IEnumerable&lt;T&gt;'</code>
        /// enumerables that are wrapped by a read locking versions.
        /// </remarks>
        /// <typeparam name="T">The type of enumerated elements.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="lock">The lock.</param>
        /// <returns>A read locked enumerable.</returns>
        public static IEnumerable<T> AsReadLocked<T>(this IEnumerable<T> enumerable, ReaderWriterLockSlim @lock, object @object)
            { return new ReaderWriterLockSlimReadLockEnumerable<T>(@lock, enumerable, @object); }
    }
}
