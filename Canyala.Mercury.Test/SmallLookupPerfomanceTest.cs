//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Canyala.Mercury.Test
{
    /*
    [TestClass]
    public class SmallLookupPerformanceTest
    {
        const int LOOPS = 1000000;

        [TestMethod]
        public void TestPerformanceDictionary()
        {
            for (int i=0; i<LOOPS; i++)
            {
                var dictionary = new Dictionary<string,int>() 
                {
                    { "A", 1 },
                    { "B", 2 },
                    { "C", 3 },
                    { "D", 4 }
                };

                var a1 = dictionary["A"];
                var a2 = dictionary["B"];
                var a3 = dictionary["C"];
                var a4 = dictionary["D"];
            }
        }

        [TestMethod]
        public void TestPerformanceSortedDictionary()
        {
            for (int i = 0; i < LOOPS; i++)
            {
                var dictionary = new SortedDictionary<string, int>() 
                {
                    { "A", 1 },
                    { "B", 2 },
                    { "C", 3 },
                    { "D", 4 }
                };

                var a1 = dictionary["A"];
                var a2 = dictionary["B"];
                var a3 = dictionary["C"];
                var a4 = dictionary["D"];
            }
        }

        [TestMethod]
        public void TestPerformanceTupleListAndLinq()
        {
            for (int i = 0; i < LOOPS; i++)
            {
                var list = new List<Tuple<string,int>>() 
                {
                    Tuple.Create( "A", 1 ),
                    Tuple.Create( "B", 2 ),
                    Tuple.Create( "C", 3 ),
                    Tuple.Create( "D", 4 )
                };

                var a1 = list.Where(item => item.Item1 == "A").Select(item => item.Item2).First();
                var a2 = list.Where(item => item.Item1 == "B").Select(item => item.Item2).First();
                var a3 = list.Where(item => item.Item1 == "C").Select(item => item.Item2).First();
                var a4 = list.Where(item => item.Item1 == "D").Select(item => item.Item2).First();
            }
        }

        [TestMethod]
        public void TestPerformanceListLookup()
        {
            Func<List<string>,string,int> lookup = (list,key) => 
            { 
                for (int j=0; j<list.Count; j++) 
                    if (key == list[j]) return j;

                return -1;
            };

            for (int i = 0; i < LOOPS; i++)
            {
                var list = new List<string> { "A", "B", "C", "D" };

                var a1 = lookup(list, "A");
                var a2 = lookup(list, "B");
                var a3 = lookup(list, "C");
                var a4 = lookup(list, "D");
            }
        }

        [TestMethod]
        public void TestPerformanceIndexLookup()
        {
            for (int i = 0; i < LOOPS; i++)
            {
                var index = new int[4] { 0, 4, 2, 1 };

                var a2 = index[1];
                var a1 = index[0];
                var a3 = index[2];
                var a4 = index[3]; 
            }
        }

        [TestMethod]
        public void TestPerformanceIndexLookupNoAlloc()
        {
            var index = new int[4] { 0, 4, 2, 1 };

            for (int i = 0; i < LOOPS; i++)
            {
                var a2 = index[1];
                var a1 = index[0];
                var a3 = index[2];
                var a4 = index[3];
            }
        }

        [TestMethod]
        public void TestPerformanceHashSet()
        {
            for (int i = 0; i < LOOPS; i++)
            {
                var set = new HashSet<string>() 
                {
                    { "A" },
                    { "B" },
                    { "C" },
                    { "D" }
                };

                var a1 = set.Contains("A");
                var a2 = set.Contains("B");
                var a3 = set.Contains("C");
                var a4 = set.Contains("D");
            }
        }

        [TestMethod]
        public void TestPerformanceSortedSet()
        {
            for (int i = 0; i < LOOPS; i++)
            {
                var set = new SortedSet<string>() 
                {
                    { "A" },
                    { "B" },
                    { "C" },
                    { "D" }
                };

                var a1 = set.Contains("A");
                var a2 = set.Contains("B");
                var a3 = set.Contains("C");
                var a4 = set.Contains("D");
            }
        }
    }
    */
}
