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
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;

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
public class RdfTest
{
    static string[][] example = new string[][] 
    {
            Seq.Array("<Subject1>", "<Predicate1>", "<Object1>"),
            Seq.Array("<Subject1>", "<Predicate1>", "<Object2>"),
            Seq.Array("<Subject1>", "<Predicate1>", "<Object3>"),
            Seq.Array("<Subject2>", "<Predicate1>", "<Object1>"),
            Seq.Array("<Subject2>", "<Predicate1>", "<Object2>"),
            Seq.Array("<Subject2>", "<Predicate1>", "<Object3>"),
            Seq.Array("<Subject2>", "<Predicate2>", "<Object1>"),
            Seq.Array("<Subject2>", "<Predicate2>", "<Object2>"),
            Seq.Array("<Subject2>", "<Predicate2>", "<Object3>"),
            Seq.Array("<Subject3>", "<Predicate1>", "<Object1>"),
            Seq.Array("<Subject3>", "<Predicate1>", "<Object2>"),
            Seq.Array("<Subject3>", "<Predicate1>", "<Object3>"),
    };

    [TestMethod]
    public void AsTurtlesAndAsTriplesShouldWork()
    {
        var turtles = example.AsTurtles().ToList();
        var triples = turtles.AsTriples().ToList();

        Assert.AreEqual(triples.SelectMany(item => item).Join(';'), example.SelectMany(item => item).Join(';'));
    }

    [TestMethod]
    public void AsTurtleShouldWork()
    {
        foreach (var line in example.AsTurtle()) 
            Trace.WriteLine(line);
    }

    static Namespace exmpl = Namespace.FromUri("http://www.example.org/-rdf-schema#");
    static Namespace rdf = Namespace.FromUri("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
    static Namespace rdfs = Namespace.FromUri("http://www.w3.org/2000/01-rdf-schema#");
    static Namespace xsd = Namespace.FromUri("http://www.w3.org/2001/XMLSchema#");
    static Namespace owl = Namespace.FromUri("http://www.w3.org/2002/07/owl#");

    static string owlClass = owl["Class"];
    static string rdfType = rdf["type"];

    static string[,] ontology =
    {
        { exmpl["Sensor"], rdfType, owlClass },
        { exmpl["Perspective"], rdfType, owlClass }
    };

    [TestMethod]
    public void TurtleGenerationTest()
    {
        var namespaces = new Namespaces {
            { "owl", owl },
            { "rdf", rdf },
            { "rdfs", rdfs },
            { "xsd", xsd  },
            { "exmpl", exmpl }
        };

        var graph = Graph.Create(true, ontology.AsRows());
        foreach (var turtleLine in graph.AsTurtle(namespaces)) 
            Trace.WriteLine(turtleLine);
    }

    [TestMethod]
    public void FundDataImportShouldWork()
    {
        var fundDataTriples = Csv
            .FromLines(File.ReadLines(Context.TestFile("fonder.csv")), ';')
            .Select(row => row.Select(1, 0, 2))
            .Select(row => row.Select(column => column.Trim()))
            .Select(row => row.ToArray());

        var graph = Graph.Create(true, fundDataTriples);
        var trend = graph.Enumerate("Carnegie Ryssland", null, null);
    }
}
