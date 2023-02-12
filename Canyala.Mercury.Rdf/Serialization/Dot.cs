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
