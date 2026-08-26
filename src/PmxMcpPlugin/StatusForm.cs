using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PmxMcp
{
    /// <summary>
    /// Shown from [編集]-[プラグイン]-[MCP Server]: current state, endpoint,
    /// and the exact command to register this server with Claude Code.
    /// </summary>
    internal class StatusForm : Form
    {
        private readonly McpService m_service;
        private readonly TextBox m_text;

        public StatusForm(McpService service)
        {
            m_service = service;

            Text = PluginInfo.DisplayName + " " + PluginInfo.Version;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(600, 300);

            m_text = new TextBox();
            m_text.Multiline = true;
            m_text.ReadOnly = true;
            m_text.ScrollBars = ScrollBars.Vertical;
            m_text.WordWrap = false;
            m_text.Font = new Font(FontFamily.GenericMonospace, 9f);
            m_text.SetBounds(12, 12, 576, 230);
            Controls.Add(m_text);

            Button copy = new Button();
            copy.Text = "コマンドをコピー / Copy command";
            copy.SetBounds(12, 254, 220, 32);
            copy.Click += CopyCommand;
            Controls.Add(copy);

            Button restart = new Button();
            restart.Text = "再起動 / Restart";
            restart.SetBounds(244, 254, 160, 32);
            restart.Click += RestartServer;
            Controls.Add(restart);

            Button close = new Button();
            close.Text = "閉じる / Close";
            close.SetBounds(428, 254, 160, 32);
            close.Click += delegate { Close(); };
            Controls.Add(close);

            AcceptButton = close;
            CancelButton = close;

            UpdateText();
        }

        private string Command
        {
            get { return "claude mcp add --transport http pmx-editor " + m_service.Url; }
        }

        private void UpdateText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("状態 / status : " + (m_service.IsRunning ? "running" : "stopped"));
            if (!m_service.IsRunning && !string.IsNullOrEmpty(m_service.LastError))
            {
                sb.AppendLine("エラー / error : " + m_service.LastError);
            }
            sb.AppendLine("URL           : " + m_service.Url);
            sb.AppendLine("ツール数 / tools : " + m_service.ToolCount);
            sb.AppendLine("書き込み / write : " + (m_service.Config.AllowWrite ? "allowed" : "disabled"));
            sb.AppendLine("ファイル / files : " + (m_service.Config.AllowFileAccess ? "allowed" : "disabled"));
            sb.AppendLine("認証 / token   : " + (string.IsNullOrEmpty(m_service.Config.Token) ? "none" : "required"));
            sb.AppendLine();
            sb.AppendLine("Claude Code に登録するコマンド:");
            sb.AppendLine("  " + Command);
            sb.AppendLine();
            sb.AppendLine("設定ファイル / config file:");
            sb.AppendLine("  PmxMcpPlugin.json (このDLLと同じフォルダ / next to this DLL)");
            sb.AppendLine("  変更後は上の[再起動]を押してください / press Restart after editing");

            m_text.Text = sb.ToString();
        }

        private void CopyCommand(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(Command);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RestartServer(object sender, EventArgs e)
        {
            m_service.Restart();
            UpdateText();
        }
    }
}
