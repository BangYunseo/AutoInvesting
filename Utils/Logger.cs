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

        /// <summary>
        /// 로그를 DB에 영구 적재하는 훅. 조립 지점(Program.cs)에서 SystemLogDAO.Insert로 연결합니다.
        /// null이면(예: DB 초기화 전) 콘솔/파일에만 기록됩니다.
        /// Logger가 Data 레이어를 직접 참조하지 않도록(순환 방지) 델리게이트로 주입받습니다.
        /// 인자: (발생시각, 레벨, 메시지)
        /// </summary>
        public static Action<DateTime, string, string>? DbSink;

        // ── DB 영구 적재 (실패 시 콘솔로만 보고 — Logger 재호출 시 무한 재귀 방지) ──
        private static void Persist(string level, string message)
        {
            var sink = DbSink;
            if (sink == null) return;
            try
            {
                sink(DateTime.Now, level, message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Logger] 로그 DB 적재 실패: {ex.Message}");
            }
        }

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
            Persist("INFO", msg);
        }

        public static void Error(string msg, Exception? ex = null)
        {
            if (ex != null)
                Log.Error(ex, msg);
            else
                Log.Error(msg);
            Persist("ERROR", ex != null ? $"{msg} | {ex.Message}" : msg);
        }

        public static void Warn(string msg)
        {
            Log.Warning(msg);
            Persist("WARN", msg);
        }

        public static void Fatal(string msg, Exception? ex = null)
        {
            if (ex != null)
                Log.Fatal(ex, msg);
            else
                Log.Fatal(msg);
            Persist("FATAL", ex != null ? $"{msg} | {ex.Message}" : msg);
        }

        public static void LogQuant(string msg)
        {
            // 퀀트 전용 로깅 - Information 레벨 사용하되 접두어 추가
            Log.Information($"[QUANT] {msg}");
            Persist("INFO", $"[QUANT] {msg}");
        }

        public static void LogQuant(string ticker, System.Collections.Generic.List<string> quantConditions, object signal, string strategyType)
        {
            string conditionsStr = quantConditions != null ? string.Join(", ", quantConditions) : "";
            string line = $"[QUANT] [{strategyType}] {ticker} | Signal: {signal} | Conditions: {conditionsStr}";
            Log.Information(line);
            Persist("INFO", line);
        }

        public static void FlushAndClose()
        {
            Log.CloseAndFlush();
        }
    }
}