using AutoInvest.Data;
using AutoInvest.Utils;
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AutoInvest.Panels
{
    /// <summary>
    /// 환경 설정 패널 — 투자금/전략유형/주문시각 등을 설정합니다.
    /// 저장 완료 시 OnSaved 이벤트 발행.
    /// </summary>
    public partial class ConfigPanel : UserControl
    {
        [GeneratedRegex(@"^\d{2}:\d{2}$")]
        private static partial Regex ScheduleRegex();
    }

    public partial class ConfigPanel
    {
        public event EventHandler? OnSaved;

        private Label lbl_title, lbl_amount, lbl_strategyType, lbl_schedule;
        private TextBox txt_amount, txt_schedule;
        private ComboBox cmb_strategyType;
        private CheckBox chk_paper;
        private Button btn_save;

        public ConfigPanel()
        {
            InitializeUI();
            this.Load += (s, e) => LoadConfig();
        }

        private void LoadConfig()
        {
            txt_amount.Text = AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000");
            txt_schedule.Text = AppConfigManager.Get("ORDER_SCHEDULE", "22:30");
            chk_paper.Checked = AppConfigManager.Get("IS_PAPER_TRADING", "1") == "1";

            var strategyType = AppConfigManager.Get("STRATEGY_TYPE", "MEAN_REVERSION");
            int idx = cmb_strategyType.Items.IndexOf(strategyType);
            cmb_strategyType.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void btn_save_Click(object? sender, EventArgs e)
        {
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
            AppConfigManager.Set("STRATEGY_TYPE", cmb_strategyType.SelectedItem?.ToString() ?? "MEAN_REVERSION");

            Logger.Info("설정 저장 완료");
            MessageBox.Show("설정이 저장되었습니다.", "저장 완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            OnSaved?.Invoke(this, EventArgs.Empty);
        }

        private void InitializeUI()
        {
            this.BackColor = AppTheme.BgMain;
            this.Dock = DockStyle.Fill;

            lbl_title = new Label
            {
                Text = "환경 설정",
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                ForeColor = AppTheme.FgPrimary,
                Location = new Point(20, 15),
                Size = new Size(200, 30)
            };

            lbl_amount = new Label
            {
                Text = "월 투자금액 (원)",
                Font = new Font("맑은 고딕", 10F),
                ForeColor = AppTheme.FgSecondary,
                Location = new Point(30, 65),
                AutoSize = true
            };

            txt_amount = new TextBox
            {
                BackColor = AppTheme.BgInput,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(30, 90),
                Size = new Size(300, 27),
                Font = new Font("맑은 고딕", 11F)
            };

            lbl_strategyType = new Label
            {
                Text = "퀀트 전략 유형",
                Font = new Font("맑은 고딕", 10F),
                ForeColor = AppTheme.FgSecondary,
                Location = new Point(30, 135),
                AutoSize = true
            };

            cmb_strategyType = new ComboBox
            {
                BackColor = AppTheme.BgInput,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(30, 160),
                Size = new Size(300, 27),
                Font = new Font("맑은 고딕", 11F)
            };
            cmb_strategyType.Items.AddRange(new object[] { "MEAN_REVERSION", "MOMENTUM", "MIXED" });

            lbl_schedule = new Label
            {
                Text = "자동 주문 시각 (HH:mm)",
                Font = new Font("맑은 고딕", 10F),
                ForeColor = AppTheme.FgSecondary,
                Location = new Point(30, 210),
                AutoSize = true
            };

            txt_schedule = new TextBox
            {
                BackColor = AppTheme.BgInput,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(30, 235),
                Size = new Size(120, 27),
                Font = new Font("맑은 고딕", 11F)
            };

            chk_paper = new CheckBox
            {
                Text = "모의투자 모드 (체크 해제 시 실거래)",
                Font = new Font("맑은 고딕", 10F),
                ForeColor = AppTheme.FgPrimary,
                Location = new Point(30, 285),
                AutoSize = true
            };

            btn_save = new Button
            {
                Text = "저장",
                BackColor = AppTheme.BtnPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(30, 340),
                Size = new Size(300, 36)
            };
            btn_save.FlatAppearance.BorderSize = 0;
            btn_save.Click += btn_save_Click;

            this.Controls.AddRange(new Control[] {
                lbl_title, lbl_amount, txt_amount,
                lbl_strategyType, cmb_strategyType,
                lbl_schedule, txt_schedule,
                chk_paper, btn_save
            });
        }
    }
}
