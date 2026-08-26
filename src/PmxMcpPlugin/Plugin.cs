using System;
using System.Windows.Forms;
using PEPlugin;

namespace PmxMcp
{
    /// <summary>
    /// Plugin entry point.
    ///
    /// Bootup:true starts the MCP server together with PMX Editor, so no clicking is
    /// required before a client can connect. Choosing the plugin from the menu opens
    /// the status dialog instead.
    /// </summary>
    public class PmxMcpPlugin : PEPluginClass
    {
        private McpService m_service;

        public PmxMcpPlugin()
            : base()
        {
            // Bootup:true | register in the plugin menu | menu caption
            m_option = new PEPluginOption(true, true, "MCP Server");
        }

        public override string Name
        {
            get { return PluginInfo.DisplayName; }
        }

        public override string Version
        {
            get { return PluginInfo.Version; }
        }

        public override string Description
        {
            get { return "Serves the running PMX Editor to MCP clients over local HTTP."; }
        }

        public override void Run(IPERunArgs args)
        {
            try
            {
                if (m_service == null)
                {
                    m_service = new McpService(args);
                    if (!m_service.Start())
                    {
                        // A failed bind must not interrupt startup; the dialog reports it.
                        Log.Error("could not start the MCP server: " + m_service.LastError, null);
                        if (!args.IsBootup) ShowStatus();
                        return;
                    }
                }

                if (!args.IsBootup)
                {
                    ShowStatus();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Run failed", ex);
                if (!args.IsBootup)
                {
                    MessageBox.Show(ex.ToString(), PluginInfo.DisplayName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowStatus()
        {
            using (StatusForm form = new StatusForm(m_service))
            {
                form.ShowDialog();
            }
        }

        public override void Dispose()
        {
            try
            {
                if (m_service != null)
                {
                    m_service.Dispose();
                    m_service = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Dispose failed", ex);
            }
            base.Dispose();
        }
    }
}
