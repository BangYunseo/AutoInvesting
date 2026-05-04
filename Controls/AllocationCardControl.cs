using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Controls
{
    /// <summary>
    /// 종목별 배분 카드 UserControl.
    /// MainForm의 FlowLayoutPanel(flp_allocation)에 종목별로 하나씩 표시됩니다.
    /// 종목명, 비중(%), 수량(주), 배분 금액(원), 비중 바를 표시합니다.
    /// </summary>
    public partial class AllocationCardControl : UserControl
    {
        public AllocationCardControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 카드에 데이터를 바인딩합니다.
        /// </summary>
        /// <param name="ticker">종목 코드 (예: "QQQM")</param>
        /// <param name="weight">비중 (0.0~1.0, 예: 0.3 = 30%)</param>
        /// <param name="qty">수량 (주)</param>
        /// <param name="amount">배분 금액 (KRW)</param>
        public void SetData(string ticker, double weight, int qty, decimal amount)
        {
            lbl_ticker.Text = ticker;              // 종목명 라벨
            lbl_weight.Text = $"{weight:P0}";      // 비중 라벨 (예: "30%")
            lbl_qty.Text = $"{qty}주";             // 수량 라벨 (예: "5주")
            lbl_amount.Text = $"{amount:N0}원";    // 금액 라벨 (예: "300,000원")

            // 비중 바 — 전체 너비 100px 기준, 비중 비례로 채움
            pnl_bar_fg.Width = (int)(100 * weight);
        }
    }
}
