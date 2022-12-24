//
// Copyright (c) 2011 Canyala Innovation AB
//
// All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Text;

using Canyala.Mercury.Rdf.Extensions;

namespace Canyala.Mercury.Rdf.Serialization
{
    /// <summary>
    /// Provides methods for working with csv data.
    /// </summary>
    public class Csv
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="commaCharacter"></param>
        /// <returns></returns>
        public static IEnumerable<string[]> FromLines(IEnumerable<string> lines, char commaCharacter = ',')
            { foreach (var line in lines) yield return line.Split(commaCharacter); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="commaCharacter"></param>
        /// <returns></returns>
        public static IEnumerable<string[]> FromText(string text, char commaCharacter = ',')
            { return FromLines(Analyzer.Lines(text), commaCharacter); }

        /// <summary>
        /// Formats a sequence of triples or turtles into a sequence of comma separated lines.
        /// </summary>
        /// <param name="tuples">A sequence of string arrays.</param>
        /// <param name="commaSeparator">The comma separator to use. Defaults to ','</param>
        /// <returns>A sequence of lines that makes up the csv document.</returns>
        public static IEnumerable<string> AsLines(IEnumerable<string[]> tuples, char commaSeparator = ',')
            { return tuples.Select(tuple => tuple.Join(commaSeparator)); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tuples"></param>
        /// <param name="commaSeparator"></param>
        /// <returns></returns>
        public static string AsText(IEnumerable<string[]> tuples, char commaSeparator = ',')
            { return AsLines(tuples, commaSeparator).Join(Environment.NewLine); }
    }
}
