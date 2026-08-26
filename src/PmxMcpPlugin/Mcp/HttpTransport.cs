using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace PmxMcp
{
    /// <summary>
    /// MCP Streamable HTTP transport, loopback only.
    ///
    /// POST   -> one JSON-RPC message in, one JSON response out (202 for notifications)
    /// DELETE -> ends the session
    /// GET    -> 405, this endpoint offers no server-initiated SSE stream
    ///
    /// The Origin check is the DNS-rebinding protection the MCP spec requires of
    /// local servers; an optional bearer token adds a second gate.
    /// </summary>
    internal class HttpTransport
    {
        private const int MaxBodyBytes = 8 * 1024 * 1024;

        private readonly PluginConfig m_config;
        private readonly McpDispatcher m_dispatcher;

        private HttpListener m_listener;
        private Thread m_thread;
        private volatile bool m_running;
        private string m_sessionId;

        public string LastError { get; private set; }

        public HttpTransport(PluginConfig config, McpDispatcher dispatcher)
        {
            m_config = config;
            m_dispatcher = dispatcher;
        }

        public bool IsRunning
        {
            get { return m_running; }
        }

        public bool Start()
        {
            Stop();
            try
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(m_config.Prefix);
                listener.Start();

                m_listener = listener;
                m_running = true;
                LastError = null;

                m_thread = new Thread(AcceptLoop);
                m_thread.IsBackground = true;
                m_thread.Name = "PmxMcpHttp";
                m_thread.Start();

                Log.Info("listening on " + m_config.Url);
                return true;
            }
            catch (Exception ex)
            {
                m_running = false;
                LastError = ex.Message;
                Log.Error("failed to listen on " + m_config.Prefix, ex);
                return false;
            }
        }

        public void Stop()
        {
            m_running = false;
            m_sessionId = null;
            HttpListener listener = m_listener;
            m_listener = null;
            if (listener != null)
            {
                try { listener.Stop(); }
                catch { }
                try { listener.Close(); }
                catch { }
            }
            m_thread = null;
        }

        private void AcceptLoop()
        {
            while (m_running)
            {
                HttpListenerContext context;
                try
                {
                    context = m_listener.GetContext();
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error("accept failed", ex);
                    break;
                }
                ThreadPool.QueueUserWorkItem(HandleContext, context);
            }
        }

        private void HandleContext(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            try
            {
                Dispatch(context);
            }
            catch (Exception ex)
            {
                Log.Error("request failed", ex);
                try
                {
                    SendJson(context, 500, McpDispatcher.Error(null, -32603, "Internal error: " + ex.Message));
                }
                catch
                {
                    // the client is already gone
                }
            }
        }

        private void Dispatch(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;

            if (!IsAllowedOrigin(request.Headers["Origin"]))
            {
                SendJson(context, 403, McpDispatcher.Error(null, -32600, "Forbidden origin"));
                return;
            }

            if (!IsAuthorized(request))
            {
                context.Response.AddHeader("WWW-Authenticate", "Bearer");
                SendJson(context, 401, McpDispatcher.Error(null, -32600, "Unauthorized"));
                return;
            }

            if (!PathMatches(request.Url.AbsolutePath))
            {
                SendJson(context, 404, McpDispatcher.Error(null, -32600, "No MCP endpoint at " + request.Url.AbsolutePath));
                return;
            }

            switch (request.HttpMethod)
            {
                case "POST":
                    HandlePost(context);
                    break;
                case "DELETE":
                    m_sessionId = null;
                    SendStatus(context, 200);
                    break;
                case "OPTIONS":
                    context.Response.AddHeader("Allow", "POST, DELETE, OPTIONS");
                    SendStatus(context, 204);
                    break;
                default:
                    context.Response.AddHeader("Allow", "POST, DELETE, OPTIONS");
                    SendJson(context, 405, McpDispatcher.Error(null, -32600,
                        "This endpoint does not offer a server-initiated SSE stream; use POST."));
                    break;
            }
        }

        private void HandlePost(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;

            if (request.ContentLength64 > MaxBodyBytes)
            {
                SendJson(context, 413, McpDispatcher.Error(null, -32600, "Request body too large"));
                return;
            }

            string body;
            using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }

            if (body != null && body.TrimStart().StartsWith("["))
            {
                SendJson(context, 400, McpDispatcher.Error(null, -32600,
                    "JSON-RPC batching is not supported by MCP 2025-06-18"));
                return;
            }

            Dictionary<string, object> message;
            try
            {
                message = Json.Parse(body);
            }
            catch (Exception)
            {
                SendJson(context, 400, McpDispatcher.Error(null, -32700, "Parse error"));
                return;
            }

            string method = Json.Str(message, "method", "");
            string incomingSession = request.Headers["Mcp-Session-Id"];
            if (method != "initialize"
                && !string.IsNullOrEmpty(m_sessionId)
                && !string.IsNullOrEmpty(incomingSession)
                && incomingSession != m_sessionId)
            {
                SendJson(context, 404, McpDispatcher.Error(null, -32600, "Unknown or expired session"));
                return;
            }

            Dictionary<string, object> response = m_dispatcher.Handle(message);

            if (method == "initialize")
            {
                m_sessionId = Guid.NewGuid().ToString("N");
            }
            if (!string.IsNullOrEmpty(m_sessionId))
            {
                context.Response.AddHeader("Mcp-Session-Id", m_sessionId);
            }

            if (response == null)
            {
                SendStatus(context, 202);
                return;
            }
            SendJson(context, 200, response);
        }

        private bool PathMatches(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (string.Equals(path, m_config.Route, StringComparison.OrdinalIgnoreCase)) return true;
            // A bare root request is accepted too, so a mistyped URL still works.
            return path == "/";
        }

        private bool IsAuthorized(HttpListenerRequest request)
        {
            if (string.IsNullOrEmpty(m_config.Token)) return true;
            string header = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(header)) return false;
            return string.Equals(header.Trim(), "Bearer " + m_config.Token, StringComparison.Ordinal);
        }

        private static bool IsAllowedOrigin(string origin)
        {
            if (string.IsNullOrEmpty(origin)) return true;
            try
            {
                Uri uri = new Uri(origin);
                string host = uri.Host;
                return host == "localhost" || host == "127.0.0.1" || host == "::1" || host == "[::1]";
            }
            catch
            {
                return false;
            }
        }

        private static void SendStatus(HttpListenerContext context, int statusCode)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentLength64 = 0;
            context.Response.Close();
        }

        private static void SendJson(HttpListenerContext context, int statusCode, object payload)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Json.Stringify(payload));
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
