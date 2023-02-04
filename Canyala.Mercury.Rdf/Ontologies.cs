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

using Canyala.Mercury.Core;

namespace Canyala.Mercury.Rdf;

/// <summary>
/// Provides declarations for common ontologies.
/// </summary>
public static class Ontologies
{
    private static Namespaces?  _namespaces;

    public static Namespaces Namespaces
    {
        get
        {
            if (_namespaces == null)
                _namespaces = new Canyala.Mercury.Rdf.Namespaces 
                {
                    { Rdf.Prefix, Rdf.ns },
                    { Rdfs.Prefix, Rdfs.ns },
                    { Xsd.Prefix, Xsd.ns },
                    { Sfn.Prefix, Sfn.ns },
                    { Foaf.Prefix, Foaf.ns }
                };

            return _namespaces;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Rdf
    {
        /// <summary>
        /// http://www.w3.org/1999/02/22-rdf-syntax-ns#
        /// </summary>
        public static readonly Namespace ns = Namespace.FromUri("http://www.w3.org/1999/02/22-rdf-syntax-ns#");

        /// <summary>
        /// 
        /// </summary>
        public static readonly string Prefix = "rdf";

        /// <summary>
        /// http://www.w3.org/1999/02/22-rdf-syntax-ns#first
        /// </summary>
        public static readonly Iri first = _(ns["first"]);

        /// <summary>
        /// http://www.w3.org/1999/02/22-rdf-syntax-ns#rest
        /// </summary>
        public static readonly Iri rest = _(ns["rest"]);

        /// <summary>
        /// http://www.w3.org/1999/02/22-rdf-syntax-ns#nil
        /// </summary>
        public static readonly Iri nil = _(ns["nil"]);

        /// <summary>
        /// http://www.w3.org/1999/02/22-rdf-syntax-ns#type
        /// </summary>
        public static readonly Iri type = _(ns["type"]);

        /// <summary>
        /// http://www.w3.org/1999/02/22-rdf-syntax-ns#langString
        /// </summary>
        public static readonly Iri langString = _(ns["langString"]);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Xsd
    {
        /// <summary>
        /// Standard Xsd prefix name.
        /// </summary>
        public static readonly string Prefix = "xsd";

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#
        /// </summary>
        public static readonly Namespace ns = Namespace.FromUri("http://www.w3.org/2001/XMLSchema#");

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#integer
        /// </summary>
        public static readonly Iri integer = _(ns["integer"]);

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#decimal
        /// </summary>
        public static readonly Iri @decimal = _(ns["decimal"]);

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#float
        /// </summary>
        public static readonly Iri @float = _(ns["float"]);

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#double
        /// </summary>
        public static readonly Iri @double = _(ns["double"]);

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#boolean
        /// </summary>
        public static readonly Iri boolean = _(ns["boolean"]);

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#string
        /// </summary>
        public static readonly Iri @string = _(ns["string"]);

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#dateTime
        /// </summary>
        public static readonly Iri dateTime = _(ns["dateTime"]);

        /// <summary>
        /// http://www.w3.org/2001/XMLSchema#dayTimeDuration
        /// </summary>
        public static readonly Iri dayTimeDuration = _(ns["dayTimeDuration"]);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Rdfs
    {
        /// <summary>
        /// http://www.w3.org/2000/01/rdf-schema#
        /// </summary>
        public static readonly Namespace ns = Namespace.FromUri("http://www.w3.org/2000/01/rdf-schema#");

        public static readonly string Prefix = "rdfs";
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Fn
    {
        /// <summary>
        /// http://www.w3.org/2005/xpath-functions#
        /// </summary>
        public static readonly Namespace ns = Namespace.FromUri("http://www.w3.org/2005/xpath-functions#");

        public static readonly string Prefix = "fn";
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Sfn
    {
        /// <summary>
        /// http://www.w3.org/ns/sparql#
        /// </summary>
        public static readonly Namespace ns = Namespace.FromUri("http://www.w3.org/ns/sparql#");

        /// <summary>
        /// Default prefix
        /// </summary>
        public static readonly string Prefix = "sfn";
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Foaf
    {
        /// <summary>
        /// http://xmlns.com/foaf/0.1/
        /// </summary>
        public static readonly Namespace ns = Namespace.FromUri("http://xmlns.com/foaf/0.1/");

        /// <summary>
        /// Default prefix
        /// </summary>
        public static readonly string Prefix = "foaf";

        /* FOAF Core */

        /// <summary>
        /// http://xmlns.com/foaf/0.1/Agent
        /// </summary>
        public static readonly Iri Agent = _(ns["Agent"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/Person
        /// </summary>
        public static readonly Iri Person = _(ns["Person"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/name
        /// </summary>
        public static readonly Iri name = _(ns["name"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/title
        /// </summary>
        public static readonly Iri title = _(ns["title"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/img
        /// </summary>
        public static readonly Iri img = _(ns["img"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/Organization
        /// </summary>
        public static readonly Iri Organization = _(ns["Organization"]);

        /* Social Web */

        /// <summary>
        /// http://xmlns.com/foaf/0.1/nick
        /// </summary>
        public static readonly Iri nick = _(ns["nick"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/mbox
        /// </summary>
        public static readonly Iri mbox = _(ns["mbox"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/homepage
        /// </summary>
        public static readonly Iri homepage = _(ns["homepage"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/weblog
        /// </summary>
        public static readonly Iri weblog = _(ns["weblog"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/openid
        /// </summary>
        public static readonly Iri openid = _(ns["openid"]);

        /// <summary>
        /// http://xmlns.com/foaf/0.1/jabberID
        /// </summary>
        public static readonly Iri jabberID = _(ns["jabberID"]);
    }

    private static Iri _(string resource)
        { return (Iri)Resource.Parse(resource, Namespaces); }
}
