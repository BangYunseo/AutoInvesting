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

            Logger.Info("[Worker] Trading Background Service가 종료되었습니다.");
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
            var engine = new SmartOrderEngine(client);
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
        }
    }
}
