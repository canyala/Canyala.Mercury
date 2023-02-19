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

using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;
using Canyala.Lagoon.Core.Text;

using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Core.Text;

namespace Canyala.Mercury.Rdf;

/// <summary>
/// Provides an API for terse turtle documents.
/// </summary>
/// <remarks>
/// Terse RDF Triple Language
/// W3C Candidate Recommendation 19 February 2013
/// </remarks>
/// <seealso cref="https://www.w3.org/TR/turtle/"/>
public partial class Turtle : Parser<Turtle.Producer>
{
    #region Public API

    /// <summary>
    /// Convert triples to a turtle document.
    /// </summary>
    /// <param name="triples">Triples as a sequence of string arrays.</param>
    /// <param name="namespaces">A namespaces object declaring base and prefixes to use.</param>
    /// <returns>A turtle document as a text string.</returns>
    public static string AsText(IEnumerable<string[]> triples, Namespaces? namespaces = null)
    {
        var text = new StringBuilder();

        foreach (var line in AsLines(triples, namespaces))
            text.AppendLine(line);

        return text.ToString();
    }

    public static string PrefixesAsText(Namespaces? namespaces = null)
    {
        var text = new StringBuilder();

        foreach (var line in PrefixesAsLines(namespaces))
            text.AppendLine(line);

        return text.ToString();
    }

    public static string TurtlesAsText(IEnumerable<string[]> triples, Namespaces? namespaces = null)
    {
        var text = new StringBuilder();

        foreach (var line in TurtlesAsLines(triples, namespaces))
            text.AppendLine(line);

        return text.ToString();
    }

    /// <summary>
    /// Convert triples to a turtle document.
    /// </summary>
    /// <param name="triples">Triples as of a sequence of string arrays.</param>
    /// <param name="namespaces">A namespaces object declaring base and prefixes to use.</param>
    /// <returns>A turtle document as a sequence of text line strings.</returns>
    public static IEnumerable<string> AsLines(IEnumerable<string[]> triples, Namespaces? namespaces = null)
    {
        foreach (var line in PrefixesAsLines(namespaces))
            yield return line;

        foreach (var line in TurtlesAsLines(triples, namespaces))
            yield return line;
    }

    public static IEnumerable<string> PrefixesAsLines(Namespaces? namespaces = null)
    {
        if (namespaces != null)
            foreach (var binding in namespaces)
                yield return "@prefix {0}: <{1}> .".Args(binding.Prefix, binding.Namespace);
    }

    public static IEnumerable<string> TurtlesAsLines(IEnumerable<string[]> triples, Namespaces? namespaces = null)
    {
        triples = triples.AsTurtles();
        namespaces = namespaces ?? new Namespaces();
        Resource[]? previous = null;

        foreach (var resources in triples.AsResources(namespaces))
        {
            if (previous != null)
                yield return "{0} {1}".Args(previous.Select(resource => resource.Short).Join(' '), TurtleTerminators[resources.Length - 1]);

            previous = resources;
        }

        if (previous != null)
            yield return "{0} .".Args(previous.Select(resource => resource.Short).Join(' '));
    }

    /// <summary>
    /// Convert a turtle document to triples.
    /// </summary>
    /// <param name="text">A turtle document as a text string.</param>
    /// <returns>Triples as a sequence of string arrays.</returns>
    public static IEnumerable<string[]> FromText(string text)
        { return new Producer(Instance, Analyzer.Lines(text)); }

    /// <summary>
    /// Convert a turtle document to triples.
    /// </summary>
    /// <param name="lines">A turtle document as a sequence of text line strings.</param>
    /// <returns>Triples as a sequence of string arrays.</returns>
    public static IEnumerable<string[]> FromLines(IEnumerable<string> lines)
        { return new Producer(Instance, lines); }

    #endregion

    #region Grammar Production Declarations

    /// <summary>
    /// The inner Productions class declare terse turtle production rules
    /// based on the terse turtle bnf grammar.
    /// </summary>
    /// <seealso cref="https://www.w3.org/TR/turtle/#sec-grammar"/>
    private static class Productions
    {
        /// <summary>
        /// ROOT! turtleDoc ::= statement* 
        /// </summary>
        public static readonly Func<Production> turtleDoc = () => _(nameof(turtleDoc), All(ZeroOrMore(statement)));

        /// <summary>
        /// statement ::= directive |  triples '.'     
        /// </summary>
        static readonly Func<Production> statement = () => AnyOf(directive, All(triples, '.'));

        /// <summary>
        /// directive ::= prefixID |  base |  sparqlPrefix |  sparqlBase 
        /// </summary>
        static readonly Func<Production> directive = () => AnyOf(prefixID, _base, sparqlPrefix, sparqlBase);

        /// <summary>
        /// prefixID ::= '@prefix' PNAME_NS IRIREF '.' 
        /// </summary>
        static readonly Func<Production> prefixID = () => All("@prefix", Named("prefix", PNAME_NS), Named("namespace", IRIREF), '.', @PrefixAndNamespace);

        /// <summary>
        /// base ::= '@base' IRIREF '.' 
        /// </summary>
        static readonly Func<Production> _base = () => All("@base", Named("base", IRIREF), '.', @Base);

        /// <summary>
        /// sparqlBase ::= "BASE" IRIREF 
        /// </summary>
        static readonly Func<Production> sparqlBase = () => All(i("BASE"), All(Named("base", IRIREF), @Base));

        /// <summary>
        /// sparqlPrefix ::= "PREFIX" PNAME_NS IRIREF 
        /// </summary>
        static readonly Func<Production> sparqlPrefix = () => All(i("PREFIX"), Named("prefix", PNAME_NS), Named("namespace", IRIREF), @PrefixAndNamespace);

        /// <summary>
        /// triples ::= subject predicateObjectList |  blankNodePropertyList predicateObjectList? 
        /// </summary>
        static readonly Func<Production> triples = () => _(nameof(triples), AnyOf(All(subject, predicateObjectList), All(@AllocBlankSubject, blankNodePropertyList, Optional(predicateObjectList))));

        /// <summary>
        /// predicateObjectList ::= verb objectList (';' (verb objectList)?)* 
        /// </summary>
        static readonly Func<Production> predicateObjectList = () => _(nameof(predicateObjectList), All(verb, objectList, ZeroOrMore(';', Optional(verb, objectList))));

        /// <summary>
        /// objectList ::= object (',' object)* 
        /// </summary>
        static readonly Func<Production> objectList = () => _(nameof(objectList), All(_object, ZeroOrMore(',', _object)));

        /// <summary>
        /// verb ::= predicate |  'a' 
        /// </summary>
        static readonly Func<Production> verb = () => _(nameof(verb), All(Named("predicate", AnyOf(predicate, All(@A, Named("node", 'a')))), @Predicate));

        /// <summary>
        /// subject ::= iri |  BlankNode |  collection 
        /// </summary>
        static readonly Func<Production> subject = () => _(nameof(subject), All(AnyOf(Named("subject", All(@Iri, iri)), Named("subject", BlankNode), All(@AllocBlankSubject, collection)), @Subject));

        /// <summary>
        /// predicate ::= iri 
        /// </summary>
        static readonly Func<Production> predicate = () => _(nameof(predicate), All(@Iri, iri));

        /// <summary>
        /// object ::= collection |  blankNodePropertyList | iri | BlankNode | literal
        /// </summary>
        static readonly Func<Production> _object = () => _("object", AnyOf(AnyOf(All(@AllocBlankObject, collection), All(@AllocBlankObject, blankNodePropertyList)), All(Named("object", AnyOf(All(@Iri, iri), BlankNode, literal)), @Object)));

        /// <summary>
        /// literal ::= RDFLiteral |  NumericLiteral |  BooleanLiteral 
        /// </summary>
        static readonly Func<Production> literal = () => _(nameof(literal), Named("node", AnyOf(RDFLiteral, NumericLiteral, BooleanLiteral)));

        /// <summary>
        /// blankNodePropertyList ::= '[' predicateObjectList ']' 
        /// </summary>
        static readonly Func<Production> blankNodePropertyList = () => _(nameof(blankNodePropertyList), All(@BeginPropertyList, '[', predicateObjectList, ']', @EndPropertyList));

        /// <summary>
        /// collection ::= '(' object* ')' 
        /// </summary>
        static readonly Func<Production> collection = () => _(nameof(collection), All(@BeginCollection, '(', ZeroOrMore(_object), ')', @EndCollection));

        /* Production token declarations */

        /// <summary>
        /// NumericLiteral ::= INTEGER |  DECIMAL |  DOUBLE 
        /// </summary>
        static readonly Func<Production> NumericLiteral = () => _(nameof(NumericLiteral), Token(AnyOf(All(@Double, DOUBLE), All(@Decimal, DECIMAL), All(@Integer, INTEGER))));

        /// <summary>
        /// RDFLiteral ::= String (LANGTAG |  '^^' iri)? 
        /// </summary>
        static readonly Func<Production> RDFLiteral = () => _(nameof(RDFLiteral), Token(_String, Optional(AnyOf(LANGTAG, All("^^", iri)))));

        /// <summary>
        /// BooleanLiteral ::= 'true' |  'false' 
        /// </summary>
        static readonly Func<Production> BooleanLiteral = () => _(nameof(BooleanLiteral), Token(@Boolean, AnyOf("true", "false")));

        /// <summary>
        /// String ::= STRING_LITERAL_QUOTE |  STRING_LITERAL_SINGLE_QUOTE |  STRING_LITERAL_LONG_SINGLE_QUOTE |  STRING_LITERAL_LONG_QUOTE 
        /// </summary>
        static readonly Func<Production> _String = () => _(nameof(String), Token(@String, AnyOf(STRING_LITERAL_QUOTE, STRING_LITERAL_SINGLE_QUOTE, STRING_LITERAL_LONG_SINGLE_QUOTE, STRING_LITERAL_LONG_QUOTE)));

        /// <summary>
        /// PrefixedName ::= PNAME_LN |  PNAME_NS 
        /// </summary>
        static readonly Func<Production> PrefixedName = () => _(nameof(PrefixedName), Token(AnyOf(PNAME_LN, PNAME_NS)));

        /// <summary>
        /// BlankNode ::= BLANK_NODE_LABEL |  ANON 
        /// </summary>
        static readonly Func<Production> BlankNode = () => _(nameof(BlankNode), Named("node", Token(@Blank, AnyOf(BLANK_NODE_LABEL, ANON))));

        /* Production terminal declarations */

        /// <summary>
        /// iri ::= IRIREF |  PrefixedName 
        /// </summary>
        static readonly Func<Production> iri = () => AnyOf(IRIREF, PrefixedName);

        /// <summary>
        /// IRIREF ::= '<' ([^#x00-#x20<>\"{}|^`\] |  UCHAR)* '>' 
        /// </summary>
        static readonly Func<Production> IRIREF = () => Named("node", All('<', Named("iri", ZeroOrMore(AnyOf(NotIn(Seq.Of('^', '<', '>', '"', '{', '}', '|', '\\', '´').Concat('\x00'.UpTo('\x20'))), UCHAR))), '>'));

        /// <summary>
        /// PNAME_NS ::= PN_PREFIX? ':' 
        /// </summary>
        static readonly Func<Production> PNAME_NS = () => All(Named("name", Optional(PN_PREFIX)), ':');

        /// <summary>
        /// PNAME_LN ::= PNAME_NS PN_LOCAL 
        /// </summary>
        static readonly Func<Production> PNAME_LN = () => Named("node", All(PNAME_NS, PN_LOCAL));

        /// <summary>
        /// BLANK_NODE_LABEL ::= '_:' (PN_CHARS_U |  [0-9]) ((PN_CHARS |  '.')* PN_CHARS)? 
        /// </summary>
        static readonly Func<Production> BLANK_NODE_LABEL = () => All("_:", AnyOf(PN_CHARS_U, InRange('0', '9')), Optional(ZeroOrMore(AnyOf(PN_CHARS, '.'), PN_CHARS)));

        /// <summary>
        /// LANGTAG ::= '@' [a-zA-Z]+ ('-' [a-zA-Z0-9]+)* 
        /// </summary>
        static readonly Func<Production> LANGTAG = () => _(nameof(LANGTAG), All('@', OneOrMore(LETTER), ZeroOrMore('-', OneOrMore(DIGIT_OR_LETTER))));

        /// <summary>
        /// STRING_LITERAL_QUOTE ::= '"' ([^#x22#x5C#xA#xD] |  ECHAR |  UCHAR)* '"' 
        /// </summary>
        static readonly Func<Production> STRING_LITERAL_QUOTE = () => All('"', ZeroOrMore(AnyOf(NotIn('\x22', '\x5C', '\xA', '\xD'), ECHAR, UCHAR)), '"');

        /// <summary>
        /// STRING_LITERAL_SINGLE_QUOTE ::= "'" ([^#x27#x5C#xA#xD] |  ECHAR |  UCHAR)* "'" 
        /// </summary>
        static readonly Func<Production> STRING_LITERAL_SINGLE_QUOTE = () => All("'", ZeroOrMore(AnyOf(NotIn('\x27', '\x5C', '\xA', '\xD'), ECHAR, UCHAR)), "'");

        /// <summary>
        /// STRING_LITERAL_LONG_SINGLE_QUOTE ::= "'''" (("'" |  "''")? [^'\] |  ECHAR |  UCHAR)* "'''" 
        /// </summary>
        static readonly Func<Production> STRING_LITERAL_LONG_SINGLE_QUOTE = () => All("'''", ZeroOrMore(AnyOf(All(Optional(AnyOf("'", "''")), NotIn('\'', '\\')), ECHAR, UCHAR)), "'''");

        /// <summary>
        /// STRING_LITERAL_LONG_QUOTE ::= '"""' (('"' |  '""')? [^"\] |  ECHAR |  UCHAR)* '"""' 
        /// </summary>
        static readonly Func<Production> STRING_LITERAL_LONG_QUOTE = () => All("\"\"\"", ZeroOrMore(AnyOf(All(Optional(AnyOf("\"", "\"\"")), NotIn('"', '\\')), ECHAR, UCHAR)), "\"\"\"");

        /// <summary>
        /// UCHAR ::= '\u' HEX HEX HEX HEX |  '\U' HEX HEX HEX HEX HEX HEX HEX HEX 
        /// </summary>
        static readonly Func<Production> UCHAR = () => AnyOf(All("\\u", HEX, HEX, HEX, HEX), All("\\U", HEX, HEX, HEX, HEX, HEX, HEX, HEX, HEX));

        /// <summary>
        /// ECHAR ::= '\' [tbnrf\"'] 
        /// </summary>
        static readonly Func<Production> ECHAR = () => All('\\', In('t', 'b', 'n', 'r', 'f', '\\', '\"', '\''));

        /// <summary>
        /// ANON ::= '[' WS* ']' 
        /// </summary>
        static readonly Func<Production> ANON = () => All('[', WHITESPACE, ']');

        /* Production terminal declarations */

        /// <summary>
        /// PN_CHARS_BASE ::= [A-Z] |  [a-z] |  [#x00C0-#x00D6] |  [#x00D8-#x00F6] |  [#x00F8-#x02FF] |  [#x0370-#x037D] |  
        ///                                     [#x037F-#x1FFF] |  [#x200C-#x200D] |  [#x2070-#x218F] |  [#x2C00-#x2FEF] |  
        ///                                     [#x3001-#xD7FF] |  [#xF900-#xFDCF] |  [#xFDF0-#xFFFD] |  [#x10000-#xEFFFF] 
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
        /// PN_CHARS_U ::= PN_CHARS_BASE |  '_' 
        /// </summary>
        static readonly Func<Production> PN_CHARS_U = () => AnyOf(PN_CHARS_BASE, '_');

        /// <summary>
        /// PN_CHARS ::= PN_CHARS_U |  '-' |  [0-9] |  #x00B7 |  [#x0300-#x036F] |  [#x203F-#x2040] 
        /// </summary>
        static readonly Func<Production> PN_CHARS = () => AnyOf(PN_CHARS_U, '-', DIGIT, '\x00B7', InRange('\x0300', '\x036F', '\x203F', '\x2040'));

        /// <summary>
        /// PN_PREFIX ::= PN_CHARS_BASE ((PN_CHARS |  '.')* PN_CHARS)? 
        /// </summary>
        static readonly Func<Production> PN_PREFIX = () => All(PN_CHARS_BASE, Optional(ZeroOrMore(AnyOf(PN_CHARS, '.')), PN_CHARS));

        /// <summary>
        /// PN_LOCAL ::= (PN_CHARS_U |  ':' |  [0-9] |  PLX) ((PN_CHARS |  '.' |  ':' |  PLX)* (PN_CHARS |  ':' |  PLX))? 
        /// </summary>
        static readonly Func<Production> PN_LOCAL = () => All(AnyOf(PN_CHARS_U, ':', DIGIT, PLX), Optional(ZeroOrMore(AnyOf(PN_CHARS, '.', ':', PLX)), AnyOf(PN_CHARS, ':', PLX)));

        /// <summary>
        /// PLX ::= PERCENT |  PN_LOCAL_ESC 
        /// </summary>
        static readonly Func<Production> PLX = () => AnyOf(PERCENT, PN_LOCAL_ESC);

        /// <summary>
        /// PERCENT ::= '%' HEX HEX 
        /// </summary>
        static readonly Func<Production> PERCENT = () => All('%', HEX, HEX);

        /// <summary>
        /// HEX ::= [0-9] |  [A-F] |  [a-f] 
        /// </summary>
        static readonly Func<Production> HEX = () => AnyOf(DIGIT, InRange('A', 'F'), InRange('a', 'f'));

        /// <summary>
        /// PN_LOCAL_ESC ::= '\' ('_' |  '~' |  '.' |  '-' |  '!' |  '$' |  '&' |  "'" |  '(' |  ')' |  '*' |  '+' |  ',' |  ';' |  '=' |  '/' |  '?' |  '#' |  '@' |  '%') 
        /// </summary>
        static readonly Func<Production> PN_LOCAL_ESC = () => All('\\', In('_', '~', '.', '-', '!', '$', '&', '\'', '(', ')', '*', '+', ',', ';', '=', '/', '?', '#', '@', '%'));

        #endregion

        #region Grammar Production Rules

        /// <summary>
        /// Applies blank subject allocation.
        /// </summary>
        static readonly Func<Production> @AllocBlankSubject = () => Call((producer, names) => producer.AllocBlankSubject());

        /// <summary>
        /// Applies blank object allocation.
        /// </summary>
        static readonly Func<Production> @AllocBlankObject = () => Call((producer, names) => producer.AllocBlankObject());

        /// <summary>
        /// Applies a scoped property list beginning.
        /// </summary>
        static readonly Func<Production> @BeginPropertyList = () => Call((producer, names) => producer.BeginPropertyList());

        /// <summary>
        /// Applies a scoped property list ending.
        /// </summary>
        static readonly Func<Production> @EndPropertyList = () => Call((producer, names) => producer.EndPropertyList());

        /// <summary>
        /// Applies a scoped object list beginning.
        /// </summary>
        static readonly Func<Production> @BeginCollection = () => Call((producer, names) => producer.BeginCollection());

        /// <summary>
        /// Applies a scoped object list ending.
        /// </summary>
        static readonly Func<Production> @EndCollection = () => Call((producer, names) => producer.EndCollection());

        /// <summary>
        /// Applies a base declaration.
        /// </summary>
        static readonly Func<Production> @Base = () => Call((producer, names) => producer.Base = names["base.node.iri"]);

        /// <summary>
        /// Applies a prefix declaration.
        /// </summary>
        static readonly Func<Production> @PrefixAndNamespace = () => Call((producer, names) => producer.PrefixAndNamespace(names["prefix.name"], names["namespace.node.iri"]));

        /// <summary>
        /// Applies a subject term.
        /// </summary>
        static readonly Func<Production> @Subject = () => Call((producer, names) => producer.Subject = names["subject.node"]);

        /// <summary>
        /// Applies a predicate term.
        /// </summary>
        static readonly Func<Production> @Predicate = () => Call((producer, names) => producer.Predicate = names["predicate.node"]);

        /// <summary>
        /// Applies an object term.
        /// </summary>
        static readonly Func<Production> @Object = () => Call((producer, names) => producer.Object = names["object.node"]);

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @Boolean = () => Call((producer, names) => producer.TermIsBoolean());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @Integer = () => Call((producer, names) => producer.TermIsInteger());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @Double = () => Call((producer, names) => producer.TermIsDouble());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @Decimal = () => Call((producer, names) => producer.TermIsDecimal());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @Iri = () => Call((producer, names) => producer.TermIsIri());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @Blank = () => Call((producer, names) => producer.TermIsBlank());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @Anon = () => Call((producer, names) => producer.TermIsAnon());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @Nil = () => Call((producer, names) => producer.TermIsNil());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @String = () => Call((producer, names) => producer.TermIsString());

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> @A = () => Call((producer, names) => producer.TermIsA());

    }

    #endregion

    #region Internal Construction

    private static readonly char[] TurtleTerminators = { ',', ';', '.' };

    private static readonly Turtle Instance = new Turtle();

    /// <summary>
    /// Creates the turtle parsing singleton.
    /// </summary>
    private Turtle()
        : base(Productions.turtleDoc)
    { /* No implementation */ }

    #endregion
}
