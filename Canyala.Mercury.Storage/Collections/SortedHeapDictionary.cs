/*

  MIT License
 
  Copyright (c) 2022 Canyala Innovation (Martin Fredriksson)

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
using System.Runtime.Serialization;
using System.Text;

using Canyala.Lagoon.Functional;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Allocators;
using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Storage.Collections;

/// <summary>
/// Provides a persisted generic dictionary.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
public class SortedHeapDictionary<TKey, TValue> : Storage.Object, IDictionary<TKey, TValue>, IOrderedCollection<TKey, KeyValuePair<TKey,TValue>>
{
    // Keys/Values yielding accessor implementation
    private class ReadOnlyCollection<T> : ICollection<T>
    {
        private readonly AATree _index;
        private readonly Allocator<T> _allocator;
        private readonly int _element;

        public ReadOnlyCollection(AATree index, Allocator<T> allocator, int element)
        {
            _index = index;
            _allocator = allocator;
            _element = element;
        }

        public void Add(T item)
            { throw new InvalidOperationException();  }

        public void Clear()
            { throw new InvalidOperationException(); }

        public bool Contains(T item)
        {
            if (_element == 0)
            {
                if (item is not IComparable comparable)
                    throw new InvalidCastException($"{typeof(T).Name} is not {nameof(IComparable)}.");

                return _index.Search(data => comparable.CompareTo(_allocator[data])) != null;
            }
            else if (item is null)
            {
                return false;
            }

            foreach (var data in _index.Enumerate())
                if (item.Equals(data)) 
                        return true;

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
            { foreach (var data in _index.Enumerate()) array[arrayIndex++] = _allocator[data[_element]]; }

        public int Count
            { get { return (int) Math.Min((long) Int32.MaxValue, _index.Count()); } }

        public bool IsReadOnly
            { get { return true; } }

        public bool Remove(T item)
            { throw new InvalidOperationException(); }

        public IEnumerator<T> GetEnumerator()
            { foreach (var data in _index.Enumerate()) yield return _allocator[data[_element]]; }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            { return GetEnumerator(); }
    }

    private readonly Allocator<TValue> _values;
    private readonly Allocator<TKey> _keys;
    private readonly AATree _tree;

    /// <summary>
    /// Creates an anonymous persisted dictionary.
    /// </summary>
    public SortedHeapDictionary(Environment environment)
    { 
        _keys = environment.Allocator<TKey>();
        _values = environment.Allocator<TValue>();
        _tree = new AATree(environment.Heap<SortedHeapDictionary<TKey,TValue>>(), Compare, 2);
    }

    /// <summary>
    /// Creates or recreates a named persisted dictionary.
    /// </summary>
    /// <param name="name">The name of the dictionary.</param>
    public SortedHeapDictionary(Environment environment, string name)
    {
        _keys = environment.Allocator<TKey>();
        _values = environment.Allocator<TValue>();
        _tree = new AATree(environment.Heap<SortedHeapDictionary<TKey, TValue>>(), name, Compare, 2);
    }

    /// <summary>
    /// Recreates a persisted dictionary.
    /// </summary>
    /// <param name="offset">The offset of the dictionary.</param>
    public SortedHeapDictionary(Environment environment, long offset)
    {
        _keys = environment.Allocator<TKey>();
        _values = environment.Allocator<TValue>();
        _tree = new AATree(environment.Heap<SortedHeapDictionary<TKey, TValue>>(), offset, Compare, 2);
    }

    private long Compare(long a, long b)
    {
        if (_keys[a] is IComparable<TKey> comparable)
            return comparable.CompareTo(_keys[b]);

        throw new InvalidCastException($"{typeof(TKey).Name} does not implement {typeof(IComparable<TKey>).Name}");
    }

    /// <summary>
    /// Gets a value representing the minimum key value of the sorted dictionary.
    /// </summary>
    public TKey Min
    {
        get
        {
            var nodeData = _tree.Min();

            if (nodeData == null)
                throw new InvalidOperationException("Set is empty.");

            return _keys[nodeData[0]];
        }
    }

    /// <summary>
    /// Gets a value representiung the maximum key value of the sorted dictionary.
    /// </summary>
    public TKey Max
    {
        get
        {
            var nodeData = _tree.Max();

            if (nodeData == null)
                throw new InvalidOperationException("Set is empty.");

            return _keys[nodeData[0]];
        }
    }

    /// <summary>
    /// Enumerate the keys of dictionary from a specific item in a specific order.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="ascending">The order.</param>
    /// <returns>A sequence of keys.</returns>
    public IEnumerable<TKey> EnumerateKeysFrom(TKey item, bool ascending, bool inclusive)
    {
        if (item is IComparable<TKey> comparableItem)
            foreach (var nodeData in _tree.Enumerate(data => comparableItem.CompareTo(_keys[data]), ascending, inclusive))
                yield return _keys[nodeData[0]];
    }

    /// <summary>
    /// Enumerate the key/value pairs of dictionary from a specific item in a specific order.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="ascending">The order.</param>
    /// <returns>A sequence of pairs.</returns>
    public IEnumerable<KeyValuePair<TKey,TValue>> Enumerate(TKey item, bool ascending, bool inclusive)
    {
        if (item is IComparable<TKey> comparableItem)
            foreach (var nodeData in _tree.Enumerate(data => comparableItem.CompareTo(_keys[data]), ascending, inclusive))
                yield return new KeyValuePair<TKey,TValue>(_keys[nodeData[0]], _values[nodeData[1]]);
    }

    /// <summary>
    /// Enumerate the keys of dictionary between two elements in a specific order.
    /// </summary>
    /// <param name="lowValue">The low key.</param>
    /// <param name="highValue">The high key.</param>
    /// <param name="ascending">Ascending or descending order.</param>
    /// <param name="inclusive">low and high are inclusive or not.</param>
    /// <returns></returns>
    public IEnumerable<TKey> EnumerateKeysBetween(TKey lowValue, TKey highValue, bool ascending, bool inclusive)
    {
        if (lowValue is IComparable<TKey> lowComparable && highValue is IComparable<TKey> highComparable)
        {
            Func<long, long> lowComparer = data => lowComparable.CompareTo(_keys[data]);
            Func<long, long> highComparer = data => highComparable.CompareTo(_keys[data]);

            foreach (var nodeData in _tree.Enumerate(lowComparer, highComparer, ascending, inclusive))
                yield return _keys[nodeData[0]];
        }
    }

    /// <summary>
    /// Enumerate the pairs of a dictionary between two elements in a specific order.
    /// </summary>
    /// <param name="lowValue">The low key.</param>
    /// <param name="highValue">The high key.</param>
    /// <param name="ascending">Ascending or descending order.</param>
    /// <param name="inclusive">low and high are inclusive or not.</param>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<TKey,TValue>> Enumerate(TKey lowValue, TKey highValue, bool ascending, bool inclusive)
    {
        if (lowValue is not IComparable lowComparable)
            throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}.");

        if (highValue is not IComparable highComparable)
            throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}.");
        
        Func<long, long> lowComparer = data => lowComparable.CompareTo(_keys[data]);
        Func<long, long> highComparer = data => highComparable.CompareTo(_keys[data]);

        foreach (var nodeData in _tree.Enumerate(lowComparer, highComparer, ascending, inclusive))
            yield return new KeyValuePair<TKey, TValue>(_keys[nodeData[0]], _values[nodeData[1]]);        
    }

    public IEnumerable<TKey> Between(TKey lowValue, TKey highValue)
    {
        return EnumerateKeysBetween(lowValue, highValue, true, false);
    }

    /// <summary>
    /// The heap offset of the dictionary.
    /// </summary>
    internal override long Offset
        { get { return _tree.Offset; } }

    internal override void AddReference()
        { _tree.IncreaseReferenceCount(); }

    internal override void Release()
    {
        if (_tree.DecreaseReferenceCount() > 0)
            return;

        _tree.Destroy(Free);
    }

    public void Add(TKey key, TValue value)
    {
        if (key is not IComparable comparable)
            throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}.");

        Action<long[]> init = offsets =>
        {
            if (offsets[0] != 0)
                throw new ArgumentException("An item with the same key has already been added.");

            offsets[0] = _keys.Alloc(key);
            offsets[1] = _values.Alloc(value);
        };

        _tree.Insert(data => comparable.CompareTo(_keys[data]), init);
    }
    
    public bool ContainsKey(TKey key)
    {
        if (key is not IComparable comparable)
            throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}.");

        return _tree.Search(data => comparable.CompareTo(_keys[data])) != null;
    }

    bool IKeyCollection<TKey>.Contains(TKey key)
        { return ContainsKey(key); }

    public ICollection<TKey> Keys
        { get { return new ReadOnlyCollection<TKey>(_tree, _keys, 0); } }

    public bool Remove(TKey key)
    {
        if (key is not IComparable comparable)
            throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}");

        bool invoked = false;

        Action<long[]> free = offsets => 
            { 
                invoked = true; 
                _keys.Free(offsets[0]); 
                _values.Free(offsets[1]); 
            };

        _tree.Remove(offset => comparable.CompareTo(_keys[offset]), free);

        return invoked;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (key is not IComparable comparable)
            throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}");

        var offsets = _tree.Search(offset => comparable.CompareTo(_keys[offset]));

        if (offsets == null)
        {
            value = default!;
            return false;
        }

        value = _values[offsets[1]];
        return true;
    }

    public bool TryGet(TKey key, out KeyValuePair<TKey,TValue> pair)
    {
        if (key is not IComparable comparable)
            throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}");

        var offsets = _tree.Search(offset => comparable.CompareTo(_keys[offset]));

        if (offsets == null)
        {
            pair = default(KeyValuePair<TKey,TValue>);
            return false;
        }

        pair = new KeyValuePair<TKey,TValue>(_keys[offsets[0]], _values[offsets[1]]);
        return true;
    }

    public TKey KeyOf(KeyValuePair<TKey,TValue> pair)
    {
        return pair.Key;
    }

    public ICollection<TValue> Values
        { get { return new ReadOnlyCollection<TValue>(_tree, _values, 1); } }

    public TValue this[TKey key]
    {
        get
        {
            if (key is not IComparable comparable)
                throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}");

            var offsets = _tree.Search(offset => comparable.CompareTo(_keys[offset]));
            if (offsets == null) throw new KeyNotFoundException();
            return _values[offsets[1]];
        }

        set
        {
            if (key is not IComparable comparable)
                throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}");

            Action<long[]> init = offsets => 
                {
                    if (offsets[0] == 0) offsets[0] = _keys.Alloc(key);
                    if (offsets[1] != 0) _values.Free(offsets[1]);                    
                    offsets[1] = _values.Alloc(value);
                };

            _tree.Insert(offset => comparable.CompareTo(_keys[offset]), init);
        }
    }

    public void Add(KeyValuePair<TKey, TValue> item)
        { Add(item.Key, item.Value); }

    public void Clear()
    { 
      
       _tree.Clear(Free, KeepNone); 
    }

    private void Free(long[] offsets)
    {
        _keys.Free(offsets[0]);
        _values.Free(offsets[1]);
    }

    private bool KeepNone(long offset)
        { return false; }
    
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (item.Key is not IComparable comparable)
            throw new InvalidCastException($"{typeof(TKey).Name} is not {nameof(IComparable)}");

        var offsets = _tree.Search(data => comparable.CompareTo(_keys[data]));

        if (offsets == null)
            return false;

        if (item.Value is null)
            return false;

        return item.Value.Equals(_values[offsets[1]]);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        var pair = GetEnumerator();

        while (pair.MoveNext())
            array[arrayIndex++] = pair.Current;
    }

    public int Count
        { get { return (int) Math.Min((long) Int32.MaxValue, _tree.Count()); } }

    public bool IsReadOnly
        { get { return _tree.IsReadOnly; } }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var data in _tree.Enumerate())
            yield return new KeyValuePair<TKey,TValue>(_keys[data[0]], _values[data[1]]);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

    public long Magnitude
        { get { return Count; } }

    public IEnumerable<TKey> Enumerate()
        { return Keys; }
}
