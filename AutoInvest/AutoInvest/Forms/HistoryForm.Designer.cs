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
            this.lbl_title = new System.Windows.Forms.Label();
            this.lvw_history = new System.Windows.Forms.ListView();
            this.col_date = new System.Windows.Forms.ColumnHeader();
            this.col_ticker = new System.Windows.Forms.ColumnHeader();
            this.col_type = new System.Windows.Forms.ColumnHeader();
            this.col_qty = new System.Windows.Forms.ColumnHeader();
            this.col_price = new System.Windows.Forms.ColumnHeader();
            this.col_status = new System.Windows.Forms.ColumnHeader();
            this.btn_refresh = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lbl_title
            //
            this.lbl_title.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lbl_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.lbl_title.Location = new System.Drawing.Point(20, 15);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Size = new System.Drawing.Size(200, 30);
            this.lbl_title.Text = "거래 내역";
            //
            // ListView columns
            //
            this.col_date.Text = "일시"; this.col_date.Width = 140;
            this.col_ticker.Text = "종목"; this.col_ticker.Width = 80;
            this.col_type.Text = "구분"; this.col_type.Width = 60;
            this.col_qty.Text = "수량"; this.col_qty.Width = 60;
            this.col_price.Text = "단가(USD)"; this.col_price.Width = 90;
            this.col_status.Text = "상태"; this.col_status.Width = 80;
            //
            // lvw_history
            //
            this.lvw_history.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lvw_history.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.lvw_history.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lvw_history.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.col_date, this.col_ticker, this.col_type,
                this.col_qty, this.col_price, this.col_status });
            this.lvw_history.FullRowSelect = true;
            this.lvw_history.GridLines = true;
            this.lvw_history.View = System.Windows.Forms.View.Details;
            this.lvw_history.Location = new System.Drawing.Point(0, 60);
            this.lvw_history.Size = new System.Drawing.Size(660, 440);
            this.lvw_history.Name = "lvw_history";
            //
            // btn_refresh
            //
            this.btn_refresh.Text = "새로고침";
            this.btn_refresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.btn_refresh.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_refresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_refresh.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(100)))), ((int)(((byte)(110)))));
            this.btn_refresh.FlatAppearance.BorderSize = 1;
            this.btn_refresh.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btn_refresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_refresh.Location = new System.Drawing.Point(530, 15);
            this.btn_refresh.Size = new System.Drawing.Size(110, 32);
            this.btn_refresh.UseVisualStyleBackColor = false;
            this.btn_refresh.Click += new System.EventHandler(this.btn_refresh_Click);
            //
            // HistoryForm
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.ClientSize = new System.Drawing.Size(660, 500);
            this.MinimumSize = new System.Drawing.Size(660, 500);
            this.Text = "거래 내역";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lbl_title, this.lvw_history, this.btn_refresh });

            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label lbl_title;
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