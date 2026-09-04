using AutoInvest.Data.DTO;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// 매수 주기마다 적립
    /// </summary>
    public class DailyExecutionService
    {

        private readonly SessionManager _session;

        public DailyExecutionService(SessionManager session)
        {
            _session = session;
        }

        public const string LastRunMonthKey = "DCA_LAST_RUN_MONTH";         // 마지막 적립 월
        public const string ForceRunMonthKey = "DCA_FORCE_RUN_MONTH";       // 예약된 추가 적립 월
        public const string LastRunDateKey = "DCA_LAST_RUN_DATE";           // 마지막 적립 일자
        public const string PendingSnapshotKey = "DCA_PENDING_SNAPSHOT";    // 체결 스냅샷(주문 직전 보유 수량 + 접수한 주문 수량)

        // 한국 표준시 기준 월
        public static string CurrentKstMonth() => DateTime.UtcNow.AddHours(9).ToString("yyyy-MM");

        /// <summary>
        /// 적립 지정일 판단
        /// </summary>
        /// <param name="kstNow"></param>
        /// <param name="runDay"></param>
        /// <returns></returns>
        public static bool IsOnOrAfterRunDay(DateTime kstNow, int runDay)
        {
            if (runDay <= 0) return true;
            int effectiveDay = Math.Min(runDay, DateTime.DaysInMonth(kstNow.Year, kstNow.Month));
            return kstNow.Day >= effectiveDay;
        }

        /// <summary>
        /// 적립식 매수 사이클 실행
        /// </summary>
        /// <param name="force">
        /// </param>
        public async Task<string> RunDcaCycleAsync(bool force = false)
        {
            Logger.Info("[DCA] 자동 매수를 시작합니다.");

            var result = new DcaCycleResult();
            string statusNote = "";
            string thisMonth = CurrentKstMonth();
            
            if (!AppConfigManager.TryReadDb(LastRunMonthKey, out string? lastRunRaw)
                || !AppConfigManager.TryReadDb(ForceRunMonthKey, out string? forceRunRaw))
            {
                statusNote = "DB 조회 실패로 매수를 시도하지 않았습니다.";
                Logger.Error($"[DCA] {LastRunMonthKey}/{ForceRunMonthKey} 중복 매수 방지를 위해 매수 중단");
                await SendDcaReportAsync(result, statusNote);
                return statusNote;
            }
            string lastRunMonth = lastRunRaw ?? "";
            bool reserved = (forceRunRaw ?? "") == thisMonth;

            // 적립일, 시간 기록
            int runDay = DcaSettings.LoadRunDay();
            var kstNow = DateTime.UtcNow.AddHours(9);
            if (!force && !reserved && !IsOnOrAfterRunDay(kstNow, runDay))
            {
                statusNote = $"적립 지정일({runDay}일)이 되지 않아 매수를 건너뜁니다. (KST {kstNow:MM-dd})";
                Logger.Info($"[DCA] 적립 지정일({runDay}일) 미도래 ({kstNow:yyyy-MM-dd} KST) : 매수 스킵");
                return statusNote;
            }

            if (lastRunMonth == thisMonth && !force && !reserved)
            {
                statusNote = $"이번 달({thisMonth}) 적립이 이미 완료되어 매수를 건너뜁니다.";
                Logger.Info($"[DCA] 이번 달({thisMonth}) 적립 이미 완료 : 매수 스킵");
                return statusNote;
            }
            if (lastRunMonth == thisMonth)
            {
                Logger.Warn($"[DCA] {(reserved ? "예약된" : "강제")} 실행 : 이번 달({thisMonth}) 적립 완료 상태에서 추가 매수를 진행합니다.");
            }

            try
            {
                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    // 로그인 시도
                    var loginOk = await client.LoginAsync();
                    if (!loginOk)
                    {
                        statusNote = "로그인에 실패했습니다. 매수를 스킵합니다.";
                        Logger.Error("[DCA] 로그인 실패 : 매수 스킵");
                        return statusNote; 
                    }
                }

                // 종목별 매수 수량·예산 로드
                var (quantities, budget) = DcaSettings.Load();
                if (quantities.Count == 0)
                {
                    statusNote = "적립할 수량이 존재하지 않습니다. 매수를 스킵합니다.";
                    Logger.Warn("[DCA] 매수 수량 없음 : 매수 스킵");
                }
                else
                {
                    Dictionary<string, int> beforeQty;
                    try
                    {
                        beforeQty = (await client.GetHoldingsAsync())
                            .GroupBy(h => h.Ticker, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key.ToUpper(), g => g.Sum(h => h.Qty), StringComparer.OrdinalIgnoreCase);
                    }
                    catch (Exception ex)
                    {
                        beforeQty = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        Logger.Warn($"[DCA] 주문 전 보유 수량 조회 실패 — 이번 건은 체결 대사 불가: {ex.Message}");
                    }

                    var engine = new DcaAccumulationEngine(client);
                    result = await engine.AccumulateAsync(quantities, budget);
                    Logger.Info($"[DCA] 주문 접수 완료 — {result.Accepted.Count}개 종목");

                    SavePendingSnapshot(thisMonth, beforeQty, result);

                    // 접수가 1건이라도 있으면 이번 달 적립 완료로 표시 → 남은 날 재실행 스킵.
                    // 접수 0건(전량 실패/장마감 등)이면 마커를 남기지 않아 다음 날 자동 재시도.
                    // 기준을 체결이 아니라 접수로 두는 이유: 접수된 주문을 재시도하면 중복 매수가 된다.
                    if (result.Accepted.Count > 0)
                    {
                        // 예약분은 접수가 있을 때만 소진한다. 접수 0건(휴장·예수금 부족)이면 예약을 남겨
                        // 다음 크론이 다시 시도한다 — 월 마커와 같은 재시도 정책이다.
                        if (reserved && !AppConfigManager.Set(ForceRunMonthKey, ""))
                        {
                            statusNote = "⚠️ 추가 적립 예약을 해제하지 못했습니다. 다음 크론 실행이 또 매수할 수 있으니 "
                                + $"TB_APP_CONFIG의 {ForceRunMonthKey}를 비우세요.";
                            Logger.Error($"[DcaCycle] {ForceRunMonthKey} 해제 실패 — 중복 매수 위험");
                        }

                        // 마커 저장이 조용히 실패하면 다음 날 크론이 같은 달에 또 매수한다(실자금 중복 집행).
                        // 저장 실패는 반드시 보고서에 실어 사람이 알아채게 한다.
                        if (AppConfigManager.Set(LastRunMonthKey, thisMonth))
                        {
                            // 집행 일자는 표시 전용이므로 실패해도 경고만 남긴다(가드는 위 월 마커가 담당).
                            string today = kstNow.ToString("yyyy-MM-dd");
                            if (!AppConfigManager.Set(LastRunDateKey, today))
                                Logger.Warn($"[DcaCycle] {LastRunDateKey} 저장 실패 — 화면에 집행 일자가 비어 보일 수 있습니다(가드는 정상).");

                            Logger.Info($"[DcaCycle] 이번 달({thisMonth}) 적립 완료 표시 저장 — 집행일 {today} KST");
                        }
                        else
                        {
                            // 예약 해제 실패 안내가 이미 있을 수 있으므로 덮어쓰지 않고 잇는다.
                            statusNote += $"{(statusNote.Length > 0 ? " " : "")}⚠️ 이번 달({thisMonth}) 적립 완료 표시를 저장하지 못했습니다. "
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

        /// <summary>대사용 스냅샷에 담는 주문 1건.</summary>
        private sealed class PendingOrder
        {
            public string Ticker { get; set; } = string.Empty;
            public int Qty { get; set; }
            public string OrderNo { get; set; } = string.Empty;
        }

        /// <summary>대사용 스냅샷 본체 (TB_APP_CONFIG에 JSON으로 보관).</summary>
        private sealed class PendingSnapshot
        {
            public string Month { get; set; } = string.Empty;
            public Dictionary<string, int> Before { get; set; } = new Dictionary<string, int>();
            public List<PendingOrder> Ordered { get; set; } = new List<PendingOrder>();
        }

        /// <summary>
        /// 장 마감 후 대사를 위해 "주문 전 보유 수량 + 이번에 접수한 주문"을 저장합니다.
        /// 접수가 없으면 대사할 것이 없으므로 남기지 않습니다.
        /// </summary>
        /// <param name="month">이번 적립의 대상 월 (KST, "yyyy-MM")</param>
        /// <param name="beforeQty">주문 직전 종목별 보유 수량</param>
        /// <param name="result">이번 사이클 결과</param>
        private static void SavePendingSnapshot(string month, Dictionary<string, int> beforeQty, DcaCycleResult result)
        {
            if (result.Accepted.Count == 0) return;

            var snapshot = new PendingSnapshot
            {
                Month = month,
                Before = beforeQty,
                Ordered = result.Accepted
                    .GroupBy(f => f.Ticker.ToUpper())
                    .Select(g => new PendingOrder
                    {
                        Ticker = g.Key,
                        Qty = g.Sum(x => x.Qty),
                        OrderNo = g.Select(x => x.OrderNo).FirstOrDefault(o => !string.IsNullOrEmpty(o)) ?? string.Empty
                    })
                    .ToList()
            };

            if (!AppConfigManager.Set(PendingSnapshotKey, JsonSerializer.Serialize(snapshot)))
                Logger.Error($"[DcaCycle] {PendingSnapshotKey} 저장 실패 — 이번 건은 체결 대사 불가");
        }

        /// <summary>
        /// 장 마감 후 실제 체결 여부를 확인하고, 전량 미체결이면 그 달을 다시 열어 재시도하게 합니다.
        ///
        /// 접수를 완료로 세면 지정가가 안 붙어 소멸해도 그 달이 닫혀 적립이 조용히 누락된다.
        /// 여기서 주문 전 보유 수량과 지금 보유 수량을 비교해 실제로 늘었는지 본다.
        ///
        /// ⚠️ 마커를 되돌리는 것은 다음 사이클의 실매수를 다시 허용한다는 뜻이다. 따라서
        /// <b>전량 미체결이 확실할 때만</b> 되돌린다 — 한 종목이라도 수량이 늘었으면 그 달은 집행된
        /// 것으로 두고, 수량이 줄어든 종목이 있으면(사람이 매도했을 가능성) 판정을 포기하고
        /// 사람에게 넘긴다. 애매하면 아무것도 하지 않는 쪽이 중복 매수보다 낫다.
        /// </summary>
        /// <returns>사람이 읽을 결과 요약</returns>
        public async Task<string> ReconcileAsync()
        {
            Logger.Info("[Reconcile] ▶ 체결 대사를 시작합니다.");

            string raw = AppConfigManager.Get(PendingSnapshotKey, "");
            if (string.IsNullOrWhiteSpace(raw))
            {
                Logger.Info("[Reconcile] 대사할 주문이 없습니다.");
                return "대사할 주문이 없습니다.";
            }

            PendingSnapshot? snap;
            try
            {
                snap = JsonSerializer.Deserialize<PendingSnapshot>(raw);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Reconcile] 스냅샷 파싱 실패 — 폐기합니다: {ex.Message}");
                AppConfigManager.Set(PendingSnapshotKey, "");
                return "스냅샷을 읽지 못해 폐기했습니다.";
            }

            if (snap == null || snap.Ordered.Count == 0)
            {
                AppConfigManager.Set(PendingSnapshotKey, "");
                return "대사할 주문이 없습니다.";
            }

            string thisMonth = CurrentKstMonth();
            if (snap.Month != thisMonth)
            {
                // 달이 바뀌었으면 되돌릴 대상이 없다(마커는 이미 다음 달 기준으로 판정된다).
                Logger.Warn($"[Reconcile] 스냅샷이 지난 달({snap.Month})분이라 폐기합니다.");
                AppConfigManager.Set(PendingSnapshotKey, "");
                return $"지난 달({snap.Month}) 스냅샷을 폐기했습니다.";
            }

            string note;
            try
            {
                var client = _session.GetClient();
                if (!client.IsLoggedIn && !await client.LoginAsync())
                {
                    Logger.Error("[Reconcile] 로그인 실패 — 대사를 건너뜁니다(스냅샷 보존).");
                    return "브로커 로그인 실패로 대사를 건너뛰었습니다. 스냅샷은 남겨 다음 실행에서 다시 시도합니다.";
                }

                var nowQty = (await client.GetHoldingsAsync())
                    .GroupBy(h => h.Ticker, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key.ToUpper(), g => g.Sum(h => h.Qty), StringComparer.OrdinalIgnoreCase);

                int totalOrdered = 0, totalFilled = 0;
                bool anySold = false;
                var lines = new List<string>();

                foreach (var o in snap.Ordered)
                {
                    snap.Before.TryGetValue(o.Ticker, out int before);
                    nowQty.TryGetValue(o.Ticker, out int now);

                    int delta = now - before;
                    if (delta < 0) anySold = true;

                    int filled = Math.Max(0, Math.Min(delta, o.Qty));
                    totalOrdered += o.Qty;
                    totalFilled += filled;

                    string status = filled >= o.Qty ? "FILLED" : filled > 0 ? "PARTIAL" : "FAILED";
                    TradeHistoryDAO.UpdateStatusByOrderNo(o.OrderNo, status);

                    lines.Add($"{o.Ticker}: 주문 {o.Qty}주 · 체결 {filled}주 (보유 {before}→{now})");
                    Logger.Info($"[Reconcile] {o.Ticker} 주문 {o.Qty}주 → 체결 {filled}주 (보유 {before}→{now}), 상태 {status}");
                }

                if (anySold)
                {
                    note = "보유 수량이 줄어든 종목이 있어 체결 판정을 신뢰할 수 없습니다(대사 전 매도 가능성). "
                        + "이번 달 적립 완료 표시는 그대로 두었습니다 — 증권사 앱에서 직접 확인하세요.";
                    Logger.Warn("[Reconcile] 수량 감소 감지 — 마커를 건드리지 않습니다.");
                }
                else if (totalFilled == 0)
                {
                    // 전량 미체결이 확실하다 → 그 달을 다시 열어 다음 크론이 재시도하게 한다.
                    if (AppConfigManager.Set(LastRunMonthKey, ""))
                    {
                        // 완료를 되돌렸으면 표시용 집행 일자도 함께 지운다(남겨두면 화면이 집행됐다고 말한다).
                        AppConfigManager.Set(LastRunDateKey, "");
                        note = $"전량 미체결로 확인되어 {thisMonth} 적립 완료 표시를 해제했습니다. 다음 사이클에서 다시 시도합니다.";
                        Logger.Warn($"[Reconcile] 전량 미체결 — {LastRunMonthKey} 해제, 재시도 허용");
                    }
                    else
                    {
                        note = $"전량 미체결이지만 완료 표시를 해제하지 못했습니다. TB_APP_CONFIG의 {LastRunMonthKey}를 비워야 재시도됩니다.";
                        Logger.Error($"[Reconcile] {LastRunMonthKey} 해제 실패");
                    }
                }
                else if (totalFilled < totalOrdered)
                {
                    note = $"일부만 체결됐습니다(주문 {totalOrdered}주 중 {totalFilled}주). "
                        + "이번 달은 집행된 것으로 두어 중복 매수를 피합니다 — 부족분은 필요 시 수동으로 매수하세요.";
                    Logger.Warn($"[Reconcile] 부분 체결 {totalFilled}/{totalOrdered}주 — 마커 유지");
                }
                else
                {
                    note = $"주문 {totalOrdered}주가 전량 체결됐습니다.";
                    Logger.Info($"[Reconcile] 전량 체결 {totalFilled}주");
                }

                AppConfigManager.Set(PendingSnapshotKey, "");
                await SendReconcileReportAsync(snap.Month, lines, note);
                return note;
            }
            catch (Exception ex)
            {
                // 스냅샷은 남긴다 — 다음 실행에서 다시 시도할 수 있어야 한다.
                Logger.Error($"[Reconcile] 대사 중 오류: {ex.Message}");
                return $"대사 중 오류가 발생했습니다: {ex.Message}";
            }
        }

        /// <summary>체결 대사 결과를 메일 1통으로 발송합니다.</summary>
        /// <param name="month">대상 월</param>
        /// <param name="lines">종목별 대사 결과 문구</param>
        /// <param name="note">종합 안내</param>
        private static async Task SendReconcileReportAsync(string month, List<string> lines, string note)
        {
            try
            {
                var body = new StringBuilder();
                body.Append($"<p style='color:#555555; font-size:13px;'>{month} 적립 · "
                    + $"{DateTime.UtcNow.AddHours(9):yyyy-MM-dd HH:mm} KST 대사</p>");
                body.Append($"<p><strong>{note}</strong></p>");

                if (lines.Count > 0)
                {
                    body.Append("<ul>");
                    foreach (var l in lines) body.Append($"<li>{l}</li>");
                    body.Append("</ul>");
                }

                body.Append("<p style='color:#8a8a8a; font-size:12px;'>"
                    + "체결 여부는 주문 전후 보유 수량 차이로 판정합니다. 대사 전에 같은 종목을 직접 매매하면 "
                    + "판정이 어긋날 수 있습니다.</p>");

                await NotificationService.SendEmailAsync("적립 체결 대사 결과", body.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error($"[Reconcile] 대사 보고서 발송 중 오류: {ex.Message}");
            }
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
                    body.Append("<p><strong>주문 접수 내역:</strong></p>" + string.Join("", cards)
                        + "<p style='color:#8a8a8a; font-size:12px; margin:4px 0 12px 0;'>"
                        + "지정가 주문이므로 접수 ≠ 체결입니다. 실제 체결 여부는 증권사 앱의 체결내역에서 확인하세요.</p>");
                }

                // ── 매수 실패 내역 (종목별 개별 메일 대신 여기에 카드로 종합) ──
                if (result.Failures.Count > 0)
                {
                    var failCards = result.Failures
                        .Select(f => BuildCard("실패", "#ff0000", "#ffffff", "#ffdddd", f.Ticker,
                            $"수량 : {f.Qty}주", $"사유 : {f.Error}"));
                    body.Append("<p style='color:#ff0000;'><strong>매수 실패 내역:</strong></p>" + string.Join("", failCards));
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
