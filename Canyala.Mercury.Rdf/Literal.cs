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
using System.Globalization;
using System.Xml;
using Canyala.Mercury.Rdf.Internal;

namespace Canyala.Mercury.Rdf
{
    /// <summary>
    /// Provides a representation for RDF literals.
    /// </summary>
    public class Literal : Resource
    {
        private readonly string _value;
        private readonly string? _language;
        private readonly Iri? _type;

        internal Literal(string text, Namespaces namespaces)
        {
            string quote;
            text = text.TrimStartAny(out quote!, "'''", "\"\"\"", "\"", "'");
            var endQuote = FindEndQuote(quote, text);

            _value = DecodeEscape(text.Substring(0, endQuote));

            text = text.Substring(endQuote + quote.Length);

            if (text.StartsWith("^^"))
            {
                _type = new Iri(text.Substring(2), namespaces);
                return;
            }

            if (text.StartsWith("@"))
            {
                _language = text.Substring(1);
            }
        }

        internal Literal(string value, string language)
        {
            _value = value;
            _language = language;
        }

        internal Literal(string value, Iri? type)
        {
            _value = value;
            _type = type;
        }

        internal Literal(string value)
        {
            _value = value;
        }

        private static string DecodeEscape(string text)
        {
            var decoded = new StringBuilder();
            var hexBuff = new StringBuilder();
            var escaped = false;
            var hexCount = 0;

            foreach (var c in text)
            {
                if (hexCount > 0)
                {
                    hexBuff.Append(c);

                    if (--hexCount == 0)
                    {
                        int hexValue = int.Parse(hexBuff.ToString(), NumberStyles.AllowHexSpecifier);
                        var x = char.ConvertFromUtf32(hexValue);

                        decoded.Append(x);
                        hexBuff.Clear();
                    }

                    continue;
                }

                if (!escaped && c == '\\')
                {
                    escaped = true;
                    continue;
                }
                else if (escaped)
                {
                    escaped = false;

                    if (c == 'u')
                    {
                        hexCount = 4;
                        continue;
                    }

                    if (c == 'U')
                    {
                        hexCount = 8;
                        continue;
                    }


                    if (c == 't')
                    {
                        decoded.Append('\t');
                        continue;
                    }

                    if (c == 'b')
                    {
                        decoded.Append('\b');
                        continue;
                    }

                    if (c == 'n')
                    {
                        decoded.Append('\n');
                        continue;
                    }

                    if (c == 'r')
                    {
                        decoded.Append('\r');
                        continue;
                    }

                    if (c == 'f')
                    {
                        decoded.Append('\f');
                        continue;
                    }

                    if (c == '\\')
                    {
                        decoded.Append('\\');
                        continue;
                    }

                    if (c == '"')
                    {
                        decoded.Append('"');
                        continue;
                    }

                    if (c == '\'')
                    {
                        decoded.Append('\'');
                        continue;
                    }

                }

                decoded.Append(c);
            }

            return decoded.ToString();
        }

        private static new string EncodeEscape(string text)
        {
            var encoded = new StringBuilder();
            char highSurrogate = '\x00';

            foreach (char c in text)
            {
                if (highSurrogate != '\x00')
                {
                    int hexValue = char.ConvertToUtf32(highSurrogate, c);
                    encoded.AppendFormat("\\U{0}", hexValue);
                    highSurrogate = '\x00';
                    continue;
                }

                if (char.IsHighSurrogate(c))
                {
                    highSurrogate = c;
                    continue;
                }

                if (c == '\t')
                {
                    encoded.Append("\\t");
                    continue;
                }

                if (c == '\b')
                {
                    encoded.Append("\\b");
                    continue;
                }

                if (c == '\n')
                {
                    encoded.Append("\\n");
                    continue;
                }

                if (c == '\r')
                {
                    encoded.Append("\\r");
                    continue;
                }

                if (c == '\f')
                {
                    encoded.Append("\\f");
                    continue;
                }

                if (c == '\\')
                {
                    encoded.Append("\\\\");
                    continue;
                }

                if (c == '"')
                {
                    encoded.Append("\\\"");
                    continue;
                }

                if (c == '\'')
                {
                    encoded.Append("\\'");
                    continue;
                }

                encoded.Append(c);
            }

            return encoded.ToString();
        }

        private int FindEndQuote(string quote, string text)
        {
            if (quote == "\"" || quote == "'")
            {
                char q = quote[0];
                char previous = (char)0;
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == q && previous != '\\')
                        return i;

                    previous = text[i];
                }
            }
            
            return text.IndexOf(quote);
        }

        public Iri? Type 
            { get { return _type; } }

        public string Language 
            { get { return _language!; } }

        public override string Value
            { get { return _value; } }

        public override string Full
        {
            get
            {
                if (_language == null)
                {
                    if (_type == null)
                        return String.Concat("\"", EncodeEscape(_value), "\"");
                    else
                        return String.Concat("\"", EncodeEscape(_value), "\"^^", _type.Full);
                }
                else
                    return String.Concat("\"", EncodeEscape(_value), "\"@", _language);
            }
        }

        public override string Short
        {
            get
            {
                if (_language == null)
                {
                    if (_type == null)
                        return String.Concat("\"", EncodeEscape(_value), "\"");
                    else
                        return String.Concat("\"", EncodeEscape(_value), "\"^^", _type.Short);
                }
                else
                    return String.Concat("\"", EncodeEscape(_value), "\"@", _language);
            }
        }

        public override string ToString()
        {
            return Full;
        }

        public override bool Equals(object? obj)
        {
            return obj is Literal other && Full == other.Full;
        }

        public override int GetHashCode()
        {
            return Full.GetHashCode();
        }

        #region AsType converters

        public int AsInt
            { get { return int.Parse(_value); } }

        public long AsLong
            { get { return long.Parse(_value); } }

        public uint AsUInt
            { get { return uint.Parse(_value); } }

        public ulong AsULong
            { get { return ulong.Parse(_value); } }

        public float AsFloat
            { get { return float.Parse(_value, CultureInfo.InvariantCulture); } }

        public decimal AsDecimal 
            { get { return decimal.Parse(_value, CultureInfo.InvariantCulture); } }

        public double AsDouble
            { get { return double.Parse(_value, CultureInfo.InvariantCulture); } }

        public bool AsBool
            { get { return bool.Parse(_value); } }

        public string AsString
            { get { return _value; } }

        public DateTimeOffset AsDateTime
            { get { return XmlConvert.ToDateTimeOffset(_value); } }

        #endregion

        #region Try converters

        public bool TryInt(out int value)
            { return int.TryParse(_value, out value); }

        public bool TryLong(out long value)
            { return long.TryParse(_value, out value); }

        public bool TryUInt(out uint value)
            { return uint.TryParse(_value, out value); }

        public bool TryULong(out ulong value)
            { return ulong.TryParse(_value, out value); }

        public bool TryFloat(out float value)
            { return float.TryParse(_value, NumberStyles.Any, CultureInfo.InvariantCulture, out value); }

        public bool TryDecimal(out decimal value)
            { return decimal.TryParse(_value, NumberStyles.Any, CultureInfo.InvariantCulture, out value); }

        public bool TryDouble(out double value)
            { return double.TryParse(_value, NumberStyles.Any, CultureInfo.InvariantCulture, out value); }

        public bool TryBool(out bool value)
            { return bool.TryParse(_value, out value); }

        public bool TryString(out string value)
        {
            value = _value;
            return true;
        }

        public bool TryDateTime(out DateTimeOffset value)
        {
            try
            {
                value = XmlConvert.ToDateTimeOffset(_value);
                return true;
            }
            catch
            {
                value = default(DateTimeOffset);
                return false;
            }
        }

        #endregion 

        #region FromType converters

        public static Literal From(int val)
            { return new Literal(string.Concat("\"", val.ToString(CultureInfo.InvariantCulture), "\"^^", (string?)Ontologies.Xsd.integer), Ontologies.Namespaces); }

        public static Literal From(uint val)
            { return new Literal(string.Concat("\"", val.ToString(CultureInfo.InvariantCulture), "\"^^", (string?)Ontologies.Xsd.integer), Ontologies.Namespaces); }

        public static Literal From(long val)
            { return new Literal(string.Concat("\"", val.ToString(CultureInfo.InvariantCulture), "\"^^", (string?)Ontologies.Xsd.integer), Ontologies.Namespaces); }

        public static Literal From(ulong val)
            { return new Literal(string.Concat("\"", val.ToString(CultureInfo.InvariantCulture), "\"^^", (string?)Ontologies.Xsd.integer), Ontologies.Namespaces); }

        public static Literal From(float val)
            { return new Literal(string.Concat("\"", val.ToString(CultureInfo.InvariantCulture), "\"^^", (string?)Ontologies.Xsd.@float), Ontologies.Namespaces); }

        public static Literal From(double val)
            { return new Literal(string.Concat("\"", val.ToString(CultureInfo.InvariantCulture), "\"^^", (string?)Ontologies.Xsd.@double), Ontologies.Namespaces); }

        public static Literal From(decimal val)
            { return new Literal(string.Concat("\"", val.ToString(CultureInfo.InvariantCulture), "\"^^", (string?)Ontologies.Xsd.@decimal), Ontologies.Namespaces); }

        public static Literal From(bool val)
            { return new Literal(string.Concat("\"", val ? "true" : "false", "\"^^", (string?)Ontologies.Xsd.boolean), Ontologies.Namespaces); }

        public static Literal From(DateTimeOffset val)
            { return new Literal(string.Concat("\"", XmlConvert.ToString(val), "\"^^", (string?)Ontologies.Xsd.dateTime), Ontologies.Namespaces); }

        public static Literal From(TimeSpan val)
            { return new Literal(string.Concat("\"", XmlConvert.ToString(val), "\"^^", (string?)Ontologies.Xsd.dayTimeDuration), Ontologies.Namespaces); }

        public static Literal From(string val)
            { return new Literal(val); }

        #endregion

        public bool IsPlain()
            { return Type == null && Language != null; }

        public bool IsNumeric()
            { return Operators.AreNumeric(this); }
    }
}
