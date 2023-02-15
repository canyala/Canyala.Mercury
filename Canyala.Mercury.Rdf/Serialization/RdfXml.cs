/*

  MIT License
 
  Copyright (c) 2011-2023 Canyala Innovation (Martin Fredriksson)

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

namespace Canyala.Mercury.Rdf.Serialization
{
    public static class RdfXml
    {
        /// <summary>
        /// Formats a sequence of triples or turtles into a rdf/xml document.
        /// </summary>
        /// <param name="triples">A sequence of triples or turtles.</param>
        /// <param name="namespaces">A dictionary where key is URI of a namespace and value is name of a namespace.</param>
        /// <returns>A sequence of lines that represents the turples in rdf/xml format.</returns>
        public static IEnumerable<string> AsLines(IEnumerable<string[]> triples, Namespaces? namespaces = null)
        {
            var turtles = triples.AsTurtles();
            namespaces = namespaces ?? new Namespaces();
            Resource? lastPredicate = null;
            Resource? lastSubject = null;

            yield return "<?xml version=\"1.0\"?>";
            var xmlNamespaces = namespaces.Select(ns => "xmlns:{0}=\"{1}\"".Args(ns.Prefix, ns.Namespace)).Join(' ');
            yield return "<rdf:RDF {0}>".Args(xmlNamespaces);

            foreach (var resources in turtles.AsResources(namespaces))
            {
                if (resources.Length == 3)
                {
                    if (lastSubject != null)
                        yield return "</rdf:Description>";

                    lastSubject = resources[0];
                    lastPredicate = resources[1];

                    yield return "<rdf:Description rdf:about=\"{0}\">".Args(resources[0].Value);
                    yield return "<{0}>{1}</{0}>".Args(resources[1].Short, resources[2].Value);
                }
                else if (resources.Length == 2)
                {
                    lastPredicate = resources[0];

                    yield return "<{0}>{1}</{0}>".Args(resources[0].Value, resources[1].Value);
                }
                else if (resources.Length == 1)
                {
                    yield return "<{0}>{1}</{0}>".Args(lastPredicate!.Value, resources[0].Value);
                }
            }

            yield return "</rdf:Description>";
            yield return "</rdf:RDF>";
        }
    }
}
