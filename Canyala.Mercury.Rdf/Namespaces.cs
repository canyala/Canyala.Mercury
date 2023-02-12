/*

  MIT License
 
  Copyright (c) 2012-2022 Canyala Innovation (Martin Fredriksson)

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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Core.Extensions;

using Canyala.Mercury.Core;

namespace Canyala.Mercury.Rdf;

/// <summary>
/// 
/// </summary>
public class Namespaces : IEnumerable<Namespaces.Binding>
{
    private readonly List<Binding> _bindings = new();

    private string _base = String.Empty;
    public string Base { get { return _base; } set { _base = value; } }

    public Namespace this[string prefix]
    {
        get
        {
            var binding = FindByPrefix(prefix) ??
                throw new ArgumentException($"'{prefix}' not found.", nameof(prefix));

            return Namespace.FromUri(binding.Namespace);
        }
    }

    public Namespaces Add(string prefix, string ns)
    {
        Add(new Binding { Prefix = prefix, Namespace = ns });
        return this;
    }

    public Namespaces Add(string prefix, Namespace ns)
    {
        Add(new Binding { Prefix = prefix, Namespace = ns });
        return this;
    }

    public void Add(Binding binding)
    {
        var byPrefix = FindByPrefix(binding.Prefix);
        var byNamespace = FindByNamespace(binding.Namespace);

        if (byNamespace is null && byPrefix is null)
            _bindings.Add(binding);
        else if (byPrefix is not null)
            byPrefix.Namespace = binding.Namespace; // Yes, an update...
        else if (byNamespace is null)
            _bindings.Add(binding);
    }

    public string PrefixOf(string @namespace)
    {
        var binding = FindByNamespace(@namespace);
        return binding is null ? string.Empty : binding.Prefix;
    }

    public string PrefixOf(Namespace @namespace)
    {
        var binding = FindByNamespace(@namespace);
        return binding is null ? string.Empty : binding.Prefix;
    }

    public class Binding 
    {
        public required string Prefix;
        public required string Namespace;
        internal Binding() { }

        public override string ToString()
        {
            return "Binding {0} - {1}".Args(Prefix, Namespace);
        }
    }

    public IEnumerator<Namespaces.Binding> GetEnumerator()
    {
        return _bindings.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private Binding? FindByPrefix(string prefix)
    {
        return _bindings.FirstOrDefault(b => b.Prefix == prefix);
    }

    private Binding? FindByNamespace(string ns)
    {
        return _bindings.FirstOrDefault(b => b.Namespace == ns);
    }

}
