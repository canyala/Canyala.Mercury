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


using Canyala.Lagoon.Extensions;

namespace Canyala.Mercury.Core;

/// <summary>
/// Provides a solution for graph enumerations.
/// </summary>
public class Solution : IDisposable, IEnumerable<string[]>
{
    private Func<IEnumerable<string[]>> _resultsBuilder;
    private IEnumerable<string[]>? _results;

    private Func<IView[]> _setsBuilder;
    private IView[]? _views;

    private int _width;

    /// <summary>
    /// The width of the solution, count of views/columns.
    /// </summary>
    public int Width { get { return _width; } }

    public Solution(Func<IEnumerable<string[]>> resultsBuilder, Func<IView[]> setsBuilder, int width)
    {
        _resultsBuilder = resultsBuilder;
        _setsBuilder = setsBuilder;
        _width = width;
    }

    public IEnumerator<string[]> GetEnumerator()
        { return (_results = _resultsBuilder()).GetEnumerator(); }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

    public void Dispose()
    {
        var disposable = _results as IDisposable;
        if (disposable != null) disposable.Dispose();
    }

    public View this[int index]
        { get { return new View((_views = _views ?? _setsBuilder())[index]); } }

    public override string ToString()
        { return this.Select(row => "[{0}]".Args(row.Select(column => "'{0}'".Args(column)).Join(","))).Join(","); }
}
