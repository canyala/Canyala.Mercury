//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Canyala.Lagoon.Collections;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;
using Canyala.Lagoon.Serialization;
using Canyala.Lagoon.Text;

using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Serialization;

using Canyala.Test.Tools;

namespace Canyala.Mercury.Test
{
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

            var graph = Graph.Create(turtleData);

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
}
