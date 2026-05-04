using AutoInvest.Core;
using AutoInvest.Core.Quant;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Forms
{
    /// <summary>
    /// 백테스팅 폼.
    /// 퀀트 전략을 과거 데이터로 검증하고, 수익률·MDD·승률 등 결과를 표시합니다.
    /// </summary>
    public class BacktestForm : Form
    {
        // ─── 컨트롤 ─────────────────────────────────
        private ComboBox cmbTicker = null!;
        private ComboBox cmbStrategyType = null!;
        private NumericUpDown nudDays = null!;
        private NumericUpDown nudInitAmount = null!;
        private Button btnRun = null!;
        private Panel pnlResult = null!;
        private Label lblReturnRate = null!;
        private Label lblMdd = null!;
        private Label lblWinRate = null!;
        private Label lblTotalTrades = null!;
        private Label lblFinalAmount = null!;
        private Label lblPeriod = null!;
        private DataGridView dgvTrades = null!;
        private ProgressBar progressBar = null!;

        private readonly SessionManager _session;

        public BacktestForm(SessionManager session)
        {
            _session = session;
            InitializeComponent();
            this.BackColor = AppTheme.BgMain;
        }

        private void InitializeComponent()
        {
            this.Text = "백테스팅 — 퀀트 전략 검증";
            this.Size = new Size(820, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 20;
            int labelW = 100;
            int controlX = 120;

            // ─── 종목 선택 ──────────────────────────────
            this.Controls.Add(new Label
            {
                Text = "종목:", Location = new Point(20, y + 3),
                Size = new Size(labelW, 20), ForeColor = Color.White
            });
            cmbTicker = new ComboBox
            {
                Location = new Point(controlX, y), Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbTicker.Items.AddRange(new[] { "SCHD", "QQQM", "GLD", "JEPI", "SPLG" });
            cmbTicker.SelectedIndex = 1; // QQQM
            this.Controls.Add(cmbTicker);

            // ─── 전략 유형 ──────────────────────────────
            this.Controls.Add(new Label
            {
                Text = "전략 유형:", Location = new Point(260, y + 3),
                Size = new Size(80, 20), ForeColor = Color.White
            });
            cmbStrategyType = new ComboBox
            {
                Location = new Point(345, y), Size = new Size(160, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStrategyType.Items.AddRange(new[] { "MEAN_REVERSION", "MOMENTUM", "MIXED" });
            cmbStrategyType.SelectedIndex = 0;
            this.Controls.Add(cmbStrategyType);

            // ─── 기간 ──────────────────────────────
            y += 40;
            this.Controls.Add(new Label
            {
                Text = "기간 (일):", Location = new Point(20, y + 3),
                Size = new Size(labelW, 20), ForeColor = Color.White
            });
            nudDays = new NumericUpDown
            {
                Location = new Point(controlX, y), Size = new Size(120, 25),
                Minimum = 90, Maximum = 3650, Value = 365, Increment = 30
            };
            this.Controls.Add(nudDays);

            // ─── 초기 투자금 ──────────────────────────────
            this.Controls.Add(new Label
            {
                Text = "초기 투자금:", Location = new Point(260, y + 3),
                Size = new Size(80, 20), ForeColor = Color.White
            });
            nudInitAmount = new NumericUpDown
            {
                Location = new Point(345, y), Size = new Size(160, 25),
                Minimum = 100_000, Maximum = 1_000_000_000, Value = 10_000_000,
                Increment = 1_000_000, ThousandsSeparator = true
            };
            this.Controls.Add(nudInitAmount);

            // ─── 실행 버튼 ──────────────────────────────
            y += 40;
            btnRun = new Button
            {
                Text = "🚀 백테스트 실행", Location = new Point(20, y),
                Size = new Size(200, 35), FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10, FontStyle.Bold)
            };
            btnRun.Click += BtnRun_Click;
            this.Controls.Add(btnRun);

            progressBar = new ProgressBar
            {
                Location = new Point(240, y + 5), Size = new Size(270, 25),
                Style = ProgressBarStyle.Marquee, Visible = false
            };
            this.Controls.Add(progressBar);

            // ─── 결과 패널 ──────────────────────────────
            y += 50;
            pnlResult = new Panel
            {
                Location = new Point(20, y), Size = new Size(760, 130),
                BackColor = Color.FromArgb(35, 35, 45), Visible = false
            };
            this.Controls.Add(pnlResult);

            // 결과 라벨들
            int ry = 15;
            lblPeriod = CreateResultLabel(pnlResult, "기간:", 15, ry);
            lblReturnRate = CreateResultLabel(pnlResult, "수익률:", 15, ry + 30);
            lblMdd = CreateResultLabel(pnlResult, "MDD:", 260, ry + 30);
            lblFinalAmount = CreateResultLabel(pnlResult, "최종 금액:", 500, ry + 30);
            lblTotalTrades = CreateResultLabel(pnlResult, "거래:", 15, ry + 60);
            lblWinRate = CreateResultLabel(pnlResult, "승률:", 260, ry + 60);

            // ─── 거래 내역 DataGridView ──────────────────
            y += 145;
            dgvTrades = new DataGridView
            {
                Location = new Point(20, y), Size = new Size(760, this.ClientSize.Height - y - 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.White, GridColor = Color.FromArgb(60, 60, 70),
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 55),
                    ForeColor = Color.White,
                    Font = new Font("맑은 고딕", 9, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 30, 40),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 100, 180)
                }
            };
            dgvTrades.Columns.Add("Date", "날짜");
            dgvTrades.Columns.Add("Ticker", "종목");
            dgvTrades.Columns.Add("Action", "매매");
            dgvTrades.Columns.Add("Price", "가격($)");
            dgvTrades.Columns.Add("Qty", "수량");
            dgvTrades.Columns.Add("PnL", "손익(₩)");
            dgvTrades.Columns.Add("Reason", "판단 근거");
            this.Controls.Add(dgvTrades);
        }

        private Label CreateResultLabel(Panel parent, string prefix, int x, int y)
        {
            var lbl = new Label
            {
                Text = prefix, Location = new Point(x, y),
                Size = new Size(230, 22), ForeColor = Color.FromArgb(180, 200, 220),
                Font = new Font("맑은 고딕", 10)
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private async void BtnRun_Click(object? sender, EventArgs e)
        {
            btnRun.Enabled = false;
            progressBar.Visible = true;
            pnlResult.Visible = false;
            dgvTrades.Rows.Clear();

            try
            {
                var client = _session.GetClient();
                string ticker = cmbTicker.SelectedItem?.ToString() ?? "QQQM";
                string strategyType = cmbStrategyType.SelectedItem?.ToString() ?? "MEAN_REVERSION";
                int days = (int)nudDays.Value;
                decimal initAmount = nudInitAmount.Value;

                var engine = new BacktestEngine(client, initAmount);
                var result = await engine.RunAsync(ticker, "백테스트", strategyType, days);

                // 결과 표시
                pnlResult.Visible = true;
                lblPeriod.Text = $"기간: {result.StartDate:yyyy-MM-dd} ~ {result.EndDate:yyyy-MM-dd}";
                lblFinalAmount.Text = $"최종 금액: {result.FinalAmount:N0}원";

                // 수익률 색상
                lblReturnRate.Text = $"수익률: {result.ReturnRate:F2}%";
                lblReturnRate.ForeColor = result.ReturnRate >= 0
                    ? Color.FromArgb(100, 220, 100)
                    : Color.FromArgb(255, 100, 100);

                lblMdd.Text = $"MDD: -{result.MaxDrawdown:F2}%";
                lblMdd.ForeColor = Color.FromArgb(255, 150, 50);

                lblTotalTrades.Text = $"거래: {result.TotalTrades}회";
                lblWinRate.Text = $"승률: {result.WinRate:F1}% ({result.WinTrades}/{result.TotalTrades / 2})";

                // 거래 내역
                foreach (var trade in result.Trades)
                {
                    var row = dgvTrades.Rows.Add(
                        trade.Date.ToString("yyyy-MM-dd"),
                        trade.Ticker,
                        trade.Action,
                        $"${trade.Price:F2}",
                        trade.Qty,
                        trade.ProfitLoss != 0 ? $"{trade.ProfitLoss:N0}" : "-",
                        trade.Reason.Length > 40 ? trade.Reason.Substring(0, 40) + "..." : trade.Reason
                    );

                    // 매수=파란색, 매도=빨간색
                    if (trade.Action == "BUY")
                        dgvTrades.Rows[row].DefaultCellStyle.ForeColor = Color.FromArgb(100, 180, 255);
                    else
                        dgvTrades.Rows[row].DefaultCellStyle.ForeColor =
                            trade.ProfitLoss >= 0 ? Color.FromArgb(100, 220, 100) : Color.FromArgb(255, 100, 100);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Backtest] 실행 오류: {ex.Message}");
                MessageBox.Show($"백테스트 실행 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRun.Enabled = true;
                progressBar.Visible = false;
            }
        }
    }
}
