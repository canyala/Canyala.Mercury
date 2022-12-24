//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Serialization;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class QueryOrderByTest
    {
        static IEnumerable<string[]> triples = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32 ;
                        :empId 1 .

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51 ;
                        :empId 2 .

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29 ;
                        :empId 3 .

            ");

        [TestMethod]
        public void TestOrderByOneVar()
        {
            var sparql = @"

                PREFIX foaf:    <http://xmlns.com/foaf/0.1/>

                SELECT ?name
                WHERE { ?x foaf:givenName ?name }
                ORDER BY ?name

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);

            var expected = new string[,]
            {
                { "name" },
                { "\"Alice\"" },
                { "\"Bob\"" },
                { "\"Carol\"" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByOneVarAscending()
        {
            var sparql = @"

                PREFIX foaf:    <http://xmlns.com/foaf/0.1/>

                SELECT ?name
                WHERE { ?x foaf:givenName ?name }
                ORDER BY ASC(?name)

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);

            var expected = new string[,]
            {
                { "name" },
                { "\"Alice\"" },
                { "\"Bob\"" },
                { "\"Carol\"" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByOneVarDescending()
        {
            var sparql = @"

                PREFIX foaf:    <http://xmlns.com/foaf/0.1/>

                SELECT ?name
                WHERE { ?x foaf:givenName ?name }
                ORDER BY DESC(?name)

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);

            var expected = new string[,]
            {
                { "name" },
                { "\"Carol\"" },
                { "\"Bob\"" },
                { "\"Alice\"" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByOneBind()
        {
            var sparql = @"

                PREFIX foaf:    <http://xmlns.com/foaf/0.1/>

                SELECT ?name
                WHERE { ?x foaf:givenName ?name }
                ORDER BY SUBSTR(?name, 1)

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);

            var expected = new string[,]
            {
                { "name" },
                { "\"Carol\"" },
                { "\"Alice\"" },
                { "\"Bob\"" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByOneBindAsc()
        {
            var sparql = @"

                PREFIX foaf:    <http://xmlns.com/foaf/0.1/>

                SELECT ?name
                WHERE { ?x foaf:givenName ?name }
                ORDER BY ASC(SUBSTR(?name, 1))

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);

            var expected = new string[,]
            {
                { "name" },
                { "\"Carol\"" },
                { "\"Alice\"" },
                { "\"Bob\"" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByOneBindDesc()
        {
            var sparql = @"

                PREFIX foaf:    <http://xmlns.com/foaf/0.1/>

                SELECT ?name
                WHERE { ?x foaf:givenName ?name }
                ORDER BY DESC(SUBSTR(?name, 1))

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);

            var expected = new string[,]
            {
                { "name" },
                { "\"Bob\"" },
                { "\"Alice\"" },
                { "\"Carol\"" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByTwoAscAsc()
        {
            var triples = Turtle.FromText(@"

            @prefix :        <http://example/> .
            @prefix letter:  <http://example/letter#> .

            :data letter:F 1 ;
                  letter:Z 8 ;
                  letter:X 2 ;
                  letter:D 3 ;
                  letter:L 4 ;
                  letter:X 3 ;
                  letter:L 5 ;
                  letter:Z 9 .

            ");

            var sparql = @"

                PREFIX :       <http://example/>
                PREFIX letter: <http://example/letter#>

                SELECT ?letter ?number
                WHERE { :data ?letter ?number }
                ORDER BY ?letter ?number

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);
            var letter = Namespace.FromUri("http://example/letter#");

            var expected = new string[,]
            {
                { "letter", "number" },
                { letter["D"], Literal.From(3) },
                { letter["F"], Literal.From(1) },
                { letter["L"], Literal.From(4) },
                { letter["L"], Literal.From(5) },
                { letter["X"], Literal.From(2) },
                { letter["X"], Literal.From(3) },
                { letter["Z"], Literal.From(8) },
                { letter["Z"], Literal.From(9) },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByTwoAscDesc()
        {
            var triples = Turtle.FromText(@"

            @prefix :        <http://example/> .
            @prefix letter:  <http://example/letter#> .

            :data letter:F 1 ;
                  letter:Z 8 ;
                  letter:X 2 ;
                  letter:D 3 ;
                  letter:L 4 ;
                  letter:X 3 ;
                  letter:L 5 ;
                  letter:Z 9 .

            ");

            var sparql = @"

                PREFIX :       <http://example/>
                PREFIX letter: <http://example/letter#>

                SELECT ?letter ?number
                WHERE { :data ?letter ?number }
                ORDER BY ?letter DESC(?number)

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);
            var letter = Namespace.FromUri("http://example/letter#");

            var expected = new string[,]
            {
                { "letter", "number" },
                { letter["D"], Literal.From(3) },
                { letter["F"], Literal.From(1) },
                { letter["L"], Literal.From(5) },
                { letter["L"], Literal.From(4) },
                { letter["X"], Literal.From(3) },
                { letter["X"], Literal.From(2) },
                { letter["Z"], Literal.From(9) },
                { letter["Z"], Literal.From(8) },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByTwoAscDescDesc()
        {
            var triples = Turtle.FromText(@"

            @prefix :        <http://example/> .
            @prefix letter:  <http://example/letter#> .

            :data letter:F 1 ;
                  letter:Z 8 ;
                  letter:X 2 ;
                  letter:D 3 ;
                  letter:L 4 ;
                  letter:X 3 ;
                  letter:L 5 ;
                  letter:Z 9 .

            ");

            var sparql = @"

                PREFIX :       <http://example/>
                PREFIX letter: <http://example/letter#>

                SELECT ?letter ?number
                WHERE { :data ?letter ?number }
                ORDER BY DESC(?letter) DESC(?number)

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);
            var letter = Namespace.FromUri("http://example/letter#");

            var expected = new string[,]
            {
                { "letter", "number" },
                { letter["Z"], Literal.From(9) },
                { letter["Z"], Literal.From(8) },
                { letter["X"], Literal.From(3) },
                { letter["X"], Literal.From(2) },
                { letter["L"], Literal.From(5) },
                { letter["L"], Literal.From(4) },
                { letter["F"], Literal.From(1) },
                { letter["D"], Literal.From(3) },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TestOrderByTwoDescAsc()
        {
            var triples = Turtle.FromText(@"

            @prefix :        <http://example/> .
            @prefix letter:  <http://example/letter#> .

            :data letter:F 1 ;
                  letter:Z 8 ;
                  letter:X 2 ;
                  letter:D 3 ;
                  letter:L 4 ;
                  letter:X 3 ;
                  letter:L 5 ;
                  letter:Z 9 .

            ");

            var sparql = @"

                PREFIX :       <http://example/>
                PREFIX letter: <http://example/letter#>

                SELECT ?letter ?number
                WHERE { :data ?letter ?number }
                ORDER BY DESC(?letter) ASC(?number)

            ";

            var dataset = Graph.Create(triples);
            var actual = Sparql.Query(dataset, sparql);
            var letter = Namespace.FromUri("http://example/letter#");

            var expected = new string[,]
            {
                { "letter", "number" },
                { letter["Z"], Literal.From(8) },
                { letter["Z"], Literal.From(9) },
                { letter["X"], Literal.From(2) },
                { letter["X"], Literal.From(3) },
                { letter["L"], Literal.From(4) },
                { letter["L"], Literal.From(5) },
                { letter["F"], Literal.From(1) },
                { letter["D"], Literal.From(3) },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

    }
}
