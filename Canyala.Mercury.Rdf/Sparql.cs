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
using System.Threading.Tasks;

using Canyala.Lagoon.Core.Collections;
using Canyala.Lagoon.Core.Contracts;
using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;

using Canyala.Mercury.Core;
using Canyala.Mercury.Core.Text;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Internal;
using Canyala.Mercury.Storage.Collections;

namespace Canyala.Mercury.Rdf;

/// <summary>
/// Class to apply a SPARQL query text to a Rdf.Query object.
/// instance.
/// </summary>
public class Sparql : Parser<Query.Builder>
{
    #region Public API

    /// <summary>
    /// http://www.w3.org/1999/02/22-rdf-syntax-ns#type
    /// </summary>
    public static readonly string a = Ontologies.Rdf.ns["type"];

    /// <summary>
    /// Applies a SPARQL text to a Rdf.Query
    /// </summary>
    /// <param name="sparql">The SPARQL text.</param>
    /// <param name="query">The Rdf.Query.</param> 
    public static bool Translate(string sparql, Query query, out string errMsg)
    {
        var queryBuilder = new Query.Builder();

        if (Sparql.Instance.Apply(Comments.Trim(sparql), queryBuilder, out errMsg))
        {
            query.Plan = queryBuilder.ExtractFinalPlan;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Executes a sparql query against a rdf compliant data set.
    /// </summary>
    /// <param name="dataset">The data set.</param>
    /// <param name="sparql">SPARQL query text.</param>
    /// <returns></returns>
    public static Table Query(Dataset dataset, string sparql)
    {
        var query = new Query();
        if (!Sparql.Translate(sparql, query, out var errMsg))
            throw new Exception(errMsg);

        return query.AsTable(dataset);
    }

    #endregion

    #region SPARQL Productions 1.1
    /*
    The EBNF notation used in the grammar is defined in Extensible Markup Language (XML) 1.1 [XML11] section 6 Notation.

    Notes:

    Keywords are matched in a case-insensitive manner with the exception of the keyword 'a' which, in line with Turtle and 
    N3, is used in place of the IRI rdf:type (in full, http://www.w3.org/1999/02/22-rdf-syntax-ns#type).
    Escape sequences are case sensitive.
    When tokenizing the input and choosing grammar rules, the longest match is chosen.
    The SPARQL grammar is LL(1) when the rules with uppercased names are used as terminals. There are two entry points into 
    the grammar: QueryUnit for SPARQL queries, and UpdateUnit for SPARQL Update requests. In signed numbers, no white space 
    is allowed between the sign and the number. The AdditiveExpression grammar rule allows for this by covering the two cases 
    of an expression followed by a signed number. These produce an addition or subtraction of the unsigned number as appropriate.
    The tokens INSERT DATA, DELETE DATA, DELETE WHERE allow any amount of white space between the words. The single space version
    is used in the grammar for clarity. The QuadData and QuadPattern rules both use rule Quads. The rule QuadData, used in INSERT 
    DATA and DELETE DATA, must not allow variables in the quad patterns. Blank node syntax is not allowed in DELETE WHERE, the 
    DeleteClause for DELETE, nor in DELETE DATA. Rules for limiting the use of blank node labels are given in section 19.6. The number 
    of variables in the variable list of VALUES block must be the same as the number of each list of associated values in the 
    DataBlock. Variables introduced by AS in a SELECT clause must not already be in-scope. The variable assigned in a BIND clause 
    must not be already in-use within the immediately preceding TriplesBlock within a GroupGraphPattern. Aggregate functions can 
    be one of the built-in keywords for aggregates or a custom aggregate, which is syntactically a function call. Aggregate 
    functions may only be used in SELECT, HAVING and ORDER BY clauses. Only custom aggregate functions use the DISTINCT keyword 
    in a function call.
    */

    /// <summary>
    /// Productions declare sparql production rules
    /// based on the bnf grammar section at https://www.w3.org/TR/sparql11-query/#grammar
    /// </summary>
    private class Productions
    {
        // [1] ROOT! QueryUnit	  ::=  	Query
        public static readonly Func<Production> QueryUnit = () => _(nameof(QueryUnit), Query);

        // [2]  	Query	  ::=  	Prologue ( SelectQuery | ConstructQuery | DescribeQuery | AskQuery ) ValuesClause
        static readonly Func<Production> Query = () => _(nameof(Query), All(Prologue, CUT, AnyOf(SelectQuery, ConstructQuery, DescribeQuery, AskQuery), CUT, ValuesClause));

        // [3] ROOT! UpdateUnit	  ::=  	Update
        public static readonly Func<Production> UpdateUnit = () => _(nameof(UpdateUnit), Update);

        // [4]  	Prologue	  ::=  	( BaseDecl | PrefixDecl )*
        static readonly Func<Production> Prologue = () => ZeroOrMore(AnyOf(BaseDecl, PrefixDecl));

        // [5]  	BaseDecl	  ::=  	'BASE' IRIREF
        static readonly Func<Production> BaseDecl = () => _(nameof(BaseDecl), All(i("BASE"), Named("base", IRIREF), @Base));

        // [6]  	PrefixDecl	  ::=  	'PREFIX' PNAME_NS IRIREF
        static readonly Func<Production> PrefixDecl = () => _(nameof(PrefixDecl), All(i("PREFIX"), Named("prefix", PNAME_NS), Named("namespace", IRIREF), @PrefixAndNamespace));

        // [7]  	SelectQuery	  ::=  	SelectClause DatasetClause* WhereClause SolutionModifier
        static readonly Func<Production> SelectQuery = () => _(nameof(SelectQuery), All(SelectClause, ZeroOrMore(DatasetClause), WhereClause, SolutionModifier));

        // [8]  	SubSelect	  ::=  	SelectClause WhereClause SolutionModifier ValuesClause
        static readonly Func<Production> SubSelect = () => _(nameof(SubSelect), All(SelectClause, WhereClause, SolutionModifier, ValuesClause));

        // [9]  	SelectClause	  ::=  	'SELECT' ( 'DISTINCT' | 'C' )? ( ( Var | ( '(' Expression 'AS' Var ')' ) )+ | '*' )
        static readonly Func<Production> SelectClause = () =>
            _(nameof(SelectClause),
                All(Named("operation", i("SELECT")), @GroupOperation,
                    Optional(AnyOf(i("DISTINCT"), i("C")), @GroupDistinct),
                    AnyOf(OneOrMore(
                        AnyOf(
                            All(Named("selectvar", Var), @SelectVar),
                            All('(', Expression, @SelectBind, i("AS"), Named("selectvar", Var), ')', @SelectAsVar))),
                        All("*", @SelectAll)
                        )
                   ));

        // [10]  	ConstructQuery	  ::=  	'CONSTRUCT' ( ConstructTemplate DatasetClause* WhereClause SolutionModifier | DatasetClause* 'WHERE' '{' TriplesTemplate? '}' SolutionModifier )
        static readonly Func<Production> ConstructQuery = () =>
            _(nameof(ConstructQuery),
            All(Named("operation", i("CONSTRUCT")), @GroupOperation,
                AnyOf(
                    All(ConstructTemplate, ZeroOrMore(DatasetClause), WhereClause, SolutionModifier),
                    All(ZeroOrMore(DatasetClause), i("WHERE"), '{', Optional(TriplesTemplate), '}', SolutionModifier))));

        // [11]  	DescribeQuery	  ::=  	'DESCRIBE' ( VarOrIri+ | '*' ) DatasetClause* WhereClause? SolutionModifier
        static readonly Func<Production> DescribeQuery = () =>
            _(nameof(DescribeQuery), All(Named("operation", i("DESCRIBE")), @GroupOperation, AnyOf(ZeroOrMore(VarOrIri), '*'), ZeroOrMore(DatasetClause), Optional(WhereClause), SolutionModifier));

        /// <summary>
        /// [12]  	AskQuery	  ::=  	'ASK' DatasetClause* WhereClause SolutionModifier
        /// </summary>
        static readonly Func<Production> AskQuery = () => _(nameof(AskQuery), All(Named("operation", i("ASK")), @GroupOperation, ZeroOrMore(DatasetClause), WhereClause, SolutionModifier));

        /// <summary>
        /// [13]  	DatasetClause	  ::=  	'FROM' ( DefaultGraphClause | NamedGraphClause )
        /// </summary>
        static readonly Func<Production> DatasetClause = () => _(nameof(DatasetClause), All(i("FROM"), AnyOf(DefaultGraphClause, NamedGraphClause)));

        /// <summary>
        /// [14]  	DefaultGraphClause	  ::=  	SourceSelector
        /// </summary>
        static readonly Func<Production> DefaultGraphClause = () => _(nameof(DefaultGraphClause), SourceSelector);

        /// <summary>
        /// [15]  	NamedGraphClause	  ::=  	'NAMED' SourceSelector
        /// </summary>
        static readonly Func<Production> NamedGraphClause = () => _(nameof(NamedGraphClause), All(i("NAMED"), SourceSelector));

        /// <summary>
        /// [16]  	SourceSelector	  ::=  	iri
        /// </summary>
        static readonly Func<Production> SourceSelector = () => _(nameof(SourceSelector), iri);

        /// <summary>
        /// [17]  	WhereClause	  ::=  	'WHERE'? GroupGraphPattern
        /// </summary>
        static readonly Func<Production> WhereClause = () => _(nameof(WhereClause), All(Optional(i("WHERE")), GroupGraphPattern));

        /// <summary>
        /// [18]  	SolutionModifier	  ::=  	GroupClause? HavingClause? OrderClause? LimitOffsetClauses?
        /// </summary>
        static readonly Func<Production> SolutionModifier = () => _(nameof(SolutionModifier), All(Optional(GroupClause), Optional(HavingClause), Optional(OrderClause), Optional(LimitOffsetClauses)));

        /// <summary>
        /// [19]  	GroupClause	  ::=  	'GROUP' 'BY' GroupCondition+
        /// </summary>
        static readonly Func<Production> GroupClause = () => _(nameof(GroupClause), All(i("GROUP"), i("BY"), OneOrMore(GroupCondition)));

        /// <summary>
        /// [20]  	GroupCondition	  ::=  	BuiltInCall | FunctionCall | '(' Expression ( 'AS' Var )? ')' | Var
        /// </summary>
        static readonly Func<Production> GroupCondition = () =>
            _(nameof(GroupCondition),
                AnyOf
                (
                    All(Named("groupby", Var), @GroupByVar),
                    All(BuiltInCall, @GroupByBind),
                    All(FunctionCall, @GroupByBind),
                    All('(', Expression, Optional(i("AS"), All(Named("groupby", Var), @GroupByBindVar)), ')', @GroupByBind)
                )
             );

        /// <summary>
        /// [21]  	HavingClause	  ::=  	'HAVING' HavingCondition+
        /// </summary>
        static readonly Func<Production> HavingClause = () => _(nameof(HavingClause), All(i("HAVING"), OneOrMore(HavingCondition)));

        /// <summary>
        /// [22]  	HavingCondition	  ::=  	Constraint
        /// </summary>
        static readonly Func<Production> HavingCondition = () => _(nameof(HavingCondition), All(Constraint, @Having));

        /// <summary>
        /// [23]  	OrderClause	  ::=  	'ORDER' 'BY' OrderCondition+
        /// </summary>
        static readonly Func<Production> OrderClause = () => _(nameof(OrderClause), All(i("ORDER"), i("BY"), OneOrMore(OrderCondition)));

        /// <summary>
        /// [24]  	OrderCondition	  ::=  	 ( ( 'ASC' | 'DESC' ) BrackettedExpression ) | ( Constraint | Var )
        /// </summary>
        static readonly Func<Production> OrderCondition = () =>
            _(nameof(OrderCondition),
                AnyOf
                (
                    All(AnyOf(All(i("ASC"), @Ascending), All(i("DESC"), @Descending)), AnyOf(All('(', Named("orderbyvar", Var), ')', @OrderByVar), All(BrackettedExpression, @OrderByBind))),
                    AnyOf(All(@Ascending, Constraint, @OrderByBind), All(@Ascending, Named("orderbyvar", Var), @OrderByVar))
                )
            );


        /// <summary>
        /// [25]  	LimitOffsetClauses	  ::=  	LimitClause OffsetClause? | OffsetClause LimitClause?
        /// </summary>
        static readonly Func<Production> LimitOffsetClauses = () =>
            _(nameof(LimitOffsetClauses), AnyOf(All(LimitClause, Optional(OffsetClause)), All(OffsetClause, Optional(LimitClause))));

        /// <summary>
        /// [26]  	LimitClause	  ::=  	'LIMIT' INTEGER
        /// </summary>
        static readonly Func<Production> LimitClause = () => _(nameof(LimitClause), All(i("LIMIT"), Named("limit", INTEGER), @Limit));

        /// <summary>
        /// [27]  	OffsetClause	  ::=  	'OFFSET' INTEGER
        /// </summary>
        static readonly Func<Production> OffsetClause = () => _(nameof(OffsetClause), All(i("OFFSET"), Named("offset", INTEGER), @Offset));

        /// <summary>
        /// [28]  	ValuesClause	  ::=  	( 'VALUES' DataBlock )?
        /// </summary>
        static readonly Func<Production> ValuesClause = () => _(nameof(ValuesClause), Optional(All(Named("operation", i("VALUES")), @GroupOperation), DataBlock));

        /// <summary>
        /// [29]  	Update	  ::=  	Prologue ( Update1 ( ';' Update )? )?
        /// </summary>
        static readonly Func<Production> Update = () => _(nameof(Update), All(Prologue, Optional(Update1, Optional(';', Update))));

        /// <summary>
        /// [30]  	Update1	  ::=  	Load | Clear | Drop | Add | Move | Copy | Create | InsertData | DeleteData | DeleteWhere | Modify
        /// </summary>
        static readonly Func<Production> Update1 = () => _(nameof(Update1), AnyOf(Load, Clear, Drop, Add, Move, Copy, Create, InsertData, DeleteData, DeleteWhere, Modify));

        /// <summary>
        /// [31]  	Load	  ::=  	'LOAD' 'SILENT'? iri ( 'INTO' GraphRef )?
        /// </summary>
        static readonly Func<Production> Load = () => _(nameof(Load), All(i("LOAD"), Optional(i("SILENT")), iri, Optional(i("INTO"), GraphRef)));

        /// <summary>
        /// [32]  	Clear	  ::=  	'CLEAR' 'SILENT'? GraphRefAll
        /// </summary>
        static readonly Func<Production> Clear = () => _(nameof(Clear), All(i("CLEAR"), Optional(i("SILENT")), GraphRefAll));

        /// <summary>
        /// [33]  	Drop	  ::=  	'DROP' 'SILENT'? GraphRefAll
        /// </summary>
        static readonly Func<Production> Drop = () => _(nameof(Drop), All(i("DROP"), Optional(i("SILENT")), GraphRefAll));

        /// <summary>
        /// [34]  	Create	  ::=  	'CREATE' 'SILENT'? GraphRef
        /// </summary>
        static readonly Func<Production> Create = () => _(nameof(Create), All(i("CREATE"), Optional(i("SILENT")), GraphRef));

        /// <summary>
        /// [35]  	Add	  ::=  	'ADD' 'SILENT'? GraphOrDefault 'TO' GraphOrDefault
        /// </summary>
        static readonly Func<Production> Add = () => _(nameof(Add), All(i("ADD"), Optional(i("SILENT"), GraphOrDefault, i("TO"), GraphOrDefault)));

        /// <summary>
        /// [36]  	Move	  ::=  	'MOVE' 'SILENT'? GraphOrDefault 'TO' GraphOrDefault
        /// </summary>
        static readonly Func<Production> Move = () => _(nameof(Move), All(i("MOVE"), Optional(i("SILENT"), GraphOrDefault, i("TO"), GraphOrDefault)));

        /// <summary>
        /// [37]  	Copy	  ::=  	'COPY' 'SILENT'? GraphOrDefault 'TO' GraphOrDefault
        /// </summary>
        static readonly Func<Production> Copy = () => _(nameof(Copy), All(i("COPY"), Optional(i("SILENT"), GraphOrDefault, i("TO"), GraphOrDefault)));

        /// <summary>
        /// [38]  	InsertData	  ::=  	'INSERT DATA' QuadData
        /// </summary>
        static readonly Func<Production> InsertData = () => _(nameof(InsertData), All(i("INSERT DATA"), QuadData));

        /// <summary>
        /// [39]  	DeleteData	  ::=  	'DELETE DATA' QuadData
        /// </summary>
        static readonly Func<Production> DeleteData = () => _(nameof(DeleteData), All(i("DELETE DATA"), QuadData));

        /// <summary>
        /// [40]  	DeleteWhere	  ::=  	'DELETE WHERE' QuadPattern
        /// </summary>
        static readonly Func<Production> DeleteWhere = () => _(nameof(DeleteWhere), All(i("DELETE WHERE"), QuadPattern));

        /// <summary>
        /// [41]  	Modify	  ::=  	( 'WITH' iri )? ( DeleteClause InsertClause? | InsertClause ) UsingClause* 'WHERE' GroupGraphPattern
        /// </summary>
        static readonly Func<Production> Modify = () =>
            _(nameof(Modify),
                All(
                    Optional(All(i("WITH"), iri)),
                    AnyOf(
                        All(DeleteClause, Optional(InsertClause)),
                        InsertClause),
                    ZeroOrMore(UsingClause),
                    i("WHERE"),
                    GroupGraphPattern)
            );

        /// <summary>
        /// [42]  	DeleteClause	  ::=  	'DELETE' QuadPattern
        /// </summary>
        static readonly Func<Production> DeleteClause = () => _(nameof(DeleteClause), All(i("DELETE"), QuadPattern));

        /// <summary>
        /// [43]  	InsertClause	  ::=  	'INSERT' QuadPattern
        /// </summary>
        static readonly Func<Production> InsertClause = () => _(nameof(InsertClause), All(i("INSERT"), QuadPattern));

        /// <summary>
        /// [44]  	UsingClause	  ::=  	'USING' ( iri | 'NAMED' iri )
        /// </summary>
        static readonly Func<Production> UsingClause = () => _(nameof(UsingClause), All(i("USING"), AnyOf(iri, All(i("NAMED"), iri))));

        /// <summary>
        /// [45]  	GraphOrDefault	  ::=  	'DEFAULT' | 'GRAPH'? iri
        /// </summary>
        static readonly Func<Production> GraphOrDefault = () => _(nameof(GraphOrDefault), AnyOf(i("DEFAULT"), All(Optional(i("GRAPH")), iri)));

        /// <summary>
        /// [46]  	GraphRef	  ::=  	'GRAPH' iri
        /// </summary>
        static readonly Func<Production> GraphRef = () => _(nameof(GraphRef), All(i("GRAPH"), iri));

        /// <summary>
        /// [47]  	GraphRefAll	  ::=  	GraphRef | 'DEFAULT' | 'NAMED' | 'ALL'
        /// </summary>
        static readonly Func<Production> GraphRefAll = () => _(nameof(GraphRefAll), AnyOf(GraphRef, i("DEFAULT"), i("NAMED"), i("ALL")));

        /// <summary>
        /// [48]  	QuadPattern	  ::=  	'{' Quads '}'
        /// </summary>
        static readonly Func<Production> QuadPattern = () => _(nameof(QuadPattern), All('{', Quads, '}'));

        /// <summary>
        /// [49]  	QuadData	  ::=  	'{' Quads '}'
        /// </summary>
        static readonly Func<Production> QuadData = () => _(nameof(QuadData), All('{', Quads, '}'));

        /// <summary>
        /// [50]  	Quads	  ::=  	TriplesTemplate? ( QuadsNotTriples '.'? TriplesTemplate? )*
        /// </summary>
        static readonly Func<Production> Quads = () => _(nameof(Quads), All(Optional(TriplesTemplate), ZeroOrMore(QuadsNotTriples, Optional('.'), Optional(TriplesTemplate))));

        /// <summary>
        /// [51]  	QuadsNotTriples	  ::=  	'GRAPH' VarOrIri '{' TriplesTemplate? '}'
        /// </summary>
        static readonly Func<Production> QuadsNotTriples = () => _(nameof(QuadsNotTriples), All(i("GRAPH"), VarOrIri, '{', Optional(TriplesTemplate), '}'));

        /// <summary>
        /// [52]  	TriplesTemplate	  ::=  	TriplesSameSubject ( '.' TriplesTemplate? )?
        /// </summary>
        static readonly Func<Production> TriplesTemplate = () => _(nameof(TriplesTemplate), All(TriplesSameSubject, Optional('.', Optional(TriplesTemplate))));

        /// <summary>
        /// [53]  	GroupGraphPattern	  ::=  	'{' ( SubSelect | GroupGraphPatternSub ) '}'
        /// </summary>
        static readonly Func<Production> GroupGraphPattern = () => _(nameof(GroupGraphPattern), All(@BeginGroupGraphPattern, '{', AnyOf(SubSelect, GroupGraphPatternSub), '}', @EndGroupGraphPattern, CUT));

        /// <summary>
        /// [54]  	GroupGraphPatternSub	  ::=  	TriplesBlock? ( GraphPatternNotTriples '.'? TriplesBlock? )*
        /// </summary>
        static readonly Func<Production> GroupGraphPatternSub = () =>
            _(nameof(GroupGraphPatternSub),
            All(
                Optional(TriplesBlock),
                ZeroOrMore(GraphPatternNotTriples, Optional('.'), Optional(TriplesBlock))));

        /// <summary>
        /// [55]  	TriplesBlock	  ::=  	TriplesSameSubjectPath ( '.' TriplesBlock? )?
        /// </summary>
        static readonly Func<Production> TriplesBlock = () => _(nameof(TriplesBlock), All(TriplesSameSubjectPath, Optional('.', Optional(TriplesBlock))));

        /// <summary>
        /// [56]  	GraphPatternNotTriples	  ::=  	GroupOrUnionGraphPattern | OptionalGraphPattern | MinusGraphPattern | GraphGraphPattern | ServiceGraphPattern | Filter | Bind | InlineData
        /// </summary>
        static readonly Func<Production> GraphPatternNotTriples = () =>
            _(nameof(GraphPatternNotTriples), AnyOf(GroupOrUnionGraphPattern, OptionalGraphPattern, MinusGraphPattern, GraphGraphPattern, ServiceGraphPattern, _Filter, _Bind, InlineData));

        /// <summary>
        /// [57]  	OptionalGraphPattern	  ::=  	'OPTIONAL' GroupGraphPattern
        /// </summary>
        static readonly Func<Production> OptionalGraphPattern = () => _(nameof(OptionalGraphPattern), All(Named("operation", i("OPTIONAL")), @GroupOperation, GroupGraphPattern));

        /// <summary>
        /// [58]  	GraphGraphPattern	  ::=  	'GRAPH' VarOrIri GroupGraphPattern
        /// </summary>
        static readonly Func<Production> GraphGraphPattern = () => _(nameof(GraphGraphPattern), All(Named("operation", i("GRAPH")), @GroupOperation, VarOrIri, GroupGraphPattern));

        /// <summary>
        /// [59]  	ServiceGraphPattern	  ::=  	'SERVICE' 'SILENT'? VarOrIri GroupGraphPattern
        /// </summary>
        static readonly Func<Production> ServiceGraphPattern = () => _(nameof(ServiceGraphPattern), All(Named("operation", i("SERVICE")), @GroupOperation, Optional(i("SILENT")), VarOrIri, GroupGraphPattern));

        /// <summary>
        /// [60]  	Bind	  ::=  	'BIND' '(' Expression 'AS' Var ')'
        /// </summary>
        static readonly Func<Production> _Bind = () => _(nameof(Bind), All(i("BIND"), '(', Expression, @Bind, i("AS"), Named("bindvar", Var), ')', @BindAsVar));

        /// <summary>
        /// [61]  	InlineData	  ::=  	'VALUES' DataBlock
        /// </summary>
        static readonly Func<Production> InlineData = () => _(nameof(InlineData), All(Named("operation", i("VALUES")), @GroupOperation, DataBlock));

        /// <summary>
        /// [62]  	DataBlock	  ::=  	InlineDataOneVar | InlineDataFull
        /// </summary>
        static readonly Func<Production> DataBlock = () => _(nameof(DataBlock), AnyOf(InlineDataOneVar, InlineDataFull));

        /// <summary>
        /// [63]  	InlineDataOneVar	  ::=  	Var '{' DataBlockValue* '}'
        /// </summary>
        static readonly Func<Production> InlineDataOneVar = () =>
            _(nameof(InlineDataOneVar),
                All(
                    All(Named("valuevar", Var), @DataBlockVar),
                    @BeginGroupGraphPattern, '{', ZeroOrMore(All(@BeginDataBlock, Named("data", _DataBlockValue))), '}', @EndGroupGraphPattern));

        /// <summary>
        /// [64]  	InlineDataFull	  ::=  	( NIL | '(' Var* ')' ) '{' ( '(' DataBlockValue* ')' | NIL )* '}'
        /// </summary>
        static readonly Func<Production> InlineDataFull = () =>
            _(nameof(InlineDataFull),
                All(
                    AnyOf(NIL, All('(', ZeroOrMore(All(Named("valuevar", Var), @DataBlockVar)), ')')),
                    @BeginGroupGraphPattern, '{',
                    ZeroOrMore(AnyOf(NIL, All(@BeginDataBlock, '(', ZeroOrMore(Named("data", _DataBlockValue))), ')')),
                    '}', @EndGroupGraphPattern));

        /// <summary>
        /// [65]  	DataBlockValue	  ::=  	iri |	RDFLiteral |	NumericLiteral |	BooleanLiteral |	'UNDEF'
        /// </summary>
        static readonly Func<Production> _DataBlockValue = () =>
            _(nameof(DataBlockValue),
                All(
                    AnyOf(iri, RDFLiteral, NumericLiteral, BooleanLiteral, Named("node", i("UNDEF"))),
                    @DataBlockValue));

        /// <summary>
        /// [66]  	MinusGraphPattern	  ::=  	'MINUS' GroupGraphPattern
        /// </summary>
        static readonly Func<Production> MinusGraphPattern = () => _(nameof(MinusGraphPattern), All(Named("operation", i("MINUS")), @GroupOperation, GroupGraphPattern));

        /// <summary>
        /// [67]  	GroupOrUnionGraphPattern	  ::=  	GroupGraphPattern ( 'UNION' GroupGraphPattern )*
        /// </summary>
        static readonly Func<Production> GroupOrUnionGraphPattern = () => _(nameof(GroupOrUnionGraphPattern), All(GroupGraphPattern, ZeroOrMore(Named("operation", i("UNION")), @GroupOperation, GroupGraphPattern)));

        /// <summary>
        /// [68]  	Filter	  ::=  	'FILTER' Constraint
        /// </summary>
        static readonly Func<Production> _Filter = () => _(nameof(Filter), All(i("FILTER"), Constraint, @Filter));

        /// <summary>
        /// [69]  	Constraint	  ::=  	BrackettedExpression | BuiltInCall | FunctionCall
        /// </summary>
        static readonly Func<Production> Constraint = () => _(nameof(Constraint), AnyOf(BrackettedExpression, BuiltInCall, FunctionCall));

        /// <summary>
        /// [70]  	FunctionCall	  ::=  	iri ArgList
        /// </summary>
        static readonly Func<Production> FunctionCall = () => _(nameof(FunctionCall), All(iri, ArgList));

        /// <summary>
        /// [71]  	ArgList	  ::=  	NIL | '(' 'DISTINCT'? Expression ( ',' Expression )* ')'
        /// </summary>
        static readonly Func<Production> ArgList = () => _(nameof(ArgList), AnyOf(NIL, All('(', Optional(i("DISTINCT")), Expression, ZeroOrMore(',', Expression), ')')));

        /// <summary>
        /// [72]  	ExpressionList	  ::=  	NIL | '(' Expression ( ',' Expression )* ')'
        /// </summary>
        static readonly Func<Production> ExpressionList = () =>
            _(nameof(ExpressionList), All(@ArgInit, AnyOf(NIL, All('(', Expression, @Arg, ZeroOrMore(',', @Arg, Expression), ')'))));

        /// <summary>
        /// [73]  	ConstructTemplate	  ::=  	'{' ConstructTriples? '}'
        /// </summary>
        static readonly Func<Production> ConstructTemplate = () => _(nameof(ConstructTemplate), All(@BeginGroupGraphPattern, '{', Optional(ConstructTriples), '}', @EndGroupGraphPattern));

        /// <summary>
        /// [74]  	ConstructTriples	  ::=  	TriplesSameSubject ( '.' ConstructTriples? )?
        /// </summary>
        static readonly Func<Production> ConstructTriples = () => _(nameof(ConstructTriples), All(TriplesSameSubject, Optional('.', ConstructTriples)));

        /// <summary>
        /// [75]  	TriplesSameSubject	  ::=  	VarOrTerm PropertyListNotEmpty |	TriplesNode PropertyList
        /// </summary>
        static readonly Func<Production> TriplesSameSubject = () =>
            _(nameof(TriplesSameSubject),
                AnyOf(
                    All(All(Named("subject", VarOrTerm), @Subject), PropertyListNotEmpty),
                    All(@AllocAnonSubject, TriplesNode, PropertyList)));

        /// <summary>
        /// [76]  	PropertyList	  ::=  	PropertyListNotEmpty?
        /// </summary>
        static readonly Func<Production> PropertyList = () => _(nameof(PropertyList), Optional(PropertyListNotEmpty));

        /// <summary>
        /// [77]  	PropertyListNotEmpty	  ::=  	Verb ObjectList ( ';' ( Verb ObjectList )? )*
        /// </summary>
        static readonly Func<Production> PropertyListNotEmpty = () =>
            _(nameof(PropertyListNotEmpty), All(Verb, ObjectList, ZeroOrMore(';', Optional(Verb, ObjectList))));

        /// <summary>
        /// [78]  	Verb	  ::=  	VarOrIri | 'a'
        /// </summary>
        static readonly Func<Production> Verb = () => _(nameof(Verb), All(Named("predicate", AnyOf(VarOrIri, _A)), @Predicate));

        /// <summary>
        /// [79]  	ObjectList	  ::=  	Object ( ',' Object )*
        /// </summary>
        static readonly Func<Production> ObjectList = () => _(nameof(ObjectList), All(_Object, ZeroOrMore(',', _Object)));

        /// <summary>
        /// [80]  	Object	  ::=  	GraphNode
        /// </summary>
        static readonly Func<Production> _Object = () => _(nameof(Object), GraphNode);

        /// <summary>
        /// [81]  	TriplesSameSubjectPath	  ::=  	VarOrTerm PropertyListPathNotEmpty |	TriplesNodePath PropertyListPath
        /// </summary>
        static readonly Func<Production> TriplesSameSubjectPath = () =>
            _(nameof(TriplesSameSubjectPath),
                AnyOf(
                    All(All(Named("subject", VarOrTerm), @Subject), PropertyListPathNotEmpty),
                    All(@AllocAnonSubject, TriplesNodePath, PropertyListPath)));

        /// <summary>
        /// [82]  	PropertyListPath	  ::=  	PropertyListPathNotEmpty?
        /// </summary>
        static readonly Func<Production> PropertyListPath = () => _(nameof(PropertyListPath), Optional(PropertyListPathNotEmpty));

        /// <summary>
        /// [83]  	PropertyListPathNotEmpty	  ::=  	( VerbPath | VerbSimple ) ObjectListPath ( ';' ( ( VerbPath | VerbSimple ) ObjectList )? )*
        /// </summary>
        static readonly Func<Production> PropertyListPathNotEmpty = () =>
            _(nameof(PropertyListPathNotEmpty), All(AnyOf(VerbPath, VerbSimple), ObjectListPath, ZeroOrMore(';', Optional(AnyOf(VerbPath, VerbSimple), ObjectList))));

        /// <summary>
        /// [84]  	VerbPath	  ::=  	Path
        /// </summary>
        static readonly Func<Production> VerbPath = () => _(nameof(VerbPath), Path);

        /// <summary>
        /// [85]  	VerbSimple	  ::=  	Var
        /// </summary>
        static readonly Func<Production> VerbSimple = () => _(nameof(VerbSimple), All(Named("predicate", Var), @Predicate));

        /// <summary>
        /// [86]  	ObjectListPath	  ::=  	ObjectPath ( ',' ObjectPath )*
        /// </summary>
        static readonly Func<Production> ObjectListPath = () => _(nameof(ObjectListPath), All(ObjectPath, ZeroOrMore(',', ObjectPath)));

        /// <summary>
        /// [87]  	ObjectPath	  ::=  	GraphNodePath
        /// </summary>
        static readonly Func<Production> ObjectPath = () => _(nameof(ObjectPath), GraphNodePath);

        /// <summary>
        /// [88]  	Path	  ::=  	PathAlternative
        /// </summary>
        static readonly Func<Production> Path = () => _(nameof(Path), PathAlternative);

        /// <summary>
        /// [89]  	PathAlternative	  ::=  	PathSequence ( '|' PathSequence )*
        /// </summary>
        static readonly Func<Production> PathAlternative = () => _(nameof(PathAlternative), All(PathSequence, ZeroOrMore('|', PathSequence)));

        /// <summary>
        /// [90]  	PathSequence	  ::=  	PathEltOrInverse ( '/' PathEltOrInverse )*
        /// </summary>
        static readonly Func<Production> PathSequence = () => _(nameof(PathSequence), All(PathEltOrInverse, ZeroOrMore('/', PathEltOrInverse)));

        /// <summary>
        /// [91]  	PathElt	  ::=  	PathPrimary PathMod?
        /// </summary>
        static readonly Func<Production> PathElt = () => _(nameof(PathElt), Token(All(PathPrimary, Optional(PathMod))));

        /// <summary>
        /// [92]  	PathEltOrInverse	  ::=  	PathElt | '^' PathElt
        /// </summary>
        static readonly Func<Production> PathEltOrInverse = () => _(nameof(PathEltOrInverse), AnyOf(PathElt, All('^', PathElt)));

        /// <summary>
        /// [93]  	PathMod	  ::=  	'?' | '*' | '+'
        /// </summary>
        static readonly Func<Production> PathMod = () => _(nameof(PathMod), In('?', '*', '+'));

        /// <summary>
        /// [94]  	PathPrimary	  ::=  	iri | 'a' | '!' PathNegatedPropertySet | '(' Path ')'
        /// </summary>
        static readonly Func<Production> PathPrimary = () =>
            _(nameof(PathPrimary),
                AnyOf(
                    All(Named("predicate", iri), @Predicate),
                    All(Named("predicate", _A), @Predicate),
                    All('!', PathNegatedPropertySet), All('(', PathNegatedPropertySet, ')')));

        /// <summary>
        /// [95]  	PathNegatedPropertySet	  ::=  	PathOneInPropertySet | '(' ( PathOneInPropertySet ( '|' PathOneInPropertySet )* )? ')'
        /// </summary>
        static readonly Func<Production> PathNegatedPropertySet = () =>
            _(nameof(PathNegatedPropertySet), AnyOf(PathOneInPropertySet, All('(', Optional(PathOneInPropertySet, ZeroOrMore('|', PathOneInPropertySet)), ')')));

        /// <summary>
        /// [96]  	PathOneInPropertySet	  ::=  	iri | 'a' | '^' ( iri | 'a' )
        /// </summary>
        static readonly Func<Production> PathOneInPropertySet = () => _(nameof(PathOneInPropertySet), AnyOf(iri, _A, All('^', AnyOf(iri, _A))));

        /// <summary>
        /// [97]  	Integer	  ::=  	INTEGER
        /// </summary>
        static readonly Func<Production> _Integer = () => _(nameof(Integer), INTEGER);

        /// <summary>
        /// [98]  	TriplesNode	  ::=  	Collection |	BlankNodePropertyList
        /// </summary>
        static readonly Func<Production> TriplesNode = () => _(nameof(TriplesNode), AnyOf(Collection, BlankNodePropertyList));

        /// <summary>
        /// [99]  	BlankNodePropertyList	  ::=  	'[' PropertyListNotEmpty ']'
        /// </summary>
        static readonly Func<Production> BlankNodePropertyList = () => _(nameof(BlankNodePropertyList), All(@BeginPropertyList, '[', PropertyListNotEmpty, ']', @EndPropertyList));

        /// <summary>
        /// [100]  	TriplesNodePath	  ::=  	CollectionPath |	BlankNodePropertyListPath
        /// </summary>
        static readonly Func<Production> TriplesNodePath = () => _(nameof(TriplesNodePath), AnyOf(CollectionPath, BlankNodePropertyListPath));

        /// <summary>
        /// [101]  	BlankNodePropertyListPath	  ::=  	'[' PropertyListPathNotEmpty ']'
        /// </summary>
        static readonly Func<Production> BlankNodePropertyListPath = () => _(nameof(BlankNodePropertyListPath), All(@BeginPropertyList, '[', PropertyListPathNotEmpty, ']', @EndPropertyList));

        /// <summary>
        /// [102]  	Collection	  ::=  	'(' GraphNode+ ')'
        /// </summary>
        static readonly Func<Production> Collection = () => _(nameof(Collection), All(@BeginCollection, '(', OneOrMore(GraphNode), ')', @EndCollection));

        /// <summary>
        /// [103]  	CollectionPath	  ::=  	'(' GraphNodePath+ ')'
        /// </summary>
        static readonly Func<Production> CollectionPath = () => _(nameof(CollectionPath), All(@BeginCollection, '(', OneOrMore(GraphNodePath), ')', @EndCollection));

        /// <summary>
        /// [104]  	GraphNode	  ::=  	VarOrTerm |	TriplesNode
        /// </summary>
        static readonly Func<Production> GraphNode = () => _(nameof(GraphNode), AnyOf(All(Named("object", VarOrTerm), @Object), All(@AllocAnonObject, TriplesNode)));

        /// <summary>
        /// [105]  	GraphNodePath	  ::=  	VarOrTerm |	TriplesNodePath
        /// </summary>
        static readonly Func<Production> GraphNodePath = () => _(nameof(GraphNodePath), AnyOf(All(Named("object", VarOrTerm), @Object), All(@AllocAnonObject, TriplesNodePath)));

        /// <summary>
        /// [106]  	VarOrTerm	  ::=  	Var | GraphTerm
        /// </summary>
        static readonly Func<Production> VarOrTerm = () => _(nameof(VarOrTerm), AnyOf(Var, GraphTerm));

        /// <summary>
        /// [107]  	VarOrIri	  ::=  	Var | iri
        /// </summary>
        static readonly Func<Production> VarOrIri = () => _(nameof(VarOrIri), AnyOf(Var, iri));

        /// <summary>
        /// [108]  	Var	  ::=  	VAR1 | VAR2
        /// </summary>
        static readonly Func<Production> Var = () => _(nameof(Var), All(AnyOf(VAR1, VAR2), @Variable));

        /// <summary>
        /// [109]  	GraphTerm	  ::=  	iri |	RDFLiteral |	NumericLiteral |	BooleanLiteral |	BlankNode |	NIL
        /// </summary>
        static readonly Func<Production> GraphTerm = () => _(nameof(GraphTerm), AnyOf(iri, RDFLiteral, NumericLiteral, BooleanLiteral, BlankNode, Named("node", NIL)));

        #region Grammar Production Declarations - Grammar

        /// <summary>
        /// [110]  	Expression	  ::=  	ConditionalOrExpression
        /// </summary>
        static readonly Func<Production> Expression = () => _(nameof(Expression), ConditionalOrExpression);

        /// <summary>
        /// [111]  	ConditionalOrExpression	  ::=  	ConditionalAndExpression ( '||' ConditionalAndExpression )*
        /// </summary>
        static readonly Func<Production> ConditionalOrExpression = () =>
            _(nameof(ConditionalOrExpression), All(ConditionalAndExpression, ZeroOrMore("||", ConditionalAndExpression, @OperatorOr)));

        /// <summary>
        /// [112]  	ConditionalAndExpression	  ::=  	ValueLogical ( '&&' ValueLogical )*
        /// </summary>
        static readonly Func<Production> ConditionalAndExpression = () => _(nameof(ConditionalAndExpression), All(ValueLogical, ZeroOrMore("&&", ValueLogical, @OperatorAnd)));

        /// <summary>
        /// [113]  	ValueLogical	  ::=  	RelationalExpression
        /// </summary>
        static readonly Func<Production> ValueLogical = () => _(nameof(ValueLogical), RelationalExpression);

        /// <summary>
        /// [114]  	RelationalExpression	  ::=  	NumericExpression ( '=' NumericExpression | '!=' NumericExpression | '&lt;' NumericExpression | '>' NumericExpression 
        /// | '<=' NumericExpression | '>=' NumericExpression | 'IN' ExpressionList | 'NOT' 'IN' ExpressionList )?
        /// </summary>
        static readonly Func<Production> RelationalExpression = () =>
            _(nameof(RelationalExpression),
                All(
                    NumericExpression,
                    Optional(
                        AnyOf(
                            All("!=", NumericExpression, @OperatorNotEquals),
                            All("<=", NumericExpression, @OperatorLessOrEqualThan),
                            All(">=", NumericExpression, @OperatorGreaterOrEqualThan),
                            All('=', NumericExpression, @OperatorEquals),
                            All('<', NumericExpression, @OperatorLessThan),
                            All('>', NumericExpression, @OperatorGreaterThan),
                            All(i("IN"), ExpressionList, @OperatorIn),
                            All(i("NOT"), i("IN"), ExpressionList, @OperatorNotIn)
            ))));

        /// <summary>
        /// [115]  	NumericExpression	  ::=  	AdditiveExpression
        /// </summary>
        static readonly Func<Production> NumericExpression = () => _(nameof(NumericExpression), AdditiveExpression);

        /// <summary>
        /// [116]  	AdditiveExpression	  ::= MultiplicativeExpression ( '+' MultiplicativeExpression | '-' MultiplicativeExpression | ( NumericLiteralPositive | NumericLiteralNegative ) ( ( '*' UnaryExpression ) | ( '/' UnaryExpression ) )* )*
        /// </summary>
        static readonly Func<Production> AdditiveExpression = () =>
            _(nameof(AdditiveExpression),
                All(
                    MultiplicativeExpression,
                        ZeroOrMore(
                            AnyOf(
                                All('+', MultiplicativeExpression, @OperatorAdd),
                                All('-', MultiplicativeExpression, @OperatorSubtract),
                                AnyOf(
                                    All(NumericLiteralPositive, @Symbol),
                                    All(NumericLiteralNegative, @Symbol),
                                    ZeroOrMore(
                                        AnyOf(
                                            All('*', UnaryExpression, @OperatorMultiply),
                                            All('/', UnaryExpression, @OperatorDivide))))))));

        /// <summary>
        /// [117]  	MultiplicativeExpression	  ::=  	UnaryExpression ( '*' UnaryExpression | '/' UnaryExpression )*
        /// </summary>
        static readonly Func<Production> MultiplicativeExpression = () =>
            _(nameof(MultiplicativeExpression),
            All(
                UnaryExpression,
                ZeroOrMore(
                    AnyOf(
                        All('*', UnaryExpression, @OperatorMultiply),
                        All('/', UnaryExpression, @OperatorDivide)))));

        /// <summary>
        /// [118]  	UnaryExpression	  ::=  	  '!' PrimaryExpression 
        ///|	'+' PrimaryExpression 
        ///|	'-' PrimaryExpression 
        ///|	PrimaryExpression
        /// </summary>
        static readonly Func<Production> UnaryExpression = () =>
            _(nameof(UnaryExpression),
                AnyOf(
                    All('!', PrimaryExpression, @OperatorNot),
                    All('+', PrimaryExpression, @OperatorPlus),
                    All('-', PrimaryExpression, @OperatorNegate),
                    PrimaryExpression
            ));

        /// <summary>
        /// [119]  	PrimaryExpression	  ::=  	BrackettedExpression | BuiltInCall | iriOrFunction | RDFLiteral | NumericLiteral | BooleanLiteral | Var
        /// </summary>
        static readonly Func<Production> PrimaryExpression = () =>
            _(nameof(PrimaryExpression),
                AnyOf(
                    BrackettedExpression,
                    BuiltInCall,
                    iriOrFunction,
                    All(RDFLiteral, @Symbol),
                    All(NumericLiteral, @Symbol),
                    All(BooleanLiteral, @Symbol),
                    All(Var, @Symbol)));

        /// <summary>
        /// [120]  	BrackettedExpression	  ::=  	'(' Expression ')'
        /// </summary>
        static readonly Func<Production> BrackettedExpression = () => _(nameof(BrackettedExpression), All('(', Expression, ')'));

        /// <summary>
        /// [121]  	BuiltInCall	  ::=  	  Aggregate 
        /// </summary>
        static readonly Func<Production> BuiltInCall = () =>
            _(nameof(BuiltInCall),
                    AnyOf(Aggregate,                                                                    //  Aggregate
                        All(i("STR"), '(', Expression, ')', @STR),                                      //|	'STR' '(' Expression ')' 
                        All(i("LANG"), '(', Expression, ')', @LANG),                                    //|	'LANG' '(' Expression ')' 
                        All(i("LANGMATCHES"), '(', Expression, ',', Expression, ')', @LANGMATCHES),     //|	'LANGMATCHES' '(' Expression ',' Expression ')' 
                        All(i("DATATYPE"), '(', Expression, ')', @DATATYPE),                            //|	'DATATYPE' '(' Expression ')'
                        All(i("BOUND"), '(', Var, ')', @BOUND),                                         //|	'BOUND' '(' Var ')'
                        All(i("IRI"), '(', Expression, ')', @IRI),                                      //|	'IRI' '(' Expression ')'
                        All(i("URI"), '(', Expression, ')', @IRI),                                      //|	'URI' '(' Expression ')'
                        All(i("BNODE"), AnyOf(All('(', Expression, ')', @BNODE1), All(NIL, @BNODE0))),  //|	'BNODE' ( '(' Expression ')' | NIL ) 
                        All(i("RAND"), NIL, @RAND),                                                     //|	'RAND' NIL
                        All(i("ABS"), '(', Expression, ')', @ABS),                                      //|	'ABS' '(' Expression ')'
                        All(i("CEIL"), '(', Expression, ')', @CEIL),                                    //| 'CEIL' '(' Expression ')'
                        All(i("FLOOR"), '(', Expression, ')', @FLOOR),                                  //|	'FLOOR' '(' Expression ')'
                        All(i("ROUND"), '(', Expression, ')', @ROUND),                                  //|	'ROUND' '(' Expression ')'
                        All(i("CONCAT"), ExpressionList, @CONCAT),                                      //|	'CONCAT' ExpressionList
                        SubstringExpression,                                                            //|	SubstringExpression 
                        All(i("STRLEN"), '(', Expression, ')', @STRLEN),                                //|	'STRLEN' '(' Expression ')' 
                        StrReplaceExpression,                                                           //|	StrReplaceExpression 
                        All(i("UCASE"), '(', Expression, ')', @UCASE),                                  //|	'UCASE' '(' Expression ')'
                        All(i("LCASE"), '(', Expression, ')', @LCASE),                                  //|	'LCASE' '(' Expression ')'
                        All(i("ENCODE_FOR_URI"), '(', Expression, ')', @ENCODE_FOR_URI),                //|	'ENCODE_FOR_URI' '(' Expression ')'
                        All(i("CONTAINS"), '(', Expression, ',', Expression, ')', @CONTAINS),           //|	'CONTAINS' '(' Expression ',' Expression ')' 
                        All(i("STRSTARTS"), '(', Expression, ',', Expression, ')', @STRSTARTS),         //|	'STRSTARTS' '(' Expression ',' Expression ')' 
                        All(i("STRENDS"), '(', Expression, ',', Expression, ')', @STRENDS),             //|	'STRENDS' '(' Expression ',' Expression ')' 
                        All(i("STRBEFORE"), '(', Expression, ',', Expression, ')', @STRBEFORE),         //|	'STRBEFORE' '(' Expression ',' Expression ')' 
                        All(i("STRAFTER"), '(', Expression, ',', Expression, ')', @STRAFTER),           //|	'STRAFTER' '(' Expression ',' Expression ')' 
                        All(i("YEAR"), '(', Expression, ')', @YEAR),                                    //|	'YEAR' '(' Expression ')'
                        All(i("MONTH"), '(', Expression, ')', @MONTH),                                  //|	'MONTH' '(' Expression ')'
                        All(i("DAY"), '(', Expression, ')', @DAY),                                      //|	'DAY' '(' Expression ')'
                        All(i("HOURS"), '(', Expression, ')', @HOURS),                                  //|	'HOURS' '(' Expression ')'
                        All(i("MINUTES"), '(', Expression, ')', @MINUTES),                              //|	'MINUTES' '(' Expression ')'
                        All(i("SECONDS"), '(', Expression, ')', @SECONDS),                              //|	'SECONDS' '(' Expression ')'
                        All(i("TIMEZONE"), '(', Expression, ')', TIMEZONE),                             //|	'TIMEZONE' '(' Expression ')'
                        All(i("TZ"), '(', Expression, ')', @TZ),                                        //|	'TZ' '(' Expression ')'
                        All(i("NOW"), NIL, @NOW),                                                       //|	'NOW' NIL
                        All(i("UUID"), NIL, @UUID),                                                     //|	'UUID' NIL
                        All(i("STRUUID"), NIL, @STRUUID),                                               //|	'STRUUID' NIL
                        All(i("MD5"), '(', Expression, ')', @MD5),                                      //|	'MD5' '(' Expression ')'
                        All(i("SHA1"), '(', Expression, ')', @SHA1),                                    //|	'SHA1' '(' Expression ')'
                        All(i("SHA256"), '(', Expression, ')', @SHA256),                                //|	'SHA256' '(' Expression ')'
                        All(i("SHA384"), '(', Expression, ')', @SHA384),                                //|	'SHA384' '(' Expression ')'
                        All(i("SHA512"), '(', Expression, ')', @SHA512),                                //|	'SHA512' '(' Expression ')'
                        All(i("COALESCE"), ExpressionList, @COALESCE),                                  //|	'COALESCE' ExpressionList
                        All(i("IF"), '(', Expression, ',', Expression, ',', Expression, ')', @IF),      //|	'IF' '(' Expression ',' Expression ',' Expression ')'
                        All(i("STRLANG"), '(', Expression, ',', Expression, ')', @STRLANG),             //|	'STRLANG' '(' Expression ',' Expression ')'
                        All(i("STRDT"), '(', Expression, ',', Expression, ')', @STRDT),                 //|	'STRDT' '(' Expression ',' Expression ')'
                        All("sameTerm", '(', Expression, ',', Expression, ')', @SAMETERM),              //|	'sameTerm' '(' Expression ',' Expression ')'
                        All(i("isIRI"), '(', Expression, ')', @IS_IRI),                                 //|	'isIRI' '(' Expression ')'
                        All(i("isURI"), '(', Expression, ')', @IS_IRI),                                 //|	'isURI' '(' Expression ')'
                        All(i("isBLANK"), '(', Expression, ')', @IS_BLANK),                             //|	'isBLANK' '(' Expression ')'
                        All(i("isLITERAL"), '(', Expression, ')', @IS_LITERAL),                         //|	'isLITERAL' '(' Expression ')'
                        All(i("isNUMERIC"), '(', Expression, ')', @IS_NUMERIC),                         //|	'isNUMERIC' '(' Expression ')'
                        RegexExpression,                                                                //|	RegexExpression 
                        ExistsFunc,                                                                     //|	ExistsFunc 
                        NotExistsFunc                                                                   //|	NotExistsFunc 
            ));


        /// <summary>
        /// [122]  	RegexExpression	  ::=  	'REGEX' '(' Expression ',' Expression ( ',' Expression )? ')'
        /// </summary>
        static readonly Func<Production> RegexExpression = () =>
            _(nameof(RegexExpression), All(i("REGEX"), '(', Expression, ',', Expression, Optional(',', Expression), ')'));

        /// <summary>
        /// [123]  	SubstringExpression	  ::=  	'SUBSTR' '(' Expression ',' Expression ( ',' Expression )? ')'
        /// </summary>
        static readonly Func<Production> SubstringExpression = () =>
            _(nameof(SubstringExpression), All(i("SUBSTR"), '(', Expression, ',', Expression, AnyOf(@SUBSTR2, Optional(',', Expression, @SUBSTR3)), ')'));

        /// <summary>
        /// [124]  	StrReplaceExpression	  ::=  	'REPLACE' '(' Expression ',' Expression ',' Expression ( ',' Expression )? ')'
        /// </summary>
        static readonly Func<Production> StrReplaceExpression = () =>
            _(nameof(StrReplaceExpression), All(i("REPLACE"), '(', Expression, ',', Expression, ',', Expression, AnyOf(@REPLACE3, Optional(',', Expression, @REPLACE4), ')')));

        /// <summary>
        /// [125]  	ExistsFunc	  ::=  	'EXISTS' GroupGraphPattern
        /// </summary>
        static readonly Func<Production> ExistsFunc = () => _(nameof(ExistsFunc), All(Named("operation", i("EXISTS")), @GroupOperation, GroupGraphPattern));

        /// <summary>
        /// [126]  	NotExistsFunc	  ::=  	'NOT' 'EXISTS' GroupGraphPattern
        /// </summary>
        static readonly Func<Production> NotExistsFunc = () => _(nameof(NotExistsFunc), All(i("NOT"), i("EXISTS"), SetName("operation", "NOTEXISTS", @GroupOperation), GroupGraphPattern));

        /// <summary>
        /// [127]  	Aggregate	  ::=  	  'COUNT' '(' 'DISTINCT'? ( '*' | Expression ) ')' 
        ///| 'SUM' '(' 'DISTINCT'? Expression ')' 
        ///| 'MIN' '(' 'DISTINCT'? Expression ')' 
        ///| 'MAX' '(' 'DISTINCT'? Expression ')' 
        ///| 'AVG' '(' 'DISTINCT'? Expression ')' 
        ///| 'SAMPLE' '(' 'DISTINCT'? Expression ')' 
        ///| 'GROUP_CONCAT' '(' 'DISTINCT'? Expression ( ';' 'SEPARATOR' '=' String )? ')'
        /// </summary>
        static readonly Func<Production> Aggregate = () =>
            _(nameof(Aggregate),
                AnyOf(
                    All(i("COUNT"), '(', AggregateDistinct,
                        AnyOf(All('*', @AggCountStar), All(Expression, @AggCountExpr)), ')'),             //| 'COUNT' '(' 'DISTINCT'? ( '*' | Expression ) ')' 
                    All(i("SUM"), '(', AggregateDistinct, Expression, ')', @AggSum),                     //| 'SUM' '(' 'DISTINCT'? Expression ')' 
                    All(i("MIN"), '(', AggregateDistinct, Expression, ')', @AggMin),                     //| 'MIN' '(' 'DISTINCT'? Expression ')' 
                    All(i("MAX"), '(', AggregateDistinct, Expression, ')', @AggMax),                     //| 'MAX' '(' 'DISTINCT'? Expression ')' 
                    All(i("AVG"), '(', AggregateDistinct, Expression, ')', @AggAvg),                     //| 'AVG' '(' 'DISTINCT'? Expression ')' 
                    All(i("SAMPLE"), '(', AggregateDistinct, Expression, ')', @AggSample),               //| 'SAMPLE' '(' 'DISTINCT'? Expression ')' 
                    All(i("GROUP_CONCAT"), '(', AggregateDistinct, Expression,                           //| 'GROUP_CONCAT' '(' 'DISTINCT'? Expression ( ';' 'SEPARATOR' '=' String )? ')'
                            Optional(';', i("SEPARATOR"), '=', All(Named("separator", _String), @GroupConcatSeparator)), ')', @AggGroupConcat)
            ));

        static readonly Func<Production> AggregateDistinct = () => _(nameof(AggregateDistinct), AnyOf(All(i("DISTINCT"), @AggDistinct), @AggNotDistinct));
        #endregion

        /// <summary>
        /// [128]  	iriOrFunction	  ::=  	iri ArgList?
        /// </summary>
        static readonly Func<Production> iriOrFunction = () => _(nameof(iriOrFunction), All(iri, Optional(ArgList)));

        /// <summary>
        /// [129]  	RDFLiteral	  ::=  	String ( LANGTAG | ( '^^' iri ) )?
        /// </summary>
        static readonly Func<Production> RDFLiteral = () => _(nameof(RDFLiteral), Named("node", All(Token(_String, Optional(AnyOf(LANGTAG, All("^^", iri)))), @String)));

        /// <summary>
        /// [130]  	NumericLiteral	  ::=  	NumericLiteralUnsigned | NumericLiteralPositive | NumericLiteralNegative
        /// </summary>
        static readonly Func<Production> NumericLiteral = () => Token(Named("node", AnyOf(NumericLiteralUnsigned, NumericLiteralPositive, NumericLiteralNegative)));

        /// <summary>
        /// [131]  	NumericLiteralUnsigned	  ::=  	INTEGER |	DECIMAL |	DOUBLE
        /// </summary>
        static readonly Func<Production> NumericLiteralUnsigned = () =>
            AnyOf(
                All(DOUBLE, @Double),
                All(DECIMAL, @Decimal),
                All(INTEGER, @Integer));

        /// <summary>
        /// [132]  	NumericLiteralPositive	  ::=  	INTEGER_POSITIVE |	DECIMAL_POSITIVE |	DOUBLE_POSITIVE
        /// </summary>
        static readonly Func<Production> NumericLiteralPositive = () =>
            AnyOf(
                All(DOUBLE_POSITIVE, @Double),
                All(DECIMAL_POSITIVE, @Decimal),
                All(INTEGER_POSITIVE, @Integer));

        /// <summary>
        /// [133]  	NumericLiteralNegative	  ::=  	INTEGER_NEGATIVE |	DECIMAL_NEGATIVE |	DOUBLE_NEGATIVE
        /// </summary>
        static readonly Func<Production> NumericLiteralNegative = () =>
            AnyOf(
                All(DOUBLE_NEGATIVE, @Double),
                All(DECIMAL_NEGATIVE, @Decimal),
                All(INTEGER_NEGATIVE, @Integer));

        /// <summary>
        /// [134]  	BooleanLiteral	  ::=  	'true' |	'false'
        /// </summary>
        static readonly Func<Production> BooleanLiteral = () => _(nameof(BooleanLiteral), All(AnyOf("true", "false"), @Boolean));

        /// <summary>
        /// [135]  	String	  ::=  	STRING_LITERAL1 | STRING_LITERAL2 | STRING_LITERAL_LONG1 | STRING_LITERAL_LONG2
        /// </summary>
        static readonly Func<Production> _String = () => _(nameof(String), Token(AnyOf(STRING_LITERAL1, STRING_LITERAL2, STRING_LITERAL_LONG1, STRING_LITERAL_LONG2)));

        /// <summary>
        /// [136]  	iri	  ::=  	IRIREF |	PrefixedName
        /// </summary>
        static readonly Func<Production> iri = () => _(nameof(iri), All(AnyOf(IRIREF, PrefixedName), @Iri));

        /// <summary>
        /// [137]  	PrefixedName	  ::=  	PNAME_LN | PNAME_NS
        /// </summary>
        static readonly Func<Production> PrefixedName = () => _(nameof(PrefixedName), Token(AnyOf(PNAME_LN, PNAME_NS)));

        /// <summary>
        /// [138]  	BlankNode	  ::=  	BLANK_NODE_LABEL |	ANON
        /// </summary>
        static readonly Func<Production> BlankNode = () =>
            Token(
                AnyOf(
                    All(BLANK_NODE_LABEL, @Blank),
                    All(ANON, @Anon)));

        static readonly Func<Production> _A = () => Token(All(Named("node", 'a'), @A));

        // ---- Productions for terminals: -------------------------------------------------------------------------------------

        #region Grammar Production Declarations - Terminals

        /// <summary>
        /// [139]  	IRIREF	  ::=  	'<' ([^<>"{}|^`\]-[#x00-#x20])* '>'
        /// </summary>
        static readonly Func<Production> IRIREF = () =>
            Token(Named("node", All('<', Named("iri", ZeroOrMore(NotIn(Seq.Of('^', '<', '>', '"', '{', '}', '|', '\\', '´').Concat('\x00'.UpTo('\x20')).ToArray()))), '>')));

        /// <summary>
        /// [140]  	PNAME_NS	  ::=  	PN_PREFIX? ':'
        /// </summary>
        static readonly Func<Production> PNAME_NS = () => All(Named("name", Optional(PN_PREFIX)), ':');

        /// <summary>
        /// [141]  	PNAME_LN	  ::=  	PNAME_NS PN_LOCAL
        /// </summary>
        static readonly Func<Production> PNAME_LN = () => Named("node", All(PNAME_NS, PN_LOCAL));

        /// <summary>
        /// [142]  	BLANK_NODE_LABEL	  ::=  	'_:' ( PN_CHARS_U | [0-9] ) ((PN_CHARS|'.')* PN_CHARS)?
        /// </summary>
        static readonly Func<Production> BLANK_NODE_LABEL = () => Named("node", All("_:", AnyOf(PN_CHARS_U, DIGIT), Optional(ZeroOrMore(AnyOf(PN_CHARS, '.')), PN_CHARS)));

        /// <summary>
        /// [143]  	VAR1	  ::=  	'?' VARNAME
        /// </summary>
        static readonly Func<Production> VAR1 = () => Token(All('?', Named("node", VARNAME)));

        /// <summary>
        /// [144]  	VAR2	  ::=  	'$' VARNAME
        /// </summary>
        static readonly Func<Production> VAR2 = () => Token(All('$', Named("node", VARNAME)));

        /// <summary>
        /// [145]  	LANGTAG	  ::=  	'@' [a-zA-Z]+ ('-' [a-zA-Z0-9]+)*
        /// </summary>
        static readonly Func<Production> LANGTAG = () => All('@', OneOrMore(All(LETTER, ZeroOrMore(All('-', OneOrMore(DIGIT_OR_LETTER))))));

        //[146]  	INTEGER	  ::=  	[0-9]+
        //[147]  	DECIMAL	  ::=  	[0-9]* '.' [0-9]+
        //[148]  	DOUBLE	  ::=  	[0-9]+ '.' [0-9]* EXPONENT | '.' ([0-9])+ EXPONENT | ([0-9])+ EXPONENT
        // Inherited from Parser<>

        /// <summary>
        /// [149]  	INTEGER_POSITIVE	  ::=  	'+' INTEGER
        /// </summary>
        static readonly Func<Production> INTEGER_POSITIVE = () => All('+', INTEGER);

        /// <summary>
        /// [150]  	DECIMAL_POSITIVE	  ::=  	'+' DECIMAL
        /// </summary>
        static readonly Func<Production> DECIMAL_POSITIVE = () => All('+', DECIMAL);

        /// <summary>
        /// [151]  	DOUBLE_POSITIVE	  ::=  	'+' DOUBLE
        /// </summary>
        static readonly Func<Production> DOUBLE_POSITIVE = () => All('+', DOUBLE);

        /// <summary>
        /// [152]  	INTEGER_NEGATIVE	  ::=  	'-' INTEGER
        /// </summary>
        static readonly Func<Production> INTEGER_NEGATIVE = () => All('-', INTEGER);

        /// <summary>
        /// [153]  	DECIMAL_NEGATIVE	  ::=  	'-' DECIMAL
        /// </summary>
        static readonly Func<Production> DECIMAL_NEGATIVE = () => All('-', DECIMAL);

        /// <summary>
        /// [154]  	DOUBLE_NEGATIVE	  ::=  	'-' DOUBLE
        /// </summary>
        static readonly Func<Production> DOUBLE_NEGATIVE = () => All('-', DOUBLE);

        //[155]  	EXPONENT	  ::=  	[eE] [+-]? [0-9]+
        // inherited from Parser<>

        /// <summary>
        /// [156]  	STRING_LITERAL1	  ::=  	"'" ( ([^#x27#x5C#xA#xD]) | ECHAR )* "'"
        /// </summary>
        static readonly Func<Production> STRING_LITERAL1 = () => All("'", ZeroOrMore(AnyOf(NotIn('\x27', '\x5C', '\xA', '\xD'), ECHAR)), "'");

        /// <summary>
        /// [157]  	STRING_LITERAL2	  ::=  	'"' ( ([^#x22#x5C#xA#xD]) | ECHAR )* '"'
        /// </summary>
        static readonly Func<Production> STRING_LITERAL2 = () => All('"', ZeroOrMore(All(AnyOf(NotIn('\x22', '\x5C', '\xA', '\xD'), ECHAR))), '"');

        /// <summary>
        /// [158]  	STRING_LITERAL_LONG1	  ::=  	"'''" ( ( "'" | "''" )? ( [^'\] | ECHAR ) )* "'''"
        /// </summary>
        static readonly Func<Production> STRING_LITERAL_LONG1 = () => All("'''", ZeroOrMore(All(Optional(AnyOf("'", "''")), AnyOf(NotIn('\'', '\\'), ECHAR))), "'''");

        /// <summary>
        /// [159]  	STRING_LITERAL_LONG2	  ::=  	'"""' ( ( '"' | '""' )? ( [^"\] | ECHAR ) )* '"""'
        /// </summary>
        static readonly Func<Production> STRING_LITERAL_LONG2 = () => All("\"\"\"", ZeroOrMore(All(Optional(AnyOf('"', "\"\"")), AnyOf(NotIn('"', '\\'), ECHAR))), "\"\"\"");

        /// <summary>
        /// [160]  	ECHAR	  ::=  	'\' [tbnrf\"']
        /// </summary>
        static readonly Func<Production> ECHAR = () => All('\\', In('t', 'b', 'n', 'r', 'f', '\\', '"', '\''));

        /// <summary>
        /// [161]  	NIL	  ::=  	'(' WS* ')'
        /// </summary>
        static readonly Func<Production> NIL = () => _(nameof(NIL), All('(', ZeroOrMore(WS), ')', @Nil));

        //[162]  	WS	  ::=  	#x20 | #x9 | #xD | #xA
        // Inherited from Parser<>

        /// <summary>
        /// [163]  	ANON	  ::=  	'[' WS* ']'
        /// </summary>
        static readonly Func<Production> ANON = () => _(nameof(ANON), All('[', WHITESPACE, ']'));

        /// <summary>
        /// [164]  	PN_CHARS_BASE	  ::=  	[A-Z] | [a-z] | [#x00C0-#x00D6] | [#x00D8-#x00F6] | [#x00F8-#x02FF] | [#x0370-#x037D] | [#x037F-#x1FFF] | [#x200C-#x200D] | [#x2070-#x218F] | [#x2C00-#x2FEF] | [#x3001-#xD7FF] | [#xF900-#xFDCF] | [#xFDF0-#xFFFD] | [#x10000-#xEFFFF]
        /// </summary>
        static readonly Func<Production> PN_CHARS_BASE = () => AnyOf(
            InRange(
                'A', 'Z',
                'a', 'z',
                '\x00C0', '\x00D6',
                '\x00D8', '\x00F6',
                '\x00F8', '\x02FF',
                '\x037F', '\x1FFF',
                '\x200C', '\x200D',
                '\x2070', '\x218F',
                '\x2C00', '\x2FEF',
                '\x3001', '\xD7FF',
                '\xF900', '\xFDCF',
                '\xFDF0', '\xFFFD'
            )
            ,
            InRangeU(
                "\U00010000", "\U000EFFFF"
            )
        );

        /// <summary>
        /// [165]  	PN_CHARS_U	  ::=  	PN_CHARS_BASE | '_'
        /// </summary>
        static readonly Func<Production> PN_CHARS_U = () => AnyOf(PN_CHARS_BASE, '_');

        /// <summary>
        /// [166]  	VARNAME	  ::=  	( PN_CHARS_U | [0-9] ) ( PN_CHARS_U | [0-9] | #x00B7 | [#x0300-#x036F] | [#x203F-#x2040] )*
        /// </summary>
        static readonly Func<Production> VARNAME = () => All(AnyOf(PN_CHARS_U, DIGIT), ZeroOrMore(AnyOf(PN_CHARS_U, DIGIT, '\x00B7', InRange('\x0300', '\x036F', '\x203F', '\x2040'))));

        /// <summary>
        /// [167]  	PN_CHARS	  ::=  	PN_CHARS_U | '-' | [0-9] | #x00B7 | [#x0300-#x036F] | [#x203F-#x2040]
        /// </summary>
        static readonly Func<Production> PN_CHARS = () => AnyOf(PN_CHARS_U, '-', DIGIT, '\x00B7', InRange('\x0300', '\x036F', '\x203F', '\x2040'));

        /// <summary>
        /// [168]  	PN_PREFIX	  ::=  	PN_CHARS_BASE ((PN_CHARS|'.')* PN_CHARS)?
        /// </summary>
        static readonly Func<Production> PN_PREFIX = () => All(PN_CHARS_BASE, Optional(ZeroOrMore(AnyOf(PN_CHARS, '.')), PN_CHARS));

        /// <summary>
        /// [169]  	PN_LOCAL	  ::=  	(PN_CHARS_U | ':' | [0-9] | PLX ) ((PN_CHARS | '.' | ':' | PLX)* (PN_CHARS | ':' | PLX) )?
        /// </summary>
        static readonly Func<Production> PN_LOCAL = () =>
            All(AnyOf(PN_CHARS, ':', DIGIT, PLX), Optional(ZeroOrMore(AnyOf(PN_CHARS, '.', ':', PLX)), AnyOf(PN_CHARS, ':', PLX)));

        /// <summary>
        /// [170]  	PLX	  ::=  	PERCENT | PN_LOCAL_ESC
        /// </summary>
        static readonly Func<Production> PLX = () => AnyOf(PERCENT, PN_LOCAL_ESC);

        /// <summary>
        /// [171]  	PERCENT	  ::=  	'%' HEX HEX
        /// </summary>
        static readonly Func<Production> PERCENT = () => All('%', HEX, HEX);

        /// <summary>
        ///[172]  	HEX	  ::=  	[0-9] | [A-F] | [a-f]
        /// </summary>  
        static readonly Func<Production> HEX = () => AnyOf(InRange('0', '9', 'A', 'F', 'a', 'f'));

        /// <summary>
        /// [173]  	PN_LOCAL_ESC	  ::=  	'\' ( '_' | '~' | '.' | '-' | '!' | '$' | '&' | "'" | '(' | ')' | '*' | '+' | ',' | ';' | '=' | '/' | '?' | '#' | '@' | '%' )
        /// </summary>
        static readonly Func<Production> PN_LOCAL_ESC = () => All('\\', In('_', '~', '.', '-', '!', '$', '&', '\'', '(', ')', '*', '+', ',', ';', '=', '/', '?', '#', '@', '%'));

        #endregion

        #endregion

        #region SPARQL Rules

        static readonly Func<Production> @Base = () => Call((producer, names) => producer.Base = names["base.node.iri"]);
        static readonly Func<Production> @PrefixAndNamespace = () => Call((producer, names) => producer.PrefixAndNamespace(names["prefix.name"], names["namespace.node.iri"]));

        static readonly Func<Production> @SelectVar = () => Call((producer, names) => producer.SelectVar = names["selectvar.node"]);
        static readonly Func<Production> @SelectAll = () => Call((producer, names) => producer.SelectAll());

        static readonly Func<Production> @GroupByVar = () => Call((producer, names) => producer.GroupByVar = names["groupby.node"]);
        static readonly Func<Production> @GroupByBindVar = () => Call((producer, names) => producer.GroupByBindVar = names["groupby.node"]);
        static readonly Func<Production> @GroupByBind = () => Call((producer, names) => producer.GroupByBind());

        static readonly Func<Production> @AggSum = () => Call((producer, names) => producer.AggSum());
        static readonly Func<Production> @AggMin = () => Call((producer, names) => producer.AggMin());
        static readonly Func<Production> @AggMax = () => Call((producer, names) => producer.AggMax());
        static readonly Func<Production> @AggAvg = () => Call((producer, names) => producer.AggAvg());
        static readonly Func<Production> @AggCountStar = () => Call((producer, names) => producer.AggCountStar());
        static readonly Func<Production> @AggCountExpr = () => Call((producer, names) => producer.AggCountExpr());
        static readonly Func<Production> @AggSample = () => Call((producer, names) => producer.AggSample());
        static readonly Func<Production> @AggGroupConcat = () => Call((producer, names) => producer.AggGroupConcat());
        static readonly Func<Production> @AggDistinct = () => Call((producer, names) => producer.AggDistinct());
        static readonly Func<Production> @AggNotDistinct = () => Call((producer, names) => producer.AggNotDistinct());
        static readonly Func<Production> @GroupConcatSeparator = () => Call((producer, names) => producer.GroupConcatSeparator = names["separator"]);
        static readonly Func<Production> @Having = () => Call((producer, names) => producer.Having());

        static readonly Func<Production> @GroupOperation = () => Call((producer, names) => producer.CurrentOperation = names["operation"]);
        static readonly Func<Production> @GroupDistinct = () => Call((producer, names) => producer.CurrentDistinct = true);
        static readonly Func<Production> @BeginGroupGraphPattern = () => Call((producer, names) => producer.BeginGroupGraphPattern());
        static readonly Func<Production> @EndGroupGraphPattern = () => Call((producer, names) => producer.EndGroupGraphPattern());

        static readonly Func<Production> @Subject = () => Call((producer, names) => producer.Subject = names["subject.node"]);
        static readonly Func<Production> @Predicate = () => Call((producer, names) => producer.Predicate = names["predicate.node"]);
        static readonly Func<Production> @Object = () => Call((producer, names) => producer.Object = names["object.node"]);

        static readonly Func<Production> @AllocAnonSubject = () => Call((producer, names) => producer.AllocAnonSubject());
        static readonly Func<Production> @AllocAnonObject = () => Call((producer, names) => producer.AllocAnonObject());

        static readonly Func<Production> @BeginPropertyList = () => Call((producer, names) => producer.BeginPropertyList());
        static readonly Func<Production> @EndPropertyList = () => Call((producer, names) => producer.EndPropertyList());

        static readonly Func<Production> @BeginCollection = () => Call((producer, names) => producer.BeginCollection());
        static readonly Func<Production> @EndCollection = () => Call((producer, names) => producer.EndCollection());

        static readonly Func<Production> @SelectAsVar = () => Call((producer, names) => producer.SelectAsVar = names["selectvar.node"]);
        static readonly Func<Production> @BindAsVar = () => Call((producer, names) => producer.BindAsVar = names["bindvar.node"]);
        static readonly Func<Production> @SelectBind = () => Call((producer, names) => producer.SelectBind());
        static readonly Func<Production> @Bind = () => Call((producer, names) => producer.Bind());

        static readonly Func<Production> @DataBlockVar = () => Call((producer, names) => producer.ValuesVar = names["valuevar.node"]);
        static readonly Func<Production> @BeginDataBlock = () => Call((producer, names) => producer.BeginDataBlock());
        static readonly Func<Production> @DataBlockValue = () => Call((producer, names) => producer.DataBlockValue = names["data.node"]);

        static readonly Func<Production> @OrderByVar = () => Call((producer, names) => producer.OrderByVar = names["orderbyvar.node"]);
        static readonly Func<Production> @OrderByBind = () => Call((producer, names) => producer.OrderByBind());
        static readonly Func<Production> @Descending = () => Call((producer, names) => producer.OrderByDesc());
        static readonly Func<Production> @Ascending = () => Call((producer, names) => producer.OrderByAsc());

        static readonly Func<Production> @Boolean = () => Call((producer, names) => producer.TermIsBoolean());
        static readonly Func<Production> @Integer = () => Call((producer, names) => producer.TermIsInteger());
        static readonly Func<Production> @Double = () => Call((producer, names) => producer.TermIsDouble());
        static readonly Func<Production> @Decimal = () => Call((producer, names) => producer.TermIsDecimal());
        static readonly Func<Production> @Variable = () => Call((producer, names) => producer.TermIsVar());
        static readonly Func<Production> @Iri = () => Call((producer, names) => producer.TermIsIri());
        static readonly Func<Production> @Blank = () => Call((producer, names) => producer.TermIsBlank());
        static readonly Func<Production> @Anon = () => Call((producer, names) => producer.TermIsAnon());
        static readonly Func<Production> @Nil = () => Call((producer, names) => producer.TermIsNil());
        static readonly Func<Production> @String = () => Call((producer, names) => producer.TermIsString());
        static readonly Func<Production> @A = () => Call((producer, names) => producer.TermIsA());

        static readonly Func<Production> @Filter = () => Call((producer, names) => producer.Filter());
        static readonly Func<Production> @Symbol = () => Call((producer, names) => producer.SymbolExpression(names["node"]));

        static readonly Func<Production> @Limit = () => Call((producer, names) => producer.Limit = names["limit"]);
        static readonly Func<Production> @Offset = () => Call((producer, names) => producer.Offset = names["offset"]);

        #region operators

        static readonly Func<Production> @OperatorOr = () => Call((producer, names) => producer.BinaryOperatorExpressionOr());
        static readonly Func<Production> @OperatorAnd = () => Call((producer, names) => producer.BinaryOperatorExpressionAnd());
        static readonly Func<Production> @OperatorLessThan = () => Call((producer, names) => producer.BinaryOperatorExpressionLessThan());
        static readonly Func<Production> @OperatorLessOrEqualThan = () => Call((producer, names) => producer.BinaryOperatorExpressionLessOrEqualThan());
        static readonly Func<Production> @OperatorGreaterThan = () => Call((producer, names) => producer.BinaryOperatorExpressionGreaterThan());
        static readonly Func<Production> @OperatorGreaterOrEqualThan = () => Call((producer, names) => producer.BinaryOperatorExpressionGreaterOrEqualThan());
        static readonly Func<Production> @OperatorEquals = () => Call((producer, names) => producer.BinaryOperatorExpressionEquals());
        static readonly Func<Production> @OperatorNotEquals = () => Call((producer, names) => producer.BinaryOperatorExpressionNotEquals());
        static readonly Func<Production> @OperatorAdd = () => Call((producer, names) => producer.BinaryOperatorExpressionAdd());
        static readonly Func<Production> @OperatorSubtract = () => Call((producer, names) => producer.BinaryOperatorExpressionSubtract());
        static readonly Func<Production> @OperatorMultiply = () => Call((producer, names) => producer.BinaryOperatorExpressionMultiply());
        static readonly Func<Production> @OperatorDivide = () => Call((producer, names) => producer.BinaryOperatorExpressionDivide());

        static readonly Func<Production> @OperatorNot = () => Call((producer, names) => producer.UnaryOperatorExpressionNot());
        static readonly Func<Production> @OperatorNegate = () => Call((producer, names) => producer.UnaryOperatorExpressionNegate());
        static readonly Func<Production> @OperatorPlus = () => Call((producer, names) => producer.UnaryOperatorExpressionPlus());

        static readonly Func<Production> @OperatorIn = () => Call((producer, names) => producer.OperatorExpressionIn());
        static readonly Func<Production> @OperatorNotIn = () => Call((producer, names) => producer.OperatorExpressionNotIn());

        #endregion

        #region Builtins

        static readonly Func<Production> @STR = () => Call((producer, names) => producer.BuiltInSTR());
        static readonly Func<Production> @LANG = () => Call((producer, names) => producer.BuiltInLANG());
        static readonly Func<Production> @LANGMATCHES = () => Call((producer, names) => producer.BuiltInLANGMATCHES());
        static readonly Func<Production> @DATATYPE = () => Call((producer, names) => producer.BuiltInDATATYPE());
        static readonly Func<Production> @BOUND = () => Call((producer, names) => producer.BuiltInBOUND());
        static readonly Func<Production> @IRI = () => Call((producer, names) => producer.BuiltInIRI());
        static readonly Func<Production> @BNODE1 = () => Call((producer, names) => producer.BuiltInBNODE1());
        static readonly Func<Production> @BNODE0 = () => Call((producer, names) => producer.BuiltInBNODE0());
        static readonly Func<Production> @RAND = () => Call((producer, names) => producer.BuiltInRAND());
        static readonly Func<Production> @ABS = () => Call((producer, names) => producer.BuiltInABS());
        static readonly Func<Production> @CEIL = () => Call((producer, names) => producer.BuiltInCEIL());
        static readonly Func<Production> @FLOOR = () => Call((producer, names) => producer.BuiltInFLOOR());
        static readonly Func<Production> @ROUND = () => Call((producer, names) => producer.BuiltInROUND());
        static readonly Func<Production> @CONCAT = () => Call((producer, names) => producer.BuiltInCONCAT());
        static readonly Func<Production> @STRLEN = () => Call((producer, names) => producer.BuiltInSTRLEN());
        static readonly Func<Production> @UCASE = () => Call((producer, names) => producer.BuiltInUCASE());
        static readonly Func<Production> @LCASE = () => Call((producer, names) => producer.BuiltInLCASE());
        static readonly Func<Production> @SUBSTR2 = () => Call((producer, names) => producer.BuiltInSUBSTR2());
        static readonly Func<Production> @SUBSTR3 = () => Call((producer, names) => producer.BuiltInSUBSTR3());
        static readonly Func<Production> @REPLACE3 = () => Call((producer, names) => producer.BuiltInREPLACE3());
        static readonly Func<Production> @REPLACE4 = () => Call((producer, names) => producer.BuiltInREPLACE4());
        static readonly Func<Production> @ENCODE_FOR_URI = () => Call((producer, names) => producer.BuiltInENCODE_FOR_URI());
        static readonly Func<Production> @CONTAINS = () => Call((producer, names) => producer.BuiltInCONTAINS());
        static readonly Func<Production> @STRSTARTS = () => Call((producer, names) => producer.BuiltInSTRSTARTS());
        static readonly Func<Production> @STRENDS = () => Call((producer, names) => producer.BuiltInSTRENDS());
        static readonly Func<Production> @STRBEFORE = () => Call((producer, names) => producer.BuiltInSTRBEFORE());
        static readonly Func<Production> @STRAFTER = () => Call((producer, names) => producer.BuiltInSTRAFTER());
        static readonly Func<Production> @YEAR = () => Call((producer, names) => producer.BuiltInYEAR());
        static readonly Func<Production> @MONTH = () => Call((producer, names) => producer.BuiltInMONTH());
        static readonly Func<Production> @DAY = () => Call((producer, names) => producer.BuiltInDAY());
        static readonly Func<Production> @HOURS = () => Call((producer, names) => producer.BuiltInHOURS());
        static readonly Func<Production> @MINUTES = () => Call((producer, names) => producer.BuiltInMINUTES());
        static readonly Func<Production> @SECONDS = () => Call((producer, names) => producer.BuiltInSECONDS());
        static readonly Func<Production> @TIMEZONE = () => Call((producer, names) => producer.BuiltInTIMEZONE());
        static readonly Func<Production> @TZ = () => Call((producer, names) => producer.BuiltInTZ());
        static readonly Func<Production> @NOW = () => Call((producer, names) => producer.BuiltInNOW());
        static readonly Func<Production> @UUID = () => Call((producer, names) => producer.BuiltInUUID());
        static readonly Func<Production> @STRUUID = () => Call((producer, names) => producer.BuiltInSTRUUID());
        static readonly Func<Production> @MD5 = () => Call((producer, names) => producer.BuiltInMD5());
        static readonly Func<Production> @SHA1 = () => Call((producer, names) => producer.BuiltInSHA1());
        static readonly Func<Production> @SHA256 = () => Call((producer, names) => producer.BuiltInSHA256());
        static readonly Func<Production> @SHA384 = () => Call((producer, names) => producer.BuiltInSHA384());
        static readonly Func<Production> @SHA512 = () => Call((producer, names) => producer.BuiltInSHA512());
        static readonly Func<Production> @COALESCE = () => Call((producer, names) => producer.BuiltInCOALESCE());
        static readonly Func<Production> @IF = () => Call((producer, names) => producer.BuiltInIF());
        static readonly Func<Production> @STRLANG = () => Call((producer, names) => producer.BuiltInSTRLANG());
        static readonly Func<Production> @STRDT = () => Call((producer, names) => producer.BuiltInSTRDT());
        static readonly Func<Production> @SAMETERM = () => Call((producer, names) => producer.BuiltInSAMETERM());
        static readonly Func<Production> @IS_IRI = () => Call((producer, names) => producer.BuiltInIS_IRI());
        static readonly Func<Production> @IS_BLANK = () => Call((producer, names) => producer.BuiltInIS_BLANK());
        static readonly Func<Production> @IS_LITERAL = () => Call((producer, names) => producer.BuiltInIS_LITERAL());
        static readonly Func<Production> @IS_NUMERIC = () => Call((producer, names) => producer.BuiltInIS_NUMERIC());

        #endregion

        static readonly Func<Production> @ArgInit = () => Call((producer, names) => producer.CountArgInit());
        static readonly Func<Production> @Arg = () => Call((producer, names) => producer.CountArg());

    }
    #endregion

    #region Private construction

    private static readonly Sparql Instance = new Sparql();

    /// <summary>
    /// Creates a sparql parser.
    /// </summary>
    /// <param name="text">SPARQL text.</param>
    private Sparql()
        : base(Productions.QueryUnit)
    {
    }

    #endregion
}
