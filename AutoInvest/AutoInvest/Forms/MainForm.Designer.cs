namespace AutoInvest.Forms
{
    partial class MainForm
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

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cms_tool = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.복사ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pnl_sidebar = new System.Windows.Forms.Panel();
            this.lbl_menu_section = new System.Windows.Forms.Label();
            this.btn_dashboard = new System.Windows.Forms.Button();
            this.btn_allocation = new System.Windows.Forms.Button();
            this.btn_history = new System.Windows.Forms.Button();
            this.lbl_system_section = new System.Windows.Forms.Label();
            this.btn_config = new System.Windows.Forms.Button();
            this.btn_log = new System.Windows.Forms.Button();
            this.pnl_card1 = new System.Windows.Forms.Panel();
            this.lbl_card1_title = new System.Windows.Forms.Label();
            this.lbl_card1_value = new System.Windows.Forms.Label();
            this.pnl_card2 = new System.Windows.Forms.Panel();
            this.lbl_card2_title = new System.Windows.Forms.Label();
            this.lbl_card2_value = new System.Windows.Forms.Label();
            this.pnl_card3 = new System.Windows.Forms.Panel();
            this.lbl_card3_title = new System.Windows.Forms.Label();
            this.lbl_card3_value = new System.Windows.Forms.Label();
            this.pnl_card4 = new System.Windows.Forms.Panel();
            this.lbl_card4_title = new System.Windows.Forms.Label();
            this.lbl_card4_value = new System.Windows.Forms.Label();
            this.pnl_alloc_header = new System.Windows.Forms.Panel();
            this.lbl_alloc_title = new System.Windows.Forms.Label();
            this.flp_allocation = new System.Windows.Forms.FlowLayoutPanel();
            this.pnl_log_header = new System.Windows.Forms.Panel();
            this.lbl_log_title = new System.Windows.Forms.Label();
            this.lbx_log = new System.Windows.Forms.ListBox();
            this.pnl_btmbar = new System.Windows.Forms.Panel();
            this.cms_tool.SuspendLayout();
            this.pnl_sidebar.SuspendLayout();
            this.pnl_card1.SuspendLayout();
            this.pnl_card2.SuspendLayout();
            this.pnl_card3.SuspendLayout();
            this.pnl_card4.SuspendLayout();
            this.pnl_alloc_header.SuspendLayout();
            this.pnl_log_header.SuspendLayout();
            this.SuspendLayout();
            // 
            // cms_tool
            // 
            this.cms_tool.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.cms_tool.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.복사ToolStripMenuItem});
            this.cms_tool.Name = "cms_tool";
            this.cms_tool.Size = new System.Drawing.Size(121, 36);
            // 
            // 복사ToolStripMenuItem
            // 
            this.복사ToolStripMenuItem.Name = "복사ToolStripMenuItem";
            this.복사ToolStripMenuItem.Size = new System.Drawing.Size(120, 32);
            this.복사ToolStripMenuItem.Text = "복사";
            this.복사ToolStripMenuItem.Click += new System.EventHandler(this.복사ToolStripMenuItem_Click);
            // 
            // pnl_sidebar
            // 
            this.pnl_sidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.pnl_sidebar.Controls.Add(this.lbl_menu_section);
            this.pnl_sidebar.Controls.Add(this.btn_dashboard);
            this.pnl_sidebar.Controls.Add(this.btn_allocation);
            this.pnl_sidebar.Controls.Add(this.btn_history);
            this.pnl_sidebar.Controls.Add(this.lbl_system_section);
            this.pnl_sidebar.Controls.Add(this.btn_config);
            this.pnl_sidebar.Controls.Add(this.btn_log);
            this.pnl_sidebar.Location = new System.Drawing.Point(0, 100);
            this.pnl_sidebar.Name = "pnl_sidebar";
            this.pnl_sidebar.Size = new System.Drawing.Size(234, 860);
            this.pnl_sidebar.TabIndex = 1;
            // 
            // lbl_menu_section
            // 
            this.lbl_menu_section.AutoSize = true;
            this.lbl_menu_section.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.lbl_menu_section.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.lbl_menu_section.Location = new System.Drawing.Point(12, 14);
            this.lbl_menu_section.Name = "lbl_menu_section";
            this.lbl_menu_section.Size = new System.Drawing.Size(42, 21);
            this.lbl_menu_section.TabIndex = 0;
            this.lbl_menu_section.Text = "메뉴";
            // 
            // btn_dashboard
            // 
            this.btn_dashboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.btn_dashboard.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_dashboard.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.btn_dashboard.FlatAppearance.BorderSize = 0;
            this.btn_dashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_dashboard.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_dashboard.ForeColor = System.Drawing.Color.White;
            this.btn_dashboard.Location = new System.Drawing.Point(0, 36);
            this.btn_dashboard.Name = "btn_dashboard";
            this.btn_dashboard.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btn_dashboard.Size = new System.Drawing.Size(200, 48);
            this.btn_dashboard.TabIndex = 1;
            this.btn_dashboard.Text = "대시보드";
            this.btn_dashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_dashboard.UseVisualStyleBackColor = false;
            // 
            // btn_allocation
            // 
            this.btn_allocation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_allocation.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_allocation.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_allocation.FlatAppearance.BorderSize = 0;
            this.btn_allocation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_allocation.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_allocation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_allocation.Location = new System.Drawing.Point(0, 84);
            this.btn_allocation.Name = "btn_allocation";
            this.btn_allocation.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btn_allocation.Size = new System.Drawing.Size(200, 48);
            this.btn_allocation.TabIndex = 2;
            this.btn_allocation.Text = "배분 설정";
            this.btn_allocation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_allocation.UseVisualStyleBackColor = false;
            // 
            // btn_history
            // 
            this.btn_history.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_history.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_history.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_history.FlatAppearance.BorderSize = 0;
            this.btn_history.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_history.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_history.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_history.Location = new System.Drawing.Point(0, 132);
            this.btn_history.Name = "btn_history";
            this.btn_history.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btn_history.Size = new System.Drawing.Size(200, 48);
            this.btn_history.TabIndex = 3;
            this.btn_history.Text = "거래 내역";
            this.btn_history.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_history.UseVisualStyleBackColor = false;
            // 
            // lbl_system_section
            // 
            this.lbl_system_section.AutoSize = true;
            this.lbl_system_section.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.lbl_system_section.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.lbl_system_section.Location = new System.Drawing.Point(12, 190);
            this.lbl_system_section.Name = "lbl_system_section";
            this.lbl_system_section.Size = new System.Drawing.Size(58, 21);
            this.lbl_system_section.TabIndex = 4;
            this.lbl_system_section.Text = "시스템";
            // 
            // btn_config
            // 
            this.btn_config.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_config.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_config.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_config.FlatAppearance.BorderSize = 0;
            this.btn_config.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_config.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_config.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_config.Location = new System.Drawing.Point(0, 210);
            this.btn_config.Name = "btn_config";
            this.btn_config.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btn_config.Size = new System.Drawing.Size(200, 48);
            this.btn_config.TabIndex = 5;
            this.btn_config.Text = "환경 설정";
            this.btn_config.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_config.UseVisualStyleBackColor = false;
            // 
            // btn_log
            // 
            this.btn_log.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_log.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_log.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_log.FlatAppearance.BorderSize = 0;
            this.btn_log.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_log.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_log.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_log.Location = new System.Drawing.Point(0, 258);
            this.btn_log.Name = "btn_log";
            this.btn_log.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btn_log.Size = new System.Drawing.Size(200, 48);
            this.btn_log.TabIndex = 6;
            this.btn_log.Text = "로그";
            this.btn_log.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_log.UseVisualStyleBackColor = false;
            // 
            // pnl_card1
            // 
            this.pnl_card1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.pnl_card1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnl_card1.Controls.Add(this.lbl_card1_title);
            this.pnl_card1.Controls.Add(this.lbl_card1_value);
            this.pnl_card1.Location = new System.Drawing.Point(259, 136);
            this.pnl_card1.Name = "pnl_card1";
            this.pnl_card1.Size = new System.Drawing.Size(195, 90);
            this.pnl_card1.TabIndex = 2;
            // 
            // lbl_card1_title
            // 
            this.lbl_card1_title.AutoSize = true;
            this.lbl_card1_title.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_card1_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lbl_card1_title.Location = new System.Drawing.Point(12, 12);
            this.lbl_card1_title.Name = "lbl_card1_title";
            this.lbl_card1_title.Size = new System.Drawing.Size(108, 25);
            this.lbl_card1_title.TabIndex = 0;
            this.lbl_card1_title.Text = "월 투자금액";
            // 
            // lbl_card1_value
            // 
            this.lbl_card1_value.AutoSize = true;
            this.lbl_card1_value.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lbl_card1_value.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.lbl_card1_value.Location = new System.Drawing.Point(12, 38);
            this.lbl_card1_value.Name = "lbl_card1_value";
            this.lbl_card1_value.Size = new System.Drawing.Size(49, 41);
            this.lbl_card1_value.TabIndex = 1;
            this.lbl_card1_value.Text = "—";
            // 
            // pnl_card2
            // 
            this.pnl_card2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.pnl_card2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnl_card2.Controls.Add(this.lbl_card2_title);
            this.pnl_card2.Controls.Add(this.lbl_card2_value);
            this.pnl_card2.Location = new System.Drawing.Point(489, 136);
            this.pnl_card2.Name = "pnl_card2";
            this.pnl_card2.Size = new System.Drawing.Size(195, 90);
            this.pnl_card2.TabIndex = 3;
            // 
            // lbl_card2_title
            // 
            this.lbl_card2_title.AutoSize = true;
            this.lbl_card2_title.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_card2_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lbl_card2_title.Location = new System.Drawing.Point(12, 12);
            this.lbl_card2_title.Name = "lbl_card2_title";
            this.lbl_card2_title.Size = new System.Drawing.Size(90, 25);
            this.lbl_card2_title.TabIndex = 0;
            this.lbl_card2_title.Text = "현재 환율";
            // 
            // lbl_card2_value
            // 
            this.lbl_card2_value.AutoSize = true;
            this.lbl_card2_value.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lbl_card2_value.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.lbl_card2_value.Location = new System.Drawing.Point(12, 38);
            this.lbl_card2_value.Name = "lbl_card2_value";
            this.lbl_card2_value.Size = new System.Drawing.Size(49, 41);
            this.lbl_card2_value.TabIndex = 1;
            this.lbl_card2_value.Text = "—";
            // 
            // pnl_card3
            // 
            this.pnl_card3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.pnl_card3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnl_card3.Controls.Add(this.lbl_card3_title);
            this.pnl_card3.Controls.Add(this.lbl_card3_value);
            this.pnl_card3.Location = new System.Drawing.Point(719, 136);
            this.pnl_card3.Name = "pnl_card3";
            this.pnl_card3.Size = new System.Drawing.Size(195, 90);
            this.pnl_card3.TabIndex = 4;
            // 
            // lbl_card3_title
            // 
            this.lbl_card3_title.AutoSize = true;
            this.lbl_card3_title.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_card3_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lbl_card3_title.Location = new System.Drawing.Point(12, 12);
            this.lbl_card3_title.Name = "lbl_card3_title";
            this.lbl_card3_title.Size = new System.Drawing.Size(90, 25);
            this.lbl_card3_title.TabIndex = 0;
            this.lbl_card3_title.Text = "다음 주문";
            // 
            // lbl_card3_value
            // 
            this.lbl_card3_value.AutoSize = true;
            this.lbl_card3_value.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lbl_card3_value.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.lbl_card3_value.Location = new System.Drawing.Point(12, 36);
            this.lbl_card3_value.Name = "lbl_card3_value";
            this.lbl_card3_value.Size = new System.Drawing.Size(36, 30);
            this.lbl_card3_value.TabIndex = 1;
            this.lbl_card3_value.Text = "—";
            // 
            // pnl_card4
            // 
            this.pnl_card4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.pnl_card4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnl_card4.Controls.Add(this.lbl_card4_title);
            this.pnl_card4.Controls.Add(this.lbl_card4_value);
            this.pnl_card4.Location = new System.Drawing.Point(944, 136);
            this.pnl_card4.Name = "pnl_card4";
            this.pnl_card4.Size = new System.Drawing.Size(195, 90);
            this.pnl_card4.TabIndex = 5;
            // 
            // lbl_card4_title
            // 
            this.lbl_card4_title.AutoSize = true;
            this.lbl_card4_title.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_card4_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lbl_card4_title.Location = new System.Drawing.Point(12, 12);
            this.lbl_card4_title.Name = "lbl_card4_title";
            this.lbl_card4_title.Size = new System.Drawing.Size(48, 25);
            this.lbl_card4_title.TabIndex = 0;
            this.lbl_card4_title.Text = "모드";
            // 
            // lbl_card4_value
            // 
            this.lbl_card4_value.AutoSize = true;
            this.lbl_card4_value.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lbl_card4_value.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(100)))), ((int)(((byte)(0)))));
            this.lbl_card4_value.Location = new System.Drawing.Point(12, 38);
            this.lbl_card4_value.Name = "lbl_card4_value";
            this.lbl_card4_value.Size = new System.Drawing.Size(49, 41);
            this.lbl_card4_value.TabIndex = 1;
            this.lbl_card4_value.Text = "—";
            // 
            // pnl_alloc_header
            // 
            this.pnl_alloc_header.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.pnl_alloc_header.Controls.Add(this.lbl_alloc_title);
            this.pnl_alloc_header.Location = new System.Drawing.Point(259, 252);
            this.pnl_alloc_header.Name = "pnl_alloc_header";
            this.pnl_alloc_header.Size = new System.Drawing.Size(880, 40);
            this.pnl_alloc_header.TabIndex = 6;
            // 
            // lbl_alloc_title
            // 
            this.lbl_alloc_title.AutoSize = true;
            this.lbl_alloc_title.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_alloc_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.lbl_alloc_title.Location = new System.Drawing.Point(10, 7);
            this.lbl_alloc_title.Name = "lbl_alloc_title";
            this.lbl_alloc_title.Size = new System.Drawing.Size(132, 25);
            this.lbl_alloc_title.TabIndex = 0;
            this.lbl_alloc_title.Text = "배분 계산 결과";
            // 
            // flp_allocation
            // 
            this.flp_allocation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.flp_allocation.Location = new System.Drawing.Point(259, 292);
            this.flp_allocation.Name = "flp_allocation";
            this.flp_allocation.Padding = new System.Windows.Forms.Padding(8);
            this.flp_allocation.Size = new System.Drawing.Size(880, 150);
            this.flp_allocation.TabIndex = 7;
            // 
            // pnl_log_header
            // 
            this.pnl_log_header.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.pnl_log_header.Controls.Add(this.lbl_log_title);
            this.pnl_log_header.Location = new System.Drawing.Point(259, 486);
            this.pnl_log_header.Name = "pnl_log_header";
            this.pnl_log_header.Size = new System.Drawing.Size(880, 40);
            this.pnl_log_header.TabIndex = 8;
            // 
            // lbl_log_title
            // 
            this.lbl_log_title.AutoSize = true;
            this.lbl_log_title.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_log_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.lbl_log_title.Location = new System.Drawing.Point(10, 7);
            this.lbl_log_title.Name = "lbl_log_title";
            this.lbl_log_title.Size = new System.Drawing.Size(108, 25);
            this.lbl_log_title.TabIndex = 0;
            this.lbl_log_title.Text = "실시간 로그";
            // 
            // lbx_log
            // 
            this.lbx_log.ContextMenuStrip = this.cms_tool;
            this.lbx_log.FormattingEnabled = true;
            this.lbx_log.HorizontalScrollbar = true;
            this.lbx_log.ItemHeight = 18;
            this.lbx_log.Location = new System.Drawing.Point(259, 526);
            this.lbx_log.Name = "lbx_log";
            this.lbx_log.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbx_log.Size = new System.Drawing.Size(880, 292);
            this.lbx_log.TabIndex = 9;
            // 
            // pnl_btmbar
            // 
            this.pnl_btmbar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.pnl_btmbar.Location = new System.Drawing.Point(0, 850);
            this.pnl_btmbar.Name = "pnl_btmbar";
            this.pnl_btmbar.Size = new System.Drawing.Size(1200, 110);
            this.pnl_btmbar.TabIndex = 10;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 960);
            this.Controls.Add(this.pnl_sidebar);
            this.Controls.Add(this.pnl_btmbar);
            this.Controls.Add(this.pnl_card1);
            this.Controls.Add(this.pnl_card2);
            this.Controls.Add(this.pnl_card3);
            this.Controls.Add(this.pnl_card4);
            this.Controls.Add(this.pnl_alloc_header);
            this.Controls.Add(this.flp_allocation);
            this.Controls.Add(this.pnl_log_header);
            this.Controls.Add(this.lbx_log);
            this.MinimumSize = new System.Drawing.Size(1200, 960);
            this.Name = "MainForm";
            this.Text = "Auto Investing";
            this.cms_tool.ResumeLayout(false);
            this.pnl_sidebar.ResumeLayout(false);
            this.pnl_sidebar.PerformLayout();
            this.pnl_card1.ResumeLayout(false);
            this.pnl_card1.PerformLayout();
            this.pnl_card2.ResumeLayout(false);
            this.pnl_card2.PerformLayout();
            this.pnl_card3.ResumeLayout(false);
            this.pnl_card3.PerformLayout();
            this.pnl_card4.ResumeLayout(false);
            this.pnl_card4.PerformLayout();
            this.pnl_alloc_header.ResumeLayout(false);
            this.pnl_alloc_header.PerformLayout();
            this.pnl_log_header.ResumeLayout(false);
            this.pnl_log_header.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        #region 필드 선언
        private System.Windows.Forms.ContextMenuStrip cms_tool;
        private System.Windows.Forms.ToolStripMenuItem 복사ToolStripMenuItem;
        private System.Windows.Forms.Panel pnl_sidebar;
        private System.Windows.Forms.Label lbl_menu_section;
        private System.Windows.Forms.Label lbl_system_section;
        private System.Windows.Forms.Button btn_dashboard;
        private System.Windows.Forms.Button btn_allocation;
        private System.Windows.Forms.Button btn_history;
        private System.Windows.Forms.Button btn_config;
        private System.Windows.Forms.Button btn_log;
        private System.Windows.Forms.Panel pnl_card1;
        private System.Windows.Forms.Label lbl_card1_title;
        private System.Windows.Forms.Label lbl_card1_value;
        private System.Windows.Forms.Panel pnl_card2;
        private System.Windows.Forms.Label lbl_card2_title;
        private System.Windows.Forms.Label lbl_card2_value;
        private System.Windows.Forms.Panel pnl_card3;
        private System.Windows.Forms.Label lbl_card3_title;
        private System.Windows.Forms.Label lbl_card3_value;
        private System.Windows.Forms.Panel pnl_card4;
        private System.Windows.Forms.Label lbl_card4_title;
        private System.Windows.Forms.Label lbl_card4_value;
        private System.Windows.Forms.Panel pnl_alloc_header;
        private System.Windows.Forms.Label lbl_alloc_title;
        private System.Windows.Forms.FlowLayoutPanel flp_allocation;
        private System.Windows.Forms.Panel pnl_log_header;
        private System.Windows.Forms.Label lbl_log_title;
        private System.Windows.Forms.ListBox lbx_log;
        #endregion

        private System.Windows.Forms.Panel pnl_btmbar;
    }
}