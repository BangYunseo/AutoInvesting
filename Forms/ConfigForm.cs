using AutoInvest.Data;
using AutoInvest.Utils;
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AutoInvest.Forms
{
    public partial class ConfigForm : Form
    {
        [GeneratedRegex(@"^\d{2}:\d{2}$")]
        private static partial Regex ScheduleRegex();
    }

    public partial class ConfigForm
    {
        public ConfigForm()
        {
            InitializeComponent();
            this.Load += ConfigForm_Load;
        }

        private void ConfigForm_Load(object? sender, EventArgs e)
        {
            txt_amount.Text = AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000");
            txt_schedule.Text = AppConfigManager.Get("ORDER_SCHEDULE", "22:30");
            chk_paper.Checked = AppConfigManager.Get("IS_PAPER_TRADING", "1") == "1";

            var strategy = AppConfigManager.Get("ACTIVE_STRATEGY", "안정형");
            rdb_balanced.Checked = strategy == "안정형";
            rdb_aggressive.Checked = strategy == "공격형";
        }

        private void btn_save_Click(object? sender, EventArgs e)
        {
            // 유효성 검사
            if (!int.TryParse(txt_amount.Text, out int amount) || amount <= 0)
            {
                MessageBox.Show("투자금액을 올바르게 입력해주세요.", "입력 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!ScheduleRegex().IsMatch(txt_schedule.Text))
            {
                MessageBox.Show("주문 시각을 HH:mm 형식으로 입력해주세요.", "입력 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AppConfigManager.Set("INVEST_AMOUNT_KRW", txt_amount.Text);
            AppConfigManager.Set("ORDER_SCHEDULE", txt_schedule.Text);
            AppConfigManager.Set("IS_PAPER_TRADING", chk_paper.Checked ? "1" : "0");
            AppConfigManager.Set("ACTIVE_STRATEGY", rdb_balanced.Checked ? "안정형" : "공격형");

            Logger.Info("설정 저장 완료");
            this.Close();
        }

        private void btn_cancel_Click(object? sender, EventArgs e)
            => this.Close();
    }
}