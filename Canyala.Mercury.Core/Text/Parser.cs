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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using Canyala.Lagoon.Core.Text;
using Canyala.Lagoon.Core.Extensions;
using Canyala.Lagoon.Core.Functional;
using Canyala.Lagoon.Core.Contracts;

namespace Canyala.Mercury.Core.Text;

/// <summary>
/// Provides a BNF style parser with grammar and production API's for a deriving parser implementation.
/// </summary>
/// <typeparam name="T">The type of the producer.</typeparam>
public abstract class Parser<T>
{
    #region Production declaration class types

    protected abstract class Production
    {
        private static readonly string StandardWhites = " \t\n\r";

        internal abstract class Condition : Production
        {
            protected readonly Production[] _productions;

            public Condition(Production[] productions)
            { 
#if DEBUG
                foreach(var declaration in productions)
                    if (declaration == null) throw new ArgumentNullException("Productions contains null elements. Do not use forward or recursive references. Use Func<> to resolve.");
#endif
                _productions = productions; 
            }

            public Production[] Productions
                { get { return _productions; } }

            public abstract void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress);
        }

        public abstract class Terminal : Production
        {
            public abstract bool Match(Context context);
        }

        #region Conditions

        internal class All : Condition
        {
            public All(Production[] productions) : base(productions) {}

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
            {
                Productions.Reverse().Do(prod => resolvent.Push(Invoke(prod)));
            }
        }

        internal sealed class Trace : All
        {
            private string _trace;

            public Trace(string trace, Production production) : base(Seq.Array(production))
                { _trace = trace;  }

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
            {
                context.Trace = _trace;
                base.Resolve(resolvent, choicePoints, context, progress);
            }
        }

        internal sealed class AnyOf : Condition
        {
            public AnyOf(Production[] productions) : base(productions) {}

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
            {
                foreach (var prod in Productions.Skip(1).Reverse())
                {
                    var clonedResolvent = CloneStack(resolvent);
                    clonedResolvent.Push(Invoke(prod));
                    choicePoints.Push(ChoicePoint.Create(clonedResolvent, context.State));
                }
                resolvent.Push(Invoke(Productions[0]));
            }
        }

        internal sealed class Optional : Condition
        {
            public Optional(Production production) : base(new[] { production }) {}

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
            {
                choicePoints.Push(ChoicePoint.Create(CloneStack(resolvent), context.State));
                resolvent.Push(Invoke(Productions[0]));
            }
        }

        internal sealed class OneOrMore : Condition
        {
            public OneOrMore(Production production) : base(new[] { production }) { }

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
            {
                resolvent.Push(Invoke(ZeroOrMore(Productions[0])));
                resolvent.Push(Invoke(Productions[0]));
            }
        }

        internal sealed class ZeroOrMore : Condition
        {
            public ZeroOrMore(Production production) : base(new[] { production }) { }

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
            {
                int index = context.Text.Length;

                if (progress >= 0 && index >= progress)
                    return;

                choicePoints.Push(ChoicePoint.Create(CloneStack(resolvent), context.State));
                resolvent.Push(Invoke(this, index));
                resolvent.Push(Invoke(Productions[0]));
            }
        }

        #endregion

        #region Terminals

        internal sealed class Literal : Terminal
        {
            private readonly string _literal;
            private readonly bool _caseSensitive;

            public Literal(string literal, bool caseSensitive)
            { 
                _literal = literal; 
                _caseSensitive = caseSensitive;
            }

            public override bool Match(Context context)
            {
                if (!context.InSequence)
                    context.Text = context.Text.TrimStart();

                if (context.Text.Length == 0)
                    return false;

                if (!context.Text.StartsWith(_literal, _caseSensitive))
                    return false;

                context.Text = context.Text[_literal.Length, 0];

                return true;
            }

            public override string ToString()
                { return "{0} \"{1}\"".Args(GetType().Name, _literal); }
        }

        internal sealed class CharLiteral : Terminal 
        {
            private readonly char _charLiteral;

            public CharLiteral(char charLiteral)
                { _charLiteral = charLiteral; }

            public override bool Match(Context context)
            {
                if (!context.InSequence)
                    context.Text = context.Text.TrimStart();

                if (context.Text.Length == 0)
                    return false;

                if (context.Text[0] != _charLiteral)
                    return false;

                context.Text = context.Text[1, 0];

                return true;
            }

            public override string ToString()
                { return "{0} '{1}'".Args(GetType().Name, _charLiteral); }
        }

        internal sealed class InRange : Terminal
        {
            private readonly IEnumerable<Tuple<char, char>> _ranges;

            public InRange(params char[] rangePairs)
            {
                Contract.Requires(rangePairs.Length % 2 == 0, "Must have an even number of range pairs!");

                _ranges = rangePairs
                    .Split(2)
                    .Select(pair => Tuple.Create(pair.First(), pair.Last()))
                    .ToArray();
            }

            public override bool Match(Context context)
            {
                if (!context.InSequence)
                    context.Text = context.Text.TrimStart();

                if (context.Text.Length == 0)
                    return false;

                foreach (var range in _ranges)
                {
                    if (context.Text[0] >= range.Item1 && context.Text[0] <= range.Item2)
                    {
                        context.Text = context.Text[1, 0];
                        return true;
                    }
                }

                return false;
            }

            public override string ToString()
            {
                return "{0} {{{1}}}".Args(GetType().Name, _ranges.Select(pair => "{0} - {1}".Args(pair.Item1, pair.Item2)).Join(','));
            }
        }

        internal sealed class InRangeU : Terminal
        {
            private string _lo;
            private string _hi;

            public InRangeU(string lo, string hi)
            {
                _lo = lo;
                _hi = hi;
            }

            public override bool Match(Context context)
            {
                context.Text = context.Text.TrimStart();

                if (context.Text.Length == 0)
                    return false;

                if (char.IsHighSurrogate(context.Text[0]) && char.IsLowSurrogate(context.Text[1]))
                {
                    var utf32 = char.ConvertToUtf32(context.Text[0], context.Text[1]);
                    var s = char.ConvertFromUtf32(utf32);

                    if (string.Compare(s, _lo, StringComparison.InvariantCulture) >= 0 && string.Compare(s, _hi, StringComparison.InvariantCulture) <= 0)
                    {
                        context.Text = context.Text[2, 0];
                        return true;
                    }
                }

                return false;
            }

            public override string ToString()
            {
                return "{0} {{{1}}}".Args(GetType().Name, "{0} - {1}".Args(_lo, _hi));
            }
        }

        internal sealed class NotInRange : Terminal
        {
            private readonly char _charLiteralLo;
            private readonly char _charLiteralHi;
            private readonly bool _includesWhite;

            public NotInRange(char lo, char hi)
            {
                _charLiteralLo = lo;
                _charLiteralHi = hi;

                _includesWhite = StandardWhites.Any(ws => ws >= _charLiteralLo && ws <= _charLiteralHi);
            }

            public override bool Match(Context context)
            {
                if (!context.InSequence)
                    context.Text = context.Text.TrimStart();

                if (context.Text.Length == 0)
                    return false;

                if (context.Text[0] >= _charLiteralLo || context.Text[0] <= _charLiteralHi)
                    return false;

                context.Text = context.Text[1, 0];

                return true;
            }

            public override string ToString()
            {
                return "{0} '{1}' - '{2}'".Args(GetType().Name, _charLiteralLo, _charLiteralHi);
            }
        }

        internal sealed class NotIn : Terminal
        {
            private readonly HashSet<char> _notIn;

            public NotIn(char[] notIn)
                { _notIn = new HashSet<char>(notIn); }

            public override bool Match(Context context)
            {
                if (!context.InSequence)
                    context.Text = context.Text.TrimStart();

                if (context.Text.Length == 0)
                    return true;

                if (_notIn.Contains(context.Text[0]))
                    return false;

                context.Text = context.Text[1, 0];

                return true;
            }

            public override string ToString()
            {
                return "{0} {1}".Args(GetType().Name, string.Join(",", _notIn.ToArray()));
            }
        }

        internal sealed class In : Terminal
        {
            private readonly HashSet<string> _in;

            public In(char[] @in)
                { _in = new HashSet<string>(@in.Select(c => c.ToString())); }

            public In(string[] @in)
                { _in = new HashSet<string>(@in); }

            public override bool Match(Context context)
            {
                if (!context.InSequence)
                    context.Text = context.Text.TrimStart();

                if (context.Text.Length == 0)
                    return false;

                if (char.IsHighSurrogate(context.Text[0]) && char.IsLowSurrogate(context.Text[1]))
                {
                    var utf32 = char.ConvertToUtf32(context.Text[0], context.Text[1]);
                    var s = char.ConvertFromUtf32(utf32);

                    if (!_in.Contains(context.Text[0].ToString()))
                        return false;

                    context.Text = context.Text[2, 0];

                    return true;
                }

                if (!_in.Contains(context.Text[0].ToString())) 
                    return false;

                context.Text = context.Text[1, 0];

                return true;
            }

            public override string ToString()
            {
                return "{0} {1}".Args(GetType().Name, string.Join(",", _in.ToArray()));
            }
        }

        #endregion

        internal sealed class Named : Production
        {
            private readonly string _name;
            internal Production _production;

            public Named(string name, Production production)
            {
                _name = name;
                _production = production;
            }

            public string Name { get { return _name; } }

            public Production Production { get { return _production; } }

            public override string ToString()
            {
                return "{0} {1}".Args(GetType().Name, _name);
            }
        }

        internal sealed class NamedCompletion : Production
        {
            private readonly string _name;
            private readonly Context.Snapshot _snapshot;

            public NamedCompletion(string name, Context.Snapshot snapshot)
            {
                _name = name;
                _snapshot = snapshot;
            }

            public string Name 
                { get { return _name; } }

            public string GetValue(Context context) 
            {
                SubString diff = _snapshot.Text[0, -context.Text.Length];
                return diff.Trim().ToString();  
            }

            public override string ToString()
                { return "{0} {1}".Args(GetType().Name, _name); }
        }

        internal sealed class Sequence : All
        {
            public Sequence(Production[] productions) : base(productions)
                { }

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
            {
                context.InSequences++;
                context.Text = context.Text.TrimStart();
                resolvent.Push(Invoke(new SequenceCompletion()));
                base.Resolve(resolvent, choicePoints, context, progress);
            }

            public override string ToString()
                { return GetType().Name; }
        }

        internal sealed class SequenceCompletion : Condition
        {
            public SequenceCompletion()
                : base(Seq.Array<Production>())
                { }

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
                { context.InSequences--; }

            public override string ToString()
                { return GetType().Name; }
        }

        internal sealed class Reference : Production
        {
            private readonly Func<Production> _reference;

            public Reference(Func<Production> reference)
                { _reference = reference; }

            public Func<Production> Accessor
                { get { return _reference; } }
        }

        internal sealed class Cut : Production
        { }

        internal sealed class Call : Production
        {
            private Action<T, IDictionary<string, string>> _rule;

            public Call(Action<T, IDictionary<string, string>> ruleAction)
                { _rule = ruleAction;}

            public Action<T, IDictionary<string, string>> Rule { get { return _rule; } }
        }

        internal sealed class SetName : Condition
        {
            private readonly string _name;
            private readonly string _value;

            public SetName(string name, string value, Production production) : base(new [] { production })
            {
                _name = name;
                _value = value;
            }

            public override void Resolve(Stack<Invoke> resolvent, Stack<ChoicePoint> choicePoints, Context context, int progress)
            {
                context.PushName(_name);
                context.Bindings[context.NamedContext] = _value;
                context.PopName();
                resolvent.Push(Invoke(Productions[0]));
            }
        }

        internal bool MatchAndProduce(Context context, out string errMsg)
        {
            errMsg = "Parsing...";
            string TRACE = String.Empty;
            string SOURCE = String.Empty;

            var choicePoints = new Stack<ChoicePoint>();
            var productions = new Stack<Invoke>(Seq.Of(Invoke(this)));

            while (!productions.IsEmpty() || context.Text.TrimStart().Length > 0)
            {
                if (productions.IsEmpty())
                {
                    errMsg = "Parser productions are empty indicating unknown input. Check syntax or rules! {0}".Args(context.Text); 
                    return false;
                }

                TRACE = context.Trace;
                SOURCE = context.Text;
                
                var invoke = productions.Pop();
                var goal = invoke.Production;
                
                if (goal is Terminal terminal)
                {
                    if (!terminal.Match(context))
                    {
                        if (choicePoints.IsEmpty())
                        {
                            errMsg = "Parser choice points are empty.  {0}".Args(context.Text); 
                            return false;
                        }
                        else
                        {
                            var choicePoint = choicePoints.Pop();
                            productions = choicePoint.Productions;
                            context.State = choicePoint.Snapshot;
                            continue;
                        }
                    }
                    else
                        continue;
                } 
                
                if (goal is Condition condition)
                {
                    condition.Resolve(productions, choicePoints, context, invoke.Progress);

                    // var trace = goal as Trace;
                    //if (trace != null)
                    //{
                    //   var text = context.Trace;
                    //   context.Appliers.Add(t => System.Diagnostics.Debug.WriteLine(text));
                    //}
                    continue;
                }

                if (goal is Call call)
                {
                    var bindings = context.Bindings.Clone();
                    context.Appliers.Add(t => call.Rule(t, bindings));
                    continue;
                }

                if (goal is Named named)
                {
                    context.PushName(named.Name);
                    productions.Push(Invoke(new NamedCompletion(context.NamedContext, context.State)));
                    productions.Push(Invoke(named.Production));

                    continue;
                }

                if (goal is NamedCompletion namedCompletion)
                {
                    context.PopName();
                    context.Bindings[namedCompletion.Name] = namedCompletion.GetValue(context);
                    continue;
                }

                if (goal is Cut cut)
                {
                    choicePoints.Clear();
                    continue;
                }

                throw new InvalidOperationException("Got illegal Production! {0}".Args(goal));
            }

            errMsg = "Parse succeeded.";
            return true;
        }

        internal Invoke Invoke(Production prod)
        {
            return new Invoke(prod, -1);
        }
        internal Invoke Invoke(Production prod, int progress)
        {
            return new Invoke(prod, progress);
        }

        internal Stack<Invoke> CloneStack(Stack<Invoke> toClone)
            { return new Stack<Invoke>(toClone.Reverse()); }

        public static implicit operator Production(string literal)
            { return (Production)new Literal(literal, true); }

        public static implicit operator Production(char character)
            { return (Production)new CharLiteral(character); }

        public static implicit operator Production(Func<Production>? accessor)
            { return (Production)new Reference(accessor!); }

        public override string ToString()
        {
            return GetType().Name;
        }
    }

    #endregion

    #region Standard production declaration primitives

    /// <summary>
    /// DIGIT ::= [0-9]
    /// </summary>
    protected static readonly Func<Production> DIGIT = () => InRange('0', '9');
    /// <summary>
    /// WS ::= #x20 |  #x9 |  #xD |  #xA 
    /// </summary>
    protected static readonly Func<Production> WS = () => In('\x20', '\x9', '\xD', '\xA');
    /// <summary>
    /// WHITESPACE ::= WS*
    /// </summary>
    /// <remarks>
    /// Use the Token specifier to specify grammar tokens instead of injecting WHITESPACE specifications.
    /// </remarks>
    protected static readonly Func<Production> WHITESPACE = () => ZeroOrMore(WS);
    /// <summary>
    /// LETTER ::= [a-zA-Z]
    /// </summary>
    protected static readonly Func<Production> LETTER = () => InRange('a','z', 'A','Z');
    /// <summary>
    /// DIGIT_OR_LETTER ::= DIGIT | LETTER
    /// </summary>
    protected static readonly Func<Production> DIGIT_OR_LETTER = () => AnyOf(DIGIT, LETTER);
    /// <summary>
    /// SIGN ::= [+-]
    /// </summary>
    protected static readonly Func<Production> SIGN = () => In('+', '-');
    /// <summary>
    /// INTEGER ::= [+-]? [0-9]+ 
    /// </summary>
    protected static readonly Func<Production> INTEGER = () => All(Optional(SIGN), OneOrMore(DIGIT));
    /// <summary>
    /// DECIMAL ::= [+-]? [0-9]* '.' [0-9]+ 
    /// </summary>
    protected static readonly Func<Production> DECIMAL = () => All(Optional(SIGN), ZeroOrMore(DIGIT), '.', OneOrMore(DIGIT));
    /// <summary>
    /// DOUBLE ::= [+-]? ([0-9]+ '.' [0-9]* EXPONENT |  '.' [0-9]+ EXPONENT |  [0-9]+ EXPONENT) 
    /// </summary>
    protected static readonly Func<Production> DOUBLE = () => All(Optional(SIGN), AnyOf(All(OneOrMore(DIGIT), '.', ZeroOrMore(DIGIT), EXPONENT!), All('.', OneOrMore(DIGIT), EXPONENT!), All(OneOrMore(DIGIT, EXPONENT!))));
    /// <summary>
    /// EXPONENT ::= [eE] [+-]? [0-9]+ 
    /// </summary>
    protected static readonly Func<Production> EXPONENT = () => All(In('e', 'E'), Optional(SIGN), OneOrMore(DIGIT));

    #endregion

    #region Production API methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="trace"></param>
    /// <param name="production"></param>
    /// <returns></returns>
    protected static Production _(string trace, Production production)
    { return new Production.Trace(trace, production); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="productions"></param>
    /// <returns></returns>
    protected static Production All(params Production[] productions)
        { return new Production.All(productions); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="productions"></param>
    /// <returns></returns>
    protected static Production AnyOf(params Production[] productions)
        { return new Production.AnyOf(productions); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="productions"></param>
    /// <returns></returns>
    protected static Production Optional(params Production[] productions)
        { return new Production.Optional(All(productions)); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="productions"></param>
    /// <returns></returns>
    protected static Production ZeroOrMore(params Production[] productions)
        { return new Production.ZeroOrMore(All(productions)); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="productions"></param>
    /// <returns></returns>
    protected static Production OneOrMore(params Production[] productions)
        { return new Production.OneOrMore(All(productions)); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loHiPairs"></param>
    /// <returns></returns>
    protected static Production InRange(params char[] loHiPairs)
        { return new Production.InRange(loHiPairs); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lo"></param>
    /// <param name="hi"></param>
    /// <returns></returns>
    protected static Production InRangeU(string lo, string hi)
        { return new Production.InRangeU(lo, hi); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="notIn"></param>
    /// <returns></returns>
    protected static Production NotIn(IEnumerable<char> notIn)
        { return new Production.NotIn(notIn.ToArray()); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="notIn"></param>
    /// <returns></returns>
    protected static Production NotIn(params char[] notIn)
        { return new Production.NotIn(notIn); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="in"></param>
    /// <returns></returns>
    protected static Production In(IEnumerable<char> @in)
        { return new Production.In(@in.ToArray()); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="in"></param>
    /// <returns></returns>
    protected static Production In(params char[] @in)
        { return new Production.In(@in); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="in"></param>
    /// <returns></returns>
    protected static Production In(params string[] @in)
        { return new Production.In(@in); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="declaration"></param>
    /// <returns></returns>
    protected static Production Named(string name, Production declaration)
        { return new Production.Named(name, declaration); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    protected static Production SetName(string name, string value, Production declaration)
        { return new Production.SetName(name, value, declaration); }

    /// <summary>
    /// A production rule cut specifier.
    /// </summary>
    /// <remarks>
    /// A 'CUT' tells the engine to accept what it has gotten so far and refrain from looking forward since we know it is not needed.
    /// This is a form of optimization.
    /// </remarks>
    protected static Production CUT
        { get { return new Production.Cut(); } }

    /// <summary>
    /// A production rule call specifier.
    /// </summary>
    /// <remarks>
    /// Calls are typically wrapped in named production declarations : <code>static readonly Func<Production> @Name = () => Call((a,b) => {});</code>
    /// </remarks>
    /// <param name="ruleAction">A lambda in the form (producer,names) => producer.Property = names["name.property"]</param>
    /// <returns>A call grammar production.</returns>
    protected static Production Call(Action<T, IDictionary<string, string>> ruleAction)
        { return new Production.Call(ruleAction); }

    /// <summary>
    /// A token specifier.
    /// </summary>
    /// <param name="productions">A list of productions defining the token.</param>
    /// <returns>A token grammar production.</returns>
    protected static Production Token(params Production[] productions)
        { return new Production.Sequence(productions); }

    /// <summary>
    /// A case insensitive statement specifier.
    /// </summary>
    /// <param name="literal">A statement.</param>
    /// <returns>A case insensitivity grammar production.</returns>
    protected static Production i(string literal)
        { return new Production.Literal(literal, false); }

    #endregion

    protected class Context
    {
        public SubString Text;
        public string Trace = string.Empty;
        public List<Action<T>> Appliers = new();
        public IDictionary<string, string> Bindings = new Dictionary<string, string>();
        public string NamedContext { get; private set; } = string.Empty;
        internal int InSequences = 0;

        internal bool InSequence { get { return InSequences > 0; } }

        public override string ToString()
        { 
            return "'{0}{1}' NamedContext: '{2}' Bindings: {3}"
                .Args(Text.Length > 42 ? Text[0, 41] : Text,
                Text.Length > 42 ? "..." : "",
                NamedContext, 
                Bindings.Count
                ); 
        }

        public void PushName(string extension)
        {
            if (NamedContext.IsEmpty())
                NamedContext = extension;
            else
                NamedContext += "." + extension;
        }

        public void PopName()
        {
            var removeFrom = NamedContext.LastIndexOf('.');
            if (removeFrom >= 0)
                NamedContext = NamedContext.Substring(0, removeFrom);
            else
                NamedContext = string.Empty; // null;
        }

        public struct Snapshot
        {
            internal int ApplierCount;
            internal SubString Text;
            internal string NamedContext;
            internal IDictionary<string, string> Bindings;
            internal int InSequences;

            internal Snapshot(int applierCount, SubString text, string namedContext, IDictionary<string, string> bindings, int inSequences)
            {
                ApplierCount = applierCount;
                Text = text;
                NamedContext = namedContext;
                Bindings = new Dictionary<string, string>(bindings);
                InSequences = inSequences;
            }

            public override string ToString()
            {
                return "'{0}...', Appliers: {1}, NamedContext: '{2}', Bindings: {3}"
                    .Args(Text.Length > 25 ? Text[0, 24] : Text, ApplierCount, NamedContext, Bindings.Count);
            }
        }

        internal Snapshot State
        {
            get 
            {
                return new Snapshot(Appliers.Count, Text, NamedContext, Bindings, InSequences);
            }

            set
            {
                Appliers.RemoveRange(value.ApplierCount, Appliers.Count - value.ApplierCount);
                NamedContext = value.NamedContext;
                InSequences = value.InSequences;
                Bindings = value.Bindings;
                Text = value.Text;
            }
        }
    }

    protected class ChoicePoint
    {
        public Context.Snapshot Snapshot { get; private set; }
        public Stack<Invoke> Productions { get; private set; }

        private ChoicePoint(Stack<Invoke> productions, Context.Snapshot snapshot)
        {
            Productions = productions;
            Snapshot = snapshot;
        }

        public static ChoicePoint Create(Stack<Invoke> productions, Context.Snapshot snapshot)
            { return new ChoicePoint(productions, snapshot); }
    }

    protected class Invoke
    {
        public readonly Production Production;
        public readonly int Progress;
        public Invoke(Production prod, int progress)
        {
            Production = prod;
            Progress = progress;
        }
        public override string ToString()
        {
            return "{0}, {1}".Args(Production, Progress);
        }
    }

    private readonly Production _rootGrammar;

    #region Construction

    /// <summary>
    /// Creates a parser
    /// </summary>
    /// <param name="grammar">The grammar.</param>
    /// <param name="rules">The production rules.</param>
    protected Parser(Production grammar)
    {
        _rootGrammar = grammar;

        var accessors = new Dictionary<Func<Production>, Production>();

        DeReferenceGrammar(ref _rootGrammar, accessors);
    }

    /// <summary>
    /// Dereferences the grammar by removing all reference productions.
    /// </summary>
    /// <param name="declaration">A root grammar declaration.</param>
    /// <param name="visited">A collector set for visited references.</param>
    /// <param name="dereferencedAccessors">A collector dictionary for resolved declaration references.</param>
    private void DeReferenceGrammar(ref Production declaration, Dictionary<Func<Production>, Production> dereferencedAccessors)
    {
        var reference = declaration as Production.Reference;

        if (reference != null)
        {
            if (dereferencedAccessors.TryGetValue(reference.Accessor, out var substitute))
            {
                declaration = substitute;
                return;
            }

            substitute = reference.Accessor();

            var subref = substitute as Production.Reference;
            while (subref != null)
            {
                substitute = subref.Accessor();
                subref = substitute as Production.Reference;
            }

            dereferencedAccessors.Add(reference.Accessor, substitute);
            DeReferenceGrammar(ref substitute, dereferencedAccessors);
            declaration = substitute;

            return;
        }

        var condition = declaration as Production.Condition;

        if (condition != null)
        {
            for (int i=0; i<condition.Productions.Length; i++)
            {
                DeReferenceGrammar(ref condition.Productions[i], dereferencedAccessors);
            }

            return;
        }

        var named = declaration as Production.Named;

        if (named != null)
        {
            DeReferenceGrammar(ref named._production, dereferencedAccessors);
            return;
        }
    }

    #endregion

    /// <summary>
    /// Applies a text translation to a target.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="target">The target.</param>
    public bool Apply(string text, T target, out string errMsg)
    {
        var context = new Context { Text = text.AsSubString(), Appliers = new List<Action<T>>() };
        if (!_rootGrammar.MatchAndProduce(context, out errMsg)) return false;
        context.Appliers.Do(action => action(target));
        return true;
    }
}
