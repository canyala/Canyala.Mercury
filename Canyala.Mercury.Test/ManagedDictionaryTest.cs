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
using Canyala.Mercury.Storage.Allocators;
using Canyala.Mercury.Storage.Collections;

using Environment = Canyala.Mercury.Storage.Environment;

namespace Canyala.Mercury.Test.All;

[TestClass]
public class ManagedDictionaryTest
{
    [TestMethod]
    public void DictionariesShouldBeAbleToStoreAndRetrieveKeysAndValues()
    {
        var dictionary = new SortedManagedDictionary<string,string>();

        dictionary["Author"] = "Ayn Rand";

        Assert.AreEqual("Ayn Rand", dictionary["Author"]);
    }

    [TestMethod]
    public void DictionariesShouldBeClearable()
    {
        var dictionary = new SortedManagedDictionary<string, string>();

        dictionary["Author"] = "Ayn Rand";
        dictionary["Driver"] = "Emerson";

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
    }

    [TestMethod]
    public void DictionariesShouldBeCountable()
    {
        var dictionary = new SortedManagedDictionary<string, string>();

        dictionary["Author"] = "Ayn Rand";
        dictionary["Driver"] = "John Galt";

        Assert.AreEqual(2, dictionary.Count);
    }

    [TestMethod]
    public void DictionariesShouldSupportMin()
    {
        var dictionary = new SortedManagedDictionary<string, string>();

        dictionary["Author"] = "Ayn Rand";
        dictionary["Driver"] = "John Galt";

        Assert.AreEqual("Author", dictionary.Min);
    }

    [TestMethod]
    public void DictionariesShouldSupportMax()
    {
        var dictionary = new SortedManagedDictionary<string, string>();

        dictionary["Author"] = "Ayn Rand";
        dictionary["Driver"] = "John Galt";

        Assert.AreEqual("Driver", dictionary.Max);
    }

    [TestMethod]
    public void DictionaryOfDictionaryOfSetOfStringShouldBeCreatable()
    {
        var ternary = new SortedManagedSet<string>();
        ternary.Add("Amidala");

        var secondaryTernary = new SortedManagedDictionary<string, SortedManagedSet<string>>();
        secondaryTernary.Add("likes", ternary);

        var primarySecondary = new SortedManagedDictionary<string, SortedManagedDictionary<string, SortedManagedSet<string>>>();
        primarySecondary.Add("Anakin", secondaryTernary);

        Assert.IsTrue(primarySecondary["Anakin"]["likes"]["Amidala"]);
    }

    [TestMethod]
    public void NamedDictionaryOfDictionaryOfSetShouldBeCreatable()
    {
        var ternary = new SortedManagedSet<string>();
        ternary.Add("Amidala");

        var secondaryTernary = new SortedManagedDictionary<string, SortedManagedSet<string>>();
        secondaryTernary.Add("likes", ternary);

        var primarySecondary = new SortedManagedDictionary<string, SortedManagedDictionary<string, SortedManagedSet<string>>>("PredicateSubjectObject");
        primarySecondary.Add("Anakin", secondaryTernary);

        Assert.IsTrue(primarySecondary["Anakin"]["likes"]["Amidala"]);
    }
}
