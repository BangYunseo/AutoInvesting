using Serilog;
using System;
using System.IO;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 시스템 로깅 유틸리티 (Serilog 래퍼).
    /// </summary>
    public static class Logger
    {
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(Path.Combine(logDir, "system-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            _initialized = true;
        }

        public static void Info(string msg)
        {
            Log.Information(msg);
        }

        public static void Error(string msg, Exception? ex = null)
        {
            if (ex != null)
                Log.Error(ex, msg);
            else
                Log.Error(msg);
        }

        public static void Warn(string msg)
        {
            Log.Warning(msg);
        }

        public static void Fatal(string msg, Exception? ex = null)
        {
            if (ex != null)
                Log.Fatal(ex, msg);
            else
                Log.Fatal(msg);
        }

        public static void LogQuant(string msg)
        {
            // 퀀트 전용 로깅 - Information 레벨 사용하되 접두어 추가
            Log.Information($"[QUANT] {msg}");
        }

        public static void LogQuant(string ticker, System.Collections.Generic.List<string> quantConditions, object signal, string strategyType)
        {
            string conditionsStr = quantConditions != null ? string.Join(", ", quantConditions) : "";
            Log.Information($"[QUANT] [{strategyType}] {ticker} | Signal: {signal} | Conditions: {conditionsStr}");
        }

        public static void FlushAndClose()
        {
            Log.CloseAndFlush();
        }
    }
}