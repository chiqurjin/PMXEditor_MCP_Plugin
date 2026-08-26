using System;
using System.IO;
using System.Text;

namespace PmxMcp
{
    /// <summary>
    /// Opt-in file logger. Disabled unless "logFile" is set in PmxMcpPlugin.json,
    /// because a plugin must never throw inside the host process.
    /// </summary>
    internal static class Log
    {
        private static readonly object s_lock = new object();
        private static string s_path;

        public static void Configure(string path)
        {
            s_path = path;
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", ex == null ? message : message + " :: " + ex);
        }

        private static void Write(string level, string message)
        {
            if (string.IsNullOrEmpty(s_path)) return;
            try
            {
                lock (s_lock)
                {
                    File.AppendAllText(
                        s_path,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] " + message + Environment.NewLine,
                        Encoding.UTF8);
                }
            }
            catch
            {
                // logging must never break the editor
            }
        }
    }
}
