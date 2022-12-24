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
