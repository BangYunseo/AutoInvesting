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
            this.lbl_amount = new System.Windows.Forms.Label();
            this.txt_amount = new System.Windows.Forms.TextBox();
            this.lbl_strategy = new System.Windows.Forms.Label();
            this.rdb_balanced = new System.Windows.Forms.RadioButton();
            this.rdb_aggressive = new System.Windows.Forms.RadioButton();
            this.lbl_schedule = new System.Windows.Forms.Label();
            this.txt_schedule = new System.Windows.Forms.TextBox();
            this.chk_paper = new System.Windows.Forms.CheckBox();
            this.btn_save = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lbl_amount
            //
            this.lbl_amount.Text = "월 투자금액 (원)";
            this.lbl_amount.Font = new System.Drawing.Font("맑은 고딕", 10f);
            this.lbl_amount.Location = new System.Drawing.Point(30, 80);
            this.lbl_amount.AutoSize = true;
            //
            // txt_amount
            //
            this.txt_amount.Location = new System.Drawing.Point(30, 105);
            this.txt_amount.Size = new System.Drawing.Size(300, 28);
            this.txt_amount.Font = new System.Drawing.Font("맑은 고딕", 11f);
            //
            // lbl_strategy
            //
            this.lbl_strategy.Text = "투자 전략";
            this.lbl_strategy.Font = new System.Drawing.Font("맑은 고딕", 10f);
            this.lbl_strategy.Location = new System.Drawing.Point(30, 155);
            this.lbl_strategy.AutoSize = true;
            //
            // rdb_balanced
            //
            this.rdb_balanced.Text = "안정형";
            this.rdb_balanced.Font = new System.Drawing.Font("맑은 고딕", 10f);
            this.rdb_balanced.Location = new System.Drawing.Point(30, 180);
            this.rdb_balanced.AutoSize = true;
            //
            // rdb_aggressive
            //
            this.rdb_aggressive.Text = "공격형";
            this.rdb_aggressive.Font = new System.Drawing.Font("맑은 고딕", 10f);
            this.rdb_aggressive.Location = new System.Drawing.Point(30, 210);
            this.rdb_aggressive.AutoSize = true;
            //
            // lbl_schedule
            //
            this.lbl_schedule.Text = "자동 주문 시각 (HH:mm)";
            this.lbl_schedule.Font = new System.Drawing.Font("맑은 고딕", 10f);
            this.lbl_schedule.Location = new System.Drawing.Point(30, 260);
            this.lbl_schedule.AutoSize = true;
            //
            // txt_schedule
            //
            this.txt_schedule.Location = new System.Drawing.Point(30, 285);
            this.txt_schedule.Size = new System.Drawing.Size(120, 28);
            this.txt_schedule.Font = new System.Drawing.Font("맑은 고딕", 11f);
            //
            // chk_paper
            //
            this.chk_paper.Text = "모의투자 모드 (체크 해제 시 실거래)";
            this.chk_paper.Font = new System.Drawing.Font("맑은 고딕", 10f);
            this.chk_paper.Location = new System.Drawing.Point(30, 335);
            this.chk_paper.AutoSize = true;
            //
            // btn_save
            //
            this.btn_save.Text = "저장";
            this.btn_save.Location = new System.Drawing.Point(30, 390);
            this.btn_save.Size = new System.Drawing.Size(120, 36);
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            //
            // btn_cancel
            //
            this.btn_cancel.Text = "취소";
            this.btn_cancel.Location = new System.Drawing.Point(165, 390);
            this.btn_cancel.Size = new System.Drawing.Size(120, 36);
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            //
            // ConfigForm
            //
            this.ClientSize = new System.Drawing.Size(400, 460);
            this.MinimumSize = new System.Drawing.Size(400, 460);
            this.Text = "환경 설정";
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lbl_amount, 
                this.txt_amount,
                this.lbl_strategy, 
                this.rdb_balanced, 
                this.rdb_aggressive,
                this.lbl_schedule, 
                this.txt_schedule,
                this.chk_paper, 
                this.btn_save, 
                this.btn_cancel
            });

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lbl_amount;
        private System.Windows.Forms.TextBox txt_amount;
        private System.Windows.Forms.Label lbl_strategy;
        private System.Windows.Forms.RadioButton rdb_balanced;
        private System.Windows.Forms.RadioButton rdb_aggressive;
        private System.Windows.Forms.Label lbl_schedule;
        private System.Windows.Forms.TextBox txt_schedule;
        private System.Windows.Forms.CheckBox chk_paper;
        private System.Windows.Forms.Button btn_save;
        private System.Windows.Forms.Button btn_cancel;
    }
}