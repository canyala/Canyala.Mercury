//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.IO;
using System.Linq;
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
    public class TurtleTest
    {
        [TestMethod]
        public void TurtleSocialTTLShouldWork()
        {
            var socialTurtles = Turtle.FromLines(File.ReadLines(Context.TestFile("social.ttl")));
            var graph = Graph.Create(socialTurtles.AsTriples());
        }

        [TestMethod]
        public void TurtleManifestTTLShouldWork()
        {
            var manifest = Turtle.FromLines(File.ReadLines(Context.TestFile("manifest.ttl")));

            int numberOfTriples = manifest.Count();

            Assert.AreEqual(25, numberOfTriples);
        }

        [TestMethod]
        public void TurtlePrefixesAndDataShouldWork()
        {
            var turtle = @" 

            @prefix :       <http://www.example.orgs/shema#> .
            @prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
            @prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> . 
            @prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> .

            <> rdf:type mf:Manifest ;
               rdfs:label ""REDUCED"" ;
               :data ""Old"" ,
                     ""New"" .

            ";

            var result = Turtle.FromLines(Seq.Of(turtle));

            Assert.AreEqual(4, result.Count());
        }

        [TestMethod]
        public void TurtlePrefixesAndDataShouldWorkUsingA()
        {
            var turtle = @" 

            @prefix :       <http://www.example.orgs/shema#> .
            @prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
            @prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> . 
            @prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> .

            <> a mf:Manifest ;
               rdfs:label ""REDUCED"" ;
               :data ""Old"" ,
                     ""New"" .

            ";

            var result = Turtle.FromText(turtle);

            Assert.AreEqual(4, result.Count());
        }

        [TestMethod]
        public void TurtleDataShouldExpand()
        {
            var turtle = @" 

            @prefix :       <http://www.example.orgs/shema#> .
            @prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
            @prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> . 
            @prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> .

            <> rdf:type mf:Manifest ;
               rdfs:label ""REDUCED"" ;
               :data ""Old"" ,
                     ""New"" .

            ";

            var result = Turtle.FromText(turtle).ToArray();

            CollectionAssert.AreEquivalent(Seq.Array("<>", "<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>", "<http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#Manifest>"), result[0]);
            CollectionAssert.AreEquivalent(Seq.Array("<>", "<http://www.w3.org/2000/01/rdf-schema#label>", "\"REDUCED\""), result[1]);
            CollectionAssert.AreEquivalent(Seq.Array("<>", "<http://www.example.orgs/shema#data>", "\"Old\""), result[2]);
            CollectionAssert.AreEquivalent(Seq.Array("<>", "<http://www.example.orgs/shema#data>", "\"New\""), result[3]);
        }

        [TestMethod]
        public void TurtleManifestObjectListShouldWork()
        {
            var turtle = @" 

            @prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
            @prefix :       <http://www.w3.org/2001/sw/DataAccess/tests/data-r2/reduced/manifest#> .
            @prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> .
            @prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> .

            <>  rdf:type mf:Manifest ;
                rdfs:label 'REDUCED' ;
                mf:entries
                ( 
                  :reduced-1
                  :reduced-2
                ) .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(7, result.Count());
        }

        [TestMethod]
        public void TurtleBasicPredicateObjectListShouldWork()
        {
            var turtle = 

            @"<s1> <p1> <o1> ;
                 <p2>
                    [ <pl1> <ol1> ;
                      <pl2> <ol2> ] ;
                 <p3> <o2> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleBasicPredicateObjectListWithQuotedStringShouldWork()
        {
            var turtle =

            @"<s1> <p1> <o1> ;
                 <p2>
                    [ <pl1> ""Hello"" ;
                      <pl2> <ol2> ] ;
                 <p3> <o2> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleWithEmptyLongQuotedStringShouldWork()
        {
            var turtle =

            @"<s1> <p1> """""""""""" .";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public void TurtleWithLongQuotedStringShouldWork()
        {
            var turtle =

            @"<s1> <p1> """"""Test"""""" .";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public void TurtleWithLongQuotedStringAndLineBreaksShouldWork()
        {
            var turtle =

            @"<s1> <p1> """"""

            Test

            """""" .";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public void TurtleWithLongQuotedStringAndLineBreaksAndCommentCharsShouldWork()
        {
            var turtle =

            @"<s1> <p1> """"""

            #Test

            """""" .";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.IsTrue(result[0][2].Contains("#Test"));
        }

        [TestMethod]
        public void TurtleBasicPredicateObjectListWithSingleQuotedStringShouldWork()
        {
            var turtle =

            @"<s1> <p1> <o1> ;
                 <p2>
                    [ <pl1> 'Hello' ;
                      <pl2> <ol2> ] ;
                 <p3> <o2> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleWithEmptyLongSingleQuotedStringShouldWork()
        {
            var turtle =

            @"<s1> <p1> '''''' .";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public void TurtleWithLongSingleQuotedStringShouldWork()
        {
            var turtle =

            @"<s1> <p1> '''Test''' .";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public void TurtleWithLongSingleQuotedStringAndLineBreaksShouldWork()
        {
            var turtle =

            @"<s1> <p1> '''

            Test

            ''' .";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public void TurtleWithLongSingleQuotedStringAndLineBreaksAndCommentCharsShouldWork()
        {
            var turtle =

            @"<s1> <p1> '''

            #Test

            ''' .";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.IsTrue(result[0][2].Contains("#Test"));
        }

        [TestMethod]
        public void TurtleBasicPredicateObjectListWithCommentShouldWork()
        {
            var turtle =

            @"<s1> <p1> <o1> ;
                 #<pX> <pY> ;
                 <p2>
                    [ <pl1> <ol1> ;
                      <pl2> <ol2> ] ;
                 <p3> <o2> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleBasicPredicateObjectListWithCommentAndCommentCharInQuotesShouldWork()
        {
            var turtle =

            @"<s1> <p1> <o1> ;
                 #<pX> <pY> ;
                 <p2>
                    [ <pl1> ""this is # not a comment"" ;
                      <pl2> <ol2> ] ;
                 <p3> <o2> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleBasicPredicateObjectListWithCommentAndCommentCharInLongQuotesShouldWork()
        {
            var turtle =

            @"<s1> <p1> <o1> ;
                 #<pX> <pY> ;
                 <p2>
                    [ <pl1> """"""this is ""not"" # a comment"""""" ;
                      <pl2> <ol2> ] ;
                 <p3> <o2> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleBasicPredicateObjectListWithCommentAndCommentCharInSingleQuotesShouldWork()
        {
            var turtle =

            @"<s1> <p1> <o1> ;
                 #<pX> <pY> ;
                 <p2>
                    [ <pl1> 'this is # not a comment' ;
                      <pl2> <ol2> ] ;
                 <p3> <o2> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleBasicPredicateObjectListWithCommentAndCommentCharInLongSingleQuotesShouldWork()
        {
            var turtle =

            @"<s1> <p1> <o1> ;
                 #<pX> <pY> ;
                 <p2>
                    [ <pl1> '''""this"" 'is' # 'not' a comment''' ;
                      <pl2> <ol2> ] ;
                 <p3> <o2> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleManifestSmallPredicateObjectListShouldWork()
        {
            var turtle = @" 

            @prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
            @prefix :       <http://www.w3.org/2001/sw/DataAccess/tests/data-r2/reduced/manifest#> .
            @prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> .
            @prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> .
            @prefix qt:     <http://www.w3.org/2001/sw/DataAccess/tests/test-query#> .
            @prefix dawgt:  <http://www.w3.org/2001/sw/DataAccess/tests/test-dawg#> .

            :reduced-1 rdf:type mf:QueryEvaluationTest ;
                #dawgt:approval dawgt:NotApproved ;
    
                mf:action
                        [ qt:query <reduced-1.rq> ;
                          qt:data <reduced-star.ttl> ] ;

                mf:result <reduced-1.srx> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(5, result.Count());
        }

        [TestMethod]
        public void TurtleManifestPredicateObjectListShouldWork()
        {
            var turtle = @" 

            @prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
            @prefix :       <http://www.w3.org/2001/sw/DataAccess/tests/data-r2/reduced/manifest#> .
            @prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> .
            @prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> .
            @prefix qt:     <http://www.w3.org/2001/sw/DataAccess/tests/test-query#> .
            @prefix dawgt:  <http://www.w3.org/2001/sw/DataAccess/tests/test-dawg#> .

            :reduced-1 rdf:type mf:QueryEvaluationTest ;
                mf:resultCardinality mf:LaxCardinality ;
                mf:name    'SELECT REDUCED *' ;
                dawgt:approval dawgt:Approved ;
                dawgt:approvedBy <http://lists.w3.org/Archives/Public/public-rdf-dawg/2007OctDec/att-0069/13-dawg-minutes.html> ;
                #dawgt:approval dawgt:NotApproved ;
    
                mf:action
                        [ qt:query <reduced-1.rq> ;
                          qt:data <reduced-star.ttl> ] ;
                mf:result <reduced-1.srx> .
            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(9, result.Count());
        }

        [TestMethod]
        public void TurtlePredicateListInPredicateListAsObjectShouldWork()
        {
            var turtle = @"

            <id> <family> 
            [ 
                <husband> <martin> ;
                <wife> <monika> ;
                <children> 
                [ 
                    <boy> <marcus> ; 
                    <boy> <pontus> ; 
                    <boy> <daniel> 
                ] ;
             ] .

            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(7, result.Count());
        }

        [TestMethod]
        public void TurtlePredicateListAsSubjectShouldWork()
        {
            var turtle = "[ <plp1> <plo1> ; <plp2> <plo2> ] <p> <o> .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public void TurtleEmptyObjectListShouldWork()
        {
            var turtle = @"

                <s> <p> () .

            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(Ontologies.Rdf.nil, result[0][2]);
        }

        [TestMethod]
        public void TurtleObjectListShouldWork()
        {
            var turtle = @"

                <s> <p> ( <o1> <o2> <o3> ) .

            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(7, result.Count());
        }

        [TestMethod]
        public void TurtleObjectListInObjectListShouldWork()
        {
            var turtle = @"

                <s> <p> ( <o1> ( <o2.1> <o2.2> ) <o3> ) .

            ";

            var result = Turtle.FromText(turtle).ToArray();

            Assert.AreEqual(11, result.Count());
        }

        [TestMethod]
        public void TurtleObjectListInPredicateListShouldWork()
        {
            var turtle = "<s> <p> [ <pl1> ( <o2.1> <o2.2> ) ] .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual(6, result.Count());
        }

        [TestMethod]
        public void TurtlePredicateListInObjectListShouldWork()
        {
            var turtle = "<s1> <p2> ( <ol1> [ <plp1> <plo1> ; <plp2> <plo2> ]  <ol3> ) .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual(9, result.Count());
        }

        [TestMethod]
        public void TurtleAnonymousSubjectShouldWork()
        {
            var turtle = "[  ] a <anonymous> .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual("_:", result[0][0].Substring(0, 2));
        }

        [TestMethod]
        public void TurtleAnonymousObjectShouldWork()
        {
            var turtle = "<anonymous> a [] .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual("_:", result[0][2].Substring(0, 2));
        }

        [TestMethod]
        public void TurtleNilObjectListShouldWork()
        {
            var turtle = "<s> <p> () .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual(Ontologies.Rdf.nil, result[0][2]);
        }

        [TestMethod]
        public void TurtleIntegersShouldWork()
        {
            var turtle = "<s> <p> 1 .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual("\"1\"^^<http://www.w3.org/2001/XMLSchema#integer>", result[0][2]);
        }

        [TestMethod]
        public void TurtleDecimalsShouldWork()
        {
            var turtle = "<s> <p> 1.2 .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual("\"1.2\"^^<http://www.w3.org/2001/XMLSchema#decimal>", result[0][2]);
        }

        [TestMethod]
        public void TurtleDoublesShouldWork()
        {
            var turtle = "<s> <p> 1.2e3 .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual("\"1.2e3\"^^<http://www.w3.org/2001/XMLSchema#double>", result[0][2]);
        }

        [TestMethod]
        public void TurtleBooleansShouldWork()
        {
            var turtle = "<s> <p> true .";
            var result = Turtle.FromText(turtle).ToArray();
            Assert.AreEqual("\"true\"^^<http://www.w3.org/2001/XMLSchema#boolean>", result[0][2]);
        }
    }
}
