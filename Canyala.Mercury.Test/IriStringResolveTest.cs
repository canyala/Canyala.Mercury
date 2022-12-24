//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Mercury.Extensions;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class IriStringResolveTest
    {
        [TestMethod]
        public void TestResolveRelative()
        {
            string baseUri = "http://a/b/c/d;p?q";

            Assert.AreEqual("g:h", "g:h".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/g", "g".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/g", "./g".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/g/", "g/".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/g", "/g".ResolveRelative(baseUri));
            Assert.AreEqual("http://g", "//g".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/d;p?y", "?y".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/g?y", "g?y".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/d;p?q#s", "#s".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/g#s", "g#s".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/g?y#s", "g?y#s".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/;x", ";x".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/g;x", "g;x".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/g;x?y#s", "g;x?y#s".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/d;p?q", "".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/", ".".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/c/", "./".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/", "..".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/", "../".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/b/g", "../g".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/", "../..".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/", "../../".ResolveRelative(baseUri));
            Assert.AreEqual("http://a/g", "../../g".ResolveRelative(baseUri));

        }

        [TestMethod]
        public void TestResolveAbsoluteWithShorterBase1()
        {
            string r,b,baseUri = "http://a/b/";
            Assert.IsTrue("http://a/b/c".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("c", r);
            Assert.AreEqual("http://a/b/", b);

            baseUri = "http://a/b/";
            Assert.IsTrue("http://a/b/c/".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("c/", r);
            Assert.AreEqual("http://a/b/", b);

            baseUri = "http://a/b/c";
            Assert.IsTrue("http://a/b/".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("..", r);
            Assert.AreEqual("http://a/b/", b);

            baseUri = "http://a/b/c/";
            Assert.IsTrue("http://a/b/".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("..", r);
            Assert.AreEqual("http://a/b/", b);

            baseUri = "http://a/b/c";
            Assert.IsTrue("http://a/b/c".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("", r);
            Assert.AreEqual("http://a/b/c", b);

            baseUri = "http://a/b/d";
            Assert.IsTrue("http://a/b/c".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("../c", r);
            Assert.AreEqual("http://a/b/", b);

            baseUri = "http://f/b/c";
            Assert.IsFalse("http://a/b/c".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("", r);
            Assert.AreEqual("", b);

            baseUri = "ftp://a/b/c";
            Assert.IsFalse("http://a/b/c".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("", r);
            Assert.AreEqual("", b);

            baseUri = "http://a/b/";
            Assert.IsTrue("http://a/b/c/d".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("c/d", r);
            Assert.AreEqual("http://a/b/", b);

            baseUri = "http://a/b/c/d";
            Assert.IsTrue("http://a/b/".ResolveAbsolute(baseUri, out r, out b));
            Assert.AreEqual("../..", r);
            Assert.AreEqual("http://a/b/", b);
        }
    }
}
