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
    /// Implements a single store in file heap factory closure.
    /// </summary>
    internal class SinglestoreInFile : Strategy
    {
        public SinglestoreInFile(int heapSize, string filePath)
        {
            HeapFactory = HeapFactoryClosure;
            HeapSize = heapSize;
            FilePath = filePath;
        }

        private Heap _singleHeap;

        public int HeapSize { get; set; }

        public string FilePath { get; set; }

        public Heap HeapFactoryClosure(Type type)
        {
            if (_singleHeap == null)
            {
                if (!File.Exists(FilePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    _singleHeap = new Heap(new FileStream(FilePath, FileMode.OpenOrCreate), HeapSize);
                } 
                else
                {
                    if (new FileInfo(FilePath).Length == 0)
                        _singleHeap = new Heap(new FileStream(FilePath, FileMode.OpenOrCreate), HeapSize);
                    else
                        _singleHeap = new Heap(new FileStream(FilePath, FileMode.OpenOrCreate));
                }
            }

            return _singleHeap;
        }

        public override void Remove()
        {
            File.Delete(FilePath);
        }
    }
}
