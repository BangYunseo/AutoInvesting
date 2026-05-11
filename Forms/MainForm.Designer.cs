namespace AutoInvest.Forms
{
    partial class MainForm
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
            this.pnl_sidebar = new System.Windows.Forms.Panel();
            this.lbl_app_title = new System.Windows.Forms.Label();
            this.lbl_menu_section = new System.Windows.Forms.Label();
            this.btn_dashboard = new System.Windows.Forms.Button();
            this.btn_allocation = new System.Windows.Forms.Button();
            this.btn_history = new System.Windows.Forms.Button();
            this.lbl_system_section = new System.Windows.Forms.Label();
            this.btn_config = new System.Windows.Forms.Button();
            this.btn_log = new System.Windows.Forms.Button();
            this.pnl_topbar = new System.Windows.Forms.Panel();
            this.lbl_topbar_title = new System.Windows.Forms.Label();
            this.pnl_content = new System.Windows.Forms.Panel();
            this.pnl_sidebar.SuspendLayout();
            this.pnl_topbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_sidebar
            // 
            this.pnl_sidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.pnl_sidebar.Controls.Add(this.lbl_app_title);
            this.pnl_sidebar.Controls.Add(this.lbl_menu_section);
            this.pnl_sidebar.Controls.Add(this.btn_dashboard);
            this.pnl_sidebar.Controls.Add(this.btn_allocation);
            this.pnl_sidebar.Controls.Add(this.btn_history);
            this.pnl_sidebar.Controls.Add(this.lbl_system_section);
            this.pnl_sidebar.Controls.Add(this.btn_config);
            this.pnl_sidebar.Controls.Add(this.btn_log);
            this.pnl_sidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnl_sidebar.Location = new System.Drawing.Point(0, 0);
            this.pnl_sidebar.Name = "pnl_sidebar";
            this.pnl_sidebar.Size = new System.Drawing.Size(160, 660);
            this.pnl_sidebar.TabIndex = 0;
            // 
            // lbl_app_title
            // 
            this.lbl_app_title.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.lbl_app_title.ForeColor = System.Drawing.Color.White;
            this.lbl_app_title.Location = new System.Drawing.Point(10, 15);
            this.lbl_app_title.Name = "lbl_app_title";
            this.lbl_app_title.Size = new System.Drawing.Size(140, 30);
            this.lbl_app_title.Text = "AutoInvest";
            // 
            // lbl_menu_section
            // 
            this.lbl_menu_section.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.lbl_menu_section.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.lbl_menu_section.Location = new System.Drawing.Point(10, 55);
            this.lbl_menu_section.Name = "lbl_menu_section";
            this.lbl_menu_section.Size = new System.Drawing.Size(140, 19);
            this.lbl_menu_section.Text = "MENU";
            // 
            // btn_dashboard
            // 
            this.btn_dashboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_dashboard.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_dashboard.FlatAppearance.BorderSize = 0;
            this.btn_dashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_dashboard.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_dashboard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_dashboard.Location = new System.Drawing.Point(0, 80);
            this.btn_dashboard.Name = "btn_dashboard";
            this.btn_dashboard.Size = new System.Drawing.Size(160, 35);
            this.btn_dashboard.TabIndex = 0;
            this.btn_dashboard.Text = "대시보드";
            this.btn_dashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_dashboard.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.btn_dashboard.UseVisualStyleBackColor = false;
            this.btn_dashboard.Click += new System.EventHandler(this.btn_dashboard_Click);
            // 
            // btn_allocation
            // 
            this.btn_allocation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_allocation.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_allocation.FlatAppearance.BorderSize = 0;
            this.btn_allocation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_allocation.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_allocation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_allocation.Location = new System.Drawing.Point(0, 120);
            this.btn_allocation.Name = "btn_allocation";
            this.btn_allocation.Size = new System.Drawing.Size(160, 35);
            this.btn_allocation.TabIndex = 1;
            this.btn_allocation.Text = "배분 설정";
            this.btn_allocation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_allocation.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.btn_allocation.UseVisualStyleBackColor = false;
            this.btn_allocation.Click += new System.EventHandler(this.btn_allocation_Click);
            // 
            // btn_history
            // 
            this.btn_history.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_history.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_history.FlatAppearance.BorderSize = 0;
            this.btn_history.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_history.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_history.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_history.Location = new System.Drawing.Point(0, 160);
            this.btn_history.Name = "btn_history";
            this.btn_history.Size = new System.Drawing.Size(160, 35);
            this.btn_history.TabIndex = 2;
            this.btn_history.Text = "거래 내역";
            this.btn_history.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_history.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.btn_history.UseVisualStyleBackColor = false;
            this.btn_history.Click += new System.EventHandler(this.btn_history_Click);
            // 
            // lbl_system_section
            // 
            this.lbl_system_section.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.lbl_system_section.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(160)))), ((int)(((byte)(170)))));
            this.lbl_system_section.Location = new System.Drawing.Point(10, 210);
            this.lbl_system_section.Name = "lbl_system_section";
            this.lbl_system_section.Size = new System.Drawing.Size(140, 19);
            this.lbl_system_section.Text = "SYSTEM";
            // 
            // btn_config
            // 
            this.btn_config.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_config.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_config.FlatAppearance.BorderSize = 0;
            this.btn_config.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_config.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_config.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_config.Location = new System.Drawing.Point(0, 235);
            this.btn_config.Name = "btn_config";
            this.btn_config.Size = new System.Drawing.Size(160, 35);
            this.btn_config.TabIndex = 3;
            this.btn_config.Text = "환경 설정";
            this.btn_config.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_config.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.btn_config.UseVisualStyleBackColor = false;
            this.btn_config.Click += new System.EventHandler(this.btn_config_Click);
            // 
            // btn_log
            // 
            this.btn_log.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.btn_log.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_log.FlatAppearance.BorderSize = 0;
            this.btn_log.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_log.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.btn_log.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.btn_log.Location = new System.Drawing.Point(0, 275);
            this.btn_log.Name = "btn_log";
            this.btn_log.Size = new System.Drawing.Size(160, 35);
            this.btn_log.TabIndex = 4;
            this.btn_log.Text = "로그";
            this.btn_log.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_log.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.btn_log.UseVisualStyleBackColor = false;
            this.btn_log.Click += new System.EventHandler(this.btn_log_Click);
            // 
            // pnl_topbar
            // 
            this.pnl_topbar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.pnl_topbar.Controls.Add(this.lbl_topbar_title);
            this.pnl_topbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_topbar.Location = new System.Drawing.Point(160, 0);
            this.pnl_topbar.Name = "pnl_topbar";
            this.pnl_topbar.Size = new System.Drawing.Size(725, 45);
            this.pnl_topbar.TabIndex = 1;
            // 
            // lbl_topbar_title
            // 
            this.lbl_topbar_title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lbl_topbar_title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.lbl_topbar_title.Location = new System.Drawing.Point(15, 12);
            this.lbl_topbar_title.Name = "lbl_topbar_title";
            this.lbl_topbar_title.Size = new System.Drawing.Size(400, 22);
            this.lbl_topbar_title.Text = "해외 ETF 자동 투자 시스템";
            // 
            // pnl_content
            // 
            this.pnl_content.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.pnl_content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_content.Location = new System.Drawing.Point(160, 45);
            this.pnl_content.Name = "pnl_content";
            this.pnl_content.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(885, 660);
            this.Controls.Add(this.pnl_content);
            this.Controls.Add(this.pnl_topbar);
            this.Controls.Add(this.pnl_sidebar);
            this.MinimumSize = new System.Drawing.Size(885, 660);
            this.Name = "MainForm";
            this.Text = "AutoInvest — 해외 ETF 자동 투자 시스템";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.pnl_sidebar.ResumeLayout(false);
            this.pnl_topbar.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnl_sidebar;
        private System.Windows.Forms.Label lbl_app_title;
        private System.Windows.Forms.Label lbl_menu_section;
        private System.Windows.Forms.Button btn_dashboard;
        private System.Windows.Forms.Button btn_allocation;
        private System.Windows.Forms.Button btn_history;
        private System.Windows.Forms.Label lbl_system_section;
        private System.Windows.Forms.Button btn_config;
        private System.Windows.Forms.Button btn_log;
        private System.Windows.Forms.Panel pnl_topbar;
        private System.Windows.Forms.Label lbl_topbar_title;
        private System.Windows.Forms.Panel pnl_content;
    }
}