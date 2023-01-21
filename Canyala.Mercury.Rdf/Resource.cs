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

using Canyala.Mercury;
using Canyala.Mercury.Extensions;

namespace Canyala.Mercury.Rdf
{
    /// <summary>
    /// Provides a representation for RDF resources.
    /// </summary>
    public class Resource : Term
    {
        protected Resource() { } 

        public virtual string Full { get { return string.Empty; } }
        public virtual string Short { get { return string.Empty; } }
        public virtual string Value { get { return string.Empty; } }

        public static Resource Parse(string resource)
        {
            return Resource.Parse(resource, null);
        }

        public static new Resource Parse(string resource, Namespaces? namespaces = null)
        {
            if (resource.IsEmpty())
                return Empty;
            
            namespaces = namespaces ?? new Namespaces();

            // a literal ("42"^^<xsd:integer>) ?
            if (resource[0] == '\'' || resource[0] == '"')
                return new Literal(resource, namespaces);

            // a blank node
            if (resource.StartsWith("_:"))
                return new Blank(resource);

            // it must be an iri
            return new Iri(resource, namespaces);
        }

        public static Resource Empty = new Resource();

        public static Resource Error = new Resource();

        public bool IsError()
            { return this == Error; }

        public bool IsIri()
            { return this is Iri; }

        public bool IsSimple()
        {
            var literal = this as Literal;
            if (literal == null) return false;
            return literal.Type == null & literal.Language == null;
        }

        public bool IsBound()
            { return Full.Length > 0; }

        public bool IsBlank()
            { return this is Blank; }

        public bool? EffectiveBooleanValue()
            { return Canyala.Mercury.Rdf.Internal.Operators.EffectiveBooleanValue(this); }

        public override string ToString()
            { return Full; }

        private static string DecodeEscape(string text)
        {
            var decoded = new StringBuilder();

            foreach (var c in text)
            {
                if (c == '\\') continue;
                decoded.Append(c);
            }

            return decoded.ToString();
        }

        private static string escapeChars = "_~.-!$&'()*+,;=/?#@%";

        protected static string EncodeEscape(string text)
        {
            var encoded = new StringBuilder();

            foreach (var c in text)
            {
                if (escapeChars.Contains(c))
                    encoded.Append('\\');
                encoded.Append(c);
            }

            return encoded.ToString();
        }

        protected static bool SplitIRI(string text, Namespaces namespaces, out string prefix, out string @namespace, out string name)
        {
            if (text[0] == '<' && text[text.Length - 1] == '>')
            {
                text = text.Substring(1, text.Length - 2);

                if (!namespaces.Base.IsEmpty())
                    text = text.ResolveRelative(namespaces.Base);

                var ns = namespaces.FirstOrDefault(binding => text.StartsWith(binding.Namespace));

                if (ns == null)
                {
                    prefix = string.Empty;
                    if (text.ResolveAbsolute(namespaces.Base, out name, out @namespace))
                    { 
                        if (name.StartsWith(".."))
                            @namespace = namespaces.Base;
                    }
                    else
                    {
                        @namespace = string.Empty;
                        name = text;
                    }
                    return true;
                }

                prefix = ns.Prefix;
                @namespace = ns.Namespace;
                name = text.Substring(@namespace.Length);
                return true;
            }

            int colonAt = text.IndexOf(':');

            try
            {
                prefix = text.Substring(0, colonAt);
                @namespace = namespaces[prefix];
                name = DecodeEscape(text.Substring(colonAt + 1));
                return true;
            }
            catch
            {
                prefix = @namespace = name = string.Empty;
                return false;
            }
        }
    }
}
