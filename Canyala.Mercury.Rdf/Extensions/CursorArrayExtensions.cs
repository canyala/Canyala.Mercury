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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Canyala.Mercury.Rdf.Extensions
{
    /*
    public static class CursorArrayExtensions
    {
        public static int MoveNext(this Cursor[] cursors, List<Dictionary<string, string>> unifiedBindings, int i)
        {
            unifiedBindings.RemoveAt(i);

            if (i == 0)
            {
                if (!cursors[i].MoveNext())
                    return -1;
            }
            else if (!cursors[i].MoveNext(unifiedBindings[i - 1]))
            {
                cursors[i].Reset();
                cursors[i].MoveNext(unifiedBindings[i - 1]);

                return cursors.MoveNext(unifiedBindings, i - 1);
            }

            return i;
        }

        public static bool MoveNext(this Cursor[] cursors)
        {
            var more = false;
            foreach (var cursor in cursors)
                if (cursor.MoveNext())
                    more = true;

            return more;
        }
    }
    */
}
