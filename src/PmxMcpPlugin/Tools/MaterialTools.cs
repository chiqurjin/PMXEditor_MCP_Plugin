using System.Collections.Generic;
using PEPlugin.Pmx;
using PEPlugin.SDX;

namespace PmxMcp
{
    internal static class MaterialTools
    {
        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "list_materials",
                "Lists materials with their index, names, face count, colours and texture paths.",
                Schema.Object(Json.Obj(
                    "offset", Schema.Int("First material index to return (default 0)"),
                    "limit", Schema.Int("How many materials to return (default 200, max 1000)"),
                    "name_contains", Schema.Str("Only materials whose Japanese or English name contains this text"))),
                true,
                delegate(Dictionary<string, object> args) { return ListMaterials(editor, args); });

            registry.Add(
                "set_material",
                "Edits one material. Identify it by index or name; only the fields you pass are changed. "
                    + "Colour components are 0.0-1.0.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Material index"),
                    "name", Schema.Str("Japanese material name used to find the material"),
                    "new_name", Schema.Str("New Japanese name"),
                    "new_name_en", Schema.Str("New English name"),
                    "diffuse", Schema.NumArray("Diffuse colour [r, g, b, a]", 4),
                    "specular", Schema.NumArray("Specular colour [r, g, b]", 3),
                    "ambient", Schema.NumArray("Ambient colour [r, g, b]", 3),
                    "power", Schema.Num("Specular power"),
                    "edge", Schema.Bool("Draw the outline"),
                    "edge_color", Schema.NumArray("Outline colour [r, g, b, a]", 4),
                    "edge_size", Schema.Num("Outline thickness"),
                    "both_draw", Schema.Bool("Render both faces"),
                    "texture", Schema.Str("Texture path relative to the model"),
                    "sphere", Schema.Str("Sphere map path relative to the model"),
                    "sphere_mode", Schema.Str("Sphere blend: " + Spheres),
                    "toon", Schema.Str("Toon path, or a shared name such as toon01.bmp"),
                    "shadow", Schema.Bool("Cast onto the ground shadow"),
                    "self_shadow_map", Schema.Bool("Write to the self-shadow map"),
                    "self_shadow", Schema.Bool("Receive the self-shadow"),
                    "vertex_color", Schema.Bool("Use per-vertex colour (PMX 2.1)"),
                    "primitive_type", Schema.Str("Primitive kind (PMX 2.1): " + Primitives),
                    "memo", Schema.Str("Free-text memo"))),
                false,
                delegate(Dictionary<string, object> args) { return SetMaterial(editor, args); });

            registry.Add(
                "get_material",
                "Full detail of one material, including the shadow flags and where its faces "
                    + "sit in the draw order.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Material index"),
                    "name", Schema.Str("Japanese material name"))),
                true,
                delegate(Dictionary<string, object> args) { return GetMaterial(editor, args); });

            registry.Add(
                "delete_material",
                "Deletes one material and every face that belongs to it. The vertices stay; "
                    + "run the editor's cleanup if you want them gone too.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Material index"),
                    "name", Schema.Str("Japanese material name"))),
                false,
                delegate(Dictionary<string, object> args) { return DeleteMaterial(editor, args); });
        }

        private static readonly string Spheres = PmxRef.Choices(typeof(SphereType));
        private static readonly string Primitives = PmxRef.Choices(typeof(PrimitiveType));

        /// <summary>Every field of one material, shared by get_material and set_material.</summary>
        private static object Detail(IPXPmx pmx, int index)
        {
            IPXMaterial m = pmx.Material[index];

            // The face range is where this material starts in the model's draw order,
            // which is what a renderer needs and PMX Editor does not show directly.
            int start = 0;
            for (int i = 0; i < index; i++)
            {
                start += pmx.Material[i].Faces == null ? 0 : pmx.Material[i].Faces.Count;
            }

            return Json.Obj(
                "index", index,
                "name", m.Name,
                "nameEn", m.NameE,
                "faceCount", m.Faces == null ? 0 : m.Faces.Count,
                "faceStart", start,
                "diffuse", PmxUtil.Vec4(m.Diffuse),
                "specular", PmxUtil.Vec3(m.Specular),
                "ambient", PmxUtil.Vec3(m.Ambient),
                "power", m.Power,
                "edge", m.Edge,
                "edgeColor", PmxUtil.Vec4(m.EdgeColor),
                "edgeSize", m.EdgeSize,
                "bothDraw", m.BothDraw,
                "shadow", m.Shadow,
                "selfShadowMap", m.SelfShadowMap,
                "selfShadow", m.SelfShadow,
                "vertexColor", m.VertexColor,
                "primitiveType", m.PrimitiveType.ToString(),
                "texture", m.Tex,
                "sphere", m.Sphere,
                "sphereMode", m.SphereMode.ToString(),
                "toon", m.Toon,
                "memo", m.Memo);
        }

        private static object GetMaterial(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                return Detail(pmx, PmxUtil.ResolveMaterial(pmx, args));
            });
        }

        private static object DeleteMaterial(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Material;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Material;

                int index = PmxUtil.ResolveMaterial(pmx, args);
                IPXMaterial m = pmx.Material[index];
                int faces = m.Faces == null ? 0 : m.Faces.Count;
                string name = m.Name;
                pmx.Material.RemoveAt(index);
                return Json.Obj("deleted", name, "index", index,
                                "facesRemoved", faces, "remaining", pmx.Material.Count);
            });
        }

        private static object ListMaterials(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                string filter = Json.Str(args, "name_contains", null);
                List<int> matched = new List<int>();
                for (int i = 0; i < pmx.Material.Count; i++)
                {
                    IPXMaterial material = pmx.Material[i];
                    if (filter == null || PmxUtil.Matches(material.Name, filter) || PmxUtil.Matches(material.NameE, filter))
                    {
                        matched.Add(i);
                    }
                }

                int offset, limit;
                PmxUtil.Page(args, matched.Count, 200, out offset, out limit);

                List<object> rows = new List<object>();
                for (int n = offset; n < offset + limit; n++)
                {
                    int i = matched[n];
                    IPXMaterial material = pmx.Material[i];
                    rows.Add(Json.Obj(
                        "index", i,
                        "name", material.Name,
                        "nameEn", material.NameE,
                        "faceCount", material.Faces == null ? 0 : material.Faces.Count,
                        "diffuse", PmxUtil.Vec4(material.Diffuse),
                        "specular", PmxUtil.Vec3(material.Specular),
                        "ambient", PmxUtil.Vec3(material.Ambient),
                        "power", material.Power,
                        "edge", material.Edge,
                        "edgeColor", PmxUtil.Vec4(material.EdgeColor),
                        "edgeSize", material.EdgeSize,
                        "bothDraw", material.BothDraw,
                        "texture", material.Tex,
                        "sphere", material.Sphere,
                        "toon", material.Toon,
                        "memo", material.Memo));
                }

                return Json.Obj(
                    "total", pmx.Material.Count,
                    "matched", matched.Count,
                    "offset", offset,
                    "count", rows.Count,
                    "materials", rows.ToArray());
            });
        }

        private static object SetMaterial(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Material;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Material;

                int index = PmxUtil.ResolveMaterial(pmx, args);
                IPXMaterial material = pmx.Material[index];

                if (Json.Has(args, "new_name")) material.Name = Json.Str(args, "new_name", material.Name);
                if (Json.Has(args, "new_name_en")) material.NameE = Json.Str(args, "new_name_en", material.NameE);
                if (Json.Has(args, "power")) material.Power = Json.Flt(args, "power", material.Power);
                if (Json.Has(args, "edge")) material.Edge = Json.Bool(args, "edge", material.Edge);
                if (Json.Has(args, "edge_size")) material.EdgeSize = Json.Flt(args, "edge_size", material.EdgeSize);
                if (Json.Has(args, "both_draw")) material.BothDraw = Json.Bool(args, "both_draw", material.BothDraw);
                if (Json.Has(args, "texture")) material.Tex = Json.Str(args, "texture", material.Tex);
                if (Json.Has(args, "sphere")) material.Sphere = Json.Str(args, "sphere", material.Sphere);
                if (Json.Has(args, "toon")) material.Toon = Json.Str(args, "toon", material.Toon);
                if (Json.Has(args, "memo")) material.Memo = Json.Str(args, "memo", material.Memo);
                if (Json.Has(args, "shadow")) material.Shadow = Json.Bool(args, "shadow", material.Shadow);
                if (Json.Has(args, "self_shadow_map"))
                    material.SelfShadowMap = Json.Bool(args, "self_shadow_map", material.SelfShadowMap);
                if (Json.Has(args, "self_shadow"))
                    material.SelfShadow = Json.Bool(args, "self_shadow", material.SelfShadow);
                if (Json.Has(args, "vertex_color"))
                    material.VertexColor = Json.Bool(args, "vertex_color", material.VertexColor);
                material.SphereMode = PmxRef.EnumArg(args, "sphere_mode", material.SphereMode);
                material.PrimitiveType = PmxRef.EnumArg(args, "primitive_type", material.PrimitiveType);

                float[] diffuse = Json.Floats(args, "diffuse", 4);
                if (diffuse != null) material.Diffuse = new V4(diffuse[0], diffuse[1], diffuse[2], diffuse[3]);

                float[] specular = Json.Floats(args, "specular", 3);
                if (specular != null) material.Specular = new V3(specular[0], specular[1], specular[2]);

                float[] ambient = Json.Floats(args, "ambient", 3);
                if (ambient != null) material.Ambient = new V3(ambient[0], ambient[1], ambient[2]);

                float[] edgeColor = Json.Floats(args, "edge_color", 4);
                if (edgeColor != null) material.EdgeColor = new V4(edgeColor[0], edgeColor[1], edgeColor[2], edgeColor[3]);

                change.Index = index;
                return Detail(pmx, index);
            });
        }
    }
}
