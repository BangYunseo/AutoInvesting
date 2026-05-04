using AutoInvest.Controls;
using AutoInvest.Core;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Forms
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            Logger.Initialize(lbx_log);
            Logger.Info("투자 자동화 시작");
            _ = DBManager.Instance;
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            var amount = AppConfigManager.Get("INVEST_AMOUNT_KRW", "0");
            var strategy = AppConfigManager.Get("ACTIVE_STRATEGY", "미설정");
            var isPaper = AppConfigManager.Get("IS_PAPER_TRADING", "1");

            lbl_card1_value.Text = $"{int.Parse(amount):N0}원";
            lbl_card4_value.Text = isPaper == "1" ? "모의투자" : "실거래";
            lbl_card4_value.ForeColor = isPaper == "1"
                ? Color.FromArgb(230, 100, 0)
                : Color.FromArgb(15, 110, 86);

            // 다음 주문 시각 계산 (DateTimeHelper 활용 — DST 자동 대응)
            var nextOrder = DateTimeHelper.GetNextNYSEOpen();

            lbl_card3_value.Text = nextOrder.ToString("M월 d일 HH:mm");

            Logger.Info($"전략: {strategy} / 투자금: {amount}원 / 다음주문: {nextOrder:M월 d일 HH:mm}");

            // 배분 결과 로드
            LoadAllocationCards(strategy, decimal.Parse(amount));
        }

        private void 복사ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (lbx_log.SelectedItems.Count == 0) return;
            var sb = new System.Text.StringBuilder();
            foreach (var item in lbx_log.SelectedItems)
                sb.AppendLine(item.ToString());
            Clipboard.SetText(sb.ToString());
        }

        private void SetActiveMenu(System.Windows.Forms.Button activeBtn)
        {
            foreach (var btn in new[] { btn_dashboard, btn_allocation, btn_history, btn_backtest, btn_config, btn_log })
            {
                btn.BackColor = Color.FromArgb(38, 50, 56);
                btn.ForeColor = Color.FromArgb(180, 200, 210);
            }
            activeBtn.BackColor = Color.FromArgb(60, 80, 90);
            activeBtn.ForeColor = Color.White;
        }

        private void btn_dashboard_Click(object? sender, EventArgs e) 
            => SetActiveMenu(btn_dashboard);

        private void btn_allocation_Click(object? sender, EventArgs e)
        {
            SetActiveMenu(btn_allocation);
            new AllocationSetupForm().ShowDialog();
            // 배분 설정 Form 닫힌 후 대시보드 갱신
            LoadDashboard();
        }

        private void btn_history_Click(object? sender, EventArgs e)
        { 
            SetActiveMenu(btn_history);
            new HistoryForm().ShowDialog();
        }

        private void btn_backtest_Click(object? sender, EventArgs e)
        {
            SetActiveMenu(btn_backtest);
            var session = new SessionManager();
            new BacktestForm(session).ShowDialog();
        }

        private void btn_config_Click(object? sender, EventArgs e)
        {
            SetActiveMenu(btn_config);
            new ConfigForm().ShowDialog();
        }

        private void btn_log_Click(object? sender, EventArgs e)
            => SetActiveMenu(btn_log);

        // ─── 배분 결과 카드 로드 ────────────────────────────

        private void LoadAllocationCards(string strategyName, decimal investAmountKrw)
        {
            flp_allocation.Controls.Clear();

            var strategies = StrategyDAO.GetStrategy(strategyName);
            if (strategies.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "배분 설정이 없습니다. [배분 설정] 메뉴에서 종목을 추가하세요.",
                    Font = new Font("맑은 고딕", 9F),
                    ForeColor = Color.FromArgb(150, 150, 150),
                    AutoSize = true,
                    Padding = new Padding(10, 10, 0, 0)
                };
                flp_allocation.Controls.Add(emptyLabel);
                return;
            }

            // 전체 비중 합계 (사용자정의 전략에서는 Weight=수량이므로 합계 계산)
            double totalWeight = 0;
            foreach (var s in strategies)
                totalWeight += s.Weight;

            if (totalWeight <= 0) totalWeight = 1;

            foreach (var s in strategies)
            {
                double normalizedWeight = s.Weight / totalWeight;
                int qty = (int)s.Weight; // 사용자정의 전략에서는 Weight에 수량 저장
                decimal allocKrw = investAmountKrw * (decimal)normalizedWeight;

                var card = new AllocationCardControl();
                card.SetData(s.Ticker, normalizedWeight, qty, allocKrw);
                flp_allocation.Controls.Add(card);
            }

            Logger.Info($"[대시보드] 배분 결과 로드: {strategyName} ({strategies.Count}종목)");
        }
    }
}