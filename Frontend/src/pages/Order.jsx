import { useState, useEffect, useCallback } from "react";

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
 * 범용 확인 모달 — 브라우저 기본 confirm() 대신 앱 테마를 그대로 쓴다.
 * 주문 확인(OrderConfirmModal)은 세금 표 등 전용 내용이 많아 따로 두고, 이쪽은
 * 문구 하나만 보여주면 되는 확인(적립 실행·예약)에 쓴다.
 */
const ConfirmDialog = ({ spec, busy, onCancel, onConfirm }) => (
  <div
    className="modal-overlay"
    onClick={() => {
      if (!busy) onCancel();
    }}
  >
    <div
      className="modal-content"
      onClick={(ev) => ev.stopPropagation()}
      style={{ maxWidth: 440 }}
    >
      <h3
        style={{
          marginBottom: 14,
          borderBottom: "1px solid var(--border-primary)",
          paddingBottom: 12,
          color:
            spec.tone === "danger" ? "var(--loss-red)" : "var(--text-primary)",
        }}
      >
        {spec.icon} {spec.title}
      </h3>

      <p
        style={{
          fontSize: "0.9rem",
          lineHeight: 1.7,
          color: "var(--text-secondary)",
          wordBreak: "keep-all",
        }}
      >
        {spec.body}
      </p>

      <div style={{ display: "flex", gap: 10, marginTop: 18 }}>
        <button
          className="btn btn--outline"
          style={{ flex: 1 }}
          onClick={onCancel}
          disabled={busy}
        >
          취소
        </button>
        <button
          className={`btn ${spec.tone === "danger" ? "btn--danger" : "btn--primary"}`}
          style={{ flex: 1 }}
          onClick={onConfirm}
          disabled={busy}
        >
          {busy ? "처리 중..." : spec.confirmLabel}
        </button>
      </div>
    </div>
  </div>
);

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
  // 적립 실행·예약 확인 모달 키 ('run' | 'run-again' | 'reserve', null = 닫힘)
  const [dcaConfirm, setDcaConfirm] = useState(null);

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
  // 매도 절세 계산용: 올해 이미 실현한 양도차익(원, 수동 입력)
  const [ytdGain, setYtdGain] = useState("");
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

  // ── 이번 달 적립 상태 조회 (실행 여부 + 예약 여부) ──
  const fetchDcaSchedule = useCallback(async () => {
    try {
      const res = await fetch("/api/order/dca-schedule");
      if (!res.ok) return; // 보조 정보 — 실패해도 실행 버튼은 막지 않는다
      setDcaSchedule(await res.json());
    } catch {
      // 조회 실패 시 확인 문구만 일반형으로 떨어진다
    }
  }, []);

  useEffect(() => {
    fetchHoldings();
    fetchDcaSchedule();
  }, [fetchHoldings, fetchDcaSchedule]);

  // 이번 달 몇 월인지 (KST 기준 서버 응답 "yyyy-MM"에서 월만 뽑음)
  const scheduleMonthLabel = dcaSchedule
    ? `${Number(dcaSchedule.month.slice(5, 7))}월`
    : "이번 달";

  // 실행/예약 확인 모달 컨텍스트 (null = 닫힘)
  // 이미 적립한 달이면 "한 번 더 사는 것"임을 분명히 묻고, 아직 안 한 달이면
  // 굳이 중복 경고를 띄우지 않는다(늑대 소년 방지).
  const dcaConfirmSpec = {
    "run-again": {
      icon: "🔁",
      tone: "danger",
      title: `${scheduleMonthLabel}은 이미 매수가 완료됐습니다`,
      body: (
        <>
          지금 실행하면 <strong>{scheduleMonthLabel}에 배정된 템플릿의 수량만큼 한 번 더</strong>{" "}
          매수합니다. 실제 자금이 추가로 집행됩니다.
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
          {scheduleMonthLabel}에 배정된 매수 템플릿의 <strong>종목별 고정 수량</strong>대로
          매수합니다. 1주를 채우지 못한 잔돈은 다음 사이클로 이월됩니다.
        </>
      ),
      confirmLabel: "적립 실행",
    },
    reserve: {
      icon: "🗓",
      tone: "primary",
      title: "다음 개장 때 추가 적립 예약",
      body: (
        <>
          다음 크론 실행(<strong>매일 KST 23:40</strong>, 미국장 개장 직후)에 추가 적립 1회를
          예약합니다. 집행 시점의 <strong>{scheduleMonthLabel} 배정 템플릿</strong>을 그대로 사므로,
          사려는 템플릿이 배정돼 있는지 먼저 확인하세요.
        </>
      ),
      confirmLabel: "예약하기",
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
  // 예약 설정은 확인을 받고, 해제는 돈이 나가지 않으므로 바로 처리한다.
  const requestToggleSchedule = () => {
    if (dcaSchedule?.reserved) setSchedule(false);
    else setDcaConfirm("reserve");
  };

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
    const ytdNum = ytdGain !== "" && Number(ytdGain) > 0 ? Number(ytdGain) : 0;
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
        if (ytdNum > 0) params.set("ytd", String(ytdNum));
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
        ytdNum,
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
        ytdNum: 0,
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
        body.ytdRealizedGainKrw = ctx.ytdNum;
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
            ✅ {scheduleMonthLabel}은 이미 매수가 완료됐습니다. 지금 실행하면{" "}
            <strong>한 번 더</strong> 매수합니다.
          </div>
        )}

        <button
          className="btn btn--primary"
          onClick={requestDcaRun}
          disabled={dcaRunning}
          style={{ width: "100%", padding: "14px", fontSize: "1rem" }}
        >
          {dcaRunning ? "⏳ 실행 요청 중..." : "🪙 지금 적립 실행"}
        </button>

        {/* 미국장이 닫힌 시간에 눌러도 주문이 거부되므로, 개장 직후 도는 크론에 1회 예약해 둔다 */}
        <button
          className="btn btn--outline"
          onClick={requestToggleSchedule}
          disabled={scheduling}
          style={{ width: "100%", padding: "12px", fontSize: "0.9rem", marginTop: 8 }}
        >
          {scheduling
            ? "⏳ 예약 변경 중..."
            : dcaSchedule?.reserved
              ? "🗓 예약됨 — 다음 개장 때 추가 적립 (누르면 취소)"
              : "🗓 다음 개장 때 추가 적립 예약"}
        </button>
        <p style={{ marginTop: 6, fontSize: "0.76rem", color: "var(--text-muted)", wordBreak: "keep-all" }}>
          한국 낮에는 미국장이 닫혀 있어 즉시 실행이 거부됩니다. 예약하면 매일 KST 23:40에 도는
          크론이 개장 직후 1회 집행하고 예약을 소진합니다. 주문 접수가 없으면 예약이 남아 다음 날 다시 시도합니다.
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
      <div className="card fade-in fade-in-delay-2">
        <h2>수동 주문</h2>

        <div className="alert alert--warn" style={{ marginBottom: 20 }}>
          ⚠️ 판단 없이 즉시 주문이 실행됩니다. 매도는 보유 종목·보유수량
          내에서만 가능하며, 매수는 보유 종목 또는 검증된 신규 종목만
          주문됩니다.
        </div>

        {/* 주문 유형 */}
        <div className="form-group">
          <label>주문 유형</label>
          <select
            value={orderType}
            onChange={(e) => {
              setOrderType(e.target.value);
              setOrderError(null);
            }}
          >
            <option value="BUY">매수 (BUY)</option>
            <option value="SELL">매도 (SELL)</option>
          </select>
        </div>

        {/* 매수 종목 소스 토글 (매수 전용) */}
        {orderType === "BUY" && (
          <div className="form-group">
            <label>종목 선택 방식</label>
            <div style={{ display: "flex", gap: 8 }}>
              <button
                type="button"
                className={`btn ${buyMode === "hold" ? "btn--primary" : "btn--outline"}`}
                style={{ flex: 1 }}
                onClick={() => {
                  setBuyMode("hold");
                  setOrderError(null);
                }}
              >
                보유 종목 선택
              </button>
              <button
                type="button"
                className={`btn ${buyMode === "new" ? "btn--primary" : "btn--outline"}`}
                style={{ flex: 1 }}
                onClick={() => {
                  setBuyMode("new");
                  setOrderError(null);
                }}
              >
                신규 직접입력
              </button>
            </div>
          </div>
        )}

        {/* 종목: 보유종목 드롭다운 (매도 전체 / 매수 'hold' 모드) */}
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
              <select
                value={selectedTicker}
                onChange={(e) => {
                  setSelectedTicker(e.target.value);
                  setOrderError(null);
                }}
              >
                {holdings.map((h) => (
                  <option key={h.ticker} value={h.ticker}>
                    {h.ticker} · {h.qty}주 보유 · $
                    {h.currentPrice?.toFixed?.(2) ?? h.currentPrice}
                  </option>
                ))}
              </select>
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
          <div className="form-group">
            <label>올해 이미 실현한 차익 (원, 선택)</label>
            <input
              type="number"
              min="0"
              step="10000"
              value={ytdGain}
              onChange={(e) => setYtdGain(e.target.value)}
              placeholder="0 (증권사 앱 등 외부 매도분이 있으면 입력)"
            />
            <div
              style={{
                marginTop: 4,
                fontSize: "0.75rem",
                color: "var(--text-muted)",
                wordBreak: "keep-all",
              }}
            >
              연 250만원 기본공제 중 남은 금액 계산에 사용됩니다. 이 시스템
              밖에서 매도한 차익은 자동 집계되지 않으니 직접 입력하세요.
            </div>
          </div>
        )}

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
          onConfirm={() => (dcaConfirm === "reserve" ? setSchedule(true) : runDca())}
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
