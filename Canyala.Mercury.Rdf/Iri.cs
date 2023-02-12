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

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury.Core.Extensions;

namespace Canyala.Mercury.Rdf;

/// <summary>
/// Provides a representation for RDF IRI's
/// </summary>
public class Iri : Resource
{
    private readonly string _prefix;
    private readonly string _namespace;
    private readonly string _class;

    internal Iri(string text, Namespaces namespaces)
    {
        if (!SplitIRI(text, namespaces, out _prefix, out _namespace, out _class))
            throw new ArgumentException("Illegal prefix in IRI : {0}".Args(text));
    }

    internal Iri(Iri iri, Namespaces namespaces)
    {
        _prefix = namespaces.PrefixOf(iri._namespace) ?? string.Empty;
        _namespace = iri._namespace ?? string.Empty;
        _class = iri._class;
    }

    public override bool Equals(object? obj)
    {
        if (object.ReferenceEquals(this, obj))
            return true;

        return obj is Iri other 
            && ToString().Equals(other.ToString());
    }

    public static bool operator ==(Iri? lhs, Iri? rhs)
    { 
        if (!Equals(lhs, null) && !Equals(rhs, null))
            return lhs.Equals(rhs);
        else if (Equals(lhs, null) && Equals(rhs, null))
            return true;
        else
            return false;
    }

    public static bool operator !=(Iri? lhs, Iri? rhs)
    {
        if (!Equals(lhs, null) && !Equals(rhs, null))
            return !lhs.Equals(rhs);
        else if (Equals(lhs, null) && Equals(rhs, null))
            return false;
        else
            return true;
    }

    public override int GetHashCode()
        { return ToString().GetHashCode(); }

    public override string ToString()
        { return Full; }

    public override string Full
        { get { return string.Concat('<', Value, '>'); } }

    public override string Short
    {
        get
        {
            if (_prefix == null)
            {
                if (_namespace == null)
                    return Full;

                return string.Concat('<', _class, '>');
            }

            return String.Concat(_prefix, ':', EncodeEscape(_class));
        }
    }

    public override string Value
    { 
        get 
        {
            if (_prefix == null)
                return _namespace.IsEmpty() ? _class : _class.ResolveRelative(_namespace);
            else
                return string.Concat(_namespace, _class);
        } 
    }
}
