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

namespace Canyala.Mercury.Rdf
{
    /// <summary>
    /// Provides a representation for RDF/SPARQL query variables
    /// </summary>
    public class Variable : Term
    {
        private string _name;

        internal Variable(string name)
            { _name = name; }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            Variable otherVar = obj as Variable;
            if (otherVar != null && _name == otherVar._name)
                return true;

            return false;
        }

        public bool IsAnonymous
            { get { return _name.StartsWith("_:var"); } }

        public string Full
        { 
            get 
            { 
                if (IsAnonymous)
                    return _name;

                return string.Concat("?", _name); 
            } 
        }

        public string Value
            { get { return _name; } }

        public override int GetHashCode()
            { return _name.GetHashCode(); }

        public override string ToString()
            { return Full; }

        public static implicit operator Variable(string value)
            { return new Variable(value); }

        public static Variable NewAnonymous(long val)
            { return new Variable("_:var{0}".Args(val)); }
    }
}
    