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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Allocators;

namespace Canyala.Mercury.Test
{
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
}
