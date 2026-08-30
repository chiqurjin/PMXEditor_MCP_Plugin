namespace PmxMcp
{
    /// <summary>Static identity of this plugin / MCP server.</summary>
    internal static class PluginInfo
    {
        public const string ServerName = "pmx-editor";
        public const string DisplayName = "PMX Editor MCP Plugin";
        public const string Version = "0.2.0";

        /// <summary>MCP protocol revision this server implements.</summary>
        public const string ProtocolVersion = "2025-06-18";

        /// <summary>Sent to the client on initialize; shown to the model as usage guidance.</summary>
        public const string Instructions =
            "This server controls a running PMX Editor instance (MMD model editor).\n" +
            "Read the model with get_model_info / list_bones / list_materials / list_morphs, " +
            "and modify it with set_model_info / set_bone / set_material.\n" +
            "Edits go through the editor's normal undo stack, so 'undo' reverts the last change.\n" +
            "capture_viewport returns a PNG screenshot of the PmxView window - use it to check results visually.\n" +
            "Indices are 0-based and always refer to the model currently open in the editor.";
    }
}
