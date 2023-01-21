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
using Canyala.Mercury;

namespace Canyala.Mercury.Extensions;

public static class ConstraintExtensions
{
    public static IEnumerable<T> ConstrainBy<T>(this IOrderedCollection<string,T> set, Constraint constraint)
        { return (constraint ?? Constraint.Empty).Enumerate<T>(set); }

    public static IEnumerable<KeyValuePair<string, T>> ConstrainBy<T>(this SortedDictionary<string, T> dictionary, Constraint constraint)
        { return (constraint ?? Constraint.Empty).Enumerate(new SortedDictionaryAsOrderedCollection<T>(dictionary)); }

    public static IEnumerable<string> ConstrainBy(this SortedSet<string> set, Constraint constraint)
        { return (constraint ?? Constraint.Empty).Enumerate(new SortedSetAsOrderedCollection(set)); }

    public static IView AsView<T>(this SortedDictionary<string, T> dictionary, Constraint constraint)
        { return new SortedDictionaryAsOrderedCollection<T>(dictionary, constraint); }

    public static IView AsView(this SortedSet<string> set, Constraint constraint)
        { return new SortedSetAsOrderedCollection(set, constraint); }

    /// <summary>
    /// Allows SortedSet to be treated as an IOrderedCollection
    /// </summary>
    private sealed class SortedSetAsOrderedCollection : IOrderedCollection<string,string>, IView
    {
        private readonly SortedSet<string> _set;
        private readonly Constraint? _constraint;

        internal SortedSetAsOrderedCollection(SortedSet<string> set)
        {
            _set = set;
        }

        internal SortedSetAsOrderedCollection(SortedSet<string> set, Constraint constraint)
        { 
            _set = set;
            _constraint = constraint;
        }

        public string Min
            { get { return _set.Min ?? string.Empty; } }

        public string Max
            { get { return _set.Max ?? string.Empty; } }

        public IEnumerable<string> Enumerate(string startAt, bool ascending, bool inclusive)
        {
            if (ascending)
                return _set.GetViewBetween(startAt, _set.Max);
            else
                return _set.GetViewBetween(_set.Min, startAt);
        }

        public IEnumerable<string> Enumerate(string from, string to, bool ascending, bool inclusive)
        {
            if (ascending)
                return _set.GetViewBetween(from, to);
            else
                return _set.GetViewBetween(from, to).Reverse();
        }

        public IEnumerable<string> Between(string low, string high)
            { return Enumerate(low, high, true, false); }

        public bool TryGet(string key, out string value)
        {
            if (_set.Contains(key))
            {
                value = key;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public string KeyOf(string element)
            { return element; }

        public bool Contains(string key)
            { return _set.Contains(key); }

        public long Magnitude
            { get { return _set.Count; } } 

        public IEnumerator<string> GetEnumerator()
            { return _set.GetEnumerator(); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            { return _set.GetEnumerator(); }

        public IEnumerable<string> Enumerate()
            { return _set; }
    }

    /// <summary>
    /// Allows a SortedDictionary to be treated as an IOrderedCollection
    /// </summary>
    private sealed class SortedDictionaryAsOrderedCollection<T> : IOrderedCollection<string,KeyValuePair<string,T>>, IView
    {
        private readonly SortedDictionary<string,T> _dictionary;
        private readonly Constraint? _constraint;

        internal SortedDictionaryAsOrderedCollection(SortedDictionary<string, T> dictionary)
        {
            _dictionary = dictionary;
        }

        internal SortedDictionaryAsOrderedCollection(SortedDictionary<string,T> dictionary, Constraint constraint)
        { 
            _dictionary = dictionary;
            _constraint = constraint;
        }

        public string Min
        {
            get { return _dictionary.Keys.Min() ?? string.Empty; }
        }

        public string Max
        {
            get { return _dictionary.Keys.Max() ?? string.Empty; }
        }

        public IEnumerable<KeyValuePair<string, T>> Enumerate(string startAt, bool ascending, bool inclusive)
        {
            if (ascending)
            {
                foreach (var element in _dictionary)
                    if (element.Key.CompareTo(startAt) >= 0)
                        yield return element;
            }
            else
            {
                foreach (var element in _dictionary.Reverse())
                    if (element.Key.CompareTo(startAt) <= 0)
                        yield return element;
            }
        }

        public IEnumerable<KeyValuePair<string, T>> Enumerate(string from, string to, bool ascending, bool inclusive)
        {
            foreach(var element in ascending ? _dictionary : _dictionary.Reverse())
                if (element.Key.CompareTo(from) >= 0 && element.Key.CompareTo(to) <= 0)
                    yield return element;
        }

        public IEnumerable<string> Between(string low, string high)
        {
            return Enumerate(low, high, true, false).Select(pair => pair.Key);
        }

        public bool TryGet(string key, out KeyValuePair<string, T> element)
        {
            if (_dictionary.TryGetValue(key, out T? value))
            {
                element = new KeyValuePair<string,T>(key, value);
                return true;
            }
                
            element = default(KeyValuePair<string,T>);
            return false;
        }

        public string KeyOf(KeyValuePair<string, T> element)
        {
            return element.Key;
        }

        public bool Contains(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public long Magnitude
        {
            get
            {
                return _dictionary.Count;
            }
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public IEnumerable<string> Enumerate()
        {
            return _dictionary.Keys;
        }
    }
}
