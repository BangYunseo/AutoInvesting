using AutoInvest.Panels;
using AutoInvest.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoInvest.Forms
{
    public partial class MainForm : Form
    {
        private UserControl? _activePanel;
        private DashboardPanel? _dashboardPanel;

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            // 대시보드 패널 생성 + Logger 초기화
            _dashboardPanel = new DashboardPanel();
            Logger.Initialize(_dashboardPanel.GetLogListBox());
            Logger.Info("투자 자동화 시작");

            _ = Data.DBManager.Instance;

            // 초기 화면: 대시보드
            SwitchPanel(_dashboardPanel);
            SetActiveMenu(btn_dashboard);
        }

        // ─── Panel 전환 핵심 로직 ────────────────────────────

        private void SwitchPanel(UserControl newPanel)
        {
            if (_activePanel == newPanel) return;

            pnl_content.SuspendLayout();

            // 기존 패널 제거
            if (_activePanel != null)
            {
                pnl_content.Controls.Remove(_activePanel);
                // 대시보드는 재사용하므로 Dispose 안 함
                if (_activePanel != _dashboardPanel)
                    _activePanel.Dispose();
            }

            newPanel.Dock = DockStyle.Fill;
            pnl_content.Controls.Add(newPanel);
            _activePanel = newPanel;

            pnl_content.ResumeLayout(true);
        }

        private void SetActiveMenu(Button activeBtn)
        {
            foreach (var btn in new[] { btn_dashboard, btn_allocation, btn_history, btn_config, btn_log })
            {
                btn.BackColor = AppTheme.BgSidebar;
                btn.ForeColor = AppTheme.FgSecondary;
            }
            activeBtn.BackColor = AppTheme.BtnActive;
            activeBtn.ForeColor = Color.White;
        }

        // ─── 사이드바 메뉴 클릭 ─────────────────────────────

        private void btn_dashboard_Click(object? sender, EventArgs e)
        {
            SetActiveMenu(btn_dashboard);
            _dashboardPanel?.LoadDashboard(); // 대시보드 새로고침
            SwitchPanel(_dashboardPanel!);
        }

        private void btn_allocation_Click(object? sender, EventArgs e)
        {
            SetActiveMenu(btn_allocation);
            var panel = new AllocationPanel();
            panel.OnSaved += (s, _) =>
            {
                _dashboardPanel?.LoadDashboard();
                Logger.Info("[MainForm] 배분 저장 → 대시보드 갱신");
            };
            SwitchPanel(panel);
        }

        private void btn_history_Click(object? sender, EventArgs e)
        {
            SetActiveMenu(btn_history);
            SwitchPanel(new HistoryPanel());
        }

        private void btn_config_Click(object? sender, EventArgs e)
        {
            SetActiveMenu(btn_config);
            var panel = new ConfigPanel();
            panel.OnSaved += (s, _) =>
            {
                _dashboardPanel?.LoadDashboard();
                Logger.Info("[MainForm] 설정 저장 → 대시보드 갱신");
            };
            SwitchPanel(panel);
        }

        private void btn_log_Click(object? sender, EventArgs e)
        {
            SetActiveMenu(btn_log);
            var logPanel = new LogPanel();
            // 기존 로그를 새 패널에 복사
            foreach (var item in _dashboardPanel!.GetLogListBox().Items)
                logPanel.GetLogListBox().Items.Add(item);
            SwitchPanel(logPanel);
        }
    }
}