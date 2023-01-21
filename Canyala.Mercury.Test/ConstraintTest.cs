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

using Canyala.Lagoon.Collections;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;
using Canyala.Lagoon.Serialization;
using Canyala.Lagoon.Text;

using Canyala.Mercury;
using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Serialization;

using Canyala.Test.Tools;

namespace Canyala.Mercury.Test;

[TestClass]
public class ConstraintTest
{
    static string[,] lovers = 
    {
        { "Adam", "loves", "Eve" },
        { "Anakin", "loves", "Amidala" },
        { "Romeo", "loves", "Juliet" }
    };

    [TestMethod]
    public void InConstraintShouldWork()
    {
        var environment = Storage.Environment.Create();
        var graph = Graph.Create(environment, lovers.AsRows());

        var result = graph[Constraint.In("Adam", "Romeo", "John"), "loves", null];

        var actual = result.Select(match => match.Join(';')).Join(" + ");
        Assert.AreEqual("Adam;Eve + Romeo;Juliet", actual);
    }

    [TestMethod]
    public void BigInSetConstraintShouldWork()
    {
        var environment = Storage.Environment.Create();
        var graph = Graph.Create(environment, lovers.AsRows());

        var famousPeople = new HashSet<string>(Seq.Of("Wolfgang", "Anakin", "John", "Steven", "Leonard"));
        var result = graph[Constraint.In(famousPeople), "loves", null];

        var actual = result.Select(match => match.Join(';')).Join(" + ");
        Assert.AreEqual("Anakin;Amidala", actual);
    }

    [TestMethod]
    public void SmallInSetConstraintShouldWork()
    {
        var environment = Storage.Environment.Create();
        var graph = Graph.Create(environment, lovers.AsRows());

        var famousPeople = new HashSet<string>(Seq.Of("Romeo", "John"));
        var result = graph[Constraint.In(famousPeople), "loves", null];

        var actual = result.Select(match => match.Join(';')).Join(" + ");
        Assert.AreEqual("Romeo;Juliet", actual);
    }

    [TestMethod]
    public void TruePredicateConstraintShouldWork()
    {
        var environment = Storage.Environment.Create();
        var graph = Graph.Create(environment, lovers.AsRows());

        var result = graph[Constraint.True(x => x.StartsWith("A")), "loves", null];

        var actual = result.Select(match => match.Join(';')).Join(" + ");
        Assert.AreEqual("Anakin;Amidala + Adam;Eve", actual);
    }

    [TestMethod]
    public void FalsePredicateConstraintShouldWork()
    {
        var environment = Storage.Environment.Create();
        var graph = Graph.Create(environment, lovers.AsRows());

        var result = graph[Constraint.False(x => x.StartsWith("A")), "loves", null];

        var actual = result.Select(match => match.Join(';')).Join(" + ");
        Assert.AreEqual("Romeo;Juliet", actual);
    }

}
