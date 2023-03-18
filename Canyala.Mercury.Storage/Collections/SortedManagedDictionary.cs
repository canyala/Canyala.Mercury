using System;
namespace Canyala.Mercury.Storage.Collections;

/// <summary>
/// Provides a managed generic dictionary.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
public class SortedManagedDictionary<TKey, TValue> : SortedDictionary<TKey, TValue>, IOrderedCollection<TKey, KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
	public SortedManagedDictionary()
	{
	}

    TKey IKeyCollection<TKey>.Min => throw new NotImplementedException();

    TKey IKeyCollection<TKey>.Max => throw new NotImplementedException();

    long IKeyCollection<TKey>.Magnitude => throw new NotImplementedException();

    IEnumerable<TKey> IKeyCollection<TKey>.Between(TKey low, TKey high)
    {
        throw new NotImplementedException();
    }

    bool IKeyCollection<TKey>.Contains(TKey element)
    {
        throw new NotImplementedException();
    }

    IEnumerable<KeyValuePair<TKey, TValue>> IOrderedCollection<TKey, KeyValuePair<TKey, TValue>>.Enumerate(TKey startAt, bool ascending, bool inclusive)
    {
        throw new NotImplementedException();
    }

    IEnumerable<KeyValuePair<TKey, TValue>> IOrderedCollection<TKey, KeyValuePair<TKey, TValue>>.Enumerate(TKey from, TKey to, bool ascending, bool inclusive)
    {
        throw new NotImplementedException();
    }

    IEnumerable<TKey> IKeyCollection<TKey>.Enumerate()
    {
        throw new NotImplementedException();
    }

    TKey IOrderedCollection<TKey, KeyValuePair<TKey, TValue>>.KeyOf(KeyValuePair<TKey, TValue> element)
    {
        throw new NotImplementedException();
    }

    bool IOrderedCollection<TKey, KeyValuePair<TKey, TValue>>.TryGet(TKey key, out KeyValuePair<TKey, TValue> value)
    {
        throw new NotImplementedException();
    }
}

