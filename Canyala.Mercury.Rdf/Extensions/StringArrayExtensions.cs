//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Canyala.Lagoon.Collections;
using Canyala.Lagoon.Contracts;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;
using Canyala.Lagoon.Models;
using Canyala.Lagoon.Text;

using Canyala.Mercury.Rdf.Serialization;

namespace Canyala.Mercury.Rdf.Extensions
{
    /// <summary>
    /// Provides extensions for working with RDF data sequences.
    /// </summary>
    public static class StringArrayExtensions
    {
        #region RDF Extensions

        /// <summary>
        /// Maps a sequnce of turtles into a sequence of triples.
        /// </summary>
        /// <param name="turtles">A normalized sequence of triples.</param>
        /// <returns>A sequence of triples.</returns>
        public static IEnumerable<string[]> AsTriples(this IEnumerable<string[]> turtles)
        {
            var triple = Seq.Array(String.Empty, String.Empty, String.Empty);

            foreach (var turtle in turtles)
            {
                for (int i = 0; i < turtle.Length; i++)
                    triple[triple.Length - turtle.Length + i] = turtle[i];

                yield return new string[] { triple[0], triple[1], triple[2] };
            }
        }

        /// <summary>
        /// Maps a sequence of triples into a sequence of turtles.
        /// </summary>
        /// <param name="triples">A sequence of triples.</param>
        /// <returns>A sequence of turtles.</returns>
        public static IEnumerable<string[]> AsTurtles(this IEnumerable<string[]> triples)
        {
            string[] previous = null;

            foreach (var current in triples)
            {
                if (previous != null)
                    yield return TurtleOf(previous, current);
                else
                    yield return current;

                previous = current;
            }
        }

        /// <summary>
        /// Maps a table into a sequence of turtles.
        /// </summary>
        /// <param name="table">The table</param>
        /// <returns>A sequence of turtles.</returns>
        public static IEnumerable<string[]> AsTurtles(this string[,] table)
            { return table.AsRows().AsTurtles(); }

        /// <summary>
        /// Returns the subject part of a turtle.
        /// </summary>
        /// <param name="turtle">The turtle.</param>
        /// <returns>The subject of the turtle.</returns>
        public static string Subject(this string[] turtle)
        {
            Contract.Requires(turtle.Length == 3, "Must have turtle with subject, predicate and object");

            return turtle[0];
        }

        /// <summary>
        /// Returns the predicate part of a turtle.
        /// </summary>
        /// <param name="turtle">The turtle.</param>
        /// <returns>The predicate of the turtle</returns>
        public static string Predicate(this string[] turtle)
        {
            Contract.Requires(turtle.Length >= 2, "Must have turtle with at least predicate and object");

            return turtle[turtle.Length - 2];
        }

        /// <summary>
        /// Returns the object part of a turtle.
        /// </summary>
        /// <param name="turtle">The turtle</param>
        /// <returns>The object of the turtle.</returns>
        public static string Object(this string[] turtle)
        {
            Contract.Requires(turtle.Length >= 1, "Must have turtle with at least an object");

            return turtle[turtle.Length - 1];
        }

        #endregion

        #region RDF formatting extensions

        /// <summary>
        /// Formats a sequence of triples or turtles into a sequence of turtle lines.
        /// </summary>
        /// <param name="turples">The triples or turtles.</param>
        /// <param name="namespaces">A dictionary where key is URI of a namespace and value is name of a namespace.</param>
        /// <returns>A sequence of lines that makes up the turtle docuemnt.</returns>
        public static IEnumerable<string> AsTurtle(this IEnumerable<string[]> turples, Namespaces namespaces = null)
            { return Turtle.AsLines(turples, namespaces); }

        /// <summary>
        /// Formats a sequence of triples or turtles into a sequence of dot lines.
        /// </summary>
        /// <param name="turtles">A sequence of triples or turtles.</param>
        /// <param name="name">The name of the dot.</param>
        /// <returns>A sequence of lines the makes up the dot document.</returns>
        public static IEnumerable<string> AsDot(this IEnumerable<string[]> turples, string name)
            { return Dot.AsLines(turples, name); }

        /// <summary>
        /// Formats a sequence of triples or turtles into a sequence of comma separated lines.
        /// </summary>
        /// <param name="tuples">A sequence of tuples.</param>
        /// <param name="commaSeparator">The comma separator to use. Defaults to ','</param>
        /// <returns>A sequence of lines that makes up the csv document.</returns>
        public static IEnumerable<string> AsCsv(this IEnumerable<string[]> tuples, char commaSeparator = ';')
            { return Csv.AsLines(tuples, commaSeparator); }

        public static string AsCsvText(this IEnumerable<string[]> tuples, char commaSeparator = ';')
            { return Csv.AsLines(tuples).Join(Environment.NewLine); }

        /// <summary>
        /// Formats a sequence of triples or turtles into a sequence of N-triple lines.
        /// </summary>
        /// <param name="turples">A sequence of triples or turtles.</param>
        /// <param name="commaSeparator">The comma separator to use. Defaults to ','</param>
        /// <returns>A sequence of lines that makes up the csv document.</returns>
        public static IEnumerable<string> AsNTriples(this IEnumerable<string[]> turples)
            { return turples.AsTriples().Select(triple => triple.Select(elem => "<{0}>".Args(elem)).Join(' ')); }

        /// <summary>
        /// Formats a sequence of triples or turtles into a rdf/xml document.
        /// </summary>
        /// <param name="turples">A sequence of triples or turtles.</param>
        /// <param name="namespaces">A dictionary where key is URI of a namespace and value is name of a namespace.</param>
        /// <returns>A sequence of lines that represents the turples in rdf/xml format.</returns>
        public static IEnumerable<string> AsRdfXml(this IEnumerable<string[]> turples, Namespaces namespaces = null)
            { return RdfXml.AsLines(turples, namespaces); }

        public static IEnumerable<string> AsN3(this IEnumerable<string[]> turples, Namespaces namespaces = null)
            { yield break; }

        #endregion

        #region Private methods
        private static string[] TurtleOf(string[] referenceTriple, string[] valueTriple)
        {
            Contract.Requires(referenceTriple.Length == 3, "referenceTriple.Length is not 3");
            Contract.Requires(valueTriple.Length == 3, "valueTriple.Length is not 3");

            if (referenceTriple[0] != valueTriple[0])
                return Seq.Array(valueTriple[0], valueTriple[1], valueTriple[2]);
            if (referenceTriple[1] != valueTriple[1])
                return Seq.Array(valueTriple[1], valueTriple[2]);
            else
                return Seq.Array(valueTriple[2]);
        }
        #endregion
    }
}
