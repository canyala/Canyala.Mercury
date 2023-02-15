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

namespace Canyala.Mercury.Core;

/// <summary>
/// Provides a web uri representation
/// </summary>
public class Uri
{
    public string Scheme { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string Fragment { get; set; } = string.Empty;

    public override string ToString()
    {
        var result = new StringBuilder();

        result
            .Append(Scheme.IsEmpty() ? "" : Scheme + ':')
            .Append(Authority.IsEmpty() ? "" : "//" + Authority)
            .Append(Path)
            .Append(Query.IsEmpty() ? "" : '?' + Query)
            .Append(Fragment.IsEmpty() ? "" : '#' + Fragment);

        return result.ToString();
    }

    public string[] PathParts
        { get { return Path.IsEmpty() ? new string[0] : Path.Split('/'); } }

    public static implicit operator string(Uri uri)
    {
        return uri.ToString();
    }

    public static implicit operator Uri(string text)
    {
        return Uri.Parse(text);
    }

    public static Uri Parse(string uri)
    {
        string scheme, authority, path, query, fragment;
        uri = GetScheme(uri, out scheme);
        uri = GetAuthority(uri, out authority);
        uri = GetPath(uri, out path);
        uri = GetQuery(uri, out query);
        uri = GetFragment(uri, out fragment);

        return new Uri { Authority = authority, Fragment = fragment, Path = path, Query = query, Scheme = scheme };
    }

    public static Uri From(string? scheme, string? authority, string? path, string? query, string? fragment)
    {
        return new Uri
        {
            Authority = authority ?? string.Empty,
            Fragment = fragment ?? string.Empty,
            Path = path ?? string.Empty,
            Query = query ?? string.Empty,
            Scheme = scheme ?? string.Empty
        };
    }

    private static string GetScheme(string uri, out string scheme)
    {
        scheme = null!;
        for (int i = 0; i < uri.Length; i++)
        {
            char c = uri[i];
            if (!(char.IsLetterOrDigit(c) || ".-+".Contains(c)))
                if (c == ':')
                {
                    scheme = uri.Substring(0, i);
                    return uri.Substring(i + 1);
                }
        }

        return uri;
    }

    private static string GetAuthority(string uri, out string authority)
    {
        authority = null!;

        if (!uri.StartsWith(@"//"))
            return uri;

        uri = uri.Substring(2);

        int end = uri.IndexOfAny(Seq.Array('/', '?', '#'));

        if (end >= 0)
        {
            authority = uri.Substring(0, end);
            return uri.Substring(end);
        }
        else
        {
            authority = uri;
            return "";
        }
    }

    private static string GetPath(string uri, out string path)
    {
        path = "";

        int end = uri.IndexOfAny(Seq.Array('?', '#'));

        if (end >= 0)
        {
            path = uri.Substring(0, end);
            return uri.Substring(end);
        }

        path = uri;
        return "";
    }

    private static string GetQuery(string uri, out string query)
    {
        query = null!;

        if (!uri.StartsWith("?"))
            return uri;

        uri = uri.Substring(1);

        int end = uri.IndexOfAny(Seq.Array('#'));

        if (end >= 0)
        {
            query = uri.Substring(0, end);
            return uri.Substring(end);
        }

        query = uri;
        return "";
    }

    private static string GetFragment(string uri, out string fragment)
    {
        fragment = null!;

        if (!uri.StartsWith("#"))
            return uri;

        fragment = uri.Substring(1);
        return "";
    }
}
