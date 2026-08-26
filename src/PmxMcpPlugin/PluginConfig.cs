using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PmxMcp
{
    /// <summary>
    /// Settings read from PmxMcpPlugin.json, which sits next to the plugin DLL.
    /// The file is created with defaults on first run.
    /// </summary>
    internal class PluginConfig
    {
        public string Host = "127.0.0.1";
        public int Port = 38731;
        public string Route = "/mcp";
        public string Token = "";
        public bool AllowWrite = true;
        public bool AllowFileAccess = true;
        public string LogFile = "";

        public string Url
        {
            get { return "http://" + Host + ":" + Port + Route; }
        }

        public string Prefix
        {
            get { return "http://" + Host + ":" + Port + "/"; }
        }

        public static string ConfigPathFor(string modulePath)
        {
            string dir = string.IsNullOrEmpty(modulePath)
                ? AppDomain.CurrentDomain.BaseDirectory
                : System.IO.Path.GetDirectoryName(modulePath);
            return System.IO.Path.Combine(dir, "PmxMcpPlugin.json");
        }

        public static PluginConfig Load(string modulePath)
        {
            PluginConfig cfg = new PluginConfig();
            string file = ConfigPathFor(modulePath);
            try
            {
                if (File.Exists(file))
                {
                    Dictionary<string, object> d = Json.Parse(File.ReadAllText(file, Encoding.UTF8));
                    cfg.Host = Json.Str(d, "host", cfg.Host);
                    cfg.Port = Json.Int(d, "port", cfg.Port);
                    cfg.Route = Json.Str(d, "path", cfg.Route);
                    cfg.Token = Json.Str(d, "token", cfg.Token);
                    cfg.AllowWrite = Json.Bool(d, "allowWrite", cfg.AllowWrite);
                    cfg.AllowFileAccess = Json.Bool(d, "allowFileAccess", cfg.AllowFileAccess);
                    cfg.LogFile = Json.Str(d, "logFile", cfg.LogFile);
                }
                else
                {
                    WriteDefault(file);
                }
            }
            catch (Exception ex)
            {
                Log.Error("failed to read " + file, ex);
            }

            if (string.IsNullOrEmpty(cfg.Route)) cfg.Route = "/mcp";
            if (!cfg.Route.StartsWith("/")) cfg.Route = "/" + cfg.Route;
            if (cfg.Port <= 0 || cfg.Port > 65535) cfg.Port = 38731;
            if (string.IsNullOrEmpty(cfg.Host)) cfg.Host = "127.0.0.1";
            return cfg;
        }

        private static void WriteDefault(string file)
        {
            string text =
                "{\r\n" +
                "  \"host\": \"127.0.0.1\",\r\n" +
                "  \"port\": 38731,\r\n" +
                "  \"path\": \"/mcp\",\r\n" +
                "  \"token\": \"\",\r\n" +
                "  \"allowWrite\": true,\r\n" +
                "  \"allowFileAccess\": true,\r\n" +
                "  \"logFile\": \"\"\r\n" +
                "}\r\n";
            try
            {
                File.WriteAllText(file, text, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Log.Error("failed to create " + file, ex);
            }
        }
    }
}
