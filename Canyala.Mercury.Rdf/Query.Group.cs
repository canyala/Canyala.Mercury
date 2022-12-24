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

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

namespace Canyala.Mercury.Rdf
{
    /// <summary>
    /// Provides ...
    /// </summary>
    public partial class Query
    {
        /// <summary>
        /// Provides ...
        /// </summary>
        internal class Group
        {
            public List<Variable> Variables { get; private set; }
            public List<Term[]> Clauses { get; private set; }
            public List<Group> Groups { get; private set; }
            public List<String[]> Values { get; private set; }
            public string Operation { get; private set; }

            public List<Func<Func<string, string>, bool>> Filters { get; set; }

            public List<Func<Func<string, string>, string>> SelectBinders { get; set; }
            public List<Variable> SelectAsVars { get; set; }

            public List<Func<Func<string, string>, string>> ExplicitBinders { get; set; }
            public List<Variable> ExplicitBindAsVars { get; set; }

            public List<Func<Func<string, string>, string>> ImplicitBinders { get; set; }
            public List<Variable> ImplicitBindAsVars { get; set; }

            public List<Func<Func<string, string>, string, HashSet<string>, string>> AggregateBinders { get; private set; }
            public List<Variable> AggregateVars { get; private set; }
            public List<bool> AggregateDistincts { get; private set; }

            public List<Variable> GroupByVars { get; private set; }

            public List<Variable> OrderByVars { get; private set; }
            public List<bool> OrderByDescends { get; private set; }

            public int Limit { get; set; }
            public int Offset { get; set; }

            public bool SelectAll { get; set; }

            public bool Distinct { get; private set; }

            private Group()
            {
                Filters = new List<Func<Func<string, string>, bool>>();
                Variables = new List<Variable>();

                SelectBinders = new List<Func<Func<string, string>, string>>();
                SelectAsVars = new List<Variable>();

                ExplicitBinders = new List<Func<Func<string, string>, string>>();
                ExplicitBindAsVars = new List<Variable>();

                ImplicitBinders = new List<Func<Func<string, string>, string>>();
                ImplicitBindAsVars = new List<Variable>();

                AggregateDistincts = new List<bool>();
                AggregateVars = new List<Variable>();
                AggregateBinders = new List<Func<Func<string, string>, string, HashSet<string>, string>>();

                GroupByVars = new List<Variable>();

                Clauses = new List<Term[]>();
                Groups = new List<Group>();
                Values = new List<string[]>();
                Operation = String.Empty;

                OrderByVars = new List<Variable>();
                OrderByDescends = new List<bool>();
                Distinct = false;
                Limit = -1;
                Offset = -1;
            }

            public Group(string operation, List<Variable> variables,
                List<Func<Func<string, string>, string>> selectBinders, List<Variable> selectAsVars,
                List<Func<Func<string, string>, string>> implicitBinders, List<Variable> implicitBindAsVars,
                List<Func<Func<string, string>, string, HashSet<string>, string>> aggregateBinders, List<Variable> aggregateVars, List<bool> aggregateDistincts,
                bool distinct) : this()
            {
                Variables = new List<Variable>(variables);

                SelectBinders = new List<Func<Func<string,string>,string>>(selectBinders);
                SelectAsVars = new List<Variable>(selectAsVars);

                ImplicitBinders = new List<Func<Func<string, string>, string>>(implicitBinders);
                ImplicitBindAsVars = new List<Variable>(implicitBindAsVars);

                AggregateBinders = new List<Func<Func<string, string>, string, HashSet<string>, string>>(aggregateBinders);
                AggregateVars = new List<Variable>(aggregateVars);
                AggregateDistincts = new List<bool>(aggregateDistincts);

                Operation = operation;
                Distinct = distinct;
            }

            public Group(string operation, List<Term[]> clauses) : this()
            {
                Clauses = new List<Term[]>(clauses);
                Operation = operation;
            }

            public Group(Group parent) : this()
            {
                Clauses.AddRange(parent.Clauses);
                ExplicitBindAsVars.AddRange(parent.ExplicitBindAsVars);
                ExplicitBinders.AddRange(parent.ExplicitBinders);
                parent.ExplicitBindAsVars.Clear();
                parent.ExplicitBinders.Clear();
                Filters.AddRange(parent.Filters);
                parent.Clauses.Clear();
                parent.Filters.Clear();
            }

            public override string ToString()
            {
                return "{0} Cl: {1}, Grp: {2}, Flt: {3}, Var:{4}, Bind: {5}, SBind: {6}, GrpBy: {7}"
                    .Args(Operation.IsEmpty() ? "EMPTY" : Operation, Clauses.Count, Groups.Count, Filters.Count, Variables.Count, ExplicitBindAsVars.Count, SelectAsVars.Count, GroupByVars.Count);
            }
        }
    }
}
