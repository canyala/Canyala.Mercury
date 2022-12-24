//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

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
