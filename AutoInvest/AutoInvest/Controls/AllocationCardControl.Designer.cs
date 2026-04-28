namespace AutoInvest.Controls
{
    partial class AllocationCardControl
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.lbl_ticker = new System.Windows.Forms.Label();
            this.lbl_weight = new System.Windows.Forms.Label();
            this.lbl_qty = new System.Windows.Forms.Label();
            this.lbl_amount = new System.Windows.Forms.Label();
            this.pnl_bar_bg = new System.Windows.Forms.Panel();
            this.pnl_bar_fg = new System.Windows.Forms.Panel();
            this.pnl_bar_bg.SuspendLayout();
            this.SuspendLayout();

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //
            // pnl_bar_bg - 인라인 프로그레스 바
            //
            this.pnl_bar_bg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.pnl_bar_bg.Controls.Add(this.pnl_bar_fg);
            this.pnl_bar_bg.Location = new System.Drawing.Point(90, 14);
            this.pnl_bar_bg.Name = "pnl_bar_bg";
            this.pnl_bar_bg.Size = new System.Drawing.Size(100, 6);
            //
            // pnl_bar_fg
            //
            this.pnl_bar_fg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(130)))), ((int)(((byte)(200)))));
            this.pnl_bar_fg.Location = new System.Drawing.Point(0, 0);
            this.pnl_bar_fg.Name = "pnl_bar_fg";
            this.pnl_bar_fg.Size = new System.Drawing.Size(0, 6);
            //
            // lbl_ticker
            //
            this.lbl_ticker.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lbl_ticker.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.lbl_ticker.Location = new System.Drawing.Point(12, 6);
            this.lbl_ticker.Name = "lbl_ticker";
            this.lbl_ticker.Size = new System.Drawing.Size(70, 22);
            this.lbl_ticker.Text = "TICKER";
            this.lbl_ticker.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lbl_weight (숨김 - 바로 시각적 표현)
            //
            this.lbl_weight.Font = new System.Drawing.Font("맑은 고딕", 1F);
            this.lbl_weight.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.lbl_weight.Location = new System.Drawing.Point(0, 0);
            this.lbl_weight.Name = "lbl_weight";
            this.lbl_weight.Size = new System.Drawing.Size(1, 1);
            this.lbl_weight.Text = "";
            this.lbl_weight.Visible = false;
            //
            // lbl_qty
            //
            this.lbl_qty.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_qty.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lbl_qty.Location = new System.Drawing.Point(220, 6);
            this.lbl_qty.Name = "lbl_qty";
            this.lbl_qty.Size = new System.Drawing.Size(60, 22);
            this.lbl_qty.Text = "0주";
            this.lbl_qty.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lbl_amount
            //
            this.lbl_amount.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_amount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lbl_amount.Location = new System.Drawing.Point(290, 6);
            this.lbl_amount.Name = "lbl_amount";
            this.lbl_amount.Size = new System.Drawing.Size(110, 22);
            this.lbl_amount.Text = "0원";
            this.lbl_amount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // AllocationCardControl
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lbl_ticker, 
                this.pnl_bar_bg,
                this.lbl_qty, 
                this.lbl_amount,
                this.lbl_weight
            });
            this.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.Name = "AllocationCardControl";
            this.Size = new System.Drawing.Size(408, 34);

            this.pnl_bar_bg.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label lbl_ticker;
        private System.Windows.Forms.Label lbl_weight;
        private System.Windows.Forms.Label lbl_qty;
        private System.Windows.Forms.Label lbl_amount;
        private System.Windows.Forms.Panel pnl_bar_bg;
        private System.Windows.Forms.Panel pnl_bar_fg;
    }
    #endregion
}