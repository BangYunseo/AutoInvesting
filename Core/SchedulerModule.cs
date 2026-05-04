using AutoInvest.Core.Quant;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using System;
using System.Timers;

namespace AutoInvest.Core
{
    /// <summary>
    /// 예약 주문 스케줄러 (Phase 2.5 — 퀀트 + 리밸런싱 통합).
    /// 설정된 시각(ORDER_SCHEDULE)에 도달하면 SmartOrderEngine을 실행합니다.
    /// 리밸런싱 주기가 도래한 경우 RebalancingEngine도 실행합니다.
    /// 1분 간격으로 현재 시각을 확인하며, 당일 중복 실행을 방지합니다.
    /// </summary>
    public class SchedulerModule : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private readonly SessionManager _session;
        private DateTime _lastExecutedDate = DateTime.MinValue;

        /// <summary>스케줄러 실행 중 여부</summary>
        public bool IsRunning => _timer.Enabled;

        /// <summary>이벤트: 스마트 주문 실행 완료 시 발생</summary>
        public event Action<string>? OnOrderExecuted;

        public SchedulerModule(SessionManager session)
        {
            _session = session;
            _timer = new System.Timers.Timer(60_000); // 1분 간격
            _timer.Elapsed += OnTimerTick;
            _timer.AutoReset = true;
        }

        /// <summary>스케줄러 시작</summary>
        public void Start()
        {
            _timer.Start();
            Logger.Info("[Scheduler] 예약 주문 스케줄러 시작됨");
        }

        /// <summary>스케줄러 중지</summary>
        public void Stop()
        {
            _timer.Stop();
            Logger.Info("[Scheduler] 예약 주문 스케줄러 중지됨");
        }

        private async void OnTimerTick(object? sender, ElapsedEventArgs e)
        {
            try
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

                Logger.Info("[Scheduler] ▶ 예약 시각 도달 — 스마트 주문 실행 시작");
                _lastExecutedDate = now;

                // 로그인 확인
                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    var loginOk = await client.LoginAsync();
                    if (!loginOk)
                    {
                        Logger.Error("[Scheduler] 로그인 실패 — 주문 스킵");
                        return;
                    }
                }

                // 전략 로드
                var strategyName = AppConfigManager.Get("ACTIVE_STRATEGY", "안정형");
                var strategies = StrategyDAO.GetStrategy(strategyName);
                if (strategies.Count == 0)
                {
                    Logger.Warn("[Scheduler] 전략 데이터 없음 — 주문 스킵");
                    return;
                }

                // 투자금액
                var amountStr = AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000");
                decimal investAmount = decimal.Parse(amountStr);

                // ── 스마트 주문 실행 (퀀트 조건 판단 포함) ──
                var engine = new SmartOrderEngine(client);
                var results = await engine.ExecuteSmartOrdersAsync(strategies, investAmount);

                var summary = $"전략={strategyName}, 분석 {results.Count}건 완료";
                Logger.Info($"[Scheduler] ✔ 예약 주문 완료 — {summary}");

                // ── 리밸런싱 주기 확인 + 실행 ──
                if (RebalancingEngine.IsDue())
                {
                    Logger.Info("[Scheduler] ▶ 리밸런싱 주기 도래 — 리밸런싱 실행 시작");
                    var thresholdStr = AppConfigManager.Get("REBALANCE_THRESHOLD", "0.05");
                    decimal threshold = decimal.Parse(thresholdStr);
                    var rebalancer = new RebalancingEngine(client, threshold);
                    var rebalOrders = await rebalancer.ExecuteAsync(strategies);
                    summary += $", 리밸런싱 {rebalOrders.Count}건";
                    Logger.Info($"[Scheduler] ✔ 리밸런싱 완료 — {rebalOrders.Count}건 조정");
                }

                OnOrderExecuted?.Invoke(summary);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Scheduler] 예약 주문 실행 오류: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
