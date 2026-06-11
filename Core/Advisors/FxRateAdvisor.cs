using System.Threading.Tasks;
using AutoInvest.Data;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;

namespace AutoInvest.Core.Advisors
{
    /// <summary>
    /// 환율 기반 부가 조언 제공자 (Phase 5-e).
    /// 매수 의향이 있는 상황에서 현재 USD/KRW 환율이 최근 분포상 고점권(상위 분위수)이면
    /// 환차손 위험을 경고하고, 종목별 환헤지(H) ETF 대안을 제시합니다.
    ///
    /// 설정(appsettings.json > FxAdvisor):
    ///   Enabled        — 활성화 여부
    ///   LookbackDays   — 분포 산출 기간(일)
    ///   HighPercentile — 고환율 판정 분위수 (예: 0.80)
    ///   HedgeMap       — 종목 → 환헤지 대안 안내 문구 매핑
    /// </summary>
    public class FxRateAdvisor : IContextAdvisor
    {
        public string Name => "환율";

        public async Task<AdvisoryNoteDto?> EvaluateAsync(AdvisoryContext context)
        {
            // ── 비활성화 또는 매수 의향이 없으면 조언 불필요 ──
            if (AppConfigManager.Get("FX_ADVISOR_ENABLED", "1") != "1") return null;
            if (!context.HasBuyIntent) return null;

            int lookbackDays = int.TryParse(AppConfigManager.Get("FX_LOOKBACK_DAYS", "60"), out var d) ? d : 60;
            decimal highPercentile = decimal.TryParse(AppConfigManager.Get("FX_HIGH_PERCENTILE", "0.80"), out var p) ? p : 0.80m;

            var (current, rank, highThreshold, isHigh, sampleCount) =
                await ExchangeRateService.GetUsdKrwContextAsync(lookbackDays, highPercentile);

            // ── 고환율이 아니거나 표본이 없으면 조언하지 않음 ──
            if (!isHigh || sampleCount == 0) return null;

            var note = new AdvisoryNoteDto
            {
                Source = Name,
                Severity = AdvisorySeverity.WARNING,
                Title = $"환율 高 — 현재 {current:N0}원 (최근 {lookbackDays}일 중 상위 {(1 - rank):P0})",
                Message =
                    $"현재 USD/KRW 환율이 최근 {lookbackDays}일 분포의 상위 {(1 - rank):P0} 수준(상위 {highPercentile:P0} 경계 {highThreshold:N0}원)입니다. " +
                    $"지금 달러 ETF를 원화로 매수하면 환차손 위험이 큽니다. 환율 안정 후 분할 진입하거나, 아래 환헤지(H) 상품을 고려하세요."
            };

            // ── 종목별 환헤지 대안 매핑 ──
            var hedgeMap = AppConfigManager.GetMap("FxAdvisor:HedgeMap");
            if (hedgeMap.TryGetValue(context.Ticker, out var alt) && !string.IsNullOrWhiteSpace(alt))
                note.SuggestedAlternatives.Add(alt);

            return note;
        }
    }
}
