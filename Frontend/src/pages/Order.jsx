import { useState } from 'react';

/**
 * 주문/적립 페이지.
 * OrderController와 연동하여 적립식(DCA) 매수 사이클과 수동 주문을 실행합니다.
 * 퀀트/AI 판단 레이어는 제거되었으며, 정해진 목표비중대로의 정수 매수와
 * 사용자 지정 수동 주문만 제공합니다.
 */
const Order = () => {
  // ── 적립식(DCA) 실행 상태 ──
  const [dcaRunning, setDcaRunning] = useState(false);
  const [dcaResult, setDcaResult] = useState(null);
  const [dcaError, setDcaError] = useState(null);

  // ── 수동 주문 상태 ──
  const [ticker, setTicker] = useState('QQQM');
  const [qty, setQty] = useState(1);
  const [orderType, setOrderType] = useState('BUY');
  const [price, setPrice] = useState('');
  const [ordering, setOrdering] = useState(false);
  const [orderResult, setOrderResult] = useState(null);
  const [orderError, setOrderError] = useState(null);

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

  const handleManualOrder = async () => {
    if (!ticker.trim()) {
      setOrderError('종목 코드를 입력하세요.');
      return;
    }
    if (qty <= 0) {
      setOrderError('수량은 1 이상이어야 합니다.');
      return;
    }
    const actionLabel = orderType === 'BUY' ? '매수' : '매도';
    if (!confirm(`${ticker.toUpperCase()} ${qty}주를 ${actionLabel}합니다.\n정말 진행하시겠습니까?`)) return;

    try {
      setOrdering(true);
      setOrderError(null);
      setOrderResult(null);
      const body = {
        ticker: ticker.trim().toUpperCase(),
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
    } catch (err) {
      setOrderError(err.message);
    } finally {
      setOrdering(false);
    }
  };

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
          ⚠️ 판단 없이 즉시 주문이 실행됩니다. 실거래 환경에서는 종목·수량·가격을 다시 확인하세요.
        </div>

        <div className="form-group">
          <label>종목 코드</label>
          <input
            type="text"
            value={ticker}
            onChange={e => setTicker(e.target.value.toUpperCase())}
            placeholder="예: QQQM"
          />
        </div>

        <div style={{ display: 'flex', gap: 10 }}>
          <div className="form-group" style={{ flex: 1 }}>
            <label>주문 유형</label>
            <select value={orderType} onChange={e => setOrderType(e.target.value)}>
              <option value="BUY">매수 (BUY)</option>
              <option value="SELL">매도 (SELL)</option>
            </select>
          </div>
          <div className="form-group" style={{ flex: 1 }}>
            <label>수량</label>
            <input
              type="number"
              min="1"
              value={qty}
              onChange={e => setQty(e.target.value)}
            />
          </div>
        </div>

        <div className="form-group">
          <label>가격 (USD, 비우면 현재가로 주문)</label>
          <input
            type="number"
            min="0"
            step="0.01"
            value={price}
            onChange={e => setPrice(e.target.value)}
            placeholder="현재가 사용"
          />
        </div>

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
