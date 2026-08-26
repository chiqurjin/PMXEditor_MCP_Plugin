using System;
using System.Windows.Forms;

namespace PmxMcp
{
    /// <summary>
    /// Marshals work onto the PMX Editor UI thread.
    /// Every PEPlugin connector call must run there; HTTP requests arrive on worker threads.
    /// </summary>
    internal class UiDispatcher
    {
        private readonly Control m_anchor;

        public UiDispatcher(Control anchor)
        {
            m_anchor = anchor;
        }

        public T Run<T>(Func<T> func)
        {
            if (m_anchor == null || m_anchor.IsDisposed || !m_anchor.InvokeRequired)
            {
                return func();
            }
            return (T)m_anchor.Invoke(func);
        }

        public void Run(Action action)
        {
            Run<object>(delegate
            {
                action();
                return null;
            });
        }
    }
}
