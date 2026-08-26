using System;
using System.Collections.Generic;

namespace PmxMcp
{
    /// <summary>A base64 payload returned as an MCP image content block.</summary>
    internal class ImagePayload
    {
        public string Base64;
        public string MimeType;

        public ImagePayload(string base64, string mimeType)
        {
            Base64 = base64;
            MimeType = mimeType;
        }
    }

    /// <summary>
    /// Handles the JSON-RPC methods of the MCP lifecycle. Transport-agnostic:
    /// it takes a parsed request and returns the response, or null for notifications.
    /// </summary>
    internal class McpDispatcher
    {
        private static readonly string[] SupportedProtocols =
        {
            "2025-06-18",
            "2025-03-26",
            "2024-11-05"
        };

        private readonly ToolRegistry m_tools;

        public McpDispatcher(ToolRegistry tools)
        {
            m_tools = tools;
        }

        public Dictionary<string, object> Handle(Dictionary<string, object> request)
        {
            object id = Json.Raw(request, "id");
            string method = Json.Str(request, "method", "");

            // Notifications and client responses carry no id and expect no reply.
            if (id == null) return null;

            switch (method)
            {
                case "initialize":
                    return Result(id, Initialize(request));
                case "ping":
                    return Result(id, new Dictionary<string, object>());
                case "tools/list":
                    return Result(id, Json.Obj("tools", m_tools.Describe()));
                case "tools/call":
                    return Result(id, CallTool(request));
                default:
                    return Error(id, -32601, "Method not found: " + method);
            }
        }

        private static Dictionary<string, object> Initialize(Dictionary<string, object> request)
        {
            string requested = Json.Str(Json.Sub(request, "params"), "protocolVersion", PluginInfo.ProtocolVersion);
            string negotiated = PluginInfo.ProtocolVersion;
            foreach (string supported in SupportedProtocols)
            {
                if (supported == requested)
                {
                    negotiated = requested;
                    break;
                }
            }

            return Json.Obj(
                "protocolVersion", negotiated,
                "capabilities", Json.Obj(
                    "tools", Json.Obj("listChanged", false)),
                "serverInfo", Json.Obj(
                    "name", PluginInfo.ServerName,
                    "title", PluginInfo.DisplayName,
                    "version", PluginInfo.Version),
                "instructions", PluginInfo.Instructions);
        }

        private Dictionary<string, object> CallTool(Dictionary<string, object> request)
        {
            Dictionary<string, object> parameters = Json.Sub(request, "params");
            string name = Json.Str(parameters, "name", "");
            Dictionary<string, object> args = Json.Sub(parameters, "arguments");

            ToolDefinition tool = m_tools.Find(name);
            if (tool == null)
            {
                return ToolResult("Unknown tool: " + name, true);
            }

            try
            {
                return ToolResult(tool.Invoke(args), false);
            }
            catch (McpToolException ex)
            {
                return ToolResult(ex.Message, true);
            }
            catch (Exception ex)
            {
                Log.Error("tool " + name + " failed", ex);
                return ToolResult(ex.GetType().Name + ": " + ex.Message, true);
            }
        }

        private static Dictionary<string, object> ToolResult(object value, bool isError)
        {
            ImagePayload image = value as ImagePayload;
            if (image != null)
            {
                return Json.Obj(
                    "content", new object[]
                    {
                        Json.Obj("type", "image", "data", image.Base64, "mimeType", image.MimeType)
                    },
                    "isError", isError);
            }

            string text = value as string;
            Dictionary<string, object> structured = value as Dictionary<string, object>;
            if (text == null)
            {
                text = Json.Stringify(value);
            }

            Dictionary<string, object> result = Json.Obj(
                "content", new object[] { Json.Obj("type", "text", "text", text) },
                "isError", isError);

            if (structured != null && !isError)
            {
                result["structuredContent"] = structured;
            }
            return result;
        }

        public static Dictionary<string, object> Result(object id, object result)
        {
            return Json.Obj("jsonrpc", "2.0", "id", id, "result", result);
        }

        public static Dictionary<string, object> Error(object id, int code, string message)
        {
            return Json.Obj(
                "jsonrpc", "2.0",
                "id", id,
                "error", Json.Obj("code", code, "message", message));
        }
    }
}
