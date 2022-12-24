//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

namespace Canyala.Mercury.Rdf
{
    /// <summary>
    /// Provides a representation for RDF query terms.
    /// </summary>
    public abstract class Term
    {
        public static Term Parse(string term, Namespaces namespaces)
        {
            // a variable (?foo) ?
            if (term.StartsWithAny("?", "$"))
                return new Variable(term.Substring(1));

            return Resource.Parse(term, namespaces);
        }

        public static implicit operator string(Term term)
            { return term.ToString(); }
    }
}
