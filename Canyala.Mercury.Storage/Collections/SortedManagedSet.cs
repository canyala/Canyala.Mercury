using System;
using System.Collections.Generic;

namespace Canyala.Mercury.Storage.Collections;

/// <summary>
/// Provides a persisted generic set.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public class SortedManagedSet<T> : SortedSet<T>, IOrderedCollection<T, T>
    where T : notnull, IComparable, IComparable<T>
{
	public SortedManagedSet()
	{
	}

    T IKeyCollection<T>.Min => throw new NotImplementedException();

    T IKeyCollection<T>.Max => throw new NotImplementedException();

    long IKeyCollection<T>.Magnitude => throw new NotImplementedException();

    IEnumerable<T> IKeyCollection<T>.Between(T low, T high)
    {
        throw new NotImplementedException();
    }

    bool IKeyCollection<T>.Contains(T element)
    {
        throw new NotImplementedException();
    }

    IEnumerable<T> IOrderedCollection<T, T>.Enumerate(T startAt, bool ascending, bool inclusive)
    {
        throw new NotImplementedException();
    }

    IEnumerable<T> IOrderedCollection<T, T>.Enumerate(T from, T to, bool ascending, bool inclusive)
    {
        throw new NotImplementedException();
    }

    IEnumerable<T> IKeyCollection<T>.Enumerate()
    {
        throw new NotImplementedException();
    }

    T IOrderedCollection<T, T>.KeyOf(T element)
    {
        throw new NotImplementedException();
    }

    bool IOrderedCollection<T, T>.TryGet(T key, out T value)
    {
        throw new NotImplementedException();
    }
}

