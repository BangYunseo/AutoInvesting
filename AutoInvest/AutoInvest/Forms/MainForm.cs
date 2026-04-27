using AutoInvest.Core;
using AutoInvest.Data;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Forms
{
    public partial class MainForm : MaterialForm
    {
        private readonly MaterialSkinManager _skinManager;

        public MainForm()
        {
            InitializeComponent();

            _skinManager = MaterialSkinManager.Instance;
            _skinManager.AddFormToManage(this);
            _skinManager.Theme = MaterialSkinManager.Themes.DARK;
            _skinManager.ColorScheme = new ColorScheme(
                Primary.Grey900,
                Primary.Grey900,
                Primary.Grey800,
                Accent.Cyan200,
                TextShade.WHITE
            );


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
                ? System.Drawing.Color.FromArgb(230, 100, 0)
                : System.Drawing.Color.FromArgb(15, 110, 86);

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
    }
}