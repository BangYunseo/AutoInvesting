using AutoInvest.Controls;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Panels
{
    /// <summary>
    /// 대시보드 패널 — MainForm의 카드/배분결과/로그를 표시.
    /// </summary>
    public class DashboardPanel : UserControl
    {
        private Panel pnl_card1, pnl_card2, pnl_card3, pnl_card4;
        private Label lbl_card1_title, lbl_card1_value;
        private Label lbl_card2_title, lbl_card2_value;
        private Label lbl_card3_title, lbl_card3_value;
        private Label lbl_card4_title, lbl_card4_value;
        private Panel pnl_alloc_header;
        private Label lbl_alloc_title;
        private FlowLayoutPanel flp_allocation;
        private Panel pnl_log_header;
        private Label lbl_log_title;
        private ListBox lbx_log;

        public DashboardPanel()
        {
            InitializeUI();
            this.Load += (s, e) => LoadDashboard();
        }

        /// <summary>
        /// Logger에서 사용하는 ListBox 반환
        /// </summary>
        public ListBox GetLogListBox() => lbx_log;

        public async void LoadDashboard()
        {
            var amount = AppConfigManager.Get("INVEST_AMOUNT_KRW", "0");
            var strategy = AppConfigManager.Get("ACTIVE_STRATEGY", "사용자정의");
            var isPaper = AppConfigManager.Get("IS_PAPER_TRADING", "1");

            lbl_card1_value.Text = $"{int.Parse(amount):N0}원";
            lbl_card4_value.Text = isPaper == "1" ? "모의투자" : "실거래";
            lbl_card4_value.ForeColor = isPaper == "1"
                ? AppTheme.Warning : AppTheme.Accent;

            var nextOrder = DateTimeHelper.GetNextNYSEOpen();
            lbl_card3_value.Text = nextOrder.ToString("M월 d일 HH:mm");

            // 환율 조회
            try
            {
                var rate = await ExchangeRateService.GetUsdKrwAsync();
                lbl_card2_value.Text = $"{rate:N1}원";
            }
            catch
            {
                lbl_card2_value.Text = "조회 실패";
            }

            LoadAllocationCards(strategy);
        }

        private void LoadAllocationCards(string strategyName)
        {
            flp_allocation.Controls.Clear();

            var strategies = StrategyDAO.GetStrategy(strategyName);
            if (strategies.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "배분 설정이 없습니다. [배분 설정] 메뉴에서 종목을 추가하세요.",
                    Font = new Font("맑은 고딕", 9F),
                    ForeColor = AppTheme.FgMuted,
                    AutoSize = true,
                    Padding = new Padding(10, 10, 0, 0)
                };
                flp_allocation.Controls.Add(emptyLabel);
                return;
            }

            int totalQty = 0;
            foreach (var s in strategies) totalQty += s.Qty;
            if (totalQty <= 0) totalQty = 1;

            foreach (var s in strategies)
            {
                double ratio = (double)s.Qty / totalQty;
                var card = new AllocationCardControl();
                card.SetData(s.Ticker, ratio, s.Qty, 0m);
                flp_allocation.Controls.Add(card);
            }
        }

        private void InitializeUI()
        {
            this.BackColor = AppTheme.BgMain;
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true;

            // ─── 카드 4개 ────────
            pnl_card1 = CreateCard(20, 15, "월 투자금", "—", out lbl_card1_title, out lbl_card1_value);
            pnl_card2 = CreateCard(185, 15, "현재 환율", "—", out lbl_card2_title, out lbl_card2_value);
            pnl_card3 = CreateCard(350, 15, "다음 주문", "—", out lbl_card3_title, out lbl_card3_value);
            pnl_card4 = CreateCard(515, 15, "투자 모드", "—", out lbl_card4_title, out lbl_card4_value);

            // ─── 배분 결과 섹션 ──────
            pnl_alloc_header = new Panel
            {
                BackColor = AppTheme.BgHeader,
                Location = new Point(20, 100),
                Size = new Size(540, 30)
            };
            lbl_alloc_title = new Label
            {
                Text = "📊 배분 결과",
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = AppTheme.FgPrimary,
                Location = new Point(10, 5),
                AutoSize = true
            };
            pnl_alloc_header.Controls.Add(lbl_alloc_title);

            flp_allocation = new FlowLayoutPanel
            {
                BackColor = AppTheme.BgContent,
                Location = new Point(20, 130),
                Size = new Size(540, 140),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            // ─── 로그 섹션 ──────
            pnl_log_header = new Panel
            {
                BackColor = AppTheme.BgHeader,
                Location = new Point(20, 280),
                Size = new Size(540, 30)
            };
            lbl_log_title = new Label
            {
                Text = "📋 실시간 로그",
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = AppTheme.FgPrimary,
                Location = new Point(10, 5),
                AutoSize = true
            };
            pnl_log_header.Controls.Add(lbl_log_title);

            lbx_log = new ListBox
            {
                BackColor = AppTheme.BgContent,
                ForeColor = AppTheme.FgPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 8.5F),
                Location = new Point(20, 310),
                Size = new Size(540, 150)
            };

            this.Controls.AddRange(new Control[] {
                pnl_card1, pnl_card2, pnl_card3, pnl_card4,
                pnl_alloc_header, flp_allocation,
                pnl_log_header, lbx_log
            });
        }

        private Panel CreateCard(int x, int y, string title, string value,
            out Label lblTitle, out Label lblValue)
        {
            var pnl = new Panel
            {
                BackColor = AppTheme.BgCard,
                Location = new Point(x, y),
                Size = new Size(155, 75)
            };

            lblTitle = new Label
            {
                Text = title,
                Font = new Font("맑은 고딕", 9F),
                ForeColor = AppTheme.FgLabel,
                Location = new Point(12, 10),
                AutoSize = true
            };

            lblValue = new Label
            {
                Text = value,
                Font = new Font("맑은 고딕", 13F, FontStyle.Bold),
                ForeColor = AppTheme.FgPrimary,
                Location = new Point(12, 38),
                AutoSize = true
            };

            pnl.Controls.AddRange(new Control[] { lblTitle, lblValue });
            return pnl;
        }
    }
}
