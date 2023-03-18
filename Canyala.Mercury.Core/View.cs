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
using System.Threading.Tasks;

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;
using Canyala.Mercury.Storage.Collections;

namespace Canyala.Mercury.Core;

/// <summary>
/// Provides a view interface.
/// </summary>
public interface IView : IKeyCollection<string>
{
    // Intentionally empty.
}

/// <summary>
/// Provides a null view implementation.
/// </summary>
internal sealed class NullView : IView
{
    public string Min
        { get { return String.Empty; } }

    public string Max
        { get { return String.Empty; } }

    public long Magnitude
        { get { return 0; } }

    public bool Contains(string element)
        { return false; }

    public IEnumerable<string> Between(string low, string high)
        { return Seq.Empty<string>(); }

    public IEnumerable<string> Enumerate()
        { return Seq.Empty<string>(); }
}

/// <summary>
/// Provides a concrete view.
/// </summary>
public class View : IView, IEnumerable<string>
{
    private IView _view;

    internal View(IView view)
        { _view = view; }

    public string Min
        { get { return _view.Min; } }

    public string Max
        { get { return _view.Max; } }

    public long Magnitude
        { get { return _view.Magnitude; } }

    public bool Contains(string element)
        { return _view.Contains(element); }

    public IEnumerable<string> Between(string low, string high)
        { return _view.Between(low, high); }

    public IEnumerator<string> GetEnumerator()
        { return _view.Enumerate().GetEnumerator(); }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

    public IEnumerable<string> Enumerate()
        { return this; }

    public override string ToString()
        { return "{{ {0} {1} }}".Args(this.Take(5).Select(s => "'{0}'".Args(s.Limit(6))).Join(' '), Magnitude > 5 ? "..." : String.Empty); }

    public static IView Empty = new NullView();
}

/// <summary>
/// Provides a concrete constrained view.
/// </summary>
internal class ConstrainedView : IView
{
    private IKeyCollection<string> _keys;
    private Constraint _constraint;
    private long _magnitude;
    private string _min;
    private string _max;

    internal ConstrainedView(IKeyCollection<string> keys, Constraint constraint)
    {
        _keys = keys;
        _constraint = constraint;
        _min = _max = string.Empty;

        foreach (var key in _keys.Enumerate())
            if (_constraint.Match(key))
            {
                _magnitude++;
                if (_min.Length == 0) _min = key;
                _max = key;
            }
    }

    public string Min
        { get { return _min; } }

    public string Max
        { get { return _max; } }

    public long Magnitude
        { get { return _magnitude; } }

    public bool Contains(string element)
        { return _constraint.Match(element) && _keys.Contains(element); }

    public IEnumerable<string> Between(string low, string high)
        { return _keys.Between(low, high).Where(element => _constraint.Match(element)); }

    public IEnumerable<string> Enumerate()
        { return Between(_min, _max); }
}

/// <summary>
/// Provides a concrete constraind union view.
/// </summary>
internal class UnionView : IView
{
    private SortedSet<string> _cache;

    public UnionView(IEnumerable<IView> views, Constraint constraint)
        { _cache = new SortedSet<string>(views.SelectMany(view => view.Enumerate()).Where(element => constraint.Match(element))); }

    public string Min
        { get { return _cache.Min ?? string.Empty; } }

    public string Max
        { get { return _cache.Max ?? string.Empty; } }

    public long Magnitude
        { get { return _cache.Count; } }

    public bool Contains(string element)
        { return _cache.Contains(element); }

    public IEnumerable<string> Between(string low, string high)
        { return _cache.GetViewBetween(low, high); }

    public IEnumerable<string> Enumerate()
        { return _cache; }
}
