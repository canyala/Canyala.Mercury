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
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury.Core;
using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Serialization;

namespace Canyala.Mercury.Test.All;

[TestClass]
public class QueryGroupByTest
{
    [TestMethod]
    public void QueryAggregateShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9 .
                :book2 :price 5 .
                :auth2 :writesBook :book3 .
                :book3 :price 7 .
                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4 .
                :book4 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT (SUM(?lprice) AS ?totalPrice)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
                GROUP BY ?org
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);

        var expected = new string[,]
        {
            { "totalPrice" },
            { Literal.From(21) },
            { Literal.From(7) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateExpressionShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 8 .
                :book2 :price 5 .
                :auth2 :writesBook :book3 .
                :book3 :price 7 .
                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4 .
                :book4 :price 6 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT ( (MIN(?lprice)+MAX(?lprice))/2 AS ?averagePrice)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
                GROUP BY ?org
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);

        var expected = new string[,]
        {
            { "averagePrice" },
            { Literal.From(6.5m) },
            { Literal.From(6m) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateWithHavingShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9 .
                :book2 :price 5 .
                :auth2 :writesBook :book3 .
                :book3 :price 7 .
                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4 .
                :book4 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT (SUM(?lprice) AS ?totalPrice)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
                GROUP BY ?org
                HAVING (SUM(?lprice) > 10)
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);

        var expected = new string[,]
        {
            { "totalPrice" },
            { Literal.From(21) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateWithDistinctShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9 .
                :book2 :price 5 .
                :auth2 :writesBook :book3, :book5 .
                :book3 :price 7 .
                :book5 :price 5 .

                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4, :book6  .
                :book4 :price 7 .
                :book6 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT (SUM(?lprice) AS ?totalPrice) (SUM(DISTINCT ?lprice) as ?distinctPrice)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
                GROUP BY ?org
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);

        var expected = new string[,]
        {
            { "totalPrice", "distinctPrice" },
            { Literal.From(26), Literal.From(21) },
            { Literal.From(14), Literal.From(7) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateWithCountAllShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9 .
                :book2 :price 5 .
                :auth2 :writesBook :book3, :book5 .
                :book3 :price 7 .
                :book5 :price 5 .

                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4, :book6  .
                :book4 :price 7 .
                :book6 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT (Count(*) AS ?count)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);

        var expected = new string[,]
        {
            { "count" },
            { Literal.From(6) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateWithCountAllDistinctShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9, 6, 6 .
                :book2 :price 5, 5 .
                :auth2 :writesBook :book3, :book5 .
                :book3 :price 7, 6 .
                :book5 :price 5 .

                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4, :book6  .
                :book4 :price 7, 3, 3 .
                :book6 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT (Count(DISTINCT *) AS ?count)
                WHERE {
                  
                  ?book :price ?lprice .
                }
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);

        var expected = new string[,]
        {
            { "count" },
            { Literal.From(9) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateWithCountExpressionShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9 .
                :book2 :price 5 .
                :auth2 :writesBook :book3, :book5 .
                :book3 :price 7 .
                :book5 :price 5 .

                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4, :book6  .
                :book4 :price 7 .
                :book6 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT (Count(?lprice) AS ?count)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);

        var expected = new string[,]
        {
            { "count" },
            { Literal.From(6) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateWithCountDistinctExpressionShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9 .
                :book2 :price 5 .
                :auth2 :writesBook :book3, :book5 .
                :book3 :price 7 .
                :book5 :price 5 .

                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4, :book6  .
                :book4 :price 7 .
                :book6 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT (Count(DISTINCT ?lprice) AS ?count)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);

        var expected = new string[,]
        {
            { "count" },
            { Literal.From(3) }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateWithGroupConcatShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9 .
                :book2 :price 5 .
                :auth2 :writesBook :book3, :book5 .
                :book3 :price 7 .
                :book5 :price 5 .

                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4, :book6  .
                :book4 :price 7 .
                :book6 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT ?org (GROUP_CONCAT(?lprice) AS ?prices)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
                GROUP BY ?org
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);
        var ns = Namespace.FromUri("http://books.example/");

        var expected = new string[,]
        {
            { "org", "prices" },
            { ns["org1"], "\"9 5 7 5\"" },
            { ns["org2"], "\"7 7\"" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }

    [TestMethod]
    public void QueryAggregateWithGroupConcatAndSeparatorShouldWork()
    {
        var data = Turtle.FromText(@"

                @prefix : <http://books.example/> .

                :org1 :affiliates :auth1, :auth2 .
                :auth1 :writesBook :book1, :book2 .
                :book1 :price 9 .
                :book2 :price 5 .
                :auth2 :writesBook :book3, :book5 .
                :book3 :price 7 .
                :book5 :price 5 .

                :org2 :affiliates :auth3 .
                :auth3 :writesBook :book4, :book6  .
                :book4 :price 7 .
                :book6 :price 7 .

            ");

        var sparql = @"

                PREFIX : <http://books.example/>
                SELECT ?org (GROUP_CONCAT(?lprice; SEPARATOR = "";"") AS ?prices)
                WHERE {
                  ?org :affiliates ?auth .
                  ?auth :writesBook ?book .
                  ?book :price ?lprice .
                }
                GROUP BY ?org
            ";

        var dataset = Graph.Create(data);
        var actual = Sparql.Query(dataset, sparql);
        var ns = Namespace.FromUri("http://books.example/");

        var expected = new string[,]
        {
            { "org", "prices" },
            { ns["org1"], "\"9;5;7;5\"" },
            { ns["org2"], "\"7;7\"" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }
}
