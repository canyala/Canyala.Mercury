//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Text;

namespace Canyala.Mercury.Rdf.Internal
{
    internal static class Comments
    {
        /// <summary>
        /// Remove comments from a turtle document.
        /// </summary>
        /// <param name="text">A turtle document as a text string.</param>
        /// <returns>A turtle document with removed comments as a text string.</returns>
        internal static string Trim(string text)
        {
            var trimmed = new StringBuilder();

            foreach (var line in Trim(Analyzer.Lines(text)))
                trimmed.AppendLine(line);

            return trimmed.ToString();
        }

        /// <summary>
        /// Remove comments from a turtle document.
        /// </summary>
        /// <param name="lines">A turtle document as sequence of text line strings.</param>
        /// <returns>A turtle document with removed comments as a sequence of text line strings.</returns>
        internal static IEnumerable<string> Trim(IEnumerable<string> lines)
        {
            var inLongQuote = false;
            var inLongSingleQuote = false;

            foreach (var line in lines)
            {
                var cutPosition = line.Length;

                for (int pos = 0; pos < line.Length; pos++)
                {
                    #region #'s inside IRIREF's are not comments

                    if (line[pos] == '<')
                    {
                        pos++;
                        while (pos < line.Length && line[pos] != '>')
                            pos++;

                        continue;
                    }

                    #endregion

                    #region #'s inside " " or """ """ strings are not comments

                    if (inLongQuote || line[pos] == '"')
                    {
                        if (inLongQuote || (pos + 2 < line.Length && line[pos + 1] == '"' && line[pos + 2] == '"'))
                        {
                            if (inLongQuote == false)
                            {
                                inLongQuote = true;
                                pos += 3;
                            }

                            if (pos < line.Length)
                            {
                                while (pos + 3 < line.Length && !(line[pos + 0] == '"' && line[pos + 1] == '"' && line[pos + 2] == '"'))
                                {
                                    if (line[pos] == '"')
                                        while (++pos + 3 < line.Length && line[pos] != '"') ;

                                    pos++;
                                }

                                if (line[pos + 0] == '"' && line[pos + 1] == '"' && line[pos + 2] == '"')
                                {
                                    inLongQuote = false;
                                    pos += 3;
                                }
                            }

                            continue;
                        }

                        pos++;
                        while (pos < line.Length && line[pos] != '"')
                        {
                            if (line[pos] == '\\')
                                pos++;

                            pos++;
                        }

                        continue;
                    }

                    #endregion

                    #region #'s inside ' ' or ''' ''' strings are not comments

                    if (inLongSingleQuote || line[pos] == '\'')
                    {
                        if (inLongSingleQuote || (pos + 2 < line.Length && line[pos + 1] == '\'' && line[pos + 2] == '\''))
                        {
                            if (inLongSingleQuote == false)
                            {
                                inLongSingleQuote = true;
                                pos += 3;
                            }

                            if (pos < line.Length)
                            {
                                while (pos + 2 < line.Length && !(line[pos + 0] == '\'' && line[pos + 1] == '\'' && line[pos + 2] == '\''))
                                {
                                    if (line[pos] == '\'')
                                        while (++pos + 3 < line.Length && line[pos] != '\'') ;

                                    pos++;
                                }

                                if (line[pos + 0] == '\'' && line[pos + 1] == '\'' && line[pos + 2] == '\'')
                                {
                                    inLongSingleQuote = false;
                                    pos += 3;
                                }
                            }

                            continue;
                        }

                        pos++;
                        while (pos < line.Length && line[pos] != '\'')
                        {
                            if (line[pos] == '\\')
                                pos++;

                            pos++;
                        }

                        continue;
                    }

                    #endregion

                    if (inLongQuote == false && inLongSingleQuote == false && line[pos] == '#')
                    {
                        cutPosition = pos;
                        break;
                    }
                }

                yield return line.Substring(0, cutPosition);
            }
        }
    }
}
