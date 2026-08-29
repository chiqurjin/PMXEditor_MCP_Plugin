using System.Collections.Generic;
using PEPlugin.Pmx;

namespace PmxMcp
{
    /// <summary>Model level information and the editor undo stack.</summary>
    internal static class ModelTools
    {
        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "get_model_info",
                "Summary of the model open in PMX Editor: names, comments, element counts, file path and undo depth.",
                Schema.None(),
                true,
                delegate(Dictionary<string, object> args) { return GetModelInfo(editor); });

            registry.Add(
                "set_model_info",
                "Renames the model or replaces its comments. Only the fields you pass are changed.",
                Schema.Object(Json.Obj(
                    "name", Schema.Str("Japanese model name"),
                    "name_en", Schema.Str("English model name"),
                    "comment", Schema.Str("Japanese comment"),
                    "comment_en", Schema.Str("English comment"))),
                false,
                delegate(Dictionary<string, object> args) { return SetModelInfo(editor, args); });

            registry.Add(
                "get_header",
                "The PMX header: format version, how strings are stored, and how many "
                    + "additional UV channels each vertex carries.",
                Schema.None(),
                true,
                delegate(Dictionary<string, object> args) { return GetHeader(editor); });

            registry.Add(
                "set_header",
                "Edits the PMX header. The change takes in the editor, but PMX Editor writes "
                    + "the header from what the model actually contains when it saves: a model "
                    + "using no 2.1 feature is written as 2.0 however you set the version, and "
                    + "an additional UV channel no vertex uses is dropped. Set the content "
                    + "first and the header follows.",
                Schema.Object(Json.Obj(
                    "version", Schema.Num("Format version, normally 2.0 or 2.1"),
                    "string_encode", Schema.Int("0 stores strings as UTF-16, 1 as UTF-8"),
                    "uva_count", Schema.Int("Additional UV channels per vertex, 0-4"))),
                false,
                delegate(Dictionary<string, object> args) { return SetHeader(editor, args); });

            registry.Add(
                "undo",
                "Undoes the last edit in PMX Editor.",
                Schema.None(),
                false,
                delegate(Dictionary<string, object> args) { return Undo(editor); });

            registry.Add(
                "redo",
                "Redoes the last undone edit in PMX Editor.",
                Schema.None(),
                false,
                delegate(Dictionary<string, object> args) { return Redo(editor); });
        }

        private static object HeaderRow(IPXPmx pmx)
        {
            return Json.Obj(
                "version", pmx.Header.Version,
                "stringEncode", pmx.Header.StringEncode,
                "stringEncodeName", pmx.Header.StringEncode == 0 ? "UTF-16" : "UTF-8",
                "uvaCount", pmx.Header.UVACount);
        }

        private static object GetHeader(Editor editor)
        {
            return editor.Read<object>(delegate(IPXPmx pmx) { return HeaderRow(pmx); });
        }

        private static object SetHeader(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Header;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Header;

                if (Json.Has(args, "version"))
                {
                    pmx.Header.Version = Json.Flt(args, "version", pmx.Header.Version);
                }
                if (Json.Has(args, "string_encode"))
                {
                    int e = Json.Int(args, "string_encode", pmx.Header.StringEncode);
                    if (e != 0 && e != 1) throw new McpToolException("string_encode must be 0 or 1");
                    pmx.Header.StringEncode = e;
                }
                if (Json.Has(args, "uva_count"))
                {
                    int n = Json.Int(args, "uva_count", pmx.Header.UVACount);
                    if (n < 0 || n > 4) throw new McpToolException("uva_count must be 0 to 4");
                    pmx.Header.UVACount = n;
                }
                return HeaderRow(pmx);
            });
        }

        private static object GetModelInfo(Editor editor)
        {
            return editor.Ui<object>(delegate
            {
                IPXPmx pmx = editor.State();
                return Json.Obj(
                    "filePath", pmx.FilePath,
                    "name", pmx.ModelInfo.ModelName,
                    "nameEn", pmx.ModelInfo.ModelNameE,
                    "comment", pmx.ModelInfo.Comment,
                    "commentEn", pmx.ModelInfo.CommentE,
                    "pmxVersion", pmx.Header.Version,
                    "counts", Json.Obj(
                        "vertex", pmx.Vertex.Count,
                        "material", pmx.Material.Count,
                        "bone", pmx.Bone.Count,
                        "morph", pmx.Morph.Count,
                        "node", pmx.Node.Count,
                        "rigidBody", pmx.Body.Count,
                        "joint", pmx.Joint.Count,
                        "softBody", pmx.SoftBody.Count),
                    "undoCount", editor.Connector.Form.UndoCount,
                    "redoCount", editor.Connector.Form.RedoCount);
            });
        }

        private static object SetModelInfo(Editor editor, Dictionary<string, object> args)
        {
            if (!Json.Has(args, "name") && !Json.Has(args, "name_en")
                && !Json.Has(args, "comment") && !Json.Has(args, "comment_en"))
            {
                throw new McpToolException("Pass at least one of: name, name_en, comment, comment_en.");
            }

            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.ModelInfo;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Header;
                change.RefreshModel = false;

                if (Json.Has(args, "name")) pmx.ModelInfo.ModelName = Json.Str(args, "name", "");
                if (Json.Has(args, "name_en")) pmx.ModelInfo.ModelNameE = Json.Str(args, "name_en", "");
                if (Json.Has(args, "comment")) pmx.ModelInfo.Comment = Json.Str(args, "comment", "");
                if (Json.Has(args, "comment_en")) pmx.ModelInfo.CommentE = Json.Str(args, "comment_en", "");

                return Json.Obj(
                    "name", pmx.ModelInfo.ModelName,
                    "nameEn", pmx.ModelInfo.ModelNameE,
                    "comment", pmx.ModelInfo.Comment,
                    "commentEn", pmx.ModelInfo.CommentE);
            });
        }

        private static object Undo(Editor editor)
        {
            editor.RequireWrite();
            return editor.Ui<object>(delegate
            {
                editor.Connector.Form.Undo();
                return Json.Obj(
                    "undone", true,
                    "undoCount", editor.Connector.Form.UndoCount,
                    "redoCount", editor.Connector.Form.RedoCount);
            });
        }

        private static object Redo(Editor editor)
        {
            editor.RequireWrite();
            return editor.Ui<object>(delegate
            {
                editor.Connector.Form.Redo();
                return Json.Obj(
                    "redone", true,
                    "undoCount", editor.Connector.Form.UndoCount,
                    "redoCount", editor.Connector.Form.RedoCount);
            });
        }
    }
}
