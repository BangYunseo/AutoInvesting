using AutoInvest.Utils;
using System;
using System.Windows.Forms;

namespace AutoInvest
{
    /// <summary>
    /// 애플리케이션 진입점.
    /// WinForms 앱의 시작점이며, 전역 예외 처리를 설정합니다.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // ── 전역 예외 처리 ──
            // UI 스레드에서 발생하는 예외를 잡아 로그에 기록
            Application.ThreadException += (s, e) =>
                Logger.Fatal($"UI 스레드 예외: {e.Exception.Message}");

            // 비관리 스레드(Task, Thread 등)에서 발생하는 예외를 잡아 로그에 기록
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Logger.Fatal($"비관리 예외: {e.ExceptionObject}");

            // ── WinForms 초기화 ──
            Application.EnableVisualStyles();                    // OS 기본 컨트롤 스타일 적용
            Application.SetCompatibleTextRenderingDefault(false); // GDI+ 텍스트 렌더링 사용
            Application.Run(new Forms.MainForm());               // 메인 폼 실행 (앱 루프 시작)
        }
    }
}
