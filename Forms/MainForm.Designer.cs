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
            this.btn_backtest = new System.Windows.Forms.Button();
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
            this.btn_reservation = new System.Windows.Forms.Button();
            this.btn_order = new System.Windows.Forms.Button();
            this.pnl_topbar = new System.Windows.Forms.Panel();
            this.btn_login = new System.Windows.Forms.Button();
            this.cms_tool.SuspendLayout();
            this.pnl_sidebar.SuspendLayout();
            this.pnl_card1.SuspendLayout();
            this.pnl_card2.SuspendLayout();
            this.pnl_card3.SuspendLayout();
            this.pnl_card4.SuspendLayout();
            this.pnl_alloc_header.SuspendLayout();
            this.pnl_log_header.SuspendLayout();
            this.pnl_btmbar.SuspendLayout();
            this.pnl_topbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // cms_tool
            // 
            this.cms_tool.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.cms_tool.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.복사ToolStripMenuItem});
            this.cms_tool.Name = "cms_tool";
            this.cms_tool.Size = new System.Drawing.Size(99, 26);
            // 
            // 복사ToolStripMenuItem
            // 
            this.복사ToolStripMenuItem.Name = "복사ToolStripMenuItem";
            this.복사ToolStripMenuItem.Size = new System.Drawing.Size(98, 22);
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
            this.pnl_sidebar.Controls.Add(this.btn_backtest);
            this.pnl_sidebar.Controls.Add(this.lbl_system_section);
            this.pnl_sidebar.Controls.Add(this.btn_config);
            this.pnl_sidebar.Controls.Add(this.btn_log);
            this.pnl_sidebar.Location = new System.Drawing.Point(0, 0);
            this.pnl_sidebar.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_sidebar.Name = "pnl_sidebar";
            this.pnl_sidebar.Size = new System.Drawing.Size(160, 660);
            this.pnl_sidebar.TabIndex = 1;
            // 
            // lbl_menu_section
            // 
            this.lbl_menu_section.AutoSize = true;
            this.lbl_menu_section.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_menu_section.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.lbl_menu_section.Location = new System.Drawing.Point(10, 10);
            this.lbl_menu_section.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_menu_section.Name = "lbl_menu_section";
            this.lbl_menu_section.Size = new System.Drawing.Size(37, 19);
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
            this.btn_dashboard.Location = new System.Drawing.Point(0, 50);
            this.btn_dashboard.Margin = new System.Windows.Forms.Padding(2);
            this.btn_dashboard.Name = "btn_dashboard";
            this.btn_dashboard.Size = new System.Drawing.Size(160, 35);
            this.btn_dashboard.TabIndex = 1;
            this.btn_dashboard.Text = "대시보드";
            this.btn_dashboard.UseVisualStyleBackColor = false;
            this.btn_dashboard.Click += new System.EventHandler(this.btn_dashboard_Click);
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
            this.btn_allocation.Location = new System.Drawing.Point(0, 90);
            this.btn_allocation.Margin = new System.Windows.Forms.Padding(2);
            this.btn_allocation.Name = "btn_allocation";
            this.btn_allocation.Size = new System.Drawing.Size(160, 35);
            this.btn_allocation.TabIndex = 2;
            this.btn_allocation.Text = "배분 설정";
            this.btn_allocation.UseVisualStyleBackColor = false;
            this.btn_allocation.Click += new System.EventHandler(this.btn_allocation_Click);
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
            this.btn_history.Location = new System.Drawing.Point(0, 130);
            this.btn_history.Margin = new System.Windows.Forms.Padding(2);
            this.btn_history.Name = "btn_history";
            this.btn_history.Size = new System.Drawing.Size(160, 35);
            this.btn_history.TabIndex = 3;
            this.btn_history.Text = "거래 내역";
            this.btn_history.UseVisualStyleBackColor = false;
            this.btn_history.Click += new System.EventHandler(this.btn_history_Click);
            // 
            // btn_backtest
            // 
            this.btn_backtest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_backtest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_backtest.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_backtest.FlatAppearance.BorderSize = 0;
            this.btn_backtest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_backtest.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_backtest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_backtest.Location = new System.Drawing.Point(0, 170);
            this.btn_backtest.Margin = new System.Windows.Forms.Padding(2);
            this.btn_backtest.Name = "btn_backtest";
            this.btn_backtest.Size = new System.Drawing.Size(160, 35);
            this.btn_backtest.TabIndex = 7;
            this.btn_backtest.Text = "백테스팅";
            this.btn_backtest.UseVisualStyleBackColor = false;
            this.btn_backtest.Click += new System.EventHandler(this.btn_backtest_Click);
            // 
            // lbl_system_section
            // 
            this.lbl_system_section.AutoSize = true;
            this.lbl_system_section.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_system_section.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.lbl_system_section.Location = new System.Drawing.Point(10, 240);
            this.lbl_system_section.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_system_section.Name = "lbl_system_section";
            this.lbl_system_section.Size = new System.Drawing.Size(51, 19);
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
            this.btn_config.Location = new System.Drawing.Point(0, 270);
            this.btn_config.Margin = new System.Windows.Forms.Padding(2);
            this.btn_config.Name = "btn_config";
            this.btn_config.Size = new System.Drawing.Size(160, 35);
            this.btn_config.TabIndex = 5;
            this.btn_config.Text = "환경 설정";
            this.btn_config.UseVisualStyleBackColor = false;
            this.btn_config.Click += new System.EventHandler(this.btn_config_Click);
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
            this.btn_log.Location = new System.Drawing.Point(0, 310);
            this.btn_log.Margin = new System.Windows.Forms.Padding(2);
            this.btn_log.Name = "btn_log";
            this.btn_log.Size = new System.Drawing.Size(160, 35);
            this.btn_log.TabIndex = 6;
            this.btn_log.Text = "로그";
            this.btn_log.UseVisualStyleBackColor = false;
            this.btn_log.Click += new System.EventHandler(this.btn_log_Click);
            // 
            // pnl_card1
            // 
            this.pnl_card1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.pnl_card1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pnl_card1.Controls.Add(this.lbl_card1_title);
            this.pnl_card1.Controls.Add(this.lbl_card1_value);
            this.pnl_card1.Location = new System.Drawing.Point(191, 70);
            this.pnl_card1.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_card1.Name = "pnl_card1";
            this.pnl_card1.Size = new System.Drawing.Size(137, 101);
            this.pnl_card1.TabIndex = 2;
            // 
            // lbl_card1_title
            // 
            this.lbl_card1_title.AutoSize = true;
            this.lbl_card1_title.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_card1_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.lbl_card1_title.Location = new System.Drawing.Point(8, 8);
            this.lbl_card1_title.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_card1_title.Name = "lbl_card1_title";
            this.lbl_card1_title.Size = new System.Drawing.Size(71, 15);
            this.lbl_card1_title.TabIndex = 0;
            this.lbl_card1_title.Text = "월 투자금액";
            // 
            // lbl_card1_value
            // 
            this.lbl_card1_value.AutoSize = true;
            this.lbl_card1_value.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lbl_card1_value.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.lbl_card1_value.Location = new System.Drawing.Point(8, 25);
            this.lbl_card1_value.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_card1_value.Name = "lbl_card1_value";
            this.lbl_card1_value.Size = new System.Drawing.Size(33, 28);
            this.lbl_card1_value.TabIndex = 1;
            this.lbl_card1_value.Text = "—";
            // 
            // pnl_card2
            // 
            this.pnl_card2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.pnl_card2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pnl_card2.Controls.Add(this.lbl_card2_title);
            this.pnl_card2.Controls.Add(this.lbl_card2_value);
            this.pnl_card2.Location = new System.Drawing.Point(361, 70);
            this.pnl_card2.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_card2.Name = "pnl_card2";
            this.pnl_card2.Size = new System.Drawing.Size(137, 101);
            this.pnl_card2.TabIndex = 3;
            // 
            // lbl_card2_title
            // 
            this.lbl_card2_title.AutoSize = true;
            this.lbl_card2_title.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_card2_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.lbl_card2_title.Location = new System.Drawing.Point(8, 8);
            this.lbl_card2_title.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_card2_title.Name = "lbl_card2_title";
            this.lbl_card2_title.Size = new System.Drawing.Size(59, 15);
            this.lbl_card2_title.TabIndex = 0;
            this.lbl_card2_title.Text = "현재 환율";
            // 
            // lbl_card2_value
            // 
            this.lbl_card2_value.AutoSize = true;
            this.lbl_card2_value.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lbl_card2_value.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.lbl_card2_value.Location = new System.Drawing.Point(8, 25);
            this.lbl_card2_value.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_card2_value.Name = "lbl_card2_value";
            this.lbl_card2_value.Size = new System.Drawing.Size(33, 28);
            this.lbl_card2_value.TabIndex = 1;
            this.lbl_card2_value.Text = "—";
            // 
            // pnl_card3
            // 
            this.pnl_card3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.pnl_card3.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pnl_card3.Controls.Add(this.lbl_card3_title);
            this.pnl_card3.Controls.Add(this.lbl_card3_value);
            this.pnl_card3.Location = new System.Drawing.Point(531, 70);
            this.pnl_card3.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_card3.Name = "pnl_card3";
            this.pnl_card3.Size = new System.Drawing.Size(137, 101);
            this.pnl_card3.TabIndex = 4;
            // 
            // lbl_card3_title
            // 
            this.lbl_card3_title.AutoSize = true;
            this.lbl_card3_title.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_card3_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.lbl_card3_title.Location = new System.Drawing.Point(8, 8);
            this.lbl_card3_title.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_card3_title.Name = "lbl_card3_title";
            this.lbl_card3_title.Size = new System.Drawing.Size(59, 15);
            this.lbl_card3_title.TabIndex = 0;
            this.lbl_card3_title.Text = "다음 주문";
            // 
            // lbl_card3_value
            // 
            this.lbl_card3_value.AutoSize = true;
            this.lbl_card3_value.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lbl_card3_value.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.lbl_card3_value.Location = new System.Drawing.Point(8, 24);
            this.lbl_card3_value.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_card3_value.Name = "lbl_card3_value";
            this.lbl_card3_value.Size = new System.Drawing.Size(24, 20);
            this.lbl_card3_value.TabIndex = 1;
            this.lbl_card3_value.Text = "—";
            // 
            // pnl_card4
            // 
            this.pnl_card4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.pnl_card4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pnl_card4.Controls.Add(this.lbl_card4_title);
            this.pnl_card4.Controls.Add(this.lbl_card4_value);
            this.pnl_card4.Location = new System.Drawing.Point(701, 70);
            this.pnl_card4.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_card4.Name = "pnl_card4";
            this.pnl_card4.Size = new System.Drawing.Size(137, 101);
            this.pnl_card4.TabIndex = 5;
            // 
            // lbl_card4_title
            // 
            this.lbl_card4_title.AutoSize = true;
            this.lbl_card4_title.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lbl_card4_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.lbl_card4_title.Location = new System.Drawing.Point(8, 8);
            this.lbl_card4_title.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_card4_title.Name = "lbl_card4_title";
            this.lbl_card4_title.Size = new System.Drawing.Size(31, 15);
            this.lbl_card4_title.TabIndex = 0;
            this.lbl_card4_title.Text = "모드";
            // 
            // lbl_card4_value
            // 
            this.lbl_card4_value.AutoSize = true;
            this.lbl_card4_value.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lbl_card4_value.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(100)))), ((int)(((byte)(0)))));
            this.lbl_card4_value.Location = new System.Drawing.Point(8, 25);
            this.lbl_card4_value.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_card4_value.Name = "lbl_card4_value";
            this.lbl_card4_value.Size = new System.Drawing.Size(33, 28);
            this.lbl_card4_value.TabIndex = 1;
            this.lbl_card4_value.Text = "—";
            // 
            // pnl_alloc_header
            // 
            this.pnl_alloc_header.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.pnl_alloc_header.Controls.Add(this.lbl_alloc_title);
            this.pnl_alloc_header.Location = new System.Drawing.Point(180, 185);
            this.pnl_alloc_header.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_alloc_header.Name = "pnl_alloc_header";
            this.pnl_alloc_header.Size = new System.Drawing.Size(420, 30);
            this.pnl_alloc_header.TabIndex = 6;
            // 
            // lbl_alloc_title
            // 
            this.lbl_alloc_title.AutoSize = true;
            this.lbl_alloc_title.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_alloc_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lbl_alloc_title.Location = new System.Drawing.Point(7, 5);
            this.lbl_alloc_title.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_alloc_title.Name = "lbl_alloc_title";
            this.lbl_alloc_title.Size = new System.Drawing.Size(87, 15);
            this.lbl_alloc_title.TabIndex = 0;
            this.lbl_alloc_title.Text = "배분 계산 결과";
            // 
            // flp_allocation
            // 
            this.flp_allocation.AutoScroll = true;
            this.flp_allocation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.flp_allocation.Location = new System.Drawing.Point(180, 215);
            this.flp_allocation.Margin = new System.Windows.Forms.Padding(2);
            this.flp_allocation.Name = "flp_allocation";
            this.flp_allocation.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.flp_allocation.Size = new System.Drawing.Size(420, 345);
            this.flp_allocation.TabIndex = 7;
            // 
            // pnl_log_header
            // 
            this.pnl_log_header.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.pnl_log_header.Controls.Add(this.lbl_log_title);
            this.pnl_log_header.Location = new System.Drawing.Point(610, 185);
            this.pnl_log_header.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_log_header.Name = "pnl_log_header";
            this.pnl_log_header.Size = new System.Drawing.Size(240, 30);
            this.pnl_log_header.TabIndex = 8;
            // 
            // lbl_log_title
            // 
            this.lbl_log_title.AutoSize = true;
            this.lbl_log_title.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_log_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lbl_log_title.Location = new System.Drawing.Point(7, 5);
            this.lbl_log_title.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_log_title.Name = "lbl_log_title";
            this.lbl_log_title.Size = new System.Drawing.Size(71, 15);
            this.lbl_log_title.TabIndex = 0;
            this.lbl_log_title.Text = "실시간 로그";
            // 
            // lbx_log
            // 
            this.lbx_log.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lbx_log.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lbx_log.ContextMenuStrip = this.cms_tool;
            this.lbx_log.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lbx_log.FormattingEnabled = true;
            this.lbx_log.HorizontalScrollbar = true;
            this.lbx_log.ItemHeight = 12;
            this.lbx_log.Location = new System.Drawing.Point(610, 215);
            this.lbx_log.Margin = new System.Windows.Forms.Padding(2);
            this.lbx_log.Name = "lbx_log";
            this.lbx_log.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbx_log.Size = new System.Drawing.Size(240, 345);
            this.lbx_log.TabIndex = 9;
            // 
            // pnl_btmbar
            // 
            this.pnl_btmbar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.pnl_btmbar.Controls.Add(this.btn_reservation);
            this.pnl_btmbar.Controls.Add(this.btn_order);
            this.pnl_btmbar.Location = new System.Drawing.Point(160, 580);
            this.pnl_btmbar.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_btmbar.Name = "pnl_btmbar";
            this.pnl_btmbar.Size = new System.Drawing.Size(725, 80);
            this.pnl_btmbar.TabIndex = 7;
            // 
            // btn_reservation
            // 
            this.btn_reservation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.btn_reservation.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_reservation.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.btn_reservation.FlatAppearance.BorderSize = 0;
            this.btn_reservation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_reservation.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btn_reservation.ForeColor = System.Drawing.Color.White;
            this.btn_reservation.Location = new System.Drawing.Point(450, 25);
            this.btn_reservation.Margin = new System.Windows.Forms.Padding(2);
            this.btn_reservation.Name = "btn_reservation";
            this.btn_reservation.Size = new System.Drawing.Size(100, 30);
            this.btn_reservation.TabIndex = 3;
            this.btn_reservation.Text = "예약 주문";
            this.btn_reservation.UseVisualStyleBackColor = false;
            // 
            // btn_order
            // 
            this.btn_order.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.btn_order.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_order.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.btn_order.FlatAppearance.BorderSize = 0;
            this.btn_order.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_order.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btn_order.ForeColor = System.Drawing.Color.White;
            this.btn_order.Location = new System.Drawing.Point(600, 25);
            this.btn_order.Margin = new System.Windows.Forms.Padding(2);
            this.btn_order.Name = "btn_order";
            this.btn_order.Size = new System.Drawing.Size(100, 30);
            this.btn_order.TabIndex = 2;
            this.btn_order.Text = "즉시 주문";
            this.btn_order.UseVisualStyleBackColor = false;
            // 
            // pnl_topbar
            // 
            this.pnl_topbar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.pnl_topbar.Controls.Add(this.btn_login);
            this.pnl_topbar.Location = new System.Drawing.Point(160, 0);
            this.pnl_topbar.Margin = new System.Windows.Forms.Padding(2);
            this.pnl_topbar.Name = "pnl_topbar";
            this.pnl_topbar.Size = new System.Drawing.Size(725, 50);
            this.pnl_topbar.TabIndex = 7;
            // 
            // btn_login
            // 
            this.btn_login.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.btn_login.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_login.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.btn_login.FlatAppearance.BorderSize = 0;
            this.btn_login.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_login.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btn_login.ForeColor = System.Drawing.Color.White;
            this.btn_login.Location = new System.Drawing.Point(630, 10);
            this.btn_login.Margin = new System.Windows.Forms.Padding(2);
            this.btn_login.Name = "btn_login";
            this.btn_login.Size = new System.Drawing.Size(70, 30);
            this.btn_login.TabIndex = 1;
            this.btn_login.Text = "로그인";
            this.btn_login.UseVisualStyleBackColor = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(884, 661);
            this.Controls.Add(this.pnl_topbar);
            this.Controls.Add(this.pnl_btmbar);
            this.Controls.Add(this.pnl_sidebar);
            this.Controls.Add(this.pnl_card1);
            this.Controls.Add(this.pnl_card2);
            this.Controls.Add(this.pnl_card3);
            this.Controls.Add(this.pnl_card4);
            this.Controls.Add(this.pnl_alloc_header);
            this.Controls.Add(this.flp_allocation);
            this.Controls.Add(this.pnl_log_header);
            this.Controls.Add(this.lbx_log);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(900, 700);
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
            this.pnl_btmbar.ResumeLayout(false);
            this.pnl_topbar.ResumeLayout(false);
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
        private System.Windows.Forms.Panel pnl_topbar;
        private System.Windows.Forms.Button btn_login;
        private System.Windows.Forms.Button btn_order;
        private System.Windows.Forms.Button btn_reservation;
        private System.Windows.Forms.Button btn_backtest;
    }
}