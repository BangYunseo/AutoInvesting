using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Panels
{
    /// <summary>
    /// 거래 내역 패널 — 최근 거래 내역을 조회하고 표시합니다.
    /// </summary>
    public class HistoryPanel : UserControl
    {
        private Label lbl_title;
        private ListView lvw_history;
        private ColumnHeader col_date, col_ticker, col_type, col_qty, col_price, col_status;
        private Button btn_refresh;

        public HistoryPanel()
        {
            InitializeUI();
            this.Load += (s, e) => LoadHistory();
        }

        private void LoadHistory()
        {
            lvw_history.Items.Clear();
            var list = TradeHistoryDAO.GetRecent(50);

            foreach (var h in list)
            {
                var item = new ListViewItem(h.TradeDate.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(h.Ticker);
                item.SubItems.Add(h.OrderType);
                item.SubItems.Add($"{h.Qty}주");
                item.SubItems.Add($"{h.Price:N2}");
                item.SubItems.Add(h.Status);
                lvw_history.Items.Add(item);
            }

            Logger.Info($"[거래내역] {list.Count}건 로드 완료");
        }

        private void btn_refresh_Click(object? sender, EventArgs e)
            => LoadHistory();

        private void InitializeUI()
        {
            this.BackColor = AppTheme.BgMain;
            this.Dock = DockStyle.Fill;

            lbl_title = new Label
            {
                Text = "거래 내역",
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                ForeColor = AppTheme.FgPrimary,
                Location = new Point(20, 15),
                Size = new Size(200, 30)
            };

            col_date = new ColumnHeader { Text = "일시", Width = 140 };
            col_ticker = new ColumnHeader { Text = "종목", Width = 80 };
            col_type = new ColumnHeader { Text = "구분", Width = 60 };
            col_qty = new ColumnHeader { Text = "수량", Width = 60 };
            col_price = new ColumnHeader { Text = "단가(USD)", Width = 90 };
            col_status = new ColumnHeader { Text = "상태", Width = 80 };

            lvw_history = new ListView
            {
                BackColor = AppTheme.BgContent,
                ForeColor = AppTheme.FgPrimary,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                GridLines = true,
                View = View.Details,
                Location = new Point(0, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            lvw_history.Columns.AddRange(new[] { col_date, col_ticker, col_type, col_qty, col_price, col_status });

            btn_refresh = new Button
            {
                Text = "새로고침",
                BackColor = AppTheme.BtnSecondary,
                ForeColor = AppTheme.FgSecondary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F),
                Cursor = Cursors.Hand,
                Location = new Point(530, 15),
                Size = new Size(110, 32)
            };
            btn_refresh.FlatAppearance.BorderColor = AppTheme.BtnBorder;
            btn_refresh.FlatAppearance.BorderSize = 1;
            btn_refresh.Click += btn_refresh_Click;

            this.Controls.AddRange(new Control[] { lbl_title, lvw_history, btn_refresh });
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lvw_history != null)
                lvw_history.Size = new Size(this.Width, this.Height - 65);
        }
    }
}
