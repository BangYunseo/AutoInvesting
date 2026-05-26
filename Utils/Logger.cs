using AutoInvest.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace AutoInvest.Utils
{
    public enum LogLevel
    {
        INFO,
        WARN,
        ERROR,
        FATAL,
        QUANT
    }

    public static class Logger
    {
        private static readonly string LogDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public static void Initialize()
        {
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

        /// <summary>
        /// 퀀트 판단 근거를 상세히 기록합니다.
        /// 출력 형식: "[QUANT] QQQM [MEAN_REVERSION]: Position=0.07 ✓, RSI=26.3 ✓ → BUY"
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="conditions">충족된 조건 목록</param>
        /// <param name="signal">최종 신호 (BUY/SELL/HOLD)</param>
        /// <param name="strategyType">전략 유형</param>
        public static void LogQuant(
            string ticker,
            List<string> conditions,
            SmartOrderSignal signal,
            string strategyType = "MEAN_REVERSION")
        {
            string condStr = conditions.Count > 0
                ? string.Join(", ", conditions)
                : "조건 없음";
            string msg = $"[QUANT] {ticker} [{strategyType}]: {condStr} → {signal}";
            Write(LogLevel.QUANT, msg);
        }

        private static void Write(LogLevel level, string msg)
        {
            string logMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {msg}";
            Console.WriteLine(logMsg);
            Directory.CreateDirectory(LogDir);
            File.AppendAllText(Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log"), logMsg + Environment.NewLine);
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