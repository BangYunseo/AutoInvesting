using AutoInvest.Utils;
using System;
using System.Windows.Forms;

namespace AutoInvest
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 전역 예외 처리
            Application.ThreadException += (s, e) =>
                Logger.Fatal($"UI 스레드 예외: {e.Exception.Message}");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Logger.Fatal($"비관리 예외: {e.ExceptionObject}");


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Forms.MainForm());
        }
    }
}
