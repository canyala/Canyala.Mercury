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
/// Implements a .NET parser for Lua script.
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

        /*
        stat::=  ‘;’ | varlist ‘=’ explist | 
		 functioncall | 
		 label | 
		 break | 
		 goto Name | 
		 do block end | 
		 while exp do block end | 

         repeat block until exp | 
		 if exp then block {elseif exp then block}
    [else block] end | 
		 for Name ‘=’ exp ‘,’ exp[‘,’ exp] do block end | 
		 for namelist in explist do block end | 

         function funcname funcbody | 
		 local function Name funcbody | 

         local attnamelist[‘=’ explist]
        */
        static readonly Func<Production> stat = () => _(nameof(stat), @NotImplemented);
        /*
    attnamelist::=  Name attrib {‘,’ Name attrib }

    attrib::= [‘<’ Name ‘>’]
        */

        /// <summary>
        /// retstat::= return [explist] [‘;’]
        /// </summary>
        static readonly Func<Production> retstat = () => _(nameof(retstat), @NotImplemented);
    /*
    label::= ‘::’ Name ‘::’

	funcname::= Name {‘.’ Name
}
[‘:’ Name]

varlist::= var {‘,’ var}

	var::= Name | prefixexp ‘[’ exp ‘]’ | prefixexp ‘.’ Name

    namelist ::= Name {‘,’ Name}

explist::= exp {‘,’ exp}

exp::= nil | false | true | Numeral | LiteralString | ‘...’ | functiondef |
     prefixexp | tableconstructor | exp binop exp | unop exp 

	prefixexp ::= var | functioncall | ‘(’ exp ‘)’

	functioncall::= prefixexp args | prefixexp ‘:’ Name args 

	args ::=  ‘(’ [explist] ‘)’ | tableconstructor | LiteralString


    functiondef::= function funcbody

    funcbody ::= ‘(’ [parlist] ‘)’ block end

	parlist ::= namelist [‘,’ ‘...’] | ‘...’

	tableconstructor::= ‘{’ [fieldlist] ‘}’

	fieldlist::= field { fieldsep field}
[fieldsep]

//field::= ‘[’ exp ‘]’ ‘=’ exp | Name ‘=’ exp | exp


    //fieldsep::= ‘,’ | ‘;’

	//binop::=  ‘+’ | ‘-’ | ‘*’ | ‘/’ | ‘//’ | ‘^’ | ‘%’ | 
	//	 ‘&’ | ‘~’ | ‘|’ | ‘>>’ | ‘<<’ | ‘..’ |
	//	 ‘<’ | ‘<=’ | ‘>’ | ‘>=’ | ‘==’ | ‘~=’ |
    //     and | or

        */
        //unop ::= ‘-’ | not | ‘#’ | ‘~’
        static readonly Func<Production> unop = () => AnyOf('-', "not", '#', '~');
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

