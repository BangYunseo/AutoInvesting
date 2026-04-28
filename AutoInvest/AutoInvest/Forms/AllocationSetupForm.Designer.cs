namespace AutoInvest.Forms
{
    partial class AllocationSetupForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lbl_title = new System.Windows.Forms.Label();
            this.lbl_targetLabel = new System.Windows.Forms.Label();
            this.txt_targetAmount = new System.Windows.Forms.TextBox();
            this.lbl_targetUnit = new System.Windows.Forms.Label();
            this.lbl_tickerLabel = new System.Windows.Forms.Label();
            this.txt_ticker = new System.Windows.Forms.TextBox();
            this.btn_addTicker = new System.Windows.Forms.Button();
            this.btn_removeTicker = new System.Windows.Forms.Button();
            this.dgv_allocation = new System.Windows.Forms.DataGridView();
            this.col_ticker = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_priceUsd = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_priceKrw = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_qty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_amountKrw = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lbl_exchangeRate = new System.Windows.Forms.Label();
            this.btn_refresh = new System.Windows.Forms.Button();
            this.pnl_summary = new System.Windows.Forms.Panel();
            this.lbl_totalInvestLabel = new System.Windows.Forms.Label();
            this.lbl_totalInvest = new System.Windows.Forms.Label();
            this.lbl_remainingLabel = new System.Windows.Forms.Label();
            this.lbl_remaining = new System.Windows.Forms.Label();
            this.btn_save = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_allocation)).BeginInit();
            this.pnl_summary.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbl_title
            // 
            this.lbl_title.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lbl_title.ForeColor = System.Drawing.Color.White;
            this.lbl_title.Location = new System.Drawing.Point(20, 15);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Size = new System.Drawing.Size(200, 30);
            this.lbl_title.TabIndex = 0;
            this.lbl_title.Text = "배분 설정";
            // 
            // lbl_targetLabel
            // 
            this.lbl_targetLabel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_targetLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_targetLabel.Location = new System.Drawing.Point(20, 60);
            this.lbl_targetLabel.Name = "lbl_targetLabel";
            this.lbl_targetLabel.Size = new System.Drawing.Size(80, 25);
            this.lbl_targetLabel.TabIndex = 1;
            this.lbl_targetLabel.Text = "목표 금액:";
            this.lbl_targetLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txt_targetAmount
            // 
            this.txt_targetAmount.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.txt_targetAmount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_targetAmount.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.txt_targetAmount.ForeColor = System.Drawing.Color.White;
            this.txt_targetAmount.Location = new System.Drawing.Point(105, 58);
            this.txt_targetAmount.Name = "txt_targetAmount";
            this.txt_targetAmount.Size = new System.Drawing.Size(160, 27);
            this.txt_targetAmount.TabIndex = 2;
            this.txt_targetAmount.Text = "0";
            this.txt_targetAmount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // lbl_targetUnit
            // 
            this.lbl_targetUnit.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_targetUnit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_targetUnit.Location = new System.Drawing.Point(270, 60);
            this.lbl_targetUnit.Name = "lbl_targetUnit";
            this.lbl_targetUnit.Size = new System.Drawing.Size(30, 25);
            this.lbl_targetUnit.TabIndex = 3;
            this.lbl_targetUnit.Text = "원";
            this.lbl_targetUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbl_tickerLabel
            // 
            this.lbl_tickerLabel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_tickerLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_tickerLabel.Location = new System.Drawing.Point(20, 100);
            this.lbl_tickerLabel.Name = "lbl_tickerLabel";
            this.lbl_tickerLabel.Size = new System.Drawing.Size(80, 25);
            this.lbl_tickerLabel.TabIndex = 4;
            this.lbl_tickerLabel.Text = "종목 추가:";
            this.lbl_tickerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txt_ticker
            // 
            this.txt_ticker.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.txt_ticker.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_ticker.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txt_ticker.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.txt_ticker.ForeColor = System.Drawing.Color.White;
            this.txt_ticker.Location = new System.Drawing.Point(105, 98);
            this.txt_ticker.Name = "txt_ticker";
            this.txt_ticker.Size = new System.Drawing.Size(100, 27);
            this.txt_ticker.TabIndex = 5;
            // 
            // btn_addTicker
            // 
            this.btn_addTicker.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(136)))));
            this.btn_addTicker.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_addTicker.FlatAppearance.BorderSize = 0;
            this.btn_addTicker.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_addTicker.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btn_addTicker.ForeColor = System.Drawing.Color.White;
            this.btn_addTicker.Location = new System.Drawing.Point(215, 97);
            this.btn_addTicker.Name = "btn_addTicker";
            this.btn_addTicker.Size = new System.Drawing.Size(60, 28);
            this.btn_addTicker.TabIndex = 6;
            this.btn_addTicker.Text = "추가";
            this.btn_addTicker.UseVisualStyleBackColor = false;
            this.btn_addTicker.Click += new System.EventHandler(this.btn_addTicker_Click);
            // 
            // btn_removeTicker
            // 
            this.btn_removeTicker.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(183)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.btn_removeTicker.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_removeTicker.FlatAppearance.BorderSize = 0;
            this.btn_removeTicker.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_removeTicker.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btn_removeTicker.ForeColor = System.Drawing.Color.White;
            this.btn_removeTicker.Location = new System.Drawing.Point(285, 97);
            this.btn_removeTicker.Name = "btn_removeTicker";
            this.btn_removeTicker.Size = new System.Drawing.Size(80, 28);
            this.btn_removeTicker.TabIndex = 7;
            this.btn_removeTicker.Text = "선택 삭제";
            this.btn_removeTicker.UseVisualStyleBackColor = false;
            this.btn_removeTicker.Click += new System.EventHandler(this.btn_removeTicker_Click);
            // 
            // dgv_allocation
            // 
            this.dgv_allocation.AllowUserToAddRows = false;
            this.dgv_allocation.AllowUserToDeleteRows = false;
            this.dgv_allocation.AllowUserToResizeRows = false;
            this.dgv_allocation.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.dgv_allocation.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgv_allocation.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgv_allocation.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle13.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            dataGridViewCellStyle13.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle13.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            dataGridViewCellStyle13.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            dataGridViewCellStyle13.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.dgv_allocation.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle13;
            this.dgv_allocation.ColumnHeadersHeight = 32;
            this.dgv_allocation.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv_allocation.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.col_ticker,
            this.col_priceUsd,
            this.col_priceKrw,
            this.col_qty,
            this.col_amountKrw});
            dataGridViewCellStyle18.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle18.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            dataGridViewCellStyle18.Font = new System.Drawing.Font("맑은 고딕", 10F);
            dataGridViewCellStyle18.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle18.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            dataGridViewCellStyle18.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle18.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv_allocation.DefaultCellStyle = dataGridViewCellStyle18;
            this.dgv_allocation.EnableHeadersVisualStyles = false;
            this.dgv_allocation.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.dgv_allocation.Location = new System.Drawing.Point(20, 140);
            this.dgv_allocation.Name = "dgv_allocation";
            this.dgv_allocation.RowHeadersVisible = false;
            this.dgv_allocation.RowTemplate.Height = 30;
            this.dgv_allocation.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_allocation.Size = new System.Drawing.Size(540, 220);
            this.dgv_allocation.TabIndex = 8;
            this.dgv_allocation.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_allocation_CellEndEdit);
            // 
            // col_ticker
            // 
            this.col_ticker.HeaderText = "종목";
            this.col_ticker.Name = "col_ticker";
            this.col_ticker.ReadOnly = true;
            this.col_ticker.Width = 80;
            // 
            // col_priceUsd
            // 
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.col_priceUsd.DefaultCellStyle = dataGridViewCellStyle14;
            this.col_priceUsd.HeaderText = "단가($)";
            this.col_priceUsd.Name = "col_priceUsd";
            this.col_priceUsd.ReadOnly = true;
            // 
            // col_priceKrw
            // 
            dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.col_priceKrw.DefaultCellStyle = dataGridViewCellStyle15;
            this.col_priceKrw.HeaderText = "단가(₩)";
            this.col_priceKrw.Name = "col_priceKrw";
            this.col_priceKrw.ReadOnly = true;
            this.col_priceKrw.Width = 110;
            // 
            // col_qty
            // 
            dataGridViewCellStyle16.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle16.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(230)))), ((int)(((byte)(118)))));
            this.col_qty.DefaultCellStyle = dataGridViewCellStyle16;
            this.col_qty.HeaderText = "수량";
            this.col_qty.Name = "col_qty";
            this.col_qty.Width = 70;
            // 
            // col_amountKrw
            // 
            dataGridViewCellStyle17.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.col_amountKrw.DefaultCellStyle = dataGridViewCellStyle17;
            this.col_amountKrw.HeaderText = "금액(₩)";
            this.col_amountKrw.Name = "col_amountKrw";
            this.col_amountKrw.ReadOnly = true;
            this.col_amountKrw.Width = 130;
            // 
            // lbl_exchangeRate
            // 
            this.lbl_exchangeRate.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_exchangeRate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.lbl_exchangeRate.Location = new System.Drawing.Point(20, 370);
            this.lbl_exchangeRate.Name = "lbl_exchangeRate";
            this.lbl_exchangeRate.Size = new System.Drawing.Size(255, 20);
            this.lbl_exchangeRate.TabIndex = 9;
            this.lbl_exchangeRate.Text = "환율: 조회 중...";
            // 
            // btn_refresh
            // 
            this.btn_refresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.btn_refresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_refresh.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(100)))), ((int)(((byte)(110)))));
            this.btn_refresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_refresh.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.btn_refresh.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_refresh.Location = new System.Drawing.Point(450, 366);
            this.btn_refresh.Name = "btn_refresh";
            this.btn_refresh.Size = new System.Drawing.Size(110, 26);
            this.btn_refresh.TabIndex = 10;
            this.btn_refresh.Text = "Price Refresh";
            this.btn_refresh.UseVisualStyleBackColor = false;
            this.btn_refresh.Click += new System.EventHandler(this.btn_refresh_Click);
            // 
            // pnl_summary
            // 
            this.pnl_summary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(40)))), ((int)(((byte)(48)))));
            this.pnl_summary.Controls.Add(this.lbl_totalInvestLabel);
            this.pnl_summary.Controls.Add(this.lbl_totalInvest);
            this.pnl_summary.Controls.Add(this.lbl_remainingLabel);
            this.pnl_summary.Controls.Add(this.lbl_remaining);
            this.pnl_summary.Location = new System.Drawing.Point(20, 400);
            this.pnl_summary.Name = "pnl_summary";
            this.pnl_summary.Size = new System.Drawing.Size(540, 45);
            this.pnl_summary.TabIndex = 11;
            // 
            // lbl_totalInvestLabel
            // 
            this.lbl_totalInvestLabel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_totalInvestLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_totalInvestLabel.Location = new System.Drawing.Point(10, 12);
            this.lbl_totalInvestLabel.Name = "lbl_totalInvestLabel";
            this.lbl_totalInvestLabel.Size = new System.Drawing.Size(80, 22);
            this.lbl_totalInvestLabel.TabIndex = 0;
            this.lbl_totalInvestLabel.Text = "총 투자금 :";
            // 
            // lbl_totalInvest
            // 
            this.lbl_totalInvest.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lbl_totalInvest.ForeColor = System.Drawing.Color.White;
            this.lbl_totalInvest.Location = new System.Drawing.Point(89, 13);
            this.lbl_totalInvest.Name = "lbl_totalInvest";
            this.lbl_totalInvest.Size = new System.Drawing.Size(160, 22);
            this.lbl_totalInvest.TabIndex = 1;
            this.lbl_totalInvest.Text = "0원";
            // 
            // lbl_remainingLabel
            // 
            this.lbl_remainingLabel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_remainingLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_remainingLabel.Location = new System.Drawing.Point(280, 12);
            this.lbl_remainingLabel.Name = "lbl_remainingLabel";
            this.lbl_remainingLabel.Size = new System.Drawing.Size(60, 22);
            this.lbl_remainingLabel.TabIndex = 2;
            this.lbl_remainingLabel.Text = "잔여금 :";
            // 
            // lbl_remaining
            // 
            this.lbl_remaining.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lbl_remaining.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(230)))), ((int)(((byte)(118)))));
            this.lbl_remaining.Location = new System.Drawing.Point(342, 12);
            this.lbl_remaining.Name = "lbl_remaining";
            this.lbl_remaining.Size = new System.Drawing.Size(180, 22);
            this.lbl_remaining.TabIndex = 3;
            this.lbl_remaining.Text = "0원";
            // 
            // btn_save
            // 
            this.btn_save.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(136)))));
            this.btn_save.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_save.FlatAppearance.BorderSize = 0;
            this.btn_save.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_save.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btn_save.ForeColor = System.Drawing.Color.White;
            this.btn_save.Location = new System.Drawing.Point(350, 455);
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(100, 35);
            this.btn_save.TabIndex = 12;
            this.btn_save.Text = "저장";
            this.btn_save.UseVisualStyleBackColor = false;
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            // 
            // btn_cancel
            // 
            this.btn_cancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.btn_cancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_cancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(100)))), ((int)(((byte)(110)))));
            this.btn_cancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_cancel.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btn_cancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_cancel.Location = new System.Drawing.Point(460, 455);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(100, 35);
            this.btn_cancel.TabIndex = 13;
            this.btn_cancel.Text = "취소";
            this.btn_cancel.UseVisualStyleBackColor = false;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            // 
            // AllocationSetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.ClientSize = new System.Drawing.Size(580, 505);
            this.Controls.Add(this.lbl_title);
            this.Controls.Add(this.lbl_targetLabel);
            this.Controls.Add(this.txt_targetAmount);
            this.Controls.Add(this.lbl_targetUnit);
            this.Controls.Add(this.lbl_tickerLabel);
            this.Controls.Add(this.txt_ticker);
            this.Controls.Add(this.btn_addTicker);
            this.Controls.Add(this.btn_removeTicker);
            this.Controls.Add(this.dgv_allocation);
            this.Controls.Add(this.lbl_exchangeRate);
            this.Controls.Add(this.btn_refresh);
            this.Controls.Add(this.pnl_summary);
            this.Controls.Add(this.btn_save);
            this.Controls.Add(this.btn_cancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AllocationSetupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "배분 설정";
            ((System.ComponentModel.ISupportInitialize)(this.dgv_allocation)).EndInit();
            this.pnl_summary.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.Label lbl_targetLabel;
        private System.Windows.Forms.TextBox txt_targetAmount;
        private System.Windows.Forms.Label lbl_targetUnit;
        private System.Windows.Forms.Label lbl_tickerLabel;
        private System.Windows.Forms.TextBox txt_ticker;
        private System.Windows.Forms.Button btn_addTicker;
        private System.Windows.Forms.Button btn_removeTicker;
        private System.Windows.Forms.DataGridView dgv_allocation;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_ticker;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_priceUsd;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_priceKrw;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_qty;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_amountKrw;
        private System.Windows.Forms.Label lbl_exchangeRate;
        private System.Windows.Forms.Button btn_refresh;
        private System.Windows.Forms.Panel pnl_summary;
        private System.Windows.Forms.Label lbl_totalInvestLabel;
        private System.Windows.Forms.Label lbl_totalInvest;
        private System.Windows.Forms.Label lbl_remainingLabel;
        private System.Windows.Forms.Label lbl_remaining;
        private System.Windows.Forms.Button btn_save;
        private System.Windows.Forms.Button btn_cancel;
    }
}
