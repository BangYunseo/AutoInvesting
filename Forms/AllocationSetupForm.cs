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
    /// <summary>
    /// 배분 설정 Form.
    /// 사용자가 종목과 수량을 입력하면, IBrokerClient를 통해 실시간 단가를 조회하고
    /// 단가 × 수량 × 환율로 금액을 자동 계산합니다.
    ///
    /// TODO [Phase 3] LS증권 API 연동 시 GetCurrentPriceAsync → 실제 시세 조회
    /// TODO [Phase 3] LS증권 환율 API 연동 시 GetExchangeRateAsync → 실제 환율 조회
    /// TODO [Phase 4] AI 추천 종목/수량을 이 Form에 자동 입력하는 기능
    /// </summary>
    public partial class AllocationSetupForm : Form
    {
        private readonly SessionManager _session = new SessionManager();
        private decimal _exchangeRate = 0m;

        public AllocationSetupForm()
        {
            InitializeComponent();
            this.Load += AllocationSetupForm_Load;
            this.txt_targetAmount.TextChanged += (s, e) => RecalculateSummary();
        }

        private async void AllocationSetupForm_Load(object? sender, EventArgs e)
        {
            // 목표 금액 기본값
            txt_targetAmount.Text = AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000");

            // 환율 조회
            var client = _session.GetClient();
            if (!client.IsLoggedIn)
                await client.LoginAsync();

            _exchangeRate = await client.GetExchangeRateAsync();
            lbl_exchangeRate.Text = $"환율: 1 USD = {_exchangeRate:N0} KRW";

            // 기존 사용자정의 전략 로드
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

                    int qty = s.Qty;

                    dgv_allocation.Rows.Add(
                        s.Ticker,
                        $"{priceUsd:N2}",
                        $"{priceKrw:N0}",
                        qty.ToString(),
                        $"{priceKrw * qty:N0}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Warn($"기존 전략 로드 실패 ({s.Ticker}): {ex.Message}");
                }
            }

            RecalculateSummary();
        }

        // ─── 종목 추가 ──────────────────────────────────────

        private async void btn_addTicker_Click(object? sender, EventArgs e)
        {
            string ticker = txt_ticker.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(ticker))
            {
                MessageBox.Show("종목 코드를 입력해주세요.", "입력 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 중복 체크
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

                dgv_allocation.Rows.Add(
                    ticker,
                    $"{priceUsd:N2}",
                    $"{priceKrw:N0}",
                    "0",         // 수량 기본값
                    "0"          // 금액 기본값
                );

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

        // ─── 종목 삭제 ──────────────────────────────────────

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

        // ─── 수량 변경 시 금액 재계산 ───────────────────────

        private void dgv_allocation_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (dgv_allocation.Columns[e.ColumnIndex].Name != "col_qty")
                return;

            var row = dgv_allocation.Rows[e.RowIndex];
            string qtyStr = row.Cells["col_qty"].Value?.ToString() ?? "0";

            if (!int.TryParse(qtyStr, out int qty) || qty < 0)
            {
                row.Cells["col_qty"].Value = "0";
                qty = 0;
            }

            // 단가(₩) 파싱
            string priceKrwStr = row.Cells["col_priceKrw"].Value?.ToString()?.Replace(",", "") ?? "0";
            if (decimal.TryParse(priceKrwStr, out decimal priceKrw))
            {
                decimal amount = priceKrw * qty;
                row.Cells["col_amountKrw"].Value = $"{amount:N0}";
            }

            RecalculateSummary();
        }

        // ─── 요약 계산 (총 투자금 / 잔여금) ─────────────────

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
                lbl_remaining.ForeColor = Color.FromArgb(255, 82, 82); // 빨간색
            }
            else
            {
                lbl_remaining.Text = $"{remaining:N0}원";
                lbl_remaining.ForeColor = Color.FromArgb(0, 230, 118); // 초록색
            }
        }

        // ─── 가격 새로고침 ──────────────────────────────────

        private async void btn_refresh_Click(object? sender, EventArgs e)
        {
            try
            {
                btn_refresh.Enabled = false;
                btn_refresh.Text = "조회중...";

                var client = _session.GetClient();

                // 환율 재조회
                _exchangeRate = await client.GetExchangeRateAsync();
                lbl_exchangeRate.Text = $"환율: 1 USD = {_exchangeRate:N0} KRW";

                // 전 종목 가격 재조회
                foreach (DataGridViewRow row in dgv_allocation.Rows)
                {
                    string? ticker = row.Cells["col_ticker"].Value?.ToString();
                    if (string.IsNullOrEmpty(ticker)) continue;

                    decimal priceUsd = await client.GetCurrentPriceAsync(ticker);
                    decimal priceKrw = Math.Round(priceUsd * _exchangeRate, 0);

                    row.Cells["col_priceUsd"].Value = $"{priceUsd:N2}";
                    row.Cells["col_priceKrw"].Value = $"{priceKrw:N0}";

                    // 수량에 따라 금액 재계산
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
                btn_refresh.Text = "🔄 가격 새로고침";
            }
        }

        // ─── 저장 ───────────────────────────────────────────

        private void btn_save_Click(object? sender, EventArgs e)
        {
            if (dgv_allocation.Rows.Count == 0)
            {
                MessageBox.Show("종목을 1개 이상 추가해주세요.", "저장 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 수량 0인 종목 체크
            var items = new List<StrategyDto>();
            bool hasZeroQty = false;

            foreach (DataGridViewRow row in dgv_allocation.Rows)
            {
                string? ticker = row.Cells["col_ticker"].Value?.ToString();
                string qtyStr = row.Cells["col_qty"].Value?.ToString() ?? "0";
                int.TryParse(qtyStr, out int qty);

                if (qty <= 0)
                {
                    hasZeroQty = true;
                    continue;
                }

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
                // 전략 저장
                StrategyDAO.SaveStrategy("사용자정의", items);

                // 활성 전략을 사용자정의로 변경
                AppConfigManager.Set("ACTIVE_STRATEGY", "사용자정의");

                // 목표 금액도 저장
                string targetStr = txt_targetAmount.Text.Replace(",", "");
                if (decimal.TryParse(targetStr, out decimal target))
                    AppConfigManager.Set("INVEST_AMOUNT_KRW", ((int)target).ToString());

                Logger.Info($"[배분설정] 사용자정의 전략 저장 완료 ({items.Count}종목)");
                MessageBox.Show($"배분 설정이 저장되었습니다.\n({items.Count}종목)",
                    "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btn_cancel_Click(object? sender, EventArgs e)
            => this.Close();
    }
}
