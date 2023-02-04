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

using Environment = Canyala.Mercury.Storage.Environment;

namespace Canyala.Mercury.Core;

/// <summary>
/// Provides a graph data set.
/// </summary>
public class Dataset
{
    /// <summary>
    /// 
    /// </summary>
    private Dictionary<string, Graph> _graphs;

    /// <summary>
    /// 
    /// </summary>
    public Graph? Default { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public string NameOfDefault { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public void SetDefault(string name)
        { Default = _graphs[NameOfDefault = name]; }

    /// <summary>
    /// 
    /// </summary>
    public Graph? Active
        { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public void SetActiveGraph(string name)
        { Active = _graphs[name]; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Graph this[string name]
        { get { return _graphs[name]; } }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool Contains(string name)
        { return _graphs.ContainsKey(name); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="graph"></param>
    public void Add(string name, Graph graph)
        { _graphs.Add(name, graph); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public void Remove(string name)
    {
        _graphs.Remove(name);

        if (name == NameOfDefault)
            NameOfDefault = string.Empty;
    }

    public static Dataset Create()
        { return new Dataset(); }

    public static Dataset Create(string defaultName, Graph defaultGraph)
        { return new Dataset(defaultName, defaultGraph); }

    public Dataset(string name, Graph graph)
    {
        Default = Active = graph; 
        _graphs = new Dictionary<string, Graph>(StringComparer.InvariantCulture);
        _graphs.Add(NameOfDefault = name, graph);
    }

    public Dataset() 
    {
        NameOfDefault = string.Empty;
        _graphs = new Dictionary<string, Graph>(StringComparer.InvariantCulture);
    }
}
