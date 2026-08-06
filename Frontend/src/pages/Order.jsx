import { useState, useEffect, useCallback } from "react";
import { countRunsInMonth } from "../utils/dcaRuns";
import ConfirmDialog from "../components/ConfirmDialog";

/** 숫자를 원화 천단위 구분 문자열로 변환 (표시용). */
const won = (n) => Number(n ?? 0).toLocaleString("ko-KR");

/** 확인 모달 내 항목 한 줄(라벨 · 값). */
const InfoRow = ({ label, value, strong, color }) => (
  <div
    style={{
      display: "flex",
      justifyContent: "space-between",
      gap: 12,
      padding: "4px 0",
      fontSize: "0.85rem",
    }}
  >
    <span style={{ color: "var(--text-secondary)" }}>{label}</span>
    <span
      style={{
        color: color || "var(--text-primary)",
        fontWeight: strong ? 700 : 500,
      }}
    >
      {value}
    </span>
  </div>
);

/** 주문 확인 모달의 종류별 헤더(아이콘 · 제목 · 색). */
const CONFIRM_HEADERS = {
  buy: { icon: "📈", title: "매수 확인", color: "var(--text-primary)" },
  "sell-taxable": {
    icon: "⚠️",
    title: "양도소득세가 예상됩니다",
    color: "var(--warn-amber)",
  },
  "sell-free": { icon: "✅", title: "매도 확인", color: "var(--profit-green)" },
  "sell-unknown": {
    icon: "❓",
    title: "매도 확인 · 취득가 불명",
    color: "var(--text-primary)",
  },
  "sell-plain": {
    icon: "📉",
    title: "매도 확인",
    color: "var(--text-primary)",
  },
};

/**
 * 주문 확인 모달 — 브라우저 기본 confirm() 대신 앱 내부 화면으로 표시한다.
 * 과세 매도(sell-taxable)는 예상 세금 내역을 표로 보여주고 위험(빨강) 버튼으로 구분한다.
 */
const OrderConfirmModal = ({ ctx, ordering, onCancel, onConfirm }) => {
  const e = ctx.est;
  const h = CONFIRM_HEADERS[ctx.kind] || CONFIRM_HEADERS["sell-plain"];
  const taxable = ctx.kind === "sell-taxable";

  return (
    <div
      className="modal-overlay"
      onClick={() => {
        if (!ordering) onCancel();
      }}
    >
      <div
        className="modal-content"
        onClick={(ev) => ev.stopPropagation()}
        style={{ maxWidth: 460 }}
      >
        <h3
          style={{
            marginBottom: 14,
            borderBottom: "1px solid var(--border-primary)",
            paddingBottom: 12,
            color: h.color,
          }}
        >
          {h.icon} {h.title}
        </h3>

        <p
          style={{
            fontSize: "0.9rem",
            marginBottom: 14,
            color: "var(--text-secondary)",
          }}
        >
          <strong style={{ color: "var(--text-primary)" }}>{ctx.ticker}</strong>{" "}
          {ctx.qty}주를 {ctx.orderType === "BUY" ? "매수" : "매도"}합니다.
        </p>

        {taxable && e && (
          <>
            <div
              style={{
                background: "rgba(245,158,11,0.08)",
                border: "1px solid rgba(245,158,11,0.25)",
                borderRadius: "var(--radius-sm)",
                padding: "10px 14px",
                marginBottom: 12,
              }}
            >
              <InfoRow label="예상 양도차익" value={`${won(e.gainKrw)}원`} />
              <InfoRow
                label="남은 기본공제"
                value={`${won(e.remainingDeductionKrw)}원`}
              />
              <InfoRow
                label="과세표준 (× 22%)"
                value={`${won(e.taxableBaseKrw)}원`}
              />
              <InfoRow
                label="예상 매도수수료"
                value={`약 ${won(e.estimatedFeeKrw)}원`}
              />
              <div
                style={{
                  borderTop: "1px solid var(--border-primary)",
                  marginTop: 6,
                  paddingTop: 6,
                }}
              >
                <InfoRow
                  label="예상 양도소득세"
                  value={`약 ${won(e.estimatedTaxKrw)}원`}
                  strong
                  color="var(--warn-amber)"
                />
              </div>
            </div>
            {e.maxTaxFreeQty >= 0 && (
              <div
                className="alert alert--ok"
                style={{ padding: "8px 12px", fontSize: "0.82rem", lineHeight: 1.5 }}
              >
                💡 지금 조건에선 <strong>{e.maxTaxFreeQty}주</strong>까지 세금
                없이 매도할 수 있습니다.
              </div>
            )}
          </>
        )}

        {ctx.kind === "sell-free" && e && (
          <div className="alert alert--ok">
            ✅ 예상 세금이 없습니다(기본공제 이내).
            <br />
            예상 양도차익 {won(e.gainKrw)}원 · 남은 공제{" "}
            {won(e.remainingDeductionKrw)}원
            <div
              style={{
                marginTop: 6,
                fontSize: "0.78rem",
                color: "var(--text-muted)",
                wordBreak: "keep-all",
              }}
            >
              ※ 올해 다른 곳(증권사 앱 등)에서 실현한 차익은 집계되지 않습니다. 그만큼 공제가
              이미 줄어 있으면 실제로는 세금이 나올 수 있습니다.
            </div>
          </div>
        )}

        {ctx.kind === "sell-unknown" && (
          <div
            style={{
              fontSize: "0.85rem",
              color: "var(--text-secondary)",
              background: "rgba(255,255,255,0.03)",
              borderRadius: "var(--radius-sm)",
              padding: "10px 14px",
              lineHeight: 1.6,
              wordBreak: "keep-all",
            }}
          >
            취득가를 확인할 수 없어 예상 세금을 계산하지 못했습니다. 진행 시
            세금이 발생할 수 있습니다.
          </div>
        )}

        {ctx.kind === "sell-plain" && (
          <div
            style={{
              fontSize: "0.85rem",
              color: "var(--text-muted)",
              lineHeight: 1.6,
              wordBreak: "keep-all",
            }}
          >
            세금 프리뷰를 불러오지 못했습니다. 서버 가드가 과세 매도를 다시
            확인합니다.
          </div>
        )}

        <div style={{ display: "flex", gap: 10, marginTop: 18 }}>
          <button
            className="btn btn--outline"
            style={{ flex: 1 }}
            onClick={onCancel}
            disabled={ordering}
          >
            취소
          </button>
          <button
            className={`btn ${taxable ? "btn--danger" : "btn--primary"}`}
            style={{ flex: 1 }}
            onClick={onConfirm}
            disabled={ordering}
          >
            {ordering
              ? "처리 중..."
              : taxable
                ? "세금 감수하고 매도"
                : ctx.orderType === "BUY"
                  ? "매수 진행"
                  : "매도 진행"}
          </button>
        </div>
      </div>
    </div>
  );
};

/**
 * 주문/적립 페이지.
 * OrderController와 연동하여 적립식(DCA) 매수 사이클과 수동 주문을 실행합니다.
 * 수동 주문은 실제 보유 종목을 끌어와, 매도는 보유 종목·보유수량 범위에서만,
 * 매수는 보유 종목 선택 또는 신규 종목 직접입력(현재가 검증)으로만 실행합니다.
 */
const Order = () => {
  // ── 적립식(DCA) 실행 상태 ──
  const [dcaRunning, setDcaRunning] = useState(false);
  const [dcaResult, setDcaResult] = useState(null);
  const [dcaError, setDcaError] = useState(null);
  // 이번 달 적립 상태 { month, alreadyRan, reserved } — 확인 문구와 예약 토글에 함께 쓴다
  const [dcaSchedule, setDcaSchedule] = useState(null);
  const [scheduling, setScheduling] = useState(false);
  // 적립 실행·예약 확인 모달 키 ('run' | 'run-again' | 'reserve' | 'unreserve', null = 닫힘)
  const [dcaConfirm, setDcaConfirm] = useState(null);
  // 이번 달 집행 회차 수 (거래이력을 회차로 묶어 센 값)
  const [monthRunCount, setMonthRunCount] = useState(0);
  // 적립 지정일 (KST, 1~28). 0 = 미설정(월초부터 시도)
  const [runDay, setRunDay] = useState(0);
  const [maxRunDay, setMaxRunDay] = useState(28);
  const [savingRunDay, setSavingRunDay] = useState(false);

  // ── 보유 종목 (수동 주문 종목 선택 소스) ──
  const [holdings, setHoldings] = useState([]);
  const [holdingsLoading, setHoldingsLoading] = useState(true);
  const [holdingsError, setHoldingsError] = useState(null);

  // ── 수동 주문 상태 ──
  const [orderType, setOrderType] = useState("BUY");
  const [buyMode, setBuyMode] = useState("hold"); // 'hold' = 보유종목 선택 / 'new' = 신규 직접입력 (매수 전용)
  const [selectedTicker, setSelectedTicker] = useState(""); // 보유종목 드롭다운 선택값
  const [newTicker, setNewTicker] = useState(""); // 신규 직접입력 티커
  // 신규 티커 검증 상태: idle | checking | valid | invalid
  const [newTickerState, setNewTickerState] = useState({
    status: "idle",
    price: 0,
    error: null,
  });
  const [qty, setQty] = useState(1);
  const [price, setPrice] = useState("");
  const [ordering, setOrdering] = useState(false);
  const [orderResult, setOrderResult] = useState(null);
  const [orderError, setOrderError] = useState(null);
  // 주문 확인 모달 컨텍스트 (null = 닫힘). '진행' 클릭 시 executeOrder가 이 값으로 제출한다.
  const [confirmModal, setConfirmModal] = useState(null);

  // ── 보유 종목 로드 ──
  const fetchHoldings = useCallback(async () => {
    try {
      setHoldingsLoading(true);
      setHoldingsError(null);
      const res = await fetch("/api/portfolio/holdings");
      if (!res.ok) throw new Error(`보유 종목 조회 실패 (${res.status})`);
      const data = await res.json();
      const list = Array.isArray(data?.holdings) ? data.holdings : [];
      setHoldings(list);
      setSelectedTicker((prev) => prev || (list[0]?.ticker ?? ""));
    } catch (err) {
      setHoldingsError(err.message);
    } finally {
      setHoldingsLoading(false);
    }
  }, []);

  // ── 이번 달 적립 상태 조회 (실행 여부 + 예약 여부 + 적용될 템플릿) ──
  const fetchDcaSchedule = useCallback(async () => {
    try {
      const res = await fetch("/api/order/dca-schedule");
      if (!res.ok) return; // 보조 정보 — 실패해도 실행 버튼은 막지 않는다
      const data = await res.json();
      setDcaSchedule(data);

      // 이번 달 집행 횟수는 거래이력을 회차로 묶어 센다.
      // 적립 설정의 월별 로그와 같은 숫자가 나와야 하므로 같은 유틸을 쓴다.
      const tradeRes = await fetch("/api/history/trades?limit=500");
      if (!tradeRes.ok) return;
      const tradeData = await tradeRes.json();
      setMonthRunCount(countRunsInMonth(tradeData.trades, data.month));
    } catch {
      // 조회 실패 시 확인 문구만 일반형으로 떨어진다
    }
  }, []);

  // ── 적립 지정일 조회 (적립 설정과 같은 엔드포인트를 쓴다) ──
  const fetchRunDay = useCallback(async () => {
    try {
      const res = await fetch("/api/dca/config");
      if (!res.ok) return; // 보조 정보 — 실패해도 실행 버튼은 막지 않는다
      const data = await res.json();
      setRunDay(data.runDay ?? 0);
      if (data.maxRunDay > 0) setMaxRunDay(data.maxRunDay);
    } catch {
      // 조회 실패 시 '지정 안 함'으로 보인다(실제 저장값은 서버가 그대로 유지)
    }
  }, []);

  useEffect(() => {
    fetchHoldings();
    fetchDcaSchedule();
    fetchRunDay();
  }, [fetchHoldings, fetchDcaSchedule, fetchRunDay]);

  // 지정일 변경은 그 자체로 돈이 나가지 않으므로 고르는 즉시 저장한다.
  const saveRunDay = async (day) => {
    setDcaError(null);
    setDcaResult(null);
    try {
      setSavingRunDay(true);
      const res = await fetch("/api/dca/config", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ runDay: day }),
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.error || `지정일 저장 실패 (${res.status})`);
      setRunDay(day);
      setDcaResult({ message: data.message });
    } catch (err) {
      setDcaError(err.message);
      fetchRunDay(); // 실패 시 서버 값으로 되돌린다
    } finally {
      setSavingRunDay(false);
    }
  };

  // 이번 달 몇 월인지 (KST 기준 서버 응답 "yyyy-MM"에서 월만 뽑음)
  const scheduleMonthLabel = dcaSchedule
    ? `${Number(dcaSchedule.month.slice(5, 7))}월`
    : "이번 달";

  // 이번 달 일수 — 달력 API 없이 표준 Date로 구한다(다음 달 0일 = 이번 달 말일, 윤년 포함).
  // 서버가 KST로 준 month를 쓰므로 브라우저 시간대와 어긋나지 않는다.
  const daysInThisMonth = dcaSchedule
    ? new Date(
        Number(dcaSchedule.month.slice(0, 4)),
        Number(dcaSchedule.month.slice(5, 7)),
        0,
      ).getDate()
    : 31;

  // 실행/예약 확인 모달 컨텍스트 (null = 닫힘)
  // 이미 적립한 달이면 "한 번 더 사는 것"임을 분명히 묻고, 아직 안 한 달이면
  // 굳이 중복 경고를 띄우지 않는다(늑대 소년 방지).
  // 무엇을 사게 되는지 — 서버가 엔진과 같은 선택 로직으로 골라 준 템플릿
  const activeTemplateName = dcaSchedule?.activeTemplateName || "";
  const activeQtyText = Object.entries(dcaSchedule?.activeQuantities ?? {})
    .map(([t, q]) => `${t} ${q}주`)
    .join(" · ");

  // 실수하면 돈이 나가는 사실만 붉은 굵은 글씨로 띄운다. 전부 강조하면 아무것도 강조되지 않는다.
  // (컴포넌트가 아니라 엘리먼트다 — 렌더 중 컴포넌트를 새로 정의하면 매 렌더마다 트리가 갈린다)
  const whatItBuys = activeTemplateName ? (
    <span className="confirm-em">
      {scheduleMonthLabel} 배정 템플릿은 &lsquo;{activeTemplateName}&rsquo;
      {activeQtyText ? ` — ${activeQtyText}` : ""}
    </span>
  ) : (
    <span className="confirm-em">
      {scheduleMonthLabel}에 배정된 템플릿이 없습니다 — 실행해도 매수되지 않습니다
    </span>
  );

  const dcaConfirmSpec = {
    "run-again": {
      icon: "🔁",
      tone: "danger",
      title: `${scheduleMonthLabel}은 이미 매수가 완료됐습니다`,
      body: (
        <>
          <span className="confirm-em">
            지금 실행하면 같은 수량을 한 번 더 매수합니다. 실제 자금이 추가로 집행됩니다.
          </span>
          <br />
          {whatItBuys}
          <br />
          이번 달 적립은 지금까지 <strong>{monthRunCount}회</strong> 실행됐습니다.
        </>
      ),
      confirmLabel: "한 번 더 매수",
    },
    run: {
      icon: "🪙",
      tone: "primary",
      title: `${scheduleMonthLabel} 적립 실행`,
      body: (
        <>
          {whatItBuys}
          <br />
          이 수량 그대로 즉시 매수합니다. 미국장이 닫혀 있으면 주문이 거부됩니다.
        </>
      ),
      confirmLabel: "적립 실행",
    },
    reserve: {
      icon: "⏰",
      tone: "primary",
      title: "다음 개장 때 추가 적립",
      body: (
        <>
          다음 크론 실행(<strong>매일 KST 00:10</strong>, 미국장 장중)에 추가 적립 1회를
          예약합니다.
          <br />
          {whatItBuys}
          <br />
          <span className="confirm-em">
            집행 시점의 배정 템플릿을 그대로 삽니다 — 집행 전에 배정을 바꾸면 바뀐 쪽을 삽니다.
          </span>
          <br />
          이번 달 적립은 지금까지 <strong>{monthRunCount}회</strong> 실행됐습니다.
        </>
      ),
      confirmLabel: "예약하기",
    },
    unreserve: {
      icon: "⏰",
      tone: "primary",
      title: `${scheduleMonthLabel} 추가 적립이 예정된 상태입니다`,
      body: (
        <>
          다음 크론 실행(<strong>매일 KST 00:10</strong>)에 추가 적립 1회가 집행됩니다.
          <br />
          {whatItBuys}
          <br />
          <span className="confirm-em">취소하면 이 집행은 일어나지 않습니다.</span>
          <br />
          이번 달 적립은 지금까지 <strong>{monthRunCount}회</strong> 실행됐습니다.
        </>
      ),
      confirmLabel: "추가 적립 취소",
    },
  };

  const requestDcaRun = () =>
    setDcaConfirm(dcaSchedule?.alreadyRan ? "run-again" : "run");

  const runDca = async () => {
    setDcaConfirm(null);
    try {
      setDcaRunning(true);
      setDcaError(null);
      setDcaResult(null);
      // force=true: 당월 1회 가드는 크론 재호출용이다. 사람이 버튼을 눌렀다면 의도된 추가 매수로 본다.
      const res = await fetch("/api/order/dca-run?force=true", {
        method: "POST",
      });
      if (!res.ok) throw new Error(`적립 실행 실패 (${res.status})`);
      const data = await res.json();
      setDcaResult(data);
      fetchDcaSchedule();
    } catch (err) {
      setDcaError(err.message);
    } finally {
      setDcaRunning(false);
    }
  };

  // ── 추가 적립 예약 토글 ──
  // 즉시 실행은 한국 낮에 누르면 미국장이 닫혀 거부된다. 크론은 이미 개장 직후에 도므로
  // 그 실행이 당월 가드를 한 번만 넘도록 예약해 둔다.
  // 켤 때든 끌 때든 현재 상태와 무엇을 사게 되는지를 같은 형식으로 보여주고 확인받는다.
  const requestToggleSchedule = () =>
    setDcaConfirm(dcaSchedule?.reserved ? "unreserve" : "reserve");

  const setSchedule = async (next) => {
    setDcaConfirm(null);
    try {
      setScheduling(true);
      setDcaError(null);
      const res = await fetch(`/api/order/dca-schedule?reserve=${next}`, {
        method: "POST",
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.error || `예약 변경 실패 (${res.status})`);
      setDcaResult({ message: data.message });
      await fetchDcaSchedule();
    } catch (err) {
      setDcaError(err.message);
    } finally {
      setScheduling(false);
    }
  };

  // ── 신규 티커 현재가 검증 (매수 전용) ──
  const validateNewTicker = async () => {
    const t = newTicker.trim().toUpperCase();
    if (!t) {
      setNewTickerState({ status: "idle", price: 0, error: null });
      return;
    }
    setNewTickerState({ status: "checking", price: 0, error: null });
    try {
      const res = await fetch(`/api/price/${encodeURIComponent(t)}`);
      if (res.status === 404) {
        setNewTickerState({
          status: "invalid",
          price: 0,
          error: "존재하지 않는 티커입니다.",
        });
        return;
      }
      if (!res.ok) throw new Error(`가격 조회 실패 (${res.status})`);
      const data = await res.json();
      setNewTickerState({
        status: "valid",
        price: data.priceUsd ?? 0,
        error: null,
      });
    } catch (err) {
      setNewTickerState({ status: "invalid", price: 0, error: err.message });
    }
  };

  // ── 현재 선택 종목/보유수량/현재가 도출 ──
  const selectedHolding =
    holdings.find((h) => h.ticker === selectedTicker) || null;
  const maxSellQty = selectedHolding?.qty ?? 0;
  const isBuyNew = orderType === "BUY" && buyMode === "new";
  const effectiveTicker =
    orderType === "SELL"
      ? selectedTicker
      : buyMode === "hold"
        ? selectedTicker
        : newTicker.trim().toUpperCase();
  const selectedCurrentPrice = isBuyNew
    ? newTickerState.status === "valid"
      ? newTickerState.price
      : 0
    : (selectedHolding?.currentPrice ?? 0);

  // ── 매도 시 보유수량 초과 입력 자동 보정 (유형/종목 변경 시) ──
  useEffect(() => {
    if (orderType === "SELL" && maxSellQty > 0 && Number(qty) > maxSellQty) {
      setQty(maxSellQty);
    }
    // qty는 의도적으로 의존성에서 제외(입력 중 무한 보정 방지)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orderType, maxSellQty]);

  // 수량 입력 핸들러: 매도는 보유수량을 상한으로 즉시 클램프
  const handleQtyChange = (e) => {
    const raw = e.target.value;
    if (orderType === "SELL" && maxSellQty > 0 && Number(raw) > maxSellQty) {
      setQty(maxSellQty);
    } else {
      setQty(raw);
    }
    setOrderError(null);
  };

  const handleManualOrder = async () => {
    // ── 입력 검증 ──
    if (orderType === "SELL") {
      if (!selectedTicker) {
        setOrderError("매도할 보유 종목을 선택하세요.");
        return;
      }
      if (Number(qty) > maxSellQty) {
        setOrderError(
          `보유 수량(${maxSellQty}주)을 초과해 매도할 수 없습니다.`,
        );
        return;
      }
    } else if (isBuyNew) {
      if (newTickerState.status !== "valid") {
        setOrderError(
          "신규 종목은 먼저 🔍 검증을 통과해야 매수할 수 있습니다.",
        );
        return;
      }
    } else if (!selectedTicker) {
      setOrderError("매수할 보유 종목을 선택하거나 신규 종목을 입력하세요.");
      return;
    }
    if (Number(qty) <= 0) {
      setOrderError("수량은 1 이상이어야 합니다.");
      return;
    }

    // ── 매도: 세금 프리뷰 조회 → 확인 모달 컨텍스트 구성 (실제 제출은 executeOrder) ──
    // 올해 실현 차익은 넘기지 않는다(서버 기본 0). 이 앱은 그 합계를 집계하지 않으므로 사람이
    // 매번 손으로 적어야 맞는 값이었고, 실제로는 늘 비워 두는 입력칸이었다. 그래서 세금 프리뷰는
    // "공제가 전액 남았다"는 가정 아래의 낙관적 추정이며, 확인 모달이 그 사실을 그대로 밝힌다.
    const priceOverride =
      price !== "" && Number(price) > 0 ? Number(price) : null;

    if (orderType === "SELL") {
      let est = null;
      try {
        const params = new URLSearchParams({
          ticker: effectiveTicker,
          qty: String(Number(qty)),
        });
        if (priceOverride) params.set("price", String(priceOverride));
        const pr = await fetch(`/api/order/sell-preview?${params.toString()}`);
        if (pr.ok) est = await pr.json();
      } catch {
        // 프리뷰 실패는 치명적이지 않음 — 확인 모달로 진행(서버 가드가 최종 방어)
      }

      // 프리뷰 결과에 따라 모달 종류를 정한다. 과세면 acknowledgeTax=true로 제출 예정.
      let kind = "sell-plain";
      let acknowledgeTax = false;
      if (est && est.costBasisUnknown) {
        kind = "sell-unknown";
      } else if (est && est.isTaxable) {
        kind = "sell-taxable";
        acknowledgeTax = true;
      } else if (est) {
        kind = "sell-free";
      }

      setConfirmModal({
        kind,
        est,
        acknowledgeTax,
        priceOverride,
        orderType,
        ticker: effectiveTicker,
        qty: Number(qty),
      });
    } else {
      setConfirmModal({
        kind: "buy",
        est: null,
        acknowledgeTax: false,
        priceOverride,
        orderType,
        ticker: effectiveTicker,
        qty: Number(qty),
      });
    }
  };

  // ── 확인 모달에서 '진행'을 눌렀을 때 실제 주문을 제출한다 ──
  const executeOrder = async () => {
    const ctx = confirmModal;
    if (!ctx) return;
    try {
      setOrdering(true);
      setOrderError(null);
      setOrderResult(null);
      const body = {
        ticker: ctx.ticker,
        qty: ctx.qty,
        orderType: ctx.orderType,
      };
      if (ctx.priceOverride) body.price = ctx.priceOverride;
      if (ctx.orderType === "SELL") {
        body.acknowledgeTax = ctx.acknowledgeTax;
      }

      const res = await fetch("/api/order/manual", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      const data = await res.json();
      if (res.status === 409 && data.taxEstimate) {
        // 서버 세금 가드에 막힘(프리뷰 우회 등) — 금액 안내 후 중단
        setOrderError(
          `${data.error} (예상 세금 약 ${won(data.taxEstimate.estimatedTaxKrw)}원)`,
        );
        return;
      }
      if (!res.ok) throw new Error(data.error || `주문 실패 (${res.status})`);
      setOrderResult(data);
      // 주문 후 잔고 반영을 위해 보유 종목 갱신
      fetchHoldings();
    } catch (err) {
      setOrderError(err.message);
    } finally {
      setOrdering(false);
      setConfirmModal(null);
    }
  };

  const hasHoldings = holdings.length > 0;

  return (
    <div className="order-layout">
      {/* ── 좌측: 적립식(DCA) 실행 ── */}
      <div className="card fade-in fade-in-delay-1">
        <h2>적립식 매수 실행</h2>

        <div className="alert alert--info" style={{ marginBottom: 20 }}>
          ℹ️ 타이밍을 판단하지 않고, 이번 달에 배정된{" "}
          <strong>매수 템플릿의 종목별 고정 수량</strong>대로 매수합니다. 1주를
          채우지 못한 잔돈은 다음 사이클로 이월됩니다. 수량·예산·월별 배정은 상단{" "}
          <code>적립 설정</code> 페이지에서 편집합니다.
        </div>

        {dcaSchedule?.alreadyRan && (
          <div className="alert alert--warn" style={{ marginBottom: 12 }}>
            ✅ {scheduleMonthLabel}은 이미 매수가 완료됐습니다
            {dcaSchedule.lastRunDate && (
              <>
                {" "}
                (<strong>{dcaSchedule.lastRunDate}</strong> KST 집행)
              </>
            )}
            . 지금 실행하면 <strong>한 번 더</strong> 매수합니다.
          </div>
        )}

        {/* 적립 지정일 — 매월 이 날짜부터 크론이 적립을 시도한다(KST 기준). */}
        <div className="form-group">
          <label>매월 적립 지정일 (KST)</label>
          <div className="day-grid" role="radiogroup" aria-label="적립 지정일">
            <label
              className={`chip ${runDay === 0 ? "chip--on" : ""}`}
              style={{ gridColumn: "span 7", justifyContent: "center" }}
            >
              <input
                type="radio"
                name="run-day"
                checked={runDay === 0}
                onChange={() => saveRunDay(0)}
                disabled={savingRunDay}
              />
              지정 안 함 (월초부터)
            </label>
            {Array.from({ length: maxRunDay }, (_, i) => i + 1).map((d) => (
              <label
                key={d}
                className={`chip ${runDay === d ? "chip--on" : ""}`}
                style={{ justifyContent: "center" }}
              >
                <input
                  type="radio"
                  name="run-day"
                  checked={runDay === d}
                  onChange={() => saveRunDay(d)}
                  disabled={savingRunDay}
                />
                {d}
              </label>
            ))}
          </div>
          <p
            style={{
              marginTop: 6,
              fontSize: "0.76rem",
              color: "var(--text-muted)",
              wordBreak: "keep-all",
              lineHeight: 1.6,
            }}
          >
            매월 <strong>{runDay > 0 ? `${runDay}일` : "1일"}</strong>부터 크론(매일 KST 00:10)이
            적립을 시도하고, 처음 주문이 접수되는 날 <strong>1회만</strong> 매수합니다.
            지정일이 주말·미국 휴장이면 다음 영업일로 넘어갑니다.
            {runDay > daysInThisMonth && (
              <>
                {" "}
                이번 달은 {daysInThisMonth}일까지라서{" "}
                <strong>{daysInThisMonth}일(말일)</strong>부터 시도합니다 — 없는 날짜는 그 달 말일로
                당깁니다.
              </>
            )}
          </p>
        </div>

        <button
          className="btn btn--primary"
          onClick={requestDcaRun}
          disabled={dcaRunning}
          style={{ width: "100%", padding: "14px", fontSize: "1rem" }}
        >
          {dcaRunning ? "⏳ 실행 요청 중..." : "🪙 지금 적립 실행"}
        </button>

        {/* 미국장이 닫힌 시간에 눌러도 주문이 거부되므로, 개장 직후 도는 크론에 1회 예약해 둔다.
            켜짐/꺼짐을 문구가 아니라 버튼이 눌린 모양으로 보여준다(토글). */}
        <button
          className={`btn ${dcaSchedule?.reserved ? "btn--primary" : "btn--outline"}`}
          onClick={requestToggleSchedule}
          disabled={scheduling}
          aria-pressed={!!dcaSchedule?.reserved}
          style={{ width: "100%", padding: "12px", fontSize: "0.9rem", marginTop: 8 }}
        >
          {scheduling ? "⏳ 예약 변경 중..." : "추가 적립"}
        </button>
        <p style={{ marginTop: 6, fontSize: "0.76rem", color: "var(--text-muted)", wordBreak: "keep-all" }}>
          한국 낮에는 미국장이 닫혀 있어 즉시 실행이 거부됩니다. 예약하면 매일 KST 00:10에 도는
          크론이 미국장 장중에 1회 집행하고 예약을 소진합니다. 주문 접수가 없으면 예약이 남아 다음 날 다시 시도합니다.
        </p>

        {dcaError && (
          <div className="alert alert--err" style={{ marginTop: 16 }}>
            ❌ {dcaError}
          </div>
        )}

        {dcaResult && (
          <div className="fade-in" style={{ marginTop: 20 }}>
            <div className="alert alert--ok">✅ {dcaResult.message}</div>
            <p
              style={{
                marginTop: 10,
                fontSize: "0.8rem",
                color: "var(--text-muted)",
              }}
            >
              사이클은 백그라운드에서 처리됩니다. 체결 내역은{" "}
              <strong>거래 내역</strong> 탭과 이메일 보고서에서 확인하세요.
            </p>
          </div>
        )}
      </div>

      {/* ── 우측: 수동 주문 ── */}
      <div
        className={`card fade-in fade-in-delay-2 manual-order manual-order--${orderType === "BUY" ? "buy" : "sell"}`}
      >
        {/* 주문 유형은 이 카드에서 가장 먼저 정하는 값이라 제목 줄 우측 토글로 둔다
            (대시보드 통화 토글과 같은 모양). 카드의 강조색이 매수=빨강/매도=파랑으로 함께 바뀐다. */}
        <div className="card-head">
          <h2>수동 주문</h2>
          <div className="ccy-toggle" role="group" aria-label="주문 유형">
            <button
              type="button"
              className={orderType === "BUY" ? "active" : ""}
              onClick={() => {
                setOrderType("BUY");
                setOrderError(null);
              }}
            >
              매수
            </button>
            <button
              type="button"
              className={orderType === "SELL" ? "active" : ""}
              onClick={() => {
                setOrderType("SELL");
                setOrderError(null);
              }}
            >
              매도
            </button>
          </div>
        </div>

        <div className="alert alert--warn" style={{ marginBottom: 20 }}>
          ⚠️ 판단 없이 즉시 주문이 실행됩니다. 매도는 보유 종목·보유수량
          내에서만 가능하며, 매수는 보유 종목 또는 검증된 신규 종목만
          주문됩니다.
        </div>

        {/* 종목 선택 방식 — 매수에만 있는 선택. 매도는 보유 종목 안에서만 가능하다. */}
        {orderType === "BUY" && (
          <div className="form-group">
            <label>종목 선택 방식</label>
            <div className="chip-row" role="radiogroup" aria-label="종목 선택 방식">
              {[
                { id: "hold", name: "보유 종목" },
                { id: "new", name: "신규 입력" },
              ].map((m) => (
                <label
                  key={m.id}
                  className={`chip ${buyMode === m.id ? "chip--on" : ""}`}
                >
                  <input
                    type="radio"
                    name="buy-mode"
                    checked={buyMode === m.id}
                    onChange={() => {
                      setBuyMode(m.id);
                      setOrderError(null);
                    }}
                  />
                  {m.name}
                </label>
              ))}
            </div>
          </div>
        )}

        {/* 종목: 보유종목 목록 (매도 전체 / 매수 'hold' 모드) */}
        {!isBuyNew && (
          <div className="form-group">
            <label>종목 {orderType === "SELL" ? "(보유 종목)" : ""}</label>
            {holdingsLoading ? (
              <div
                style={{
                  fontSize: "0.85rem",
                  color: "var(--text-muted)",
                  padding: "8px 0",
                }}
              >
                보유 종목을 불러오는 중...
              </div>
            ) : holdingsError ? (
              <div style={{ fontSize: "0.85rem", color: "var(--loss-red)" }}>
                ❌ {holdingsError}{" "}
                <button
                  type="button"
                  className="btn btn--outline"
                  onClick={fetchHoldings}
                  style={{ padding: "2px 10px", fontSize: "0.8rem" }}
                >
                  다시 시도
                </button>
              </div>
            ) : !hasHoldings ? (
              <div style={{ fontSize: "0.85rem", color: "var(--text-muted)" }}>
                보유 종목이 없습니다.{" "}
                {orderType === "BUY" && "신규 직접입력으로 매수하세요."}
              </div>
            ) : (
              <div className="pick-list" role="radiogroup" aria-label="보유 종목">
                {holdings.map((h) => (
                  <label
                    key={h.ticker}
                    className={`chip ${selectedTicker === h.ticker ? "chip--on" : ""}`}
                  >
                    <input
                      type="radio"
                      name="holding-ticker"
                      checked={selectedTicker === h.ticker}
                      onChange={() => {
                        setSelectedTicker(h.ticker);
                        setOrderError(null);
                      }}
                    />
                    {h.ticker}
                    <span className="pick-list__num">
                      {h.qty}주
                      <span className="cell-sub">
                        ${h.currentPrice?.toFixed?.(2) ?? h.currentPrice}
                      </span>
                    </span>
                  </label>
                ))}
              </div>
            )}
          </div>
        )}

        {/* 종목: 신규 직접입력 (매수 'new' 모드) */}
        {isBuyNew && (
          <div className="form-group">
            <label>신규 종목 코드</label>
            <div style={{ display: "flex", gap: 8 }}>
              <input
                type="text"
                value={newTicker}
                onChange={(e) => {
                  setNewTicker(e.target.value.toUpperCase());
                  setNewTickerState({ status: "idle", price: 0, error: null });
                }}
                placeholder="예: VOO"
                style={{ flex: 1 }}
              />
              <button
                type="button"
                className="btn btn--outline"
                onClick={validateNewTicker}
                disabled={
                  newTickerState.status === "checking" || !newTicker.trim()
                }
              >
                {newTickerState.status === "checking"
                  ? "검증 중..."
                  : "🔍 검증"}
              </button>
            </div>
            {newTickerState.status === "valid" && (
              <div
                style={{
                  marginTop: 6,
                  fontSize: "0.82rem",
                  color: "var(--profit-green)",
                }}
              >
                ✅ 유효 · 현재가 $
                {newTickerState.price?.toFixed?.(2) ?? newTickerState.price}
              </div>
            )}
            {newTickerState.status === "invalid" && (
              <div
                style={{
                  marginTop: 6,
                  fontSize: "0.82rem",
                  color: "var(--loss-red)",
                }}
              >
                ❌ {newTickerState.error}
              </div>
            )}
          </div>
        )}

        {/* 수량 / 가격 */}
        <div style={{ display: "flex", gap: 10 }}>
          <div className="form-group" style={{ flex: 1 }}>
            <label>
              수량
              {orderType === "SELL" && selectedHolding
                ? ` (보유 ${maxSellQty}주)`
                : ""}
            </label>
            <input
              type="number"
              min="1"
              max={orderType === "SELL" ? maxSellQty || undefined : undefined}
              value={qty}
              onChange={handleQtyChange}
            />
          </div>
          <div className="form-group" style={{ flex: 1 }}>
            <label>가격 (USD, 비우면 현재가)</label>
            <input
              type="number"
              min="0"
              step="0.01"
              value={price}
              onChange={(e) => setPrice(e.target.value)}
              placeholder={
                selectedCurrentPrice > 0
                  ? `현재가 $${selectedCurrentPrice.toFixed(2)}`
                  : "현재가 사용"
              }
            />
          </div>
        </div>


        {orderType === "SELL" && (
          <button
            type="button"
            className="btn btn--outline"
            style={{ fontSize: "0.8rem", padding: "4px 10px", marginBottom: 8 }}
            onClick={() => setQty(maxSellQty)}
            disabled={maxSellQty <= 0}
          >
            전량({maxSellQty}주)
          </button>
        )}

        <button
          className="btn btn--primary"
          onClick={handleManualOrder}
          disabled={ordering}
          style={{
            width: "100%",
            padding: "14px",
            fontSize: "1rem",
            marginTop: 8,
          }}
        >
          {ordering
            ? "⏳ 주문 중..."
            : orderType === "BUY"
              ? "📈 매수 주문"
              : "📉 매도 주문"}
        </button>

        {orderError && (
          <div className="alert alert--err" style={{ marginTop: 16 }}>
            ❌ {orderError}
          </div>
        )}

        {orderResult && (
          <div className="fade-in" style={{ marginTop: 20 }}>
            <div className="alert alert--ok" style={{ marginBottom: 12 }}>
              ✅ {orderResult.message}
            </div>
            <div
              style={{
                fontSize: "0.85rem",
                color: "var(--text-secondary)",
                lineHeight: 1.8,
              }}
            >
              <div>
                종목:{" "}
                <strong style={{ color: "var(--text-primary)" }}>
                  {orderResult.ticker}
                </strong>
              </div>
              <div>
                유형:{" "}
                <strong style={{ color: "var(--text-primary)" }}>
                  {orderResult.orderType}
                </strong>
              </div>
              <div>
                수량:{" "}
                <strong style={{ color: "var(--text-primary)" }}>
                  {orderResult.qty}주
                </strong>
              </div>
              <div>
                체결가:{" "}
                <strong style={{ color: "var(--text-primary)" }}>
                  ${orderResult.price?.toFixed?.(2) ?? orderResult.price}
                </strong>
              </div>
              <div>
                주문번호:{" "}
                <strong style={{ color: "var(--text-primary)" }}>
                  {orderResult.orderNo}
                </strong>
              </div>
            </div>
          </div>
        )}
      </div>

      {dcaConfirm && (
        <ConfirmDialog
          spec={dcaConfirmSpec[dcaConfirm]}
          busy={dcaRunning || scheduling}
          onCancel={() => setDcaConfirm(null)}
          onConfirm={() => {
            if (dcaConfirm === "reserve") setSchedule(true);
            else if (dcaConfirm === "unreserve") setSchedule(false);
            else runDca();
          }}
        />
      )}

      {confirmModal && (
        <OrderConfirmModal
          ctx={confirmModal}
          ordering={ordering}
          onCancel={() => setConfirmModal(null)}
          onConfirm={executeOrder}
        />
      )}
    </div>
  );
};

export default Order;
