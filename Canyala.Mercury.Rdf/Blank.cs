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

        public override bool Equals(object obj)
        {
            Blank other = obj as Blank;
            return other != null && ToString().Equals(other.ToString());
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
