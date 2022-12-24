//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Canyala.Mercury.Storage.Strategies.Internal
{
    /// <summary>
    /// Implements a single store in memory heap factory closure.
    /// </summary>
    internal class SinglestoreInMemory : Strategy
    {
        public SinglestoreInMemory(int heapSize)
        {
            HeapFactory = HeapFactoryClosure;
            HeapSize = heapSize;
        }

        private Heap _singleHeap;

        public int HeapSize { get; set; }

        public Heap HeapFactoryClosure(Type type)
        {
            if (_singleHeap == null)
                _singleHeap = new Heap(new MemoryStream(), HeapSize);

            return _singleHeap;
        }

        public override void Remove()
        {
            _singleHeap = null;
        }
    }
}
