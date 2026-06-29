import { useEffect, useState } from 'react';

/**
 * 적립 설정 페이지.
 * DcaController(/api/dca/config)와 연동하여 종목별 "고정 매수 수량"과 월 예산을 편집합니다.
 *
 * - 사람이 정하는 값: 종목(티커)과 매 사이클 매수 수량(주). 수량은 +/− 로 직접 조절.
 * - 자동 계산(읽기 전용): 매수금액(수량×현재가) · 비중(%) · 총 매수금액 · 비중 합계.
 * - 티커는 /api/price/{ticker}로 실시간 검증 — 현재가가 확인된 종목만 저장됩니다.
 * - 예산은 초과 경고용 상한일 뿐, 수량을 줄이지 않습니다.
 */
const DcaConfig = () => {
  const [budget, setBudget] = useState('');
  // rows: [{ ticker, qty, status: 'idle'|'checking'|'valid'|'invalid', price(USD), error }]
  const [rows, setRows] = useState([]);
  const [exchangeRate, setExchangeRate] = useState(0);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [notice, setNotice] = useState(null);

  const won = (n) => '₩' + Math.round(n || 0).toLocaleString('ko-KR');

  // ── 티커 검증 + 현재가 조회 ──
  const validateRow = async (idx, tickerRaw) => {
    const ticker = (tickerRaw ?? '').trim().toUpperCase();
    if (!ticker) {
      setRows(prev => prev.map((r, i) => (i === idx ? { ...r, status: 'idle', price: 0, error: null } : r)));
      return;
    }
    setRows(prev => prev.map((r, i) => (i === idx ? { ...r, ticker, status: 'checking', error: null } : r)));
    try {
      const res = await fetch(`/api/price/${encodeURIComponent(ticker)}`);
      if (res.status === 404) {
        setRows(prev => prev.map((r, i) => (i === idx ? { ...r, status: 'invalid', price: 0, error: '존재하지 않는 티커' } : r)));
        return;
      }
      if (!res.ok) throw new Error(`가격 조회 실패 (${res.status})`);
      const data = await res.json();
      if (data.exchangeRate > 0) setExchangeRate(data.exchangeRate);
      setRows(prev => prev.map((r, i) => (i === idx ? { ...r, ticker, status: 'valid', price: data.priceUsd, error: null } : r)));
    } catch (err) {
      setRows(prev => prev.map((r, i) => (i === idx ? { ...r, status: 'invalid', price: 0, error: err.message } : r)));
    }
  };

  const loadConfig = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch('/api/dca/config');
      if (!res.ok) throw new Error(`설정 조회 실패 (${res.status})`);
      const data = await res.json();
      setBudget(String(data.budgetKrw ?? ''));
      const q = data.quantities || {};
      const initRows = Object.keys(q).map(k => ({ ticker: k, qty: String(q[k]), status: 'idle', price: 0, error: null }));
      setRows(initRows);
      // 저장된 종목들의 현재가를 즉시 조회
      initRows.forEach((r, i) => validateRow(i, r.ticker));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadConfig(); }, []);

  // ── 행 조작 ──
  const setTicker = (idx, val) => {
    const t = val.toUpperCase();
    // 티커가 바뀌면 재검증 필요 → 상태 초기화
    setRows(rows.map((r, i) => (i === idx ? { ...r, ticker: t, status: 'idle', price: 0, error: null } : r)));
  };

  const setQty = (idx, val) => {
    if (val === '') {
      setRows(rows.map((r, i) => (i === idx ? { ...r, qty: '' } : r)));
      return;
    }
    let n = parseInt(val, 10);
    if (Number.isNaN(n)) return;
    if (n < 1) n = 1;
    setRows(rows.map((r, i) => (i === idx ? { ...r, qty: String(n) } : r)));
  };

  const stepQty = (idx, delta) => {
    setRows(rows.map((r, i) => {
      if (i !== idx) return r;
      const cur = parseInt(r.qty, 10) || 0;
      return { ...r, qty: String(Math.max(1, cur + delta)) };
    }));
  };

  const addRow = () => setRows([...rows, { ticker: '', qty: '1', status: 'idle', price: 0, error: null }]);
  const removeRow = (idx) => setRows(rows.filter((_, i) => i !== idx));

  // ── 계산 (읽기 전용) ──
  const budgetNum = Number(budget) || 0;
  const rowAmount = (r) => (r.status === 'valid' && r.price > 0 ? (parseInt(r.qty, 10) || 0) * r.price * exchangeRate : 0);
  const totalCost = rows.reduce((s, r) => s + rowAmount(r), 0);
  const overBudget = budgetNum > 0 && totalCost > budgetNum;

  const handleSave = async () => {
    setError(null);
    setNotice(null);

    const quantities = {};
    for (const r of rows) {
      const ticker = r.ticker.trim().toUpperCase();
      const qty = parseInt(r.qty, 10);
      if (!ticker) continue;
      if (r.status !== 'valid') {
        setError(`'${ticker}'은(는) 아직 검증되지 않았습니다. 엔터(또는 🔍)로 현재가를 확인한 종목만 저장됩니다.`);
        return;
      }
      if (!(qty > 0)) {
        setError(`'${ticker}'의 수량은 1 이상이어야 합니다.`);
        return;
      }
      if (quantities[ticker]) {
        setError(`중복된 종목이 있습니다: ${ticker}`);
        return;
      }
      quantities[ticker] = qty;
    }

    if (Object.keys(quantities).length === 0) {
      setError('수량을 지정한 유효 종목이 최소 1개 필요합니다.');
      return;
    }
    if (!(budgetNum > 0)) {
      setError('예산은 0보다 커야 합니다.');
      return;
    }

    try {
      setSaving(true);
      const res = await fetch('/api/dca/config', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ budgetKrw: budgetNum, quantities }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || `저장 실패 (${res.status})`);
      setNotice(data.message || '저장되었습니다.');
      await loadConfig();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="loading-container fade-in">
        <div className="loading-spinner" />
        <span className="loading-text">설정을 불러오는 중...</span>
      </div>
    );
  }

  // 행 하단 상태/금액 텍스트
  const statusLine = (r) => {
    if (r.status === 'checking') return <span style={{ color: 'var(--text-muted)' }}>검증 중…</span>;
    if (r.status === 'invalid') return <span style={{ color: 'var(--loss-red)' }}>✕ {r.error || '확인 불가'}</span>;
    if (r.status === 'valid') {
      const amount = rowAmount(r);
      const weight = budgetNum > 0 ? (amount / budgetNum) * 100 : 0; // 예산 대비 비중
      return (
        <span style={{ color: 'var(--text-secondary)' }}>
          <span style={{ color: 'var(--profit-green)' }}>✓ ${r.price.toFixed(2)}</span>
          {' · '}매수금액 {won(amount)}
          {' · '}비중 {weight.toFixed(1)}%
        </span>
      );
    }
    return <span style={{ color: 'var(--text-muted)' }}>엔터(또는 🔍)로 현재가를 확인하세요</span>;
  };

  return (
    <div className="card fade-in fade-in-delay-1" style={{ maxWidth: 720, margin: '0 auto' }}>
      <h2>적립 설정</h2>
      <p style={{ color: 'var(--text-secondary)', fontSize: '0.88rem', lineHeight: 1.6, marginBottom: 24, wordBreak: 'keep-all' }}>
        매 사이클에 매수할 <strong>종목과 수량(주)</strong>을 직접 지정합니다.
        타이밍 판단 없이, 설정한 수량을 그대로 매수합니다.
        <strong>비중(%)·매수금액</strong>은 수량 × 현재가로 자동 계산되어 표시만 됩니다(조절 불가).
        티커는 현재가가 확인된 종목만 저장되며, <strong>월 예산</strong>은 초과 시 경고용 상한입니다.
      </p>

      {error && (
        <div style={{ padding: '10px 14px', background: 'var(--loss-red-bg)', color: 'var(--loss-red)', borderRadius: 'var(--radius-sm)', fontSize: '0.85rem', marginBottom: 16 }}>
          ❌ {error}
        </div>
      )}
      {notice && (
        <div style={{ padding: '10px 14px', background: 'var(--profit-green-bg)', color: 'var(--profit-green)', borderRadius: 'var(--radius-sm)', fontSize: '0.85rem', marginBottom: 16 }}>
          ✅ {notice}
        </div>
      )}

      {/* 예산 */}
      <div className="form-group">
        <label>월 예산 (원)</label>
        <input
          type="number"
          min="0"
          step="10000"
          value={budget}
          onChange={e => setBudget(e.target.value)}
          placeholder="예: 1000000"
        />
      </div>

      {/* ETF 설정 */}
      <label style={{ display: 'block', fontWeight: 600, marginBottom: 8, fontSize: '0.9rem' }}>ETF 설정</label>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginBottom: 12 }}>
        {rows.map((row, idx) => (
          <div key={idx} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              {/* 티커 */}
              <input
                type="text"
                className="input-field"
                value={row.ticker}
                onChange={e => setTicker(idx, e.target.value)}
                onKeyDown={e => { if (e.key === 'Enter') validateRow(idx, row.ticker); }}
                onBlur={() => { if (row.ticker && row.status === 'idle') validateRow(idx, row.ticker); }}
                placeholder="티커 (예: QQQM)"
                style={{ flex: 2 }}
              />
              {/* 검증/새로고침 */}
              <button
                className="btn btn--outline"
                onClick={() => validateRow(idx, row.ticker)}
                style={{ padding: '8px 12px' }}
                title="현재가 확인"
              >
                🔍
              </button>
              {/* 수량 스테퍼 */}
              <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                <button className="btn btn--outline" onClick={() => stepQty(idx, -1)} style={{ padding: '8px 12px' }} title="감소">−</button>
                <input
                  type="number"
                  className="input-field"
                  min="1"
                  step="1"
                  value={row.qty}
                  onChange={e => setQty(idx, e.target.value)}
                  style={{ width: 64, textAlign: 'center' }}
                />
                <button className="btn btn--outline" onClick={() => stepQty(idx, 1)} style={{ padding: '8px 12px' }} title="증가">+</button>
                <span style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>주</span>
              </div>
              {/* 삭제 */}
              <button
                className="btn btn--outline"
                onClick={() => removeRow(idx)}
                style={{ padding: '8px 12px' }}
                title="삭제"
              >
                ✕
              </button>
            </div>
            {/* 상태 · 현재가 · 매수금액 · 비중 */}
            <div style={{ fontSize: '0.78rem', paddingLeft: 2 }}>{statusLine(row)}</div>
          </div>
        ))}
        {rows.length === 0 && (
          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>등록된 종목이 없습니다. 아래에서 추가하세요.</p>
        )}
      </div>

      <button className="btn btn--outline" onClick={addRow} style={{ marginBottom: 20 }}>
        + 종목 추가
      </button>

      {/* 합계 안내 */}
      <div style={{
        padding: '12px 14px',
        background: 'rgba(255,255,255,0.03)',
        borderRadius: 'var(--radius-sm)',
        fontSize: '0.85rem',
        marginBottom: 20,
        display: 'flex',
        flexDirection: 'column',
        gap: 6,
      }}>
        <div style={{ color: overBudget ? 'var(--loss-red)' : 'var(--text-secondary)' }}>
          총 매수금액: <strong>{won(totalCost)}</strong> / 예산 {won(budgetNum)}
          {overBudget && ` · ⚠ 예산 초과 (${won(totalCost - budgetNum)})`}
        </div>
        <div style={{ color: overBudget ? 'var(--loss-red)' : 'var(--text-secondary)' }}>
          비중 합계 (예산 대비): <strong>{(budgetNum > 0 ? (totalCost / budgetNum) * 100 : 0).toFixed(1)}%</strong>
        </div>
      </div>

      <button
        className="btn btn--primary"
        onClick={handleSave}
        disabled={saving}
        style={{ width: '100%', padding: '14px', fontSize: '1rem' }}
      >
        {saving ? '⏳ 저장 중...' : '💾 적립 설정 저장'}
      </button>
    </div>
  );
};

export default DcaConfig;
