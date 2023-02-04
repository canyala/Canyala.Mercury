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
using System.Text;
using System.Threading.Tasks;

using Canyala.Mercury.Storage.Collections;

namespace Canyala.Mercury.Core;

/// <summary>
/// Provides a domain model for IOrderedCollection constraints.
/// </summary>
public abstract class Constraint
{
    /// <summary>
    /// Null object instance for an empty constraint.
    /// </summary>
    public static readonly Constraint Empty = new Null();

    /// <summary>
    /// Implements a null object for constraints.
    /// </summary>
    private sealed class Null : Constraint
    {
        /// <summary>
        /// Enumerates a collection according to the constraint.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="collection">The collection to enumerate.</param>
        /// <returns>A constrained enumeration of the collection.</returns>
        public override IEnumerable<T> Enumerate<T>(IOrderedCollection<string, T> collection)
            { return collection; }

        /// <summary>
        /// Polumorphic constraint matching. 
        /// </summary>
        /// <param name="element">An element to test.</param>
        /// <returns><code>true</code> or <code>false</code> depending on if the conatraint is fullfiled or not.</returns>
        public override bool Match(string element)
        {
            return true;
        }
    }
    
    /// <summary>
    /// Implements a constraint for an exact match.
    /// </summary>
    /// <remarks>
    /// Internal to grant the Graph implementation access to this type of constraint for optimization purposes.
    /// </remarks>
    internal sealed class Specific : Constraint
    {
        /// <summary>
        /// The value of a specific constraint.
        /// </summary>
        private readonly string _value;

        /// <summary>
        /// Creates a specific constraints.
        /// </summary>
        /// <param name="value">The value of the specific constraint.</param>
        internal Specific(string value)
            { _value = value; }

        /// <summary>
        /// Enumerates a collection according to the constraint.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="collection">The collection to enumerate.</param>
        /// <returns>A constrained enumeration of the collection.</returns>
        public override IEnumerable<T> Enumerate<T>(IOrderedCollection<string, T> collection)
        {
            if (collection.TryGet(_value, out T item)) 
                yield return item; 
        }

        /// <summary>
        /// Polumorphic constraint matching. 
        /// </summary>
        /// <param name="element">An element to test.</param>
        /// <returns><code>true</code> or <code>false</code> depending on if the conatraint is fullfiled or not.</returns>
        public override bool Match(string element)
        {
            return String.Compare(element, _value, StringComparison.InvariantCulture) == 0;
        }

        /// <summary>
        /// A specific constraint can be used as a <code>string</code>.
        /// </summary>
        /// <param name="specific">A specific constraint.</param>
        /// <returns>The value of the specific constraint.</returns>
        public static implicit operator string(Constraint.Specific specific)
            { return specific._value; }
    }

    /// <summary>
    /// Implements a constraint for a range.
    /// </summary>
    private sealed class Range : Constraint
    {
        /// <summary>
        /// The minimum value of the constraint.
        /// </summary>
        private readonly string _minimumValue;

        /// <summary>
        /// The maximum value of the constraint.
        /// </summary>
        private readonly string _maximumValue;

        /// <summary>
        /// Creates a range constraint.
        /// </summary>
        /// <param name="minimumValue">The minimum value of the constraint.</param>
        /// <param name="maximumValue">The maximum value of the constraint.</param>
        internal Range(string minimumValue, string maximumValue)
        {
            _minimumValue = minimumValue;
            _maximumValue = maximumValue;
        }

        /// <summary>
        /// Enumerates a collection according to the constraint.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="collection">The collection to enumerate.</param>
        /// <returns>A constrained enumeration of the collection.</returns>
        public override IEnumerable<T> Enumerate<T>(IOrderedCollection<string,T> collection)
            { return collection.Enumerate(_minimumValue, _maximumValue, true, false); }

        /// <summary>
        /// Polumorphic constraint matching. 
        /// </summary>
        /// <param name="element">An element to test.</param>
        /// <returns><code>true</code> or <code>false</code> depending on if the conatraint is fullfiled or not.</returns>
        public override bool Match(string element)
        {
            if (String.Compare(element, _minimumValue, StringComparison.InvariantCulture) < 0)
                return false;

            if (String.Compare(element, _maximumValue, StringComparison.InvariantCulture) > 0)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Implements an array constraint (assumed to be very small in length).
    /// </summary>
    private sealed class Array : Constraint
    {
        /// <summary>
        /// The sequence is stored as a distinct, sorted array.
        /// </summary>
        private string[] _array;

        /// <summary>
        /// Creates a small array constraint.
        /// </summary>
        /// <param name="sequence">The small sequence.</param>
        internal Array(string[] array)
            { _array = array.Distinct().OrderBy(element => element).ToArray(); }

        /// <summary>
        /// Enumerates a collection according to the constraint.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="collection">The collection to enumerate.</param>
        /// <returns>A constrained enumeration of the collection.</returns>
        public override IEnumerable<T> Enumerate<T>(IOrderedCollection<string, T> collection)
        { 
            for (int i=0; i<_array.Length; i++)
            {
                if (collection.TryGet(_array[i], out T element))
                    yield return element;
            }
        }

        /// <summary>
        /// Polumorphic constraint matching. 
        /// </summary>
        /// <param name="element">An element to test.</param>
        /// <returns><code>true</code> or <code>false</code> depending on if the conatraint is fullfiled or not.</returns>
        public override bool Match(string element)
        {
            return _array.Contains(element, StringComparer.InvariantCulture);
        }
    }

    private sealed class Set : Constraint
    {
        /// <summary>
        /// The constraining set
        /// </summary>
        private ISet<string> _set;

        /// <summary>
        /// Creates a set based constraint
        /// </summary>
        /// <param name="set">A set</param>
        internal Set(ISet<string> set)
            { _set = set; }

        /// <summary>
        /// Enumerates a collection according to the constraint.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="collection">The collection to enumerate.</param>
        /// <returns>A constrained enumeration of the collection.</returns>
        public override IEnumerable<T> Enumerate<T>(IOrderedCollection<string, T> collection)
        {
            var collectionSize = collection.Magnitude;

            if (_set.Count < collectionSize)
            {
                foreach (var item in _set)
                {
                    if (collection.TryGet(item, out T element))
                        yield return element;
                }

                yield break;
            }

            foreach (var element in collection)
            {
                if (_set.Contains(collection.KeyOf(element)))
                    yield return element;
            }
        }

        /// <summary>
        /// Polumorphic constraint matching. 
        /// </summary>
        /// <param name="element">An element to test.</param>
        /// <returns><code>true</code> or <code>false</code> depending on if the conatraint is fullfiled or not.</returns>
        public override bool Match(string element)
        {
            return _set.Contains(element);
        }
    }

    private sealed class View : Constraint
    {
        private Core.View _view;

        internal View(Core.View view)
            { _view = view; }

        public override IEnumerable<T> Enumerate<T>(IOrderedCollection<string, T> collection)
        {
            IKeyCollection<string>? small = null;
            IKeyCollection<string>? large = null;

            if (collection.Magnitude > _view.Magnitude)
            {
                large = collection;
                small = _view;
            }
            else
            {
                small = collection;
                large = _view;
            }

            foreach (var key in small.Between(large.Min, large.Max))
            {
                // TODO: Optimize 'inside' so that we avoid duplicate true accesses in small or large.

                if (large.Contains(key))
                {
                    if (!collection.TryGet(key, out T item))
                        throw new KeyNotFoundException("Internal error. Backing collection failed to access the expected key.");

                    yield return item;
                }
            }
        }

        /// <summary>
        /// Polumorphic constraint matching. 
        /// </summary>
        /// <param name="element">An element to test.</param>
        /// <returns><code>true</code> or <code>false</code> depending on if the conatraint is fullfiled or not.</returns>
        public override bool Match(string element)
        {
            return _view.Contains(element);
        }
    }

    /// <summary>
    /// Implements a predicate constraint.
    /// </summary>
    private sealed class Predicate : Constraint
    {
        /// <summary>
        /// The predicate to test elements with.
        /// </summary>
        private readonly Predicate<string> _predicate;

        /// <summary>
        /// Creates apredicate constraint.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        internal Predicate(Predicate<string> predicate)
            { _predicate = predicate; }

        /// <summary>
        /// Enumerates a collection according to the constraint.
        /// </summary>
        /// <typeparam name="T">The element type of the collection.</typeparam>
        /// <param name="collection">The collection to enumerate.</param>
        /// <returns>A constrained enumeration of the collection.</returns>
        public override IEnumerable<T> Enumerate<T>(IOrderedCollection<string, T> collection)
            { return collection.Where(element => _predicate(collection.KeyOf(element))); }

        /// <summary>
        /// Polumorphic constraint matching. 
        /// </summary>
        /// <param name="element">An element to test.</param>
        /// <returns><code>true</code> or <code>false</code> depending on if the conatraint is fullfiled or not.</returns>
        public override bool Match(string element)
        {
            return _predicate(element);
        }
    }

    /// <summary>
    /// Polymorphic enumeration of a collection according to the constraint.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to enumerate.</param>
    /// <returns>A constrained enumeration of the collection.</returns>
    public abstract IEnumerable<T> Enumerate<T>(IOrderedCollection<string,T> collection);

    /// <summary>
    /// Polumorphic constraint matching. 
    /// </summary>
    /// <param name="element">An element to test.</param>
    /// <returns><code>true</code> or <code>false</code> depending on if the conatraint is fullfiled or not.</returns>
    public abstract bool Match(string element);

    /// <summary>
    /// Allows strings to be converted to specific constraints.
    /// </summary>
    /// <param name="specificValue">The specific value.</param>
    /// <returns>A specific constraint or an emtpy constraint.</returns>
    public static implicit operator Constraint(string specificValue)
        { return specificValue == null ? (Constraint)new Null() : (Constraint)new Specific(specificValue); }

    /// <summary>
    /// Allows solution views to be converted to view constraints.
    /// </summary>
    /// <param name="view">A solution view.</param>
    /// <returns>A view constraint or an empty constraint.</returns>
    public static implicit operator Constraint(Mercury.Core.View view)
        { return view == null ? (Constraint)new Null() : (Constraint)new View(view); }

    /// <summary>
    /// Create a specific constraint.
    /// </summary>
    /// <param name="specificValue">The specific value.</param>
    /// <returns>A specific constraint as a constraint.</returns>
    public static Constraint Is(string specificValue)
        { return new Specific(specificValue); }

    /// <summary>
    /// Create a range constraint.
    /// </summary>
    /// <param name="minimumValue">The minimum value of the constraint.</param>
    /// <param name="maximumValue">The maximum value of the constraint.</param>
    /// <returns>A range constraint as a constraint.</returns>
    public static Constraint Between(string minimumValue, string maximumValue)
        { return new Range(minimumValue, maximumValue); }

    /// <summary>
    /// Create an array constraint.
    /// </summary>
    /// <param name="array">An array of matches.</param>
    /// <returns>An array constraint as a constraint.</returns>
    public static Constraint In(params string[] array)
        { return new Array(array); }

    /// <summary>
    /// Create a set constraint.
    /// </summary>
    /// <param name="set">A set of matches.</param>
    /// <returns>A set constraint as a constraint.</returns>
    public static Constraint In(ISet<string> set)
        { return new Set(set); }

    /// <summary>
    /// Create a true predicate constraint.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A predicate constraint as a constraint.</returns>
    public static Constraint True(Predicate<string> predicate)
        { return new Predicate(predicate); }

    /// <summary>
    /// Create a false predicate constraint.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A predicate constraint as a constraint.</returns>
    public static Constraint False(Predicate<string> predicate)
        { return new Predicate(value => !predicate(value)); }
}
