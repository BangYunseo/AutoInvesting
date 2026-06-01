using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoInvest.Core.Quant
{
    public class SellStrategyManager
    {
        private readonly IBrokerClient _broker;

        public SellStrategyManager(IBrokerClient broker)
        {
            _broker = broker;
        }

        public async Task ProcessActivePlansAsync(string ticker, decimal currentPrice, IndicatorDto indicators)
        {
            var plans = SellPlanDAO.GetPlansByTicker(ticker).FindAll(p => p.Status == "ACTIVE");

            foreach (var plan in plans)
            {
                try
                {
                    await EvaluatePlanAsync(plan, currentPrice, indicators);
                }
                catch (Exception ex)
                {
                    Logger.Error($"[SellStrategyManager] 플랜 평가 실패 (ID: {plan.PlanId}): {ex.Message}");
                }
            }
        }

        private async Task EvaluatePlanAsync(SellPlanDto plan, decimal currentPrice, IndicatorDto indicators)
        {
            int remainingQty = plan.TargetQty - plan.SoldQty;
            if (remainingQty <= 0)
            {
                plan.Status = "COMPLETED";
                SellPlanDAO.Update(plan);
                return;
            }

            bool shouldSell = false;
            int sellQty = 0;
            var jsonDoc = JsonDocument.Parse(plan.Params);

            if (plan.StrategyType == "PRICE")
            {
                // Params: {"TargetPrice": 250, "TrancheQty": 2}
                if (jsonDoc.RootElement.TryGetProperty("TargetPrice", out var targetProp) &&
                    jsonDoc.RootElement.TryGetProperty("TrancheQty", out var qtyProp))
                {
                    decimal targetPrice = targetProp.GetDecimal();
                    int trancheQty = qtyProp.GetInt32();

                    if (currentPrice >= targetPrice)
                    {
                        shouldSell = true;
                        sellQty = Math.Min(trancheQty, remainingQty);
                    }
                }
            }
            else if (plan.StrategyType == "TIME")
            {
                // Params: {"NextExecutionDate": "2026-06-01", "TrancheQty": 2}
                if (jsonDoc.RootElement.TryGetProperty("NextExecutionDate", out var dateProp) &&
                    jsonDoc.RootElement.TryGetProperty("TrancheQty", out var qtyProp))
                {
                    if (DateTime.TryParse(dateProp.GetString(), out DateTime nextExec))
                    {
                        if (DateTime.Now.Date >= nextExec.Date)
                        {
                            shouldSell = true;
                            sellQty = Math.Min(qtyProp.GetInt32(), remainingQty);
                        }
                    }
                }
            }
            else if (plan.StrategyType == "CHART")
            {
                // Params: {"Condition": "MA20_BREAK", "TrancheQty": 10}
                if (jsonDoc.RootElement.TryGetProperty("Condition", out var condProp) &&
                    jsonDoc.RootElement.TryGetProperty("TrancheQty", out var qtyProp))
                {
                    string condition = condProp.GetString() ?? "";
                    
                    // MA20 break down
                    // Using BbMiddle as MA20 approximation
                    if (condition == "MA20_BREAK" && currentPrice < indicators.BbMiddle)
                    {
                        shouldSell = true;
                        sellQty = Math.Min(qtyProp.GetInt32(), remainingQty);
                    }
                }
            }

            if (shouldSell && sellQty > 0)
            {
                string orderNo = await _broker.PlaceSellOrderAsync(plan.Ticker, sellQty, currentPrice);
                
                plan.SoldQty += sellQty;
                if (plan.SoldQty >= plan.TargetQty)
                {
                    plan.Status = "COMPLETED";
                }

                // Update NextExecutionDate for TIME strategy
                if (plan.StrategyType == "TIME")
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(plan.Params) ?? new Dictionary<string, object>();
                    dict["NextExecutionDate"] = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                    plan.Params = JsonSerializer.Serialize(dict);
                }

                SellPlanDAO.Update(plan);
                
                TradeHistoryDAO.Insert(new TradeHistoryDto
                {
                    TradeDate = DateTime.Now,
                    Ticker = plan.Ticker,
                    OrderType = "SELL",
                    Qty = sellQty,
                    Price = currentPrice,
                    Status = "FILLED",
                    OrderNo = orderNo
                });

                string msg = $"[{plan.StrategyType} 분할매도] {plan.Ticker} {sellQty}주 매도 완료. (진행률: {plan.SoldQty}/{plan.TargetQty})";
                Logger.Info(msg);
                _ = NotificationService.SendEmailAsync("분할매도 체결 알림", msg);
            }
        }
    }
}
