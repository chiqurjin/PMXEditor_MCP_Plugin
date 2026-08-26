using System;
using PEPlugin;
using PEPlugin.Pmx;

namespace PmxMcp
{
    /// <summary>
    /// Wraps the PEPlugin connectors and guarantees every call happens on the UI thread.
    /// Read-modify-write follows the documented pattern: GetCurrentState -> mutate the
    /// copy -> Update -> refresh the lists and the view.
    /// </summary>
    internal class Editor
    {
        /// <summary>What a mutating tool changed, so the right refresh is issued.</summary>
        public class Change
        {
            public PmxUpdateObject Target = PmxUpdateObject.All;
            public int Index = -1;
            public PEPlugin.Pmd.UpdateObject ListTarget = PEPlugin.Pmd.UpdateObject.All;
            public bool RefreshModel = true;
        }

        private readonly IPERunArgs m_args;
        private readonly UiDispatcher m_ui;
        private readonly PluginConfig m_config;

        public Editor(IPERunArgs args, UiDispatcher ui, PluginConfig config)
        {
            m_args = args;
            m_ui = ui;
            m_config = config;
        }

        public PluginConfig Config
        {
            get { return m_config; }
        }

        public IPEConnector Connector
        {
            get { return m_args.Host.Connector; }
        }

        public T Ui<T>(Func<T> func)
        {
            return m_ui.Run(func);
        }

        public void Ui(Action action)
        {
            m_ui.Run(action);
        }

        /// <summary>Snapshot of the model currently open. Call only on the UI thread.</summary>
        public IPXPmx State()
        {
            return Connector.Pmx.GetCurrentState();
        }

        public T Read<T>(Func<IPXPmx, T> func)
        {
            return m_ui.Run(delegate { return func(State()); });
        }

        /// <summary>Mutates the model and pushes the result back into the editor.</summary>
        public T Edit<T>(Func<IPXPmx, Change, T> func)
        {
            RequireWrite();
            return m_ui.Run(delegate
            {
                IPXPmx pmx = State();
                Change change = new Change();
                T result = func(pmx, change);

                Connector.Pmx.Update(pmx, change.Target, change.Index);
                Connector.Form.UpdateList(change.ListTarget);
                if (change.RefreshModel)
                {
                    try
                    {
                        Connector.View.PmxView.UpdateModel();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("view refresh failed", ex);
                    }
                }
                return result;
            });
        }

        public void RequireWrite()
        {
            if (!m_config.AllowWrite)
            {
                throw new McpToolException(
                    "Write operations are disabled. Set allowWrite to true in PmxMcpPlugin.json to enable them.");
            }
        }

        public void RequireFileAccess()
        {
            if (!m_config.AllowFileAccess)
            {
                throw new McpToolException(
                    "File operations are disabled. Set allowFileAccess to true in PmxMcpPlugin.json to enable them.");
            }
        }
    }
}
