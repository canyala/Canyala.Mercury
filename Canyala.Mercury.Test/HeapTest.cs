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
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Lagoon.Extensions;

using Canyala.Mercury.Storage;
using Canyala.Mercury.Storage.Internal;

namespace Canyala.Mercury.Test;

[TestClass]
public class HeapTest
{
    [TestMethod]
    public void NewHeapShouldHaveOneFreeBlock()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);

        Assert.AreEqual(1,  heap.CountFreeBlocks());
    }

    [TestMethod]
    public void NewHeapShouldHaveZeroUsedBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);

        Assert.AreEqual(0, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapWithOneAllocationShouldHaveOneFreeBlock()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset = heap.Alloc(64);

        Assert.AreEqual(1, heap.CountFreeBlocks());
    }

    [TestMethod]
    public void HeapWithOneAllocationShouldHaveOneUsedBlock()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset = heap.Alloc(64);

        Assert.AreEqual(1, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapWithOneAllocationAndOneFreeShouldHaveNoUsedBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);

        var offset = heap.Alloc(64);
        heap.Free(offset);

        Assert.AreEqual(0, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapWithTwoAllocationsShouldHaveTwoUsedBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(64);
        var offset2 = heap.Alloc(32);

        Assert.AreEqual(2, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapWithThreeAllocationsShouldHaveThreeUsedBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(64);
        var offset2 = heap.Alloc(32);
        var offset3 = heap.Alloc(16);

        Assert.AreEqual(3, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapWithTwoAllocationsShouldHaveOneFreeBlock()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(64);
        var offset2 = heap.Alloc(32);

        Assert.AreEqual(1, heap.CountFreeBlocks());
    }

    [TestMethod]
    public void HeapWithOneAllocationAndOneFreeShouldHaveOneFreeBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset = heap.Alloc(64);
        heap.Free(offset);

        Assert.AreEqual(1, heap.CountFreeBlocks());
    }

    [TestMethod]
    public void HeapWithThreeAllocationsAndFreeOfFirstShouldHaveTwoUsedBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);

        heap.Free(offset1);

        Assert.AreEqual(2, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapWithThreeAllocationsAndFreeOfMiddleShouldHaveTwoUsedBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);
        heap.Free(offset2);

        Assert.AreEqual(2, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapWithThreeAllocationsAndFreeOfLastShouldHaveTwoUsedBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);
        heap.Free(offset3);

        Assert.AreEqual(2, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapWithThreeAllocationsAndFreeOfFirstShouldHaveTwoFreeBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);
        heap.Free(offset1);

        Assert.AreEqual(2, heap.CountFreeBlocks());
    }

    [TestMethod]
    public void HeapWithThreeAllocationsAndFreeOfMiddleShouldHaveTwoFreeBlocks()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);
        heap.Free(offset2);

        Assert.AreEqual(2, heap.CountFreeBlocks());
    }

    [TestMethod]
    public void HeapWithThreeAllocationsAndFreeOfLastShouldHaveOneFreeBlock()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);
        heap.Free(offset3);

        Assert.AreEqual(1, heap.CountFreeBlocks());
    }

    [TestMethod]
    public void FirstOfAllocatedOffsetsShouldBeValid()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);

        Assert.AreEqual(true, heap.IsValid(offset1));
    }

    [TestMethod]
    public void SecondOfAllocatedOffsetsShouldBeValid()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);

        Assert.AreEqual(true, heap.IsValid(offset2));
    }

    [TestMethod]
    public void ThirdOfAllocatedOffsetsShouldBeValid()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);

        Assert.AreEqual(true, heap.IsValid(offset3));
    }

    [TestMethod]
    public void FirstOfAllocatedOffsetsShouldBeInvalidAfterFree()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);
        heap.Free(offset1);

        Assert.AreEqual(false, heap.IsValid(offset1));
    }

    [TestMethod]
    public void SecondOfAllocatedOffsetsShouldBeInvalidAfterFree()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);
        heap.Free(offset2);

        Assert.AreEqual(false, heap.IsValid(offset2));
    }

    [TestMethod]
    public void ThirdOfAllocatedOffsetsShouldBeInvalidAfterFree()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);
        heap.Free(offset3);

        Assert.AreEqual(false, heap.IsValid(offset3));
    }

    [TestMethod]
    public void MadeUpOffsetShouldBeInvalid()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(64);
        var offset3 = heap.Alloc(32);

        Assert.AreEqual(false, heap.IsValid(offset2 + 4));
    }

    [TestMethod]
    public void HeapsShouldBeAbleToStoreRoots()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);

        var offset = heap.Alloc(128);

        heap.SetRoot("Test", offset);
        var root = heap.GetRoot("Test");

        Assert.AreEqual(offset, root);
    }

    [TestMethod]
    public void HeapsShouldBecomeFragmented()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(128);
        var offset3 = heap.Alloc(128);
        var offset4 = heap.Alloc(128);
        var offset5 = heap.Alloc(128);

        heap.Free(offset1);
        heap.Free(offset2);

        heap.Free(offset4);
        heap.Free(offset5);

        Assert.AreEqual(4, heap.CountFreeBlocks()); 
    }

    [TestMethod]
    public void HeapsShouldSupportGarbageCollection()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(128);
        var offset3 = heap.Alloc(128);
        var offset4 = heap.Alloc(128);
        var offset5 = heap.Alloc(128);
        heap.Free(offset1);
        heap.Free(offset2);
        heap.Free(offset4);
        heap.Free(offset5);

        heap.GarbageCollect();

        Assert.AreEqual(2, heap.CountFreeBlocks());
    }

    [TestMethod]
    public void HeapsShouldSupportForwardGarbageCollection()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(128);
        var offset3 = heap.Alloc(128);
        var offset4 = heap.Alloc(128);
        var offset5 = heap.Alloc(128);
        heap.Free(offset1);
        heap.Free(offset2);
        heap.Free(offset3);
        heap.Free(offset4);
        heap.Free(offset5);

        heap.GarbageCollect();

        Assert.AreEqual(1, heap.CountFreeBlocks());
        Assert.AreEqual(0, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapsShouldSupportReverseGarbageCollection()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024 * 1024);
        var offset1 = heap.Alloc(128);
        var offset2 = heap.Alloc(128);
        var offset3 = heap.Alloc(128);
        var offset4 = heap.Alloc(128);
        var offset5 = heap.Alloc(128);
        heap.Free(offset5);
        heap.Free(offset4);
        heap.Free(offset3);
        heap.Free(offset2);
        heap.Free(offset1);

        heap.GarbageCollect();

        Assert.AreEqual(1, heap.CountFreeBlocks());
        Assert.AreEqual(0, heap.CountUsedBlocks());
    }

    [TestMethod]
    public void HeapShouldBeAbleToStoreStrings()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024);

        var buffer = "Ayn Rand".AsBytes();
        var offset = heap.Alloc(buffer.Length);
        heap[offset] = buffer;

        Assert.AreEqual("Ayn Rand", heap[offset].AsString());
    }

    [TestMethod]
    public void HeapShouldProvideSizeOf()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024);

        var offset = heap.Alloc(512);
        
        Assert.AreEqual(512, heap.SizeOf(offset));
    }

    [TestMethod]
    public void HeapShouldProvideNamedRoots()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024*1024);

        var offset1 = heap.Alloc(513);
        heap.SetRoot("SPO", offset1);

        var offset2 = heap.Alloc(256);
        heap.SetRoot("PSO", offset2);

        var offset3 = heap.Alloc(88);

        var spo = heap.GetRoot("SPO");
        var pso = heap.GetRoot("PSO");

        Assert.AreEqual(offset1, spo);
        Assert.AreEqual(offset2, pso);
    }

    [TestMethod]
    public void AllocAfterFreeShouldReuseBlock()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 1024*1024);

        var firstOffset = heap.Alloc(32);
        var secondOffset = heap.Alloc(32);  
        heap.Free(firstOffset);

        var lastOffset = heap.Alloc(32);
        var size = heap.SizeOf(lastOffset);

        Assert.AreEqual(firstOffset, lastOffset);
    }

    [TestMethod]
    public void CloseCallsShouldWork()
    {
        var stream = new MemoryStream();
        var heap = new Heap(stream, 8 * sizeof(long) + 10);
        var offset = heap.Alloc(8);
    }
}
