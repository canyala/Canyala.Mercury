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
using Canyala.Mercury.Rdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Mercury.Core;

namespace Canyala.Mercury.Test.All;

[TestClass]
public class TermTest
{
    [TestMethod]
    public void TestStringLiteralNoLang()
    {
        Literal literal = (Literal)Term.Parse("'foobar'", Ontologies.Namespaces);

        Assert.AreEqual("\"foobar\"", literal.ToString());
        Assert.AreEqual("\"foobar\"", (string)literal);
        Assert.AreEqual("foobar", literal.Value);
        Assert.AreEqual("\"foobar\"", literal.Full);
        Assert.AreEqual("\"foobar\"", literal.Short);
    }

    [TestMethod]
    public void TestStringLiteralTyped()
    {
        Literal literal = (Literal)Term.Parse("'foobar'^^xsd:string" , Ontologies.Namespaces);

        Assert.AreEqual("\"foobar\"^^<http://www.w3.org/2001/XMLSchema#string>", literal.ToString());
        Assert.AreEqual("\"foobar\"^^<http://www.w3.org/2001/XMLSchema#string>", (string)literal);
        Assert.AreEqual("foobar", literal.Value);
        Assert.AreEqual("\"foobar\"^^<http://www.w3.org/2001/XMLSchema#string>", literal.Full);
        Assert.AreEqual("\"foobar\"^^xsd:string", literal.Short);
    }

    [TestMethod]
    public void TestStringLiteralWithLang()
    {
        Literal literal = (Literal)Term.Parse("\"foobar\"@en-US", Ontologies.Namespaces);

        Assert.AreEqual("\"foobar\"@en-US", literal.ToString());
        Assert.AreEqual("\"foobar\"@en-US", (string)literal);
        Assert.AreEqual("foobar", literal.Value);
        Assert.AreEqual("\"foobar\"@en-US", literal.Full);
        Assert.AreEqual("\"foobar\"@en-US", literal.Short);
        Assert.AreEqual("en-US", literal.Language);
    }

    [TestMethod]
    public void TestStringLiteralWithLangAndEscapedChars()
    {
        Literal literal = (Literal)Term.Parse("\"\\t \\n \\r \\b \\f \\\" \\' \\\\ \"@en", Ontologies.Namespaces);

        Assert.AreEqual("\t \n \r \b \f \" ' \\ ", literal.Value);
        Assert.AreEqual("\"\\t \\n \\r \\b \\f \\\" \\' \\\\ \"@en", literal.Full);
        Assert.AreEqual("en", literal.Language);
    }

    [TestMethod]
    public void TestStringLiteralWithLangAndEscapedHexChars()
    {
        string expected = new String(new[] { 'L', '\u0061', '\u0308', 's', 'k' });
        Literal literal = (Literal)Term.Parse("\"L\\u0061\\u0308sk\"@en", Ontologies.Namespaces);
        Literal literal2 = (Literal)Term.Parse("\"L\\U00000061\\U00000308sk\"@en", Ontologies.Namespaces);

        Assert.AreEqual(expected, literal.Value);
        Assert.AreEqual(expected, literal2.Value);
    }

    [TestMethod]
    public void TestVariable()
    {
        Variable literal1 = (Variable)Term.Parse("?foo", Ontologies.Namespaces);
        Variable literal2 = (Variable)Term.Parse("$foo", Ontologies.Namespaces);

        Assert.AreEqual("?foo", literal1.ToString());
        Assert.AreEqual("?foo", (string)literal1);

        Assert.AreEqual("?foo", literal2.ToString());
        Assert.AreEqual("?foo", (string)literal2);

        Assert.AreEqual(literal1, literal2);
    }
    
    [TestMethod]
    public void TestIriNotPrefixed()
    {
        Iri literal = (Iri)Term.Parse("<http://canyala.se/testing/test>", Ontologies.Namespaces);

        Assert.AreEqual("http://canyala.se/testing/test", literal.Value);
        Assert.AreEqual("<http://canyala.se/testing/test>", literal.ToString());
        Assert.AreEqual("<http://canyala.se/testing/test>", (string)literal);
        Assert.AreEqual("<http://canyala.se/testing/test>", literal.Full);
        Assert.AreEqual("<http://canyala.se/testing/test>", literal.Short);
    }

    [TestMethod]
    public void TestIriNotPrefixedBasedLong()
    {
        var ns = new Namespaces();
        ns.Base = "http://canyala.se/testing/";

        Iri literal = (Iri)Term.Parse("<http://canyala.se/testing/test>", ns);

        Assert.AreEqual("http://canyala.se/testing/test", literal.Value);
        Assert.AreEqual("<http://canyala.se/testing/test>", literal.ToString());
        Assert.AreEqual("<http://canyala.se/testing/test>", (string)literal);
        Assert.AreEqual("<http://canyala.se/testing/test>", literal.Full);
        Assert.AreEqual("<test>", literal.Short);
    }

    [TestMethod]
    public void TestIriNotPrefixedBasedLongRelative()
    {
        var ns = new Namespaces();
        ns.Base = "http://canyala.se/testing/mean/";

        Iri literal = (Iri)Term.Parse("<http://canyala.se/testing/test>", ns);

        Assert.AreEqual("http://canyala.se/testing/test", literal.Value);
        Assert.AreEqual("<http://canyala.se/testing/test>", literal.ToString());
        Assert.AreEqual("<http://canyala.se/testing/test>", (string)literal);
        Assert.AreEqual("<http://canyala.se/testing/test>", literal.Full);
        Assert.AreEqual("<../test>", literal.Short);
    }

    [TestMethod]
    public void TestIriPrefixed()
    {
        Iri literal = (Iri)Term.Parse("rdf:type", Ontologies.Namespaces);

        Assert.AreEqual("http://www.w3.org/1999/02/22-rdf-syntax-ns#type", literal.Value);
        Assert.AreEqual("<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>", literal.ToString());
        Assert.AreEqual("<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>", (string)literal);
        Assert.AreEqual("<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>", literal.Full);
        Assert.AreEqual("rdf:type", literal.Short);
    }

    [TestMethod]
    public void TestIriPrefixedEmptyPrefix()
    {
        var ns = new Namespaces { { "", "http://canyala.se/testing/" } };
        Iri literal = (Iri)Term.Parse(":gurka", ns);

        Assert.AreEqual("http://canyala.se/testing/gurka", literal.Value);
        Assert.AreEqual("<http://canyala.se/testing/gurka>", literal.ToString());
        Assert.AreEqual("<http://canyala.se/testing/gurka>", (string)literal);
        Assert.AreEqual("<http://canyala.se/testing/gurka>", literal.Full);
        Assert.AreEqual(":gurka", literal.Short);
    }

    [TestMethod]
    public void TestIriPrefixedLocalNameWithColon()
    {
        var ns = new Namespaces { { "test", "http://canyala.se/testing/" } };
        Iri literal = (Iri)Term.Parse("test:my:gnu", ns);

        Assert.AreEqual("http://canyala.se/testing/my:gnu", literal.Value);
        Assert.AreEqual("<http://canyala.se/testing/my:gnu>", literal.ToString());
        Assert.AreEqual("<http://canyala.se/testing/my:gnu>", (string)literal);
        Assert.AreEqual("<http://canyala.se/testing/my:gnu>", literal.Full);
        Assert.AreEqual("test:my:gnu", literal.Short);
    }

    [TestMethod]
    public void TestIriPrefixedLocalNameWithEscapedChars()
    {
        var ns = new Namespaces { { "test", "http://canyala.se/testing/" } };
        Iri literal = (Iri)Term.Parse("test:func\\(\\)", ns);

        Assert.AreEqual("http://canyala.se/testing/func()", literal.Value);
        Assert.AreEqual("<http://canyala.se/testing/func()>", literal.ToString());
        Assert.AreEqual("<http://canyala.se/testing/func()>", (string)literal);
        Assert.AreEqual("<http://canyala.se/testing/func()>", literal.Full);
        Assert.AreEqual("test:func\\(\\)", literal.Short);
    }

    [TestMethod]
    public void TestIriNotPrefixedBasedShort()
    {
        var ns = new Namespaces { { "rdf", Ontologies.Rdf.ns } };
        ns.Base = Namespace.FromUri("http://canyala.se/testing/");

        Iri literal = (Iri)Term.Parse("<gurka>", ns);

        Assert.AreEqual("http://canyala.se/testing/gurka", literal.Value);
        Assert.AreEqual("<http://canyala.se/testing/gurka>", literal.ToString());
        Assert.AreEqual("<http://canyala.se/testing/gurka>", (string)literal);
        Assert.AreEqual("<http://canyala.se/testing/gurka>", literal.Full);
        Assert.AreEqual("<gurka>", literal.Short);
    }
    
    [TestMethod]
    public void TestIriPrefixedNoPrefixWithColon()
    {
        var ns = new Namespaces { { "rdf", Ontologies.Rdf.ns } };
        ns.Base = Namespace.FromUri("http://canyala.se/testing/");

        Iri literal = (Iri)Term.Parse("<gu:rka>", ns);

        Assert.AreEqual("gu:rka", literal.Value);
        Assert.AreEqual("<gu:rka>", literal.ToString());
        Assert.AreEqual("<gu:rka>", (string)literal);
        Assert.AreEqual("<gu:rka>", literal.Full);
        Assert.AreEqual("<gu:rka>", literal.Short);
    }
    
    [TestMethod]
    public void TestBlank()
    {
        var ns = new Namespaces { { "rdf", Ontologies.Rdf.ns } };
        ns.Base = Namespace.FromUri("http://canyala.se/testing/");

        Blank literal = (Blank)Term.Parse("_:test", ns);

        Assert.AreEqual("_:test", literal.Value);
        Assert.AreEqual("_:test", literal.ToString());
        Assert.AreEqual("_:test", (string)literal);
        Assert.AreEqual("_:test", literal.Full);
        Assert.AreEqual("_:test", literal.Short);
    }
    
    [TestMethod]
    public void TestIntLiteral()
    {
        Literal literal1 = (Literal)Term.Parse("'''42'''^^xsd:integer", Ontologies.Namespaces);
        Literal literal2 = (Literal)Term.Parse("\"42\"^^" + Ontologies.Xsd.ns["integer"], Ontologies.Namespaces);

        Assert.AreEqual("\"42\"^^<http://www.w3.org/2001/XMLSchema#integer>", literal1.ToString());
        Assert.AreEqual("\"42\"^^<http://www.w3.org/2001/XMLSchema#integer>", (string)literal1);
        Assert.AreEqual("42", literal1.Value);
        Assert.AreEqual("\"42\"^^<http://www.w3.org/2001/XMLSchema#integer>", literal1.Full);
        Assert.AreEqual("\"42\"^^xsd:integer", literal1.Short);

        Assert.AreEqual(literal1, literal2);
        Assert.AreEqual(42, literal1.AsInt);
        Assert.AreEqual(literal1, Literal.From(42));
    }

    [TestMethod]
    public void TestLongLiteral()
    {
        Literal literal1 = (Literal)Term.Parse("'212568934563856'^^xsd:integer", Ontologies.Namespaces);
        Literal literal2 = (Literal)Term.Parse("\"212568934563856\"^^" + Ontologies.Xsd.ns["integer"], Ontologies.Namespaces);

        Assert.AreEqual("\"212568934563856\"^^<http://www.w3.org/2001/XMLSchema#integer>", literal1.ToString());
        Assert.AreEqual("\"212568934563856\"^^<http://www.w3.org/2001/XMLSchema#integer>", (string)literal1);
        Assert.AreEqual("212568934563856", literal1.Value);
        Assert.AreEqual("\"212568934563856\"^^<http://www.w3.org/2001/XMLSchema#integer>", literal1.Full);
        Assert.AreEqual("\"212568934563856\"^^xsd:integer", literal1.Short);

        Assert.AreEqual(literal1, literal2);
        Assert.AreEqual(212568934563856, literal1.AsLong);
        Assert.AreEqual(literal1, Literal.From(212568934563856));
    }

    [TestMethod]
    public void TestFloatLiteral()
    {
        Literal literal1 = (Literal)Term.Parse("'212.5689'^^xsd:float", Ontologies.Namespaces);
        Literal literal2 = (Literal)Term.Parse("\"212.5689\"^^" + Ontologies.Xsd.ns["float"], Ontologies.Namespaces);

        Assert.AreEqual("\"212.5689\"^^<http://www.w3.org/2001/XMLSchema#float>", literal1.ToString());
        Assert.AreEqual("\"212.5689\"^^<http://www.w3.org/2001/XMLSchema#float>", (string)literal1);
        Assert.AreEqual("212.5689", literal1.Value);
        Assert.AreEqual("\"212.5689\"^^<http://www.w3.org/2001/XMLSchema#float>", literal1.Full);
        Assert.AreEqual("\"212.5689\"^^xsd:float", literal1.Short);

        Assert.AreEqual(literal1, literal2);
        Assert.AreEqual(212.5689F, literal1.AsFloat);
        Assert.AreEqual(literal1, Literal.From(212.5689F));
    }

    [TestMethod]
    public void TestDecimalLiteral()
    {
        Literal literal1 = (Literal)Term.Parse("'2124545776.5689'^^xsd:decimal", Ontologies.Namespaces);
        Literal literal2 = (Literal)Term.Parse("\"2124545776.5689\"^^" + Ontologies.Xsd.ns["decimal"], Ontologies.Namespaces);

        Assert.AreEqual("\"2124545776.5689\"^^<http://www.w3.org/2001/XMLSchema#decimal>", literal1.ToString());
        Assert.AreEqual("\"2124545776.5689\"^^<http://www.w3.org/2001/XMLSchema#decimal>", (string)literal1);
        Assert.AreEqual("2124545776.5689", literal1.Value);
        Assert.AreEqual("\"2124545776.5689\"^^<http://www.w3.org/2001/XMLSchema#decimal>", literal1.Full);
        Assert.AreEqual("\"2124545776.5689\"^^xsd:decimal", literal1.Short);

        Assert.AreEqual(literal1, literal2);
        Assert.AreEqual(2124545776.5689D, literal1.AsDouble);
        Assert.AreEqual(literal1, Literal.From(2124545776.5689M));
    }

    [TestMethod]
    public void TestBoolLiteral()
    {
        Literal literal1 = (Literal)Term.Parse("'true'^^xsd:boolean", Ontologies.Namespaces);
        Literal literal2 = (Literal)Term.Parse("\"false\"^^" + Ontologies.Xsd.ns["boolean"], Ontologies.Namespaces);

        Assert.AreEqual("\"true\"^^<http://www.w3.org/2001/XMLSchema#boolean>", literal1.ToString());
        Assert.AreEqual("\"true\"^^<http://www.w3.org/2001/XMLSchema#boolean>", (string)literal1);
        Assert.AreEqual("true", literal1.Value);
        Assert.AreEqual("\"true\"^^<http://www.w3.org/2001/XMLSchema#boolean>", literal1.Full);
        Assert.AreEqual("\"true\"^^xsd:boolean", literal1.Short);

        Assert.AreEqual("\"false\"^^<http://www.w3.org/2001/XMLSchema#boolean>", literal2.ToString());
        Assert.AreEqual("\"false\"^^<http://www.w3.org/2001/XMLSchema#boolean>", (string)literal2);
        Assert.AreEqual("false", literal2.Value);
        Assert.AreEqual("\"false\"^^<http://www.w3.org/2001/XMLSchema#boolean>", literal2.Full);
        Assert.AreEqual("\"false\"^^xsd:boolean", literal2.Short);

        Assert.AreEqual(literal1.AsBool, !literal2.AsBool);
        Assert.AreEqual(literal1, Literal.From(true));
        Assert.AreEqual(literal2, Literal.From(false));
    }

    [TestMethod]
    public void TestDateTimeLiteral()
    {
        DateTimeOffset dt = new DateTimeOffset(2013, 4, 18, 16, 35, 29, TimeSpan.FromHours(1));
        Literal literal1 = (Literal)Term.Parse("'2013-04-18T16:35:29+01:00'^^xsd:dateTime", Ontologies.Namespaces);
        Literal literal2 = (Literal)Term.Parse("\"2013-04-18T15:35:29+00:00\"^^" + Ontologies.Xsd.ns["dateTime"], Ontologies.Namespaces);

        Assert.AreEqual("\"2013-04-18T16:35:29+01:00\"^^<http://www.w3.org/2001/XMLSchema#dateTime>", literal1.ToString());
        Assert.AreEqual("\"2013-04-18T16:35:29+01:00\"^^<http://www.w3.org/2001/XMLSchema#dateTime>", (string)literal1);
        Assert.AreEqual("2013-04-18T16:35:29+01:00", literal1.Value);
        Assert.AreEqual("\"2013-04-18T16:35:29+01:00\"^^<http://www.w3.org/2001/XMLSchema#dateTime>", literal1.Full);
        Assert.AreEqual("\"2013-04-18T16:35:29+01:00\"^^xsd:dateTime", literal1.Short);

        Assert.AreEqual(dt, literal1.AsDateTime);
        Assert.AreEqual(dt, literal2.AsDateTime);

        Assert.AreEqual("\"2013-04-18T16:35:29+01:00\"^^xsd:dateTime", Literal.From(dt).Short);
    }
}
