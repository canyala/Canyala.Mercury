//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

using Canyala.Mercury.Extensions;

namespace Canyala.Mercury.Rdf
{
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
            if (!SplitIRI(text, namespaces, ref _prefix, ref _namespace, ref _class))
                throw new ArgumentException("Illegal prefix in IRI : {0}".Args(text));
        }

        internal Iri(Iri iri, Namespaces namespaces)
        {
            _prefix = namespaces.PrefixOf(iri._namespace);
            _namespace = iri._namespace;
            _class = iri._class;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            Iri other = obj as Iri;
            if (other != null && ToString().Equals(other.ToString()))
                return true;

            return false;
        }

        public static bool operator ==(Iri lhs, Iri rhs)
        { 
            if (!Equals(lhs, null) && !Equals(rhs, null))
                return lhs.Equals(rhs);
            else if (Equals(lhs, null) && Equals(rhs, null))
                return true;
            else
                return false;
        }

        public static bool operator !=(Iri lhs, Iri rhs)
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
                    return string.Concat(_namespace ?? "", _class);
            } 
        }
    }
}
