/*

  MIT License
 
  Copyright (c) 2011-2023 Canyala Innovation (Martin Fredriksson)

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
using System.Threading;

using Canyala.Lagoon.Core.Contracts;
using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;
using Canyala.Lagoon.Core.Models;
using Canyala.Lagoon.Core.Serialization;
using Canyala.Lagoon.Core.Text;

using Canyala.Mercury.Core;
using Canyala.Mercury.Storage.Collections;
using Canyala.Mercury.Storage.Extensions;
using Canyala.Mercury.Core.Extensions;
using Canyala.Mercury.Core.Internal;

namespace Canyala.Mercury.Core;

/// <summary>
/// Provides a collection for graphs, a storage for { subject, predicate, object } triples.
/// </summary>
public class Graph : IEnumerable<string[]>, IDisposable
{
    /// <summary>
    /// Provides an index interface.
    /// </summary>
    /// <remarks>
    /// The Graph implementation provides a default memory based
    /// implementation for Index that can be replaced by deriving
    /// classes.
    /// </remarks>
    internal interface Index : IDisposable
    {
        void Add(string primary, string secondary, string ternary);
        void Remove(string? primary, string? secondary, string? ternary);
        void Clear();

        bool Contains(string primary);
        bool Contains(string primary, string secondary);
        bool Contains(string primary, string secondary, string ternary);

        IEnumerable<string[]> Enumerate(Constraint.Specific primary, Constraint.Specific secondary, Constraint ternary);
        IEnumerable<string[]> Enumerate(Constraint.Specific primary, Constraint secondary, Constraint ternary);
        IEnumerable<string[]> Enumerate(Constraint primary, Constraint secondary, Constraint ternary);

        IView[] Views(Constraint.Specific primary, Constraint.Specific secondary, Constraint ternary);
        IView[] Views(Constraint.Specific primary, Constraint secondary, Constraint ternary);
        IView View(Constraint constraint);
    }

    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    internal readonly Index SubjectPredicateObject;
    internal readonly Index PredicateObjectSubject;
    internal readonly Index ObjectSubjectPredicate;

    private const string Default = "Default";

    /// <summary>
    /// Creates a Graph by specifying an index builder.
    /// May be invoked from a deriving class specifying a custom index builder.
    /// </summary>
    /// <param name="indexFactory">A function that builds and returns an index.</param>
    internal Graph(Func<string, Index> indexFactory)
    {
        SubjectPredicateObject = indexFactory(nameof(SubjectPredicateObject));
        PredicateObjectSubject = indexFactory(nameof(PredicateObjectSubject));
        ObjectSubjectPredicate = indexFactory(nameof(ObjectSubjectPredicate));
    }

    private Graph(string instanceName, IEnumerable<string[]> triples)
        : this(indexName => new ManagedIndex($"{instanceName ?? Default}.{indexName}"))
        { triples.Do(triple => Assert(triple)); }

    private Graph(Storage.Environment environment, string instanceName, IEnumerable<string[]> triples) 
        : this(indexName => new HeapIndex(environment, $"{instanceName}.{indexName}"))
        { triples.Do(triple => Assert(triple)); }

    /// <summary>
    /// Asserts an assumption in the graph in the form of 3 separate strings.
    /// </summary>
    /// <param name="subject">The subject string.</param>
    /// <param name="predicate">The predicate string.</param>
    /// <param name="object">The object string.</param>
    /// <returns>The graph instance.</returns>
    public Graph Assert(string subject, string predicate, string @object)
    {
        _lock.EnterWriteLock();

        try
        {
            foreach (var rule in _implicitRules)
                rule(this, Seq.Array(subject, predicate, @object));

            SubjectPredicateObject.Add(subject, predicate, @object);
            PredicateObjectSubject.Add(predicate, @object, subject);
            ObjectSubjectPredicate.Add(@object, subject, predicate);

            return this;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Asserts an assumptions in the form of string array.
    /// </summary>
    /// <param name="triple">A string array forming a triple.</param>
    /// <returns>The graph instance.</returns>
    public Graph Assert(string[] triple)
        { return Assert(triple[0], triple[1], triple[2]); }

    /// <summary>
    /// Asserts a set of assumptions in the form of a sequence of string arrays.
    /// </summary>
    /// <param name="triples">The sequence of triples as string arrays.</param>
    /// <returns>The graph instance.</returns>
    public Graph Assert(IEnumerable<string[]> triples)
    { 
        triples.Do(triple => Assert(triple[0], triple[1], triple[2])); 
        return this;
    }

    /// <summary>
    /// Retracts the assumtions matching a triple pattern in the form of separate subject, predicat and object strings. }
    /// </summary>
    /// <param name="subject">The subject. <code>null</code> denotess a wildcard.</param>
    /// <param name="predicate">The predicate. <code>null</code> denotes a wildcard.</param>
    /// <param name="object">The object. <code>null</code> denotes a wildcard.</param>
    /// <returns>The graph instance.</returns>
    public Graph Retract(string? subject, string? predicate, string? @object)
    {
        _lock.EnterWriteLock();

        try
        {
            SubjectPredicateObject.Remove(subject, predicate, @object);
            PredicateObjectSubject.Remove(predicate, @object, subject);
            ObjectSubjectPredicate.Remove(@object, subject, predicate);
            return this;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Retracts the assumtions matching a triple pattern in the form of an array of strings.
    /// </summary>
    /// <param name="triple">The triple pattern in the form of an array of strings.</param>
    /// <returns>The graph instance.</returns>
    public Graph Retract(string[] triple)
        { return Retract(triple[0], triple[1], triple[2]); }

    /// <summary>
    /// Retracts the assumptions matching a set of triple patterns in the form of a sequence of string arrays.
    /// </summary>
    /// <param name="triples">The sequence of patterns.</param>
    /// <returns>The graph instance.</returns>
    public Graph Retract(IEnumerable<string[]> triples)
    { 
        triples.Do(triple => Retract(triple)); 
        return this;
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            SubjectPredicateObject.Clear();
            PredicateObjectSubject.Clear();
            ObjectSubjectPredicate.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Test if an assumtion has been stated.
    /// </summary>
    /// <param name="subject">The subject of the assumption.</param>
    /// <param name="predicate">The predicate of the assumption.</param>
    /// <param name="object">The object of the assumption.</param>
    /// <returns><code>true</code> if the assumption is stated, otherwize <code>false</code>.</returns>
    public bool IsTrue(string subject, string predicate, string @object)
    {
        _lock.EnterReadLock();

        try
        {
            return SubjectPredicateObject.Contains(subject, predicate, @object); 
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    #region Inference (Experimental)

    private List<Action<Graph, string[]>> _implicitRules = new List<Action<Graph, string[]>>();

    public Graph Infer(Action<Graph, string[]> rule)
    { 
        _implicitRules.Add(rule);  
        return this;
    }

    public Graph Infer(IEnumerable<Action<Graph>> rules)
    {
        rules.Do(rule => rule(this));
        return this;
    }

    public Graph Infer(Action<Graph> rule)
        { return Infer(Seq.Of(rule)); }

    #endregion

    /// <summary>
    /// Create a graph instance.
    /// </summary>
    /// <param name="useStreamMemory"></param>
    /// <returns></returns>
    public static Graph Create(bool useStreamMemory)
    {
        if (useStreamMemory)
            return new Graph(Storage.Environment.Create(), Default, Seq.Empty<string[]>());
        else
            return new Graph(Default, Seq.Empty<string[]>());
    }

    /// <summary>
    /// Create a graph instance.
    /// </summary>
    /// <param name="useStreamMemory"></param>
    /// <param name="triples"></param>
    /// <returns></returns>
    public static Graph Create(bool useStreamMemory, IEnumerable<string[]> triples)
    {
        if (useStreamMemory)
            return new Graph(Storage.Environment.Create(), Default, triples);
        else
            return new Graph(Default, triples);
    }

    /// <summary>
    /// Create a graph instance.
    /// </summary>
    /// <param name="useStreamMemory"></param>
    /// <param name="name"></param>
    /// <param name="triples"></param>
    /// <returns></returns>
    public static Graph Create(bool useStreamMemory, string name, IEnumerable<string[]> triples)
    {
        if (useStreamMemory)
            return new Graph(Storage.Environment.Create(), name, triples);
        else
            return new Graph(name, triples);
    }

    /// <summary>
    /// Create a graph instance.
    /// </summary>
    /// <param name="environment"></param>
    /// <param name="triples"></param>
    /// <returns></returns>
    public static Graph Create(Storage.Environment environment, IEnumerable<string[]> triples)
        { return new Graph(environment, Default, triples); }

    /// <summary>
    /// Create a graph instance.
    /// </summary>
    /// <param name="environment"></param>
    /// <param name="name"></param>
    /// <param name="triples"></param>
    /// <returns></returns>
    public static Graph Create(Storage.Environment environment, string name, IEnumerable<string[]> triples)
        { return new Graph(environment, name, triples); }

    /// <summary>
    /// Create a graph instance.
    /// </summary>
    /// <param name="environment"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Graph Create(Storage.Environment environment, string name)
        { return new Graph(environment, name, Seq.Empty<string[]>()); }

    /// <summary>
    /// Create a graph instance.
    /// </summary>
    /// <param name="environment"></param>
    /// <returns></returns>
    public static Graph Create(Storage.Environment environment)
        { return new Graph(environment, Default, Seq.Empty<string[]>()); }

    /// <summary>
    /// Create a graph instance.
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static implicit operator Dataset(Graph graph)
        { return Dataset.Create("Default", graph); }

    /// <summary>
    /// Support for typed enumerations.
    /// </summary>
    /// <returns>A sequence of arrays of strings.</returns>
    public IEnumerator<string[]> GetEnumerator()
        { foreach (var result in Enumerate(null, null, null)) yield return result; }

    /// <summary>
    /// Support for <code>foreach</code> loops.
    /// </summary>
    /// <returns>An enumerator for the graph.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

    public Solution this[Constraint? subject, Constraint? predicate, Constraint? @object]
        { get { return Enumerate(subject, predicate, @object); } }


    /// <summary>
    /// Enumerates graph contents.
    /// </summary>
    /// <param name="subject">Constraint for subject.</param>
    /// <param name="predicate">Constraint for predicate.</param>
    /// <param name="object">Constraint for object.</param>
    /// <returns>A solution for matches.</returns>
    public Solution Enumerate(Constraint? subject, Constraint? predicate, Constraint? @object)
    { 
        int width = 0;

        if (!(subject is Constraint.Specific))
            width++;

        if (!(predicate is Constraint.Specific))
            width++;

        if (!(@object is Constraint.Specific))
            width++;

        return new Solution(() => InternalEnumerate(subject, predicate, @object).AsReadLocked(_lock, this), () => Views(subject, predicate, @object), width); 
    }

    /// <summary>
    /// Enumerates graph contents.
    /// </summary>
    /// <param name="subject">Constraint for subject.</param>
    /// <param name="predicate">Constraint for predicate.</param>
    /// <param name="object">Constraint for object.</param>
    /// <returns>Sequence of matches not containing specific's in subject, predicate, object order.</returns>
    private IEnumerable<string[]> InternalEnumerate(Constraint? subject, Constraint? predicate, Constraint? @object)
    {
        subject = subject ?? Constraint.Empty;
        predicate = predicate ?? Constraint.Empty;
        @object = @object ?? Constraint.Empty;

        if (subject is Constraint.Specific && predicate is Constraint.Specific && @object is Constraint.Specific)
        {
            if (IsTrue((Constraint.Specific)subject, (Constraint.Specific)predicate, (Constraint.Specific)@object))
                return Seq.Of(Seq.Array<string>());
            else
                return Seq.Of<string[]>();
        }

        else 
            
            if (subject is Constraint.Specific && predicate is Constraint.Specific)
            {
                return SubjectPredicateObject.Enumerate((Constraint.Specific)subject, (Constraint.Specific)predicate, @object);
            }
            else if (predicate is Constraint.Specific && @object is Constraint.Specific)
            {
                return PredicateObjectSubject.Enumerate((Constraint.Specific)predicate, (Constraint.Specific)@object, subject);
            }
            else if (@object is Constraint.Specific && subject is Constraint.Specific)
            {
                return ObjectSubjectPredicate.Enumerate((Constraint.Specific)@object, (Constraint.Specific)subject, predicate);
            }

        else 
            
            if (subject is Constraint.Specific)
            {
                return SubjectPredicateObject.Enumerate((Constraint.Specific)subject, predicate, @object);
            }
            else if (predicate is Constraint.Specific)
            {
                return PredicateObjectSubject.Enumerate((Constraint.Specific)predicate, @object, subject).Select(result => Seq.Array(result[1], result[0]));
            }
            else if (@object is Constraint.Specific)
            {
                return ObjectSubjectPredicate.Enumerate((Constraint.Specific)@object, subject, predicate);
            }

        else

            return SubjectPredicateObject.Enumerate(subject, predicate, @object);
    }

    private IView[] Views(Constraint? subject, Constraint? predicate, Constraint? @object)
    {
        subject = subject ?? Constraint.Empty;
        predicate = predicate ?? Constraint.Empty;
        @object = @object ?? Constraint.Empty;

        if (subject is Constraint.Specific && predicate is Constraint.Specific && @object is Constraint.Specific)
        {
            if (IsTrue((Constraint.Specific)subject, (Constraint.Specific)predicate, (Constraint.Specific)@object))
                return Seq.Array(View.Empty, View.Empty, View.Empty);
            else
                return Seq.Array<IView>();
        }

        else

            if (subject is Constraint.Specific && predicate is Constraint.Specific)
            {
                return SubjectPredicateObject.Views((Constraint.Specific)subject, (Constraint.Specific)predicate, @object);
            }
            else if (predicate is Constraint.Specific && @object is Constraint.Specific)
            {
                return PredicateObjectSubject.Views((Constraint.Specific)predicate, (Constraint.Specific)@object, subject);
            }
            else if (@object is Constraint.Specific && subject is Constraint.Specific)
            {
                return ObjectSubjectPredicate.Views((Constraint.Specific)@object, (Constraint.Specific)subject, predicate);
            }

            else

                if (subject is Constraint.Specific)
                {
                    return SubjectPredicateObject.Views((Constraint.Specific)subject, predicate, @object);
                }
                else if (predicate is Constraint.Specific)
                {
                    var result = PredicateObjectSubject.Views((Constraint.Specific)predicate, @object, subject);
                    return Seq.Array(result[1], result[0]);
                }
                else if (@object is Constraint.Specific)
                {
                    return ObjectSubjectPredicate.Views((Constraint.Specific)@object, subject, predicate);
                }

                else

                    return Seq.Array(SubjectPredicateObject.View(subject), PredicateObjectSubject.View(predicate), ObjectSubjectPredicate.View(@object));
    }

    public void Dispose()
    {
        SubjectPredicateObject.Dispose();
        PredicateObjectSubject.Dispose();
        ObjectSubjectPredicate.Dispose();
    }
}

