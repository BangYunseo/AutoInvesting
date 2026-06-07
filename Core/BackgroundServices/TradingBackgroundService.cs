using AutoInvest.Core.Quant;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoInvest.Core.BackgroundServices
{
    /// <summary>
    /// 자동 매매 시스템의 메인 백그라운드 데몬 (ASP.NET Core Worker).
    /// 기존 SchedulerModule의 1분 주기를 대체합니다.
    /// </summary>
    public class TradingBackgroundService : BackgroundService
    {
        private readonly SessionManager _session;
        private DateTime _lastExecutedDate = DateTime.MinValue;

        public TradingBackgroundService(SessionManager session)
        {
            _session = session;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.Info("[Worker] Trading Background Service가 시작되었습니다.");

            try
            {
                // 1분 간격으로 루프 실행
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await CheckAndExecuteOrderAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[Worker] 반복 루프 중 오류 발생: {ex.Message}");
                    }

                    // 다음 1분(60초)까지 대기
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                Logger.Info("[Worker] 서비스 종료 신호(SIGTERM) 수신. 진행 중인 작업을 마무리하고 종료합니다.");
            }
            finally
            {
                Logger.Info("[Worker] Trading Background Service가 안전하게 종료되었습니다.");
            }
        }

        private async Task CheckAndExecuteOrderAsync()
        {
            // 당일 이미 실행했으면 스킵
            if (_lastExecutedDate.Date == DateTime.Now.Date)
                return;

            var schedule = AppConfigManager.Get("ORDER_SCHEDULE", "22:30");
            var parts = schedule.Split(':');
            if (parts.Length != 2) return;

            int targetHour = int.Parse(parts[0]);
            int targetMin = int.Parse(parts[1]);
            var now = DateTime.Now;

            // 예약 시각 ±1분 범위 체크
            if (now.Hour != targetHour || now.Minute != targetMin)
                return;

            Logger.Info("[Worker] ▶ 예약 시각 도달 — 스마트 주문 실행 시작");
            _lastExecutedDate = now;

            // 로그인 확인
            var client = _session.GetClient();
            if (!client.IsLoggedIn)
            {
                var loginOk = await client.LoginAsync();
                if (!loginOk)
                {
                    Logger.Error("[Worker] 로그인 실패 — 주문 스킵");
                    return;
                }
            }

            // 전략 로드
            var strategyName = AppConfigManager.Get("ACTIVE_STRATEGY", "안정형");
            var strategies = StrategyDAO.GetStrategy(strategyName);
            if (strategies.Count == 0)
            {
                Logger.Warn("[Worker] 전략 데이터 없음 — 주문 스킵");
                return;
            }

            // 투자금액
            var amountStr = AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000");
            decimal investAmount = decimal.Parse(amountStr);

            // ── 스마트 주문 실행 (퀀트 조건 판단 포함) ──
            var engine = new SmartOrderEngine(client, _session.GetAnalyzer());
            var results = await engine.ExecuteSmartOrdersAsync(strategies, investAmount);

            var summary = $"전략={strategyName}, 분석 {results.Count}건 완료";
            Logger.Info($"[Worker] ✔ 예약 주문 완료 — {summary}");

            // ── 리밸런싱 주기 확인 + 실행 ──
            if (RebalancingEngine.IsDue())
            {
                Logger.Info("[Worker] ▶ 리밸런싱 주기 도래 — 리밸런싱 실행 시작");
                var thresholdStr = AppConfigManager.Get("REBALANCE_THRESHOLD", "0.05");
                decimal threshold = decimal.Parse(thresholdStr);
                var rebalancer = new RebalancingEngine(client, threshold);
                var rebalOrders = await rebalancer.ExecuteAsync(strategies);
                Logger.Info($"[Worker] ✔ 리밸런싱 완료 — {rebalOrders.Count}건 조정");
            }

            // ── Phase 5-b: AI 성과 과거 기록 평가 (7일 전 데이터) ──
            await EvaluatePastAiPerformanceAsync(client);

            // ── Phase 5-b: 일일 운용 보고서 (이메일 발송) ──
            await SendDailyReportAsync(results);
        }

        private async Task EvaluatePastAiPerformanceAsync(IBrokerClient client)
        {
            try
            {
                Logger.Info("[Worker] ▶ 과거 AI 성과 평가 시작 (7일 경과 데이터)");
                var unevaluated = AiPerformanceDAO.GetUnevaluated(7);
                foreach (var perf in unevaluated)
                {
                    decimal currentPrice = await client.GetCurrentPriceAsync(perf.Ticker);
                    if (currentPrice <= 0) continue;

                    decimal winRate = 0m;
                    if (perf.Signal == "BUY")
                    {
                        winRate = currentPrice > perf.PriceAtSignal ? 1m : 0m;
                    }
                    else if (perf.Signal == "SELL")
                    {
                        winRate = currentPrice < perf.PriceAtSignal ? 1m : 0m;
                    }

                    AiPerformanceDAO.UpdateEvaluation(perf.PerfId, currentPrice, winRate);
                }
                if (unevaluated.Count > 0)
                {
                    Logger.Info($"[Worker] ✔ AI 과거 성과 평가 완료 — {unevaluated.Count}건 업데이트");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Worker] AI 성과 평가 중 오류: {ex.Message}");
            }
        }

        private async Task SendDailyReportAsync(System.Collections.Generic.List<SmartOrderResult> todayResults)
        {
            try
            {
                // 1. 토큰 사용량
                int totalTokens = TokenUsageDAO.GetTodayTotalTokens();
                
                // 2. AI 성과
                var (perfCount, avgWinRate) = AiPerformanceDAO.GetOverallPerformance();

                // 3. 오늘 매매 내역 HTML
                string ordersHtml = todayResults.Count == 0 ? "<p>오늘 발생한 매매 신호가 없습니다.</p>" : "<ul>";
                foreach (var r in todayResults)
                {
                    ordersHtml += $"<li><strong>{r.Ticker}</strong>: {r.Signal} ({r.Reason})</li>";
                }
                if (todayResults.Count > 0) ordersHtml += "</ul>";

                string htmlBody = $@"
                    <h2>AutoInvesting 일일 운용 보고서 ({DateTime.Now:yyyy-MM-dd})</h2>
                    <hr/>
                    <h3>1. 금일 매매 내역</h3>
                    {ordersHtml}
                    <br/>
                    <h3>2. AI 성과 요약</h3>
                    <ul>
                        <li>현재까지 평가 완료된 신호 건수: {perfCount}건</li>
                        <li><strong>AI 누적 적중률(Win Rate): {avgWinRate:P1}</strong></li>
                    </ul>
                    <br/>
                    <h3>3. AI API 토큰 소모량</h3>
                    <ul>
                        <li>금일 사용 토큰 합계: <strong>{totalTokens:N0} tokens</strong></li>
                    </ul>
                    <hr/>
                    <p style='color: gray; font-size: 12px;'>본 메일은 AutoInvesting 시스템에서 자동 발송되었습니다.</p>";

                await NotificationService.SendEmailAsync("일일 운용 보고서", htmlBody);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Worker] 일일 보고서 발송 중 오류: {ex.Message}");
            }
        }
    }
}
