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

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

using Canyala.Mercury;
using Canyala.Mercury.Rdf.Extensions;
using System.Linq.Expressions;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class TableTest
    {
        #region Match Data

        static IEnumerable<string[]> leftData = new string[,]
        {
            { "Jonas", "Lagersson" },
            { "Martin", "Fredriksson" }
        }
        .AsRows();

        static Dictionary<string, int> leftColumns = new Dictionary<string, int> 
        { 
            { "hisName", 0 }, 
            { "lastName", 1 } 
        }; 

        static IEnumerable<string[]> rightData = new string [,]
        {
            { "Anna", "Lagersson" },
            { "Monika", "Fredriksson" }
        }
        .AsRows();

        static Dictionary<string, int> rightColumns = new Dictionary<string, int> 
        { 
            { "herName", 0 }, 
            { "lastName", 1 } 
        };

        static IEnumerable<string[]> expected = new string[,]
        {
            { "hisName", "herName" },
            { "Jonas", "Anna" },
            { "Martin", "Monika" }
        }
        .AsRows();

        #endregion

        [TestMethod]
        public void TableMatchingShouldWork()
        {
            var left = new Table(leftData, leftColumns);
            var right = new Table(rightData, rightColumns);
            var match = left.Intersection(right, "hisName", "herName");

            Assert.AreEqual(expected.AsCsvText(), match.AsCsvText());
        }

        #region Union Data

        static IEnumerable<string[]> unionData = new string[,]
        {
            { "_:a", "dc10:creator", "Alice" },
            { "_:a", "dc10:title", "SPARQL Query Language Tutorial" },
            { "_:b", "dc11:title", "SPARQL Protocol Tutorial" },
            { "_:b", "dc11:creator", "Bob" },
            { "_:c", "dc10:title", "SPARQL" },
            { "_:c", "dc11:title", "SPARQL (updated)" }
        }
        .AsRows();

        static IEnumerable<string[]> unionExpectedOne = new string[,]
        {
            { "title" },
            { "SPARQL Query Language Tutorial" },
            { "SPARQL" },
            { "SPARQL Protocol Tutorial" },
            { "SPARQL (updated)" },
        }
        .AsRows();

        #endregion

        [TestMethod]
        public void TableUnionOneColumnShouldWork()
        {
            var union1Data = unionData.Where(row => row[1] == "dc10:title").Select(row => Seq.Array(row[0], row[2]));
            var union1Columns = new Dictionary<string, int> { { "book", 0 }, { "title", 1 } };

            var union2Data = unionData.Where(row => row[1] == "dc11:title").Select(row => Seq.Array(row[0], row[2]));
            var union2Columns = new Dictionary<string, int> { { "book", 0 }, { "title", 1 } };

            var group1 = new Table(union1Data, union1Columns);
            var group2 = new Table(union2Data, union2Columns);

            var union = group1.Union(group2, "title");

            Assert.AreEqual(unionExpectedOne.AsCsvText(), union.AsCsvText());
        }

        static IEnumerable<string[]> unionExpectedTwo = new string[,]
        {
            { "x", "y" },
            { "SPARQL Query Language Tutorial", "" },
            { "SPARQL", "" },
            { "", "SPARQL Protocol Tutorial" },
            { "", "SPARQL (updated)" },
        }
        .AsRows();

        [TestMethod]
        public void TableUnionTwoColumnsShouldWork()
        {
            var union1Data = unionData.Where(row => row[1] == "dc10:title").Select(row => Seq.Array(row[0], row[2]));
            var union1Columns = new Dictionary<string, int> { { "book", 0 }, { "x", 1 } };

            var union2Data = unionData.Where(row => row[1] == "dc11:title").Select(row => Seq.Array(row[0], row[2]));
            var union2Columns = new Dictionary<string, int> { { "book", 0 }, { "y", 1 } };

            var group1 = new Table(union1Data, union1Columns);
            var group2 = new Table(union2Data, union2Columns);

            var union = group1.Union(group2, "x", "y");

            Assert.AreEqual(unionExpectedTwo.AsCsvText(), union.AsCsvText());
        }

        #region Optional Data

        static IEnumerable<string[]> optionalData = new string[,] 
        {
            { "_:a",  "foaf:mbox", "<mailto:alice@example.com>" },
            { "_:a",  "foaf:mbox", "<mailto:alice@work.example>" },
            { "_:a",  "foaf:name", "Alice" },
            { "_:a",  "rdf:type", "foaf:Person" },
            { "_:b",  "foaf:name", "Bob" },
            { "_:b",  "rdf:type", "foaf:Person" },
        }
        .AsRows();

        static IEnumerable<string[]> optionalExpected = new string[,]
        {
            { "name", "mbox" },
            { "Alice", "<mailto:alice@example.com>" },
            { "Alice", "<mailto:alice@work.example>" },
            { "Bob", "" } 
        }
        .AsRows();

        #endregion

        [TestMethod]
        public void TableOptionShouldWork()
        {
            var optional1Data = optionalData.Where(row => row[1] == "foaf:name").Select(row => Seq.Array(row[0], row[2]));
            var optional1Columns = new Dictionary<string, int> { { "x", 0 }, { "name", 1 } };

            var optional2Data = optionalData.Where(row => row[1] == "foaf:mbox").Select(row => Seq.Array(row[0], row[2]));
            var optional2Columns = new Dictionary<string, int> { { "x", 0 }, { "mbox", 1 } };

            var table1 = new Table(optional1Data, optional1Columns);
            var table2 = new Table(optional2Data, optional2Columns);

            var optional = table1.Optional(table2, "name", "mbox");

            Assert.AreEqual(optionalExpected.AsCsvText(), optional.AsCsvText());
        }

        static IEnumerable<string[]> multiOptionsData = new string[,]
        {
            { "_:a",  "foaf:name",  "Alice" },
            { "_:a",  "foaf:homepage", "<http://work.example.org/alice/>" },
            { "_:b",  "foaf:name", "Bob" },
            { "_:b",  "foaf:mbox", "<mailto:bob@work.example>" }
        }
        .AsRows();

        static IEnumerable<string[]> multiOptionsExpected = new string[,]
        {
            { "name", "mbox", "hpage" },
            { "Alice", "", "<http://work.example.org/alice/>" },
            { "Bob", "<mailto:bob@work.example>", "" }
        }
        .AsRows();

        [TestMethod]
        public void TestMultipleOptionsShouldWork()
        {
            var g1data = multiOptionsData.Where(row => row[1] == "foaf:name").Select(row => Seq.Array(row[0], row[2]));
            var g1columns = new Dictionary<string,int> { { "x", 0 }, { "name", 1 } };

            var g2data = multiOptionsData.Where(row => row[1] == "foaf:mbox").Select(row => Seq.Array(row[0], row[2]));
            var g2columns = new Dictionary<string, int> { { "x", 0 }, { "mbox", 1 } };

            var g3data = multiOptionsData.Where(row => row[1] == "foaf:homepage").Select(row => Seq.Array(row[0], row[2]));
            var g3columns = new Dictionary<string, int> { { "x", 0 }, { "hpage", 1 } };

            var table1 = new Table(g1data, g1columns);
            var table2 = new Table(g2data, g2columns);
            var table3 = new Table(g3data, g3columns);

            var result = table1.Optional(table2).Optional(table3, "name", "mbox", "hpage");

            Assert.AreEqual(multiOptionsExpected.AsCsvText(), result.AsCsvText());
        }

        #region Minus Data

        static IEnumerable<string[]> minusData = new string[,]
        {
            { ":alice", "foaf:familyName", "Smith" },
            { ":alice", "foaf:givenName", "Alice" },
            { ":bob", "foaf:familyName", "Jones" },
            { ":bob",   "foaf:givenName", "Bob" },
            { ":carol", "foaf:familyName", "Smith" },
            { ":carol", "foaf:givenName", "Carol" },
        }
        .AsRows();

        static IEnumerable<string[]> minusExpected = new string[,]
        {
            { "s" },
            { ":alice" },
            { ":carol" }
        }
        .AsRows();

        #endregion

        [TestMethod]
        public void TableMinusShouldWork()
        {
            var minus1Data = minusData.Where(row => true).Select(row => row);
            var minus1Columns = new Dictionary<string, int> { { "s", 0 }, { "p", 1 }, { "o", 2 } };

            var minus2Data = minusData.Where(row => row[1] == "foaf:givenName" && row[2] == "Bob").Select(row => Seq.Array(row[0]));
            var minus2Columns = new Dictionary<string, int> { { "s", 0 } };

            var table1 = new Table(minus1Data, minus1Columns);
            var table2 = new Table(minus2Data, minus2Columns);

            var minus = table1.Minus(table2, "s").Distinct();

            Assert.AreEqual(minusExpected.AsCsvText(), minus.AsCsvText());
        }

        #region Group By Data

        static IEnumerable<string[]> groupByData = new string[,]
        {
            { ":january", "expense", "5" },
            { ":january", "expense", "6" },
            { ":february", "expense", "7" },
            { ":mars", "expense", "3" },
            { ":february", "expense", "8" },
            { ":january", "expense", "2" },
            { ":april", "expense", "3" },
            { ":may", "expense", "10" },
            { ":january", "expense", "2" },
            { ":mars", "expense", "1" },
            { ":may", "expense", "15" },
            { ":may", "expense", "3" },
        }
        .AsRows();

        static IEnumerable<string[]> groupByExpected = new string[,]
        {
            { "month", "paid" },
            { ":january", "5" },
            { ":january", "6" },
            { ":january", "2" },
            { ":january", "2" },
            { ":february", "7" },
            { ":february", "8" },
            { ":mars", "3" },
            { ":mars", "1" },
            { ":april", "3" },
            { ":may", "10" },
            { ":may", "15" },
            { ":may", "3" },
        }
        .AsRows();

        #endregion

        /*
        [TestMethod]
        public void TableGroupByShouldWork()
        {
            var data = groupByData.Where(row => true).Select(row => row);
            var columns = new Dictionary<string, int> { { "month", 0 }, { "expense", 1 }, { "paid", 2 } };

            var table = new Table(data, columns).Select("month", "paid");

            var groupBy = table.GroupBy("month").SelectMany(part => part);

            Assert.AreEqual(groupByExpected.AsCsvText(), groupBy.AsCsvText());
        }
        */

        [TestMethod]
        public void TableFilterByShouldWork()
        {
            var data = groupByData.Where(row => true).Select(row => row);
            var columns = new Dictionary<string, int> { { "month", 0 }, { "expense", 1 }, { "paid", 2 } };

            var table = new Table(data, columns).Select("month", "paid");

            Expression<Func<Func<string, string>, bool>> filter = 
                get => int.Parse(get("paid")) > 3 && int.Parse(get("paid")) < 10;

            var filterBy = table.FilterBy(filter.Compile());

        }

        [TestMethod]
        public void TableBindShouldWork()
        {
            var rows = new string[,]
            {
                { "Martin", "Fredriksson" },
                { "Jonas", "Lagersson" }
            }
            .AsRows();

            var columns = new Dictionary<string, int> { { "firstName", 0 }, { "lastName", 1 } };

            var table = new Table(rows, columns);

            var bindings = Seq.Of("fullName");
            var binders = Seq.Of<Func<Func<string, string>, string>>(get => String.Concat(get("firstName"), " ", get("lastName")));

            var expected = new string[,]
            {
                { "firstName", "lastName", "fullName" },
                { "Martin", "Fredriksson", "Martin Fredriksson" },
                { "Jonas", "Lagersson", "Jonas Lagersson" }
            }
            .AsRows();

            var actual = table.Bind(bindings, binders);

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TableBindAndSelectShouldWork()
        {
            var rows = new string[,]
            {
                { "Martin", "Fredriksson" },
                { "Jonas", "Lagersson" }
            }
            .AsRows();

            var columns = new Dictionary<string,int> { { "firstName", 0 }, { "lastName", 1 } };

            var table = new Table(rows, columns);

            var expected = new string[,]
            {
                { "firstName", "fullName" },
                { "Martin", "Martin Fredriksson" },
                { "Jonas", "Jonas Lagersson" }
            }
            .AsRows();

            var bindings = Seq.Of("fullName");
            var binders = Seq.Of<Func<Func<string, string>, string>>(get => String.Concat(get("firstName"), " ", get("lastName")));
            var actual = table.Bind(bindings, binders).Select("firstName", "fullName" );

            Assert.AreEqual(expected.AsCsvText(), actual.AsCsvText());
        }

        [TestMethod]
        public void TableAggregateShouldWork()
        {
            var rows = new string[,]
            {
                { "Martin", "Fredriksson" },
                { "Jonas", "Lagersson" }
            }
            .AsRows();
        }

        [TestMethod]
        public void TableSortingTwoColumnsAscendingAscendingTest()
        {
            var rows = new string[,]
            {
                { "F", "1" },
                { "Z", "8" },
                { "X", "2" },
                { "D", "3" },
                { "L", "4" },
                { "X", "3" },
                { "L", "5" },
                { "Z", "9" },
            }
            .AsRows();

            var columns = new Dictionary<string,int> { { "letter", 0 }, { "number", 1 } };
           
            var table = new Table(rows, columns);

            var actual = table.OrderBy(Seq.Of("letter", "number"), Seq.Of(false, false));

            var expected = new string[,]
            {
                { "D", "3" },
                { "F", "1" },
                { "L", "4" },
                { "L", "5" },
                { "X", "2" },
                { "X", "3" },
                { "Z", "8" },
                { "Z", "9" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.Rows.AsCsvText());
        }

        [TestMethod]
        public void TableSortingTwoColumnsDescendingDescendingTest()
        {
            var rows = new string[,]
            {
                { "F", "1" },
                { "Z", "8" },
                { "X", "2" },
                { "D", "3" },
                { "L", "4" },
                { "X", "3" },
                { "L", "5" },
                { "Z", "9" },
            }
            .AsRows();

            var columns = new Dictionary<string, int> { { "letter", 0 }, { "number", 1 } };

            var table = new Table(rows, columns);

            var actual = table.OrderBy(Seq.Of("letter", "number"), Seq.Of(true, true));

            var expected = new string[,]
            {
                { "Z", "9" },
                { "Z", "8" },
                { "X", "3" },
                { "X", "2" },
                { "L", "5" },
                { "L", "4" },
                { "F", "1" },
                { "D", "3" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.Rows.AsCsvText());
        }

        [TestMethod]
        public void TableSortingTwoColumnsAscendingDescendingTest()
        {
            var rows = new string[,]
            {
                { "F", "1" },
                { "Z", "8" },
                { "X", "2" },
                { "D", "3" },
                { "L", "4" },
                { "X", "3" },
                { "L", "5" },
                { "Z", "9" },
            }
            .AsRows();

            var columns = new Dictionary<string, int> { { "letter", 0 }, { "number", 1 } };

            var table = new Table(rows, columns);

            var actual = table.OrderBy(Seq.Of("letter", "number"), Seq.Of(false, true));

            var expected = new string[,]
            {
                { "D", "3" },
                { "F", "1" },
                { "L", "5" },
                { "L", "4" },
                { "X", "3" },
                { "X", "2" },
                { "Z", "9" },
                { "Z", "8" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.Rows.AsCsvText());
        }

        [TestMethod]
        public void TableSortingTwoColumnsDescendingAscendingTest()
        {
            var rows = new string[,]
            {
                { "F", "1" },
                { "Z", "8" },
                { "X", "2" },
                { "D", "3" },
                { "L", "4" },
                { "X", "3" },
                { "L", "5" },
                { "Z", "9" },
            }
            .AsRows();

            var columns = new Dictionary<string, int> { { "letter", 0 }, { "number", 1 } };

            var table = new Table(rows, columns);

            var actual = table.OrderBy(Seq.Of("letter", "number"), Seq.Of(true, false));

            var expected = new string[,]
            {
                { "Z", "8" },
                { "Z", "9" },
                { "X", "2" },
                { "X", "3" },
                { "L", "4" },
                { "L", "5" },
                { "F", "1" },
                { "D", "3" },
            }
            .AsRows();

            Assert.AreEqual(expected.AsCsvText(), actual.Rows.AsCsvText());
        }
    }
}