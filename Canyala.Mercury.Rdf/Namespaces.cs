//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Extensions;

using Canyala.Mercury;

namespace Canyala.Mercury.Rdf
{
    /// <summary>
    /// 
    /// </summary>
    public class Namespaces : IEnumerable<Namespaces.Binding>
    {
        private List<Binding> _bindings = new List<Binding>();

        private string _base = String.Empty;
        public string Base { get { return _base; } set { _base = value ?? String.Empty; } }

        public Namespace this[string prefix]
        {
            get { 
                if (prefix != null)
                    return Namespace.FromUri(FindByPrefix(prefix).Namespace); 
                else
                    return Namespace.FromUri(Base);
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

            if (byNamespace == null && byPrefix == null)
                _bindings.Add(binding);
            else if (byNamespace == null)
                byPrefix.Namespace = binding.Namespace;
            else if (byPrefix == null)
                _bindings.Add(binding);
        }

        public string PrefixOf(string @namespace)
        {
            var binding = FindByNamespace(@namespace);
            return binding != null ? binding.Prefix : null;
        }

        public string PrefixOf(Namespace @namespace)
        {
            var binding = FindByNamespace(@namespace);
            return binding != null ? binding.Prefix : null;
        }

        public class Binding 
        {
            public string Prefix;
            public string Namespace;
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

        private Binding FindByPrefix(string prefix)
        {
            return _bindings.FirstOrDefault(b => b.Prefix == prefix);
        }

        private Binding FindByNamespace(string ns)
        {
            return _bindings.FirstOrDefault(b => b.Namespace == ns);
        }

    }
}
