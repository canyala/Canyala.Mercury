/*

  MIT License
 
  Copyright (c) 2011-2023 Canyala Innovation (Martin Fredriksson)

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
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Core.Collections;
using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;
using Canyala.Lagoon.Core.Serialization;
using Canyala.Lagoon.Core.Text;

using Canyala.Mercury.Core;
using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Serialization;

using Canyala.Test.Tools;

namespace Canyala.Mercury.Test.All;

[TestClass]
public class QueryTest
{
    [TestMethod]
    public void QueryBase()
    {
        // TODO: Implement test for BASE directive
        /*
        var query = new Query();
        _ = Sparql.Translate("BASE <http://canyala.se/sparql#test1>", query, out _);
        Assert.AreEqual("http://canyala.se/sparql#test1", query.Plan.Namespaces.Base);
        */
    }

    /// <summary>
    /// Written 2013-04-28 07.23
    /// </summary>
    [TestMethod]
    public void QuerySimpleSparqlShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                <http://example.org/book/book1> <http://purl.org/dc/elements/1.1/title> ""SPARQL Tutorial"" .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                SELECT ?title
                WHERE
                {
                  <http://example.org/book/book1> <http://purl.org/dc/elements/1.1/title> ?title .
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "title" },
            { @"""SPARQL Tutorial""" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMultipleMatchesSparqlShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix foaf:  <http://xmlns.com/foaf/0.1/> .

                _:a  foaf:name   ""Johnny Lee Outlaw"" .
                _:a  foaf:mbox   <mailto:jlow@example.com> .
                _:b  foaf:name   ""Peter Goodguy"" .
                _:b  foaf:mbox   <mailto:peter@example.org> .
                _:c  foaf:mbox   <mailto:carol@example.org> .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>
                select ?name ?mbox
                where
                {   ?x foaf:name ?name .
                    ?x foaf:mbox ?mbox }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "mbox" },
            { "\"Johnny Lee Outlaw\"", "<mailto:jlow@example.com>" },
            { "\"Peter Goodguy\"", "<mailto:peter@example.org>" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMultipleMatchesUsingGroupsSparqlShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix foaf:  <http://xmlns.com/foaf/0.1/> .

                _:a  foaf:name   ""Johnny Lee Outlaw"" .
                _:a  foaf:mbox   <mailto:jlow@example.com> .
                _:b  foaf:name   ""Peter Goodguy"" .
                _:b  foaf:mbox   <mailto:peter@example.org> .
                _:c  foaf:mbox   <mailto:carol@example.org> .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>
                select ?name ?mbox
                where
                {   
                    { ?x foaf:name ?name . }
                    { ?x foaf:mbox ?mbox . }
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "mbox" },
            { "\"Johnny Lee Outlaw\"", "<mailto:jlow@example.com>" },
            { "\"Peter Goodguy\"", "<mailto:peter@example.org>" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMatchingRdfLiteralsSparqlShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix dt:   <http://example.org/datatype#> .
                @prefix ns:   <http://example.org/ns#> .
                @prefix :     <http://example.org/ns#> .
                @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .

                :x   ns:p     ""cat""@en .
                :y   ns:p     ""42""^^xsd:integer .
                :z   ns:p     ""abc""^^dt:specialDatatype .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery1 = @"SELECT ?v WHERE { ?v ?p ""cat"" }";
        var actual1 = Sparql.Query(graph, sparqlQuery1);
        var expected1 = new string[,] { { "v" } }.AsRows();
        Assert.AreEqual(expected1.AsCsvText(), actual1.AsCsvText());

        var sparqlQuery2 = @"SELECT ?v WHERE { ?v ?p ""cat""@en }";
        var actual2 = Sparql.Query(graph, sparqlQuery2);
        var expected2 = new string[,] { { "v" }, { "<http://example.org/ns#x>" } }.AsRows();
        Assert.AreEqual(expected2.AsCsvText(), actual2.AsCsvText());

    }

    [TestMethod]
    public void QueryMatchingIntegersSparqlShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix dt:   <http://example.org/datatype#> .
                @prefix ns:   <http://example.org/ns#> .
                @prefix :     <http://example.org/ns#> .
                @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .

                :x   ns:p     ""cat""@en .
                :y   ns:p     ""42""^^xsd:integer .
                :z   ns:p     ""abc""^^dt:specialDatatype .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                SELECT ?v WHERE { ?v ?p 42 }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,] { { "v" }, { "<http://example.org/ns#y>" } }.AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMatchingArbitraryDatatypeSparqlShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix dt:   <http://example.org/datatype#> .
                @prefix ns:   <http://example.org/ns#> .
                @prefix :     <http://example.org/ns#> .
                @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .

                :x   ns:p     ""cat""@en .
                :y   ns:p     ""42""^^xsd:integer .
                :z   ns:p     ""abc""^^dt:specialDatatype .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                SELECT ?v WHERE { ?v ?p ""abc""^^<http://example.org/datatype#specialDatatype> }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,] { { "v" }, { "<http://example.org/ns#z>" } }.AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryBlankNodeLabelsInQueryResultsShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix foaf:  <http://xmlns.com/foaf/0.1/> .

                _:a  foaf:name   ""Alice"" .
                _:d  foaf:name   ""Bob"" .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>
                SELECT ?x ?name
                WHERE  { ?x foaf:name ?name }
          ";

        var actual = Sparql.Query(graph, sparqlQuery).ToArray();

        Assert.AreNotEqual("_:a", actual[1][0]);
        Assert.AreNotEqual("_:d", actual[2][0]); 
    }

    [TestMethod]
    public void QueryCreatingValuesWithExpressionsShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix foaf:  <http://xmlns.com/foaf/0.1/> .
          
                _:a  foaf:givenName   ""John"" .
                _:a  foaf:surname  ""Doe"" .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>
                SELECT ( CONCAT(?G, "" "", ?S) AS ?name )
                WHERE  { ?P foaf:givenName ?G ; foaf:surname ?S }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name" },
            { "\"John Doe\"" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryEmptyGroupPatternShouldWork()
    {
        var graph = Graph.Create();

        var sparqlQuery = @"

                SELECT ?x
                WHERE
                {
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery).ToArray();
    }

    [TestMethod]
    public void QueryOptionalPatternMatchingSparqlShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix foaf:       <http://xmlns.com/foaf/0.1/> .
                @prefix rdf:        <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .

                _:a  rdf:type        foaf:Person .
                _:a  foaf:name       ""Alice"" .
                _:a  foaf:mbox       <mailto:alice@example.com> .
                _:a  foaf:mbox       <mailto:alice@work.example> .

                _:b  rdf:type        foaf:Person .
                _:b  foaf:name       ""Bob"" .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                SELECT ?name ?mbox
                WHERE  { ?x foaf:name  ?name .
                         OPTIONAL { ?x  foaf:mbox  ?mbox }
                       }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "mbox" },
            { "\"Alice\"", "<mailto:alice@example.com>" },
            { "\"Alice\"", "<mailto:alice@work.example>" },
            { "\"Bob\"", "" },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMultipleOptionalGraphPatternsShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix foaf:       <http://xmlns.com/foaf/0.1/> .

                _:a  foaf:name       ""Alice"" .
                _:a  foaf:homepage   <http://work.example.org/alice/> .

                _:b  foaf:name       ""Bob"" .
                _:b  foaf:mbox       <mailto:bob@work.example> .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                SELECT ?name ?mbox ?hpage
                WHERE  { ?x foaf:name  ?name .
                         OPTIONAL { ?x foaf:mbox ?mbox } .
                         OPTIONAL { ?x foaf:homepage ?hpage }
                       }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "mbox", "hpage" },
            { "\"Alice\"", "", "<http://work.example.org/alice/>"  },
            { "\"Bob\"", "<mailto:bob@work.example>", ""  },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMatchingAlternativesShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix dc10:  <http://purl.org/dc/elements/1.0/> .
                @prefix dc11:  <http://purl.org/dc/elements/1.1/> .

                _:a  dc10:title     ""SPARQL Query Language Tutorial"" .
                _:a  dc10:creator   ""Alice"" .

                _:b  dc11:title     ""SPARQL Protocol Tutorial"" .
                _:b  dc11:creator   ""Bob"" .

                _:c  dc10:title     ""SPARQL"" .
                _:c  dc11:title     ""SPARQL (updated)"" .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX dc10:  <http://purl.org/dc/elements/1.0/>
                PREFIX dc11:  <http://purl.org/dc/elements/1.1/>

                SELECT ?title
                WHERE  { { ?book dc10:title  ?title } UNION { ?book dc11:title  ?title } }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "title" },
            { "\"SPARQL Query Language Tutorial\"" },
            { "\"SPARQL\"" },
            { "\"SPARQL (updated)\"" },
            { "\"SPARQL Protocol Tutorial\"" },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMatchingMultipleAlternativesShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix dc10:  <http://purl.org/dc/elements/1.0/> .
                @prefix dc11:  <http://purl.org/dc/elements/1.1/> .

                _:a  dc10:title     ""SPARQL Query Language Tutorial"" .
                _:a  dc10:creator   ""Alice"" .

                _:b  dc11:title     ""SPARQL Protocol Tutorial"" .
                _:b  dc11:creator   ""Bob"" .

                _:c  dc10:title     ""SPARQL"" .
                _:c  dc11:title     ""SPARQL (updated)"" .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX dc10:  <http://purl.org/dc/elements/1.0/>
                PREFIX dc11:  <http://purl.org/dc/elements/1.1/>

                SELECT ?x ?y
                WHERE  { { ?book dc10:title ?x } UNION { ?book dc11:title  ?y } }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "x", "y" },
            { "\"SPARQL Query Language Tutorial\"", "" },
            { "\"SPARQL\"", "" },
            { "", "\"SPARQL (updated)\"" },
            { "", "\"SPARQL Protocol Tutorial\"" },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMatchingCombinedAlternativesShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix dc10:  <http://purl.org/dc/elements/1.0/> .
                @prefix dc11:  <http://purl.org/dc/elements/1.1/> .

                _:a  dc10:title     ""SPARQL Query Language Tutorial"" .
                _:a  dc10:creator   ""Alice"" .

                _:b  dc11:title     ""SPARQL Protocol Tutorial"" .
                _:b  dc11:creator   ""Bob"" .

                _:c  dc10:title     ""SPARQL"" .
                _:c  dc11:title     ""SPARQL (updated)"" .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX dc10:  <http://purl.org/dc/elements/1.0/>
                PREFIX dc11:  <http://purl.org/dc/elements/1.1/>

                SELECT ?title ?author
                WHERE 
                { 
                    { ?book dc10:title ?title .  ?book dc10:creator ?author }
                    UNION
                    { ?book dc11:title ?title .  ?book dc11:creator ?author }
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "title", "author" },
            { "\"SPARQL Query Language Tutorial\"", "\"Alice\"" },
            { "\"SPARQL Protocol Tutorial\"", "\"Bob\"" },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryRemovingPossibleSolutionsShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" .

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" .

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT DISTINCT ?s
                WHERE {
                   ?s ?p ?o .
                   MINUS {
                      ?s foaf:givenName ""Bob"" .
                   }
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "s" },
            { "<http://example/alice>" },
            { "<http://example/carol>" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QuerySelectAsVariableShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        :points 12.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        :points 9.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:nick   ""cc"";
                        :points 17.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name (?points * 2 As ?double)
                WHERE {
                    ?person foaf:givenName ?name .
                    ?person :points ?points .
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "double" },
            { "\"Alice\"", Literal.From(24) },
            { "\"Bob\"", Literal.From(18) },
            { "\"Carol\"", Literal.From(34) },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryBindAsVariableShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        :points 12.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        :points 9.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:nick   ""cc"";
                        :points 17.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?double
                WHERE {
                    ?person foaf:givenName ?name .
                    ?person :points ?points .
                    BIND (?points * 2 AS ?double)
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "double" },
            { "\"Alice\"", Literal.From(24) },
            { "\"Bob\"", Literal.From(18) },
            { "\"Carol\"", Literal.From(34) },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryBindAndSelectAsVariableShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        :points 12.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        :points 9.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:nick   ""cc"";
                        :points 17.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name (?double * 2 AS ?doubledouble)
                WHERE {
                    ?person foaf:givenName ?name .
                    ?person :points ?points .
                    BIND (?points * 2 AS ?double)
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "doubledouble" },
            { "\"Alice\"", Literal.From(48) },
            { "\"Bob\"", Literal.From(36) },
            { "\"Carol\"", Literal.From(68) },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryBindTerminatesGroupAsVariableShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        :points 12;
                        :grade 2.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        :points 9;
                        :grade 1.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:nick   ""cc"";
                        :points 17;
                        :grade 4.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name (?double * 2 AS ?doubledouble) ?halfgrade
                WHERE {
                    ?person foaf:givenName ?name .
                    ?person :points ?points .
                    BIND (?points * 2 AS ?double)
                    ?person :grade ?grade .
                    BIND (?grade / 2 AS ?halfgrade)
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "doubledouble", "halfgrade" },
            { "\"Alice\"", Literal.From(48), Literal.From(1M) },
            { "\"Bob\"", Literal.From(36), Literal.From(0.5M) },
            { "\"Carol\"", Literal.From(68), Literal.From(2M) },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QuerySelectWithValuesShouldWork()
    {
        var turtleData = Turtle.FromText(@"
                @prefix dc:   <http://purl.org/dc/elements/1.1/> .
                @prefix :     <http://example.org/book/> .
                @prefix ns:   <http://example.org/ns#> .

                :book1  dc:title  ""SPARQL Tutorial"" .
                :book1  ns:price  42 .
                :book2  dc:title  ""The Semantic Web"" .
                :book2  ns:price  23 .
            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX dc:   <http://purl.org/dc/elements/1.1/> 
                PREFIX :     <http://example.org/book/> 
                PREFIX ns:   <http://example.org/ns#> 

                SELECT ?book ?title ?price
                {
                   VALUES ?book { :book1 :book3 }
                   ?book dc:title ?title ;
                         ns:price ?price .
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "book", "title", "price" },
            { "<http://example.org/book/book1>", "\"SPARQL Tutorial\"", Literal.From(42) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QuerySelectAllShouldWork()
    {
        var turtleData = Turtle.FromText(@"
                @prefix dc:   <http://purl.org/dc/elements/1.1/> .
                @prefix :     <http://example.org/book/> .
                @prefix ns:   <http://example.org/ns#> .

                :book1  dc:title  ""SPARQL Tutorial"" .
                :book1  ns:price  42 .
                :book2  dc:title  ""The Semantic Web"" .
                :book2  ns:price  23 .
            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX dc:   <http://purl.org/dc/elements/1.1/> 
                PREFIX :     <http://example.org/book/> 
                PREFIX ns:   <http://example.org/ns#> 

                SELECT *
                {
                   VALUES ?book { :book1 :book3 }
                   
                    ?book dc:title ?title .
                         
                    {
                        SELECT ?book
                        {
                            ?book ns:price ?price .
                        }
                    }
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "book", "title" },
            { "<http://example.org/book/book1>", "\"SPARQL Tutorial\"" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QuerySelectWithValuesUndefShouldWork()
    {
        var turtleData = Turtle.FromText(@"
                @prefix dc:   <http://purl.org/dc/elements/1.1/> .
                @prefix :     <http://example.org/book/> .
                @prefix ns:   <http://example.org/ns#> .

                :book1  dc:title  ""SPARQL Tutorial"" .
                :book1  ns:price  42 .
                :book2  dc:title  ""The Semantic Web"" .
                :book2  ns:price  23 .
            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX dc:   <http://purl.org/dc/elements/1.1/> 
                PREFIX :     <http://example.org/book/> 
                PREFIX ns:   <http://example.org/ns#> 

                SELECT ?book ?title ?price
                {
                   ?book dc:title ?title ;
                         ns:price ?price .
                   VALUES (?book ?title)
                   { (UNDEF ""SPARQL Tutorial"")
                     (:book2 UNDEF)
                   }
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "book", "title", "price" },
            { "<http://example.org/book/book1>", "\"SPARQL Tutorial\"", Literal.From(42) },
            { "<http://example.org/book/book2>", "\"The Semantic Web\"", Literal.From(23) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryConstructShouldWork()
    {
        var turtleData = Turtle.FromText(@"
                @prefix  foaf:  <http://xmlns.com/foaf/0.1/> .

                _:a    foaf:givenname   ""Alice"" .
                _:a    foaf:family_name ""Hacker"" .

                _:b    foaf:firstname   ""Bob"" .
                _:b    foaf:surname     ""Hacker"" .
            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX foaf:    <http://xmlns.com/foaf/0.1/>
                PREFIX vcard:   <http://www.w3.org/2001/vcard-rdf/3.0#>

                CONSTRUCT { ?x  vcard:N _:v .
                            _:v vcard:givenName ?gname .
                            _:v vcard:familyName ?fname }
                WHERE
                 {
                    { ?x foaf:firstname ?gname } UNION  { ?x foaf:givenname   ?gname } .
                    { ?x foaf:surname   ?fname } UNION  { ?x foaf:family_name ?fname } .
                 }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "s", "p", "o" },
        }
        .AsRows();

        Assert.AreEqual(7, actual.Count());
    }

    [TestMethod]
    public void QueryAskShouldWork()
    {
        var turtleData = Turtle.FromText(@"
                @prefix  foaf:  <http://xmlns.com/foaf/0.1/> .

                _:a    foaf:givenname   ""Alice"" .
                _:a    foaf:family_name ""Hacker"" .

                _:b    foaf:firstname   ""Bob"" .
                _:b    foaf:surname     ""Hacker"" .
            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX foaf:    <http://xmlns.com/foaf/0.1/>
                PREFIX vcard:   <http://www.w3.org/2001/vcard-rdf/3.0#>

                ASK 
                {
                    { ?x foaf:firstname ?y }
                    UNION
                    { ?x foaf:givenname ?y }
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "ask" },
            { Literal.From(true) },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryFilterExistsShouldWork()
    {
        var turtleData = Turtle.FromText(@"
                @prefix  :       <http://example/> .
                @prefix  rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
                @prefix  foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  rdf:type   foaf:Person .
                :alice  foaf:name  ""Alice"" .
                :bob    rdf:type   foaf:Person .
            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX  rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
                PREFIX  foaf:   <http://xmlns.com/foaf/0.1/> 

                SELECT ?person
                WHERE 
                {
                    ?person rdf:type  foaf:Person .
                    FILTER NOT EXISTS { ?person foaf:name ?name }
                }  
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "person" },
            { "<http://example/bob>" },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryLimitAndOffsetShouldWork()
    {
        var turtleData = Turtle.FromText(@"
                @prefix  :       <http://example/> .

                :alice  :hasBook ""Foundation"", ""I Robot"", ""Catcher in the rye"", ""Semantic web for the working ontoligist"", ""Programming the semantic web"".
                
            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX  :    <http://example/> 

                SELECT ?book
                WHERE 
                {
                    ?person :hasBook  ?book .
                }  
                ORDER BY ?book
                LIMIT 2
                OFFSET 2
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "book" },
            { "\"I Robot\"" },
            { "\"Programming the semantic web\"" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

}
