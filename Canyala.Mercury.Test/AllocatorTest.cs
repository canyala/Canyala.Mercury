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

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Allocators;

namespace Canyala.Mercury.Test.All;

[TestClass]
public class AllocatorTest
{
    [TestMethod]
    public void SingletonAllocatorsShouldSaveSpace()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024*1024);
        var allocator = new SingletonAllocator<string>(heap);

        var offset1 = allocator.Alloc("Ayn Rand");
        var offset2 = allocator.Alloc("Ayn Rand");

        Assert.AreEqual(offset1, offset2);
    }

    [TestMethod]
    public void SingletonAllocatorsShouldBeAtomic()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var allocator = new SingletonAllocator<string>(heap);

        var offset1 = allocator.Alloc("Ayn Rand");
        var offset2 = allocator.Alloc("Ayn Rand");

        var s1 = allocator[offset1];
        var s2 = allocator[offset2];

        Assert.AreEqual("Ayn Rand", s1);
        Assert.AreEqual("Ayn Rand", s2);
        Assert.AreEqual(s1, s2);
    }
}
