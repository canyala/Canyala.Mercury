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
using Canyala.Mercury.Core.Text;

namespace Canyala.Mercury.Lua;

/// <summary>
/// Implements a parser for Lua script.
/// </summary>
/// <remarks>
/// The Lua class is partial because the inner Lua.Producer class is implemented in
/// its own file, Lua.Producer.cs
/// </remarks>
/// <seealso cref=""/>
public partial class Lua : Parser<Lua.Producer>
{
    #region Grammar Production Declarations

    // <summary>
    /// Productions declare Lua production rules
    /// based on the bnf grammar section at TBD
    /// </summary>
    private class Productions
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly Func<Production> luaDoc = () => _(nameof(luaDoc), NotImplemented);

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @NotImplemented = () => Call((_, _) => throw new NotImplementedException());
    }

    #endregion

    #region Internal Construction

    private static readonly Lua Instance = new Lua();

    /// <summary>
    /// Creates the Lua parsing singleton.
    /// </summary>
    private Lua()
        : base(Productions.luaDoc)
    { /* No implementation */ }

    #endregion
}

