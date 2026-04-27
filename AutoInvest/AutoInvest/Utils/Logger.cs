using System;
using System.IO;
using System.Windows.Forms;

namespace AutoInvest.Utils
{
    public enum LogLevel
    {
        INFO,
        WARN,
        ERROR,
        FATAL
    }

    public static class Logger
    {
        private static ListBox _listBox;
        private static readonly string LogDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public static void Initialize(ListBox listBox)
        {
            _listBox = listBox;
            if(!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);
            }
            DeleteOldLogs();
        }
        
        public static void Info(string msg) => Write(LogLevel.INFO, msg);
        public static void Warn(string msg) => Write(LogLevel.WARN, msg);
        public static void Error(string msg) => Write(LogLevel.ERROR, msg);
        public static void Fatal(string msg) => Write(LogLevel.FATAL, msg);

        private static void Write(LogLevel level, string msg)
        {
            string logMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {msg}";
            Console.WriteLine(logMsg);
            if (_listBox != null)
            {
                _listBox.Invoke(new Action(() => _listBox.Items.Add(logMsg)));
            }
            Directory.CreateDirectory(LogDir);
            File.AppendAllText(Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log"), logMsg + Environment.NewLine);
        }

        private static void AppendToListBox(string line)
        {
            _listBox.Items.Add(line);
            _listBox.TopIndex = _listBox.Items.Count - 1; 
        }

        private static void DeleteOldLogs()
        {
            foreach(var file in Directory.GetFiles(LogDir, "*.log"))
            {
                if (File.GetCreationTime(file) < DateTime.Now.AddDays(-7))
                {
                    File.Delete(file);
                }
            }
        }
    }
}