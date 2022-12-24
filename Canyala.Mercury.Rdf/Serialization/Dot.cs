//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Extensions;

using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Serialization;

namespace Canyala.Mercury.Rdf.Serialization
{
    public static class Dot
    {
        /// <summary>
        /// Formats a sequence of triples or turtles into a sequence of dot lines.
        /// </summary>
        /// <param name="turtles">A sequence of triples or turtles.</param>
        /// <param name="name">The name of the dot.</param>
        /// <returns>A sequence of lines the makes up the dot document.</returns>
        public static IEnumerable<string> AsLines(this IEnumerable<string[]> turples, string name)
        {
            yield return "graph \"{0}\" {{".Args(name);
            foreach (var triple in turples.AsTriples()) yield return "\"{0}\" -- \"{1}\" [label=\"{2}\"]".Args(triple[0], triple[1], triple[2]);
            yield return "}";
        }
    }
}
