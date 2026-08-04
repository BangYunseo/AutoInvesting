using AutoInvest.Data.DTO;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// 외부 크론잡(Cron-job.org, GitHub Actions 등)에 의해 매수 주기마다 호출되는 적립식 사이클 실행기.
    ///
    /// 백테스트 결과 "타이밍 판단은 잘해야 본전, 실제로는 손해"로 검증되어 퀀트/AI 판단 레이어를
    /// 제거하고, 정해진 날 설정 종목을 정수 단위로 매수→기록→메일 발송만 수행합니다.
    /// </summary>
    public class DailyExecutionService
    {
        private readonly SessionManager _session;

        public DailyExecutionService(SessionManager session)
        {
            _session = session;
        }

        /// <summary>
        /// 적립은 KST 월 1회만 실행합니다. 이미 적립한 월은 TB_APP_CONFIG의
        /// DCA_LAST_RUN_MONTH("yyyy-MM")에 기록되며, 같은 달 재호출은 스킵됩니다.
        /// 거래이력이 아니라 전용 마커를 쓰는 이유: 수동 단일 매수가 월 적립을 오판하지 않게 하기 위함.
        /// </summary>
        private const string LastRunMonthKey = "DCA_LAST_RUN_MONTH";

        /// <summary>현재(KST=UTC+9) 월을 "yyyy-MM"으로 반환합니다.</summary>
        private static string CurrentKstMonth() => DateTime.UtcNow.AddHours(9).ToString("yyyy-MM");

        /// <summary>
        /// 적립식(DCA) 자동 매수 사이클을 실행합니다 (판단 없는 단순 자동화).
        /// 퀀트/AI 판단을 하지 않고, 설정한 종목별 고정 수량(Dca:Quantities)을 그대로 매수합니다.
        ///
        /// 월 1회 멱등 가드: 이번 달(KST) 이미 적립했으면 매수하지 않고 스킵합니다.
        /// 크론이 매일(월초부터) 호출해도 처음 성공하는 날 1회만 적립되고, 성공 후 그 달 남은
        /// 호출은 모두 스킵되며, 실패(접수 0건)한 날은 마커가 남지 않아 다음 날 자동 재시도됩니다.
        /// </summary>
        /// <param name="force">
        /// true면 당월 가드를 무시하고 한 번 더 적립합니다 (사람이 화면에서 명시적으로 추가 매수할 때).
        /// 가드는 크론의 매일 재호출을 막기 위한 것이므로, 크론 경로는 이 값을 넘기지 않습니다.
        /// </param>
        public async Task<string> RunDcaCycleAsync(bool force = false)
        {
            Logger.Info("[DcaCycle] ▶ 적립식 자동 매수 사이클이 시작되었습니다.");
            var result = new DcaCycleResult();
            string statusNote = "";

            // ── 월 1회 멱등 가드: 이번 달(KST) 이미 적립했으면 스킵 ──
            string thisMonth = CurrentKstMonth();
            string lastRunMonth = AppConfigManager.Get(LastRunMonthKey, "");
            if (lastRunMonth == thisMonth && !force)
            {
                statusNote = $"이번 달({thisMonth}) 적립이 이미 완료되어 매수를 건너뜁니다.";
                Logger.Info($"[DcaCycle] 이번 달({thisMonth}) 적립 완료 상태 — 매수 스킵");
                return statusNote;
            }
            if (lastRunMonth == thisMonth)
                Logger.Warn($"[DcaCycle] 강제 실행 — 이번 달({thisMonth}) 적립 완료 상태에서 추가 매수를 진행합니다.");

            try
            {
                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    var loginOk = await client.LoginAsync();
                    if (!loginOk)
                    {
                        statusNote = "브로커 로그인에 실패하여 오늘은 적립식 매수를 건너뛰었습니다.";
                        Logger.Error("[DcaCycle] 로그인 실패 — 매수 스킵");
                        return statusNote; // finally에서 보고서 발송 후 반환
                    }
                }

                // ── 종목별 매수 수량·예산 로드 (DB 우선 → appsettings 폴백) ──
                var (quantities, budget) = DcaSettings.Load();

                if (quantities.Count == 0)
                {
                    statusNote = "적립 수량(DCA Quantities) 설정이 비어 있어 오늘은 매수를 건너뛰었습니다.";
                    Logger.Warn("[DcaCycle] 매수 수량 없음 — 매수 스킵");
                }
                else
                {
                    var engine = new DcaAccumulationEngine(client);
                    result = await engine.AccumulateAsync(quantities, budget);
                    Logger.Info($"[DcaCycle] ✔ 적립식 주문 접수 완료 — {result.Accepted.Count}개 종목");

                    // 접수가 1건이라도 있으면 이번 달 적립 완료로 표시 → 남은 날 재실행 스킵.
                    // 접수 0건(전량 실패/장마감 등)이면 마커를 남기지 않아 다음 날 자동 재시도.
                    // 기준을 체결이 아니라 접수로 두는 이유: 접수된 주문을 재시도하면 중복 매수가 된다.
                    if (result.Accepted.Count > 0)
                    {
                        // 마커 저장이 조용히 실패하면 다음 날 크론이 같은 달에 또 매수한다(실자금 중복 집행).
                        // 저장 실패는 반드시 보고서에 실어 사람이 알아채게 한다.
                        if (AppConfigManager.Set(LastRunMonthKey, thisMonth))
                        {
                            Logger.Info($"[DcaCycle] 이번 달({thisMonth}) 적립 완료 표시 저장");
                        }
                        else
                        {
                            statusNote = $"⚠️ 이번 달({thisMonth}) 적립 완료 표시를 저장하지 못했습니다. "
                                + "이 상태로 두면 다음 크론 실행이 같은 달에 다시 매수합니다 — "
                                + $"TB_APP_CONFIG의 {LastRunMonthKey}를 '{thisMonth}'로 직접 넣거나 크론을 멈추세요.";
                            Logger.Error($"[DcaCycle] {LastRunMonthKey} 저장 실패 — 중복 매수 위험");
                        }
                    }
                    else
                    {
                        statusNote = "접수된 주문이 없어 이번 달 적립 완료로 표시하지 않았습니다 (다음 호출 시 재시도).";
                        Logger.Warn("[DcaCycle] 접수 0건 — 적립 완료 미표시, 다음 날 재시도 예정");
                    }
                }
            }
            catch (Exception ex)
            {
                statusNote = $"적립식 사이클 처리 중 오류가 발생했습니다: {ex.Message}";
                Logger.Error($"[DcaCycle] 사이클 처리 중 오류: {ex.Message}");
            }
            finally
            {
                await SendDcaReportAsync(result, statusNote);
            }

            Logger.Info("[DcaCycle] ✔ 적립식 매수 사이클이 종료되었습니다.");
            return string.IsNullOrEmpty(statusNote) ? $"적립식 주문 접수: {result.Accepted.Count}개 종목" : statusNote;
        }

        /// <summary>
        /// 적립식 주문 결과(접수·실패·예산경고)를 <b>한 통</b>의 종합 이메일로 발송합니다.
        /// 조기 종료/오류 시에도 항상 발송하며, 종목별 개별 메일은 보내지 않습니다.
        /// </summary>
        /// <param name="result">사이클 실행 결과 (접수·실패·예산경고)</param>
        /// <param name="statusNote">조기 종료·오류 등 안내 문구 (없으면 빈 문자열)</param>
        private async Task SendDcaReportAsync(DcaCycleResult result, string statusNote = "")
        {
            try
            {
                var body = new StringBuilder();

                // ── 헤더: 계좌 모드·시각·환율·집계 ──
                // 메일만 보고 "이게 실계좌인가"를 판별할 수 있어야 한다(실전 전환 후 필수).
                // 계좌번호는 개인정보이므로 모드만 쓰고 마스킹 계좌번호는 버린다.
                var (accountMode, _) = _session.GetAccountInfo();
                string modeLabel = accountMode switch
                {
                    "LIVE" => "🔴 실전(LIVE)",
                    "PAPER" => "🟡 모의(PAPER)",
                    _ => "⚪ 시뮬레이션(SIM)"
                };

                body.Append("<p style='color:#555555; font-size:13px; margin:0 0 4px 0;'>"
                    + $"<strong>{modeLabel}</strong> &nbsp;&middot;&nbsp; {DateTime.UtcNow.AddHours(9):yyyy-MM-dd HH:mm} KST");
                if (result.ExchangeRate > 0)
                    body.Append($" &nbsp;&middot;&nbsp; 환율 {result.ExchangeRate:N0}원");
                body.Append("</p>");

                // 계획에 도달한 사이클만 집계를 표시한다(로그인 실패·설정 없음·예외는 0이므로 생략).
                if (result.TotalCostKrw > 0)
                {
                    int acceptedQty = result.Accepted.Sum(f => f.Qty);
                    int failedQty = result.Failures.Sum(f => f.Qty);
                    body.Append("<p style='color:#555555; font-size:13px; margin:0 0 12px 0;'>"
                        + $"계획 {acceptedQty + failedQty}주 / {result.TotalCostKrw:N0}원 "
                        + $"&nbsp;&middot;&nbsp; 접수 {acceptedQty}주 &nbsp;&middot;&nbsp; 실패 {failedQty}주</p>");
                }

                // ── 안내(조기 종료/오류 사유) ──
                if (!string.IsNullOrEmpty(statusNote))
                    body.Append($"<p style='color:#b8860b;'><strong>ℹ️ 안내:</strong> {statusNote}</p>");

                // ── 예산 초과 경고 ──
                if (!string.IsNullOrEmpty(result.BudgetWarning))
                    body.Append($"<p style='color:#b8860b;'><strong>⚠️ 예산 초과:</strong> {result.BudgetWarning}</p>");

                // ── 주문 접수 내역 (종목별 수량 합산, 종목당 카드 1장) ──
                if (result.Accepted.Count == 0)
                {
                    body.Append("<p>✅ <strong>주문 접수:</strong> 없음 (예산 부족·설정 없음·전량 실패 등)</p>");
                }
                else
                {
                    // 이메일 클라이언트 호환을 위해 카드는 인라인 스타일 div로 구성한다(head 스타일·flex 미지원 대비).
                    // 단가는 체결가가 아니라 주문 지정가다(접수 시점 기록) — 라벨을 "주문가"로 둔다.
                    var cards = result.Accepted
                        .GroupBy(f => f.Ticker)
                        .Select(g => BuildCard("접수", "#1e8e3e", "#f4faf6", "#d7e3ef", g.Key,
                            $"주문가 : ${g.First().Price:N2}",
                            $"수량 : {g.Sum(x => x.Qty)}주",
                            $"소계 : {g.Sum(x => x.Qty * x.Price) * result.ExchangeRate:N0}원",
                            $"주문번호 : {string.Join(", ", g.Select(x => string.IsNullOrEmpty(x.OrderNo) ? "미수신" : x.OrderNo))}"));
                    body.Append("<p>✅ <strong>오늘의 적립식 주문 접수 내역:</strong></p>" + string.Join("", cards)
                        + "<p style='color:#8a8a8a; font-size:12px; margin:4px 0 12px 0;'>"
                        + "지정가 주문이므로 접수 ≠ 체결입니다. 실제 체결 여부는 증권사 앱의 체결내역에서 확인하세요.</p>");
                }

                // ── 매수 실패 내역 (종목별 개별 메일 대신 여기에 카드로 종합) ──
                if (result.Failures.Count > 0)
                {
                    var failCards = result.Failures
                        .Select(f => BuildCard("실패", "#c0392b", "#fdf5f4", "#f0d0cc", f.Ticker,
                            $"수량 : {f.Qty}주", $"사유 : {f.Error}"));
                    body.Append("<p style='color:#c0392b;'>❌ <strong>매수 실패 내역:</strong></p>" + string.Join("", failCards));
                }

                await NotificationService.SendEmailAsync("적립식 매수 보고서", body.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error($"[DcaCycle] 적립식 보고서 발송 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 보고서용 종목 카드 HTML 1장을 생성합니다.
        /// 이메일 클라이언트가 &lt;head&gt; 스타일·flex를 무시할 수 있으므로 인라인 스타일 div로 구성합니다.
        /// </summary>
        /// <param name="label">좌상단 배지 문구 (예: "매수", "실패")</param>
        /// <param name="accent">강조색 (배지 배경·좌측 테두리)</param>
        /// <param name="bg">카드 배경색</param>
        /// <param name="border">카드 테두리색</param>
        /// <param name="ticker">종목 코드</param>
        /// <param name="details">상세 문구들 (예: "주문가 : $185.30", "수량 : 2주"). 가운뎃점으로 이어 붙입니다.</param>
        private static string BuildCard(string label, string accent, string bg, string border,
            string ticker, params string[] details)
        {
            return
                $"<div style='border:1px solid {border}; border-left:4px solid {accent}; border-radius:8px; padding:12px 16px; margin:8px 0; background:{bg};'>"
                + $"<span style='display:inline-block; background:{accent}; color:#ffffff; font-size:12px; font-weight:700; border-radius:4px; padding:2px 8px; margin-right:8px;'>{label}</span>"
                + $"<strong style='font-size:15px;'>{ticker}</strong>"
                + $"<div style='margin-top:6px; color:#333333; font-size:14px;'>{string.Join(" &nbsp;&middot;&nbsp; ", details)}</div>"
                + "</div>";
        }
    }
}
