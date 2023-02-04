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
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

namespace Canyala.Mercury.Core;

/// <summary>
/// Provides a table abstraction for string array sequences.
/// </summary>
public class Table : IEnumerable<string[]>
{
    /// <summary>
    /// The empty table.
    /// </summary>
    public static Table Empty = new Table(Seq.Empty<string[]>(), new Dictionary<string,int>());

    /// <summary>
    /// The sequence of rows of this table.
    /// </summary>
    public IEnumerable<string[]> Rows { get; private set; }

    /// <summary>
    /// The column name / row index bindings of this table.
    /// </summary>
    public IDictionary<string,int> Columns { get; private set; }

    /// <summary>
    /// Create a table from a sequence of string arrays and a column name / index map.
    /// </summary>
    /// <param name="rows">The rows of the table.</param>
    /// <param name="columns">The columns of the table.</param>
    public Table(IEnumerable<string[]> rows, IDictionary<string,int> columns)
    {
        Rows = rows;
        Columns = columns;
    }

    /// <summary>
    /// Projects a table.
    /// </summary>
    /// <param name="selects">Columns to select.</param>
    /// <returns>A projected table.</returns>
    public Table Select(params string[] selects)
    {
        var columns = BuildColumns(selects);
        return new Table(SelectYielder(this, columns), columns);
    }

    /// <summary>
    /// Projects a table.
    /// </summary>
    /// <param name="selects">Columns to select.</param>
    /// <returns>A projected table.</returns>
    public Table Select(IEnumerable<string> selects)
    {
        var columns = BuildColumns(selects);
        return new Table(SelectYielder(this, columns), columns);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Table Distinct()
        { return new Table(DistinctYielder(this), this.Columns); }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Table Reduce()
        { return new Table(DistinctYielder(this), this.Columns); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public Table OrderBy(params string[] columns)
        { return new Table(OrderByYielder(this, columns, Seq.Allways(false), DefaultCompare), this.Columns); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public Table OrderBy(IEnumerable<string> columns, IEnumerable<bool> descends, Func<string, string, int>? comparer = null)
        { return new Table(OrderByYielder(this, columns, descends, comparer), this.Columns); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="selects"></param>
    /// <returns></returns>
    public IEnumerable<Table> GroupBy(params string[] selects)
        { return GroupByYielder(this, selects, null); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="selects"></param>
    /// <returns></returns>
    public IEnumerable<Table> GroupBy(IEnumerable<string> selects, Func<string[],IEnumerable<int>,string>? keyBuilder = null)
        { return GroupByYielder(this, selects, keyBuilder); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="table"></param>
    /// <param name="groupBy"></param>
    /// <param name="aggregates"></param>
    /// <param name="aggregators"></param>
    /// <returns></returns>
    public Table Aggregate(IEnumerable<string> groupBy, 
                           IEnumerable<string> aggregates, IEnumerable<Func<Func<string, string>, string, HashSet<string>, string>> aggregators, IEnumerable<bool> distincts,
                           Func<string[],IEnumerable<int>,string> keyBuilder)
    {
        int index = 0;
        var aggregatedColumns = aggregates.ToDictionary(var => var, var => index++);
        return new Table(AggregateYielder(this, groupBy, aggregatedColumns.Count, aggregators, distincts, keyBuilder), aggregatedColumns);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expressions"></param>
    /// <returns></returns>
    public Table FilterBy(params Func<Func<string, string>, bool>[] expressions)
        { return new Table(FilterYielder(this, expressions), this.Columns); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expressions"></param>
    /// <returns></returns>
    public Table FilterBy(IEnumerable<Func<Func<string, string>, bool>> expressions)
        { return new Table(FilterYielder(this, expressions), this.Columns); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bindings"></param>
    /// <returns></returns>
    public Table Bind(IEnumerable<string> bindings, IEnumerable<Func<Func<string, string>, string>> binders)
    {
        var columns = new Dictionary<string, int>(this.Columns);
        foreach (var binding in bindings) columns.Add(binding, columns.Count);
        return new Table(BindYielder(this, binders, columns), columns);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <param name="selectVars"></param>
    /// <returns></returns>
    public Table Intersection(Table other, params string[] selects)
    { 
        var columns = BuildColumns(selects, this.Columns, other.Columns);
        return new Table(IntersectionYielder(this, other, columns), columns); 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <param name="selects"></param>
    /// <returns></returns>
    public Table Union(Table other, params string[] selects)
    {
        var columns = BuildColumns(selects, this.Columns, other.Columns);
        return new Table(UnionYielder(this, other, columns), columns); 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <param name="selectVars"></param>
    /// <returns></returns>
    public Table Optional(Table other, params string[] selects)
    {
        var columns = BuildColumns(selects, this.Columns, other.Columns);
        return new Table(OptionalYielder(this, other, columns), columns); 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <param name="selects"></param>
    /// <returns></returns>
    public Table Minus(Table other, params string[] selects)
    {
        var columns = BuildColumns(selects, this.Columns, other.Columns);
        return new Table(MinusYielder(this, other, columns), columns); 
    }

    #region Private Implementation

    /// <summary>
    /// 
    /// </summary>
    /// <param name="this"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> SelectYielder(Table @this, IDictionary<string,int> columns)
    {
        var selectMap = new List<Tuple<int,int>>();

        foreach (var column in columns)
        {
            if (@this.Columns.TryGetValue(column.Key, out int columnIndex))
                selectMap.Add(Tuple.Create(column.Value, columnIndex));
        }

        foreach (var thisRow in @this.Rows)
        {
            var row = new string[columns.Count];

            foreach (var map in selectMap)
                row[map.Item1] = thisRow[map.Item2];

            yield return row;
       }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="this"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> DistinctYielder(Table @this)
    {
        var set = new HashSet<string>();

        foreach (var row in @this.Rows)
        {
            if (set.Count > 10000)
                set.Clear();

            var rowKey = String.Concat('|', row.Join('|'), '|');

            if (!set.Contains(rowKey))
            {
                yield return row;
                set.Add(rowKey);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="this"></param>
    /// <param name="groupByVars"></param>
    /// <returns></returns>
    private static IEnumerable<Table> GroupByYielder(Table @this, IEnumerable<string> groupByVars, Func<string[], IEnumerable<int>, string>? keyBuilder)
    {
        if (!groupByVars.Any())
        {
            yield return @this;
            yield break;
        }

        var groupMap = new Dictionary<string, List<string[]>>();
        var keyMap = groupByVars.Select(var => @this.Columns[var]);
        keyBuilder = keyBuilder ?? DefaultBuildCompositeKey;

        foreach (var row in @this.Rows)
        {
            var key = keyBuilder(row, keyMap);

            if (!groupMap.TryGetValue(key, out var group))
            {
                group = new List<string[]>();
                groupMap.Add(key, group);
            }

            group.Add(row);
        }

        foreach (var group in groupMap.Values)
            yield return new Table(group, @this.Columns);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="table"></param>
    /// <param name="groupBy"></param>
    /// <param name="aggregatedColumns"></param>
    /// <param name="aggregators"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> AggregateYielder(Table table, IEnumerable<string> groupBy, 
        int aggregatedColumnsCount, IEnumerable<Func<Func<string, string>, string, HashSet<string>, string>> aggregators, IEnumerable<bool> distincts,
        Func<string[],IEnumerable<int>,string> keyBuilder)
    {

        foreach (var group in table.GroupBy(groupBy, keyBuilder))
        {
            var distinctSets = distincts.Select(distinct => distinct ? new HashSet<string>() : null).ToArray();
            var aggregatedRow = new string[aggregatedColumnsCount];

            var isEmpty = true;
            foreach (var row in group.Rows)
            {
                int index = 0;
                foreach (var aggregate in aggregators)
                    aggregatedRow[index] = aggregate(var => var == "*" ? BuildRowKey(row) : row[table.Columns[var]], aggregatedRow[index], distinctSets[index++]!);
                
                isEmpty = false;
            }

            if (isEmpty)
                yield break;

            for (int i = 0; i < aggregatedRow.Length; i++)
                if (aggregatedRow[i] == null) aggregatedRow[i] = String.Empty;

            yield return aggregatedRow;
        }
    }

    private static string BuildRowKey(string[] row)
        { return string.Concat('|', row.Join('|'), '|'); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="this"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> OrderByYielder(Table @this, IEnumerable<string> columns, IEnumerable<bool> descends, Func<string,string,int>? comparer)
    {
        comparer = comparer ?? DefaultCompare;
        var list = @this.Rows.Select(row => Tuple.Create(row, 0)).ToList();
        var map = columns.Zip(descends, (column, descend) => Tuple.Create(@this.Columns[column], descend ? -1 : 1)).ToList();

        for (int i=map.Count-1; i>=0; i--)
        {
            for (int key=0; key<list.Count; key++) 
                list[key] = Tuple.Create(list[key].Item1, key);

            Sort(list, (t1, t2) => StableCompare(t1, t2, map[i].Item1, map[i].Item2, comparer));
        }

        return list.Select(row => row.Item1);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="this"></param>
    /// <param name="expressions"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> FilterYielder(Table @this, IEnumerable<Func<Func<string, string>, bool>> expressions)
        { return @this.Rows.Where(row => expressions.All(filter => filter(var => row[@this.Columns[var]]))); }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="this"></param>
    /// <param name="bindings"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> BindYielder(Table @this, IEnumerable<Func<Func<string, string>, string>> expressions, IDictionary<string,int> columns)
    {
        int oldCount = @this.Columns.Count;

        foreach (var row in @this.Rows)
        {
            var output = new string[columns.Count];

            for (int i=0; i<oldCount; i++)
                output[i] = row[i];

            int index = oldCount;
            foreach (var expression in expressions)
                output[index++] = expression(var => output[columns[var]]);

            yield return output;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> IntersectionYielder(Table left, Table right, IDictionary<string,int> columns)
    {
        BuildMaps(left, right, columns, out var intersectionMap, out var rightMap, out var leftMap);

        foreach (var leftRow in left.Rows)
        {
            foreach (var rightRow in right.Rows)
            {
                if (intersectionMap.All(map => IsMatch(leftRow[map.Item1], rightRow[map.Item2])))
                {
                    var row = new string[columns.Count];

                    foreach (var map in leftMap)
                        row[map.Item1] = leftRow[map.Item2];

                    foreach (var map in rightMap)
                        row[map.Item1] = rightRow[map.Item2];

                    yield return row;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> UnionYielder(Table left, Table right, IDictionary<string, int> columns)
    {
        BuildMaps(left, right, columns, out var intersectionMap, out var rightMap, out var leftMap, true);

        foreach (var leftRow in left.Rows)
        {
            var row = Seq.Array(columns.Count, String.Empty);

            foreach (var map in leftMap)
                row[map.Item1] = leftRow[map.Item2];
            
            yield return row;
        }

        foreach (var rightRow in right.Rows)
        {
            var row = Seq.Array(columns.Count, String.Empty);

            foreach (var map in rightMap)
                row[map.Item1] = rightRow[map.Item2];

            yield return row;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> OptionalYielder(Table left, Table right, IDictionary<string, int> columns)
    {
        BuildMaps(left, right, columns, out var intersectionMap, out var rightMap, out var leftMap);

        foreach (var leftRow in left.Rows)
        {
            bool addOptional = true;

            foreach (var rightRow in right.Rows)
            {
                if (intersectionMap.All(map => IsMatch(leftRow[map.Item1], rightRow[map.Item2])))
                {
                    addOptional = false;

                    var row = new string[columns.Count];

                    foreach (var map in leftMap)
                        row[map.Item1] = leftRow[map.Item2];

                    foreach (var map in rightMap)
                        row[map.Item1] = rightRow[map.Item2];

                    yield return row;
                }
            }

            if (addOptional)
            {
                var row = Seq.Array(columns.Count, String.Empty);

                foreach (var map in leftMap)
                    row[map.Item1] = leftRow[map.Item2];

                yield return row;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    private static IEnumerable<string[]> MinusYielder(Table left, Table right, IDictionary<string, int> columns)
    {
        BuildMaps(left, right, columns, out var intersectionMap, out var rightMap, out var leftMap);

        foreach (var leftRow in left.Rows)
        {
            foreach (var rightRow in right.Rows)
            {
                if (intersectionMap.Any(map => !IsMatch(leftRow[map.Item1], rightRow[map.Item2])))
                {
                    var row = new string[columns.Count];

                    foreach (var map in leftMap)
                        row[map.Item1] = leftRow[map.Item2];

                    foreach (var map in rightMap)
                        row[map.Item1] = rightRow[map.Item2];

                    yield return row;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="selects"></param>
    /// <returns></returns>
    private static IDictionary<string, int> BuildColumns(IEnumerable<string> selects, IDictionary<string, int>? thisColumns = null, IDictionary<string,int>? otherColumns = null)
    {
        var index = 0;
        var columns = selects.ToDictionary(column => column, column => index++);

        if (columns.Count == 0)
        {
            if (thisColumns is not null)
            {
                foreach (var key in thisColumns.Keys)
                    if (!columns.ContainsKey(key))
                        columns.Add(key, columns.Count);
            }

            if (otherColumns is not null)
            {
                foreach (var key in otherColumns.Keys)
                    if (!columns.ContainsKey(key))
                        columns.Add(key, columns.Count);
            }
        }

        return columns;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="selectColumns"></param>
    /// <param name="intersectionMap"></param>
    /// <param name="rightMap"></param>
    /// <param name="leftMap"></param>
    private static void BuildMaps(Table left, Table right, IDictionary<string, int> selectColumns, out List<Tuple<int, int>> intersectionMap, out List<Tuple<int, int>> rightMap, out List<Tuple<int, int>> leftMap, bool mapCompleteRight = false)
    {
        intersectionMap = new List<Tuple<int, int>>();
        rightMap = new List<Tuple<int, int>>();
        leftMap = new List<Tuple<int, int>>();

        var intersectionColumns = new HashSet<string>(left.Columns.Keys);
        intersectionColumns.IntersectWith(right.Columns.Keys);

        foreach (var key in intersectionColumns)
            intersectionMap.Add(Tuple.Create(left.Columns[key], right.Columns[key]));

        foreach (var column in selectColumns)
        {
            if (left.Columns.TryGetValue(column.Key, out int leftIndex))
            {
                leftMap.Add(Tuple.Create(column.Value, leftIndex));
                if (mapCompleteRight && right.Columns.TryGetValue(column.Key, out int rightIndex))
                    rightMap.Add(Tuple.Create(column.Value, rightIndex));
            }
            else
                rightMap.Add(Tuple.Create(column.Value, right.Columns[column.Key]));
        }
    }

    private static string DefaultBuildCompositeKey(string[] row, IEnumerable<int> indexMap)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append('|');

        foreach (var index in indexMap)
        {
            keyBuilder.Append(row[index]);
            keyBuilder.Append('|');
        }

        return keyBuilder.ToString();
    }

    private static bool IsMatch(string lhs, string rhs)
    {
        if (lhs != string.Empty && rhs != string.Empty)
            return lhs == rhs;

        return true;
    }

    private static int DefaultCompare(string s1, string s2)
        { return String.Compare(s1, s2, StringComparison.InvariantCulture); }

    private static int StableCompare(Tuple<string[], int> t1, Tuple<string[], int> t2, int column, int direction, Func<string, string, int> comparer)
    {
        var result = comparer(t1.Item1[column], t2.Item1[column]);
        if (result != 0) return result * direction;
        return t1.Item2 - t2.Item2;
    }

    private static void Sort(List<Tuple<string[], int>> list, Func<Tuple<string[], int>, Tuple<string[], int>, int> comparer)
        { Sort(list, 0, list.Count-1, comparer); }

    private static void Sort(List<Tuple<string[], int>> list, int first, int last, Func<Tuple<string[], int>, Tuple<string[], int>, int> comparer)
    {
        if (last - first <= 0)
            return;

        var pivot = list[first];
        var left = first;
        var right = last;

        while (left <= right)
        {
            while (comparer(list[left], pivot) < 0)
                left++;

            while (comparer(list[right], pivot) > 0)
                right--;

            if (left <= right)
            {
                var temp = list[left];
                list[left] = list[right];
                list[right] = temp;

                right--;
                left++;
            }
        }

        Sort(list, first, right, comparer);
        Sort(list, left, last, comparer);
    }

    #endregion

    /// <summary>
    /// A table enumerator.
    /// </summary>
    /// <returns>A header with column names followed by rows.</returns>
    public IEnumerator<string[]> GetEnumerator()
    {
        yield return Columns.OrderBy(column => column.Value).Select(column => column.Key).ToArray();
        foreach (var row in Rows) yield return row;
    }

    /// <summary>
    /// Interface specific enumerator for object enumerator.
    /// </summary>
    /// <returns>An object enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }
}
