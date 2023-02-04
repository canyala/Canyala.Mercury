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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using Canyala.Lagoon.Functional;

using Canyala.Mercury.Storage.Allocators;
using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Storage.Collections;

/// <summary>
/// Provides a persisted generic set.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public class SortedHeapSet<T> : Storage.Object, ISet<T>, IOrderedCollection<T,T>
{
    private readonly Allocator<T> _objects;
    private readonly AATree _tree;

    /// <summary>
    /// Creates an anonymous persisted set.
    /// </summary>
    public SortedHeapSet(Environment environment)
    { 
        _objects =  environment.Allocator<T>();
        _tree = new AATree(environment.Heap<SortedHeapSet<T>>(), Compare);
    }

    /// <summary>
    /// Creates or recreates a named persisted set.
    /// </summary>
    /// <param name="name">The name of the set.</param>
    public SortedHeapSet(Environment environment, string name)
    { 
        _objects = environment.Allocator<T>();
        _tree = new AATree(environment.Heap<SortedHeapSet<T>>(), name, Compare);
    }

    /// <summary>
    /// Recreates a persisted set.
    /// </summary>
    /// <param name="offset">The offset of the set.</param>
    public SortedHeapSet(Environment environment, long offset)
    {
        _objects = environment.Allocator<T>();
        _tree = new AATree(environment.Heap<SortedHeapSet<T>>(), offset, Compare);
    }

    private long Compare(long a, long b)
    {
        if (_objects[a] is IComparable<T> comparable)
            return comparable.CompareTo(_objects[b]);

        throw new InvalidCastException($"{typeof(T).Name} does not implement {typeof(IComparable<T>).Name}");
    }

    /// <summary>
    /// The heap offset of the set.
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

    /// <summary>
    /// Gets a value representing the minimum value of the sorted set.
    /// </summary>
    public T Min
    {
        get
        {
            var nodeData = _tree.Min();

            if (nodeData == null)
                throw new InvalidOperationException("Set is empty.");

            return _objects[nodeData[0]];
        }
    }

    /// <summary>
    /// Gets a value representiung the maximum value of the sorted set.
    /// </summary>
    public T Max
    {
        get
        {
            var nodeData = _tree.Max();

            if (nodeData == null)
                throw new InvalidOperationException("Set is empty.");

            return _objects[nodeData[0]];
        }
    }

    /// <summary>
    /// Enumerate the set from a specific item in a specific order
    /// </summary>
    /// <param name="value">The item.</param>
    /// <param name="ascending">The order.</param>
    /// <returns>A sequence of items.</returns>
    public IEnumerable<T> Enumerate(T value, bool ascending, bool inclusive)
    {
        if (value is IComparable<T> comparableItem)
            foreach (var nodeData in _tree.Enumerate(data => comparableItem.CompareTo(_objects[data]), ascending, inclusive))
                yield return _objects[nodeData[0]];

        throw new InvalidCastException($"{typeof(T).Name} does not implement {nameof(IComparable)}");
    }

    /// <summary>
    /// Enumerate the set betwwen two items in a specific order
    /// </summary>
    /// <param name="lowValue"></param>
    /// <param name="highValue"></param>
    /// <param name="acsending"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    public IEnumerable<T> Enumerate(T lowValue, T highValue, bool ascending, bool inclusive)
    {
        if (!(lowValue is IComparable lowComparable))
            throw new InvalidCastException($"{typeof(T).Name} does not implement {nameof(IComparable)}");

        if (!(highValue is IComparable<T> highComparable))
            throw new InvalidCastException($"{typeof(T).Name} does not implement {nameof(IComparable)}");

        Func<long,long> lowComparer = data => lowComparable.CompareTo(_objects[data]);
        Func<long,long> highComparer = data => highComparable.CompareTo(_objects[data]);

        foreach (var nodeData in _tree.Enumerate(lowComparer, highComparer, ascending, inclusive))
            yield return _objects[nodeData[0]];
    }

    public IEnumerable<T> Between(T lowValue, T highValue)
    {
        return Enumerate(lowValue, highValue, true, false);
    }
    
    /// <summary>
    /// A convenience addition to the operation of sets.
    /// </summary>
    /// <param name="item">The item</param>
    /// <returns>true if item is a member of the set otherwise false.</returns>
    public bool this[T item]
    {
        get
        {
            return Contains(item);
        }

        set 
        {
            if (value) Add(item);
            else Remove(item);
        }
    }

    /// <summary>
    /// Adds an element to the current set and returns a value to indicate if the element was successfully added. 
    /// </summary>
    /// <param name="item">The element to add to the set.</param>
    /// <returns>true if the element is added to the set; false if the element is already in the set.</returns>
    public bool Add(T item)
    { 
        bool allocated = false;

        Action<long[]> make = offsets => 
        { 
            if (offsets[0] == 0L)
            {
                offsets[0] = _objects.Alloc(item); 
                allocated = true; 
            }
        };

        if (item is IComparable comparable)
            _tree.Insert(data => comparable.CompareTo(_objects[data]), make);
        else
            throw new InvalidCastException($"{typeof(T).Name} does not implement {nameof(IComparable)}");

        return allocated;
    }

    /// <summary>
    /// Removes all elements in the specified collection from the current set.
    /// </summary>
    /// <param name="other">The collection of items to remove from the set.</param>
    public void ExceptWith(IEnumerable<T> other)
        { foreach (var item in other) Remove(item); }

    /// <summary>
    /// Modifies the current set so that it contains only elements that are also in a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    public void IntersectWith(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);
        _tree.Clear(Free, data => otherSet.Contains(_objects[data]));
    }

    /// <summary>
    /// Determines whether the current set is a property (strict) subset of a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns>true if the current set is a correct subset of other; otherwise, false.</returns>
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);

        if (Count >= otherSet.Count)
            return false;

        if (Count == 0 && otherSet.Count > 0)
            return true;

        foreach (var item in this)
            if (!otherSet.Contains(item))
                return false;

        return true;
    }

    /// <summary>
    /// Determines whether the current set is a correct superset of a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns>true if the current set is a correct superset of other; otherwise, false.</returns>
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);

        if (Count <= otherSet.Count)
            return false;

        if (Count == 0 && otherSet.Count > 0)
            return true;

        foreach (var item in this)
            if (!otherSet.Contains(item))
                return true;

        return false;
    }

    /// <summary>
    /// Determines whether a set is a subset of a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns>true if the current set is a subset of other; otherwise, false.</returns>
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);

        if (Count == 0)
            return true;

        if (Count > otherSet.Count)
            return false;

        foreach (var item in this)
            if (!otherSet.Contains(item))
                return false;

        return true;
    }

    /// <summary>
    /// Determines whether the current set is a superset of a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns>true if the current set is a superset of other; otherwise, false.</returns>
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);

        if (Count < otherSet.Count)
            return false;

        foreach (var item in otherSet)
            if (!this.Contains(item))
                return false;

        return true;
    }

    /// <summary>
    /// Determines whether the current set overlaps with the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns>true if the current set and other share at least one common element; otherwise, false.</returns>
    public bool Overlaps(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);

        if (Count < otherSet.Count)
        {
            foreach (var item in this)
                if (otherSet.Contains(item))
                    return true;
        }
        else
        {
            foreach (var item in otherSet)
                if (this.Contains(item))
                    return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the current set and the specified collection contain the same elements.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns>true if the current set is equal to other; otherwise, false.</returns>
    public bool SetEquals(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);

        if (Count != otherSet.Count)
            return false;

        if (Count == 0)
            return true;

        foreach (var item in this)
            if (!otherSet.Contains(item))
                return false;

        return true;
    }

    /// <summary>
    /// Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);

        foreach (var item in otherSet)
        {
            if (Contains(item))
                Remove(item);
            else
                Add(item);
        }
    }

    /// <summary>
    /// Modifies the current set so that it contains all elements that are present in both the current set and in the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    public void UnionWith(IEnumerable<T> other)
    {
        foreach (var item in other)
            Add(item);
    }

    void ICollection<T>.Add(T item)
        { Add(item);  }

    /// <summary>
    /// Removes all items in the set.
    /// </summary>
    public void Clear()
        { _tree.Clear(Free); }

    private void Free(long[] data)
        { _objects.Free(data[0]); }

    /// <summary>
    /// Test if an item is a member of the set.
    /// </summary>
    /// <param name="item">The item to test.</param>
    /// <returns>true if item is a member, otherwize false.</returns>
    public bool Contains(T item)
    { 
        if (item is IComparable comparable)
            return _tree.Search(data => comparable.CompareTo(_objects[data])) != null;

        throw new InvalidCastException($"{typeof(T).Name} does not implement {nameof(IComparable)}");
    }

    /// <summary>
    /// Copies the set into an array.
    /// </summary>
    /// <param name="array">The array to copy to.</param>
    /// <param name="arrayIndex">Start index in the array.</param>
    public void CopyTo(T[] array, int arrayIndex)
        { foreach(var item in this) array[arrayIndex++] = item; }

    /// <summary>
    /// The numnber of items in the set.
    /// </summary>
    public int Count
        { get { return (int) Math.Min((long) Int32.MaxValue, _tree.Count()); } }

    /// <summary>
    /// true or false depending on if the set is read only or not.
    /// Relies on if the underlying storage is read only.
    /// </summary>
    public bool IsReadOnly
        { get { return _tree.IsReadOnly; } }

    /// <summary>
    /// Remove an item from the set.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>true if the item was in the set, otherwize false.</returns>
    public bool Remove(T item)
    { 
        bool invoked = false;
        Action<long[]> free = data => { invoked = true; _objects.Free(data[0]); };

        if (item is IComparable comparable)
            _tree.Remove(compareTo: data => comparable.CompareTo(_objects[data]), free);
        else
            throw new InvalidCastException($"{typeof(T).Name} does not implement {nameof(IComparable)}");

        return invoked;
    }

    /// <summary>
    /// HeapSet is enumerable.
    /// </summary>
    /// <returns>A set enumerator</returns>
    public IEnumerator<T> GetEnumerator()
        { foreach (var data in _tree.Enumerate()) yield return _objects[data[0]]; }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

    public bool TryGet(T key, out T value)
    {
        return (Contains(value = key)); // Assignment intended.
    }

    public T KeyOf(T element)
    {
        return element;
    }

    public long Magnitude
    {
        get { return this.Count; }
    }

    public IEnumerable<T> Enumerate()
    {
        return this;
    }
}
