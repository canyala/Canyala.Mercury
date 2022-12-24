//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Canyala.Mercury.Internal;
using Canyala.Mercury.Storage;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class HeapIndexTest
    {
        [TestMethod]
        public void AddAndRemove()
        {
            var environment = Storage.Environment.Create();
            var index = new HeapIndex(environment);

            index.Add("a", "b", "c");
            index.Add("a", "bc", "c");
            index.Remove("a", null, "c");
            index.Add("a", "bc", "c");
            index.Add("a", "b", "c");
        }
    }
}
