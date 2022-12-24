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
    public static class EnumeratorArrayExtensions
    {
        public static int MoveNext<T>(this IEnumerator<T>[] enumerators, List<Dictionary<string, string>> unifiedBindings, int i)
        {
            unifiedBindings.RemoveAt(i);

            if (i == 0)
            {
                if (!enumerators[i].MoveNext())
                    return -1;
            }
            else if (!enumerators[i].MoveNext())
            {
                enumerators[i].Reset();
                enumerators[i].MoveNext();

                return enumerators.MoveNext(unifiedBindings, i - 1);
            }

            return i;
        }

        public static bool MoveNext<T>(this IEnumerator<T>[] enumerators)
        {
            foreach (var enumerator in enumerators)
                if (!enumerator.MoveNext())
                    return false;

            return true;
        }
    }
}
