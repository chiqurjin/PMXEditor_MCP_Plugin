using System.Collections.Generic;
using PEPlugin.Pmx;

namespace PmxMcp
{
    internal static class MorphTools
    {
        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "list_morphs",
                "Lists morphs with their index, names, kind (Vertex/Bone/Material/UV/Group), panel and offset count.",
                Schema.Object(Json.Obj(
                    "offset", Schema.Int("First morph index to return (default 0)"),
                    "limit", Schema.Int("How many morphs to return (default 200, max 1000)"),
                    "name_contains", Schema.Str("Only morphs whose Japanese or English name contains this text"),
                    "kind", Schema.Str("Only morphs of this kind, e.g. Vertex, Bone, Material, UV, Group"))),
                true,
                delegate(Dictionary<string, object> args) { return ListMorphs(editor, args); });

            registry.Add(
                "set_morph_name",
                "Renames one morph, found by index or current Japanese name.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Morph index"),
                    "name", Schema.Str("Current Japanese name used to find the morph"),
                    "new_name", Schema.Str("New Japanese name"),
                    "new_name_en", Schema.Str("New English name"))),
                false,
                delegate(Dictionary<string, object> args) { return SetMorphName(editor, args); });

            registry.Add(
                "get_morph",
                "Full detail of one morph, including its offsets. The shape of each offset "
                    + "depends on the kind: a vertex morph moves vertices, a bone morph moves and "
                    + "turns bones, a material morph scales or replaces material values, a group "
                    + "morph drives other morphs.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Morph index"),
                    "name", Schema.Str("Japanese morph name"),
                    "offset", Schema.Int("First offset to return (default 0)"),
                    "limit", Schema.Int("How many offsets to return (default 200, max 1000)"))),
                true,
                delegate(Dictionary<string, object> args) { return GetMorph(editor, args); });

            registry.Add(
                "set_morph",
                "Edits one morph. panel is 1 eyebrow, 2 eye, 3 mouth, 4 other. Changing kind "
                    + "clears the offsets, because offsets of one kind do not fit another.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Morph index"),
                    "name", Schema.Str("Japanese name used to find the morph"),
                    "new_name", Schema.Str("New Japanese name"),
                    "new_name_en", Schema.Str("New English name"),
                    "panel", Schema.Int("Operation panel: 1 eyebrow, 2 eye, 3 mouth, 4 other"),
                    "kind", Schema.Str("Morph kind: " + Kinds))),
                false,
                delegate(Dictionary<string, object> args) { return SetMorph(editor, args); });

            registry.Add(
                "set_morph_offsets",
                "Replaces every offset of one morph. Each entry matches the morph kind. "
                    + "Vertex: vertex index plus offset [x,y,z]. "
                    + "UV: vertex index plus offset [x,y,z,w]. "
                    + "Bone: bone index or bone_name, plus translation [x,y,z] and rotation [x,y,z,w]. "
                    + "Group: morph index plus ratio. "
                    + "Material: material index, op 0 or 1, then any of diffuse, specular, ambient, "
                    + "power, edge_size, edge_color, tex, sphere, toon. "
                    + "Impulse: body index, local, velocity [x,y,z], torque [x,y,z].",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Morph index"),
                    "name", Schema.Str("Japanese name used to find the morph"),
                    "offsets", Schema.ObjArray("The complete replacement offset list"))),
                false,
                delegate(Dictionary<string, object> args) { return SetMorphOffsets(editor, args); });

            registry.Add(
                "add_morph",
                "Adds an empty morph and returns its index. Fill it with set_morph_offsets.",
                Schema.Object(Json.Obj(
                    "new_name", Schema.Str("Japanese name"),
                    "new_name_en", Schema.Str("English name"),
                    "panel", Schema.Int("Operation panel: 1 eyebrow, 2 eye, 3 mouth, 4 other"),
                    "kind", Schema.Str("Morph kind: " + Kinds),
                    "offsets", Schema.ObjArray("Optional initial offsets, as in set_morph_offsets"))),
                false,
                delegate(Dictionary<string, object> args) { return AddMorph(editor, args); });

            registry.Add(
                "delete_morph",
                "Deletes one morph. Display frames and group morphs that referenced it are left "
                    + "pointing at nothing, so check list_nodes afterwards.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Morph index"),
                    "name", Schema.Str("Japanese morph name"))),
                false,
                delegate(Dictionary<string, object> args) { return DeleteMorph(editor, args); });
        }

        private static readonly string Kinds = PmxRef.Choices(typeof(MorphKind));

        /// <summary>One offset as JSON. The shape follows the morph kind.</summary>
        private static object OffsetRow(IPXPmx pmx, object o)
        {
            IPXVertexMorphOffset v = o as IPXVertexMorphOffset;
            if (v != null)
            {
                return Json.Obj("vertex", PmxUtil.IndexOf(pmx.Vertex, v.Vertex),
                                "offset", PmxUtil.Vec3(v.Offset));
            }
            IPXUVMorphOffset u = o as IPXUVMorphOffset;
            if (u != null)
            {
                return Json.Obj("vertex", PmxUtil.IndexOf(pmx.Vertex, u.Vertex),
                                "offset", PmxUtil.Vec4(u.Offset));
            }
            IPXBoneMorphOffset b = o as IPXBoneMorphOffset;
            if (b != null)
            {
                return Json.Obj("bone", PmxUtil.IndexOf(pmx.Bone, b.Bone),
                                "boneName", b.Bone == null ? null : b.Bone.Name,
                                "translation", PmxUtil.Vec3(b.Translation),
                                "rotation", PmxRef.Quat(b.Rotation));
            }
            IPXGroupMorphOffset g = o as IPXGroupMorphOffset;
            if (g != null)
            {
                return Json.Obj("morph", PmxUtil.IndexOf(pmx.Morph, g.Morph),
                                "morphName", g.Morph == null ? null : g.Morph.Name,
                                "ratio", g.Ratio);
            }
            IPXMaterialMorphOffset m = o as IPXMaterialMorphOffset;
            if (m != null)
            {
                return Json.Obj("material", PmxUtil.IndexOf(pmx.Material, m.Material),
                                "materialName", m.Material == null ? null : m.Material.Name,
                                "op", m.Op,
                                "diffuse", PmxUtil.Vec4(m.Diffuse),
                                "specular", PmxUtil.Vec3(m.Specular),
                                "ambient", PmxUtil.Vec3(m.Ambient),
                                "power", m.Power,
                                "edgeSize", m.EdgeSize,
                                "edgeColor", PmxUtil.Vec4(m.EdgeColor),
                                "tex", PmxUtil.Vec4(m.Tex),
                                "sphere", PmxUtil.Vec4(m.Sphere),
                                "toon", PmxUtil.Vec4(m.Toon));
            }
            IPXImpulseMorphOffset im = o as IPXImpulseMorphOffset;
            if (im != null)
            {
                return Json.Obj("body", PmxUtil.IndexOf(pmx.Body, im.Body),
                                "bodyName", im.Body == null ? null : im.Body.Name,
                                "local", im.Local,
                                "velocity", PmxUtil.Vec3(im.Velocity),
                                "torque", PmxUtil.Vec3(im.Torque));
            }
            return Json.Obj("kind", "unknown");
        }

        private static object Detail(IPXPmx pmx, int index, Dictionary<string, object> args)
        {
            IPXMorph morph = pmx.Morph[index];
            int offset, limit;
            PmxUtil.Page(args, morph.Offsets.Count, 200, out offset, out limit);

            List<object> rows = new List<object>();
            for (int i = offset; i < offset + limit; i++)
            {
                rows.Add(OffsetRow(pmx, morph.Offsets[i]));
            }
            return Json.Obj(
                "index", index,
                "name", morph.Name,
                "nameEn", morph.NameE,
                "kind", morph.Kind.ToString(),
                "panel", morph.Panel,
                "offsetCount", morph.Offsets.Count,
                "offsetsFrom", offset,
                "offsets", rows.ToArray());
        }

        private static object GetMorph(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                return Detail(pmx, PmxRef.ResolveMorph(pmx, args), args);
            });
        }

        private static object SetMorph(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Morph;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Morph;

                int index = PmxRef.ResolveMorph(pmx, args);
                IPXMorph morph = pmx.Morph[index];

                if (Json.Has(args, "new_name")) morph.Name = Json.Str(args, "new_name", morph.Name);
                if (Json.Has(args, "new_name_en")) morph.NameE = Json.Str(args, "new_name_en", morph.NameE);
                if (Json.Has(args, "panel")) morph.Panel = Json.Int(args, "panel", morph.Panel);

                MorphKind kind = PmxRef.EnumArg(args, "kind", morph.Kind);
                if (kind != morph.Kind)
                {
                    // Offsets of the old kind would be invalid under the new one.
                    morph.Offsets.Clear();
                    morph.Kind = kind;
                }

                change.Index = index;
                return Detail(pmx, index, new Dictionary<string, object>());
            });
        }

        /// <summary>Rebuilds a morph's offsets from an "offsets" argument, if it was given.</summary>
        private static void ApplyOffsets(IPXPmx pmx, IPXMorph morph, Dictionary<string, object> args)
        {
            object[] items = Json.Arr(args, "offsets");
            if (items == null) return;

            morph.Offsets.Clear();
            foreach (object raw in items)
            {
                Dictionary<string, object> it = raw as Dictionary<string, object>;
                if (it == null) throw new McpToolException("each entry of offsets must be an object");
                morph.Offsets.Add(BuildOffset(pmx, morph.Kind, it));
            }
        }

        private static IPXMorphOffset BuildOffset(IPXPmx pmx, MorphKind kind, Dictionary<string, object> it)
        {
            switch (kind)
            {
                case MorphKind.Vertex:
                {
                    IPXVertex v = PmxRef.VertexAt(pmx, Json.Int(it, "vertex", -1));
                    float[] o = Json.Floats(it, "offset", 3);
                    if (o == null) throw new McpToolException("a vertex offset needs offset [x, y, z]");
                    return PmxRef.Builder.VertexMorphOffset(v, PmxRef.V3Of(o));
                }
                case MorphKind.UV:
                case MorphKind.UVA1:
                case MorphKind.UVA2:
                case MorphKind.UVA3:
                case MorphKind.UVA4:
                {
                    IPXVertex v = PmxRef.VertexAt(pmx, Json.Int(it, "vertex", -1));
                    float[] o = Json.Floats(it, "offset", 4);
                    if (o == null) throw new McpToolException("a UV offset needs offset [x, y, z, w]");
                    return PmxRef.Builder.UVMorphOffset(v, PmxRef.V4Of(o));
                }
                case MorphKind.Bone:
                {
                    bool given;
                    IPXBone b = PmxRef.BoneArg(pmx, it, "bone", out given);
                    if (!given || b == null) throw new McpToolException("a bone offset needs bone or bone_name");
                    float[] t = Json.Floats(it, "translation", 3);
                    float[] r = Json.Floats(it, "rotation", 4);
                    return PmxRef.Builder.BoneMorphOffset(b,
                        t == null ? new PEPlugin.SDX.V3(0, 0, 0) : PmxRef.V3Of(t),
                        r == null ? new PEPlugin.SDX.Q(0, 0, 0, 1) : PmxRef.QOf(r));
                }
                case MorphKind.Group:
                {
                    int mi = Json.Int(it, "morph", -1);
                    if (mi < 0 || mi >= pmx.Morph.Count)
                    {
                        throw new McpToolException("a group offset needs a morph index in range");
                    }
                    return PmxRef.Builder.GroupMorphOffset(pmx.Morph[mi], Json.Flt(it, "ratio", 1f));
                }
                case MorphKind.Material:
                {
                    IPXMaterialMorphOffset m = PmxRef.Builder.MaterialMorphOffset();
                    bool given;
                    m.Material = PmxRef.MaterialArg(pmx, it, "material", out given);
                    m.Op = Json.Int(it, "op", 0);
                    float[] a;
                    a = Json.Floats(it, "diffuse", 4); if (a != null) m.Diffuse = PmxRef.V4Of(a);
                    a = Json.Floats(it, "specular", 3); if (a != null) m.Specular = PmxRef.V3Of(a);
                    a = Json.Floats(it, "ambient", 3); if (a != null) m.Ambient = PmxRef.V3Of(a);
                    a = Json.Floats(it, "edge_color", 4); if (a != null) m.EdgeColor = PmxRef.V4Of(a);
                    a = Json.Floats(it, "tex", 4); if (a != null) m.Tex = PmxRef.V4Of(a);
                    a = Json.Floats(it, "sphere", 4); if (a != null) m.Sphere = PmxRef.V4Of(a);
                    a = Json.Floats(it, "toon", 4); if (a != null) m.Toon = PmxRef.V4Of(a);
                    if (Json.Has(it, "power")) m.Power = Json.Flt(it, "power", m.Power);
                    if (Json.Has(it, "edge_size")) m.EdgeSize = Json.Flt(it, "edge_size", m.EdgeSize);
                    return m;
                }
                case MorphKind.Impulse:
                {
                    bool given;
                    IPXBody body = PmxRef.BodyArg(pmx, it, "body", out given);
                    if (!given || body == null) throw new McpToolException("an impulse offset needs body");
                    float[] v = Json.Floats(it, "velocity", 3);
                    float[] t = Json.Floats(it, "torque", 3);
                    return PmxRef.Builder.ImpulseMorphOffset(body, Json.Bool(it, "local", false),
                        v == null ? new PEPlugin.SDX.V3(0, 0, 0) : PmxRef.V3Of(v),
                        t == null ? new PEPlugin.SDX.V3(0, 0, 0) : PmxRef.V3Of(t));
                }
                default:
                    throw new McpToolException("morphs of kind " + kind + " cannot be filled by this tool");
            }
        }

        private static object SetMorphOffsets(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Morph;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Morph;

                int index = PmxRef.ResolveMorph(pmx, args);
                ApplyOffsets(pmx, pmx.Morph[index], args);
                change.Index = index;
                return Detail(pmx, index, new Dictionary<string, object>());
            });
        }

        private static object AddMorph(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Morph;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Morph;

                IPXMorph morph = PmxRef.Builder.Morph();
                morph.Name = Json.Str(args, "new_name", "新規モーフ");
                morph.NameE = Json.Str(args, "new_name_en", "");
                morph.Panel = Json.Int(args, "panel", 4);
                morph.Kind = PmxRef.EnumArg(args, "kind", MorphKind.Vertex);
                ApplyOffsets(pmx, morph, args);
                pmx.Morph.Add(morph);

                // 足したときは番号を渡さない(向こうの一覧はまだ増えていない)
                return Detail(pmx, pmx.Morph.Count - 1, new Dictionary<string, object>());
            });
        }

        private static object DeleteMorph(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Morph;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Morph;

                int index = PmxRef.ResolveMorph(pmx, args);
                string name = pmx.Morph[index].Name;
                pmx.Morph.RemoveAt(index);
                return Json.Obj("deleted", name, "index", index, "remaining", pmx.Morph.Count);
            });
        }

        private static object ListMorphs(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                string filter = Json.Str(args, "name_contains", null);
                string kind = Json.Str(args, "kind", null);

                List<int> matched = new List<int>();
                for (int i = 0; i < pmx.Morph.Count; i++)
                {
                    IPXMorph morph = pmx.Morph[i];
                    bool nameOk = filter == null || PmxUtil.Matches(morph.Name, filter) || PmxUtil.Matches(morph.NameE, filter);
                    bool kindOk = kind == null || string.Equals(morph.Kind.ToString(), kind, System.StringComparison.OrdinalIgnoreCase);
                    if (nameOk && kindOk) matched.Add(i);
                }

                int offset, limit;
                PmxUtil.Page(args, matched.Count, 200, out offset, out limit);

                List<object> rows = new List<object>();
                for (int n = offset; n < offset + limit; n++)
                {
                    int i = matched[n];
                    IPXMorph morph = pmx.Morph[i];
                    rows.Add(Json.Obj(
                        "index", i,
                        "name", morph.Name,
                        "nameEn", morph.NameE,
                        "kind", morph.Kind.ToString(),
                        "panel", morph.Panel,
                        "offsetCount", morph.Offsets == null ? 0 : morph.Offsets.Count));
                }

                return Json.Obj(
                    "total", pmx.Morph.Count,
                    "matched", matched.Count,
                    "offset", offset,
                    "count", rows.Count,
                    "morphs", rows.ToArray());
            });
        }

        private static object SetMorphName(Editor editor, Dictionary<string, object> args)
        {
            if (!Json.Has(args, "new_name") && !Json.Has(args, "new_name_en"))
            {
                throw new McpToolException("Pass new_name and/or new_name_en.");
            }

            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Morph;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Morph;
                change.RefreshModel = false;

                int index = -1;
                if (Json.Has(args, "index"))
                {
                    index = Json.Int(args, "index", -1);
                    if (index < 0 || index >= pmx.Morph.Count)
                    {
                        throw new McpToolException("morph index " + index + " is out of range (0.." + (pmx.Morph.Count - 1) + ")");
                    }
                }
                else if (Json.Has(args, "name"))
                {
                    string name = Json.Str(args, "name", "");
                    for (int i = 0; i < pmx.Morph.Count; i++)
                    {
                        if (pmx.Morph[i].Name == name) { index = i; break; }
                    }
                    if (index < 0) throw new McpToolException("no morph named " + name);
                }
                else
                {
                    throw new McpToolException("pass either index or name to identify the morph");
                }

                IPXMorph morph = pmx.Morph[index];
                if (Json.Has(args, "new_name")) morph.Name = Json.Str(args, "new_name", morph.Name);
                if (Json.Has(args, "new_name_en")) morph.NameE = Json.Str(args, "new_name_en", morph.NameE);

                return Json.Obj(
                    "index", index,
                    "name", morph.Name,
                    "nameEn", morph.NameE,
                    "kind", morph.Kind.ToString());
            });
        }
    }
}
