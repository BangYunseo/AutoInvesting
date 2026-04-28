using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoInvest.Controls
{
    public partial class AllocationCardControl : UserControl
    {
        public AllocationCardControl()
        {
            InitializeComponent();
        }

        public void SetData(string ticker, double weight, int qty, decimal amount)
        {
            lbl_ticker.Text = ticker;
            lbl_weight.Text = $"{weight:P0}";
            lbl_qty.Text = $"{qty}주";
            lbl_amount.Text = $"{amount:N0}원";
            pnl_bar_fg.Width = (int)(176 * weight);

            // 우측 정렬은 AutoSize 후 위치 보정
            lbl_qty.Left = this.Width - lbl_qty.PreferredWidth - 12;
            lbl_amount.Left = this.Width - lbl_amount.PreferredWidth - 12;
        }
    }
}
