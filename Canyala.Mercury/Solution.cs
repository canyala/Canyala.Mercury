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
using Canyala.Mercury.Extensions;
using Canyala.Mercury.Storage.Collections;

namespace Canyala.Mercury
{
    /// <summary>
    /// Provides a solution for graph enumerations.
    /// </summary>
    public class Solution : IDisposable, IEnumerable<string[]>
    {
        private Func<IEnumerable<string[]>> _resultsBuilder;
        private IEnumerable<string[]> _results;

        private Func<IView[]> _setsBuilder;
        private IView[] _views;

        private int _width;

        /// <summary>
        /// The width of the solution, count of views/columns.
        /// </summary>
        public int Width { get { return _width; } }

        public Solution(Func<IEnumerable<string[]>> resultsBuilder, Func<IView[]> setsBuilder, int width)
        {
            _resultsBuilder = resultsBuilder;
            _setsBuilder = setsBuilder;
            _width = width;
        }

        public IEnumerator<string[]> GetEnumerator()
            { return (_results = _resultsBuilder()).GetEnumerator(); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            { return GetEnumerator(); }

        public void Dispose()
        {
            var disposable = _results as IDisposable;
            if (disposable != null) disposable.Dispose();
        }

        public View this[int index]
            { get { return new View((_views = _views ?? _setsBuilder())[index]); } }

        public override string ToString()
            { return this.Select(row => "[{0}]".Args(row.Select(column => "'{0}'".Args(column)).Join(","))).Join(","); }
    }
}
