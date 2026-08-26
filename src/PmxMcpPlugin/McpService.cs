using System;
using System.Windows.Forms;
using PEPlugin;

namespace PmxMcp
{
    /// <summary>
    /// Owns everything that lives for as long as PMX Editor does: the hidden window used
    /// to reach the UI thread, the tool registry, and the HTTP listener.
    /// </summary>
    internal class McpService : IDisposable
    {
        private readonly PluginConfig m_config;
        private readonly Form m_anchor;
        private readonly ToolRegistry m_registry;
        private readonly HttpTransport m_transport;

        public McpService(IPERunArgs args)
        {
            m_config = PluginConfig.Load(args.ModulePath);
            Log.Configure(m_config.LogFile);
            Log.Info(PluginInfo.DisplayName + " " + PluginInfo.Version + " starting");

            // Created on the UI thread during bootup; every connector call is marshalled back to it.
            m_anchor = new Form();
            m_anchor.Text = PluginInfo.DisplayName;
            m_anchor.ShowInTaskbar = false;
            IntPtr handle = m_anchor.Handle;
            GC.KeepAlive(handle);

            Editor editor = new Editor(args, new UiDispatcher(m_anchor), m_config);

            m_registry = new ToolRegistry();
            ModelTools.Register(m_registry, editor);
            BoneTools.Register(m_registry, editor);
            MaterialTools.Register(m_registry, editor);
            MorphTools.Register(m_registry, editor);
            SelectionTools.Register(m_registry, editor);
            FileTools.Register(m_registry, editor);
            ViewTools.Register(m_registry, editor);

            m_transport = new HttpTransport(m_config, new McpDispatcher(m_registry));
        }

        public PluginConfig Config
        {
            get { return m_config; }
        }

        public bool IsRunning
        {
            get { return m_transport.IsRunning; }
        }

        public string Url
        {
            get { return m_config.Url; }
        }

        public string LastError
        {
            get { return m_transport.LastError; }
        }

        public int ToolCount
        {
            get { return m_registry.Count; }
        }

        public bool Start()
        {
            return m_transport.Start();
        }

        public void Stop()
        {
            m_transport.Stop();
        }

        public bool Restart()
        {
            m_transport.Stop();
            return m_transport.Start();
        }

        public void Dispose()
        {
            try
            {
                m_transport.Stop();
            }
            catch (Exception ex)
            {
                Log.Error("failed to stop the transport", ex);
            }

            try
            {
                if (m_anchor != null && !m_anchor.IsDisposed) m_anchor.Dispose();
            }
            catch
            {
                // the editor is closing anyway
            }
        }
    }
}
