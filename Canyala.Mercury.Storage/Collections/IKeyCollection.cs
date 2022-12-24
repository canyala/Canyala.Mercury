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

namespace Canyala.Mercury.Storage.Collections
{
    public interface IKeyCollection<T>
    {
        T Min { get; }
        T Max { get; }
        long Magnitude { get; }
        bool Contains(T element);
        IEnumerable<T> Between(T low, T high);
        IEnumerable<T> Enumerate();
    }
}
