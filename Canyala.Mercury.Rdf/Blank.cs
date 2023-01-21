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

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;
using System.Globalization;

namespace Canyala.Mercury.Rdf
{
    /// <summary>
    /// Provides a representation for RDF blanks.
    /// </summary>
    public class Blank : Resource
    {
        private string _name;
        /// <summary>
        /// Creates a blank from a string.
        /// </summary>
        /// <param name="content">A string, must be formatted as a blank.</param>
        internal Blank(string text)
        {
            _name = text.TrimStartAny("_:");
        }

        public override bool Equals(object? obj)
        {
            return obj is Blank other && ToString()
                .Equals(other.ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Blanks can be converted to string.
        /// </summary>
        /// <returns>The blank as a string.</returns>
        public override string ToString()
        { return Full; }

        /// <summary>
        /// Creates a new global blank.
        /// </summary>
        /// <returns>A new global blank.</returns>
        internal static Blank NewBlank()
            { return new Blank("_:" + Guid.NewGuid().ToString()); }

        public override string Full
        {
            get { return string.Concat("_:", _name); }
        }

        public override string Short
        {
            get { return Full; }
        }

        public override string Value
        {
            get { return Full; }
        }
    }
}
