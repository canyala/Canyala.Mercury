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
    /// Implements a poly store in memory heap factory closure.
    /// </summary>
    internal class PolystoreInMemory : Strategy
    {
        public int HeapSize { get; set; }

        public PolystoreInMemory(int heapSize)
        {
            HeapFactory = HeapFactoryClosure;
            HeapSize = heapSize;
        }

        private Heap HeapFactoryClosure(Type type)
            { return new Heap(new MemoryStream(), HeapSize); }

        public override void Remove()
        {
        }
    }
}
