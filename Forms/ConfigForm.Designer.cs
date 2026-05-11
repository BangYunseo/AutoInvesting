namespace AutoInvest.Forms
{
    partial class ConfigForm
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
            this.lbl_amount = new System.Windows.Forms.Label();
            this.txt_amount = new System.Windows.Forms.TextBox();
            this.lbl_strategyType = new System.Windows.Forms.Label();
            this.cmb_strategyType = new System.Windows.Forms.ComboBox();
            this.lbl_schedule = new System.Windows.Forms.Label();
            this.txt_schedule = new System.Windows.Forms.TextBox();
            this.chk_paper = new System.Windows.Forms.CheckBox();
            this.btn_save = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lbl_title
            //
            this.lbl_title.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lbl_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.lbl_title.Location = new System.Drawing.Point(20, 15);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Size = new System.Drawing.Size(200, 30);
            this.lbl_title.Text = "환경 설정";
            //
            // lbl_amount
            //
            this.lbl_amount.Text = "월 투자금액 (원)";
            this.lbl_amount.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_amount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_amount.Location = new System.Drawing.Point(30, 65);
            this.lbl_amount.AutoSize = true;
            //
            // txt_amount
            //
            this.txt_amount.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.txt_amount.ForeColor = System.Drawing.Color.White;
            this.txt_amount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_amount.Location = new System.Drawing.Point(30, 90);
            this.txt_amount.Size = new System.Drawing.Size(300, 27);
            this.txt_amount.Font = new System.Drawing.Font("맑은 고딕", 11F);
            //
            // lbl_strategyType
            //
            this.lbl_strategyType.Text = "퀀트 전략 유형";
            this.lbl_strategyType.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_strategyType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_strategyType.Location = new System.Drawing.Point(30, 135);
            this.lbl_strategyType.AutoSize = true;
            //
            // cmb_strategyType
            //
            this.cmb_strategyType.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.cmb_strategyType.ForeColor = System.Drawing.Color.White;
            this.cmb_strategyType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmb_strategyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_strategyType.Location = new System.Drawing.Point(30, 160);
            this.cmb_strategyType.Size = new System.Drawing.Size(300, 27);
            this.cmb_strategyType.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.cmb_strategyType.Items.AddRange(new object[] { "MEAN_REVERSION", "MOMENTUM", "MIXED" });
            //
            // lbl_schedule
            //
            this.lbl_schedule.Text = "자동 주문 시각 (HH:mm)";
            this.lbl_schedule.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_schedule.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_schedule.Location = new System.Drawing.Point(30, 210);
            this.lbl_schedule.AutoSize = true;
            //
            // txt_schedule
            //
            this.txt_schedule.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.txt_schedule.ForeColor = System.Drawing.Color.White;
            this.txt_schedule.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_schedule.Location = new System.Drawing.Point(30, 235);
            this.txt_schedule.Size = new System.Drawing.Size(120, 27);
            this.txt_schedule.Font = new System.Drawing.Font("맑은 고딕", 11F);
            //
            // chk_paper
            //
            this.chk_paper.Text = "모의투자 모드 (체크 해제 시 실거래)";
            this.chk_paper.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.chk_paper.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.chk_paper.Location = new System.Drawing.Point(30, 285);
            this.chk_paper.AutoSize = true;
            //
            // btn_save
            //
            this.btn_save.Text = "저장";
            this.btn_save.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(136)))));
            this.btn_save.ForeColor = System.Drawing.Color.White;
            this.btn_save.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_save.FlatAppearance.BorderSize = 0;
            this.btn_save.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btn_save.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_save.Location = new System.Drawing.Point(30, 340);
            this.btn_save.Size = new System.Drawing.Size(140, 36);
            this.btn_save.UseVisualStyleBackColor = false;
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            //
            // btn_cancel
            //
            this.btn_cancel.Text = "취소";
            this.btn_cancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.btn_cancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_cancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_cancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(100)))), ((int)(((byte)(110)))));
            this.btn_cancel.FlatAppearance.BorderSize = 1;
            this.btn_cancel.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btn_cancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_cancel.Location = new System.Drawing.Point(185, 340);
            this.btn_cancel.Size = new System.Drawing.Size(140, 36);
            this.btn_cancel.UseVisualStyleBackColor = false;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            //
            // ConfigForm
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.ClientSize = new System.Drawing.Size(400, 410);
            this.MinimumSize = new System.Drawing.Size(400, 410);
            this.Text = "환경 설정";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lbl_title,
                this.lbl_amount, 
                this.txt_amount,
                this.lbl_strategyType,
                this.cmb_strategyType,
                this.lbl_schedule, 
                this.txt_schedule,
                this.chk_paper, 
                this.btn_save, 
                this.btn_cancel
            });

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.Label lbl_amount;
        private System.Windows.Forms.TextBox txt_amount;
        private System.Windows.Forms.Label lbl_strategyType;
        private System.Windows.Forms.ComboBox cmb_strategyType;
        private System.Windows.Forms.Label lbl_schedule;
        private System.Windows.Forms.TextBox txt_schedule;
        private System.Windows.Forms.CheckBox chk_paper;
        private System.Windows.Forms.Button btn_save;
        private System.Windows.Forms.Button btn_cancel;
    }
}