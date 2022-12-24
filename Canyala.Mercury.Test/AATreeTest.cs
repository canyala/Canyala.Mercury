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
using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class AATreeTest
    {
        private static AATree CreateAATree()
        {
            var stream = new MemoryStream();
            var heap = new Heap(stream, 1024);
            return new AATree(heap, (a, b) =>  a - b);
        }

        [TestMethod]
        public void AATreesShouldBeCreatable()
        {
            var tree = CreateAATree();

            Assert.AreEqual(0, tree.Count());
        }

        [TestMethod]
        public void AATreesShouldAllowInsertionOfOneItem()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);

            Assert.AreEqual(1, tree.Count());
        }

        [TestMethod]
        public void AATreesShouldAllowInsertionOfTwoItems()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);

            Assert.AreEqual(2, tree.Count());
        }

        [TestMethod]
        public void AATreesShouldAllowInsertionOfThreeItems()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.AreEqual(3, tree.Count());
        }

        [TestMethod]
        public void AATreesShouldBeOrdered()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue(Seq.AreEqual(Seq.Of(3L, 5L, 7L), tree.Enumerate().Select(offsets => offsets[0]), (a,b) => a == b));
        }

        [TestMethod]
        public void AATreesShouldBeReversibleOrdered()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue(Seq.AreEqual(Seq.Of(7L, 5L, 3L), tree.Enumerate(false).Select(offsets => offsets[0]), (a, b) => a == b));
        }

        [TestMethod]
        public void AATreesShouldBeMiddleSearchable()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue( tree.Search(data => data.CompareTo(5)) != null );
        }

        [TestMethod]
        public void AATreesShouldBeBottomSearchable()
        {
            long searchFor = 3;
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue(tree.Search(data => searchFor.CompareTo(data)) != null);
        }

        [TestMethod]
        public void AATreesShouldBeTopSearchable()
        {
            long searchFor = 7;
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue(tree.Search(data => searchFor.CompareTo(data)) != null);
        }

        [TestMethod]
        public void AATreesShouldBeInsideMissingHiSearchable()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue(tree.Search(data => data.CompareTo(6)) == null);
        }

        [TestMethod]
        public void AATreesShouldBeInsideMissingLoSearchable()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue(tree.Search(data => data.CompareTo(4)) == null);
        }

        [TestMethod]
        public void AATreesShouldBeTopMissingSearchable()
        {
            var tree = CreateAATree();
            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue(tree.Search(data => data.CompareTo(9)) == null);
        }

        [TestMethod]
        public void AATreesShouldBeBottomMissingSearchable()
        {
            var tree = CreateAATree();

            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            Assert.IsTrue(tree.Search(data => data.CompareTo(2)) == null);
        }

        [TestMethod]
        public void AATreesShouldSupportRemoveInside()
        {
            long searchFor = 5;
            Func<long,long> compareTo = data => searchFor.CompareTo(data);
            var tree = CreateAATree();

            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            tree.Remove(compareTo, data => {});

            Assert.IsTrue(tree.Search(compareTo) == null);
        }

        [TestMethod]
        public void RemovingOneItemShouldReduceTheCountByOne()
        {
            var tree = CreateAATree();

            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            long maxCount = tree.Count();

            tree.Remove(data => 7L.CompareTo(data), data => {});

            Assert.AreEqual(1, maxCount - tree.Count());
        }

        [TestMethod]
        public void RemovingTwoItemShouldReduceTheCountByTwo()
        {
            var tree = CreateAATree();

            tree.Insert(data => 5L.CompareTo(data), fields => fields[0] = 5);
            tree.Insert(data => 7L.CompareTo(data), fields => fields[0] = 7);
            tree.Insert(data => 3L.CompareTo(data), fields => fields[0] = 3);

            long maxCount = tree.Count();

            tree.Remove(data => 5L.CompareTo(data), data => {});
            tree.Remove(data => 7L.CompareTo(data), data => {});

            Assert.AreEqual(2, maxCount - tree.Count());
        }
    }
}
