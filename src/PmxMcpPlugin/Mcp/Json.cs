using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace PmxMcp
{
    /// <summary>
    /// Thin JSON layer over JavaScriptSerializer (part of the framework, so the plugin
    /// ships as a single DLL with no third-party dependency to resolve at load time).
    /// </summary>
    internal static class Json
    {
        private static JavaScriptSerializer Serializer()
        {
            JavaScriptSerializer s = new JavaScriptSerializer();
            s.MaxJsonLength = int.MaxValue;
            s.RecursionLimit = 500;
            return s;
        }

        public static string Stringify(object value)
        {
            return Serializer().Serialize(value);
        }

        public static Dictionary<string, object> Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return new Dictionary<string, object>();
            Dictionary<string, object> d = Serializer().Deserialize<Dictionary<string, object>>(text);
            return d == null ? new Dictionary<string, object>() : d;
        }

        /// <summary>Builds a dictionary from alternating key/value arguments.</summary>
        public static Dictionary<string, object> Obj(params object[] keyValues)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            for (int i = 0; i + 1 < keyValues.Length; i += 2)
            {
                d[Convert.ToString(keyValues[i], CultureInfo.InvariantCulture)] = keyValues[i + 1];
            }
            return d;
        }

        public static bool Has(Dictionary<string, object> d, string key)
        {
            return d != null && d.ContainsKey(key) && d[key] != null;
        }

        public static object Raw(Dictionary<string, object> d, string key)
        {
            return Has(d, key) ? d[key] : null;
        }

        public static string Str(Dictionary<string, object> d, string key, string fallback)
        {
            object v = Raw(d, key);
            if (v == null) return fallback;
            string s = v as string;
            return s != null ? s : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public static int Int(Dictionary<string, object> d, string key, int fallback)
        {
            object v = Raw(d, key);
            if (v == null) return fallback;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        public static float Flt(Dictionary<string, object> d, string key, float fallback)
        {
            object v = Raw(d, key);
            if (v == null) return fallback;
            try { return Convert.ToSingle(v, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        public static bool Bool(Dictionary<string, object> d, string key, bool fallback)
        {
            object v = Raw(d, key);
            if (v == null) return fallback;
            if (v is bool) return (bool)v;
            bool parsed;
            if (bool.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), out parsed)) return parsed;
            return fallback;
        }

        public static Dictionary<string, object> Sub(Dictionary<string, object> d, string key)
        {
            object v = Raw(d, key);
            Dictionary<string, object> sub = v as Dictionary<string, object>;
            return sub == null ? new Dictionary<string, object>() : sub;
        }

        public static object[] Arr(Dictionary<string, object> d, string key)
        {
            object v = Raw(d, key);
            object[] a = v as object[];
            if (a != null) return a;
            IEnumerable e = v as IEnumerable;
            if (e != null && !(v is string))
            {
                List<object> list = new List<object>();
                foreach (object o in e) list.Add(o);
                return list.ToArray();
            }
            return null;
        }

        public static int[] Ints(Dictionary<string, object> d, string key)
        {
            object[] a = Arr(d, key);
            if (a == null) return null;
            int[] result = new int[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                try { result[i] = Convert.ToInt32(a[i], CultureInfo.InvariantCulture); }
                catch { throw new McpToolException(key + "[" + i + "] is not an integer"); }
            }
            return result;
        }

        public static float[] Floats(Dictionary<string, object> d, string key, int expectedLength)
        {
            object[] a = Arr(d, key);
            if (a == null) return null;
            if (expectedLength > 0 && a.Length != expectedLength)
            {
                throw new McpToolException(key + " must be an array of " + expectedLength + " numbers");
            }
            float[] result = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                try { result[i] = Convert.ToSingle(a[i], CultureInfo.InvariantCulture); }
                catch { throw new McpToolException(key + "[" + i + "] is not a number"); }
            }
            return result;
        }
    }
}
