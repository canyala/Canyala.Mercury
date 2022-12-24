//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Allocators;
using Canyala.Mercury.Storage.Collections;

using Environment = Canyala.Mercury.Storage.Environment;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class HeapDictionaryTest
    {
        [TestMethod]
        public void DictionaryOfStringsShouldUseSingletonAllocatorAsDefault()
        {
            var environment = Environment.Create(Strategy.PolystoreInMemory(1024*1024));
            var dictionary = new SortedHeapDictionary<string, string>(environment);

            Assert.AreEqual(1, environment.Heap(dictionary.GetType()).CountUsedBlocks());   
        }

        [TestMethod]
        public void DictionariesShouldBeAbleToStoreAndRetrieveKeysAndValues()
        {
            var environment = Environment.Create();
            var dictionary = new SortedHeapDictionary<string,string>(environment);

            dictionary["Author"] = "Ayn Rand";

            Assert.AreEqual("Ayn Rand", dictionary["Author"]);
        }

        [TestMethod]
        public void DictionariesShouldNotGenerateGarbage()
        {
            var environment = Environment.Create();
            var dictionary = new SortedHeapDictionary<string,string>(environment);

            var preUsedBlocks = environment.Heaps.Sum(heap => heap.CountUsedBlocks());

            dictionary["Author"] = "Ayn Rand";
            dictionary["Driver"] = "Howard Rourke";

            dictionary.Remove("Author");
            dictionary.Remove("Driver");

            var postUsedBlocks = environment.Heaps.Sum(heap => heap.CountUsedBlocks());

            Assert.AreEqual(preUsedBlocks, postUsedBlocks);

        }

        [TestMethod]
        public void DictionariesShouldBeClearable()
        {
            var environment = Environment.Create();
            var dictionary = new SortedHeapDictionary<string, string>(environment);

            dictionary["Author"] = "Ayn Rand";
            dictionary["Driver"] = "Emerson";

            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Count);
        }

        [TestMethod]
        public void DictionariesShouldBeCountable()
        {
            var environment = Environment.Create();
            var dictionary = new SortedHeapDictionary<string, string>(environment);

            dictionary["Author"] = "Ayn Rand";
            dictionary["Driver"] = "John Galt";

            Assert.AreEqual(2, dictionary.Count);
        }

        [TestMethod]
        public void DictionariesShouldSupportMin()
        {
            var environment = Environment.Create();
            var dictionary = new SortedHeapDictionary<string, string>(environment);

            dictionary["Author"] = "Ayn Rand";
            dictionary["Driver"] = "John Galt";

            Assert.AreEqual("Author", dictionary.Min);
        }

        [TestMethod]
        public void DictionariesShouldSupportMax()
        {
            var environment = Environment.Create();
            var dictionary = new SortedHeapDictionary<string, string>(environment);

            dictionary["Author"] = "Ayn Rand";
            dictionary["Driver"] = "John Galt";

            Assert.AreEqual("Driver", dictionary.Max);
        }

        [TestMethod]
        public void DictionaryOfDictionaryOfSetOfStringShouldBeCreatable()
        {
            var environment = Environment.Create();
            var ternary = new SortedHeapSet<string>(environment);
            ternary.Add("Amidala");

            var secondaryTernary = new SortedHeapDictionary<string, SortedHeapSet<string>>(environment);
            secondaryTernary.Add("likes", ternary);

            var primarySecondary = new SortedHeapDictionary<string,SortedHeapDictionary<string,SortedHeapSet<string>>>(environment);
            primarySecondary.Add("Anakin", secondaryTernary);

            Assert.IsTrue(primarySecondary["Anakin"]["likes"]["Amidala"]);
        }

        [TestMethod]
        public void NamedDictionaryOfDictionaryOfSetShouldBeCreatable()
        {
            var environment = Environment.Create();
            var ternary = new SortedHeapSet<string>(environment);
            ternary.Add("Amidala");

            var secondaryTernary = new SortedHeapDictionary<string, SortedHeapSet<string>>(environment);
            secondaryTernary.Add("likes", ternary);

            var primarySecondary = new SortedHeapDictionary<string, SortedHeapDictionary<string, SortedHeapSet<string>>>(environment, "SPO");
            primarySecondary.Add("Anakin", secondaryTernary);

            Assert.IsTrue(primarySecondary["Anakin"]["likes"]["Amidala"]);
        }

        [TestMethod]
        public void CollectionsShouldBeRefCounted()
        {
            var environment = Environment.Create(Strategy.PolystoreInMemory(1024*1024));

            var secondaryTernary = new SortedHeapDictionary<string, SortedHeapSet<string>>(environment);
            var preUsedBlocks = environment.CountUsedBlocks();

            var ternary = new SortedHeapSet<string>(environment);
            ternary.Add("Amidala");

            var midUsedBlocks = environment.CountUsedBlocks();

            secondaryTernary.Add("likes", ternary);
            ternary.Dispose();
            ternary = null;

            secondaryTernary.Remove("likes");

            var postUsedBlocks = environment.CountUsedBlocks();

            Assert.AreEqual(preUsedBlocks, postUsedBlocks);
        }
    }
}
