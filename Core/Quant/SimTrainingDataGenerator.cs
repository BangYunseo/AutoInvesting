using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoInvest.Core.Quant
{
    /// <summary>
    /// Phase 6-a: SimBroker(시뮬레이션) 기반 AI 학습데이터 대량 생성기.
    ///
    /// 실거래로는 종목당 하루 1건씩만 쌓여 피드백 엔진·적응형 임계값에 필요한 누적 데이터가 매우 느리게 모입니다.
    /// 본 생성기는 SimBroker의 랜덤 가격경로로 분석 파이프라인(SmartOrderEngine)을 반복 실행하여
    /// 라벨링된 스냅샷을 빠르게 합성하고, 출처를 "SIM"으로 태깅해 실데이터(REAL)와 격리합니다.
    ///
    /// ⚠️ 안전:
    ///  - 내부에서 <see cref="SimBrokerClient"/>와 Mock <see cref="AiMarketAnalyzer"/>를 직접 생성합니다.
    ///    (SessionManager를 경유하지 않으므로 운영이 Gemini로 설정돼 있어도 토큰 비용 0, 실 브로커 절대 미접촉)
    ///  - 저장되는 스냅샷은 SmartOrderEngine이 DATA_SOURCE='SIM'으로 태깅하므로 REAL 분석을 오염시키지 않습니다.
    /// </summary>
    public static class SimTrainingDataGenerator
    {
        /// <summary>학습데이터 생성 요청.</summary>
        public class GenerateRequest
        {
            /// <summary>대상 종목 목록 (비우면 SimBroker 기본 종목 세트 사용)</summary>
            public List<string>? Tickers { get; set; }

            /// <summary>종목당 생성할 일별 스냅샷 수 (가상 일자를 1일 간격으로 분산 저장)</summary>
            public int SnapshotsPerTicker { get; set; } = 50;

            /// <summary>전략 유형 (MEAN_REVERSION / MOMENTUM / MIXED)</summary>
            public string StrategyType { get; set; } = "MEAN_REVERSION";
        }

        /// <summary>학습데이터 생성 결과.</summary>
        public class GenerateResult
        {
            public int InsertedCount { get; set; }
            public int TickerCount { get; set; }
            public Dictionary<string, int> PerTicker { get; set; } = new();
        }

        /// <summary>SimBroker 기본 종목 세트 (요청에 종목이 없을 때 사용).</summary>
        private static readonly List<string> DefaultTickers = new() { "SCHD", "QQQM", "GLD", "JEPI", "SPLG" };

        /// <summary>
        /// 시뮬레이션 학습데이터를 생성하여 TB_MARKET_SNAPSHOT에 SIM 출처로 저장합니다.
        /// </summary>
        public static async Task<GenerateResult> GenerateAsync(GenerateRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            var tickers = (req.Tickers != null && req.Tickers.Count > 0) ? req.Tickers : DefaultTickers;
            int perTicker = Math.Max(1, req.SnapshotsPerTicker);
            string strategyType = string.IsNullOrWhiteSpace(req.StrategyType) ? "MEAN_REVERSION" : req.StrategyType;

            // ── SimBroker + Mock AI 직접 생성 (비용 0, REAL 미접촉) ──
            var broker = new SimBrokerClient();
            var analyzer = new AiMarketAnalyzer();
            await broker.LoginAsync();

            var engine = new SmartOrderEngine(broker, analyzer);

            var result = new GenerateResult { TickerCount = tickers.Count };
            // forward-return 매칭이 가능하도록 가상 일자를 과거→현재 방향으로 1일 간격 분산
            DateTime startDate = DateTime.Today.AddDays(-perTicker);

            Logger.Info($"[SimTrainingData] 생성 시작 — 종목 {tickers.Count}개 × {perTicker}건, 전략={strategyType}");

            foreach (var ticker in tickers)
            {
                int inserted = 0;
                for (int i = 0; i < perTicker; i++)
                {
                    try
                    {
                        DateTime snapDate = startDate.AddDays(i);
                        await engine.AnalyzeAndSaveSnapshotAsync(ticker, strategyType, snapDate);
                        inserted++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[SimTrainingData] {ticker} {i + 1}번째 스냅샷 생성 실패: {ex.Message}");
                    }
                }
                result.PerTicker[ticker] = inserted;
                result.InsertedCount += inserted;
            }

            Logger.Info($"[SimTrainingData] 생성 완료 — 총 {result.InsertedCount}건 저장 (SIM)");
            return result;
        }
    }
}
