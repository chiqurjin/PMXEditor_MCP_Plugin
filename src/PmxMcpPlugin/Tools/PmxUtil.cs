using System.Collections.Generic;
using PEPlugin.Pmx;
using PEPlugin.SDX;

namespace PmxMcp
{
    /// <summary>Conversions between PMX types and plain JSON values, plus index lookup.</summary>
    internal static class PmxUtil
    {
        public static object Vec3(V3 v)
        {
            if (v == null) return null;
            return new object[] { v.X, v.Y, v.Z };
        }

        public static object Vec4(V4 v)
        {
            if (v == null) return null;
            return new object[] { v.X, v.Y, v.Z, v.W };
        }

        public static object Vec3(PEPlugin.Pmd.IPEVector3 v)
        {
            if (v == null) return null;
            return new object[] { v.X, v.Y, v.Z };
        }

        public static int IndexOf<T>(IList<T> list, T item) where T : class
        {
            if (item == null) return -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item)) return i;
            }
            return -1;
        }

        /// <summary>Resolves a bone from an "index" or "name" argument.</summary>
        public static int ResolveBone(IPXPmx pmx, Dictionary<string, object> args)
        {
            return Resolve(args, pmx.Bone.Count, "bone", delegate(string name)
            {
                for (int i = 0; i < pmx.Bone.Count; i++)
                {
                    if (pmx.Bone[i].Name == name) return i;
                }
                return -1;
            });
        }

        /// <summary>Resolves a material from an "index" or "name" argument.</summary>
        public static int ResolveMaterial(IPXPmx pmx, Dictionary<string, object> args)
        {
            return Resolve(args, pmx.Material.Count, "material", delegate(string name)
            {
                for (int i = 0; i < pmx.Material.Count; i++)
                {
                    if (pmx.Material[i].Name == name) return i;
                }
                return -1;
            });
        }

        private delegate int NameLookup(string name);

        private static int Resolve(Dictionary<string, object> args, int count, string kind, NameLookup lookup)
        {
            if (Json.Has(args, "index"))
            {
                int index = Json.Int(args, "index", -1);
                if (index < 0 || index >= count)
                {
                    throw new McpToolException(kind + " index " + index + " is out of range (0.." + (count - 1) + ")");
                }
                return index;
            }

            if (Json.Has(args, "name"))
            {
                string name = Json.Str(args, "name", "");
                int index = lookup(name);
                if (index < 0)
                {
                    throw new McpToolException("no " + kind + " named " + name);
                }
                return index;
            }

            throw new McpToolException("pass either index or name to identify the " + kind);
        }

        /// <summary>Clamps a paging window to the size of a list.</summary>
        public static void Page(Dictionary<string, object> args, int total, int defaultLimit, out int offset, out int limit)
        {
            offset = Json.Int(args, "offset", 0);
            if (offset < 0) offset = 0;
            if (offset > total) offset = total;

            limit = Json.Int(args, "limit", defaultLimit);
            if (limit <= 0) limit = defaultLimit;
            if (limit > 1000) limit = 1000;
            if (offset + limit > total) limit = total - offset;
        }

        public static bool Matches(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(needle)) return true;
            if (string.IsNullOrEmpty(haystack)) return false;
            return haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
