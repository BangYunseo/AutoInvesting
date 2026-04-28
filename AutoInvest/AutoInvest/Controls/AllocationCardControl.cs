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
            pnl_bar_fg.Width = (int)(100 * weight);
        }
    }
}
