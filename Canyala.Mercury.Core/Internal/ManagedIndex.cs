/*

  MIT License
 
  Copyright (c) 2011-2023 Canyala Innovation (Martin Fredriksson)

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
using System.Linq;
using System.Text;
using System.Threading;

using Canyala.Lagoon.Core.Collections;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury;
using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Collections;
using Canyala.Mercury.Storage.Extensions;

using Canyala.Mercury.Core.Extensions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("Canyala.Mercury.Test")]

namespace Canyala.Mercury.Core.Internal;

/// <summary>
/// Provides an index implementation using managed dictionaries and sets.
/// </summary>
internal sealed class ManagedIndex : Graph.Index, IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    private readonly SortedManagedDictionary<string, SortedManagedDictionary<string, SortedManagedSet<string>>> _primaries;
    private readonly string _name;

    public ManagedIndex(string name)
    {
        _primaries = new SortedManagedDictionary<string, SortedManagedDictionary<string, SortedManagedSet<string>>>();
        _name = name;
    }

    public void Add(string primary, string secondary, string ternary)
    {
        SortedManagedDictionary<string, SortedManagedSet<string>>? secondaryTernaries = null;
        SortedManagedSet<string>? ternaries = null;

        try
        {
            _lock.EnterWriteLock();

            if (!_primaries.TryGetValue(primary, out secondaryTernaries))
            {
                secondaryTernaries = new SortedManagedDictionary<string, SortedManagedSet<string>>();
                _primaries.Add(primary, secondaryTernaries);
            }

            if (!secondaryTernaries.TryGetValue(secondary, out ternaries))
            {
                ternaries = new SortedManagedSet<string>();
                secondaryTernaries.Add(secondary, ternaries);
            }

            ternaries.Add(ternary);
        }
        finally
        {
            secondaryTernaries = null;
            ternaries = null;

            _lock.ExitWriteLock();
        }
    }

    public void Remove(string? primary, string? secondary, string? ternary)
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
                if (_primaries.TryGetValue(primaryResult, out var secondaryTernaries))
                {
                    var secondaries = String
                        .IsNullOrEmpty(secondary) ? 
                        secondaryTernaries.Keys.AsEnumerable() : 
                        Seq.Of(secondary);

                    foreach (var secondaryResult in secondaries)
                    {
                        if (secondaryTernaries.TryGetValue(secondaryResult, out var ternaries))
                        {
                            if (!String.IsNullOrEmpty(ternary))
                                ternaries.Remove(ternary);
                            else
                                ternaries.Clear();

                            ternaries = null;
                        }
                    }

                    secondaryTernaries = null;
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
        SortedManagedDictionary<string, SortedManagedSet<string>>? secondaryTernary = null;

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
                secondaryTernary = null;

            _lock.ExitReadLock();
        }
    }

    public bool Contains(string primary, string secondary)
    {
        SortedManagedDictionary<string, SortedManagedSet<string>>? secondaryTernary = null;
        SortedManagedSet<string>? ternaries = null;

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
                secondaryTernary = null;

            if (ternaries != null)
                ternaries = null;

            _lock.ExitReadLock();
        }
    }

    public bool Contains(string primary, string secondary, string ternary)
    {
        SortedManagedDictionary<string, SortedManagedSet<string>>? secondaryTernary = null;
        SortedManagedSet<string>? ternaries = null;

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
                secondaryTernary = null;

            if (ternaries != null)
                ternaries = null;

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
        if (_primaries.TryGetValue(primary, out var secondaries))
        {
            if (secondaries.TryGetValue(secondary, out var ternaries))
            {
                return Seq.Array(new ConstrainedView(ternaries, ternary));
            }

            secondaries = null;
        }

        return Seq.Array<IView>();
    }

    public IView[] Views(Constraint.Specific primary, Constraint secondary, Constraint ternary)
    {
        if (_primaries.TryGetValue(primary, out var secondaries))
        {
            return Seq.Array<IView>(new ConstrainedView(secondaries, secondary), new UnionView(secondaries.Values.Select(value => new ConstrainedView(value, ternary)), ternary));
        }

        return Seq.Array<IView>();
    }

    public IView View(Constraint constraint)
        { return new ConstrainedView(_primaries, constraint); }

    public void Dispose()
        { _primaries.Clear(); }

    private IEnumerable<string[]> InternalEnumerate(Constraint.Specific primary, Constraint.Specific secondary, Constraint ternary)
    {
        if (_primaries.TryGetValue(primary, out var secondaries))
        {
            if (secondaries.TryGetValue(secondary, out var ternaries))
            {
                foreach (var ternaryMatch in ternaries.ConstrainBy(ternary))
                    yield return Seq.Array(ternaryMatch);

                ternaries = null;
            }

            secondaries = null;
        }
    }

    private IEnumerable<string[]> InternalEnumerate(Constraint.Specific primary, Constraint secondary, Constraint ternary)
    {
        if (_primaries.TryGetValue(primary, out var secondaries))
        {
            foreach (var secondaryMatch in secondaries.ConstrainBy(secondary))
            {
                var ternaries = secondaryMatch.Value;
                foreach (var ternaryMatch in ternaries.ConstrainBy(ternary))
                    yield return Seq.Array(secondaryMatch.Key, ternaryMatch);

                ternaries = null;
            }

            secondaries = null;
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

                ternaries = null;
            }

            secondaries = null;
        }
    }
}
