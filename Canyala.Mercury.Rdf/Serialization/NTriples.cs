//
// Copyright (c) 2011 Canyala Innovation AB
//
// All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Extensions;

using Canyala.Mercury.Rdf.Extensions;

namespace Canyala.Mercury.Rdf.Serialization
{
    /// <summary>
    /// Provides N-Triples serialization
    /// </summary>
    public static class NTriples
    {
        /// <summary>
        /// Formats a sequence of triples or turtles into a sequence of N-triple lines.
        /// </summary>
        /// <param name="turples">A sequence of triples or turtles.</param>
        /// <returns>A sequence of lines that makes up the csv document.</returns>
        public static IEnumerable<string> AsLines(IEnumerable<string[]> turples)
            { return turples.AsTriples().Select(triple => triple.Select(elem => "<{0}>".Args(elem)).Join(' ')); }
    }
}
