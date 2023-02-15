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
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Test.All;

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
