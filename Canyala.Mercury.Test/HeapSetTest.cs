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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;
using Canyala.Lagoon.Text;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Collections;

using Environment = Canyala.Mercury.Storage.Environment;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class HeapSetTest
    {
        [TestMethod]
        public void SetsShouldSupportDispose()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            set.Dispose();
        }

        [TestMethod]
        public void SetsShouldSupportDisposeByFinalizer()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
        }

        [TestMethod]
        public void SetsShouldBeAbleToLiveInDictionaries()
        {
            var environment = Environment.Create();
            var set1 = new SortedHeapSet<int>(environment);
            var set2 = new SortedHeapSet<int>(environment, set1.Offset);
            set1.Dispose();
            set2.Dispose();
        }
        
        [TestMethod]
        public void SetOfLongShouldBeCreateble()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<long>(environment);

            set.Add(5);
            set.Add(7);
            set.Add(3);

            Assert.AreEqual(4, environment.Heap(set.GetType()).CountUsedBlocks());
        }

        [TestMethod]
        public void SetOfStringShouldBeCreateble()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

            set.Add("Martin Fredriksson");
            set.Add("Bill Gates");
            set.Add("Ayn Rand");
            set.Add("Steve Jobs");

            Assert.AreEqual(4, set.Count());
        }

        [TestMethod]
        public void SetOfStringShouldBeIntersectable()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

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
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

            set.Add("Martin Fredriksson");
            set.Add("Bill Gates");
            set.Add("Ayn Rand");
            set.Add("Steve Jobs");

            Assert.AreEqual("Ayn Rand", set.Min);
        }

        [TestMethod]
        public void SetOfStringShouldSupportMax()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

            set.Add("Martin Fredriksson");
            set.Add("Bill Gates");
            set.Add("Ayn Rand");
            set.Add("Steve Jobs");

            Assert.AreEqual("Steve Jobs", set.Max);
        }

        [TestMethod]
        public void SetOfStringShouldBeEnumerable()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

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
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

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
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

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
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

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
            var environment = Environment.Create();
            var set = new SortedHeapSet<double>(environment);

            set.Add(Math.PI);

            Assert.AreEqual(Math.PI, set.First());
        }

        [TestMethod]
        public void SetOfLongShouldBeEnumerableFromExactWhenInclusive()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<long>(environment);

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
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

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
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(2, 4, true, true);

            Assert.AreEqual("1;3;5", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableExclusiveOnLowEnd()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(2, 4, true, false);

            Assert.AreEqual("3", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableInclusiveOnHighEnd()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(10, 12, true, true);

            Assert.AreEqual("9;11;13", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableExclusiveOnHighEnd()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(10, 12, true, false);

            Assert.AreEqual("11", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableInclusiveOnLowEndInReverse()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(2, 4, false, true);

            Assert.AreEqual("5;3;1", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableExclusiveOnLowEndInReverse()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(2, 4, false, false);

            Assert.AreEqual("3", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableInclusiveOnHighEndInReverse()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(10, 12, false, true);

            Assert.AreEqual("13;11;9", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableExclusiveOnHighEndInReverse()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(10, 12, false, false);

            Assert.AreEqual("11", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableExclusiveToEmpty()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(7, 9, true, false);

            Assert.AreEqual("", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableInclusiveToExpand()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(7, 9, true, true);

            Assert.AreEqual("5;11", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableExclusiveToEmptyInReverse()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(7, 9, false, false);

            Assert.AreEqual("", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableInclusiveToExpandInReverse()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(7, 9, false, true);

            Assert.AreEqual("11;5", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableInclusiveOutOfBounds()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(-5, 4, true, true);

            Assert.AreEqual("1;3;5", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableExclusiveOutOfBounds()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(-5, 4, true, false);

            Assert.AreEqual("1;3", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableInReverseFromExactWhenInclusive()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(9, false, true);

            Assert.AreEqual("9;7;5;3;1", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableBetweenExactWhenInclusive()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(3, 9, true, true);

            Assert.AreEqual("3;5;7;9", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableBetweenExactWhenExclusive()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));
            
            var all = set.Enumerate(3, 9, true, true);

            Assert.AreEqual("3;5;7;9", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableBetweenWhenInclusive()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(4, 8, true, true);

            Assert.AreEqual("3;5;7;9", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableBetweenWhenExclusive()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(4, 8, true, false);

            Assert.AreEqual("5;7", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableBetweenWhenInclusiveInReverse()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(4, 8, false, true);

            Assert.AreEqual("9;7;5;3", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfIntShouldBeEnumerableBetweenWhenExclusiveInReverse()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            Seq.Of(1, 3, 5, 7, 9, 11, 13).Do(item => set.Add(item));

            var all = set.Enumerate(4, 8, false, false);

            Assert.AreEqual("7;5", all.Select(i => i.ToString()).Join(';'));
        }

        [TestMethod]
        public void SetOfStringShouldBeExceptable()
        {
            var environment = Environment.Create();
            var set = new SortedHeapSet<string>(environment);

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
            var set = new SortedHeapSet<string>(Environment.Create());

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
            var environment = Storage.Environment.Create();
            var set = new SortedHeapSet<string>(environment);

            GenerateDateStrings(new DateTime(2012, 1, 1), new DateTime(2013, 8, 20)).Do(date => set.Add(date));

            var expected = GenerateDateStrings(new DateTime(2012,8,8), new DateTime(2012,8,16)).ToList();
            var actual = set.Enumerate("2012-08-08", "2012-08-16", true, false).ToList();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SetBetweenLongShouldWork()
        {
            var environment = Storage.Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            12.UpTo(93).Do(value => set.Add(value));

            var section = set.Enumerate(52, 53, true, false).ToList();

            CollectionAssert.AreEqual(52.UpTo(53).ToList(), section);
        }

        [TestMethod]
        public void SetOfManyLongsShouldWork()
        {
            var environment = Storage.Environment.Create();
            var set = new SortedHeapSet<int>(environment);
            12.UpTo(176).Do(value => set.Add(value));

            CollectionAssert.AreEqual(12.UpTo(176).ToList(), set.ToList()); 
        }
    }
}
