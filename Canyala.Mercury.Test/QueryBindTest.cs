﻿/*

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
public class QueryBindTest
{
    [TestMethod]
    public void TestBindToTIMEZONE()
    {
        var turtleData = Turtle.FromText(@"
                @prefix :  <http://canyala.se/testing> .
                @prefix foaf:  <http://xmlns.com/foaf/0.1/> .
                @prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .

                _:a  foaf:givenName   ""John"" .
                _:a  foaf:surname  ""Doe"" .
                _:a  :time ""2011-01-10T14:45:13.815-05:00""^^xsd:dateTime .

            ");

        var graph = Graph.Create(true, turtleData);

        var sparqlQuery = @"
                prefix :  <http://canyala.se/testing>
                prefix foaf:   <http://xmlns.com/foaf/0.1/>
                prefix xsd:  <http://www.w3.org/2001/XMLSchema#>

                select ( timezone(?time) AS ?tz )
                { ?x :time ?time }
            ";

        var actual = Sparql.Query(graph, sparqlQuery);

        var expected = new string[,]
        {
            { "tz" },
            { "\"-PT5H\"^^<http://www.w3.org/2001/XMLSchema#dayTimeDuration>" }
        }
        .AsRows();

        Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
    }
}
