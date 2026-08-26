using System.Collections.Generic;
using System.IO;

namespace PmxMcp
{
    /// <summary>Opening and saving model files through the editor.</summary>
    internal static class FileTools
    {
        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "open_model",
                "Opens a .pmx or .pmd file in PMX Editor, replacing whatever is loaded. Unsaved changes are lost.",
                Schema.Object(Json.Obj(
                    "path", Schema.Str("Absolute path to a .pmx or .pmd file")),
                    "path"),
                false,
                delegate(Dictionary<string, object> args) { return OpenModel(editor, args); });

            registry.Add(
                "save_model",
                "Saves the current model. Without a path it overwrites the file that is open; "
                    + "with a path it writes a new file, and refuses to clobber an existing one unless overwrite is true.",
                Schema.Object(Json.Obj(
                    "path", Schema.Str("Absolute path of the .pmx file to write"),
                    "overwrite", Schema.Bool("Allow replacing an existing file at path (default false)"))),
                false,
                delegate(Dictionary<string, object> args) { return SaveModel(editor, args); });
        }

        private static object OpenModel(Editor editor, Dictionary<string, object> args)
        {
            editor.RequireWrite();
            editor.RequireFileAccess();

            string path = Json.Str(args, "path", "");
            if (string.IsNullOrEmpty(path)) throw new McpToolException("path is required");
            if (!File.Exists(path)) throw new McpToolException("file not found: " + path);

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".pmx" && extension != ".pmd")
            {
                throw new McpToolException("only .pmx and .pmd files can be opened, got " + extension);
            }

            return editor.Ui<object>(delegate
            {
                if (extension == ".pmx")
                {
                    editor.Connector.Form.OpenPMXFile(path);
                }
                else
                {
                    editor.Connector.Form.OpenPMDFile(path);
                }

                return Json.Obj(
                    "opened", path,
                    "name", editor.State().ModelInfo.ModelName);
            });
        }

        private static object SaveModel(Editor editor, Dictionary<string, object> args)
        {
            editor.RequireWrite();
            editor.RequireFileAccess();

            string path = Json.Str(args, "path", "");
            bool overwrite = Json.Bool(args, "overwrite", false);

            return editor.Ui<object>(delegate
            {
                string target = path;
                if (string.IsNullOrEmpty(target))
                {
                    target = editor.Connector.Pmx.CurrentPath;
                    if (string.IsNullOrEmpty(target))
                    {
                        throw new McpToolException("this model has no file yet; pass an explicit path");
                    }
                }
                else
                {
                    if (Path.GetExtension(target).ToLowerInvariant() != ".pmx")
                    {
                        throw new McpToolException("path must end with .pmx");
                    }
                    if (File.Exists(target) && !overwrite)
                    {
                        throw new McpToolException(target + " already exists; pass overwrite=true to replace it");
                    }
                    string directory = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        throw new McpToolException("directory does not exist: " + directory);
                    }
                }

                editor.Connector.Form.SavePMXFile(target);

                long size = 0;
                if (File.Exists(target)) size = new FileInfo(target).Length;

                return Json.Obj("saved", target, "bytes", size);
            });
        }
    }
}
