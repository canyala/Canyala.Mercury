//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
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
}
