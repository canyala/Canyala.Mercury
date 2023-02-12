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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury.Core;
using Canyala.Mercury.Rdf.Internal;
using Canyala.Mercury.Rdf.Extensions;

namespace Canyala.Mercury.Rdf;

/// <summary>
/// Provides an API for terse turtle documents.
/// </summary>
/// <remarks>
/// Terse RDF Triple Language
/// W3C Candidate Recommendation 19 February 2013
/// </remarks>
/// <seealso cref="http://www.w3.org/TR/2013/CR-turtle-20130219/"/>
public partial class Turtle
{
    /// <summary>
    /// Provides a turtle producer.
    /// </summary>
    public class Producer : IEnumerable<string[]>, IDisposable
    {
        #region State

        readonly Turtle Parser;
        readonly IEnumerable<string> TurtleLines;

        readonly Namespaces Namespaces = new Namespaces();

        readonly Stack<Resource?> Subjects = new();
        readonly Stack<Resource?> Predicates = new();
        readonly Stack<Action<Resource?>?> Setters = new();
        readonly Stack<Action<Resource?>?> Emitters = new();

        readonly ConcurrentQueue<string[]> Triples = new();

        readonly Dictionary<Blank, Blank> InternalBlanks = new();

        #endregion

        #region Construction

        /// <summary>
        /// Create a turtle producer.
        /// </summary>
        /// <param name="parser">The turtle parse instance.</param>
        /// <param name="turtleLines">A turtle document as a sequence of text line strings.</param>
        internal Producer(Turtle parser, IEnumerable<string> turtleLines)
        {
            Emitters.Push(DefaultEmitter);
            Setters.Push(BlankNodeExceptionSetter);
            TurtleLines = turtleLines;
            Parser = parser;
        }

        #endregion

        #region Production Rule State Appliers

        /// <summary>
        /// Applies a base.
        /// </summary>
        internal Namespace Base
        { set { Namespaces.Base = value; } }

        /// <summary>
        /// Applies prefix and namespace.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="namespace"></param>
        internal void PrefixAndNamespace(string prefix, string @namespace)
        { Namespaces.Add(prefix, @namespace); }

        /// <summary>
        /// Applies subject.
        /// </summary>
        internal string Subject
        { set { Subjects.Poke(CreateTerm(value)); } }

        /// <summary>
        /// Applies predicate.
        /// </summary>
        internal string Predicate
        { set { Predicates.Poke(CreateTerm(value)); } }

        /// <summary>
        /// Applies object.
        /// </summary>
        internal string Object
        { set { Emitters.Peek()!(CreateTerm(value)); } }

        /// <summary>
        /// Applies blank allocation for a subject.
        /// </summary>
        internal void AllocBlankSubject()
        { Setters.Push(BlankNodeIsSubjectSetter); }

        /// <summary>
        /// Applies blank allocation for an object.
        /// </summary>
        internal void AllocBlankObject()
        { Setters.Push(BlankNodeIsObjectSetter); }

        /// <summary>
        /// Applies the beginning of a property object list.
        /// </summary>
        internal void BeginPropertyList()
        {
            Emitters.Push(DefaultEmitter);
            Subjects.Push(Rdf.Blank.NewBlank());
            Predicates.Push(null);
        }

        /// <summary>
        /// Applies the end of a property object list.
        /// </summary>
        internal void EndPropertyList()
        {
            Emitters.Pop();
            Predicates.Pop();
            CreateTerm = BlankCreator;
            Setters.Pop()!(Subjects.Pop());
        }

        /// <summary>
        /// Applies the beginning of an object list.
        /// </summary>
        internal void BeginCollection()
        {
            Emitters.Push(FirstCollectionEmitter);
            Subjects.Push(Rdf.Blank.NewBlank());
            Subjects.Push(Subjects.Peek());
        }

        /// <summary>
        /// Applies the end of an object list.
        /// </summary>
        internal void EndCollection()
        {
            var subject = Subjects.Pop();
            var blankNode = Subjects.Pop();

            if (RestCollectionEmitter == Emitters.Pop())
            {
                EmitTriple(subject, Ontologies.Rdf.rest, Ontologies.Rdf.nil);
                CreateTerm = BlankCreator;
            }
            else
            {
                blankNode = Ontologies.Rdf.nil;
                CreateTerm = NilCreator;
            }

            Setters.Pop()!(blankNode);
        }

        #endregion

        #region Enumeration implementation

        /// <summary>
        /// Specific IEnumerable implementation for enumeration.
        /// </summary>
        /// <returns>An IEnumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

        /// <summary>
        /// Enumerates the triples of the turtle document.
        /// </summary>
        /// <returns></returns>

        public IEnumerator<string[]> GetEnumerator()
        {
            foreach (var lines in Comments.Trim(TurtleLines).CombineLines('.'))
            {
                if (!Parser.Apply(lines, this, out var errMsg))
                    throw new Exception(errMsg);

                while (Triples.TryDequeue(out var triple))
                    yield return triple;
            }
        }

        #endregion

        #region Setter Handlers

        private void BlankNodeExceptionSetter(Resource? blankNode)
        { throw new Exception("Attempt to set undefined property list or collection node."); }

        private void BlankNodeIsSubjectSetter(Resource? blankNode)
        { Subject = blankNode!; }

        private void BlankNodeIsObjectSetter(Resource? blankNode)
        { Object = blankNode!; }

        #endregion

        #region Emitter Handlers

        private void EmitTriple(Resource? subject, Resource? predicate, Resource? @object)
        {
            if (subject is Blank)
                subject = UniqueInternalBlank(subject as Blank);

            if (predicate is Blank)
                predicate = UniqueInternalBlank(predicate as Blank);

            if (@object is Blank)
                @object = UniqueInternalBlank(@object as Blank);

            Triples.Enqueue(Seq.Array<string>(subject!, predicate!, @object!)); 
        }

        private Blank UniqueInternalBlank(Blank? externalBlank)
        {
            if (!InternalBlanks.TryGetValue(externalBlank!, out var internalBlank))
            {
                internalBlank = Rdf.Blank.NewBlank();
                InternalBlanks.Add(externalBlank!, internalBlank);
            }

            return internalBlank;
        }

        private void DefaultEmitter(Resource? @object)
            { EmitTriple(Subjects.Peek(), Predicates.Peek(), @object); }

        private void FirstCollectionEmitter(Resource? @object)
        {
            EmitTriple(Subjects.Peek(), Ontologies.Rdf.first, @object);
            Emitters.Poke(RestCollectionEmitter);
        }

        private void RestCollectionEmitter(Resource? @object)
        {
            EmitTriple(Subjects.Peek(), Ontologies.Rdf.rest, Subjects.Poke(Rdf.Blank.NewBlank()));
            EmitTriple(Subjects.Peek(), Ontologies.Rdf.first, @object);
        }

        #endregion

        #region Special Term Resolvers

        #region Special Term Resolvers

        Func<string, Resource> CreateTerm = UndefinedTerm;

        internal void TermIsBoolean()
        { CreateTerm = BooleanCreator; }

        internal void TermIsInteger()
        { CreateTerm = IntegerCreator; }

        internal void TermIsDouble()
        { CreateTerm = DoubleCreator; }

        internal void TermIsDecimal()
        { CreateTerm = DecimalCreator; }

        internal void TermIsIri()
        { CreateTerm = IriCreator; }

        internal void TermIsBlank()
        { CreateTerm = BlankCreator; }

        internal void TermIsAnon()
        { CreateTerm = AnonCreator; }

        internal void TermIsNil()
        { CreateTerm = NilCreator; }

        internal void TermIsString()
        { CreateTerm = StringCreator; }

        internal void TermIsA()
        { CreateTerm = ACreator; }

        private static Resource UndefinedTerm(string na)
        { throw new NotImplementedException("UndefinedTerm"); }

        private Resource BooleanCreator(string value)
        { return new Literal("\"{0}\"^^{1}".Args(value, Ontologies.Xsd.boolean), Namespaces); }

        private Resource IntegerCreator(string value)
        { return new Literal("\"{0}\"^^{1}".Args(value, Ontologies.Xsd.integer), Namespaces); }

        private Resource DoubleCreator(string value)
        { return new Literal("\"{0}\"^^{1}".Args(value, Ontologies.Xsd.@double), Namespaces); }

        private Resource DecimalCreator(string value)
        { return new Literal("\"{0}\"^^{1}".Args(value, Ontologies.Xsd.@decimal), Namespaces); }

        private Resource IriCreator(string value)
        { return new Iri(value, Namespaces); }

        private Resource BlankCreator(string value)
        { return new Blank(value); }

        private Resource AnonCreator(string value)
        { return NewBlank(); }

        private Resource NilCreator(string value)
        { return Ontologies.Rdf.nil; }

        private Resource StringCreator(string value)
        { return new Literal(value, Namespaces); }

        private Resource ACreator(string value)
        { return Ontologies.Rdf.type; }

        private Resource NewBlank()
        { return Rdf.Blank.NewBlank(); }

        #endregion

        #endregion

        public void Dispose()
        {
            // Cancellation.Cancel();
        }
    }
}
