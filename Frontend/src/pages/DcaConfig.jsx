import { useEffect, useState } from 'react';

/**
 * 적립 설정 페이지.
 * DcaController(/api/dca/config)와 연동하여 목표비중과 월 예산을 편집합니다.
 * 화면에서는 비중을 100% 기준(예: 40%)으로 다루고, 저장 시 분수(0.4)로 변환합니다.
 * 백엔드는 비중을 합계 1 기준 분수로 사용하므로 변환을 거칩니다.
 * 저장값은 DB에 기록되어 다음 적립 사이클부터 반영됩니다.
 */
const DcaConfig = () => {
  const [budget, setBudget] = useState('');
  const [rows, setRows] = useState([]); // [{ ticker, weight }] — weight는 % 단위
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [notice, setNotice] = useState(null);

  // ── 비중 포맷 (소수점 노이즈 제거) ──
  const fmtPct = (n) => String(Math.round(n * 100) / 100);

  // ── ETF 개수만큼 100%를 정수로 균등 분배 (나머지는 앞 종목에 +1) ──
  const distributeEven = (count) => {
    if (count <= 0) return [];
    const base = Math.floor(100 / count);
    const remainder = 100 - base * count;
    return Array.from({ length: count }, (_, i) => String(base + (i < remainder ? 1 : 0)));
  };

  const loadConfig = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch('/api/dca/config');
      if (!res.ok) throw new Error(`설정 조회 실패 (${res.status})`);
      const data = await res.json();
      setBudget(String(data.budgetKrw ?? ''));
      const t = data.targets || {};
      // 저장된 분수(0.4)를 화면용 %(40)로 변환
      setRows(Object.keys(t).map(k => ({ ticker: k, weight: fmtPct(Number(t[k]) * 100) })));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadConfig(); }, []);

  const budgetNum = Number(budget) || 0;
  const totalWeight = rows.reduce((sum, r) => sum + (Number(r.weight) || 0), 0); // % 합계

  const updateRow = (idx, field, value) => {
    // 비중은 합계 100%를 넘을 수 없도록 입력 단계에서 제한 (초과 시에만 클램프)
    if (field === 'weight') {
      const otherSum = rows.reduce((s, r, i) => (i === idx ? s : s + (Number(r.weight) || 0)), 0);
      const maxAllowed = Math.max(0, 100 - otherSum);
      let v = value;
      if (value !== '') {
        const num = Number(value);
        if (!Number.isNaN(num) && num > maxAllowed) v = fmtPct(maxAllowed);
        else if (!Number.isNaN(num) && num < 0) v = '0';
      }
      setRows(rows.map((r, i) => (i === idx ? { ...r, weight: v } : r)));
      return;
    }
    setRows(rows.map((r, i) => (i === idx ? { ...r, [field]: value } : r)));
  };

  const addRow = () => setRows([...rows, { ticker: '', weight: '' }]);
  const removeRow = (idx) => setRows(rows.filter((_, i) => i !== idx));

  // ── ETF 개수 입력 → 행 개수 조정 + 100% 균등 분배 ──
  const setEtfCount = (raw) => {
    const count = Math.max(0, Math.min(20, parseInt(raw, 10) || 0));
    const weights = distributeEven(count);
    setRows(prev => Array.from({ length: count }, (_, i) => ({
      ticker: prev[i]?.ticker ?? '',
      weight: weights[i],
    })));
  };

  // ── 현재 종목들에 100% 균등 재분배 (개수 유지) ──
  const equalizeWeights = () => {
    const weights = distributeEven(rows.length);
    setRows(rows.map((r, i) => ({ ...r, weight: weights[i] })));
  };

  const handleSave = async () => {
    setError(null);
    setNotice(null);

    const targets = {};
    for (const r of rows) {
      const ticker = r.ticker.trim().toUpperCase();
      const pct = Number(r.weight);
      if (!ticker) continue;
      if (!(pct > 0)) {
        setError(`'${ticker || '(빈 종목)'}'의 비중은 0보다 커야 합니다.`);
        return;
      }
      if (targets[ticker]) {
        setError(`중복된 종목이 있습니다: ${ticker}`);
        return;
      }
      // 화면 %(40) → 저장 분수(0.4)
      targets[ticker] = pct / 100;
    }

    if (Object.keys(targets).length === 0) {
      setError('목표비중을 최소 1개 이상 입력하세요.');
      return;
    }
    if (totalWeight > 100.001) {
      setError(`비중 합계가 100%를 초과했습니다 (${fmtPct(totalWeight)}%). 100% 이하로 맞춰주세요.`);
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
        body: JSON.stringify({ budgetKrw: budgetNum, targets }),
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

  const totalIs100 = Math.abs(totalWeight - 100) < 0.01;

  return (
    <div className="card fade-in fade-in-delay-1" style={{ maxWidth: 720, margin: '0 auto' }}>
      <h2>적립 설정</h2>
      <p style={{ color: 'var(--text-secondary)', fontSize: '0.88rem', lineHeight: 1.6, marginBottom: 24, wordBreak: 'keep-all' }}>
        매 사이클에 투입할 <strong>월 예산</strong>과 종목별 <strong>목표비중(%)</strong>을 설정합니다.
        타이밍 판단 없이, 설정한 비중을 향해 예산만큼 정수 단위로 매수합니다.
        <strong>ETF 개수</strong>를 입력하면 100%를 균등 분배하며, 각 종목이 예산에서
        차지하는 비중과 금액을 자동으로 보여줍니다. 비중 합계는 <strong>100%를 넘을 수 없으며</strong>,
        투자 규모를 키우려면 비중이 아니라 <strong>월 예산</strong>을 조정하세요.
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

      {/* ETF 개수 */}
      <div className="form-group">
        <label>ETF 개수 (입력 시 100% 균등 분배)</label>
        <input
          type="number"
          min="0"
          max="20"
          step="1"
          value={rows.length}
          onChange={e => setEtfCount(e.target.value)}
          placeholder="예: 3"
        />
      </div>

      {/* 목표비중 목록 */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <label style={{ fontWeight: 600, fontSize: '0.9rem' }}>목표비중 (%)</label>
        {rows.length > 0 && (
          <button className="btn btn--outline" onClick={equalizeWeights} style={{ padding: '6px 12px', fontSize: '0.8rem' }}>
            균등 분배
          </button>
        )}
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 12 }}>
        {rows.map((row, idx) => {
          const pct = Number(row.weight) || 0;
          // 예산 기준 실제 비중·금액 (입력 합계로 정규화)
          const sharePct = totalWeight > 0 ? (pct / totalWeight) * 100 : 0;
          const amountKrw = totalWeight > 0 ? Math.round(budgetNum * (pct / totalWeight)) : 0;
          return (
            <div key={idx} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <input
                  type="text"
                  className="input-field"
                  value={row.ticker}
                  onChange={e => updateRow(idx, 'ticker', e.target.value.toUpperCase())}
                  placeholder="종목 (예: QQQM)"
                  style={{ flex: 2 }}
                />
                <div style={{ flex: 1, display: 'flex', alignItems: 'center', gap: 4 }}>
                  <input
                    type="number"
                    className="input-field"
                    min="0"
                    step="1"
                    value={row.weight}
                    onChange={e => updateRow(idx, 'weight', e.target.value)}
                    placeholder="예: 40"
                    style={{ width: '100%' }}
                  />
                  <span style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>%</span>
                </div>
                <button
                  className="btn btn--outline"
                  onClick={() => removeRow(idx)}
                  style={{ padding: '8px 12px' }}
                  title="삭제"
                >
                  ✕
                </button>
              </div>
              {/* 예산 기준 실제 비중·금액 */}
              <div style={{ color: 'var(--text-muted)', fontSize: '0.78rem', paddingLeft: 2 }}>
                {pct > 0 && totalWeight > 0
                  ? `실제 비중 ${sharePct.toFixed(1)}%${budgetNum > 0 ? ` · 약 ₩${amountKrw.toLocaleString('ko-KR')}` : ''}`
                  : '비중을 입력하세요'}
              </div>
            </div>
          );
        })}
        {rows.length === 0 && (
          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>등록된 종목이 없습니다. 위에 ETF 개수를 입력하거나 아래에서 추가하세요.</p>
        )}
      </div>

      <button className="btn btn--outline" onClick={addRow} style={{ marginBottom: 20 }}>
        + 종목 추가
      </button>

      {/* 합계 안내 */}
      <div style={{
        padding: '10px 14px',
        background: 'rgba(255,255,255,0.03)',
        borderRadius: 'var(--radius-sm)',
        fontSize: '0.85rem',
        marginBottom: 20,
        color: totalIs100 ? 'var(--profit-green)' : 'var(--text-secondary)'
      }}>
        비중 합계: <strong>{fmtPct(totalWeight)}%</strong> / 100%
        {!totalIs100 && ` · 남은 배분 가능: ${fmtPct(Math.max(0, 100 - totalWeight))}%`}
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
