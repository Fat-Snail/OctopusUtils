using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JiebaNet.Segmenter.Common
{
    public static class Extensions
    {
        private static readonly Regex RegexDigits = new Regex(@"\d+", RegexOptions.Compiled);
        private static readonly Regex RegexNewline = new Regex("(\r\n|\n|\r)", RegexOptions.Compiled);

        #region Objects

        public static Boolean IsNull(this Object obj)
        {
            return obj == null;
        }

        public static Boolean IsNotNull(this Object obj)
        {
            return obj != null;
        }

        #endregion


        #region Enumerable

        public static Boolean IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            return (enumerable == null) || !enumerable.Any();
        }

        public static Boolean IsNotEmpty<T>(this IEnumerable<T> enumerable)
        {
            return (enumerable != null) && enumerable.Any();
        }

        public static TValue GetValueOrDefaultEx<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
        {
            return d.ContainsKey(key) ? d[key] : default(TValue);
        }

        public static TValue GetDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return defaultValue;
        }

        public static void Update<TKey, TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> other)
        {
            foreach (var key in other.Keys)
            {
                dict[key] = other[key];
            }
        }

        #endregion

        #region String & Text

        public static String Left(this String s, Int32 endIndex)
        {
            if (String.IsNullOrEmpty(s))
            {
                return s;
            }

            return s.Substring(0, endIndex);
        }

        public static String Right(this String s, Int32 startIndex)
        {
            if (String.IsNullOrEmpty(s))
            {
                return s;
            }


            return s.Substring(startIndex);
        }

        public static String Sub(this String s, Int32 startIndex, Int32 endIndex)
        {
            return s.Substring(startIndex, endIndex - startIndex);
        }

        public static Boolean IsInt32(this String s)
        {
            return RegexDigits.IsMatch(s);
        }

        public static String[] SplitLines(this String s)
        {
            return RegexNewline.Split(s);
        }

        public static String Join(this IEnumerable<String> inputs, String separator = ", ")
        {
            return String.Join(separator, inputs);
        }

        public static IEnumerable<String> SubGroupValues(this GroupCollection groups)
        {
            var result = from Group g in groups
                         select g.Value;
            return result.Skip(1);
        }

        #endregion

        #region Conversion

        public static Int32 ToInt32(this Char ch)
        {
            return ch;
        }

        public static Char ToChar(this Int32 i)
        {
            return (Char)i;
        }

        #endregion
    }
}