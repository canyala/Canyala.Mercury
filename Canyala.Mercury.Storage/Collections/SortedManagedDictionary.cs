using System;
namespace Canyala.Mercury.Storage.Collections;

/// <summary>
/// Provides a managed generic dictionary.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
public class SortedManagedDictionary<TKey, TValue> : SortedDictionary<TKey, TValue>, IOrderedCollection<TKey, KeyValuePair<TKey, TValue>>, IDisposable
    where TKey : notnull, IComparable<TKey>
{
    private bool _disposedValue;
    private readonly string _name;

    /// <summary>
    /// 
    /// </summary>
    public SortedManagedDictionary()
	{
        _name = string.Empty;
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public SortedManagedDictionary(string name)
    {
        _name = name;
    }

    /// <summary>
    /// 
    /// </summary>
    public TKey? Min => this.FirstOrDefault<KeyValuePair<TKey, TValue>>().Key;

    /// <summary>
    /// 
    /// </summary>
    public TKey? Max => this.LastOrDefault<KeyValuePair<TKey, TValue>>().Key;

    /// <summary>
    /// 
    /// </summary>
    public long Magnitude => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="low"></param>
    /// <param name="high"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TKey> Between(TKey low, TKey high)
    {
        var enumerator = GetEnumerator();
        int relativeOrder = 0;

        while (enumerator.MoveNext())
        {
            relativeOrder = enumerator.Current.Key.CompareTo(low);

            if (relativeOrder < 0)
                continue;

            break;
        }

        yield return enumerator.Current.Key;

        while (enumerator.MoveNext())
        {
            relativeOrder = enumerator.Current.Key.CompareTo(high);

            if (relativeOrder < 0)
            {
                yield return enumerator.Current.Key;
                continue;
            }

            break;
        }

        if (relativeOrder == 0)
            yield return enumerator.Current.Key;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public bool Contains(TKey element) => ContainsKey(element);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startAt"></param>
    /// <param name="ascending"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<KeyValuePair<TKey, TValue>> Enumerate(TKey startAt, bool ascending, bool inclusive)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="ascending"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<TKey, TValue>> Enumerate(TKey from, TKey to, bool ascending, bool inclusive)
    {
        var enumerator = GetEnumerator();
        bool hasCurrent = false;
        int relativeOrder = 0;

        while (hasCurrent = enumerator.MoveNext())
        {
            relativeOrder = enumerator.Current.Key.CompareTo(from);

            if (relativeOrder < 0)
                continue;

            break;
        }

        if (ascending)
        {
            if (hasCurrent)
            { 
                if (relativeOrder == 0 && inclusive || relativeOrder > 0)
                    yield return enumerator.Current;

                while (hasCurrent = enumerator.MoveNext())
                {
                    relativeOrder = enumerator.Current.Key.CompareTo(to);

                    if (relativeOrder < 0)
                        yield return enumerator.Current;

                    break;
                }

                if (hasCurrent && relativeOrder == 0 && inclusive)
                    yield return enumerator.Current;
            }

            yield break;
        }

        Stack<KeyValuePair<TKey, TValue>> keyValuePairs = new();

        if (relativeOrder == 0 && inclusive || relativeOrder > 0)
            keyValuePairs.Push(enumerator.Current);

        while (hasCurrent = enumerator.MoveNext())
        {
            relativeOrder = enumerator.Current.Key.CompareTo(to);

            if (relativeOrder < 0)
            { 
                keyValuePairs.Push(enumerator.Current);
                continue;
            }

            break;
        }

        if (relativeOrder == 0 && inclusive)
            yield return enumerator.Current;

        KeyValuePair<TKey, TValue> keyValuePair;
        while (keyValuePairs.TryPop(out keyValuePair))
            yield return keyValuePair;

        yield break;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TKey> Enumerate()
    {
        return Keys;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public TKey KeyOf(KeyValuePair<TKey, TValue> element)
    {
        return element.Key;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="keyValuePair"></param>
    /// <returns></returns>
    public bool TryGet(TKey key, out KeyValuePair<TKey, TValue> keyValuePair)
    {
        TValue? value;
        if (this.TryGetValue(key, out value))
        {
            keyValuePair = new KeyValuePair<TKey, TValue>(key, value);
            return true;
        }

        keyValuePair = default;
        return false;
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

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

