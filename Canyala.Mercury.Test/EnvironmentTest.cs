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

using Canyala.Mercury.Storage;

using Environment = Canyala.Mercury.Storage.Environment;

namespace Canyala.Mercury.Test.All;

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
