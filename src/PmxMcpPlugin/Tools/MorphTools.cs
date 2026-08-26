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
