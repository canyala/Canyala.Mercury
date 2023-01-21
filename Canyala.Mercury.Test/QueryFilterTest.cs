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

using Canyala.Lagoon.Collections;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;
using Canyala.Lagoon.Serialization;
using Canyala.Lagoon.Text;

using Canyala.Mercury;
using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Canyala.Mercury.Test;

[TestClass]
public class QueryFilterTest
{
    [TestMethod]
    public void QueryFilterShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32 .

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51 .

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29 .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?age
                WHERE {
                   ?person foaf:givenName ?name .
                   ?person foaf:age ?age . 
                   Filter (?age > 29)
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "age" },
            { "\"Alice\"", Literal.From(32) },
            { "\"Bob\"", Literal.From(51) },
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryFilterAddShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32 .

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51 .

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29 .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?age
                WHERE {
                   ?person foaf:givenName ?name .
                   ?person foaf:age ?age . 
                   Filter (?age - 30 > 0 && ?age / 2 < 25)
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "age" },
            { "\"Alice\"", Literal.From(32) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryMultipleFilterShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32 .

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51 .

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29 .

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?age
                WHERE {
                    ?person foaf:givenName ?name .
                    ?person foaf:age ?age . 
                    Filter (?age - 30 > 0)
                    Filter (?age / 2 < 25)
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "age" },
            { "\"Alice\"", Literal.From(32) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryComplexFilterOrWithShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32;
                        :score 4.75.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51;
                        :score 3.32.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29;
                        :score 6.84.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?score
                WHERE {
                   ?person  foaf:givenName ?name;
                            foaf:age ?age;
                            :score ?score . 
                   Filter (?age * ?score + 25 >= 200 || ?score <= 4)
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "score" },
            { "\"Bob\"", Literal.From(3.32M) },
            { "\"Carol\"", Literal.From(6.84M) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryComplexFilterEqualsNotEqualsWithShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32;
                        :score 4.75.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51;
                        :score 3.32.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29;
                        :score 6.84.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?score
                WHERE {
                   ?person  foaf:givenName ?name;
                            foaf:age ?age;
                            :score ?score . 
                   Filter (?age != 32 && (?score = 6.84 || ?score = 3.32))
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "score" },
            { "\"Bob\"", Literal.From(3.32M) },
            { "\"Carol\"", Literal.From(6.84M) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryComplexFilterNotAndNegateShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32;
                        :score 4.75.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51;
                        :score 3.32.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29;
                        :score 6.84.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?score
                WHERE {
                   ?person  foaf:givenName ?name;
                            foaf:age ?age;
                            :score ?score . 
                   Filter (!(?age = 32) && ?score > -(-6))
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "score" },
            { "\"Carol\"", Literal.From(6.84M) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryComplexFilterInShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32;
                        :score 4.75.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51;
                        :score 3.32.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29;
                        :score 6.84.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?score
                WHERE {
                   ?person  foaf:givenName ?name;
                            foaf:age ?age;
                            :score ?score . 
                   Filter (?age IN (14, 18, 29, 32))
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "score" },
            { "\"Alice\"", Literal.From(4.75M) },
            { "\"Carol\"", Literal.From(6.84M) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryComplexFilterNotInShouldWork()
    {
        var turtleData = Turtle.FromText(@"

                @prefix :       <http://example/> .
                @prefix foaf:   <http://xmlns.com/foaf/0.1/> .

                :alice  foaf:givenName ""Alice"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 32;
                        :score 4.75.

                :bob    foaf:givenName ""Bob"" ;
                        foaf:familyName ""Jones"" ;
                        foaf:age 51;
                        :score 3.32.

                :carol  foaf:givenName ""Carol"" ;
                        foaf:familyName ""Smith"" ;
                        foaf:age 29;
                        :score 6.84.

            ");

        var graph = Graph.Create(turtleData);

        var sparqlQuery = @"

                PREFIX :       <http://example/>
                PREFIX foaf:   <http://xmlns.com/foaf/0.1/>

                SELECT ?name ?score
                WHERE {
                   ?person  foaf:givenName ?name;
                            foaf:age ?age;
                            :score ?score . 
                   Filter (?age NOT IN (14, 18, 3 * 17))
                }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "name", "score" },
            { "\"Alice\"", Literal.From(4.75M) },
            { "\"Carol\"", Literal.From(6.84M) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }
}
