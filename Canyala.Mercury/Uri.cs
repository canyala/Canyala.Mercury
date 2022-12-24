//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;

namespace Canyala.Mercury
{
    /// <summary>
    /// Provides a web uri representation
    /// </summary>
    public class Uri
    {
        public string Scheme { get; set; }
        public string Authority { get; set; }
        public string Path { get; set; }
        public string Query { get; set; }
        public string Fragment { get; set; }

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

        public static Uri From(string scheme, string authority, string path, string query, string fragment)
        {
            return new Uri { Authority = authority, Fragment = fragment, Path = path, Query = query, Scheme = scheme };
        }

        private static string GetScheme(string uri, out string scheme)
        {
            scheme = null;
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
            authority = null;

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
            query = null;

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
            fragment = null;

            if (!uri.StartsWith("#"))
                return uri;

            fragment = uri.Substring(1);
            return "";
        }
    }
}
