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

using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Internal;

namespace Canyala.Mercury.Rdf
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Query
    {
        /// <summary>
        /// 
        /// </summary>
        internal class Specification
        {
            public Namespaces Namespaces { get; private set; }
            public List<Group> Groups { get; private set; }
            public BuiltIns BuiltIns { get; private set; }
            public Operators Operators { get; private set; }

            public Specification()
            {
                Groups = new List<Group>();
                Namespaces = new Namespaces();
                Operators = new Operators();
                BuiltIns = new BuiltIns();

                Operators.Namespaces = Namespaces;
                BuiltIns.Namespaces = Namespaces;
                BuiltIns.Operators = Operators;
            }

            internal static Specification Empty = new Specification();
        }
    }
}
