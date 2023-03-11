/*

  MIT License
 
  Copyright (c) 2012-2022 Canyala Innovation (Martin Fredriksson)

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
using System.Linq;
using System.Text;

namespace Canyala.Mercury.Storage;

/// <summary>
/// Provides an interface for objects that are persisted in a heap.
/// </summary>
/// <remarks>
/// Classes implementing 'Object' is full class citizens in the world
/// of heaps. This means that they live in persistent storage and they 
/// will be managed as reference types in the heap, not as value types. 
/// A similar situation exists between value types and reference types 
/// in the CLR managed heap; the persistent world
/// of heaps takes the concept a bit further.
/// </remarks>
public abstract class Object : IDisposable
{
    /// <summary>
    /// Provides the heap offset of an Object.
    /// </summary>
    internal abstract long Offset { get; }
    
    internal abstract void AddReference();
    internal abstract void Release();

    private bool disposed = false;

    public void Dispose()
    {
        Console.WriteLine($"Object.Dispose()");
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        try
        {
            if (!disposed)
            {
                Release();
            }
        }
        finally
        {
            disposed = true;
        }
    }

    ~Object()
    {
        Dispose(false);
    }
}
