using System;

namespace PmxMcp
{
    /// <summary>
    /// A tool failure the caller is meant to read (bad argument, missing target,
    /// writes disabled). Reported as an MCP tool error result, not a JSON-RPC error.
    /// </summary>
    internal class McpToolException : Exception
    {
        public McpToolException(string message) : base(message)
        {
        }
    }
}
