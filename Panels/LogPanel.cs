using AutoInvest.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Panels
{
    /// <summary>
    /// 로그 전용 패널 — 전체 화면 로그 뷰.
    /// Logger에 연결된 ListBox를 표시합니다.
    /// </summary>
    public class LogPanel : UserControl
    {
        private Label lbl_title;
        private ListBox lbx_log;
        private Button btn_clear, btn_copy;

        public LogPanel()
        {
            InitializeUI();
        }

        /// <summary>
        /// Logger에서 사용하는 ListBox를 반환합니다.
        /// MainForm에서 Logger.Initialize() 시 이 ListBox를 사용합니다.
        /// </summary>
        public ListBox GetLogListBox() => lbx_log;

        private void btn_clear_Click(object? sender, EventArgs e)
        {
            lbx_log.Items.Clear();
            Logger.Info("[로그] 로그 화면 초기화");
        }

        private void btn_copy_Click(object? sender, EventArgs e)
        {
            if (lbx_log.Items.Count == 0) return;
            var sb = new System.Text.StringBuilder();
            foreach (var item in lbx_log.Items)
                sb.AppendLine(item.ToString());
            Clipboard.SetText(sb.ToString());
            Logger.Info("[로그] 전체 로그 클립보드 복사 완료");
        }

        private void InitializeUI()
        {
            this.BackColor = AppTheme.BgMain;
            this.Dock = DockStyle.Fill;

            lbl_title = new Label
            {
                Text = "시스템 로그",
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                ForeColor = AppTheme.FgPrimary,
                Location = new Point(20, 15),
                Size = new Size(200, 30)
            };

            btn_copy = new Button
            {
                Text = "전체 복사",
                BackColor = AppTheme.BtnSecondary,
                ForeColor = AppTheme.FgSecondary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9F),
                Cursor = Cursors.Hand,
                Location = new Point(400, 15),
                Size = new Size(90, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btn_copy.FlatAppearance.BorderColor = AppTheme.BtnBorder;
            btn_copy.Click += btn_copy_Click;

            btn_clear = new Button
            {
                Text = "초기화",
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9F),
                Cursor = Cursors.Hand,
                Location = new Point(500, 15),
                Size = new Size(70, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btn_clear.FlatAppearance.BorderSize = 0;
            btn_clear.Click += btn_clear_Click;

            lbx_log = new ListBox
            {
                BackColor = AppTheme.BgContent,
                ForeColor = AppTheme.FgPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9F),
                Location = new Point(0, 55),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                SelectionMode = SelectionMode.MultiExtended
            };

            this.Controls.AddRange(new Control[] { lbl_title, btn_copy, btn_clear, lbx_log });
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lbx_log != null)
                lbx_log.Size = new Size(this.Width, this.Height - 60);
        }
    }
}
