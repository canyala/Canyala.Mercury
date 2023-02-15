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

namespace Canyala.Mercury.Storage.Collections;

/// <summary>
/// Represents ordered collections.
/// </summary>
/// <typeparam name="TKey">The type of the keys of the collection.</typeparam>
/// <typeparam name="TElement">The type of the elements of the collection.</typeparam>
public interface IOrderedCollection<TKey,TElement> : IEnumerable<TElement>, IKeyCollection<TKey>
{
    /// <summary>
    /// Ordered collections can be enumerated starting at a specific element.
    /// </summary>
    /// <param name="startAt">The element to start at.</param>
    /// <param name="ascending">Ascending (<code>true</code>) or descending (<code>false</code>) enumeration.</param>
    /// <param name="inclusive">If keys not present in the collection should be considered to be within the returned collection or not.</param>
    /// <returns>A sequence of elements.</returns>
    IEnumerable<TElement> Enumerate(TKey startAt, bool ascending, bool inclusive);

    /// <summary>
    /// Ordered collections can be enumerated between specific elements.
    /// </summary>
    /// <param name="from">Low key.</param>
    /// <param name="to">High key.</param>
    /// <param name="inclusive">If keys not present should be within the returned range or not.</param>
    /// <returns>A sequence of elements.</returns>
    IEnumerable<TElement> Enumerate(TKey from, TKey to, bool ascending, bool inclusive);

    /// <summary>
    /// Ordered collections can extract an element by a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value"><code>out</code> reference to an element.</param>
    /// <returns><code>true</code> if an element could be extracted, otherwise <code>false</code>.</returns>
    bool TryGet(TKey key, out TElement value);

    /// <summary>
    /// Extracts the key of an element.
    /// </summary>
    /// <param name="element">An element</param>
    /// <returns>The key</returns>
    TKey KeyOf(TElement element);
}
