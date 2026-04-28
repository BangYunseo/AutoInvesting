namespace AutoInvest.Forms
{
    partial class HistoryForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lvw_history = new System.Windows.Forms.ListView();
            this.col_date = new System.Windows.Forms.ColumnHeader();
            this.col_ticker = new System.Windows.Forms.ColumnHeader();
            this.col_type = new System.Windows.Forms.ColumnHeader();
            this.col_qty = new System.Windows.Forms.ColumnHeader();
            this.col_price = new System.Windows.Forms.ColumnHeader();
            this.col_status = new System.Windows.Forms.ColumnHeader();
            this.btn_refresh = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // ListView 컬럼
            this.col_date.Text = "일시"; this.col_date.Width = 140;
            this.col_ticker.Text = "종목"; this.col_ticker.Width = 80;
            this.col_type.Text = "구분"; this.col_type.Width = 60;
            this.col_qty.Text = "수량"; this.col_qty.Width = 60;
            this.col_price.Text = "단가(USD)"; this.col_price.Width = 90;
            this.col_status.Text = "상태"; this.col_status.Width = 80;

            this.lvw_history.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.col_date, this.col_ticker, this.col_type,
                this.col_qty, this.col_price, this.col_status });
            this.lvw_history.FullRowSelect = true;
            this.lvw_history.GridLines = true;
            this.lvw_history.View = System.Windows.Forms.View.Details;
            this.lvw_history.Location = new System.Drawing.Point(0, 80);
            this.lvw_history.Size = new System.Drawing.Size(660, 460);
            this.lvw_history.Name = "lvw_history";

            // btn_refresh
            this.btn_refresh.Text = "새로고침";
            this.btn_refresh.Location = new System.Drawing.Point(550, 36);
            this.btn_refresh.Size = new System.Drawing.Size(100, 36);
            this.btn_refresh.Click += new System.EventHandler(this.btn_refresh_Click);

            // HistoryForm
            this.ClientSize = new System.Drawing.Size(660, 540);
            this.MinimumSize = new System.Drawing.Size(660, 540);
            this.Text = "거래 내역";
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lvw_history, this.btn_refresh });

            this.ResumeLayout(false);
        }

        private System.Windows.Forms.ListView lvw_history;
        private System.Windows.Forms.ColumnHeader col_date;
        private System.Windows.Forms.ColumnHeader col_ticker;
        private System.Windows.Forms.ColumnHeader col_type;
        private System.Windows.Forms.ColumnHeader col_qty;
        private System.Windows.Forms.ColumnHeader col_price;
        private System.Windows.Forms.ColumnHeader col_status;
        private System.Windows.Forms.Button btn_refresh;
    }
}