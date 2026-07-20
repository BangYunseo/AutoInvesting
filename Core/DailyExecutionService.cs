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
        /// 호출은 모두 스킵되며, 실패(체결 0건)한 날은 마커가 남지 않아 다음 날 자동 재시도됩니다.
        /// </summary>
        public async Task<string> RunDcaCycleAsync()
        {
            Logger.Info("[DcaCycle] ▶ 적립식 자동 매수 사이클이 시작되었습니다.");
            var result = new DcaCycleResult();
            string statusNote = "";

            // ── 월 1회 멱등 가드: 이번 달(KST) 이미 적립했으면 스킵 ──
            string thisMonth = CurrentKstMonth();
            string lastRunMonth = AppConfigManager.Get(LastRunMonthKey, "");
            if (lastRunMonth == thisMonth)
            {
                statusNote = $"이번 달({thisMonth}) 적립이 이미 완료되어 매수를 건너뜁니다.";
                Logger.Info($"[DcaCycle] 이번 달({thisMonth}) 적립 완료 상태 — 매수 스킵");
                return statusNote;
            }

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
                    Logger.Info($"[DcaCycle] ✔ 적립식 매수 완료 — {result.Filled.Count}개 종목 체결");

                    // 체결이 1건이라도 있으면 이번 달 적립 완료로 표시 → 남은 날 재실행 스킵.
                    // 체결 0건(전량 실패/장마감 등)이면 마커를 남기지 않아 다음 날 자동 재시도.
                    if (result.Filled.Count > 0)
                    {
                        AppConfigManager.Set(LastRunMonthKey, thisMonth);
                        Logger.Info($"[DcaCycle] 이번 달({thisMonth}) 적립 완료 표시 저장");
                    }
                    else
                    {
                        statusNote = "체결된 종목이 없어 이번 달 적립 완료로 표시하지 않았습니다 (다음 호출 시 재시도).";
                        Logger.Warn("[DcaCycle] 체결 0건 — 적립 완료 미표시, 다음 날 재시도 예정");
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
            return string.IsNullOrEmpty(statusNote) ? $"적립식 매수 완료: {result.Filled.Count}개 종목 체결" : statusNote;
        }

        /// <summary>
        /// 적립식 매수 결과(성공·실패·예산경고)를 <b>한 통</b>의 종합 이메일로 발송합니다.
        /// 조기 종료/오류 시에도 항상 발송하며, 종목별 개별 메일은 보내지 않습니다.
        /// </summary>
        /// <param name="result">사이클 실행 결과 (체결·실패·예산경고)</param>
        /// <param name="statusNote">조기 종료·오류 등 안내 문구 (없으면 빈 문자열)</param>
        private async Task SendDcaReportAsync(DcaCycleResult result, string statusNote = "")
        {
            try
            {
                var body = new StringBuilder();

                // ── 안내(조기 종료/오류 사유) ──
                if (!string.IsNullOrEmpty(statusNote))
                    body.Append($"<p style='color:#b8860b;'><strong>ℹ️ 안내:</strong> {statusNote}</p>");

                // ── 예산 초과 경고 ──
                if (!string.IsNullOrEmpty(result.BudgetWarning))
                    body.Append($"<p style='color:#b8860b;'><strong>⚠️ 예산 초과:</strong> {result.BudgetWarning}</p>");

                // ── 매수 성공 내역 (종목별 수량 합산) ──
                if (result.Filled.Count == 0)
                {
                    body.Append("<p>✅ <strong>매수 성공:</strong> 없음 (예산 부족·설정 없음·전량 실패 등)</p>");
                }
                else
                {
                    var lines = result.Filled
                        .GroupBy(f => f.Ticker)
                        .Select(g => $"<li><strong>{g.Key}</strong> {g.Sum(x => x.Qty)}주 매수 (단가 ${g.First().Price:N2})</li>");
                    body.Append("<p>✅ <strong>오늘의 적립식 매수 내역:</strong></p><ul>" + string.Join("", lines) + "</ul>");
                }

                // ── 매수 실패 내역 (종목별 개별 메일 대신 여기에 종합) ──
                if (result.Failures.Count > 0)
                {
                    var failLines = result.Failures
                        .Select(f => $"<li><strong>{f.Ticker}</strong> {f.Qty}주 실패 — {f.Error}</li>");
                    body.Append("<p style='color:#c0392b;'>❌ <strong>매수 실패 내역:</strong></p>"
                        + "<ul style='color:#c0392b;'>" + string.Join("", failLines) + "</ul>");
                }

                await NotificationService.SendEmailAsync("적립식 매수 보고서", body.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error($"[DcaCycle] 적립식 보고서 발송 중 오류: {ex.Message}");
            }
        }
    }
}
