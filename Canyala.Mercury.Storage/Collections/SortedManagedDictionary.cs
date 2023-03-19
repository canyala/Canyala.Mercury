using System;
namespace Canyala.Mercury.Storage.Collections;

/// <summary>
/// Provides a managed generic dictionary.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
public class SortedManagedDictionary<TKey, TValue> : SortedDictionary<TKey, TValue>, IOrderedCollection<TKey, KeyValuePair<TKey, TValue>>, IDisposable
    where TKey : notnull
{
    private bool _disposedValue;
    private readonly string _name;

    public SortedManagedDictionary()
	{
        _name = string.Empty;
	}

    public SortedManagedDictionary(string name)
    {
        _name = name;
    }

    public TKey? Min => Keys.Min();

    public TKey? Max => Keys.Max();

    public long Magnitude => throw new NotImplementedException();

    public IEnumerable<TKey> Between(TKey low, TKey high)
    {
        throw new NotImplementedException();
    }

    public bool Contains(TKey element) => ContainsKey(element);

    public IEnumerable<KeyValuePair<TKey, TValue>> Enumerate(TKey startAt, bool ascending, bool inclusive)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> Enumerate(TKey from, TKey to, bool ascending, bool inclusive)
    {
        var enumerator = GetEnumerator();

        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    public IEnumerable<TKey> Enumerate()
    {
        return Keys;
    }

    public TKey KeyOf(KeyValuePair<TKey, TValue> element)
    {
        return element.Key;
    }

    public bool TryGet(TKey key, out KeyValuePair<TKey, TValue> value)
    {
        throw new NotImplementedException();
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!_disposedValue)
        {
            if(disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~SortedManagedDictionary()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

