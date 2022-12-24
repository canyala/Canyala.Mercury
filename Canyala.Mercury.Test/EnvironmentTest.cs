//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Linq;
using System.IO;

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

using Canyala.Mercury.Storage;

using Environment = Canyala.Mercury.Storage.Environment;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class EnvironmentTest
    {
        [TestMethod]
        public void TripleStoreResurrectMustWork()
        {
            var socialTurtles = Turtle.FromLines(File.ReadLines(Context.TestFile("social.ttl")));

            using (var temp = Context.GetTemporary())
            {
                int tripleCount;

                using (var envPre = Environment.Create(Strategy.SinglestoreInFile(124 * 1024, temp.FilePath)))
                {
                    var graph = Graph.Create(envPre, "Test", socialTurtles.AsTriples());
                    tripleCount = graph.Count();
                }

                using (var envPost = Environment.Create(Strategy.SinglestoreInFile(124 * 1024, temp.FilePath)))
                {
                    var graph = Graph.Create(envPost, "Test");
                    Assert.AreEqual(tripleCount, graph.Count());
                }
            }
        }

        [TestMethod]
        public void AnonymousTripleStoresMustNotLeakMemory()
        {
            long used;

            using (var temp = Context.GetTemporary())
            {
                using (var envPre = Environment.Create(Strategy.SinglestoreInFile(124 * 1024, temp.FilePath)))
                {
                    using (var graph1 = Graph.Create(envPre))
                    {
                        using (var graph2 = Graph.Create(envPre, Seq.Of(Seq.Array("Alla", "Goda", "Ting"))))
                        {
                            long free2 = envPre.CountFreeBlocks();
                            long used2 = envPre.CountUsedBlocks();
                        }

                        used = envPre.CountUsedBlocks();
                    }
                }

                using (var envPost = Environment.Create(Strategy.SinglestoreInFile(124 * 1024, temp.FilePath)))
                {
                    var graph1 = Graph.Create(envPost);
                    var actualUsed = envPost.CountUsedBlocks();
                    Assert.AreEqual(used, actualUsed);
                }
            }
        }

        [TestMethod]
        public void EnvironmentsMustSupportRootEnumeration()
        {
            using (var temp = Context.GetTemporary())
            {
                using (var environment = Environment.Create(Strategy.SinglestoreInFile(1024 * 1024, temp.FilePath)))
                {
                    using (var graph = Graph.Create(environment, Seq.Of(Seq.Array("All", "good", "things"))))
                    {
                        var roots = environment.Roots.ToArray();    
                        Assert.AreEqual("Default.OSP;Default.POS;Default.SPO;SingletonAllocatorOfString.Index", roots.Join(';'));
                        Assert.AreEqual(1, graph.Count());
                    }
                }
            }
        }
    }
}
