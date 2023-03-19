using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace Canyala.Mercury.Storage.Collections;

/// <summary>
/// Provides a persisted generic set.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public class SortedManagedSet<T> : SortedSet<T>, IOrderedCollection<T, T>, IDisposable
    where T : notnull, IComparable, IComparable<T>
{
    private bool disposedValue;

    public SortedManagedSet()
	{
	}

    public bool this[T item] => Contains(item);

    public long Magnitude => throw new NotImplementedException();

    public IEnumerable<T> Between(T low, T high)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<T> Enumerate(T startAt, bool ascending, bool inclusive)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<T> Enumerate(T from, T to, bool ascending, bool inclusive)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<T> Enumerate()
    {
        throw new NotImplementedException();
    }

    public T KeyOf(T element)
    {
        throw new NotImplementedException();
    }

    public bool TryGet(T key, out T value)
    {
        throw new NotImplementedException();
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!disposedValue)
        {
            if(disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~SortedManagedSet()
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

