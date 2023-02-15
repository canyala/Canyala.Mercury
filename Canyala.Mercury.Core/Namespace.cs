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

namespace Canyala.Mercury.Core;

/// <summary>
/// Provides a namespace representation
/// </summary>
public class Namespace
{
    private string _iri;

    /// <summary>
    /// Creates a namespace
    /// </summary>
    /// <param name="iri"></param>
    private Namespace(string iri)
        { _iri = iri; }

    public string this[string @class]
        { get { return String.Concat("<",_iri, @class,">"); } }

    public string IriRef
        { get { return String.Concat("<", _iri, ">"); } }

    public string Iri(string @class)
        { return string.Concat(_iri, @class); }

    public static Namespace FromUri(string uri)
        { return new Namespace(uri); }

    public static implicit operator string(Namespace ns)
        { return ns.ToString(); }

    public static implicit operator Namespace(string ns)
        { return Namespace.FromUri(ns.Trim('<', '>')); }

    public override string ToString()
        { return _iri; }
}

