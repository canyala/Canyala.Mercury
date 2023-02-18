/*

  MIT License
 
  Copyright (c) 2023 Canyala Innovation (Martin Fredriksson)

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
using System.Xml.Linq;
using Canyala.Mercury.Core.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Canyala.Mercury.Luna;

/// <summary>
/// Implements a .NET parser for Lua scripts.
/// </summary>
/// <remarks>
/// The Luna class is partial because the inner Luna.Producer class is implemented in
/// its own file, Luna.Producer.cs
/// </remarks>
/// <seealso cref="https://www.lua.org"/>
public partial class Luna : Parser<Luna.Producer>
{
    #region Grammar Production Declarations

    // <summary>
    /// Productions declare Lua production rules
    /// based on the Lua bnf grammar.
    /// </summary>
    /// <seealso cref="https://www.lua.org/manual/5.4/manual.html#9"/>
    private class Productions
    {
        static readonly Func<Production> @NotImplemented = () => Call((_, _) => throw new NotImplementedException());

        /// <summary>
        /// chunk ::= block
        /// </summary>
        public static readonly Func<Production> chunk = () => _(nameof(chunk), block);

        /// <summary>
        /// block ::= { stat } [retstat]
        /// </summary>
        static readonly Func<Production> block = () => _(nameof(block), All(ZeroOrMore(stat), Optional(retstat)));

        /// <summary>
        /// stat::=  ‘;’ |
        ///    varlist ‘=’ explist | 
		///    functioncall | 
		///    label | 
		///    'break' | 
		///    'goto' Name | 
		///    'do' block 'end' | 
		///    'while' exp 'do' block 'end' | 
        ///    'repeat' block 'until' exp | 
		///    'if' exp 'then' block {'elseif' exp 'then' block} [else block] 'end' | 
		///    'for' Name ‘=’ exp ‘,’ exp [‘,’ exp] 'do' block 'end' | 
		///    'for' namelist 'in' explist 'do' block 'end' | 
        ///    'function' funcname funcbody | 
		///    'local' 'function' Name funcbody | 
        ///    'local' attnamelist[‘=’ explist]
        /// </summary>
        static readonly Func<Production> stat = () => _(nameof(stat), @NotImplemented);

        /// <summary>
        /// attnamelist::=  Name attrib {‘,’ Name attrib }
        /// </summary>
        static readonly Func<Production> attnamelist = () => _(nameof(attnamelist), All(Name, attrib, ZeroOrMore(',', Name, attrib)));

        /// <summary>
        /// attrib::= [‘<’ Name ‘>’]
        /// </summary>
        static readonly Func<Production> attrib = () => _(nameof(attrib), Optional('<', Name, '>'));

        /// <summary>
        /// retstat::= return [explist] [‘;’]
        /// </summary>
        static readonly Func<Production> retstat = () => _(nameof(retstat), @NotImplemented);

        /// <summary>
        /// label::= ‘::’ Name ‘::’
        /// </summary>
        static readonly Func<Production> label = () => _(nameof(label), All("::", Name, "::"));

        /// <summary>
        /// funcname::= Name {‘.’ Name } [‘:’ Name]
        /// </summary>
        static readonly Func<Production> funcname = () => _(nameof(funcname), All(Name, ZeroOrMore('.', Name), Optional(':', Name)));
        
        /// <summary>
        /// varlist::= var {‘,’ var}
        /// </summary>
        static readonly Func<Production> varlist = () => _(nameof(varlist), @NotImplemented);

	    /// <summary>
        /// var::= Name | prefixexp ‘[’ exp ‘]’ | prefixexp ‘.’ Name
        /// </summary>
        static readonly Func<Production> var = () => _(nameof(var), @NotImplemented);

        /// <summary>
        /// namelist ::= Name {‘,’ Name}
        /// </summary>
        static readonly Func<Production> namelist = () => _(nameof(namelist), @NotImplemented);

        /// <summary>
        /// explist::= exp {‘,’ exp}
        /// </summary>
        static readonly Func<Production> explist = () => _(nameof(explist), @NotImplemented);

        /// <summary>
        /// exp::= 'nil' | 'false' | 'true' | Numeral | LiteralString | ‘...’ | functiondef | prefixexp | tableconstructor | exp binop exp | unop exp
        /// </summary>
        static readonly Func<Production> exp = () => _(nameof(exp), @NotImplemented);

        /// <summary>
        /// prefixexp ::= var | functioncall | ‘(’ exp ‘)’
        /// </summary>
        static readonly Func<Production> prefixexp = () => _(nameof(prefixexp), @NotImplemented);

        /// <summary>
        /// functioncall::= prefixexp args | prefixexp ‘:’ Name args
        /// </summary>
        static readonly Func<Production> functioncall = () => _(nameof(functioncall), @NotImplemented);

        /// <summary>
        /// args ::=  ‘(’ [explist] ‘)’ | tableconstructor | LiteralString
        /// </summary>
        static readonly Func<Production> args = () => _(nameof(args), @NotImplemented);

        /// <summary>
        /// functiondef::= function funcbody
        /// </summary>
        static readonly Func<Production> functiondef = () => _(nameof(functiondef), @NotImplemented);

        /// <summary>
        /// funcbody ::= ‘(’ [parlist] ‘)’ block end
        /// </summary>
        static readonly Func<Production> funcbody = () => _(nameof(funcbody), @NotImplemented);

        /// <summary>
        /// parlist ::= namelist [‘,’ ‘...’] | ‘...’
        /// </summary>
        static readonly Func<Production> parlist = () => _(nameof(parlist), @NotImplemented);

        /// <summary>
        /// tableconstructor::= ‘{’ [fieldlist] ‘}’
        /// </summary>
        static readonly Func<Production> tableconstructor = () => _(nameof(tableconstructor), @NotImplemented);

        /// <summary>
        /// fieldlist::= field { fieldsep field} [fieldsep]
        /// </summary>
        static readonly Func<Production> fieldlist = () => _(nameof(fieldlist), @NotImplemented);

        /// <summary>
        /// field::= ‘[’ exp ‘]’ ‘=’ exp | Name ‘=’ exp | exp
        /// </summary>
        static readonly Func<Production> field = () => _(nameof(field), @NotImplemented);

        /// <summary>
        /// fieldsep::= ‘,’ | ‘;’
        /// </summary>
        static readonly Func<Production> fieldsep = () => _(nameof(fieldsep), @NotImplemented);

        /// <summary>
        /// binop::=  ‘+’ | ‘-’ | ‘*’ | ‘/’ | ‘//’ | ‘^’ | ‘%’ | 
        ///	 ‘&’ | ‘~’ | ‘|’ | ‘>>’ | ‘<<’ | ‘..’ |
        ///	 ‘<’ | ‘<=’ | ‘>’ | ‘>=’ | ‘==’ | ‘~=’ |
        ///     and | or
        /// </summary>
        static readonly Func<Production> binop = () => _(nameof(binop), @NotImplemented);

        /// <summary>
        /// unop ::= ‘-’ | not | ‘#’ | ‘~’
        /// </summary>
        static readonly Func<Production> unop = () => AnyOf('-', "not", '#', '~');

        /// <summary>
        /// 
        /// </summary>
        static readonly Func<Production> Name = () => All(AnyOf('_', LETTER), ZeroOrMore(AnyOf('_', DIGIT_OR_LETTER)));
    }

    #endregion

    #region Internal Construction

    private static readonly Luna Instance = new Luna();

    /// <summary>
    /// Creates the Luna parsing singleton.
    /// </summary>
    private Luna()
        : base(Productions.chunk)
    { /* No implementation */
    }

    #endregion
}

