/*

  MIT License
 
  Copyright (c) 2022 Canyala Innovation (Martin Fredriksson)

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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

using Canyala.Lagoon.Collections;
using Canyala.Lagoon.Contracts;
using Canyala.Lagoon.Expressions;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

using Canyala.Mercury.Core;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Storage.Collections;

[assembly: InternalsVisibleToAttribute("Canyala.Mercury.Test")]

namespace Canyala.Mercury.Rdf;

/// <summary>
/// Provides a query abstraction.
/// </summary>
public partial class Query
{
    internal Specification Plan { get; set; }

    /// <summary>
    /// 
    /// </summary>
    internal Query()
        { Plan = Specification.Empty; }

    /// <summary>
    /// Enumerates a dataset according to this query.
    /// </summary>
    /// <param name="dataset">The dataset to enumerate.</param>
    /// <returns>A table</returns>
    public Table AsTable(Dataset dataset)
    {
        if (Plan == Specification.Empty)
            return Table.Empty;

        dataset.SetActiveGraph(dataset.NameOfDefault);

        return AsTable(dataset, Plan.Groups);
    }

    /// <summary>
    /// Enumerates a dataset according to this query.
    /// </summary>
    /// <param name="dataset">The dataset to enumerate.</param>
    /// <returns>A sequence of dictionaries.</returns>
    public IEnumerable<IDictionary<string, string>> AsDictionaries(Dataset dataset)
    {
        if (Plan == Specification.Empty)
            yield break;

        dataset.SetActiveGraph(dataset.NameOfDefault);

        var table = AsTable(dataset, Plan.Groups);

        foreach (var row in table.Rows)
        {
            yield return table.Columns.ToDictionary(column => column.Key, column => row[column.Value]);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="groups"></param>
    /// <returns></returns>
    private Table AsTable(Dataset dataset, IEnumerable<Group> groups, IDictionary<string, string>? preBindings = null)
    {
        Table table = Table.Empty;

        foreach (var group in groups)
        {
            switch (group.Operation)
            {
                case "":
                    #region ""
                    if (group.Clauses.Count > 0)
                    {
                        if (table == Table.Empty)
                            table = AsTable(dataset.Active!, group, preBindings);
                        else
                            table = table.Intersection(AsTable(dataset.Active!, group));
                    }
                    else
                    {
                        if (table == Table.Empty)
                            table = AsTable(dataset, group.Groups, preBindings);
                        else
                            table = table.Intersection(AsTable(dataset, group.Groups, preBindings));
                    }

                    if (group.ExplicitBindAsVars.Count > 0 && group.ExplicitBindAsVars.Count == group.ExplicitBinders.Count)
                        table = table.Bind(group.ExplicitBindAsVars.Select(var => var.Value), group.ExplicitBinders);

                    if (group.Filters.Count > 0)
                        table = table.FilterBy(group.Filters);

                    break;
                    #endregion

                case "ASK":
                    #region ASK
                    Contract.Assume(table == Table.Empty, "Internal logic error, table should be null.");

                    table = AsTable(dataset, group.Groups, preBindings);

                    if (group.GroupByVars.Count > 0) 
                    {
                        foreach (var part in table.GroupBy(group.GroupByVars.Select(grp => grp.Value)))
                        {
                            // TODO: Create an aggregated table on the parts using aggregate expressions (SUM, MIN, MAX etc)
                        }
                    }

                    var rows = Seq.Of(Seq.Array(Literal.From(table.Rows.Any()).ToString()));
                    var columns = new Dictionary<string, int> { { "ask", 0 } };
                    table = new Table(rows, columns);

                    break;
                    #endregion

                case "SELECT":
                    #region SELECT
                    Contract.Assume(table == Table.Empty, "Internal error: table should be null.");
                    Contract.Assume(group.SelectAsVars.Count == group.SelectBinders.Count, "Internal error: SelectAsVar and SelectBinders count mismatch.");
                    Contract.Assume(group.ExplicitBindAsVars.Count == group.ExplicitBinders.Count, "Internal error: SelectAsVar and SelectBinders count mismatch.");

                    table = AsTable(dataset, group.Groups, preBindings);

                    var bindings = group.ImplicitBindAsVars.Select(var => var.Value);
                    var binders = group.ImplicitBinders;

                    if (bindings.Any())
                    {
                        table = table.Bind(bindings, binders);
                    }

                    if (group.AggregateVars.Count > 0) 
                    {
                        var groupByVars = group.GroupByVars.Select(var => var.Value);
                        var aggregateVars = group.AggregateVars.Select(var => var.Value);
                        var aggregators = group.AggregateBinders;
                        var distincts = group.AggregateDistincts;

                        table = table.Aggregate(groupByVars, aggregateVars, aggregators, distincts, BuildCompositeKey);

                        if (group.Filters.Count > 0)
                           table = table.FilterBy(group.Filters);
                    }

                    bindings = group.SelectAsVars.Select(var => var.Value);
                    binders = group.SelectBinders;

                    if (bindings.Any())
                    {
                        table = table.Bind(bindings, binders);
                    }

                    if (group.OrderByVars.Count > 0)
                    {
                        Contract.Assume(group.OrderByDescends.Count == group.OrderByVars.Count, "Internal: OrderBy vars & descends mismatch.");

                        var descends = group.OrderByDescends;
                        var variables = group.OrderByVars.Select(var => var.Value);

                        table = table.OrderBy(variables, descends, RdfCompare);
                    }

                    table = table.Select(group.Variables.Select(var => var.Value));

                    if (group.Distinct) 
                        table = table.Distinct();

                    if (group.Offset >= 0)
                        table = new Table(table.Rows.Skip(group.Offset), table.Columns);

                    if (group.Limit >= 0)
                        table = new Table(table.Rows.Take(group.Limit), table.Columns);

                    break;
                    #endregion

                case "UNION":
                    #region UNION
                    Contract.Assume(table != Table.Empty, "Internal logic error, table should not be null.");
                    table = table.Union(AsTable(dataset, group.Groups, preBindings));
                    break;
                    #endregion

                case "OPTIONAL":
                    #region OPTIONAL
                    Contract.Assume(table != Table.Empty, "Internal logic error, table should not be null.");
                    table = table.Optional(AsTable(dataset, group.Groups, preBindings));
                    break;
                    #endregion

                case "MINUS":
                    #region MINUS
                    Contract.Assume(table != Table.Empty, "Internal logic error, table should not be null.");
                    table = table.Minus(AsTable(dataset, group.Groups, preBindings));
                    break;
                    #endregion

                case "VALUES":
                    #region VALUES
                    var index = 0;
                    var values = new Table(group.Values, group.Variables.ToDictionary(var => var.Value, var => index++));

                    if (table == Table.Empty)
                        table = values;
                    else
                        table = table.Intersection(values);
                    break;
                    #endregion

                case "CONSTRUCT":
                    #region CONSTRUCT
                    table = Construct(table, group.Clauses);
                    break;
                    #endregion

                case "EXISTS":
                #region EXISTS
                    table = FilterExists(dataset, table, group.Groups);
                    break;
                #endregion

                case "NOTEXISTS":
                    #region NOTEXISTS
                    table = FilterNotExists(dataset, table, group.Groups);
                    break;
                    #endregion

                default:
                    Contract.Assume(false, "Internal logic error. Unknown group specifier: '{0}'.".Args(group.Operation));
                    break;
            }
        }

        return table;
    }

    private static string BuildCompositeKey(string[] row, IEnumerable<int> indexMap)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append('|');

        foreach (var index in indexMap)
        {
            var resource = Resource.Parse(row[index]);

            if (!resource.IsBound())
                keyBuilder.Append("0 ");
            else if (resource.IsBlank())
                keyBuilder.Append("1 ");
            else if (resource.IsIri())
                keyBuilder.Append("2 ");
            else
                keyBuilder.Append("3 ");

            keyBuilder.Append(resource.Value);
            keyBuilder.Append('|');
        }

        return keyBuilder.ToString();
    }

    private int RdfCompare(string lhs, string rhs)
    {
        return Plan.Operators.Compare(Resource.Parse(lhs, Plan.Namespaces), Resource.Parse(rhs, Plan.Namespaces));
    }

    private Table FilterExists(Dataset dataset, Table table, IEnumerable<Group> existsGroups)
    {
        return new Table(FilterExistsYielder(dataset, table, existsGroups), table.Columns);
    }

    private IEnumerable<string[]> FilterExistsYielder(Dataset dataset, Table table, IEnumerable<Group> existsGroups)
    {
        foreach (var row in table.Rows)
        {
            var preBindings = table.Columns.Keys.ToDictionary(name => name, name => row[table.Columns[name]]);
            var existsTable = AsTable(dataset, existsGroups, preBindings);

            if (existsTable.Rows.Any())
                yield return row;
        }
    }

    private Table FilterNotExists(Dataset dataset, Table table, IEnumerable<Group> existsGroups)
    {
        return new Table(FilterNotExistsYielder(dataset, table, existsGroups), table.Columns);
    }

    private IEnumerable<string[]> FilterNotExistsYielder(Dataset dataset, Table table, IEnumerable<Group> existsGroups)
    {
        foreach (var row in table.Rows)
        {
            var preBindings = table.Columns.Keys.ToDictionary(name => name, name => row[table.Columns[name]]);
            var existsTable = AsTable(dataset, existsGroups, preBindings);

            if (!existsTable.Rows.Any())
                yield return row;
        }
    }

    private Table Construct(Table table, IEnumerable<Term?[]> clauses)
    {
        var columns = new Dictionary<string,int>() { {"s", 0}, {"p", 1}, {"o", 2} };
        return new Table(ConstructYielder(table, clauses), columns);
    }

    private IEnumerable<string[]> ConstructYielder(Table table, IEnumerable<Term?[]> clauses)
    {
        foreach (var row in table.Rows)
        {
            var blanks = new Dictionary<string, string>();

            foreach (var clause in clauses)
            {
                var newRow = new string[3];

                for (int i = 0; i < clause.Length; i++)
                {
                    var term = clause[i];

                    var variable = term as Variable;

                    if (variable != null)
                    {
                        newRow[i] = row[table.Columns[variable.Value]];
                        continue;
                    }

                    var blank = term as Blank;

                    if (blank != null)
                    {
                        if (!blanks.TryGetValue(blank.Value, out var substitute))
                        {
                            substitute = Blank.NewBlank();
                            blanks.Add(blank.Value, substitute!);
                        }
                        newRow[i] = substitute!;
                    }
                    else
                        newRow[i] = term!;
                }

                yield return newRow;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="group"></param>
    /// <returns>A table representing group clause intersections.</returns>
    private Table AsTable(Graph graph, Group group, IDictionary<string, string>? preBindings = null)
    {
        Contract.Assume(group.Clauses.Count > 0, "Internal logic error, no clauses for triple graph query.");

        var clauses = group.Clauses.OrderBy(clause => clause.Count(term => term is Variable));
        var setConstraints = new Dictionary<string, Constraint>();
        var table = Table.Empty;

        foreach (var clause in clauses)
        {
            var map = new Dictionary<string, int>();
            var args = new Constraint[3];
            var vars = new string[3];
            int index = 0;

            for (int i = 0; i < 3; i++)
            {
                var variable = clause[i] as Variable;

                if (variable != null)
                {
                    if (preBindings != null && preBindings.TryGetValue(variable.Value, out var preBound))
                        args[i] = preBound;
                    else
                    {
                        vars[index] = variable.Value;
                        map.Add(variable.Value, index++);

                        // TODO: ? if (!setConstraints.TryGetValue(variable.Value, out args[i]))
                        args[i] = Constraint.Empty;
                    }
                }
                else
                    args[i] = clause![i]!.ToString()!; 
            }

            var solution = graph.Enumerate(args[0], args[1], args[2]);

            // TODO: ? for (int i = 0; i < index; i++) setConstraints[vars[i]] = solution[i];

            table = table == Table.Empty ? new Table(solution, map) : table.Intersection(new Table(solution, map));
        }

        return table;
    }
}
