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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Core.Collections;
using Canyala.Lagoon.Core.Contracts;
using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury.Core.Text;

namespace Canyala.Mercury.Test.All;

[TestClass]
public class ParserTest
{
    [TestMethod]
    public void SimpleLanguageShouldParsePositiveNumber()
    {
        string errMsg;
        var integer = new Int();
        SimpleLanguage.Translate("42", integer, out errMsg);
        Assert.AreEqual(42, integer.Value);
    }

    [TestMethod]
    public void SimpleLanguageShouldParseNegativeNumber()
    {
        string errMsg;
        var integer = new Int();
        SimpleLanguage.Translate("-42", integer, out errMsg);
        Assert.AreEqual(-42, integer.Value);
    }

    [TestMethod]
    public void SimpleLanguageShouldNotParseAnyString()
    {
        string errMsg;
        var integer = new Int();
        Assert.IsFalse(SimpleLanguage.Translate("gurka", integer, out errMsg));
    }

    [TestMethod]
    public void LoopTest()
    {
        string errMsg;
        var integer = new Int();
        Assert.IsFalse(LoopLanguage.Translate("1", integer, out errMsg));
    }

    class Int
    {
        public int Value { get; set; }
    }

    class SimpleLanguage : Parser<Int>
    {
        public static bool Translate(string text, Int query, out string errMsg)
        {
            return SimpleLanguage.Instance.Apply(text, query, out errMsg);
        }

        static readonly Func<Production> Integer = () => All(Named("value", AnyOf(Number, All('-', Number))), Call((@int, names) => @int.Value = int.Parse(names["value"])));
        static readonly Func<Production> Number = () => OneOrMore(Digit);
        static readonly Func<Production> Digit = () => InRange('0', '9');

        private static readonly SimpleLanguage Instance = new SimpleLanguage();

        private SimpleLanguage() : base(Integer) { }
    }

    class LoopLanguage : Parser<Int>
    {
        public static bool Translate(string text, Int query, out string errMsg)
        {
            return LoopLanguage.Instance.Apply(text, query, out errMsg);
        }

        static readonly Func<Production> Main = () => ZeroOrMore(ZeroOrMore('a'));

        private static readonly LoopLanguage Instance = new LoopLanguage();

        private LoopLanguage() : base(Main) { }
    }
}
