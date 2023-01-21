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

namespace Canyala.Mercury.Rdf.Extensions
{
    /// <summary>
    /// Provides extensions for rdf term handling.
    /// </summary>
    public static class TermExtensions
    {
        /// <summary>
        /// Transforms a sequence of string arrays into a sequenece of rdf term arrays.
        /// </summary>
        /// <param name="turples">A sequence of possibly jagged string arrays with maximum length of 3´.</param>
        /// <param name="namespaces">A rdf namespaces object that provides namespaces for the resources.</param>
        /// <returns>A sequence of possibly jagged (turtle like) rdf term arrays.</returns>
        public static IEnumerable<Term[]> AsTerms(this IEnumerable<string[]> tuples, Namespaces namespaces)
            { foreach (var tuple in tuples) yield return tuple.Select(term => Term.Parse(term, namespaces)).ToArray(); }

        /// <summary>
        /// Transforms a sequence of string arrays into a sequenece of rdf resource arrays.
        /// </summary>
        /// <param name="turples">A sequence of possibly jagged string arrays with maximum length of 3´.</param>
        /// <param name="namespaces">A rdf namespaces object that provides namespaces for the resources.</param>
        /// <returns>A sequence of possibly jagged (turtle like) rdf resource arrays.</returns>
        public static IEnumerable<Resource[]> AsResources(this IEnumerable<string[]> tuples, Namespaces namespaces)
            { foreach (var tuple in tuples) yield return tuple.Select(resource => Resource.Parse(resource, namespaces)).ToArray(); }

        /// <summary>
        /// Joins consecutive lines upto a terminator character.
        /// </summary>
        /// <param name="lines">A sequence of lines.</param>
        /// <param name="terminator">A terminator character.</param>
        /// <returns>A sequence of lines and joined lines.</returns>
        public static IEnumerable<string> CombineLines(this IEnumerable<string> lines, char terminator)
        {
            var collection = new StringBuilder();

            foreach (var line in lines)
            {
                collection.AppendLine(line);

                var terminatorPos = line.LastIndexOf(terminator);

                if (terminatorPos < 0)
                    continue;

                if (line.TrimEnd().Length - 1 == terminatorPos)
                {
                    yield return collection.ToString();
                    collection.Clear();
                }
            }

            if (collection.Length > 0)
                yield return collection.ToString();
        }
    }
}
