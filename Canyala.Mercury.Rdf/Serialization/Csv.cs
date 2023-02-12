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

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Text;

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
