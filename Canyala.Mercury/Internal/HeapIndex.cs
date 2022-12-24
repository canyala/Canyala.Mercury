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

using Canyala.Lagoon.Collections;
using Canyala.Lagoon.Functional;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Collections;
using Canyala.Mercury.Storage.Extensions;

using Canyala.Mercury.Extensions;

namespace Canyala.Mercury.Internal
{
    /// <summary>
    /// Provides an index implementation using environment heap based dictionaries and sets.
    /// </summary>
    internal sealed class HeapIndex : Graph.Index, IDisposable
    {
        private readonly Storage.Environment _environment;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly SortedHeapDictionary<string, SortedHeapDictionary<string, SortedHeapSet<string>>> _primaries;

        public HeapIndex(Storage.Environment environment)
        {
            _primaries = new SortedHeapDictionary<string, SortedHeapDictionary<string, SortedHeapSet<string>>>(_environment = environment);
        }

        public HeapIndex(Storage.Environment environment, string name)
        { 
            _primaries = new SortedHeapDictionary<string,SortedHeapDictionary<string,SortedHeapSet<string>>>(_environment = environment, name);
        }

        public void Add(string primary, string secondary, string ternary)
        {
            SortedHeapDictionary<string, SortedHeapSet<string>> secondaryTernaries = null;
            SortedHeapSet<string> ternaries = null;

            try
            {
                _lock.EnterWriteLock();

                if (!_primaries.TryGetValue(primary, out secondaryTernaries))
                {
                    secondaryTernaries = new SortedHeapDictionary<string, SortedHeapSet<string>>(_environment);
                    _primaries.Add(primary, secondaryTernaries);
                }

                if (!secondaryTernaries.TryGetValue(secondary, out ternaries))
                {
                    ternaries = new SortedHeapSet<string>(_environment);
                    secondaryTernaries.Add(secondary, ternaries);
                }

                ternaries.Add(ternary);
            }
            finally
            {
                secondaryTernaries.Dispose();
                ternaries.Dispose();

                _lock.ExitWriteLock();
            }
        }

        public void Remove(string primary, string secondary, string ternary)
        {
            _lock.EnterWriteLock();

            try
            {
                var primaries = String
                    .IsNullOrEmpty(primary) ? 
                    _primaries.Keys.AsEnumerable() : 
                    Seq.Of(primary);

                foreach (var primaryResult in primaries)
                {
                    SortedHeapDictionary<string, SortedHeapSet<string>> secondaryTernaries;

                    if (_primaries.TryGetValue(primaryResult, out secondaryTernaries))
                    {
                        var secondaries = String
                            .IsNullOrEmpty(secondary) ? 
                            secondaryTernaries.Keys.AsEnumerable() : 
                            Seq.Of(secondary);

                        foreach (var secondaryResult in secondaries)
                        {
                            SortedHeapSet<string> ternaries;

                            if (secondaryTernaries.TryGetValue(secondaryResult, out ternaries))
                            {
                                if (!String.IsNullOrEmpty(ternary))
                                    ternaries.Remove(ternary);
                                else
                                    ternaries.Clear();

                                ternaries.Dispose();
                            }
                        }

                        secondaryTernaries.Dispose();
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Contains(string primary)
        {
            SortedHeapDictionary<string, SortedHeapSet<string>> secondaryTernary = null;

            _lock.EnterReadLock();

            try
            {
                if (_primaries.TryGetValue(primary, out secondaryTernary))
                     return secondaryTernary.Count > 0;

                return false;
            }
            finally
            {
                if (secondaryTernary != null)
                    secondaryTernary.Dispose();

                _lock.ExitReadLock();
            }
        }

        public bool Contains(string primary, string secondary)
        {
            SortedHeapDictionary<string, SortedHeapSet<string>> secondaryTernary = null;
            SortedHeapSet<string> ternaries = null;

            _lock.EnterReadLock();

            try
            {
                if (_primaries.TryGetValue(primary, out secondaryTernary))
                {
                    if (secondaryTernary.TryGetValue(secondary, out ternaries))
                        return ternaries.Count > 0;
                }

                return false;
            }
            finally
            {
                if (secondaryTernary != null)
                    secondaryTernary.Dispose();

                if (ternaries != null)
                    ternaries.Dispose();

                _lock.ExitReadLock();
            }
        }

        public bool Contains(string primary, string secondary, string ternary)
        {
            SortedHeapDictionary<string, SortedHeapSet<string>> secondaryTernary = null;
            SortedHeapSet<string> ternaries = null;

            _lock.EnterReadLock();

            try
            {
                if (_primaries.TryGetValue(primary, out secondaryTernary))
                {
                    if (secondaryTernary.TryGetValue(secondary, out ternaries))
                        if (ternaries.Contains(ternary))
                            return true;
                }

                return false;
            }
            finally
            {
                if (secondaryTernary != null)
                    secondaryTernary.Dispose();

                if (ternaries != null)
                    ternaries.Dispose();

                _lock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _primaries.Clear();
        }

        public IEnumerable<string[]> Enumerate(Constraint.Specific primary, Constraint.Specific secondary, Constraint ternary)
            { return InternalEnumerate(primary, secondary, ternary).AsReadLocked(_lock, this); }

        public IEnumerable<string[]> Enumerate(Constraint.Specific primary, Constraint secondary, Constraint ternary)
            { return InternalEnumerate(primary, secondary, ternary).AsReadLocked(_lock, this); }

        public IEnumerable<string[]> Enumerate(Constraint primary, Constraint secondary, Constraint ternary)
            { return InternalEnumerate(primary, secondary, ternary).AsReadLocked(_lock, this); }

        public IView[] Views(Constraint.Specific primary, Constraint.Specific secondary, Constraint ternary)
        {
            SortedHeapDictionary<string, SortedHeapSet<string>> secondaries;

            if (_primaries.TryGetValue(primary, out secondaries))
            {
                SortedHeapSet<string> ternaries;
                if (secondaries.TryGetValue(secondary, out ternaries))
                {
                    return Seq.Array(new ConstrainedView(ternaries, ternary));
                }

                secondaries.Dispose();
            }

            return Seq.Array<IView>();
        }

        public IView[] Views(Constraint.Specific primary, Constraint secondary, Constraint ternary)
        {
            SortedHeapDictionary<string, SortedHeapSet<string>> secondaries;

            if (_primaries.TryGetValue(primary, out secondaries))
            {
                return Seq.Array<IView>(new ConstrainedView(secondaries, secondary), new UnionView(secondaries.Values.Select(value => new ConstrainedView(value, ternary)), ternary));
            }

            return Seq.Array<IView>();
        }

        public IView View(Constraint constraint)
            { return new ConstrainedView(_primaries, constraint); }

        public void Dispose()
            { _primaries.Dispose(); }

        private IEnumerable<string[]> InternalEnumerate(Constraint.Specific primary, Constraint.Specific secondary, Constraint ternary)
        {
            SortedHeapDictionary<string, SortedHeapSet<string>> secondaries;

            if (_primaries.TryGetValue(primary, out secondaries))
            {
                SortedHeapSet<string> ternaries;
                if (secondaries.TryGetValue(secondary, out ternaries))
                {
                    foreach (var ternaryMatch in ternaries.ConstrainBy(ternary))
                        yield return Seq.Array(ternaryMatch);

                    ternaries.Dispose();
                }

                secondaries.Dispose();
            }
        }

        private IEnumerable<string[]> InternalEnumerate(Constraint.Specific primary, Constraint secondary, Constraint ternary)
        {
            SortedHeapDictionary<string, SortedHeapSet<string>> secondaries;

            if (_primaries.TryGetValue(primary, out secondaries))
            {
                foreach (var secondaryMatch in secondaries.ConstrainBy(secondary))
                {
                    var ternaries = secondaryMatch.Value;
                    foreach (var ternaryMatch in ternaries.ConstrainBy(ternary))
                        yield return Seq.Array(secondaryMatch.Key, ternaryMatch);

                    ternaries.Dispose();
                }

                secondaries.Dispose();
            }
        }

        private IEnumerable<string[]> InternalEnumerate(Constraint primary, Constraint secondary, Constraint ternary)
        {
            foreach (var primaryMatch in _primaries.ConstrainBy(primary))
            {
                var secondaries = primaryMatch.Value;
                foreach (var secondaryMatch in secondaries.ConstrainBy(secondary))
                {
                    var ternaries = secondaryMatch.Value;
                    foreach (var ternaryMatch in ternaries.ConstrainBy(ternary))
                        yield return Seq.Array(primaryMatch.Key, secondaryMatch.Key, ternaryMatch);

                    ternaries.Dispose();
                }

                secondaries.Dispose();
            }
        }
    }
}
