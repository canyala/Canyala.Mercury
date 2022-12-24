//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;

namespace Canyala.Mercury
{
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
}

