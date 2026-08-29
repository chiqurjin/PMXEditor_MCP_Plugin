using System.Collections.Generic;
using PEPlugin.Pmx;
using PEPlugin.SDX;

namespace PmxMcp
{
    /// <summary>
    /// Vertices and faces.
    ///
    /// A model has tens of thousands of vertices, so listing is always paged and the
    /// default page is small.  Faces are read through the material that owns them,
    /// which is also how PMX stores them.
    /// </summary>
    internal static class VertexTools
    {
        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "list_vertices",
                "Lists vertices with position, normal, UV and bone weights. Paged; models have "
                    + "tens of thousands of vertices, so ask for a window rather than all of them.",
                Schema.Object(Json.Obj(
                    "offset", Schema.Int("First vertex index to return (default 0)"),
                    "limit", Schema.Int("How many vertices to return (default 50, max 1000)"))),
                true,
                delegate(Dictionary<string, object> args) { return ListVertices(editor, args); });

            registry.Add(
                "get_vertex",
                "Full detail of one vertex: position, normal, UV, additional UVs, the bones that "
                    + "deform it with their weights, the edge scale and any SDEF data.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Vertex index")),
                    "index"),
                true,
                delegate(Dictionary<string, object> args) { return GetVertex(editor, args); });

            registry.Add(
                "set_vertex",
                "Edits one vertex. Only the fields you pass are changed. Weights are not "
                    + "normalised for you: pass the set you want.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Vertex index"),
                    "position", Schema.NumArray("Position [x, y, z]", 3),
                    "normal", Schema.NumArray("Normal [x, y, z]", 3),
                    "uv", Schema.NumArray("Texture coordinate [u, v]", 2),
                    "uva1", Schema.NumArray("Additional UV 1 [x, y, z, w]", 4),
                    "uva2", Schema.NumArray("Additional UV 2 [x, y, z, w]", 4),
                    "uva3", Schema.NumArray("Additional UV 3 [x, y, z, w]", 4),
                    "uva4", Schema.NumArray("Additional UV 4 [x, y, z, w]", 4),
                    "bone1", Schema.Int("Bone index for slot 1, or -1 for none"),
                    "bone2", Schema.Int("Bone index for slot 2, or -1 for none"),
                    "bone3", Schema.Int("Bone index for slot 3, or -1 for none"),
                    "bone4", Schema.Int("Bone index for slot 4, or -1 for none"),
                    "weight1", Schema.Num("Weight for slot 1"),
                    "weight2", Schema.Num("Weight for slot 2"),
                    "weight3", Schema.Num("Weight for slot 3"),
                    "weight4", Schema.Num("Weight for slot 4"),
                    "edge_scale", Schema.Num("Per-vertex outline scale; 0 hides the outline here"),
                    "sdef", Schema.Bool("Use SDEF deformation"),
                    "qdef", Schema.Bool("Use QDEF deformation"),
                    "sdef_c", Schema.NumArray("SDEF C [x, y, z]", 3),
                    "sdef_r0", Schema.NumArray("SDEF R0 [x, y, z]", 3),
                    "sdef_r1", Schema.NumArray("SDEF R1 [x, y, z]", 3)),
                    "index"),
                false,
                delegate(Dictionary<string, object> args) { return SetVertex(editor, args); });

            registry.Add(
                "list_faces",
                "Lists the triangles of one material as vertex indices. Identify the material by "
                    + "index or name. Paged.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Material index"),
                    "name", Schema.Str("Japanese material name"),
                    "offset", Schema.Int("First triangle to return (default 0)"),
                    "limit", Schema.Int("How many triangles to return (default 200, max 1000)"))),
                true,
                delegate(Dictionary<string, object> args) { return ListFaces(editor, args); });
        }

        /// <summary>Vertex index of one of the four deform slots, or -1 when the slot is empty.</summary>
        private static int SlotIndex(IPXPmx pmx, IPXBone bone)
        {
            return PmxUtil.IndexOf(pmx.Bone, bone);
        }

        private static object Row(IPXPmx pmx, int i, IPXVertex v, bool full)
        {
            Dictionary<string, object> row = Json.Obj(
                "index", i,
                "position", PmxUtil.Vec3(v.Position),
                "normal", PmxUtil.Vec3(v.Normal),
                "uv", PmxRef.Vec2(v.UV),
                "bones", new object[]
                {
                    SlotIndex(pmx, v.Bone1), SlotIndex(pmx, v.Bone2),
                    SlotIndex(pmx, v.Bone3), SlotIndex(pmx, v.Bone4)
                },
                "weights", new object[] { v.Weight1, v.Weight2, v.Weight3, v.Weight4 },
                "edgeScale", v.EdgeScale);

            if (full)
            {
                row["uva1"] = PmxUtil.Vec4(v.UVA1);
                row["uva2"] = PmxUtil.Vec4(v.UVA2);
                row["uva3"] = PmxUtil.Vec4(v.UVA3);
                row["uva4"] = PmxUtil.Vec4(v.UVA4);
                row["sdef"] = v.SDEF;
                row["qdef"] = v.QDEF;
                if (v.SDEF)
                {
                    row["sdefC"] = PmxUtil.Vec3(v.SDEF_C);
                    row["sdefR0"] = PmxUtil.Vec3(v.SDEF_R0);
                    row["sdefR1"] = PmxUtil.Vec3(v.SDEF_R1);
                }
                row["boneNames"] = new object[]
                {
                    v.Bone1 == null ? null : v.Bone1.Name,
                    v.Bone2 == null ? null : v.Bone2.Name,
                    v.Bone3 == null ? null : v.Bone3.Name,
                    v.Bone4 == null ? null : v.Bone4.Name
                };
            }
            return row;
        }

        private static object ListVertices(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                int offset, limit;
                PmxUtil.Page(args, pmx.Vertex.Count, 50, out offset, out limit);

                List<object> rows = new List<object>();
                for (int i = offset; i < offset + limit; i++)
                {
                    rows.Add(Row(pmx, i, pmx.Vertex[i], false));
                }
                return Json.Obj(
                    "total", pmx.Vertex.Count,
                    "offset", offset,
                    "count", rows.Count,
                    "vertices", rows.ToArray());
            });
        }

        private static object GetVertex(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                int i = PmxRef.ResolveVertex(pmx, args);
                return Row(pmx, i, pmx.Vertex[i], true);
            });
        }

        private static object SetVertex(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Vertex;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Vertex;

                int i = PmxRef.ResolveVertex(pmx, args);
                IPXVertex v = pmx.Vertex[i];

                float[] a;
                a = Json.Floats(args, "position", 3); if (a != null) v.Position = PmxRef.V3Of(a);
                a = Json.Floats(args, "normal", 3); if (a != null) v.Normal = PmxRef.V3Of(a);
                a = Json.Floats(args, "uv", 2); if (a != null) v.UV = PmxRef.V2Of(a);
                a = Json.Floats(args, "uva1", 4); if (a != null) v.UVA1 = PmxRef.V4Of(a);
                a = Json.Floats(args, "uva2", 4); if (a != null) v.UVA2 = PmxRef.V4Of(a);
                a = Json.Floats(args, "uva3", 4); if (a != null) v.UVA3 = PmxRef.V4Of(a);
                a = Json.Floats(args, "uva4", 4); if (a != null) v.UVA4 = PmxRef.V4Of(a);

                bool given;
                IPXBone b;
                b = PmxRef.BoneArg(pmx, args, "bone1", out given); if (given) v.Bone1 = b;
                b = PmxRef.BoneArg(pmx, args, "bone2", out given); if (given) v.Bone2 = b;
                b = PmxRef.BoneArg(pmx, args, "bone3", out given); if (given) v.Bone3 = b;
                b = PmxRef.BoneArg(pmx, args, "bone4", out given); if (given) v.Bone4 = b;

                if (Json.Has(args, "weight1")) v.Weight1 = Json.Flt(args, "weight1", v.Weight1);
                if (Json.Has(args, "weight2")) v.Weight2 = Json.Flt(args, "weight2", v.Weight2);
                if (Json.Has(args, "weight3")) v.Weight3 = Json.Flt(args, "weight3", v.Weight3);
                if (Json.Has(args, "weight4")) v.Weight4 = Json.Flt(args, "weight4", v.Weight4);
                if (Json.Has(args, "edge_scale")) v.EdgeScale = Json.Flt(args, "edge_scale", v.EdgeScale);
                if (Json.Has(args, "sdef")) v.SDEF = Json.Bool(args, "sdef", v.SDEF);
                if (Json.Has(args, "qdef")) v.QDEF = Json.Bool(args, "qdef", v.QDEF);

                a = Json.Floats(args, "sdef_c", 3); if (a != null) v.SDEF_C = PmxRef.V3Of(a);
                a = Json.Floats(args, "sdef_r0", 3); if (a != null) v.SDEF_R0 = PmxRef.V3Of(a);
                a = Json.Floats(args, "sdef_r1", 3); if (a != null) v.SDEF_R1 = PmxRef.V3Of(a);

                change.Index = i;
                return Row(pmx, i, v, true);
            });
        }

        private static object ListFaces(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                int mi = PmxUtil.ResolveMaterial(pmx, args);
                IPXMaterial material = pmx.Material[mi];

                // The face list holds vertex objects, so the index has to be looked up.
                // Doing that against the whole vertex list for every corner would be
                // quadratic, so a map is built once.
                Dictionary<IPXVertex, int> at = new Dictionary<IPXVertex, int>();
                for (int i = 0; i < pmx.Vertex.Count; i++) at[pmx.Vertex[i]] = i;

                int offset, limit;
                PmxUtil.Page(args, material.Faces.Count, 200, out offset, out limit);

                List<object> rows = new List<object>();
                for (int i = offset; i < offset + limit; i++)
                {
                    IPXFace f = material.Faces[i];
                    rows.Add(new object[] { At(at, f.Vertex1), At(at, f.Vertex2), At(at, f.Vertex3) });
                }
                return Json.Obj(
                    "materialIndex", mi,
                    "materialName", material.Name,
                    "total", material.Faces.Count,
                    "offset", offset,
                    "count", rows.Count,
                    "faces", rows.ToArray());
            });
        }

        private static int At(Dictionary<IPXVertex, int> map, IPXVertex v)
        {
            int i;
            if (v != null && map.TryGetValue(v, out i)) return i;
            return -1;
        }
    }
}
