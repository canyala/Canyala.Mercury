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
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Core.Contracts;
using Canyala.Lagoon.Core.Extensions;

using Canyala.Mercury.Core;

namespace Canyala.Mercury.Rdf.Internal;

/// <summary>
/// 
/// </summary>
public class Operators
{
    public Dataset? Dataset { get; set; }
    public Namespaces? Namespaces { get; set; }

    public Operators() { }

#pragma warning disable CA1822 // No mark members as static, used by reflection!

    public Resource Or(Resource left, Resource right)
    {
        bool? booleanLeft = EffectiveBooleanValue(left);
        bool? booleanRight = EffectiveBooleanValue(right);

        if (!(booleanLeft.HasValue || booleanRight.HasValue))
            return Resource.Error;
        else if (booleanLeft.HasValue && booleanRight.HasValue)
            return Literal.From(booleanLeft.Value || booleanRight.Value);
        else if (booleanLeft.HasValue)
            return booleanLeft.Value ? Literal.From(true) : Resource.Error;
        else
            return booleanRight!.Value ? Literal.From(true) : Resource.Error;
    }

    public Resource And(Resource left, Resource right)
    {
        bool? booleanLeft = EffectiveBooleanValue(left);
        bool? booleanRight = EffectiveBooleanValue(right);

        if (!(booleanLeft.HasValue || booleanRight.HasValue))
            return Resource.Error;
        else if (booleanLeft.HasValue && booleanRight.HasValue)
            return Literal.From(booleanLeft.Value && booleanRight.Value);
        else if (booleanLeft.HasValue)
            return booleanLeft.Value ? Resource.Error : Literal.From(false);
        else 
            return booleanRight!.Value ? Resource.Error : Literal.From(false);
    }

    public Resource Not(Resource arg)
    {
        bool? booleanLeft = EffectiveBooleanValue(arg);

        if (booleanLeft == null)
            return Resource.Error;

        return Literal.From(!booleanLeft.Value);
    }

    public Resource AreEqual(Resource left, Resource right)
        { return CompareResources(left, right, i => i == 0); }

    public Resource NotEquals(Resource left, Resource right)
        { return CompareResources(left, right, i => i != 0); }

    public Resource LessThan(Resource left, Resource right)
        { return CompareResources(left, right, i => i < 0); }

    public Resource GreaterThan(Resource left, Resource right)
        { return CompareResources(left, right, i => i > 0); }

    public Resource LessOrEqualThan(Resource left, Resource right)
        { return CompareResources(left, right, i => i <= 0); }

    public Resource GreaterOrEqualThan(Resource left, Resource right)
        { return CompareResources(left, right, i => i >= 0); }

    public Resource In(Resource number, params Resource[] ins)
    {
        Resource? error = null;

        foreach (var item in ins)
        {
            var val = AreEqual(number, item) as Literal;
            if (val != null)
            {
                if (val.AsBool)
                    return val;
            }
            else
                error = Resource.Error;
        }

        if  (error != null)
            return error;

        return Literal.From(false);
    }

    public Resource NotIn(Resource number, params Resource[] notIns)
    {
        var val = In(number, notIns) as Literal;
        if (val != null)
            return Literal.From(! val.AsBool);

        return Resource.Error;
    }

    public Resource Add(Resource left, Resource right)
    {
        if (!AreNumeric(left, right))
            return Resource.Error;

        var leftLiteral = (Literal)left;
        var rightLiteral = (Literal)right;

        var promote = NumericPromotionType(leftLiteral, rightLiteral);

        if (promote == null)
            return Resource.Error;

        if (promote == Ontologies.Xsd.integer)
            return Literal.From(leftLiteral.AsInt + rightLiteral.AsInt);
        else if (promote == Ontologies.Xsd.@float)
            return Literal.From(leftLiteral.AsFloat + rightLiteral.AsFloat);
        else if (promote == Ontologies.Xsd.@double)
            return Literal.From(leftLiteral.AsDouble + rightLiteral.AsDouble);
        else if (promote == Ontologies.Xsd.@decimal)
            return Literal.From(leftLiteral.AsDecimal + rightLiteral.AsDecimal);
        else
            return Resource.Error;
    }

    public Resource Subtract(Resource left, Resource right)
    {
        if (!AreNumeric(left, right))
            return Resource.Error;

        var leftLiteral = (Literal)left;
        var rightLiteral = (Literal)right;

        var promote = NumericPromotionType(leftLiteral, rightLiteral);

        if (promote == null)
            return Resource.Error;

        if (promote == Ontologies.Xsd.integer)
            return Literal.From(leftLiteral.AsInt - rightLiteral.AsInt);
        else if (promote == Ontologies.Xsd.@float)
            return Literal.From(leftLiteral.AsFloat - rightLiteral.AsFloat);
        else if (promote == Ontologies.Xsd.@double)
            return Literal.From(leftLiteral.AsDouble - rightLiteral.AsDouble);
        else if (promote == Ontologies.Xsd.@decimal)
            return Literal.From(leftLiteral.AsDecimal - rightLiteral.AsDecimal);
        else
            return Resource.Error;
    }

    public Resource Multiply(Resource left, Resource right)
    {
        if (!AreNumeric(left, right))
            return Resource.Error;

        var leftLiteral = (Literal)left;
        var rightLiteral = (Literal)right;

        var promote = NumericPromotionType(leftLiteral, rightLiteral);

        if (promote == null)
            return Resource.Error;

        if (promote == Ontologies.Xsd.integer)
            return Literal.From(leftLiteral.AsInt * rightLiteral.AsInt);
        else if (promote == Ontologies.Xsd.@float)
            return Literal.From(leftLiteral.AsFloat * rightLiteral.AsFloat);
        else if (promote == Ontologies.Xsd.@double)
            return Literal.From(leftLiteral.AsDouble * rightLiteral.AsDouble);
        else if (promote == Ontologies.Xsd.@decimal)
            return Literal.From(leftLiteral.AsDecimal * rightLiteral.AsDecimal);
        else
            return Resource.Error;

    }

    public Resource Divide(Resource left, Resource right)
    {
        if (!AreNumeric(left, right))
            return Resource.Error;

        var leftLiteral = (Literal)left;
        var rightLiteral = (Literal)right;

        Iri? promote = null;
        if (leftLiteral.Type == Ontologies.Xsd.integer && rightLiteral.Type == Ontologies.Xsd.integer)
            promote = Ontologies.Xsd.@decimal;
        else
            promote = NumericPromotionType(leftLiteral, rightLiteral);
        
        if (promote == null)
            return Resource.Error;

        if (promote == Ontologies.Xsd.@float)
            return Literal.From(leftLiteral.AsFloat / rightLiteral.AsFloat);
        else if (promote == Ontologies.Xsd.@double)
            return Literal.From(leftLiteral.AsDouble / rightLiteral.AsDouble);
        else if (promote == Ontologies.Xsd.@decimal)
            return Literal.From(leftLiteral.AsDecimal / rightLiteral.AsDecimal);
        else
            return Resource.Error;
    }

    public Resource Negate(Resource arg)
    {
        if (!AreNumeric(arg))
            return Resource.Error;

        var literal = (Literal)arg;

        if (literal.Type == Ontologies.Xsd.integer)
            return Literal.From(-literal.AsInt);
        else if (literal.Type == Ontologies.Xsd.@float)
            return Literal.From(-literal.AsFloat);
        else if (literal.Type == Ontologies.Xsd.@double)
            return Literal.From(-literal.AsDouble);
        else if (literal.Type == Ontologies.Xsd.@decimal)
            return Literal.From(-literal.AsDecimal);
        else
            return Resource.Error;
    }

    #region Typed comparers

    private int NumericCompare(Literal left, Literal right)
    {
        Contract.Assume(left.Type!.Equals(right.Type), "Operands must have same numeric type");

        if (left.Type.Equals(Ontologies.Xsd.integer))
            return left.AsInt.CompareTo(right.AsInt);

        if (left.Type == Ontologies.Xsd.@decimal)
            return left.AsDecimal.CompareTo(right.AsDecimal);

        if (left.Type == Ontologies.Xsd.@double)
            return left.AsDouble.CompareTo(right.AsDouble);

        if (left.Type == Ontologies.Xsd.@float)
            return left.AsFloat.CompareTo(right.AsFloat);

        throw new NotImplementedException("LessThanNumeric is not defined for type: {0}".Args(left.Type));
    }

    private int SimpleCompare(Literal left, Literal right)
        { return string.Compare(left.Value, right.Value, StringComparison.InvariantCulture); }

    private int BoolCompare(Literal left, Literal right)
        { return left.AsBool.CompareTo(right.AsBool); }

    private int DateTimeCompare(Literal left, Literal right)
        { return left.AsDateTime.CompareTo(right.AsDateTime); }

    private Resource CompareResources(Resource left, Resource right, Predicate<int> comparer)
    {
        return Literal.From(comparer(Compare(left, right)));
    }

    public int Compare(Resource left, Resource right)
    {
        if (AreNumeric(left, right))
        {
            var leftLiteral = (Literal)left;
            var rightLiteral = (Literal)right;
            
            var type = NumericPromotionType(leftLiteral, rightLiteral);

            leftLiteral = Promote(leftLiteral, type!);
            rightLiteral = Promote(rightLiteral, type!);

            return NumericCompare(leftLiteral, rightLiteral);
        }
        else if (AreSimple(left, right))
        {
            var leftLiteral = (Literal)left;
            var rightLiteral = (Literal)right;
            return SimpleCompare(leftLiteral, rightLiteral);
        }
        else if (AreString(left, right))
        {
            var leftLiteral = (Literal)left;
            var rightLiteral = (Literal)right;
            return SimpleCompare(leftLiteral, rightLiteral);   // Yes! string and simple literal are the same here!
        }
        else if (AreBool(left, right))
        {
            var leftLiteral = (Literal)left;
            var rightLiteral = (Literal)right;
            return BoolCompare(leftLiteral, rightLiteral);
        }
        else if (AreDateTime(left, right))
        {
            var leftLiteral = (Literal)left;
            var rightLiteral = (Literal)right;
            return DateTimeCompare(leftLiteral, rightLiteral);
        }
        else if (!left.IsBound() && !right.IsBound())
        {
            return 0;
        }
        else if (!left.IsBound())
        {
            return -1;
        }
        else if (!right.IsBound())
        {
            return 1;
        }
        else if (left.IsBlank() && right.IsBlank())
        {
            return String.Compare(left.Value, right.Value, StringComparison.InvariantCulture);
        }
        else if (left.IsBlank())
        {
            return -1;
        }
        else if (right.IsBlank())
        {
            return 1;
        }
        else if (left.IsIri() && right.IsIri())
        {
            return String.Compare(left.Value, right.Value, StringComparison.InvariantCulture);
        }
        else if (left.IsIri())
        {
            return -1;
        }
        else if (right.IsIri())
        {
            return 1;
        }
        else
        {
            return String.Compare(left.Value, right.Value, StringComparison.InvariantCulture);
        }
    }

    #endregion

    #region Type functions

    private static readonly HashSet<Iri> _numericTypes = new HashSet<Iri> 
    {
        Ontologies.Xsd.integer,
        Ontologies.Xsd.@decimal,
        Ontologies.Xsd.@double,
        Ontologies.Xsd.@float
    };

    internal static bool AreNumeric(params Resource[] args)
    {
        foreach (var arg in args)
        {
            var literal = arg as Literal;
            if (literal == null || !_numericTypes.Contains(literal.Type!))
               return false;         
        }

        return true;
    }

    private static bool AreSimple(params Resource[] args)
    {
        foreach (var arg in args)
        {
            var literal = arg as Literal;
            if (literal == null || literal.Type != null || literal.Language != null)
                return false;
        }

        return true;
    }

    private bool AreString(params Resource[] args)
    {
        foreach (var arg in args)
        {
            var literal = arg as Literal;
            if (literal == null || !(literal.Type!.Equals(Ontologies.Xsd.@string) || literal.Language != null))
                return false;
        }

        return true;
    }

    private bool AreBool(params Resource[] args)
    {
        foreach (var arg in args)
        {
            var literal = arg as Literal;
            if (literal == null || !literal.Type!.Equals(Ontologies.Xsd.boolean))
                return false;
        }

        return true;
    }

    private bool AreDateTime(params Resource[] args)
    {
        foreach (var arg in args)
        {
            var literal = arg as Literal;
            if (literal == null || !literal.Type!.Equals(Ontologies.Xsd.dateTime))
                return false;
        }

        return true;
    }

    #endregion

    #region Promotions

    private Iri? NumericPromotionType(params Literal[] args)
    {
        Iri? promoteTo = null;

        foreach (var literal in args)
        {
            if (promoteTo == null)
            {
                promoteTo = literal.Type;
                continue;
            }

            if (promoteTo.Equals(Ontologies.Xsd.integer))
            {
                if (literal.Type!.Equals(Ontologies.Xsd.@decimal))
                {
                    promoteTo = Ontologies.Xsd.@decimal;
                    continue;
                }

                if (literal.Type.Equals(Ontologies.Xsd.@float))
                {
                    promoteTo = Ontologies.Xsd.@float;
                    continue;
                }

                if (literal.Type.Equals(Ontologies.Xsd.@double))
                {
                    promoteTo = Ontologies.Xsd.@double;
                    continue;
                }

                continue;
            }

            if (promoteTo.Equals(Ontologies.Xsd.@decimal))
            {
                if (literal.Type!.Equals(Ontologies.Xsd.@float) || literal.Type.Equals(Ontologies.Xsd.@double))
                    return null;
            }

            if (promoteTo.Equals(Ontologies.Xsd.@float))
            {
                if (literal.Type!.Equals(Ontologies.Xsd.@decimal))
                    return null;

                if (literal.Type.Equals(Ontologies.Xsd.@double))
                {
                    promoteTo = Ontologies.Xsd.@double;
                    continue;
                }

                continue;
            }

            if (promoteTo.Equals(Ontologies.Xsd.@double))
            {
                if (literal.Type!.Equals(Ontologies.Xsd.@decimal))
                    return null;
            }
        }

        return promoteTo;
    }

    private Literal Promote(Literal old, Iri promotonType)
        { return new Literal(old.Value, promotonType); }

    #endregion

    internal static bool? EffectiveBooleanValue(Resource arg)
    {
        var literal = arg as Literal;
        if (literal == null)
            return null;

        if (literal.Type!.Equals(Ontologies.Xsd.boolean))
        {
            bool boolResult;
            return literal.TryBool(out boolResult) ? boolResult : false;
        }

        if (literal.Type!.Equals(Ontologies.Xsd.integer))
        {
            int intResult;
            return literal.TryInt(out intResult) ? intResult != 0 : false;
        }

        if (literal.Type!.Equals(Ontologies.Xsd.@decimal))
        {
            decimal decResult;
            return literal.TryDecimal(out decResult) ? decResult != decimal.Zero : false;
        }

        if (literal.Type!.Equals(Ontologies.Xsd.@float))
        {
            float floatResult;
            return literal.TryFloat(out floatResult) ? floatResult != 0f && !float.IsNaN(floatResult) : false;
        }

        if (literal.Type!.Equals(Ontologies.Xsd.@double))
        {
            double doubleResult;
            return literal.TryDouble(out doubleResult) ? doubleResult != 0f && !double.IsNaN(doubleResult) : false;
        }

        if (literal.Type == null || literal.Type.Equals(Ontologies.Xsd.@string))
            return literal.Value.Length > 0;

        return null;
    }

#pragma warning restore CA1822 // Mark members as static

}
