using AutoInvest.Core;
using AutoInvest.Data;
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            Logger.Initialize(lbx_log);
            Logger.Info("투자 자동화 시작");
            _ = DBManager.Instance;
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            var amount = AppConfigManager.Get("INVEST_AMOUNT_KRW", "0");
            var schedule = AppConfigManager.Get("ORDER_SCHEDULE", "22:30");
            var strategy = AppConfigManager.Get("ACTIVE_STRATEGY", "미설정");
            var isPaper = AppConfigManager.Get("IS_PAPER_TRADING", "1");

            lbl_card1_value.Text = $"{int.Parse(amount):N0}원";
            lbl_card4_value.Text = isPaper == "1" ? "모의투자" : "실거래";
            lbl_card4_value.ForeColor = isPaper == "1"
                ? Color.FromArgb(230, 100, 0)
                : Color.FromArgb(15, 110, 86);

            // 다음 주문 시각 계산
            var timeParts = schedule.Split(':');
            var orderHour = int.Parse(timeParts[0]);
            var orderMin = int.Parse(timeParts[1]);
            var now = DateTime.Now;
            var orderToday = new DateTime(now.Year, now.Month, now.Day, orderHour, orderMin, 0);
            var nextOrder = now < orderToday ? orderToday : orderToday.AddDays(1);

            lbl_card3_value.Text = nextOrder.ToString("M월 d일 HH:mm");

            Logger.Info($"전략: {strategy} / 투자금: {amount}원 / 다음주문: {nextOrder:M월 d일 HH:mm}");
        
        }

        private void 복사ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lbx_log.SelectedItems.Count == 0) return;
            var sb = new System.Text.StringBuilder();
            foreach (var item in lbx_log.SelectedItems)
                sb.AppendLine(item.ToString());
            Clipboard.SetText(sb.ToString());
        }

        private void SetActiveMenu(System.Windows.Forms.Button activeBtn)
        {
            foreach (var btn in new[] { btn_dashboard, btn_allocation, btn_history, btn_config, btn_log })
            {
                btn.BackColor = Color.FromArgb(38, 50, 56);
                btn.ForeColor = Color.FromArgb(180, 200, 210);
            }
            activeBtn.BackColor = Color.FromArgb(60, 80, 90);
            activeBtn.ForeColor = Color.White;
        }

        private void btn_dashboard_Click(object sender, EventArgs e) 
            => SetActiveMenu(btn_dashboard);

        private void btn_allocation_Click(object sender, EventArgs e)
            => SetActiveMenu(btn_allocation);

        private void btn_history_Click(object sender, EventArgs e)
        { 
            SetActiveMenu(btn_history);
            new HistoryForm().ShowDialog();
        }

        private void btn_config_Click(object sender, EventArgs e)
        {
            SetActiveMenu(btn_config);
            new ConfigForm().ShowDialog();
        }

        private void btn_log_Click(object sender, EventArgs e)
            => SetActiveMenu(btn_log);
    }
}