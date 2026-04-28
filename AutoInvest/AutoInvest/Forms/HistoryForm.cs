using AutoInvest.Data.DAO;
using System;
using System.Windows.Forms;

namespace AutoInvest.Forms
{
    public partial class HistoryForm : Form
    {
        public HistoryForm()
        {
            InitializeComponent();
            this.Load += (s, e) => LoadHistory();
        }

        private void LoadHistory()
        {
            lvw_history.Items.Clear();
            var list = TradeHistoryDAO.GetRecent(50);

            foreach (var h in list)
            {
                var item = new ListViewItem(h.TradeDate.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(h.Ticker);
                item.SubItems.Add(h.OrderType);
                item.SubItems.Add($"{h.Qty}주");
                item.SubItems.Add($"{h.Price:N2}");
                item.SubItems.Add(h.Status);
                lvw_history.Items.Add(item);
            }
        }

        private void btn_refresh_Click(object sender, EventArgs e)
            => LoadHistory();
    }
}