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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Canyala.Mercury.Storage.Strategies.Internal;

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

    private Heap? _singleHeap;

    public int HeapSize { get; set; }

    public string FilePath { get; set; }

    public Heap HeapFactoryClosure(Type type)
    {
        if (_singleHeap == null)
        {
            if (!File.Exists(FilePath))
            {
                string? directory = Path.GetDirectoryName(FilePath);
                if (!String.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
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
