//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Collections;
using Canyala.Lagoon.Contracts;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

using Canyala.Mercury.Text;

namespace Canyala.Mercury.Test
{
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
}
