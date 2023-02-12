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
using System.Text;
using System.Collections.Generic;
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
using Canyala.Mercury.Storage;

using Environment = Canyala.Mercury.Storage.Environment;

namespace Canyala.Mercury.Test.All;

public static class RulesCreatedForTestingPurposes
{
    public static void ObjectsAreIndexedRule(this Graph graph, string[] triple, string index)
    {
        if (triple.Subject() == index)
            return;

        graph.Retract(index, null, triple.Subject());

        Analyzer
            .Words(triple.Object())
            .SelectMany(word => Analyzer.Permutations(word))
            .Do(permutation => graph.Assert(index, permutation, triple.Subject()));
    }

    public static void PredicateIsSingletonRule(this Graph graph, string[] triple, string predicate)
    {
        if (triple.Predicate() == predicate)
            graph.Retract(triple.Subject(), triple.Predicate(), null);
    }

    public static void PredicateIsMutualRule(this Graph graph, string predicate)
    {
        var results = graph
            .Enumerate(null, predicate, null)
            .Where(result => !graph.IsTrue(result[1], predicate, result[0])).ToList();

        graph
            .Assert(results.Select(result => Seq.Array(result[1], predicate, result[0])));
    }
}

[TestClass]
public class GraphTest
{
    [TestMethod]
    public void VerySimpleTestWithUsing()
    {
        var example = new string[,] 
        {
            { "a", "1", "x" }
        };

        var x = string.Empty;

        using (var environment = Storage.Environment.Create())
        using (var graph = Graph.Create(environment, example.AsTurtles()))
        {
            foreach (var result in graph.Enumerate(null, null, null))
            {
                x += result.Join(';');
            }
        }

        Assert.AreEqual("a;1;x", x);
    }

    [TestMethod]
    public void VerySimpleTestWithoutUsing()
    {
        var example = new string[,] 
        {
            { "a", "1", "x" }
        };

        var x = string.Empty;

        var environment = Storage.Environment.Create();
        var graph = Graph.Create(environment, example.AsTurtles());

        foreach (var result in graph.Enumerate(null, null, null))
        {
            x += result.Join(';');
        }

        Assert.AreEqual("a;1;x", x);
    }

    [TestMethod]
    public void ImplicitRulesTestLarge()
    {
        using (var environment = Storage.Environment.Create())
        using (var testGraph = Graph.Create(environment))
        {
            testGraph.Infer((graph, fact) => graph.PredicateIsSingletonRule(fact, "Heading"));
            testGraph.Infer((graph, fact) => graph.ObjectsAreIndexedRule(fact, "FullTextIndex"));

            testGraph.Assert("/Packages/TheFirst", "Heading", "Hello there my friend!");
            testGraph.Assert("/Packages/TheSecond", "Paragraph1", "We are not who you are looking for.");
            testGraph.Assert("/Packages/TheSecond", "Paragraph2", "Let us on our way.");

            var countWithFr = testGraph.Enumerate("FullTextIndex", "fr", null).Count();

            testGraph.Assert("/Packages/TheFirst", "Heading", "Hello there!");

            var countWithoutFr = testGraph.Enumerate("FullTextIndex", "fr", null).Count();

            Assert.AreEqual(1, countWithFr - countWithoutFr);
        }
    }

    [TestMethod]
    public void ImplicitRulesTestMedium()
    {
        var environment = Storage.Environment.Create();
        var testGraph = Graph.Create(environment);

        testGraph.Infer((graph, fact) => graph.PredicateIsSingletonRule(fact, "Heading"));
        testGraph.Infer((graph, fact) => graph.ObjectsAreIndexedRule(fact, "FullTextIndex"));

        testGraph.Assert("/Packages/TheFirst", "Heading", "Hi");
        testGraph.Assert("/Packages/TheSecond", "Paragraph1", "Who are you?");
        testGraph.Assert("/Packages/TheSecond", "Paragraph2", "Goodbye.");

        var countWithFr = testGraph.Enumerate("FullTextIndex", "i", null).Count();

        testGraph.Assert("/Packages/TheFirst", "Heading", "Thanks!");

        var countWithoutFr = testGraph.Enumerate("FullTextIndex", "i", null).Count();

        Assert.AreEqual(1, countWithFr - countWithoutFr);
    }

    [TestMethod]
    public void ImplicitRulesTestSmallReduced()
    {
        var environment = Storage.Environment.Create();
        var testGraph = Graph.Create(environment);

        testGraph.Infer((graph, fact) => graph.ObjectsAreIndexedRule(fact, "FullTextIndex"));

        testGraph.Assert("1", "a", "ab");

        var state1 = testGraph.Enumerate(null, null, null).ToArray();

        testGraph.Assert("1", "a", "ab");

        var state2 = testGraph.Enumerate(null, null, null).ToArray();
    }

    [TestMethod]
    public void ImplicitRulesFullyReduced()
    {
        var environment = Storage.Environment.Create();

        var testGraph = Graph.Create(environment);
        testGraph.Assert("FullTextIndex", "ab", "1");
        testGraph.Assert("FullTextIndex", "b", "1");
        testGraph.Assert("1", "a", "ab");
        testGraph.Retract("FullTextIndex", null, "1");
        testGraph.Assert("FullTextIndex", "ab", "1");
        testGraph.Assert("FullTextIndex", "a", "1");
    }

    [TestMethod]
    public void ExplicitRulesCorrectCountTest()
    {
        var graph = Graph.Create(Storage.Environment.Create());

        graph.Assert("Anakin", "likes", "Amidala");
        graph.Assert("Amidala", "likes", "Anakin");
        graph.Assert("Martin", "likes", "Monika");

        Assert.AreEqual(4, graph.Infer(g => g.PredicateIsMutualRule("likes")).Count());
    }

    [TestMethod]
    public void ExplicitRulesExpectedFactTest()
    {
        var graph = Graph.Create(Storage.Environment.Create());

        graph.Assert("Anakin", "likes", "Amidala");
        graph.Assert("Amidala", "likes", "Anakin");
        graph.Assert("Martin", "likes", "Monika");

        graph.Infer(g => g.PredicateIsMutualRule("likes"));

        Assert.AreEqual(true, graph.IsTrue("Monika", "likes", "Martin"));
    }

    [TestMethod]
    public void ExplicitRuleCallExpectedFactTest()
    {
        var graph = Graph.Create(Storage.Environment.Create());

        graph.Assert("Anakin", "likes", "Amidala");
        graph.Assert("Amidala", "likes", "Anakin");
        graph.Assert("Martin", "likes", "Monika");

        graph.PredicateIsMutualRule("likes");

        Assert.AreEqual(true, graph.IsTrue("Monika", "likes", "Martin"));
    }

    static string[] example =
    {
        "luke,brother,leia",
        "leia,sister,luke",
        "amidala,mother,leia",
        "amidala,mother,luke",
        "anakin,father,luke",
        "anakin,father,leia"
    };

    [TestMethod]
    public void GraphsSholdBeCreateable()
    {
        Assert
            .AreEqual(
                6,
                Graph.Create(Storage.Environment.Create(), Csv.FromLines(example)).Count()
             );
    }

    [TestMethod]
    public void SetExtractionShouldWork()
    {
        var example = new string[,]
        {
            { "sausage", "is", "meat" },
            { "icecream", "is", "derry" },
            { "apple", "is", "fruit" },
            { "orange", "is", "fruit" },
            { "fruit", "taste", "sweet" },
            { "derry", "taste", "sweet" }
        };

        var graph = Graph.Create(Storage.Environment.Create(), example.AsTurtles());

        var solution = graph.Enumerate(null, "taste", "sweet");

        var results = graph.Enumerate(null, "is", solution[0]);

        var output = results.Select(row => row.Join('+')).OrderBy(row => row).Join(';');

        Assert.AreEqual("apple+fruit;icecream+derry;orange+fruit", output); 
    }

    [TestMethod]
    public void GraphsShouldAllowDeleteByObject()
    {
        Assert
            .AreEqual(
                3,
                Graph.Create(Storage.Environment.Create(), Csv.FromLines(example)).Retract(null, null, "leia").Count()
             );
    }

    [TestMethod]
    public void GraphsShouldAllowDeleteByPredicate()
    {
        Assert
            .AreEqual(
                4,
                Graph.Create(Storage.Environment.Create(), Csv.FromLines(example)).Retract(null, "mother", null).Count()
             );
    }

    [TestMethod]
    public void GraphsShouldAllowDeleteBySubject()
    {
        Assert
            .AreEqual(
                4,
                Graph.Create(Storage.Environment.Create(), Csv.FromLines(example)).Retract("amidala", null, null).Count()
             );
    }

    [TestMethod]
    public void GraphsShouldBeQueryable()
    {
        Assert
            .AreEqual(
                "leia",
                Graph.Create(Storage.Environment.Create(), Csv.FromLines(example)).Enumerate(null, "sister", "luke").Single()[0]
             );
    }

    [TestMethod]
    public void GraphsShouldBeSearchable()
    {
        Assert
            .AreEqual(
                2,
                Graph.Create(Storage.Environment.Create(), Csv.FromLines(example)).Enumerate("amidala", "mother", null).Count()
             );
    }

    [TestMethod]
    public void GraphsShouldProvideMultipleResults()
    {
        Assert
            .AreEqual(
                "leia,luke",
                Graph.Create(Storage.Environment.Create(), Csv.FromLines(example)).Enumerate("amidala", "mother", null)
                .Select(t => t.Object()).OrderBy(s => s).Join(',')
             );
    }

    static Namespace exmpl = Namespace.FromUri("http://www.example.org/-rdf-schema#");
    static Namespace rdf = Namespace.FromUri("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
    static Namespace rdfs = Namespace.FromUri("http://www.w3.org/2000/01-rdf-schema#");
    static Namespace xsd = Namespace.FromUri("http://www.w3.org/2001/XMLSchema#");
    static Namespace owl = Namespace.FromUri("http://www.w3.org/2002/07/owl#");

    static string owlClass = owl["Class"];
    static string rdfType = rdf["type"];

    static IEnumerable<string[]> ontology = new string[,]
    {
        { exmpl["Sensor"], rdfType, owlClass },
        { exmpl["Perspective"], rdfType, owlClass }
    }
    .AsRows();

    [TestMethod]
    public void ConvertingToNTriplesShouldWork()
    {
        var graph = Graph.Create(Storage.Environment.Create(), ontology);
        var result = graph.AsNTriples();
    }

    [TestMethod]
    public void TriplesShouldAlsoBeEnumerable()
    {
        Assert.AreEqual(2, Graph.Create(Storage.Environment.Create(), ontology).Enumerate(null, rdfType, null).Count());
    }

    [TestMethod]
    public void TriplesBySubjectAndObjectShouldHaveCorrectPredicate()
    {
        Assert.AreEqual(rdfType, Graph.Create(Storage.Environment.Create(), ontology).Enumerate(exmpl["Sensor"], null, owlClass).Single()[0]);
    }

    [TestMethod]
    public void TriplesBySubjectAndPredicateShouldHaveCorrectObject()
    {
        Assert.AreEqual(owlClass, Graph.Create(Storage.Environment.Create(), ontology).Enumerate(exmpl["Sensor"], rdfType, null).Single()[0]);
    }


    private static IEnumerable<string[]> GenerateTriples(DateTime from, DateTime to)
    {
        int value = 0;

        for (DateTime current = from; current <= to; current += TimeSpan.FromDays(1))
        {
            yield return new string[] 
            { 
                "SEB TrendSafe Fund",
                "{0}-{1:00}-{2:00}".Args(current.Year, current.Month, current.Day),
                "{0}".Args(value++)
            };
        }
    }

    [TestMethod]
    public void RangedEnumerationsShouldWorkInAHeap()
    {
        var environment = Storage.Environment.Create();
        var graph = Graph.Create(environment, GenerateTriples(new DateTime(2011,1,1), new DateTime(2013,1,19)));
        var actual = graph.Enumerate("SEB TrendSafe Fund", Constraint.Between("2012-08-08", "2012-08-16"), null);
        Assert.AreEqual(9, actual.Count());
    }

    [TestMethod]
    public void RangedEnumerationsShouldWorkInMemory()
    {
        var graph = Graph.Create(GenerateTriples(new DateTime(2011, 1, 1), new DateTime(2013, 1, 19)));
        var actual = graph.Enumerate("SEB TrendSafe Fund", Constraint.Between("2012-08-08", "2012-08-16"), null);
        Assert.AreEqual(9, actual.Count());
    }
}
