using System.Collections.Generic;

namespace PmxMcp
{
    internal delegate object ToolFunc(Dictionary<string, object> args);

    internal class ToolDefinition
    {
        public string Name;
        public string Description;
        public Dictionary<string, object> InputSchema;
        public ToolFunc Invoke;
        public bool ReadOnly;
    }

    /// <summary>The set of tools advertised over tools/list and executed by tools/call.</summary>
    internal class ToolRegistry
    {
        private readonly List<ToolDefinition> m_tools = new List<ToolDefinition>();

        public void Add(string name, string description, Dictionary<string, object> inputSchema, bool readOnly, ToolFunc invoke)
        {
            ToolDefinition tool = new ToolDefinition();
            tool.Name = name;
            tool.Description = description;
            tool.InputSchema = inputSchema;
            tool.ReadOnly = readOnly;
            tool.Invoke = invoke;
            m_tools.Add(tool);
        }

        public ToolDefinition Find(string name)
        {
            foreach (ToolDefinition tool in m_tools)
            {
                if (tool.Name == name) return tool;
            }
            return null;
        }

        public int Count
        {
            get { return m_tools.Count; }
        }

        public object[] Describe()
        {
            List<object> list = new List<object>();
            foreach (ToolDefinition tool in m_tools)
            {
                list.Add(Json.Obj(
                    "name", tool.Name,
                    "description", tool.Description,
                    "inputSchema", tool.InputSchema,
                    "annotations", Json.Obj(
                        "readOnlyHint", tool.ReadOnly,
                        "destructiveHint", !tool.ReadOnly,
                        "idempotentHint", tool.ReadOnly,
                        "openWorldHint", false)));
            }
            return list.ToArray();
        }
    }
}
