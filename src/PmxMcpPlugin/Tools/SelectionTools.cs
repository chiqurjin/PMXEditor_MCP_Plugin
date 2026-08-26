using System.Collections.Generic;
using PEPlugin.Form;
using PEPlugin.View;

namespace PmxMcp
{
    /// <summary>What the user has selected in the editor lists and in PmxView.</summary>
    internal static class SelectionTools
    {
        private const int MaxReportedIndices = 500;

        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "get_selection",
                "Reads the current selection: bones, vertices, faces and materials selected in PmxView and in the editor lists. "
                    + "Long index lists are truncated, the full size is reported as a count.",
                Schema.None(),
                true,
                delegate(Dictionary<string, object> args) { return GetSelection(editor); });

            registry.Add(
                "set_selection",
                "Replaces the selection in PmxView and the material list. Pass only the kinds you want to change.",
                Schema.Object(Json.Obj(
                    "bone_indices", Schema.IntArray("Bone indices to select"),
                    "vertex_indices", Schema.IntArray("Vertex indices to select"),
                    "face_indices", Schema.IntArray("Face indices to select"),
                    "material_indices", Schema.IntArray("Material indices to select"))),
                false,
                delegate(Dictionary<string, object> args) { return SetSelection(editor, args); });
        }

        private static object GetSelection(Editor editor)
        {
            return editor.Ui<object>(delegate
            {
                IPXPmxViewConnector view = editor.Connector.View.PmxView;
                IPEFormConnector form = editor.Connector.Form;

                return Json.Obj(
                    "bones", Cap(view.GetSelectedBoneIndices()),
                    "vertices", Cap(view.GetSelectedVertexIndices()),
                    "faces", Cap(view.GetSelectedFaceIndices()),
                    "materials", Cap(form.GetSelectedMaterialIndices()),
                    "listCursor", Json.Obj(
                        "bone", form.SelectedBoneIndex,
                        "material", form.SelectedMaterialIndex,
                        "morph", form.SelectedExpressionIndex,
                        "vertex", form.SelectedVertexIndex,
                        "rigidBody", form.SelectedBodyIndex,
                        "joint", form.SelectedJointIndex));
            });
        }

        private static object SetSelection(Editor editor, Dictionary<string, object> args)
        {
            editor.RequireWrite();

            int[] bones = Json.Ints(args, "bone_indices");
            int[] vertices = Json.Ints(args, "vertex_indices");
            int[] faces = Json.Ints(args, "face_indices");
            int[] materials = Json.Ints(args, "material_indices");

            if (bones == null && vertices == null && faces == null && materials == null)
            {
                throw new McpToolException(
                    "Pass at least one of: bone_indices, vertex_indices, face_indices, material_indices.");
            }

            return editor.Ui<object>(delegate
            {
                IPXPmxViewConnector view = editor.Connector.View.PmxView;
                IPEFormConnector form = editor.Connector.Form;

                if (bones != null) view.SetSelectedBoneIndices(bones);
                if (vertices != null) view.SetSelectedVertexIndices(vertices);
                if (faces != null) view.SetSelectedFaceIndices(faces);
                if (materials != null) form.SetSelectedMaterialIndices(materials);

                view.UpdateView();

                return Json.Obj(
                    "bones", bones == null ? -1 : bones.Length,
                    "vertices", vertices == null ? -1 : vertices.Length,
                    "faces", faces == null ? -1 : faces.Length,
                    "materials", materials == null ? -1 : materials.Length);
            });
        }

        private static Dictionary<string, object> Cap(int[] indices)
        {
            if (indices == null)
            {
                return Json.Obj("count", 0, "indices", new object[0], "truncated", false);
            }

            int take = indices.Length < MaxReportedIndices ? indices.Length : MaxReportedIndices;
            object[] shown = new object[take];
            for (int i = 0; i < take; i++) shown[i] = indices[i];

            return Json.Obj(
                "count", indices.Length,
                "indices", shown,
                "truncated", take < indices.Length);
        }
    }
}
