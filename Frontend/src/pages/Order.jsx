import { useState, useEffect, useCallback } from 'react';

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

  // ── 보유 종목 (수동 주문 종목 선택 소스) ──
  const [holdings, setHoldings] = useState([]);
  const [holdingsLoading, setHoldingsLoading] = useState(true);
  const [holdingsError, setHoldingsError] = useState(null);

  // ── 수동 주문 상태 ──
  const [orderType, setOrderType] = useState('BUY');
  const [buyMode, setBuyMode] = useState('hold'); // 'hold' = 보유종목 선택 / 'new' = 신규 직접입력 (매수 전용)
  const [selectedTicker, setSelectedTicker] = useState(''); // 보유종목 드롭다운 선택값
  const [newTicker, setNewTicker] = useState(''); // 신규 직접입력 티커
  // 신규 티커 검증 상태: idle | checking | valid | invalid
  const [newTickerState, setNewTickerState] = useState({ status: 'idle', price: 0, error: null });
  const [qty, setQty] = useState(1);
  const [price, setPrice] = useState('');
  const [ordering, setOrdering] = useState(false);
  const [orderResult, setOrderResult] = useState(null);
  const [orderError, setOrderError] = useState(null);

  // ── 보유 종목 로드 ──
  const fetchHoldings = useCallback(async () => {
    try {
      setHoldingsLoading(true);
      setHoldingsError(null);
      const res = await fetch('/api/portfolio/holdings');
      if (!res.ok) throw new Error(`보유 종목 조회 실패 (${res.status})`);
      const data = await res.json();
      const list = Array.isArray(data) ? data : [];
      setHoldings(list);
      setSelectedTicker(prev => prev || (list[0]?.ticker ?? ''));
    } catch (err) {
      setHoldingsError(err.message);
    } finally {
      setHoldingsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchHoldings();
  }, [fetchHoldings]);

  const handleDcaRun = async () => {
    if (!confirm('설정된 목표비중(Dca:Targets)대로 적립식 매수 사이클을 실행합니다.\n정말 진행하시겠습니까?')) return;
    try {
      setDcaRunning(true);
      setDcaError(null);
      setDcaResult(null);
      const res = await fetch('/api/order/dca-run', { method: 'POST' });
      if (!res.ok) throw new Error(`적립 실행 실패 (${res.status})`);
      const data = await res.json();
      setDcaResult(data);
    } catch (err) {
      setDcaError(err.message);
    } finally {
      setDcaRunning(false);
    }
  };

  // ── 신규 티커 현재가 검증 (매수 전용) ──
  const validateNewTicker = async () => {
    const t = newTicker.trim().toUpperCase();
    if (!t) {
      setNewTickerState({ status: 'idle', price: 0, error: null });
      return;
    }
    setNewTickerState({ status: 'checking', price: 0, error: null });
    try {
      const res = await fetch(`/api/price/${encodeURIComponent(t)}`);
      if (res.status === 404) {
        setNewTickerState({ status: 'invalid', price: 0, error: '존재하지 않는 티커입니다.' });
        return;
      }
      if (!res.ok) throw new Error(`가격 조회 실패 (${res.status})`);
      const data = await res.json();
      setNewTickerState({ status: 'valid', price: data.priceUsd ?? 0, error: null });
    } catch (err) {
      setNewTickerState({ status: 'invalid', price: 0, error: err.message });
    }
  };

  // ── 현재 선택 종목/보유수량/현재가 도출 ──
  const selectedHolding = holdings.find(h => h.ticker === selectedTicker) || null;
  const maxSellQty = selectedHolding?.qty ?? 0;
  const isBuyNew = orderType === 'BUY' && buyMode === 'new';
  const effectiveTicker = orderType === 'SELL'
    ? selectedTicker
    : (buyMode === 'hold' ? selectedTicker : newTicker.trim().toUpperCase());
  const selectedCurrentPrice = isBuyNew
    ? (newTickerState.status === 'valid' ? newTickerState.price : 0)
    : (selectedHolding?.currentPrice ?? 0);

  // ── 매도 시 보유수량 초과 입력 자동 보정 (유형/종목 변경 시) ──
  useEffect(() => {
    if (orderType === 'SELL' && maxSellQty > 0 && Number(qty) > maxSellQty) {
      setQty(maxSellQty);
    }
    // qty는 의도적으로 의존성에서 제외(입력 중 무한 보정 방지)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orderType, maxSellQty]);

  // 수량 입력 핸들러: 매도는 보유수량을 상한으로 즉시 클램프
  const handleQtyChange = (e) => {
    const raw = e.target.value;
    if (orderType === 'SELL' && maxSellQty > 0 && Number(raw) > maxSellQty) {
      setQty(maxSellQty);
    } else {
      setQty(raw);
    }
    setOrderError(null);
  };

  const handleManualOrder = async () => {
    // ── 입력 검증 ──
    if (orderType === 'SELL') {
      if (!selectedTicker) {
        setOrderError('매도할 보유 종목을 선택하세요.');
        return;
      }
      if (Number(qty) > maxSellQty) {
        setOrderError(`보유 수량(${maxSellQty}주)을 초과해 매도할 수 없습니다.`);
        return;
      }
    } else if (isBuyNew) {
      if (newTickerState.status !== 'valid') {
        setOrderError('신규 종목은 먼저 🔍 검증을 통과해야 매수할 수 있습니다.');
        return;
      }
    } else if (!selectedTicker) {
      setOrderError('매수할 보유 종목을 선택하거나 신규 종목을 입력하세요.');
      return;
    }
    if (Number(qty) <= 0) {
      setOrderError('수량은 1 이상이어야 합니다.');
      return;
    }

    const actionLabel = orderType === 'BUY' ? '매수' : '매도';
    if (!confirm(`${effectiveTicker} ${qty}주를 ${actionLabel}합니다.\n정말 진행하시겠습니까?`)) return;

    try {
      setOrdering(true);
      setOrderError(null);
      setOrderResult(null);
      const body = {
        ticker: effectiveTicker,
        qty: Number(qty),
        orderType,
      };
      if (price !== '' && Number(price) > 0) body.price = Number(price);

      const res = await fetch('/api/order/manual', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || `주문 실패 (${res.status})`);
      setOrderResult(data);
      // 주문 후 잔고 반영을 위해 보유 종목 갱신
      fetchHoldings();
    } catch (err) {
      setOrderError(err.message);
    } finally {
      setOrdering(false);
    }
  };

  const hasHoldings = holdings.length > 0;

  return (
    <div className="order-layout">
      {/* ── 좌측: 적립식(DCA) 실행 ── */}
      <div className="card fade-in fade-in-delay-1">
        <h2>적립식 매수 실행</h2>

        <div style={{
          padding: '14px 16px',
          background: 'rgba(59, 130, 246, 0.08)',
          border: '1px solid rgba(59, 130, 246, 0.25)',
          borderRadius: 'var(--radius-sm)',
          marginBottom: 20,
          fontSize: '0.85rem',
          color: 'var(--text-secondary)',
          lineHeight: 1.6,
          wordBreak: 'keep-all'
        }}>
          ℹ️ 타이밍을 판단하지 않고, 설정된 <strong>목표비중(Dca:Targets)</strong>을 향해
          정해진 예산만큼 정수 단위로 매수합니다. 1주를 채우지 못한 잔돈은 다음 사이클로 이월됩니다.
          (예산·목표비중은 <code>appsettings.json</code>의 <code>Dca</code> 섹션에서 설정)
        </div>

        <button
          className="btn btn--primary"
          onClick={handleDcaRun}
          disabled={dcaRunning}
          style={{ width: '100%', padding: '14px', fontSize: '1rem' }}
        >
          {dcaRunning ? '⏳ 실행 요청 중...' : '🪙 지금 적립 실행'}
        </button>

        {dcaError && (
          <div style={{ marginTop: 16, padding: '10px 14px', background: 'var(--loss-red-bg)', color: 'var(--loss-red)', borderRadius: 'var(--radius-sm)', fontSize: '0.85rem' }}>
            ❌ {dcaError}
          </div>
        )}

        {dcaResult && (
          <div className="fade-in" style={{ marginTop: 20 }}>
            <div style={{
              padding: '12px 16px',
              background: 'var(--profit-green-bg)',
              color: 'var(--profit-green)',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.85rem',
              lineHeight: 1.6
            }}>
              ✅ {dcaResult.message}
            </div>
            <p style={{ marginTop: 10, fontSize: '0.8rem', color: 'var(--text-muted)' }}>
              사이클은 백그라운드에서 처리됩니다. 체결 내역은 <strong>거래 내역</strong> 탭과 이메일 보고서에서 확인하세요.
            </p>
          </div>
        )}
      </div>

      {/* ── 우측: 수동 주문 ── */}
      <div className="card fade-in fade-in-delay-2">
        <h2>수동 주문</h2>

        <div style={{
          padding: '14px 16px',
          background: 'rgba(245, 158, 11, 0.08)',
          border: '1px solid rgba(245, 158, 11, 0.2)',
          borderRadius: 'var(--radius-sm)',
          marginBottom: 20,
          fontSize: '0.82rem',
          color: 'var(--warn-amber)',
          wordBreak: 'keep-all'
        }}>
          ⚠️ 판단 없이 즉시 주문이 실행됩니다. 매도는 보유 종목·보유수량 내에서만 가능하며,
          매수는 보유 종목 또는 검증된 신규 종목만 주문됩니다.
        </div>

        {/* 주문 유형 */}
        <div className="form-group">
          <label>주문 유형</label>
          <select
            value={orderType}
            onChange={e => {
              setOrderType(e.target.value);
              setOrderError(null);
            }}
          >
            <option value="BUY">매수 (BUY)</option>
            <option value="SELL">매도 (SELL)</option>
          </select>
        </div>

        {/* 매수 종목 소스 토글 (매수 전용) */}
        {orderType === 'BUY' && (
          <div className="form-group">
            <label>종목 선택 방식</label>
            <div style={{ display: 'flex', gap: 8 }}>
              <button
                type="button"
                className={`btn ${buyMode === 'hold' ? 'btn--primary' : 'btn--outline'}`}
                style={{ flex: 1 }}
                onClick={() => { setBuyMode('hold'); setOrderError(null); }}
              >
                보유 종목 선택
              </button>
              <button
                type="button"
                className={`btn ${buyMode === 'new' ? 'btn--primary' : 'btn--outline'}`}
                style={{ flex: 1 }}
                onClick={() => { setBuyMode('new'); setOrderError(null); }}
              >
                신규 직접입력
              </button>
            </div>
          </div>
        )}

        {/* 종목: 보유종목 드롭다운 (매도 전체 / 매수 'hold' 모드) */}
        {!isBuyNew && (
          <div className="form-group">
            <label>종목 {orderType === 'SELL' ? '(보유 종목)' : ''}</label>
            {holdingsLoading ? (
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', padding: '8px 0' }}>
                보유 종목을 불러오는 중...
              </div>
            ) : holdingsError ? (
              <div style={{ fontSize: '0.85rem', color: 'var(--loss-red)' }}>
                ❌ {holdingsError}{' '}
                <button type="button" className="btn btn--outline" onClick={fetchHoldings} style={{ padding: '2px 10px', fontSize: '0.8rem' }}>다시 시도</button>
              </div>
            ) : !hasHoldings ? (
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                보유 종목이 없습니다. {orderType === 'BUY' && '신규 직접입력으로 매수하세요.'}
              </div>
            ) : (
              <select
                value={selectedTicker}
                onChange={e => { setSelectedTicker(e.target.value); setOrderError(null); }}
              >
                {holdings.map(h => (
                  <option key={h.ticker} value={h.ticker}>
                    {h.ticker} · {h.qty}주 보유 · ${h.currentPrice?.toFixed?.(2) ?? h.currentPrice}
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
            <div style={{ display: 'flex', gap: 8 }}>
              <input
                type="text"
                value={newTicker}
                onChange={e => {
                  setNewTicker(e.target.value.toUpperCase());
                  setNewTickerState({ status: 'idle', price: 0, error: null });
                }}
                placeholder="예: VOO"
                style={{ flex: 1 }}
              />
              <button
                type="button"
                className="btn btn--outline"
                onClick={validateNewTicker}
                disabled={newTickerState.status === 'checking' || !newTicker.trim()}
              >
                {newTickerState.status === 'checking' ? '검증 중...' : '🔍 검증'}
              </button>
            </div>
            {newTickerState.status === 'valid' && (
              <div style={{ marginTop: 6, fontSize: '0.82rem', color: 'var(--profit-green)' }}>
                ✅ 유효 · 현재가 ${newTickerState.price?.toFixed?.(2) ?? newTickerState.price}
              </div>
            )}
            {newTickerState.status === 'invalid' && (
              <div style={{ marginTop: 6, fontSize: '0.82rem', color: 'var(--loss-red)' }}>
                ❌ {newTickerState.error}
              </div>
            )}
          </div>
        )}

        {/* 수량 / 가격 */}
        <div style={{ display: 'flex', gap: 10 }}>
          <div className="form-group" style={{ flex: 1 }}>
            <label>
              수량{orderType === 'SELL' && selectedHolding ? ` (보유 ${maxSellQty}주)` : ''}
            </label>
            <input
              type="number"
              min="1"
              max={orderType === 'SELL' ? maxSellQty || undefined : undefined}
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
              onChange={e => setPrice(e.target.value)}
              placeholder={selectedCurrentPrice > 0 ? `현재가 $${selectedCurrentPrice.toFixed(2)}` : '현재가 사용'}
            />
          </div>
        </div>

        {orderType === 'SELL' && (
          <button
            type="button"
            className="btn btn--outline"
            style={{ fontSize: '0.8rem', padding: '4px 10px', marginBottom: 8 }}
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
          style={{ width: '100%', padding: '14px', fontSize: '1rem', marginTop: 8 }}
        >
          {ordering ? '⏳ 주문 중...' : (orderType === 'BUY' ? '📈 매수 주문' : '📉 매도 주문')}
        </button>

        {orderError && (
          <div style={{ marginTop: 16, padding: '10px 14px', background: 'var(--loss-red-bg)', color: 'var(--loss-red)', borderRadius: 'var(--radius-sm)', fontSize: '0.85rem' }}>
            ❌ {orderError}
          </div>
        )}

        {orderResult && (
          <div className="fade-in" style={{ marginTop: 20 }}>
            <div style={{
              padding: '12px 16px',
              background: 'var(--profit-green-bg)',
              color: 'var(--profit-green)',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.85rem',
              marginBottom: 12
            }}>
              ✅ {orderResult.message}
            </div>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', lineHeight: 1.8 }}>
              <div>종목: <strong style={{ color: 'var(--text-primary)' }}>{orderResult.ticker}</strong></div>
              <div>유형: <strong style={{ color: 'var(--text-primary)' }}>{orderResult.orderType}</strong></div>
              <div>수량: <strong style={{ color: 'var(--text-primary)' }}>{orderResult.qty}주</strong></div>
              <div>체결가: <strong style={{ color: 'var(--text-primary)' }}>${orderResult.price?.toFixed?.(2) ?? orderResult.price}</strong></div>
              <div>주문번호: <strong style={{ color: 'var(--text-primary)' }}>{orderResult.orderNo}</strong></div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Order;
