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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Canyala.Mercury.Core;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

namespace Canyala.Mercury.Rdf.Internal;

/// <summary>
/// 
/// </summary>
public class BuiltIns
{
    public Dataset Dataset { get; set; }
    public Namespaces Namespaces { get; set; }
    public Operators Operators { get; set; }

    private Dictionary<string, Blank> Blanks { get; set; }
    private DateTimeOffset Now;
    private Random Random;

    public BuiltIns()
    {
        Dataset = Dataset.Create();
        Namespaces = new();
        Operators = new();
        Blanks = new(); 
        Random = new Random((int) DateTime.UtcNow.Ticks);
        Now = DateTimeOffset.Now;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource STR(Resource res)
    {
        return Literal.From(res.Value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource LANG(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        return Literal.From(literal.Language);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="languageTag"></param>
    /// <param name="languageRange"></param>
    /// <returns></returns>
    public Resource LANGMATCHES(Resource languageTag, Resource languageRange)
    {
        if (languageTag is Literal simpleTag && languageRange is Literal simpleRange)
        {
            if (simpleRange == "*" && simpleTag.Value.Length > 0)
                return Literal.From(true);

            if (String.Equals(simpleTag.Value, simpleRange.Value, StringComparison.InvariantCultureIgnoreCase))
                return Literal.From(true);
        }

        return Literal.From(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource DATATYPE(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Language == null)
            return Ontologies.Rdf.langString;

        if (literal.Type == null)
            return Ontologies.Xsd.@string;

        return literal.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource IRI(Resource res)
    {
        var iri = res as Iri;

        if (iri != null)
            return iri;

        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type == Ontologies.Xsd.@string)
            return new Iri(literal.Value, Namespaces);

        if (literal.Language == null)
            return new Iri(literal.Value, Namespaces);

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Resource BNODE()
    {
        return Blank.NewBlank();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource BNODE(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal == Ontologies.Xsd.@string || (literal.Type == null && literal.Language == null))
        {
            if (!Blanks.TryGetValue(literal.Value, out var knownBlank))
                Blanks.Add(literal.Value, knownBlank = Blank.NewBlank());

            return knownBlank;
        }

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Resource RAND()
    {
        return Literal.From(Random.NextDouble());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource ABS(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (!Operators.AreNumeric(res))
            return Resource.Error;

        if (literal.Value[0] == '-')
            return new Literal(literal.Value.Substring(1), literal.Type);

        return literal;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource CEIL(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type == Ontologies.Xsd.@double)
            return Literal.From(Math.Ceiling(literal.AsDouble));

        if (literal.Type == Ontologies.Xsd.@float)
            return Literal.From(Math.Ceiling(literal.AsFloat));

        if (literal.Type == Ontologies.Xsd.@decimal)
            return Literal.From(Math.Ceiling(literal.AsDecimal));

        if (literal.Type == Ontologies.Xsd.integer)
            return literal;

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource FLOOR(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type == Ontologies.Xsd.@double)
            return Literal.From(Math.Floor(literal.AsDouble));

        if (literal.Type == Ontologies.Xsd.@float)
            return Literal.From(Math.Floor(literal.AsFloat));

        if (literal.Type == Ontologies.Xsd.@decimal)
            return Literal.From(Math.Floor(literal.AsDecimal));

        if (literal.Type == Ontologies.Xsd.integer)
            return literal;

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource ROUND(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type == Ontologies.Xsd.@double)
            return Literal.From(Math.Round(literal.AsDouble));

        if (literal.Type == Ontologies.Xsd.@float)
            return Literal.From(Math.Round(literal.AsFloat));

        if (literal.Type == Ontologies.Xsd.@decimal)
            return Literal.From(Math.Round(literal.AsDecimal));

        if (literal.Type == Ontologies.Xsd.integer)
            return literal;

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public Resource CONCAT(params Resource[] list)
    {
        var literals = list.OfType<Literal>().ToArray();

        if (literals.Length != list.Length)
            return Resource.Error;

        if (literals.All(literal => literal.Type == Ontologies.Xsd.@string))
        {
            var builder = new StringBuilder();
            foreach (var literal in literals) builder.Append(literal.Value);
            return new Literal(builder.ToString(), Ontologies.Xsd.@string);
        }

        var language = literals[0].Language;

        if (language != null && literals.All(literal => language.Equals(literal.Language, StringComparison.InvariantCultureIgnoreCase)))
        {
            var builder = new StringBuilder();
            foreach (var literal in literals) builder.Append(literal.Value);
            return new Literal(builder.ToString(), language);
        }

        var finalBuilder = new StringBuilder();
        foreach (var literal in literals) finalBuilder.Append(literal.Value);
        return new Literal(finalBuilder.ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource STRLEN(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type == null || literal.Type == Ontologies.Xsd.@string)
            return Literal.From(literal.Value.Length);

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource UCASE(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type == null || literal.Type == Ontologies.Xsd.@string)
            return Literal.From(literal.Value.ToUpper());

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource LCASE(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type == null || literal.Type == Ontologies.Xsd.@string)
            return Literal.From(literal.Value.ToLower());

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource ENCODE_FOR_URI(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        var builder = new StringBuilder();
        foreach (var c in literal.Value)
        {
            if (Char.IsLetterOrDigit(c) || "=-._~".Contains(c))
            {
                builder.Append(c);
                continue;
            }

            builder.Append('%');
            builder.Append(((int)c).ToString("X"));
        }

        return Literal.From(builder.ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource CONTAINS(Resource res1, Resource res2)
    {
        var arg1 = res1 as Literal;
        var arg2 = res2 as Literal;

        if (arg1 == null || arg2 == null)
            return Resource.Error;

        if (arg2.Language != null && arg2.Language != arg1.Language)
            return Resource.Error;

        if (arg1.Type != null && arg1.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (arg2.Type != null && arg2.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        return Literal.From(arg1.Value.Contains(arg2.Value));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource STRSTARTS(Resource res1, Resource res2)
    {
        var arg1 = res1 as Literal;
        var arg2 = res2 as Literal;

        if (arg1 == null || arg2 == null)
            return Resource.Error;

        if (arg2.Language != null && arg2.Language != arg1.Language)
            return Resource.Error;

        if (arg1.Type != null && arg1.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (arg2.Type != null && arg2.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        return Literal.From(arg1.Value.StartsWith(arg2.Value));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource STRENDS(Resource res1, Resource res2)
    {
        var arg1 = res1 as Literal;
        var arg2 = res2 as Literal;

        if (arg1 == null || arg2 == null)
            return Resource.Error;

        if (arg2.Language != null && arg2.Language != arg1.Language)
            return Resource.Error;

        if (arg1.Type != null && arg1.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (arg2.Type != null && arg2.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        return Literal.From(arg1.Value.EndsWith(arg2.Value));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource STRBEFORE(Resource res1, Resource res2)
    {
        var arg1 = res1 as Literal;
        var arg2 = res2 as Literal;

        if (arg1 == null || arg2 == null)
            return Resource.Error;

        if (arg2.Language != null && arg2.Language != arg1.Language)
            return Resource.Error;

        if (arg1.Type != null && arg1.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (arg2.Type != null && arg2.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        int pos = arg1.Value.IndexOf(arg2.Value);

        if (pos < 0)
        {
            if (arg1.Language != null)
                return new Literal(String.Empty, arg1.Language);
            else
                return new Literal(String.Empty, arg1.Type);
        }

        if (arg1.Language != null)
            return new Literal(arg1.Value.Substring(0, pos), arg1.Language);
        else
            return new Literal(arg1.Value.Substring(0, pos), arg1.Type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource STRAFTER(Resource res1, Resource res2)
    {
        var arg1 = res1 as Literal;
        var arg2 = res2 as Literal;

        if (arg1 == null || arg2 == null)
            return Resource.Error;

        if (arg2.Language != null && arg2.Language != arg1.Language)
            return Resource.Error;

        if (arg1.Type != null && arg1.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (arg2.Type != null && arg2.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        int pos = arg1.Value.IndexOf(arg2.Value);

        if (pos < 0)
            return arg1;

        if (arg1.Language != null)
            return new Literal(arg1.Value.Substring(pos + arg2.Value.Length), arg1.Language);
        else
            return new Literal(arg1.Value.Substring(pos + arg2.Value.Length), arg1.Type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource YEAR(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        DateTimeOffset dto;
        if (!literal.TryDateTime(out dto))
            return Resource.Error;

        return Literal.From(dto.Year);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource MONTH(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        DateTimeOffset dto;
        if (!literal.TryDateTime(out dto))
            return Resource.Error;

        return Literal.From(dto.Month);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource DAY(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        DateTimeOffset dto;
        if (!literal.TryDateTime(out dto))
            return Resource.Error;

        return Literal.From(dto.Day);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource HOURS(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        DateTimeOffset dto;
        if (!literal.TryDateTime(out dto))
            return Resource.Error;

        return Literal.From(dto.Hour);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource MINUTES(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        DateTimeOffset dto;
        if (!literal.TryDateTime(out dto))
            return Resource.Error;

        return Literal.From(dto.Minute);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource SECONDS(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        DateTimeOffset dto;
        if (!literal.TryDateTime(out dto))
            return Resource.Error;

        decimal seconds = dto.Second;
        decimal milliseconds = dto.Millisecond;
        return Literal.From(dto.Second + dto.Millisecond / 1000M);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource TIMEZONE(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        DateTimeOffset dto;
        if (!literal.TryDateTime(out dto))
            return Resource.Error;

        return Literal.From(dto.Offset);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource TZ(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type != Ontologies.Xsd.dateTime)
            return Resource.Error;

        return Literal.From(literal.Value.Substring(23));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Resource NOW()
    {
        return Literal.From(Now);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Resource UUID()
    {
        return Iri.Parse(String.Concat("<urn:uuid:", Guid.NewGuid().ToString(), ">"));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Resource STRUUID()
    {
        return Literal.From(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource MD5(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type != null && literal.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (literal.Language != null)
            return Resource.Error;

        using (var md5Hash = System.Security.Cryptography.MD5.Create())
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(literal.Value));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return Literal.From(sBuilder.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource SHA1(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type != null && literal.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (literal.Language != null)
            return Resource.Error;

        using (var sha1Hash = System.Security.Cryptography.SHA1.Create())
        {
            byte[] data = sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(literal.Value));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return Literal.From(sBuilder.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource SHA256(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type != null && literal.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (literal.Language != null)
            return Resource.Error;

        using (var sha256Hash = System.Security.Cryptography.SHA256.Create())
        {
            byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(literal.Value));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return Literal.From(sBuilder.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource SHA384(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type != null && literal.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (literal.Language != null)
            return Resource.Error;

        using (var sha384Hash = System.Security.Cryptography.SHA384.Create())
        {
            byte[] data = sha384Hash.ComputeHash(Encoding.UTF8.GetBytes(literal.Value));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return Literal.From(sBuilder.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource SHA512(Resource res)
    {
        var literal = res as Literal;

        if (literal == null)
            return Resource.Error;

        if (literal.Type != null && literal.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        if (literal.Language != null)
            return Resource.Error;

        using (var sha512Hash = System.Security.Cryptography.SHA512.Create())
        {
            byte[] data = sha512Hash.ComputeHash(Encoding.UTF8.GetBytes(literal.Value));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return Literal.From(sBuilder.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public Resource COALESCE(params Resource[] list)
    {
        foreach (var res in list)
        {
            if (res.IsBound())
                return res;
        }

        return Resource.Error;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <param name="res3"></param>
    /// <returns></returns>
    public Resource IF(Resource res1, Resource res2, Resource res3)
    {
        var ebv = res1.EffectiveBooleanValue();

        if (!ebv.HasValue)
            return Resource.Error;

        if (ebv.Value)
            return res2;
        else
            return res3;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource STRLANG(Resource res1, Resource res2)
    {
        var arg1 = res1 as Literal;
        var arg2 = res2 as Literal;

        if (arg1 == null || arg2 == null)
            return Resource.Error;

        if (!arg1.IsSimple())
            return Resource.Error;

        if (!arg2.IsSimple())
            return Resource.Error;

        return new Literal(arg1.Value, arg2.Value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource STRDT(Resource res1, Resource res2)
    {
        var literal = res1 as Literal;

        if (literal == null)
            return Resource.Error;

        if (!res2.IsIri())
            return Resource.Error;

        return Resource.Parse("\"{0}\"^^{1}".Args(literal.Value, res2), Namespaces);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource sameTerm(Resource res1, Resource res2)
    {
        return Literal.From(res1.Equals(res2));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource isIRI(Resource res)
    {
        return Literal.From(res is Iri);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource isBLANK(Resource res)
    {
        return Literal.From(res is Blank);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource isLITERAL(Resource res)
    {
        return Literal.From(res is Literal);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public Resource isNUMERIC(Resource res)
    {
        return Literal.From(Operators.AreNumeric(res));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    /// <returns></returns>
    public static Resource BOUND(Resource res)
    {
        return Literal.From(res.IsBound());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource REGEX(Resource res1, Resource res2)
    {
        var text = res1 as Literal;

        if (text == null)
            return Resource.Error;

        if (text.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        var pattern = res2 as Literal;

        if (pattern == null)
            return Resource.Error;

        return Literal.From(Regex.IsMatch(text.Value, pattern.Value, RegexOptions.CultureInvariant));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <param name="res3"></param>
    /// <returns></returns>
    public Resource REGEX(Resource res1, Resource res2, Resource res3)
    {
        var text = res1 as Literal;
        if (text == null || text.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        var pattern = res2 as Literal;
        if (pattern == null || !pattern.IsSimple())
            return Resource.Error;

        var flags = res3 as Literal;
        if (flags == null || !flags.IsSimple())
            return Resource.Error;

        return Literal.From(Regex.IsMatch(text.Value, pattern.Value, ParseOptions(flags.Value)));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <returns></returns>
    public Resource SUBSTR(Resource res1, Resource res2)
    {
        var arg1 = res1 as Literal;
        if (arg1 == null) return Resource.Error;

        var arg2 = res2 as Literal;
        if (arg2 == null) return Resource.Error;

        int startingLoc;
        if (!arg2.TryInt(out startingLoc))
            return Resource.Error;

        if (arg1.Language != null)
            return new Literal(arg1.Value.Substring(startingLoc), arg1.Language);

        return new Literal(arg1.Value.Substring(startingLoc), arg1.Type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <param name="res3"></param>
    /// <returns></returns>
    public Resource SUBSTR(Resource res1, Resource res2, Resource res3)
    {
        var arg1 = res1 as Literal;
        if (arg1 == null) return Resource.Error;

        var arg2 = res2 as Literal;
        if (arg2 == null) return Resource.Error;

        var arg3 = res3 as Literal;
        if (arg3 == null) return Resource.Error;

        int startingLoc;
        if (!arg2.TryInt(out startingLoc))
            return Resource.Error;

        int length;
        if (!arg2.TryInt(out length))
            return Resource.Error;


        if (arg1.Language != null)
            return new Literal(arg1.Value.Substring(startingLoc, length), arg1.Language);

        return new Literal(arg1.Value.Substring(startingLoc, length), arg1.Type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <param name="res3"></param>
    /// <returns></returns>
    public Resource REPLACE(Resource res1, Resource res2, Resource res3)
    {
        var arg = res1 as Literal;
        if (arg == null || arg.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        var pattern = res2 as Literal;
        if (pattern == null || !pattern.IsSimple())
            return Resource.Error;

        var replacement = res3 as Literal;
        if (replacement == null || !replacement.IsSimple())
            return Resource.Error;

        return new Literal(Regex.Replace(arg.Value, pattern.Value, replacement.Value, RegexOptions.CultureInvariant), arg.Type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res1"></param>
    /// <param name="res2"></param>
    /// <param name="res3"></param>
    /// <param name="res4"></param>
    /// <returns></returns>
    public Resource REPLACE(Resource res1, Resource res2, Resource res3, Resource res4)
    {
        var arg = res1 as Literal;
        if (arg == null || arg.Type != Ontologies.Xsd.@string)
            return Resource.Error;

        var pattern = res2 as Literal;
        if (pattern == null || !pattern.IsSimple())
            return Resource.Error;

        var replacement = res3 as Literal;
        if (replacement == null || !replacement.IsSimple())
            return Resource.Error;

        var flags = res4 as Literal;
        if (flags == null || !flags.IsSimple())
            return Resource.Error;

        return new Literal(Regex.Replace(arg.Value, pattern.Value, replacement.Value, ParseOptions(flags.Value)), arg.Type);
    }

    #region Aggregator implementations

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="accumulated"></param>
    /// <returns></returns>
    public Resource COUNT(Resource value, Resource accumulated, HashSet<string> distinctSet)
    {
        if (distinctSet != null)
        {
            if (distinctSet.Contains(value!))
                return accumulated;
            else
                distinctSet.Add(value!);
        }

        if (accumulated == Resource.Empty)
        {
            if (value != null && value.IsBound())
                return Literal.From(1);
            else
                return Literal.From(0);
        }

        if (value != null && value.IsBound())
            return Operators.Add(accumulated, Literal.From(1));

        return accumulated;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rowKey"></param>
    /// <param name="accumulated"></param>
    /// <param name="distinctSet"></param>
    /// <returns></returns>
    public Resource COUNTALL(string rowKey, Resource accumulated, HashSet<string> distinctSet)
    {
        if (distinctSet != null)
        {
            if (distinctSet.Contains(rowKey))
                return accumulated;
            else
                distinctSet.Add(rowKey);
        }

        if (accumulated == Resource.Empty)
        {
            if (rowKey.All(c => c == '|'))
                return Literal.From(0);
            else
                return Literal.From(1);
        }

        if (!rowKey.All(c => c == '|'))
            return Operators.Add(accumulated, Literal.From(1));

        return accumulated;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="accumulated"></param>
    /// <returns></returns>
    public Resource SUM(Resource value, Resource accumulated, HashSet<string> distinctSet)
    {
        if (distinctSet != null)
        {
            if (distinctSet.Contains(value!))
                return accumulated;
            else
                distinctSet.Add(value!);
        }

        if (accumulated == Resource.Empty)
            return value;

        return Operators.Add(accumulated, value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="accumulated"></param>
    /// <returns></returns>
    public Resource MIN(Resource value, Resource accumulated, HashSet<string> distinctSet)
    {
        if (distinctSet != null)
        {
            if (distinctSet.Contains(value!))
                return accumulated;
            else
                distinctSet.Add(value!);
        }

        if (accumulated == Resource.Empty)
            return value;

        var lessThanLiteral = Operators.LessThan(value, accumulated) as Literal;

        if (lessThanLiteral == Resource.Error)
            return Resource.Error;

        bool lessThan;
        if (!lessThanLiteral!.TryBool(out lessThan))
            return Resource.Error;

        if (lessThan)
            return value;

        return accumulated;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="accumulated"></param>
    /// <returns></returns>
    public Resource MAX(Resource value, Resource accumulated, HashSet<string> distinctSet)
    {
        if (distinctSet != null)
        {
            if (distinctSet.Contains(value!))
                return accumulated;
            else
                distinctSet.Add(value!);
        }

        if (accumulated == Resource.Empty)
            return value;

        var greaterThanLiteral = Operators.GreaterThan(value, accumulated) as Literal;

        if (greaterThanLiteral == Resource.Error)
            return Resource.Error;

        bool greaterThan;
        if (!greaterThanLiteral!.TryBool(out greaterThan))
            return Resource.Error;

        if (greaterThan)
            return value;

        return accumulated;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="accumulated"></param>
    /// <returns></returns>
    public Resource AVG(Resource value, Resource accumulated, HashSet<string> distinctSet)
    {
        if (distinctSet != null)
        {
            if (distinctSet.Contains(value!))
                return accumulated;
            else
                distinctSet.Add(value!);
        }

        if (accumulated == Resource.Empty)
            return value;

        return Operators.Divide(Operators.Add(value, accumulated), Literal.From(2));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="accumulated"></param>
    /// <returns></returns>
    public Resource SAMPLE(Resource value, Resource accumulated, HashSet<string> distinctSet)
    {
        if (distinctSet != null)
        {
            if (distinctSet.Contains(value!))
                return accumulated;
            else
                distinctSet.Add(value!);
        }

        if (accumulated == Resource.Empty)
            return value;

        return accumulated;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="accumulated"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public Resource GROUP_CONCAT(Resource value, Resource accumulated, Resource separator, HashSet<string> distinctSet)
    {
        if (distinctSet != null)
        {
            if (distinctSet.Contains(value!))
                return accumulated;
            else
                distinctSet.Add(value!);
        }

        if (accumulated == Resource.Empty)
            return value;

        return Literal.From(String.Concat(accumulated.Value, separator.Value, value.Value));
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flags"></param>
    /// <returns></returns>
    private static RegexOptions ParseOptions(string flags)
    {
        var options = RegexOptions.CultureInvariant;

        foreach (var o in flags)
        {
            switch (o)
            {
                case 's':
                    options |= RegexOptions.Singleline;
                    break;
                case 'm':
                    options |= RegexOptions.Multiline;
                    break;
                case 'i':
                    options |= RegexOptions.IgnoreCase;
                    break;
                case 'x':
                    options |= RegexOptions.IgnorePatternWhitespace;
                    break;
            }
        }

        return options;
    }
}
