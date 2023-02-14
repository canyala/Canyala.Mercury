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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Core.Contracts;
using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury.Core;
using Canyala.Mercury.Rdf.Internal;

namespace Canyala.Mercury.Rdf;

/// <summary>
/// 
/// </summary>
public partial class Query
{
    /// <summary>
    /// 
    /// </summary>
    public class Builder
    {
        internal Specification Plan { get; private set; }

        private Stack<Term?> Predicates = new();
        private Stack<Term?> Subjects = new();

        private Stack<Action<Term?>?> Emitters = new();
        private Stack<Action<Term?>?> Setters = new();

        private Stack<Group> Groups { get; set; }
        private Stack<List<Group>> GroupLists { get; set; }
        private Group CurrentGroup { get { return Groups.Peek(); } }
        private Group? LastGroup { get; set; }

        private string? _currentOperation;
        private bool _distinct;

        private int _currentDataIndex;
        private string[]? _currentDataBlock;

        private bool _selectAll;

        private Stack<Expression> Expressions { get; set; }
        private Stack<int> ArgCounts { get; set; }
        
        private static ParameterExpression getVarParam = Expression.Parameter(typeof(Func<string, string>));

        internal Builder()
        {
            Plan = new Specification();
            Groups = new Stack<Group>();
            Emitters.Push(DefaultEmitter);
            Setters.Push(AnonymousVarExceptionSetter);
            GroupLists = new Stack<List<Group>>(Seq.Of(Plan.Groups));
            CurrentVariables = new List<Variable>();
            CurrentValuesVariables = new List<Variable>();

            CurrentSelectBinders = new List<Func<Func<string, string>, string>>();
            CurrentSelectVariables = new List<Variable>();
            
            CurrentImplicitBindBinders = new List<Func<Func<string, string>, string>>();
            CurrentImplicitBindVariables = new List<Variable>();

            CurrentAggregateBinders = new List<Func<Func<string, string>, string, HashSet<string>, string>>();
            CurrentAggregateVariables = new List<Variable>();
            CurrentAggregateDistincts = new List<bool>();

            CurrentOperation = string.Empty;
            Expressions = new Stack<Expression>();
            ArgCounts = new Stack<int>();

            GroupConcatSeparator = "\" \"";

            CreateTerm = UndefinedTerm;
        }

        #region Finalization
        internal Specification ExtractFinalPlan
        {
            get
            {
                if (Plan.Groups[0].Operation == "CONSTRUCT")
                {
                    var constructGroup = Plan.Groups[0];
                    Plan.Groups.RemoveAt(0);
                    Plan.Groups.Add(constructGroup);
                }

                FinalizeSelectAll(Plan.Groups);

                var finalPlan = Plan;
                Plan = Specification.Empty;
                return finalPlan;
            }
        }

        private void FinalizeSelectAll(List<Group> groups)
        {
            FinalizeSelectAll(groups, new HashSet<Variable>());
        }

        private void FinalizeSelectAll(List<Group> groups, HashSet<Variable> variables)
        {
            foreach (var group in groups)
            {
                if (group.Operation == "SELECT")
                {
                    if (!group.SelectAll)
                        group.Variables.Do(v => variables.Add(v));
                    else
                    {
                        var subvariables = new HashSet<Variable>();
                        FinalizeSelectAll(group.Groups, subvariables);
                        subvariables.Do(v => variables.Add(v));

                        Contract.Assume(group.Variables.Count == 0, "Cannot have variables when using select *");
                        Contract.Assume(group.Operation == "SELECT", "Cannot have select all on any groups other than select");

                        group.Variables.AddRange(subvariables);
                    }
                    continue;
                }

                FinalizeSelectAll(group.Groups, variables);

                if (group.Operation == "MINUS" || group.Operation.StartsWith("FILTER"))
                    continue;
                
                group.Clauses
                    .SelectMany(term => term)
                    .OfType<Variable>()
                    .Where(v => !v.IsAnonymous)
                    .Do(v => variables.Add(v));

            }
        }
        #endregion

        #region Production State Appliers

        internal List<Variable> CurrentVariables { get; private set; }
        internal List<Variable> CurrentValuesVariables { get; private set; }

        internal List<Func<Func<string, string>, string>> CurrentSelectBinders { get; private set; }
        internal List<Variable> CurrentSelectVariables { get; private set; }

        internal List<Func<Func<string, string>, string>> CurrentImplicitBindBinders { get; private set; }
        internal List<Variable> CurrentImplicitBindVariables { get; private set; }

        internal List<Func<Func<string, string>, string, HashSet<string>, string>> CurrentAggregateBinders { get; private set; }
        internal List<Variable> CurrentAggregateVariables { get; private set; }
        internal List<bool> CurrentAggregateDistincts { get; private set; }

        /// <summary>
        /// Applies a base.
        /// </summary>
        internal Namespace Base
            { set { Plan!.Namespaces.Base = value; } }

        /// <summary>
        /// Applies a prefix and a namespace.
        /// </summary>
        /// <param name="prefix">A prefix.</param>
        /// <param name="namespace">A namespace.</param>
        internal void PrefixAndNamespace(string prefix, string @namespace)
            { Plan!.Namespaces.Add(prefix, @namespace); }

        internal Variable SelectVar
            { set { CurrentVariables.Add(value); } }

        internal Variable SelectAsVar
        { 
            set 
            { 
                CurrentSelectVariables.Add(value);
                CurrentVariables.Add(value);
            } 
        }

        internal void SelectAll()
        {
            _selectAll = true;
        }

        internal Variable BindAsVar
            { set { CurrentGroup.ExplicitBindAsVars.Add(value); } }

        internal Variable ValuesVar
            { set { CurrentValuesVariables.Add(value); } }

        internal Variable GroupByVar
        { 
            set 
            { 
                var ignored = Expression.Parameter(typeof(string));
                var distinctSet = Expression.Parameter(typeof(HashSet<string>));
                var body = Expression.Invoke(getVarParam, Expression.Constant(value.Value));
                var lambdaBinder = Expression.Lambda<Func<Func<string, string>, string, HashSet<string>, string>>(body, getVarParam, ignored, distinctSet);
                var aggregateBinder = lambdaBinder.Compile();

                LastGroup!.AggregateBinders.Add(aggregateBinder);
                LastGroup.AggregateVars.Add(value);
                LastGroup.AggregateDistincts.Add(false);
                LastGroup.GroupByVars.Add(value);
            } 
        }

        internal Variable GroupByBindVar
        { 
            set 
            {
                var ignored = Expression.Parameter(typeof(string));
                var distinctSet = Expression.Parameter(typeof(HashSet<string>));
                var body = Expression.Invoke(getVarParam, Expression.Constant(value.Value));
                var lambdaBinder = Expression.Lambda<Func<Func<string, string>, string, HashSet<string>, string>>(body, getVarParam, ignored, distinctSet);
                var aggregateBinder = lambdaBinder.Compile();

                LastGroup!.AggregateBinders.Add(aggregateBinder);
                LastGroup.AggregateVars.Add(value);
                LastGroup.AggregateDistincts.Add(false);
                LastGroup.GroupByVars.Add(value);

                LastGroup.ExplicitBindAsVars.Add(value); 
            } 
        }

        internal Variable OrderByVar
            { set { LastGroup!.OrderByVars.Add(value); } }

        internal string CurrentOperation { 
            get { return _currentOperation!; }
            set { _currentOperation = value.ToUpperInvariant(); } 
        }

        internal bool CurrentDistinct
        {
            get { return _distinct; }
            set { _distinct = value; }
        }

        internal void BeginGroupGraphPattern()
        {
            if (Groups.Count > 0 && CurrentGroup.Clauses.Count > 0)
            {
                var implicitGroup = new Group(CurrentGroup);
                CurrentGroup.Groups.Add(implicitGroup);
            }

            var group = new Group(CurrentOperation, CurrentVariables, CurrentSelectBinders, CurrentSelectVariables,  
                CurrentImplicitBindBinders, CurrentImplicitBindVariables,
                CurrentAggregateBinders, CurrentAggregateVariables, CurrentAggregateDistincts,
                CurrentDistinct);

            group.SelectAll = _selectAll;
            CurrentOperation = string.Empty;
            CurrentDistinct = false;
            CurrentVariables.Clear();

            _selectAll = false;
            CurrentSelectVariables.Clear();
            CurrentSelectBinders.Clear();
            CurrentImplicitBindBinders.Clear();
            CurrentImplicitBindVariables.Clear();
            CurrentAggregateBinders.Clear();
            CurrentAggregateVariables.Clear();
            CurrentAggregateDistincts.Clear();

            Groups.Push(group);

            GroupLists.Peek().Add(group);
            GroupLists.Push(group.Groups);
        }

        internal void EndGroupGraphPattern()
        {
            if (((CurrentGroup.Operation != String.Empty && CurrentGroup.Operation != "CONSTRUCT") || CurrentGroup.Groups.Count > 0) && CurrentGroup.Clauses.Count > 0)
            {
                var implicitGroup = new Group(CurrentGroup);
                CurrentGroup.Groups.Add(implicitGroup);
            }

            LastGroup = CurrentGroup;

            Groups.Pop();
            GroupLists.Pop();
        }

        internal void GroupByBind()
        {
            var body = Expressions.Pop();
            var castToString = Expression.Call(body, "ToString", null);
            var lambdaExpression = Expression.Lambda<Func<Func<string, string>, string>>(castToString, getVarParam);
            var compiledExpression = lambdaExpression.Compile();

            LastGroup!.ExplicitBinders.Add(compiledExpression);

            if (LastGroup.ExplicitBindAsVars.Count < LastGroup.ExplicitBinders.Count)
            {
                Contract.Assume(LastGroup.ExplicitBindAsVars.Count + 1 == LastGroup.ExplicitBinders.Count, "Internal error: count mismatch.");

                var anonVar = NewAnonymous() as Variable;
                LastGroup.ExplicitBindAsVars.Add(anonVar!);
            }

            LastGroup.GroupByVars.Add(LastGroup.ExplicitBindAsVars[LastGroup.ExplicitBindAsVars.Count-1]);
        }

        internal void BeginDataBlock()
        {
            if (CurrentGroup.Variables.Count == 0)
            {
                CurrentGroup.Variables.AddRange(CurrentValuesVariables);
                CurrentValuesVariables.Clear();
            }
            
            _currentDataIndex = 0;
            _currentDataBlock = new string[CurrentGroup.Variables.Count];
        }

        internal string DataBlockValue
        {
            set
            {
                var val = string.Equals(value, "UNDEF", StringComparison.InvariantCultureIgnoreCase) ? "" : value;
                 _currentDataBlock![_currentDataIndex++] = Resource.Parse(val, Plan.Namespaces)!;
                 if (_currentDataIndex == CurrentGroup.Variables.Count)
                    CurrentGroup.Values.Add(_currentDataBlock);
            }
        }

        /// <summary>
        /// Applies subject.
        /// </summary>
        internal string Subject
            { set { Subjects!.Poke(CreateTerm(value)); } }

        /// <summary>
        /// Applies predicate.
        /// </summary>
        internal string Predicate
            { set { Predicates!.Poke(CreateTerm(value)); } }

        /// <summary>
        /// Applies object.
        /// </summary>
        internal string Object
            { set { Emitters.Peek()!(CreateTerm(value)); } }

        /// <summary>
        /// Applies blank allocation for a subject.
        /// </summary>
        internal void AllocAnonSubject()
            { Setters.Push(AnonymousVarIsSubjectSetter); }

        /// <summary>
        /// Applies blank allocation for an object.
        /// </summary>
        internal void AllocAnonObject()
            { Setters.Push(AnonymousVarIsObjectSetter); }

        /// <summary>
        /// Applies the beginning of a property object list.
        /// </summary>
        internal void BeginPropertyList()
        {
            Emitters.Push(DefaultEmitter);
            Subjects.Push(NewAnonymous());
            Predicates.Push(null);
        }

        /// <summary>
        /// Applies the end of a property object list.
        /// </summary>
        internal void EndPropertyList()
        {
            Emitters.Pop();
            Predicates.Pop();
            CreateTerm = VariableCreator;
            Setters.Pop()!(Subjects.Pop());
        }

        /// <summary>
        /// Applies the beginning of an object list.
        /// </summary>
        internal void BeginCollection()
        {
            Emitters.Push(FirstCollectionEmitter);
            Subjects.Push(NewAnonymous());
            Subjects.Push(Subjects.Peek());
        }

        /// <summary>
        /// Applies the end of an object list.
        /// </summary>
        internal void EndCollection()
        {
            var subject = Subjects.Pop();
            var anonymousVar = Subjects.Pop();

            if (RestCollectionEmitter == Emitters.Pop())
                EmitTriple(subject!, Ontologies.Rdf.rest, Ontologies.Rdf.nil);
            else
                anonymousVar = Ontologies.Rdf.nil;

            CreateTerm = VariableCreator;
            Setters.Pop()!(anonymousVar);
        }

        #endregion

        #region Setter Handlers

        private void AnonymousVarExceptionSetter(Term? anonymousVar)
            { throw new Exception("Attempt to set undefined property list or collection node."); }

        private void AnonymousVarIsSubjectSetter(Term? anonymousVar)
            { Subject = anonymousVar!; }

        private void AnonymousVarIsObjectSetter(Term? anonymousVar)
            { Object = anonymousVar!; }

        #endregion

        #region Emitter Handlers

        private void EmitTriple(Term? subject, Term? predicate, Term? @object)
        {
            if (CurrentGroup.ExplicitBindAsVars.Count > 0)
            {
                var implicitGroup = new Group(CurrentGroup);
                CurrentGroup.Groups.Add(implicitGroup);
            }

            CurrentGroup.Clauses.Add(Seq.Array(subject, predicate, @object)); 
        }

        private void DefaultEmitter(Term? @object)
            { EmitTriple(Subjects.Peek(), Predicates.Peek(), @object); }

        private void FirstCollectionEmitter(Term? @object)
        {
            EmitTriple(Subjects.Peek(), Ontologies.Rdf.first, @object);
            Emitters.Poke(RestCollectionEmitter);
        }

        private void RestCollectionEmitter(Term? @object)
        {
            EmitTriple(Subjects.Peek(), Ontologies.Rdf.rest, Subjects.Poke(NewAnonymous()));
            EmitTriple(Subjects.Peek(), Ontologies.Rdf.first, @object);
        }

        #endregion

        #region Special Term Resolvers

        Func<string, Term?> CreateTerm;

        internal void TermIsBoolean()
            { CreateTerm = BooleanCreator; }

        internal void TermIsInteger()
            { CreateTerm = IntegerCreator; }
    
        internal void TermIsDouble()
            { CreateTerm = DoubleCreator; }

        internal void TermIsDecimal()
            { CreateTerm = DecimalCreator; }

        internal void TermIsVar()
            { CreateTerm = VariableCreator; }

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

        private Term UndefinedTerm(string value)
            { throw new NotImplementedException(nameof(UndefinedTerm)); }

        private Term BooleanCreator(string value)
            { return new Literal(value, Ontologies.Xsd.boolean); }

        private Term IntegerCreator(string value)
            { return new Literal(value, Ontologies.Xsd.integer); }

        private Term FloatCreator(string value)
            { return new Literal(value, Ontologies.Xsd.@float); }

        private Term DoubleCreator(string value)
            { return new Literal(value, Ontologies.Xsd.@double); }

        private Term DecimalCreator(string value)
            { return new Literal(value, Ontologies.Xsd.@decimal); }

        private Term VariableCreator(string value)
            { return new Variable(value); }

        private Term IriCreator(string value)
            { return new Iri(value, Plan!.Namespaces); }

        private Term BlankCreator(string value)
            { return new Blank(value); }

        private Term AnonCreator(string value)
            { return NewAnonymous(); }

        private Term NilCreator(string value)
            { return Ontologies.Rdf.nil; }

        private Term StringCreator(string value)
            { return new Literal(value, Plan!.Namespaces); }

        private Term ACreator(string value)
            { return Ontologies.Rdf.type; }

        private long blankCount = 0;

        private Term NewAnonymous()
            { return Variable.NewAnonymous(blankCount++); }

        #endregion

        #region OrderBy

        internal void OrderByAsc()
        {
            LastGroup!.OrderByDescends.Add(false);
        }

        internal void OrderByDesc()
        {
            LastGroup!.OrderByDescends.Add(true);
        }

        internal void OrderByBind()
        {
            var body = Expressions.Pop();
            var castToString = Expression.Call(body, "ToString", null);
            var lambdaExpression = Expression.Lambda<Func<Func<string, string>, string>>(castToString, getVarParam);
            var compiledExpression = lambdaExpression.Compile();

            var anonVar = NewAnonymous() as Variable;

            LastGroup!.OrderByVars.Add(anonVar!);
            LastGroup!.ImplicitBinders.Add(compiledExpression);
            LastGroup!.ImplicitBindAsVars.Add(anonVar!);
        }

        #endregion

        #region Limit & Offset

        internal string Limit
            { set { LastGroup!.Limit = int.Parse(value); } }

        internal string Offset
            { set { LastGroup!.Offset = int.Parse(value); } }

        #endregion

        #region Expressions

        internal void Filter()
        {
            if (Expressions.Count == 0)
                return;

            var expr = Expressions.Pop();
            var castToLiteral = Expression.Convert(expr, typeof(Literal));
            var asBool = Expression.Property(castToLiteral, "AsBool");
            var lambdaExpression = Expression.Lambda<Func<Func<string, string>, bool>>(asBool, getVarParam);
            var compiledExpression = lambdaExpression.Compile();

            CurrentGroup.Filters.Add(compiledExpression);
        }

        internal void Having()
        {
            if (Expressions.Count == 0)
                return;

            var expr = Expressions.Pop();
            var castToLiteral = Expression.Convert(expr, typeof(Literal));
            var asBool = Expression.Property(castToLiteral, "AsBool");
            var lambdaExpression = Expression.Lambda<Func<Func<string, string>, bool>>(asBool, getVarParam);
            var compiledExpression = lambdaExpression.Compile();

            LastGroup!.AggregateVars.AddRange(CurrentAggregateVariables);
            CurrentAggregateVariables.Clear();

            LastGroup!.AggregateBinders.AddRange(CurrentAggregateBinders);
            CurrentAggregateBinders.Clear();

            LastGroup!.ImplicitBindAsVars.AddRange(CurrentImplicitBindVariables);
            CurrentImplicitBindVariables.Clear();

            LastGroup!.ImplicitBinders.AddRange(CurrentImplicitBindBinders);
            CurrentImplicitBindBinders.Clear();

            LastGroup!.AggregateDistincts.AddRange(CurrentAggregateDistincts);
            CurrentAggregateDistincts.Clear();

            LastGroup!.Filters.Add(compiledExpression);
        }

        internal void Bind()
        {
            var body = Expressions.Pop();
            var castToString = Expression.Call(body, "ToString", null);
            var lambdaExpression = Expression.Lambda<Func<Func<string, string>, string>>(castToString, getVarParam);
            var compiledExpression = lambdaExpression.Compile();

            CurrentGroup.ExplicitBinders.Add(compiledExpression);
        }

        internal void SelectBind()
        {
            var body = Expressions.Pop();
            var castToString = Expression.Call(body, "ToString", null);
            var lambdaExpression = Expression.Lambda<Func<Func<string, string>, string>>(castToString, getVarParam);
            var compiledExpression = lambdaExpression.Compile();

            CurrentSelectBinders.Add(compiledExpression);
        }

        internal void SymbolExpression(string value) 
        {
            Expression? expr = null;

            if (CreateTerm == VariableCreator)
            {
                 expr = Expression.Invoke(getVarParam, Expression.Constant(value));
                 expr = Expression.Call(typeof(Resource), "Parse", null, expr);
            } 
            else
            {
                expr = Expression.Constant(CreateTerm(value));
            }

            Expressions.Push(expr);
        }

        internal void OperatorExpressionIn()
        {
            var argCount = ArgCounts.Pop();
            Expression[] argExprs = new Expression[argCount];

            for (int i = argExprs.Length - 1; i >= 0; i--)
                argExprs[i] = Expressions.Pop();

            var arg = Expression.NewArrayInit(typeof(Resource), argExprs);

            var num = Expressions.Pop();

            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "In", null, num, arg));
        }

        internal void OperatorExpressionNotIn()
        {
            var argCount = ArgCounts.Pop();
            Expression[] argExprs = new Expression[argCount];

            for (int i = argExprs.Length - 1; i >= 0; i--)
                argExprs[i] = Expressions.Pop();

            var arg = Expression.NewArrayInit(typeof(Resource), argExprs);

            var num = Expressions.Pop();

            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "NotIn", null, num, arg));
        }

        #region Binary operator expressions
        internal void BinaryOperatorExpressionOr()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "Or", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionAnd()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "And", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionLessThan()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "LessThan", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionLessOrEqualThan()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "LessOrEqualThan", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionGreaterThan()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "GreaterThan", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionGreaterOrEqualThan()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "GreaterOrEqualThan", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionEquals()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "AreEqual", null, lhs, rhs));   // Named "AreEqual" to not collide with the Object.Equals() method!
        }

        internal void BinaryOperatorExpressionNotEquals()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "NotEquals", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionAdd()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "Add", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionSubtract()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "Subtract", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionMultiply()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "Multiply", null, lhs, rhs));
        }

        internal void BinaryOperatorExpressionDivide()
        {
            var rhs = Expressions.Pop();
            var lhs = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "Divide", null, lhs, rhs));
        }

        #endregion

        #region Unary operator expressions
        internal void UnaryOperatorExpressionNot()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "Not", null, arg));
        }

        internal void UnaryOperatorExpressionNegate()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.Operators), "Negate", null, arg));
        }

        internal void UnaryOperatorExpressionPlus()
        {
        }
        #endregion

        #region BuiltIn expressions

        internal void BuiltInSTR()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STR", null, arg));
        }

        internal void BuiltInLANG()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "LANG", null, arg));
        }

        internal void BuiltInLANGMATCHES()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "LANGMATCHES", null, arg1, arg2));
        }

        internal void BuiltInDATATYPE()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "DATATYPE", null, arg));
        }

        internal void BuiltInBOUND()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "BOUND", null, arg));
        }

        internal void BuiltInIRI()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "IRI", null, arg));
        }

        internal void BuiltInBNODE1()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "BNODE", null, arg));
        }

        internal void BuiltInBNODE0()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "BNODE", null));
        }

        internal void BuiltInRAND()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "RAND", null));
        }

        internal void BuiltInABS()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "ABS", null, arg));
        }

        internal void BuiltInCEIL()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "CEIL", null, arg));
        }

        internal void BuiltInFLOOR()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "FLOOR", null, arg));
        }

        internal void BuiltInROUND()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "ROUND", null, arg));
        }

        internal void BuiltInCONCAT()
        {
            var argCount = ArgCounts.Pop();
            Expression[] argExprs = new Expression[argCount];

            for (int i = argExprs.Length - 1; i >= 0 ; i--)
			        argExprs[i] = Expressions.Pop(); 

            var arg = Expression.NewArrayInit(typeof(Resource),  argExprs);

            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "CONCAT", null, arg));
        }

        internal void BuiltInSTRLEN()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STRLEN", null, arg));
        }

        internal void BuiltInUCASE()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "UCASE", null, arg));
        }

        internal void BuiltInLCASE()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "LCASE", null, arg));
        }

        internal void BuiltInSUBSTR2()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "SUBSTR", null, arg1, arg2));
        }

        internal void BuiltInSUBSTR3()
        {
            var arg3 = Expressions.Pop();
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "SUBSTR", null, arg1, arg2, arg3));
        }

        internal void BuiltInREPLACE3()
        {
            var arg3 = Expressions.Pop();
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "REPLACE", null, arg1, arg2, arg3));
        }

        internal void BuiltInREPLACE4()
        {
            var arg4 = Expressions.Pop();
            var arg3 = Expressions.Pop();
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "REPLACE", null, arg1, arg2, arg3, arg4));
        }

        internal void BuiltInENCODE_FOR_URI()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "ENCODE_FOR_URI", null, arg));
        }

        internal void BuiltInCONTAINS()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "CONTAINS", null, arg1, arg2));
        }

        internal void BuiltInSTRSTARTS()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STRSTARTS", null, arg1, arg2));
        }

        internal void BuiltInSTRENDS()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STRENDS", null, arg1, arg2));
        }

        internal void BuiltInSTRBEFORE()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STRBEFORE", null, arg1, arg2));
        }

        internal void BuiltInSTRAFTER()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STRAFTER", null, arg1, arg2));
        }

        internal void BuiltInYEAR()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "YEAR", null, arg));
        }

        internal void BuiltInMONTH()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "MONTH", null, arg));
        }

        internal void BuiltInDAY()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "DAY", null, arg));
        }

        internal void BuiltInHOURS()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "HOURS", null, arg));
        }

        internal void BuiltInMINUTES()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "MINUTES", null, arg));
        }

        internal void BuiltInSECONDS()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "SECONDS", null, arg));
        }

        internal void BuiltInTIMEZONE()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "TIMEZONE", null, arg));
        }

        internal void BuiltInTZ()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "TZ", null, arg));
        }

        internal void BuiltInNOW()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "NOW", null));
        }

        internal void BuiltInUUID()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "UUID", null));
        }

        internal void BuiltInSTRUUID()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STRUUID", null));
        }

        internal void BuiltInMD5()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "MD5", null));
        }

        internal void BuiltInSHA1()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "SHA1", null));
        }

        internal void BuiltInSHA256()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "SHA256", null));
        }

        internal void BuiltInSHA384()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "SHA384", null));
        }

        internal void BuiltInSHA512()
        {
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "SHA512", null));
        }

        internal void BuiltInCOALESCE()
        {
            var argCount = ArgCounts.Pop();
            Expression[] argExprs = new Expression[argCount];

            for (int i = argExprs.Length - 1; i >= 0; i--)
                argExprs[i] = Expressions.Pop();

            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "COALESCE", null, argExprs));
        }

        internal void BuiltInIF()
        {
            var arg3 = Expressions.Pop();
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "IF", null, arg1, arg2, arg3));
        }

        internal void BuiltInSTRLANG()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STRLANG", null, arg1, arg2));
        }

        internal void BuiltInSTRDT()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "STRDT", null, arg1, arg2));
        }

        internal void BuiltInSAMETERM()
        {
            var arg2 = Expressions.Pop();
            var arg1 = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "SAMETERM", null, arg1, arg2));
        }

        internal void BuiltInIS_IRI()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "IS_IRI", null, arg));
        }

        internal void BuiltInIS_BLANK()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "IS_BLANK", null, arg));
        }

        internal void BuiltInIS_LITERAL()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "IS_LITERAL", null, arg));
        }

        internal void BuiltInIS_NUMERIC()
        {
            var arg = Expressions.Pop();
            Expressions.Push(Expression.Call(Expression.Constant(Plan.BuiltIns), "IS_NUMERIC", null, arg));
        }

        #endregion
        
        internal void CountArgInit()
        {
            ArgCounts.Push(0);
        }

        internal void CountArg()
        {
            ArgCounts.Poke(ArgCounts.Peek() + 1);
        }

        #endregion

        #region Aggregations
        internal void AggDistinct()
            { CurrentAggregateDistincts.Add(true); }

        internal void AggNotDistinct()
            { CurrentAggregateDistincts.Add(false); }

        internal string GroupConcatSeparator { get; set; }

        private void BuildAggregateFunc(string function)
        {
            var body = Expressions.Pop();
            var castToString = Expression.Call(body, "ToString", null);
            var lambdaExpression = Expression.Lambda<Func<Func<string, string>, string>>(castToString, getVarParam);
            var compiledExpression = lambdaExpression.Compile();

            var anonVar = NewAnonymous() as Variable;
            CurrentImplicitBindBinders.Add(compiledExpression);
            CurrentImplicitBindVariables.Add(anonVar!);

            var name = anonVar!.Value;

            var value = Expression.Invoke(getVarParam, Expression.Constant(name));
            var resValue = Expression.Call(typeof(Resource), "Parse", null, value);

            var accumulated = Expression.Parameter(typeof(string));
            var resAccumulated = Expression.Call(typeof(Resource), "Parse", null, accumulated);

            var distinctSet = Expression.Parameter(typeof(HashSet<string>));

            Expression aggregateBody;
            if (function == "GROUP_CONCAT")
            {
                var resSeparator = Expression.Call(typeof(Resource), "Parse", null, Expression.Constant(GroupConcatSeparator));
                aggregateBody = Expression.Call(Expression.Constant(Plan.BuiltIns), function, null, resValue, resAccumulated, resSeparator, distinctSet);
                GroupConcatSeparator = "\" \"";
            }
            else
                aggregateBody = Expression.Call(Expression.Constant(Plan.BuiltIns), function, null, resValue, resAccumulated, distinctSet);

            var aggregateAsString = Expression.Call(aggregateBody, "ToString", null);
            var lambdaBinder = Expression.Lambda<Func<Func<string,string>,string, HashSet<string>,string>>(aggregateAsString, getVarParam, accumulated, distinctSet);

            var aggregateBinder = lambdaBinder.Compile();
            var aggregateVar = NewAnonymous() as Variable;

            CurrentAggregateBinders.Add(aggregateBinder);
            CurrentAggregateVariables.Add(aggregateVar!);

            var varReturn = Expression.Invoke(getVarParam, Expression.Constant(aggregateVar!.Value));
            var resReturn = Expression.Call(typeof(Resource), "Parse", null, varReturn);

            Expressions.Push(resReturn);
        }

        internal void AggSum()
        {
            BuildAggregateFunc("SUM");
        }

        internal void AggMin()
        {
            BuildAggregateFunc("MIN");
        }

        internal void AggMax()
        {
            BuildAggregateFunc("MAX");
        }

        internal void AggAvg()
        {
            BuildAggregateFunc("AVG");
        }

        internal void AggSample()
        {
            BuildAggregateFunc("SAMPLE");
        }

        internal void AggCountStar()
        {
            var value = Expression.Invoke(getVarParam, Expression.Constant("*"));

            var accumulated = Expression.Parameter(typeof(string));
            var resAccumulated = Expression.Call(typeof(Resource), "Parse", null, accumulated);

            var distinctSet = Expression.Parameter(typeof(HashSet<string>));

            var aggregateBody = Expression.Call(Expression.Constant(Plan.BuiltIns), "COUNTALL", null, value, resAccumulated, distinctSet);

            var aggregateAsString = Expression.Call(aggregateBody, "ToString", null);
            var lambdaBinder = Expression.Lambda<Func<Func<string, string>, string, HashSet<string>, string>>(aggregateAsString, getVarParam, accumulated, distinctSet);

            var aggregateBinder = lambdaBinder.Compile();
            var aggregateVar = NewAnonymous() as Variable;

            CurrentAggregateBinders.Add(aggregateBinder);
            CurrentAggregateVariables.Add(aggregateVar!);

            var varReturn = Expression.Invoke(getVarParam, Expression.Constant(aggregateVar!.Value));
            var resReturn = Expression.Call(typeof(Resource), "Parse", null, varReturn);

            Expressions.Push(resReturn);
        }

        internal void AggCountExpr()
        {
            BuildAggregateFunc("COUNT");
        }

        internal void AggGroupConcat()
        {
            BuildAggregateFunc("GROUP_CONCAT");
        }

        #endregion
    }
}
