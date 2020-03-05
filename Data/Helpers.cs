using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Common;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace Data {
    public static class Helpers {
        [NotNull]
        public static string CleanAdressString([NotNull] string s)
        {
            var s2 = s.ToLower(CultureInfo.InvariantCulture).Replace("/", "").Replace("-", "").Replace("ú", "u");
            return s2.Replace(" ", "").Trim();
        }

        public static DateTime GetDateTime([CanBeNull] object o)
        {
            if (o == null) {
                return DateTime.MinValue;
            }

            if (o is DateTime time) {
                return time;
            }

            if (o is string s && s.Split('.').Length > 1) {
                return DateTime.Parse(s, CultureInfo.InvariantCulture);
            }

            var d = GetDouble(o);
            if (d != null) {
                return DateTime.FromOADate(d.Value);
            }

            throw new Exception("Unknown date");
        }

        [CanBeNull]
        public static double? GetDouble([CanBeNull] object o)
        {
            if (o == null) {
                return null;
            }

            var i = o as double?;
            if (i != null) {
                return i;
            }

            if (o is string s) {
                if (s.Length == 0) {
                    return null;
                }

                return Convert.ToDouble(s, CultureInfo.InvariantCulture);
            }

            string typename = o.GetType().FullName;
            throw new Exception("Unknown type: " + typename);
        }

        [NotNull]
        public static string GetElapsedTimeString([NotNull] Stopwatch sw)
        {
            var ts = sw.Elapsed;
            return ts.ToString("g", CultureInfo.InvariantCulture) + " (Total " + ts.TotalMilliseconds + " ms)";
        }

        [CanBeNull]
        public static int? GetInt([CanBeNull] object o)
        {
            if (o == null) {
                return null;
            }

            var i = o as int?;
            if (i != null) {
                if (i == -2146826246) {
                    throw new FlaException("wrong id");
                }

                return i;
            }

            if (o is string s) {
                s = s.Trim();
                if (s.Length == 0) {
                    return null;
                }

                int i1 = Convert.ToInt32(s, CultureInfo.InvariantCulture);
                if (i1 == -2146826246) {
                    throw new FlaException("wrong id");
                }

                return i1;
            }

            var d = o as double?;
            if (d != null && Math.Abs(Math.Round(d.Value) - d.Value) < 0.000001) {
                int i2 = (int)d.Value;
                if (i2 == -2146826246) {
                    throw new FlaException("wrong id");
                }

                return i2;
            }

            if (o is ExcelErrorValue _) {
                return null;
            }

            throw new Exception("Unknown type: " + o.GetType().FullName);
        }

        public static int GetIntNoNull([CanBeNull] object o)
        {
            if (o == null) {
                throw new Exception("o was null");
            }

            var i = o as int?;
            if (i != null) {
                return i.Value;
            }

            if (o is string s) {
                s = s.Trim();
                if (s.Length == 0) {
                    throw new Exception("o was empty");
                }

                return Convert.ToInt32(s, CultureInfo.InvariantCulture);
            }

            var d = o as double?;
            if (d != null && Math.Abs(Math.Round(d.Value) - d.Value) < 0.000001) {
                return (int)d.Value;
            }

            throw new Exception("Unknown type");
        }

        public static double GetNoNullDouble([CanBeNull] object o)
        {
            if (o == null) {
                throw new Exception("was null");
            }

            var i = o as double?;
            if (i != null) {
                return i.Value;
            }

            if (o is string s) {
                if (s.Length == 0) {
                    throw new Exception("Was null");
                }

                return Convert.ToDouble(s, CultureInfo.InvariantCulture);
            }

            throw new Exception("Unknown type");
        }

        [CanBeNull]
        public static string GetString([CanBeNull] object o)
        {
            if (o == null) {
                return null;
            }

            if (o.GetType().FullName == typeof(double).FullName) {
                return o.ToString();
            }

            string s = (string)o;
            s = s.Trim();
            while (s.Contains("  ")) {
                s = s.Replace("  ", " ");
            }

            return s;
        }

        [NotNull]
        public static string GetStringNotNull([CanBeNull] object o)
        {
            if (o == null) {
                throw new Exception("String was null");
            }

            if (o.GetType().FullName == typeof(double).FullName) {
                return o.ToString();
            }

            return (string)o;
        }

        [NotNull]
        public static Dictionary<string, T> MakeSortedDictionary<T>([NotNull] Dictionary<string, T> dict)
        {
            var l = dict.OrderBy(key => key.Key);
            dict = l.ToDictionary(keyItem => keyItem.Key, valueItem => valueItem.Value);
            return dict;
        }
    }
}