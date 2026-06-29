using AutoInvest.Data.DTO;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// 적립식(DCA) 자동 매수 사이클을 실행합니다 (판단 없는 단순 자동화).
        /// 퀀트/AI 판단을 하지 않고, 설정한 종목별 고정 수량(Dca:Quantities)을 그대로 매수합니다.
        /// </summary>
        public async Task<string> RunDcaCycleAsync()
        {
            Logger.Info("[DcaCycle] ▶ 적립식 자동 매수 사이클이 시작되었습니다.");
            var filled = new List<TradeHistoryDto>();
            string statusNote = "";

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
                    filled = await engine.AccumulateAsync(quantities, budget);
                    Logger.Info($"[DcaCycle] ✔ 적립식 매수 완료 — {filled.Count}주 체결");
                }
            }
            catch (Exception ex)
            {
                statusNote = $"적립식 사이클 처리 중 오류가 발생했습니다: {ex.Message}";
                Logger.Error($"[DcaCycle] 사이클 처리 중 오류: {ex.Message}");
            }
            finally
            {
                await SendDcaReportAsync(filled, statusNote);
            }

            Logger.Info("[DcaCycle] ✔ 적립식 매수 사이클이 종료되었습니다.");
            return string.IsNullOrEmpty(statusNote) ? $"적립식 매수 완료: {filled.Count}주 체결" : statusNote;
        }

        /// <summary>
        /// 적립식 매수 결과를 이메일로 발송합니다 (조기 종료/오류 시에도 항상 발송).
        /// </summary>
        private async Task SendDcaReportAsync(List<TradeHistoryDto> filled, string statusNote = "")
        {
            try
            {
                string noticeHtml = string.IsNullOrEmpty(statusNote)
                    ? ""
                    : $"<p style='color:#b8860b;'><strong>ℹ️ 안내:</strong> {statusNote}</p>";

                string body;
                if (filled.Count == 0)
                {
                    body = noticeHtml + "<p>오늘은 매수한 종목이 없습니다. (예산 부족 또는 설정 없음)</p>";
                }
                else
                {
                    var lines = filled
                        .GroupBy(f => f.Ticker)
                        .Select(g => $"<li><strong>{g.Key}</strong> {g.Count()}주 매수 (단가 ${g.First().Price:N2})</li>");
                    body = noticeHtml + "<p>오늘의 적립식 매수 내역입니다:</p><ul>" + string.Join("", lines) + "</ul>";
                }

                await NotificationService.SendEmailAsync("적립식 매수 보고서", body);
            }
            catch (Exception ex)
            {
                Logger.Error($"[DcaCycle] 적립식 보고서 발송 중 오류: {ex.Message}");
            }
        }
    }
}
