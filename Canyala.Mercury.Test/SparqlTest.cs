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
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Core.Collections;
using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;
using Canyala.Lagoon.Core.Serialization;
using Canyala.Lagoon.Core.Text;

using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Serialization;

using Canyala.Test.Tools;

namespace Canyala.Mercury.Test;

[TestClass]
public class SparqlTest
{
    [TestMethod]
    public void SelectWithWhereClause()
    {
        var sparql = @"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?foo $bar 
                WHERE { ?foo rdf:type $bar }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var vars = query.Plan.Groups[0].Variables;

        Assert.AreEqual("rdf", ns.Single().Prefix);
        Assert.AreEqual("http://www.w3.org/1999/02/22-rdf-syntax-ns#", ns.Single().Namespace);

        Assert.AreEqual(2, vars.Count);
        Assert.IsTrue(vars.Contains("foo"));
        Assert.IsTrue(vars.Contains("bar"));

        var clauses = query.Plan.Groups[0].Groups[0].Clauses;
        CollectionAssert.AreEquivalent(AsTerms("?foo", "rdf:type", "?bar", ns), clauses[0]);
    }

    [TestMethod]
    public void SelectWithWhereClauseMultiClauses()
    {
        var sparql = @"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX : <http://canyala.se/Testing#>

                SELECT ?actress
                WHERE { 
                    :JamesDean :PlayedIn $movie . 
                    ?actress :PlayedIn $movie .
                    ?actress a :Woman .
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Groups.Count == 1);
        Assert.IsTrue(group.Clauses.Count == 0);
        Assert.IsTrue(group.Groups[0].Clauses.Count == 3);

        CollectionAssert.AreEquivalent(AsTerms(":JamesDean", ":PlayedIn", "$movie", ns), group.Groups[0].Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", ":PlayedIn", "$movie", ns), group.Groups[0].Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", Sparql.a, ":Woman", ns), group.Groups[0].Clauses[2]);

    }

    [TestMethod]
    public void SelectWithWhereClauseMultiTurtlePredicateList()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>

                SELECT ?actress
                WHERE { 
                    :JamesDean :PlayedIn $movie . 
                    ?actress :PlayedIn $movie ; 
                             a :Woman .
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 3);

        CollectionAssert.AreEquivalent(AsTerms(":JamesDean", ":PlayedIn", "$movie", ns), group.Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", ":PlayedIn", "$movie", ns), group.Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", Sparql.a, ":Woman", ns), group.Clauses[2]);
    }

    [TestMethod]
    public void SelectWithWhereClauseMultiTurtlePredicateAndObjectList()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                SELECT *
                WHERE { 
                    :JamesDean :PlayedIn ?q1 . 
                    ?actress :PlayedIn ?q1, ?q2; a :Woman .
                    ?q2 :directedBy :JohnFord .
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 5);

        CollectionAssert.AreEquivalent(AsTerms(":JamesDean", ":PlayedIn", "?q1", ns), group.Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", ":PlayedIn", "?q1", ns), group.Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", ":PlayedIn", "?q2", ns), group.Clauses[2]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", Sparql.a, ":Woman", ns), group.Clauses[3]);
        CollectionAssert.AreEquivalent(AsTerms("?q2", ":directedBy", ":JohnFord", ns), group.Clauses[4]);

    }

    [TestMethod]
    public void SelectWithWhereClauseSingleTurtlePredicateAndObjectBlankNode()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                SELECT ?actress
                WHERE { 
                    ?actress :PlayedIn [ :directedBy [ a :director; a :man ]], [:directedBy :JohnFord ] .
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 6);

        CollectionAssert.AreEquivalent(AsTerms("_:var1", Sparql.a, ":director", ns), group.Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("_:var1", Sparql.a, ":man", ns), group.Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", ":directedBy", "_:var1", ns), group.Clauses[2]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", ":PlayedIn", "_:var0", ns), group.Clauses[3]);
        CollectionAssert.AreEquivalent(AsTerms("_:var2", ":directedBy", ":JohnFord", ns), group.Clauses[4]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", ":PlayedIn", "_:var2", ns), group.Clauses[5]);
    }

    [TestMethod]
    public void SelectWithWhereClauseSingleTurtlePredicateAndSubjectBlankNodes()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                SELECT ?movie
                WHERE { 
                    [ :PlayedIn ?movie ] a :woman.
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 2);

        CollectionAssert.AreEquivalent(AsTerms("_:var0", ":PlayedIn", "?movie", ns), group.Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", Sparql.a, ":woman", ns), group.Clauses[1]);
    }

    [TestMethod]
    public void SelectWithWhereClauseMultiTurtlePredicateAndObjectBlankNodes()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                SELECT ?actress
                WHERE { 
                    :JamesDean :PlayedIn ?q1 . 
                    ?actress :PlayedIn ?q1 .
                    ?actress :PlayedIn [ :directedBy :JohnFord ] .
                    ?actress a :Woman .
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 5);

        CollectionAssert.AreEquivalent(AsTerms(":JamesDean", ":PlayedIn", "?q1", ns), group.Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", ":PlayedIn", "?q1", ns), group.Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", ":directedBy", ":JohnFord", ns), group.Clauses[2]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", ":PlayedIn", "_:var0", ns), group.Clauses[3]);
        CollectionAssert.AreEquivalent(AsTerms("?actress", Sparql.a, ":Woman", ns), group.Clauses[4]);

    }

    [TestMethod]
    public void SelectWithWhereClauseMultiTurtlePredicateAndSubjectBlankNodes()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                SELECT ?actress
                WHERE { 
                    [ :PlayedIn :Giant ; a :Woman ] :PlayedIn [ :directedBy :JohnFord ] .
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 4);

        CollectionAssert.AreEquivalent(AsTerms("_:var0", ":PlayedIn", ":Giant", ns), group.Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", Sparql.a, ":Woman", ns), group.Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("_:var1", ":directedBy", ":JohnFord" , ns), group.Clauses[2]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", ":PlayedIn", "_:var1", ns), group.Clauses[3]);
    }

    [TestMethod]
    public void SelectWithWhereClauseMultiTurtleListNil()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?person
                WHERE { 
                    ?person :ListValue () .
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 1);

        CollectionAssert.AreEquivalent(AsTerms("?person", ":ListValue", "rdf:nil", ns), group.Clauses[0]);
    }

    [TestMethod]
    public void SelectWithWhereClauseMultiTurtleListObject()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?person
                WHERE { 
                    ?person :ListValue (1 2) .
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 5);

        CollectionAssert.AreEquivalent(AsTerms("_:var0", "rdf:first", Literal.From(1), ns), group.Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", "rdf:rest", "_:var1", ns), group.Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("_:var1", "rdf:first", Literal.From(2), ns), group.Clauses[2]);
        CollectionAssert.AreEquivalent(AsTerms("_:var1", "rdf:rest", "rdf:nil", ns), group.Clauses[3]);
        CollectionAssert.AreEquivalent(AsTerms("?person", ":ListValue", "_:var0", ns), group.Clauses[4]);
    }

    [TestMethod]
    public void SelectWithWhereClauseMultiTurtleListSubject()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?list
                WHERE { 
                    (1 2) ?list :ListValue .
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 5);

        CollectionAssert.AreEquivalent(AsTerms("_:var0", "rdf:first", Literal.From(1), ns), group.Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", "rdf:rest", "_:var1", ns), group.Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("_:var1", "rdf:first", Literal.From(2), ns), group.Clauses[2]);
        CollectionAssert.AreEquivalent(AsTerms("_:var1", "rdf:rest", "rdf:nil", ns), group.Clauses[3]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", "?list", ":ListValue", ns), group.Clauses[4]);
    }

    [TestMethod]
    public void SelectWithWhereClauseMultiTurtleListObjects()
    {
        var sparql = @"
                PREFIX : <http://canyala.se/semantic#>
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?person
                WHERE { 
                    ?person :ListValue (1 (2 3) 4) .
                }";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0].Groups[0];

        Assert.IsTrue(group.Groups.Count == 0);
        Assert.IsTrue(group.Clauses.Count == 11);

        CollectionAssert.AreEquivalent(AsTerms("_:var0", "rdf:first", Literal.From(1), ns), group.Clauses[0]);
        
        CollectionAssert.AreEquivalent(AsTerms("_:var1", "rdf:first", Literal.From(2), ns), group.Clauses[1]);
        CollectionAssert.AreEquivalent(AsTerms("_:var1", "rdf:rest", "_:var2", ns), group.Clauses[2]);

        CollectionAssert.AreEquivalent(AsTerms("_:var2", "rdf:first", Literal.From(3), ns), group.Clauses[3]);
        CollectionAssert.AreEquivalent(AsTerms("_:var2", "rdf:rest", "rdf:nil", ns), group.Clauses[4]);

        CollectionAssert.AreEquivalent(AsTerms("_:var0", "rdf:rest", "_:var3", ns), group.Clauses[5]);

        CollectionAssert.AreEquivalent(AsTerms("_:var3", "rdf:first", "_:var1", ns), group.Clauses[6]);
        CollectionAssert.AreEquivalent(AsTerms("_:var3", "rdf:rest", "_:var4", ns), group.Clauses[7]);

        CollectionAssert.AreEquivalent(AsTerms("_:var4", "rdf:first", Literal.From(4), ns), group.Clauses[8]);
        CollectionAssert.AreEquivalent(AsTerms("_:var4", "rdf:rest", "rdf:nil", ns), group.Clauses[9]);

        CollectionAssert.AreEquivalent(AsTerms("?person", ":ListValue", "_:var0", ns), group.Clauses[10]);
    }

    [TestMethod]
    public void SelectWithSubGroup()
    {
        var sparql = @"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?foo $bar 
                { 
                   { 
                     ?foo rdf:type $bar . 
                     ?bar   <in> <gbg>  
                    }
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Groups.Count == 1);
        Assert.IsTrue(group.Groups[0].Clauses.Count == 2);

        Assert.AreEqual("", group.Groups[0].Operation);
        CollectionAssert.AreEquivalent(AsTerms("?foo", "rdf:type", "?bar", ns), group.Groups[0].Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("?bar", "<in>", "<gbg>", ns), group.Groups[0].Clauses[1]);

    }

    [TestMethod]
    public void SelectWithSubGroups()
    {
        var sparql = @"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?foo $bar 
                { 
                   { 
                     ?x rdf:type ?y . 
                     ?foo   <in> <gbg>  
                    }
                    { 
                     ?x rdf:type ?y . 
                     ?foo   <in> <sthlm> . 
                    }
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Groups.Count == 2);
        Assert.IsTrue(group.Groups[0].Clauses.Count == 2);
        Assert.IsTrue(group.Groups[1].Clauses.Count == 2);

        Assert.AreEqual("", group.Groups[0].Operation);
        CollectionAssert.AreEquivalent(AsTerms("?x", "rdf:type", "?y", ns), group.Groups[0].Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("?foo", "<in>", "<gbg>", ns), group.Groups[0].Clauses[1]);

        Assert.AreEqual("", group.Groups[1].Operation);
        CollectionAssert.AreEquivalent(AsTerms("?x", "rdf:type", "?y", ns), group.Groups[1].Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("?foo", "<in>", "<sthlm>", ns), group.Groups[1].Clauses[1]);

    }

    [TestMethod]
    public void SelectWithSubGroupsWithOperations()
    {
        var sparql = @"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX : <http://canyala.se/semantic#>
                SELECT ?foo $bar 
                { 
                   { ?x rdf:type ?y .} union { ?foo a :Man }
                    optional { ?x rdf:type ?y }
                    minus { ?x rdf:type :Series }
                    graph :term { ?x rdf:type :Series }
                    service ?s { ?x rdf:type :Series }
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Groups.Count == 6);

        Assert.AreEqual("", group.Groups[0].Operation);
        Assert.AreEqual("UNION", group.Groups[1].Operation);
        Assert.AreEqual("OPTIONAL", group.Groups[2].Operation);
        Assert.AreEqual("MINUS", group.Groups[3].Operation);
        Assert.AreEqual("GRAPH", group.Groups[4].Operation);
        Assert.AreEqual("SERVICE", group.Groups[5].Operation);
    }

    [TestMethod]
    public void TestQOptComplex4()
    {
        var sparql = @"
                PREFIX  foaf:   <http://xmlns.com/foaf/0.1/>
                PREFIX    ex:   <http://example.org/things#>
                SELECT ?name ?plan ?dept ?img 
                WHERE 
                { 
                    ?person foaf:name ?name  
                    { ?person ex:healthplan ?plan } UNION { ?person ex:department ?dept } 
                    OPTIONAL { 
                        ?person a foaf:Person
                        GRAPH ?g { 
                            [] foaf:name ?name;
                            foaf:depiction ?img 
                        } 
                    } 
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Clauses.Count == 0);
        Assert.IsTrue(group.Groups.Count == 4);
        CollectionAssert.AreEquivalent(AsTerms("?person", "foaf:name", "?name", ns), group.Groups[0].Clauses[0]);

        Assert.IsTrue(group.Groups[3].Clauses.Count == 0);
        Assert.IsTrue(group.Groups[3].Groups.Count == 2);
        CollectionAssert.AreEquivalent(AsTerms("?person", Sparql.a, "foaf:Person", ns), group.Groups[3].Groups[0].Clauses[0]);

        CollectionAssert.AreEquivalent(AsTerms("_:var0", "foaf:name", "?name", ns), group.Groups[3].Groups[1].Groups[0].Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("_:var0", "foaf:depiction", "?img", ns), group.Groups[3].Groups[1].Groups[0].Clauses[1]);
    }

    [TestMethod]
    public void SelectWithAs()
    {
        var sparql = @"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?foo (?x + 3 As $bar) 
                { 
                     ?foo rdf:type ?b . 
                     ?b   <in> <gbg>  
                     BIND (?b - 5 As ?x)
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Groups.Count == 1);
        Assert.IsTrue(group.SelectAsVars.Count == 1);
        Assert.IsTrue(group.SelectBinders.Count == 1);
        Assert.IsTrue(group.Groups[0].Clauses.Count == 2);
        Assert.IsTrue(group.Groups[0].ExplicitBindAsVars.Count == 1);
        Assert.IsTrue(group.Groups[0].ExplicitBinders.Count == 1);

        Assert.AreEqual("", group.Groups[0].Operation);
        CollectionAssert.AreEquivalent(AsTerms("?foo", "rdf:type", "?b", ns), group.Groups[0].Clauses[0]);
        CollectionAssert.AreEquivalent(AsTerms("?b", "<in>", "<gbg>", ns), group.Groups[0].Clauses[1]);

    }

    [TestMethod]
    public void SelectWithSimpleValues()
    {
        var sparql = @"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                SELECT ?name 
                { 
                    VALUES ?x {
                        ""jonas""
                        ""martin""
                    }

                    ?name a ?x .
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Groups.Count == 2);
        Assert.IsTrue(group.Variables.Count == 1);
        Assert.IsTrue(group.Groups[0].Operation == "VALUES");
        Assert.IsTrue(group.Groups[1].Operation == "");
    }

    [TestMethod]
    public void SelectWithFilterExists()
    {
        var sparql = @"
                PREFIX  rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
                PREFIX  foaf:   <http://xmlns.com/foaf/0.1/> 

                SELECT ?person
                WHERE 
                {
                    ?person rdf:type  foaf:Person .
                    FILTER EXISTS { ?person foaf:name ?name }
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Groups.Count == 2);
        Assert.IsTrue(group.Variables.Count == 1);
        Assert.IsTrue(group.Groups[0].Operation == "");
        Assert.IsTrue(group.Groups[1].Operation == "EXISTS");
    }

    [TestMethod]
    public void SelectWithFilterNotExists()
    {
        var sparql = @"
                PREFIX  rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
                PREFIX  foaf:   <http://xmlns.com/foaf/0.1/> 

                SELECT ?person
                WHERE 
                {
                    ?person rdf:type  foaf:Person .
                    FILTER  NOT  EXISTS { ?person foaf:name ?name }
                }
            ";

        string errMsg;
        var query = new Query();
        Assert.IsTrue(Sparql.Translate(sparql, query, out errMsg));

        var ns = query.Plan.Namespaces;
        var group = query.Plan.Groups[0];

        Assert.IsTrue(group.Groups.Count == 2);
        Assert.IsTrue(group.Variables.Count == 1);
        Assert.IsTrue(group.Groups[0].Operation == "");
        Assert.IsTrue(group.Groups[1].Operation == "NOTEXISTS");
    }

    private Term[] AsTerms(string subject, string predicate, string @object, Namespaces? namespaces = null)
    {
        var ns = namespaces ?? new Namespaces();
        var result = new Term[3] 
        {
              subject.StartsWith("_:var") ? new Variable(subject) : Term.Parse(subject, ns),
              predicate.StartsWith("_:var") ? new Variable(predicate) : Term.Parse(predicate, ns),
              @object.StartsWith("_:var") ? new Variable(@object) : Term.Parse(@object, ns)
        };

        return result;
    }
}
