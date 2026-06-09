using AutoInvest.Core.Quant;
using AutoInvest.Data.DTO;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// 외부 크론잡(Cron-job.org, GitHub Actions 등)에 의해 하루에 한 번 호출되는 일일 사이클 실행기.
    /// 기존 TradingBackgroundService의 역할을 대체합니다.
    /// </summary>
    public class DailyExecutionService
    {
        private readonly SessionManager _session;

        public DailyExecutionService(SessionManager session)
        {
            _session = session;
        }

        public async Task<string> RunDailyCycleAsync()
        {
            Logger.Info("[DailyCycle] ▶ 외부 호출에 의해 일일 매매 사이클이 시작되었습니다.");

            // 조기 종료(로그인 실패·전략 0건·예외) 시에도 일일 보고서는 항상 발송되도록
            // 결과/평가/상태 사유를 누적해 두고, finally에서 한 번 발송한다.
            var results = new List<SmartOrderResult>();
            var evaluatedItems = new List<AiPerformanceDto>();
            string statusNote = "";
            string summary = "";

            try
            {
                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    var loginOk = await client.LoginAsync();
                    if (!loginOk)
                    {
                        statusNote = "브로커 로그인에 실패하여 오늘은 매매와 AI 평가를 건너뛰었습니다.";
                        Logger.Error("[DailyCycle] 로그인 실패 — 주문 스킵");
                        return "로그인 실패 — 주문 스킵"; // finally에서 보고서 발송 후 반환
                    }
                }

                // 1. 전략 로드
                var strategyName = AppConfigManager.Get("ACTIVE_STRATEGY", "안정형");
                var strategies = StrategyDAO.GetStrategy(strategyName);
                if (strategies.Count == 0)
                {
                    statusNote = $"활성 전략 '{strategyName}'에 등록된 종목이 없어 오늘은 매매를 건너뛰었습니다.";
                    Logger.Warn("[DailyCycle] 전략 데이터 없음 — 주문 스킵");
                }
                else
                {
                    // 2. 스마트 주문 실행
                    var amountStr = AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000");
                    decimal investAmount = decimal.Parse(amountStr);

                    var engine = new SmartOrderEngine(client, _session.GetAnalyzer());
                    results = await engine.ExecuteSmartOrdersAsync(strategies, investAmount);

                    summary = $"전략={strategyName}, 분석 {results.Count}건 완료";
                    Logger.Info($"[DailyCycle] ✔ 스마트 주문 완료 — {summary}");

                    // 3. 리밸런싱 실행
                    if (RebalancingEngine.IsDue())
                    {
                        Logger.Info("[DailyCycle] ▶ 리밸런싱 주기 도래 — 리밸런싱 실행 시작");
                        var thresholdStr = AppConfigManager.Get("REBALANCE_THRESHOLD", "0.05");
                        decimal threshold = decimal.Parse(thresholdStr);
                        var rebalancer = new RebalancingEngine(client, threshold);
                        var rebalOrders = await rebalancer.ExecuteAsync(strategies);
                        Logger.Info($"[DailyCycle] ✔ 리밸런싱 완료 — {rebalOrders.Count}건 조정");
                    }
                }

                // 4. AI 성과 과거 기록 평가 (7일 전 데이터) — 전략 유무와 무관하게 수행
                evaluatedItems = await EvaluatePastAiPerformanceAsync(client);
            }
            catch (Exception ex)
            {
                statusNote = $"사이클 처리 중 오류가 발생했습니다: {ex.Message}";
                Logger.Error($"[DailyCycle] 사이클 처리 중 오류: {ex.Message}");
            }
            finally
            {
                // 5. 일일 운용 보고서 — 어떤 경우에도 항상 발송
                await SendDailyReportAsync(results, evaluatedItems, statusNote);
            }

            Logger.Info("[DailyCycle] ✔ 일일 매매 사이클이 무사히 종료되었습니다.");
            return string.IsNullOrEmpty(statusNote) ? $"사이클 완료: {summary}" : statusNote;
        }

        private async Task<List<AiPerformanceDto>> EvaluatePastAiPerformanceAsync(IBrokerClient client)
        {
            var evaluatedList = new List<AiPerformanceDto>();
            try
            {
                Logger.Info("[DailyCycle] ▶ 과거 AI 성과 평가 시작 (7일 경과 데이터)");
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
                    
                    // 상세 보고서를 위해 현재 가격과 적중 여부 기록
                    perf.PriceLater = currentPrice;
                    perf.WinRate = winRate;
                    evaluatedList.Add(perf);
                }
                if (unevaluated.Count > 0)
                {
                    Logger.Info($"[DailyCycle] ✔ AI 과거 성과 평가 완료 — {unevaluated.Count}건 업데이트");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyCycle] AI 성과 평가 중 오류: {ex.Message}");
            }
            return evaluatedList;
        }

        private async Task SendDailyReportAsync(List<SmartOrderResult> todayResults, List<AiPerformanceDto> evaluatedItems, string statusNote = "")
        {
            try
            {
                int totalTokens = TokenUsageDAO.GetTodayTotalTokens();
                var (perfCount, avgWinRate) = AiPerformanceDAO.GetOverallPerformance();

                // 조기 종료/오류 등으로 매매를 건너뛴 경우 사유를 보고서 상단에 안내한다.
                string noticeHtml = string.IsNullOrEmpty(statusNote)
                    ? ""
                    : $"<p style='color:#b8860b;'><strong>ℹ️ 안내:</strong> {statusNote}</p>";

                string ordersHtml = noticeHtml + (todayResults.Count == 0 ? "<p>오늘 발생한 매매 신호가 없어서 아무 종목도 사거나 팔지 않았습니다.</p>" : "<ul>");
                foreach (var r in todayResults)
                {
                    string actionStr = r.Signal == SmartOrderSignal.BUY ? "샀습니다" : (r.Signal == SmartOrderSignal.SELL ? "팔았습니다" : "그대로 두었습니다");
                    ordersHtml += $"<li><strong>{r.Ticker}</strong> 종목을 <strong>{actionStr}</strong>. (이유: {r.Reason})</li>";
                }
                if (todayResults.Count > 0) ordersHtml += "</ul>";

                string evalHtml = "";
                if (evaluatedItems.Count == 0)
                {
                    evalHtml = "<p>오늘은 일주일 전에 예측했던 기록 중 새로 채점할 항목이 없습니다.</p>";
                }
                else
                {
                    evalHtml = "<ul>";
                    foreach (var item in evaluatedItems)
                    {
                        string action = item.Signal == "BUY" ? "오를 것" : "내릴 것";
                        string result = item.WinRate == 1m ? "정확하게 맞혔습니다" : "틀렸습니다";
                        string color = item.WinRate == 1m ? "#28a745" : "#dc3545"; // 녹색 또는 빨간색
                        
                        evalHtml += $@"
                            <li style='margin-bottom: 10px;'>
                                7일 전에 AI가 <strong>{item.Ticker}</strong> 종목의 가격이 <strong>{action}</strong>이라고 예측했었어요.<br/>
                                당시 가격은 {item.PriceAtSignal:N2}달러였는데, 7일이 지난 지금은 {item.PriceLater:N2}달러가 되었습니다.<br/>
                                따라서 AI의 예측은 <strong style='color: {color};'>{result}</strong>!
                            </li>";
                    }
                    evalHtml += "</ul>";
                }

                // 템플릿 파일 읽기
                string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "DailyReportTemplate.html");
                string htmlBody = System.IO.File.ReadAllText(templatePath);

                // 값 치환
                htmlBody = htmlBody.Replace("{{DATE}}", DateTime.Now.ToString("yyyy-MM-dd"))
                                   .Replace("{{ORDERS}}", ordersHtml)
                                   .Replace("{{EVAL_DETAILS}}", evalHtml)
                                   .Replace("{perfCount}", perfCount.ToString())
                                   .Replace("{avgWinRate}", avgWinRate.ToString("P1"))
                                   .Replace("{totalTokens}", totalTokens.ToString("N0"));

                await NotificationService.SendEmailAsync("일일 운용 보고서", htmlBody);
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyCycle] 일일 보고서 발송 중 오류: {ex.Message}");
            }
        }
    }
}
