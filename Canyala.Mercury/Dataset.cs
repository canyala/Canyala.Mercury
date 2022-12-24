//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Canyala.Mercury;

using Environment = Canyala.Mercury.Storage.Environment;

namespace Canyala.Mercury
{
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
        public Graph Default { get; private set; }

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
        public Graph Active
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
                NameOfDefault = null;
        }

        public static Dataset Create()
            { return new Dataset(); }

        public static Dataset Create(string defaultName, Graph defaultGraph)
            { return new Dataset(defaultName, defaultGraph); }

        private Dataset(string name, Graph graph)
        {
            Default = Active = graph; 
            _graphs = new Dictionary<string, Graph>(StringComparer.InvariantCulture);
            _graphs.Add(NameOfDefault = name, graph);
        }

        private Dataset() 
        {
            _graphs = new Dictionary<string, Graph>(StringComparer.InvariantCulture);
        }
    }
}
