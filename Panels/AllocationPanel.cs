using AutoInvest.Core;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Panels
{
    /// <summary>
    /// 배분 설정 패널 — AllocationSetupForm을 UserControl로 변환.
    /// 저장 완료 시 OnSaved 이벤트 발행.
    /// </summary>
    public class AllocationPanel : UserControl
    {
        public event EventHandler? OnSaved;

        private readonly SessionManager _session = new SessionManager();
        private decimal _exchangeRate = 0m;

        private Label lbl_title, lbl_targetLabel, lbl_targetUnit, lbl_tickerLabel;
        private Label lbl_exchangeRate, lbl_totalInvestLabel, lbl_totalInvest;
        private Label lbl_remainingLabel, lbl_remaining;
        private TextBox txt_targetAmount, txt_ticker;
        private Button btn_addTicker, btn_removeTicker, btn_refresh, btn_save;
        private DataGridView dgv_allocation;
        private Panel pnl_summary;

        public AllocationPanel()
        {
            InitializeUI();
            this.Load += AllocationPanel_Load;
        }

        private async void AllocationPanel_Load(object? sender, EventArgs e)
        {
            txt_targetAmount.Text = AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000");

            var client = _session.GetClient();
            if (!client.IsLoggedIn)
                await client.LoginAsync();

            // 실시간 환율 조회
            try
            {
                _exchangeRate = await ExchangeRateService.GetUsdKrwAsync();
            }
            catch
            {
                _exchangeRate = await client.GetExchangeRateAsync();
            }
            lbl_exchangeRate.Text = $"환율: 1 USD = {_exchangeRate:N0} KRW";

            LoadExistingStrategy();
        }

        private async void LoadExistingStrategy()
        {
            var strategies = StrategyDAO.GetStrategy("사용자정의");
            if (strategies.Count == 0) return;

            var client = _session.GetClient();

            foreach (var s in strategies)
            {
                try
                {
                    decimal priceUsd = await client.GetCurrentPriceAsync(s.Ticker);
                    decimal priceKrw = Math.Round(priceUsd * _exchangeRate, 0);

                    dgv_allocation.Rows.Add(
                        s.Ticker,
                        $"{priceUsd:N2}",
                        $"{priceKrw:N0}",
                        s.Qty.ToString(),
                        $"{priceKrw * s.Qty:N0}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Warn($"기존 전략 로드 실패 ({s.Ticker}): {ex.Message}");
                }
            }

            RecalculateSummary();
        }

        private async void btn_addTicker_Click(object? sender, EventArgs e)
        {
            string ticker = txt_ticker.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(ticker))
            {
                MessageBox.Show("종목 코드를 입력해주세요.", "입력 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (DataGridViewRow row in dgv_allocation.Rows)
            {
                if (row.Cells["col_ticker"].Value?.ToString() == ticker)
                {
                    MessageBox.Show($"{ticker}는 이미 추가된 종목입니다.", "중복",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                btn_addTicker.Enabled = false;
                btn_addTicker.Text = "조회중..";

                var client = _session.GetClient();
                decimal priceUsd = await client.GetCurrentPriceAsync(ticker);
                decimal priceKrw = Math.Round(priceUsd * _exchangeRate, 0);

                dgv_allocation.Rows.Add(ticker, $"{priceUsd:N2}", $"{priceKrw:N0}", "0", "0");
                txt_ticker.Clear();
                txt_ticker.Focus();
                Logger.Info($"[배분설정] 종목 추가: {ticker} (${priceUsd:N2})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"종목 가격 조회 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn_addTicker.Enabled = true;
                btn_addTicker.Text = "추가";
            }
        }

        private void btn_removeTicker_Click(object? sender, EventArgs e)
        {
            if (dgv_allocation.SelectedRows.Count == 0)
            {
                MessageBox.Show("삭제할 종목을 선택해주세요.", "선택 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (DataGridViewRow row in dgv_allocation.SelectedRows)
            {
                if (!row.IsNewRow)
                {
                    string? ticker = row.Cells["col_ticker"].Value?.ToString();
                    dgv_allocation.Rows.Remove(row);
                    Logger.Info($"[배분설정] 종목 삭제: {ticker}");
                }
            }

            RecalculateSummary();
        }

        private void dgv_allocation_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (dgv_allocation.Columns[e.ColumnIndex].Name != "col_qty") return;

            var row = dgv_allocation.Rows[e.RowIndex];
            string qtyStr = row.Cells["col_qty"].Value?.ToString() ?? "0";

            if (!int.TryParse(qtyStr, out int qty) || qty < 0)
            {
                row.Cells["col_qty"].Value = "0";
                qty = 0;
            }

            string priceKrwStr = row.Cells["col_priceKrw"].Value?.ToString()?.Replace(",", "") ?? "0";
            if (decimal.TryParse(priceKrwStr, out decimal priceKrw))
                row.Cells["col_amountKrw"].Value = $"{priceKrw * qty:N0}";

            RecalculateSummary();
        }

        private void RecalculateSummary()
        {
            decimal totalInvest = 0;

            foreach (DataGridViewRow row in dgv_allocation.Rows)
            {
                string amountStr = row.Cells["col_amountKrw"].Value?.ToString()?.Replace(",", "") ?? "0";
                if (decimal.TryParse(amountStr, out decimal amount))
                    totalInvest += amount;
            }

            decimal targetAmount = 0;
            string targetStr = txt_targetAmount.Text.Replace(",", "");
            decimal.TryParse(targetStr, out targetAmount);
            decimal remaining = targetAmount - totalInvest;

            lbl_totalInvest.Text = $"{totalInvest:N0}원";

            if (remaining < 0)
            {
                lbl_remaining.Text = $"{remaining:N0}원 ⚠ 초과";
                lbl_remaining.ForeColor = Color.FromArgb(255, 82, 82);
            }
            else
            {
                lbl_remaining.Text = $"{remaining:N0}원";
                lbl_remaining.ForeColor = AppTheme.Success;
            }
        }

        private async void btn_refresh_Click(object? sender, EventArgs e)
        {
            try
            {
                btn_refresh.Enabled = false;
                btn_refresh.Text = "조회중...";
                var client = _session.GetClient();

                try { _exchangeRate = await ExchangeRateService.GetUsdKrwAsync(); }
                catch { _exchangeRate = await client.GetExchangeRateAsync(); }
                lbl_exchangeRate.Text = $"환율: 1 USD = {_exchangeRate:N0} KRW";

                foreach (DataGridViewRow row in dgv_allocation.Rows)
                {
                    string? ticker = row.Cells["col_ticker"].Value?.ToString();
                    if (string.IsNullOrEmpty(ticker)) continue;

                    decimal priceUsd = await client.GetCurrentPriceAsync(ticker);
                    decimal priceKrw = Math.Round(priceUsd * _exchangeRate, 0);

                    row.Cells["col_priceUsd"].Value = $"{priceUsd:N2}";
                    row.Cells["col_priceKrw"].Value = $"{priceKrw:N0}";

                    string qtyStr = row.Cells["col_qty"].Value?.ToString() ?? "0";
                    if (int.TryParse(qtyStr, out int qty))
                        row.Cells["col_amountKrw"].Value = $"{priceKrw * qty:N0}";
                }

                RecalculateSummary();
                Logger.Info("[배분설정] 가격 새로고침 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가격 새로고침 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn_refresh.Enabled = true;
                btn_refresh.Text = "🔄 새로고침";
            }
        }

        private void btn_save_Click(object? sender, EventArgs e)
        {
            if (dgv_allocation.Rows.Count == 0)
            {
                MessageBox.Show("종목을 1개 이상 추가해주세요.", "저장 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var items = new List<StrategyDto>();
            bool hasZeroQty = false;

            foreach (DataGridViewRow row in dgv_allocation.Rows)
            {
                string? ticker = row.Cells["col_ticker"].Value?.ToString();
                string qtyStr = row.Cells["col_qty"].Value?.ToString() ?? "0";
                int.TryParse(qtyStr, out int qty);

                if (qty <= 0) { hasZeroQty = true; continue; }

                items.Add(new StrategyDto
                {
                    StrategyName = "사용자정의",
                    Ticker = ticker ?? string.Empty,
                    Qty = qty
                });
            }

            if (items.Count == 0)
            {
                MessageBox.Show("수량이 1 이상인 종목이 없습니다.", "저장 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (hasZeroQty)
            {
                var result = MessageBox.Show(
                    "수량이 0인 종목은 저장에서 제외됩니다. 계속하시겠습니까?",
                    "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;
            }

            try
            {
                StrategyDAO.SaveStrategy("사용자정의", items);
                AppConfigManager.Set("ACTIVE_STRATEGY", "사용자정의");

                string targetStr = txt_targetAmount.Text.Replace(",", "");
                if (decimal.TryParse(targetStr, out decimal target))
                    AppConfigManager.Set("INVEST_AMOUNT_KRW", ((int)target).ToString());

                Logger.Info($"[배분설정] 사용자정의 전략 저장 완료 ({items.Count}종목)");
                MessageBox.Show($"배분 설정이 저장되었습니다.\n({items.Count}종목)",
                    "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeUI()
        {
            this.BackColor = AppTheme.BgMain;
            this.Dock = DockStyle.Fill;

            lbl_title = new Label { Text = "배분 설정", Font = new Font("맑은 고딕", 14F, FontStyle.Bold), ForeColor = AppTheme.FgPrimary, Location = new Point(20, 15), Size = new Size(200, 30) };
            lbl_targetLabel = new Label { Text = "목표 금액:", Font = new Font("맑은 고딕", 10F), ForeColor = AppTheme.FgSecondary, Location = new Point(20, 60), Size = new Size(80, 25), TextAlign = ContentAlignment.MiddleLeft };
            txt_targetAmount = new TextBox { BackColor = AppTheme.BgInput, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("맑은 고딕", 11F), Location = new Point(105, 58), Size = new Size(160, 27), TextAlign = HorizontalAlignment.Right, Text = "0" };
            txt_targetAmount.TextChanged += (s, e) => RecalculateSummary();
            lbl_targetUnit = new Label { Text = "원", Font = new Font("맑은 고딕", 10F), ForeColor = AppTheme.FgSecondary, Location = new Point(270, 60), Size = new Size(30, 25), TextAlign = ContentAlignment.MiddleLeft };
            lbl_tickerLabel = new Label { Text = "종목 추가:", Font = new Font("맑은 고딕", 10F), ForeColor = AppTheme.FgSecondary, Location = new Point(20, 100), Size = new Size(80, 25), TextAlign = ContentAlignment.MiddleLeft };
            txt_ticker = new TextBox { BackColor = AppTheme.BgInput, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, CharacterCasing = CharacterCasing.Upper, Font = new Font("맑은 고딕", 11F), Location = new Point(105, 98), Size = new Size(100, 27) };

            btn_addTicker = new Button { Text = "추가", BackColor = AppTheme.BtnPrimary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 9F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(215, 97), Size = new Size(60, 28) };
            btn_addTicker.FlatAppearance.BorderSize = 0;
            btn_addTicker.Click += btn_addTicker_Click;

            btn_removeTicker = new Button { Text = "선택 삭제", BackColor = AppTheme.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 9F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(285, 97), Size = new Size(80, 28) };
            btn_removeTicker.FlatAppearance.BorderSize = 0;
            btn_removeTicker.Click += btn_removeTicker_Click;

            // DataGridView
            dgv_allocation = new DataGridView
            {
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
                BackgroundColor = AppTheme.BgSidebar, BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                GridColor = AppTheme.BgInput,
                Location = new Point(20, 140), Size = new Size(540, 220),
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersHeight = 32, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };

            var headerStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = AppTheme.BgInput, ForeColor = AppTheme.FgSecondary,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                SelectionBackColor = AppTheme.BgInput, SelectionForeColor = AppTheme.FgSecondary
            };
            dgv_allocation.ColumnHeadersDefaultCellStyle = headerStyle;

            var cellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.BgSidebar, ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10F),
                SelectionBackColor = AppTheme.Selection, SelectionForeColor = Color.White
            };
            dgv_allocation.DefaultCellStyle = cellStyle;
            dgv_allocation.RowTemplate.Height = 30;

            dgv_allocation.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "종목", Name = "col_ticker", ReadOnly = true, Width = 80 });
            dgv_allocation.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "단가($)", Name = "col_priceUsd", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv_allocation.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "단가(₩)", Name = "col_priceKrw", ReadOnly = true, Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv_allocation.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "수량", Name = "col_qty", Width = 70, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = AppTheme.Success } });
            dgv_allocation.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "금액(₩)", Name = "col_amountKrw", ReadOnly = true, Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv_allocation.CellEndEdit += dgv_allocation_CellEndEdit;

            lbl_exchangeRate = new Label { Text = "환율: 조회 중...", Font = new Font("맑은 고딕", 9F), ForeColor = AppTheme.FgMuted, Location = new Point(20, 370), Size = new Size(255, 20) };

            btn_refresh = new Button { Text = "🔄 새로고침", BackColor = AppTheme.BtnSecondary, ForeColor = AppTheme.FgSecondary, FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 8F), Cursor = Cursors.Hand, Location = new Point(450, 366), Size = new Size(110, 26) };
            btn_refresh.FlatAppearance.BorderColor = AppTheme.BtnBorder;
            btn_refresh.Click += btn_refresh_Click;

            pnl_summary = new Panel { BackColor = Color.FromArgb(30, 40, 48), Location = new Point(20, 400), Size = new Size(540, 45) };
            lbl_totalInvestLabel = new Label { Text = "총 투자금 :", Font = new Font("맑은 고딕", 10F), ForeColor = AppTheme.FgSecondary, Location = new Point(10, 12), Size = new Size(80, 22) };
            lbl_totalInvest = new Label { Text = "0원", Font = new Font("맑은 고딕", 11F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(89, 13), Size = new Size(160, 22) };
            lbl_remainingLabel = new Label { Text = "잔여금 :", Font = new Font("맑은 고딕", 10F), ForeColor = AppTheme.FgSecondary, Location = new Point(280, 12), Size = new Size(60, 22) };
            lbl_remaining = new Label { Text = "0원", Font = new Font("맑은 고딕", 11F, FontStyle.Bold), ForeColor = AppTheme.Success, Location = new Point(342, 12), Size = new Size(180, 22) };
            pnl_summary.Controls.AddRange(new Control[] { lbl_totalInvestLabel, lbl_totalInvest, lbl_remainingLabel, lbl_remaining });

            btn_save = new Button { Text = "저장", BackColor = AppTheme.BtnPrimary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 11F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(450, 455), Size = new Size(110, 35) };
            btn_save.FlatAppearance.BorderSize = 0;
            btn_save.Click += btn_save_Click;

            this.Controls.AddRange(new Control[] {
                lbl_title, lbl_targetLabel, txt_targetAmount, lbl_targetUnit,
                lbl_tickerLabel, txt_ticker, btn_addTicker, btn_removeTicker,
                dgv_allocation, lbl_exchangeRate, btn_refresh, pnl_summary, btn_save
            });
        }
    }
}
