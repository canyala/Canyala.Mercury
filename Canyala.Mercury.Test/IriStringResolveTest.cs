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
