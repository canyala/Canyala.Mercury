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
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;
using Canyala.Lagoon.Core.Text;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Collections;

using Environment = Canyala.Mercury.Storage.Environment;
using System.Text.Json.Serialization.Metadata;

namespace Canyala.Mercury.Test.All;

[TestClass]
public class ManagedSetTest
{
    [TestMethod]
    public void SetsShouldSupportDispose()
    {
        var set = new SortedManagedSet<int>();
        set.Dispose();
    }

    [TestMethod]
    public void SetsShouldSupportDisposeByFinalizer()
    {
        var set = new SortedManagedSet<int>();
    }
    
    [TestMethod]
    public void SetOfLongShouldBeCreateble()
    {
        var set = new SortedManagedSet<long>();

        set.Add(5);
        set.Add(7);
        set.Add(3);

        Assert.AreEqual(3, set.Count);
    }

    [TestMethod]
    public void SetOfStringShouldBeCreateble()
    {
        var set = new SortedManagedSet<string>();

        set.Add("Martin Fredriksson");
        set.Add("Bill Gates");
        set.Add("Ayn Rand");
        set.Add("Steve Jobs");

        Assert.AreEqual(4, set.Count());
    }

    [TestMethod]
    public void SetOfStringShouldBeIntersectable()
    {
        var set = new SortedManagedSet<string>();

        set.Add("Martin Fredriksson");
        set.Add("Bill Gates");
        set.Add("Ayn Rand");
        set.Add("Steve Jobs");

        set.IntersectWith(Seq.Of("Ayn Rand", "Martin Fredriksson", "Steve Ballmer"));

        var array = set.ToArray();

        Assert.AreEqual(2, set.Count);
    }

    [TestMethod]
    public void SetOfStringShouldSupportMin()
    {
        var set = new SortedManagedSet<string>();

        set.Add("Martin Fredriksson");
        set.Add("Bill Gates");
        set.Add("Ayn Rand");
        set.Add("Steve Jobs");

        Assert.AreEqual("Ayn Rand", set.Min);
    }

    [TestMethod]
    public void SetOfStringShouldSupportMax()
    {
        var set = new SortedManagedSet<string>();

        set.Add("Martin Fredriksson");
        set.Add("Bill Gates");
        set.Add("Ayn Rand");
        set.Add("Steve Jobs");

        Assert.AreEqual("Steve Jobs", set.Max);
    }

    [TestMethod]
    public void SetOfStringShouldBeEnumerable()
    {
        var set = new SortedManagedSet<string>();

        set.Add("A");
        set.Add("C");
        set.Add("E");
        set.Add("G");
        set.Add("I");
        set.Add("K");
        set.Add("M");

        var all = set.Join(';');

        Assert.AreEqual("A;C;E;G;I;K;M", all);
    }


    [TestMethod]
    public void SetOfStringShouldBeEnumerableFromExclusive()
    {
        var set = new SortedManagedSet<string>();

        set.Add("A");
        set.Add("C");
        set.Add("E");
        set.Add("G");
        set.Add("I");
        set.Add("K");
        set.Add("M");

        var all = set.Enumerate("D", true, false);

        Assert.AreEqual("E;G;I;K;M", all.Join(';'));
    }

    [TestMethod]
    public void SetOfStringShouldBeEnumerableFromExclusiveInReverse()
    {
        var set = new SortedManagedSet<string>();

        set.Add("A");
        set.Add("C");
        set.Add("E");
        set.Add("G");
        set.Add("I");
        set.Add("K");
        set.Add("M");

        var all = set.Enumerate("J", false, false);

        Assert.AreEqual("I;G;E;C;A", all.Join(';'));
    }

    [TestMethod]
    public void SetOfStringShouldBeEnumerableFromExactWhenExclusive()
    {
        var set = new SortedManagedSet<string>();

        set.Add("A");
        set.Add("C");
        set.Add("E");
        set.Add("G");
        set.Add("I");
        set.Add("K");
        set.Add("M");

        var all = set.Enumerate("C", true, false);

        Assert.AreEqual("C;E;G;I;K;M", all.Join(';'));
    }

    [TestMethod]
    public void SetOfDoubleShouldBeLossless()
    {
        var set = new SortedManagedSet<double>();

        set.Add(Math.PI);

        Assert.AreEqual(Math.PI, set.First());
    }

    [TestMethod]
    public void SetOfLongShouldBeEnumerableFromExactWhenInclusive()
    {
        var set = new SortedManagedSet<long>();

        set.Add(1);
        set.Add(3);
        set.Add(5);
        set.Add(7);
        set.Add(9);
        set.Add(11);
        set.Add(13);

        var all = set.Enumerate(5, true, true);

        Assert.AreEqual("5;7;9;11;13", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfStringShouldBeEnumerableInReverseFromExactWhenExclusive()
    {
        var set = new SortedManagedSet<string>();

        set.Add("A");
        set.Add("C");
        set.Add("E");
        set.Add("G");
        set.Add("I");
        set.Add("K");
        set.Add("M");

        var all = set.Enumerate("I", false, false);

        Assert.AreEqual("I;G;E;C;A", all.Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableInclusiveOnLowEnd()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(2, 4, true, true);

        Assert.AreEqual("1;3;5", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableExclusiveOnLowEnd()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(2, 4, true, false);

        Assert.AreEqual("3", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableInclusiveOnHighEnd()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(10, 12, true, true);

        Assert.AreEqual("9;11;13", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableExclusiveOnHighEnd()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(10, 12, true, false);

        Assert.AreEqual("11", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableInclusiveOnLowEndInReverse()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(2, 4, false, true);

        Assert.AreEqual("5;3;1", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableExclusiveOnLowEndInReverse()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(2, 4, false, false);

        Assert.AreEqual("3", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableInclusiveOnHighEndInReverse()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(10, 12, false, true);

        Assert.AreEqual("13;11;9", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableExclusiveOnHighEndInReverse()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(10, 12, false, false);

        Assert.AreEqual("11", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableExclusiveToEmpty()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(7, 9, true, false);

        Assert.AreEqual("", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableInclusiveToExpand()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(7, 9, true, true);

        Assert.AreEqual("5;11", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableExclusiveToEmptyInReverse()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(7, 9, false, false);

        Assert.AreEqual("", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableInclusiveToExpandInReverse()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(7, 9, false, true);

        Assert.AreEqual("11;5", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableInclusiveOutOfBounds()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(-5, 4, true, true);

        Assert.AreEqual("1;3;5", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableExclusiveOutOfBounds()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(-5, 4, true, false);

        Assert.AreEqual("1;3", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableInReverseFromExactWhenInclusive()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(9, false, true);

        Assert.AreEqual("9;7;5;3;1", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableBetweenExactWhenInclusive()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(3, 9, true, true);

        Assert.AreEqual("3;5;7;9", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableBetweenExactWhenExclusive()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));
        
        var all = set.Enumerate(3, 9, true, true);

        Assert.AreEqual("3;5;7;9", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableBetweenWhenInclusive()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(4, 8, true, true);

        Assert.AreEqual("3;5;7;9", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableBetweenWhenExclusive()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(4, 8, true, false);

        Assert.AreEqual("5;7", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableBetweenWhenInclusiveInReverse()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(4, 8, false, true);

        Assert.AreEqual("9;7;5;3", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfIntShouldBeEnumerableBetweenWhenExclusiveInReverse()
    {
        var set = new SortedManagedSet<int>();
        Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

        var all = set.Enumerate(4, 8, false, false);

        Assert.AreEqual("7;5", all.Select(i => i.ToString()).Join(';'));
    }

    [TestMethod]
    public void SetOfStringShouldBeExceptable()
    {
        var set = new SortedManagedSet<string>();

        set.Add("Martin Fredriksson");
        set.Add("Bill Gates");
        set.Add("Ayn Rand");
        set.Add("Steve Jobs");

        set.ExceptWith(Seq.Of("Ayn Rand", "Steve Jobs", "Steve Ballmer"));

        Assert.AreEqual(2, set.Count);
    }

    static string[] permutations =
    {
		    "a","ar","are","d","e","el","ell","ello","en","end","er","ere","f","fo","for","fr","fri","frie","frien","friend",
		    "g","h","H","he","He","Hel","Hell","Hello","her","here","ho","i","ie","ien","iend","in","ing","k","ki","kin","king",
		    "l","ll","llo","lo","loo","look","looki","lookin","looking","m","my","n","nd","ng","no","not","o","ok","oki","okin",
		    "oking","oo","ook","ooki","ookin","ooking","or","ot","ou","r","re","ri","rie","rien","riend","t","th","the","ther",
		    "there","u","w","W","We","wh","who","y","yo","you"
    };

    [TestMethod]
    public void SetShouldAllowRemove()
    {
        var set = new SortedManagedSet<string>();

        set.UnionWith(permutations);

        foreach (var permutation in permutations.Reverse())
        {
            var preCount = set.Count;
            set.Remove(permutation);

            var postCount = set.Count;

            Assert.AreEqual(1, preCount-postCount);
        }

        set.UnionWith(permutations.Reverse());

        foreach (var permutation in permutations)
        {
            var preCount = set.Count;
            set.Remove(permutation);

            var postCount = set.Count;

            Assert.AreEqual(1, preCount - postCount);
        }

        Assert.AreEqual(0, set.Count);
    }

    private static IEnumerable<string> GenerateDateStrings(DateTime from, DateTime to)
    {
        for (DateTime current = from; current <= to; current = current.AddDays(1))
            yield return "{0}-{1:00}-{2:00}".Args(current.Year, current.Month, current.Day);
    }

    [TestMethod]
    public void SetBetweenStringShouldWork()
    {
        var set = new SortedManagedSet<string>();

        GenerateDateStrings(new DateTime(2012, 1, 1), new DateTime(2013, 8, 20)).Do(date => set.Add(date));

        var expected = GenerateDateStrings(new DateTime(2012,8,8), new DateTime(2012,8,16)).ToList();
        var actual = set.Enumerate("2012-08-08", "2012-08-16", true, false).ToList();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void SetBetweenLongShouldWork()
    {
        var set = new SortedManagedSet<int>();
        12.UpTo(93).Do(value => set.Add(value));

        var section = set.Enumerate(52, 53, true, false).ToList();

        CollectionAssert.AreEqual(52.UpTo(53).ToList(), section);
    }

    [TestMethod]
    public void SetOfManyLongsShouldWork()
    {
        var set = new SortedManagedSet<int>();
        12.UpTo(176).Do(value => set.Add(value));

        CollectionAssert.AreEqual(12.UpTo(176).ToList(), set.ToList()); 
    }
}
