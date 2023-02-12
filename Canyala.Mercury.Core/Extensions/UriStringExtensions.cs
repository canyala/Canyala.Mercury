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
using System.Threading.Tasks;

using Canyala.Lagoon.Core.Extensions;

namespace Canyala.Mercury.Core.Extensions;

public static class UriStringExtensions
{
    /// <summary>
    /// Resolves a relative iri string with a base iri string by combining
    /// them according to the rules of relative uri:s.
    /// </summary>
    /// <param name="relativeUri">The relative iri string</param>
    /// <param name="baseUri">The base iri string</param>
    /// <returns>The resolved iri string</returns>
    public static string ResolveRelative(this string relativeUri, string baseUri)
    {
        var r = Uri.Parse(relativeUri);
        var @base = Uri.Parse(baseUri);
        var t = new Uri();

        if (@base.Scheme.IsEmpty() || @base.Fragment != null)
            throw new ArgumentException("A base uri must conform to the rules for absolute URI references and include a scheme part but no fragment! Offending base uri => '{0}'".Args(baseUri));

        if (r.Scheme != null)
        {
            t.Scheme = r.Scheme;
            t.Authority = r.Authority;
            t.Path = RemoveDotSegments(r.Path);
            t.Query = r.Query;
        }
        else
        {
            if (r.Authority != null)
            {
                t.Authority = r.Authority;
                t.Path = RemoveDotSegments(r.Path);
                t.Query = r.Query;
            }
            else
            {
                if (r.Path == "")
                {
                    t.Path = @base.Path;
                    t.Query = r.Query != null ? r.Query : @base.Query;
                }
                else
                {
                    t.Path = r.Path.StartsWith("/") ? RemoveDotSegments(r.Path) : RemoveDotSegments(Merge(@base.Path, r.Path));
                    t.Query = r.Query;
                }

                t.Authority = @base.Authority;
            }

            t.Scheme = @base.Scheme;
        }

        t.Fragment = r.Fragment;
        return t.ToString();
    }

    /// <summary>
    /// Compares a uri with another uri and splits it into a common base prefix part and the relative URI that can be
    /// used to recreate the original uri.
    /// </summary>
    /// <param name="thisUri">The iri to split</param>
    /// <param name="otherUri">The other iri to use for the comparison</param>
    /// <param name="relativeUri">The relative uri part</param>
    /// <param name="commonBaseUri">The base uri part</param>
    /// <returns>true if a common base was found else false</returns>
    public static bool ResolveAbsolute(this string thisUri, string otherUri, out string relativeUri, out string commonBaseUri)
    {
        relativeUri = "";
        commonBaseUri = "";

        var a = Uri.Parse(thisUri);
        var o = Uri.Parse(otherUri);
        var t = new Uri();

        if (a.Scheme == o.Scheme && a.Authority == o.Authority)
        {
            string thisPath = a.Path;
            string otherPath = o.Path;
            string prefix = GetCommonSegmentPrefix(thisPath, otherPath);

            if (!prefix.IsEmpty())
            {
                string thisSuffix = thisPath.Substring(prefix.Length);
                string otherSuffix = otherPath.Substring(prefix.Length);

                if (thisSuffix.IsEmpty())
                {
                    if (otherSuffix.IsEmpty())
                        relativeUri = "";
                    else
                        relativeUri = CountSegments(otherSuffix).Generate(() => "..").Join("/");
                }
                else
                {
                    if (otherSuffix.IsEmpty())
                        relativeUri = thisSuffix;
                    else
                        relativeUri = CountSegments(otherSuffix).Generate(() => "..").Join("/") + "/" + thisSuffix;
                }
                
                commonBaseUri = Uri.From(o.Scheme, o.Authority, prefix, null, null).ToString();
                return true;
            }
        }

        return false;
    }

    private static string Merge(string basePath, string relPath)
    {
        if (basePath.IsEmpty())
            return "/" + relPath;

        int lastSlash = basePath.LastIndexOf('/');

        if (lastSlash >= 0)
            return string.Concat(basePath.Substring(0, lastSlash + 1), relPath);

        return relPath;
    }

    private static string RemoveDotSegments(string path)
    {
        string input = path;
        string output = "";

        while (!input.IsEmpty())
        {
            if (input.StartsWithAny("../", "./"))
                input = input.TrimStartAny("../", "./");
            else if (input.StartsWith("/./")) 
                input = input.Substring(2);
            else if (input.StartsWith("/.") && input.Length == 2)
                input = input.ReplacePrefix("/.", "/");
            else if (input.StartsWith("/../"))
            {
                input = input.Substring(3);
                output = TrimLastPathSegment(output);
            }
            else if ((input.StartsWith("/..") && input.Length == 3))
            {
                input = input.ReplacePrefix("/..", "/");
                output = TrimLastPathSegment(output);
            }
            else if (input == ".." || input == ".")
                input = "";
            else
            {
                input = TrimFirstPathSegment(input, out string segment);
                output += segment;
            }
        }

        return output;
    }

    private static string GetCommonSegmentPrefix(string path1, string path2)
    {
        var common = new StringBuilder();

        while (!(path1.IsEmpty() || path2.IsEmpty()))
        {
            if (path1.StartsWithAny("./"))
            {
                path1 = path1.TrimStartAny("./");
                continue;
            }
            else if (path1.StartsWith("/./"))
            {
                path1 = path1.Substring(2);
                continue;
            }
            else if (path1.StartsWith("/.") && path1.Length == 2)
            {
                path1 = path1.ReplacePrefix("/.", "/");
                continue;
            }

            if (path2.StartsWithAny("./"))
            {
                path2 = path2.TrimStartAny("./");
                continue;
            }
            else if (path2.StartsWith("/./"))
            {
                path2 = path2.Substring(2);
                continue;
            }
            else if (path2.StartsWith("/.") && path2.Length == 2)
            {
                path2 = path2.ReplacePrefix("/.", "/");
                continue;
            }

            if (path1[0] == path2[0])
            {
                common.Append(path1[0]);
                path1 = path1.Substring(1);
                path2 = path2.Substring(1);
            }
            else
                break;
        }

        return common.ToString();;
    }

    private static int CountSegments(string path)
    {
        string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Length;
    }

    private static string TrimFirstPathSegment(string path, out string segment)
    {
        segment = "";
        int start = 0;
        if (path.StartsWith("/"))
        {
            segment = "/";
            start = 1;
        }

        int end = path.IndexOf('/', start);
        if (end >= 0)
        {
            segment += path.Substring(start, end - start);
            return path.Substring(end);
        }
        else
        {
            segment = path;
            return "";
        }
    }

    private static string TrimLastPathSegment(string path)
    {
        int end = path.LastIndexOf('/');

        if (end >= 0)
            return path.Substring(0, end);

        return "";
    }
}
