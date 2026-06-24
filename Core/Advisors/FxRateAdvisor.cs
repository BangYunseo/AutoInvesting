using System.Threading.Tasks;
using AutoInvest.Data;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;

namespace AutoInvest.Core.Advisors
{
    /// <summary>
    /// 환율 기반 매매 컨텍스트 조언 제공자.
    /// 현재 USD/KRW 환율의 최근 분포상 위치(백분위)를 보고, 매매 방향에 맞춰 유불리를 설명합니다.
    ///   • 매수: 환율이 낮으면(하위 분위수) 유리(INFO), 높으면(상위 분위수) 환차손 경고(WARNING) + 환헤지(H) 대안 제시
    ///   • 매도: 환율이 높으면 원화 환산 유리(INFO), 낮으면 불리 경고(WARNING)
    /// 매매 판정 자체를 막지는 않으며(설명·경고 전용), 결과에 첨부되어 리포트/화면에 표시됩니다.
    ///
    /// 설정(appsettings.json > FxAdvisor / 환경변수):
    ///   Enabled        — 활성화 여부 (FX_ADVISOR_ENABLED)
    ///   LookbackDays   — 분포 산출 기간(일) (FX_LOOKBACK_DAYS)
    ///   HighPercentile — 고환율 판정 분위수 (FX_HIGH_PERCENTILE, 예: 0.80)
    ///   LowPercentile  — 저환율 판정 분위수 (FX_LOW_PERCENTILE, 예: 0.20)
    ///   HedgeMap       — 종목 → 환헤지 대안 안내 문구 매핑
    /// </summary>
    public class FxRateAdvisor : IContextAdvisor
    {
        public string Name => "환율";

        public async Task<AdvisoryNoteDto?> EvaluateAsync(AdvisoryContext context)
        {
            // ── 비활성화이거나 매수/매도 의향이 모두 없으면 조언 불필요 ──
            if (AppConfigManager.Get("FX_ADVISOR_ENABLED", "1") != "1") return null;
            bool buy = context.HasBuyIntent;
            bool sell = context.HasSellIntent;
            if (!buy && !sell) return null;

            int lookbackDays = int.TryParse(AppConfigManager.Get("FX_LOOKBACK_DAYS", "60"), out var d) ? d : 60;
            decimal highPercentile = decimal.TryParse(AppConfigManager.Get("FX_HIGH_PERCENTILE", "0.80"), out var p) ? p : 0.80m;
            decimal lowPercentile = decimal.TryParse(AppConfigManager.Get("FX_LOW_PERCENTILE", "0.20"), out var lp) ? lp : 0.20m;

            var (current, rank, highThreshold, isHigh, sampleCount) =
                await ExchangeRateService.GetUsdKrwContextAsync(lookbackDays, highPercentile);

            // ── 표본이 없으면 판단 보류 ──
            if (sampleCount == 0) return null;

            bool isLow = rank <= lowPercentile;
            string distPos = $"최근 {lookbackDays}일 분포의 백분위 {rank:P0}, 현재 {current:N0}원";

            // ── 매수 의향: 환율이 낮으면 유리(INFO), 높으면 환차손 경고(WARNING) ──
            if (buy)
            {
                if (isLow)
                {
                    return new AdvisoryNoteDto
                    {
                        Source = Name,
                        Severity = AdvisorySeverity.INFO,
                        Title = $"환율 低 — 매수에 유리 (현재 {current:N0}원)",
                        Message =
                            $"현재 USD/KRW 환율이 {distPos}로 낮은 편입니다. " +
                            $"같은 원화로 더 많은 달러 자산을 살 수 있어, 환율 측면에서 매수 타이밍이 유리합니다."
                    };
                }
                if (isHigh)
                {
                    var note = new AdvisoryNoteDto
                    {
                        Source = Name,
                        Severity = AdvisorySeverity.WARNING,
                        Title = $"환율 高 — 환차손 위험 (현재 {current:N0}원, 상위 {(1 - rank):P0})",
                        Message =
                            $"현재 USD/KRW 환율이 {distPos}로 상위권입니다(상위 {highPercentile:P0} 경계 {highThreshold:N0}원). " +
                            $"지금 달러 ETF를 원화로 매수하면 환차손 위험이 큽니다. 환율 안정 후 분할 진입하거나, 아래 환헤지(H) 상품을 고려하세요."
                    };
                    var hedgeMap = AppConfigManager.GetMap("FxAdvisor:HedgeMap");
                    if (hedgeMap.TryGetValue(context.Ticker, out var alt) && !string.IsNullOrWhiteSpace(alt))
                        note.SuggestedAlternatives.Add(alt);
                    return note;
                }
                return null; // 중립 구간 — 조언 없음
            }

            // ── 매도 의향: 환율이 높으면 원화 환산 유리(INFO), 낮으면 불리 경고(WARNING) ──
            if (isHigh)
            {
                return new AdvisoryNoteDto
                {
                    Source = Name,
                    Severity = AdvisorySeverity.INFO,
                    Title = $"환율 高 — 매도에 유리 (현재 {current:N0}원, 상위 {(1 - rank):P0})",
                    Message =
                        $"현재 USD/KRW 환율이 {distPos}로 높은 편입니다(상위 {highPercentile:P0} 경계 {highThreshold:N0}원). " +
                        $"달러 매도 대금을 원화로 환산할 때 환차익이 더해져, 환율 측면에서 매도 타이밍이 유리합니다."
                };
            }
            if (isLow)
            {
                return new AdvisoryNoteDto
                {
                    Source = Name,
                    Severity = AdvisorySeverity.WARNING,
                    Title = $"환율 低 — 원화 환산 불리 (현재 {current:N0}원)",
                    Message =
                        $"현재 USD/KRW 환율이 {distPos}로 낮은 편입니다. " +
                        $"지금 매도하면 원화 환산액이 줄어 불리합니다. 환율이 회복된 뒤 매도를 고려하세요."
                };
            }
            return null; // 중립 구간 — 조언 없음
        }
    }
}
