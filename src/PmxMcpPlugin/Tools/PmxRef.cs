using System;
using System.Collections.Generic;
using System.Reflection;
using PEPlugin;
using PEPlugin.Pmx;
using PEPlugin.SDX;

namespace PmxMcp
{
    /// <summary>
    /// Lookup and conversion helpers shared by the element tools.
    ///
    /// Two things are worth knowing:
    ///
    /// * Most PMX elements are referenced by index. Bones, materials, morphs, nodes,
    ///   bodies and joints also have names, so those accept either.  Vertices and faces
    ///   have no names and take an index only.
    /// * A bone's local axis (LocalX / LocalZ) is not on the IPXBone interface.  It lives
    ///   on the concrete class behind it, which PEPlugin keeps internal, so it is reached
    ///   by reflection.  Everything else is plain interface access.
    /// </summary>
    internal static class PmxRef
    {
        public static object Vec2(V2 v)
        {
            if (v == null) return null;
            return new object[] { v.X, v.Y };
        }

        public static object Quat(Q q)
        {
            if (q == null) return null;
            return new object[] { q.X, q.Y, q.Z, q.W };
        }

        public static V3 V3Of(float[] a)
        {
            return new V3(a[0], a[1], a[2]);
        }

        public static V4 V4Of(float[] a)
        {
            return new V4(a[0], a[1], a[2], a[3]);
        }

        public static V2 V2Of(float[] a)
        {
            return new V2(a[0], a[1]);
        }

        public static Q QOf(float[] a)
        {
            return new Q(a[0], a[1], a[2], a[3]);
        }

        /// <summary>The builder PMX Editor uses to create new elements.</summary>
        public static IPXPmxBuilder Builder
        {
            get { return PEStaticBuilder.Pmx; }
        }

        // ---- index lookup -----------------------------------------------------------

        public delegate string NameOf(int index);

        /// <summary>
        /// Resolves an element from an "index" argument, or from a "name" argument when the
        /// element kind has names.  Throws a tool error naming the kind when neither works.
        /// </summary>
        public static int Resolve(Dictionary<string, object> args, string indexKey, string nameKey,
                                  int count, string kind, NameOf nameOf)
        {
            if (Json.Has(args, indexKey))
            {
                int index = Json.Int(args, indexKey, -1);
                if (index < 0 || index >= count)
                {
                    throw new McpToolException(
                        kind + " index " + index + " is out of range (0.." + (count - 1) + ")");
                }
                return index;
            }

            if (nameOf != null && Json.Has(args, nameKey))
            {
                string name = Json.Str(args, nameKey, "");
                for (int i = 0; i < count; i++)
                {
                    if (nameOf(i) == name) return i;
                }
                throw new McpToolException("no " + kind + " named " + name);
            }

            if (nameOf == null)
            {
                throw new McpToolException("pass " + indexKey + " to identify the " + kind);
            }
            throw new McpToolException("pass either " + indexKey + " or " + nameKey +
                                       " to identify the " + kind);
        }

        public static int ResolveMorph(IPXPmx pmx, Dictionary<string, object> args)
        {
            return Resolve(args, "index", "name", pmx.Morph.Count, "morph",
                delegate(int i) { return pmx.Morph[i].Name; });
        }

        public static int ResolveNode(IPXPmx pmx, Dictionary<string, object> args)
        {
            return Resolve(args, "index", "name", pmx.Node.Count, "node",
                delegate(int i) { return pmx.Node[i].Name; });
        }

        public static int ResolveBody(IPXPmx pmx, Dictionary<string, object> args)
        {
            return Resolve(args, "index", "name", pmx.Body.Count, "rigid body",
                delegate(int i) { return pmx.Body[i].Name; });
        }

        public static int ResolveJoint(IPXPmx pmx, Dictionary<string, object> args)
        {
            return Resolve(args, "index", "name", pmx.Joint.Count, "joint",
                delegate(int i) { return pmx.Joint[i].Name; });
        }

        public static int ResolveSoftBody(IPXPmx pmx, Dictionary<string, object> args)
        {
            return Resolve(args, "index", "name", pmx.SoftBody.Count, "soft body",
                delegate(int i) { return pmx.SoftBody[i].Name; });
        }

        public static int ResolveVertex(IPXPmx pmx, Dictionary<string, object> args)
        {
            return Resolve(args, "index", null, pmx.Vertex.Count, "vertex", null);
        }

        // ---- reading a reference out of an argument ---------------------------------

        /// <summary>
        /// A bone named by "key" (index) or "key_name".  Sets <paramref name="given"/> when
        /// either was present, so callers can tell "not passed" from "passed as none".
        /// An index of -1 or an empty name means "none".
        /// </summary>
        public static IPXBone BoneArg(IPXPmx pmx, Dictionary<string, object> args, string key, out bool given)
        {
            given = false;
            if (Json.Has(args, key))
            {
                given = true;
                int i = Json.Int(args, key, -1);
                if (i < 0) return null;
                if (i >= pmx.Bone.Count)
                {
                    throw new McpToolException("bone index " + i + " is out of range");
                }
                return pmx.Bone[i];
            }
            if (Json.Has(args, key + "_name"))
            {
                given = true;
                string name = Json.Str(args, key + "_name", "");
                if (name.Length == 0) return null;
                for (int i = 0; i < pmx.Bone.Count; i++)
                {
                    if (pmx.Bone[i].Name == name) return pmx.Bone[i];
                }
                throw new McpToolException("no bone named " + name);
            }
            return null;
        }

        public static IPXMaterial MaterialArg(IPXPmx pmx, Dictionary<string, object> args, string key, out bool given)
        {
            given = false;
            if (!Json.Has(args, key)) return null;
            given = true;
            int i = Json.Int(args, key, -1);
            if (i < 0) return null;
            if (i >= pmx.Material.Count) throw new McpToolException("material index " + i + " is out of range");
            return pmx.Material[i];
        }

        public static IPXBody BodyArg(IPXPmx pmx, Dictionary<string, object> args, string key, out bool given)
        {
            given = false;
            if (!Json.Has(args, key)) return null;
            given = true;
            int i = Json.Int(args, key, -1);
            if (i < 0) return null;
            if (i >= pmx.Body.Count) throw new McpToolException("rigid body index " + i + " is out of range");
            return pmx.Body[i];
        }

        public static IPXVertex VertexAt(IPXPmx pmx, int i)
        {
            if (i < 0 || i >= pmx.Vertex.Count)
            {
                throw new McpToolException("vertex index " + i + " is out of range (0.." + (pmx.Vertex.Count - 1) + ")");
            }
            return pmx.Vertex[i];
        }

        // ---- the local axis, which the interface does not expose --------------------

        private const BindingFlags Any =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// The local frame axes live on the concrete bone as plain fields (LocalX, LocalY,
        /// LocalZ) of SlimDX's Vector3, not on IPXBone and not as V3. Both shapes are
        /// handled so this keeps working if a later build promotes them to properties.
        /// </summary>
        private static object LocalMemberGet(IPXBone bone, string which, out System.Type kind)
        {
            kind = null;
            if (bone == null) return null;

            PropertyInfo p = bone.GetType().GetProperty(which, Any);
            if (p != null && p.CanRead)
            {
                kind = p.PropertyType;
                return p.GetValue(bone, null);
            }
            FieldInfo f = bone.GetType().GetField(which, Any);
            if (f != null)
            {
                kind = f.FieldType;
                return f.GetValue(bone);
            }
            return null;
        }

        private static bool LocalMemberSet(IPXBone bone, string which, object value)
        {
            if (bone == null) return false;

            PropertyInfo p = bone.GetType().GetProperty(which, Any);
            if (p != null && p.CanWrite) { p.SetValue(bone, value, null); return true; }
            FieldInfo f = bone.GetType().GetField(which, Any);
            if (f != null) { f.SetValue(bone, value); return true; }
            return false;
        }

        /// <summary>Reads x, y and z off any vector-shaped value, field or property.</summary>
        private static bool Components(object v, out float x, out float y, out float z)
        {
            x = y = z = 0;
            if (v == null) return false;
            System.Type t = v.GetType();
            if (!Component(t, v, "X", ref x)) return false;
            if (!Component(t, v, "Y", ref y)) return false;
            if (!Component(t, v, "Z", ref z)) return false;
            return true;
        }

        private static bool Component(System.Type t, object v, string name, ref float into)
        {
            PropertyInfo p = t.GetProperty(name, Any);
            if (p != null && p.CanRead) { into = System.Convert.ToSingle(p.GetValue(v, null)); return true; }
            FieldInfo f = t.GetField(name, Any);
            if (f != null) { into = System.Convert.ToSingle(f.GetValue(v)); return true; }
            return false;
        }

        /// <summary>Builds a value of the axis member's own type from three numbers.</summary>
        private static object MakeVector(System.Type kind, float x, float y, float z)
        {
            if (kind == typeof(V3)) return new V3(x, y, z);

            object v = System.Activator.CreateInstance(kind);
            SetComponent(kind, ref v, "X", x);
            SetComponent(kind, ref v, "Y", y);
            SetComponent(kind, ref v, "Z", z);
            return v;
        }

        private static void SetComponent(System.Type t, ref object v, string name, float value)
        {
            PropertyInfo p = t.GetProperty(name, Any);
            if (p != null && p.CanWrite) { p.SetValue(v, value, null); return; }
            FieldInfo f = t.GetField(name, Any);
            if (f != null) f.SetValue(v, value);
        }

        /// <summary>The bone's local X, Y or Z axis, or null when this build does not carry it.</summary>
        public static V3 LocalAxis(IPXBone bone, string which)
        {
            System.Type kind;
            object v = LocalMemberGet(bone, which, out kind);
            float x, y, z;
            if (!Components(v, out x, out y, out z)) return null;
            return new V3(x, y, z);
        }

        public static bool SetLocalAxis(IPXBone bone, string which, V3 value)
        {
            System.Type kind;
            LocalMemberGet(bone, which, out kind);
            if (kind == null) return false;
            return LocalMemberSet(bone, which, MakeVector(kind, value.X, value.Y, value.Z));
        }

        /// <summary>
        /// Writes a whole local frame from X and Z, deriving Y the way the PMX specification
        /// says an editor should: Y = Z x X, then Z is remade as X x Y so the three are
        /// orthogonal even when the caller's X and Z were not.
        /// </summary>
        public static bool SetLocalFrame(IPXBone bone, V3 ax, V3 az)
        {
            V3 ay = Cross(az, ax);
            V3 z2 = Cross(ax, ay);
            return SetLocalAxis(bone, "LocalX", ax)
                 & SetLocalAxis(bone, "LocalY", ay)
                 & SetLocalAxis(bone, "LocalZ", z2);
        }

        public static V3 Cross(V3 a, V3 b)
        {
            return new V3(a.Y * b.Z - a.Z * b.Y,
                          a.Z * b.X - a.X * b.Z,
                          a.X * b.Y - a.Y * b.X);
        }

        // ---- enums ------------------------------------------------------------------

        /// <summary>Parses an enum by name, case-insensitively, listing the choices on failure.</summary>
        public static T EnumArg<T>(Dictionary<string, object> args, string key, T fallback)
        {
            if (!Json.Has(args, key)) return fallback;
            string text = Json.Str(args, key, "");
            try
            {
                return (T)System.Enum.Parse(typeof(T), text, true);
            }
            catch (Exception)
            {
                throw new McpToolException(key + " must be one of: " +
                    string.Join(", ", System.Enum.GetNames(typeof(T))) + " (got " + text + ")");
            }
        }

        /// <summary>The names of an enum, for a schema description.</summary>
        public static string Choices(Type t)
        {
            return string.Join(", ", System.Enum.GetNames(t));
        }
    }
}
