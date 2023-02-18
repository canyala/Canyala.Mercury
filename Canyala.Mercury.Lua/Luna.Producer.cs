/*

  MIT License
 
  Copyright (c) 2023 Canyala Innovation (Martin Fredriksson)

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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury.Core;
using Canyala.Mercury.Core.Text;

namespace Canyala.Mercury.Luna;

/// <summary>
/// Implements a code producer for running Luna scripts on the .NET CORE CLR.
/// </summary>
/// <remarks>
/// This class is partial because the inner Luna.Producer class is implemented in
/// this file, the Luna class is implemented in Luna.cs.
/// </remarks>
/// <seealso cref=""/>
public partial class Luna
{
    public class Producer : IEnumerable<string[]>, IDisposable
    {
        #region State
        #endregion

        #region Construction

        /// <summary>
        /// Create a Luna producer.
        /// </summary>
        /// <param name="parser">The turtle parse instance.</param>
        /// <param name="turtleLines">A turtle document as a sequence of text line strings.</param>
        internal Producer(Luna parser, IEnumerable<string> luaLines)
        {
            /*
            Emitters.Push(DefaultEmitter);
            Setters.Push(BlankNodeExceptionSetter);
            TurtleLines = turtleLines;
            Parser = parser;
            */
        }

        #endregion

        #region Production Rule State Appliers
        #endregion

        #region Enumeration implementation

        IEnumerator<string[]> IEnumerable<string[]>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        public void Dispose()
        {
            // No implementation.
        }
    }
}


