import { useEffect, useState } from 'react';

/**
 * 적립 설정 페이지.
 * DcaController(/api/dca/config)와 연동하여 목표비중과 월 예산을 편집합니다.
 * 저장값은 DB에 기록되어 다음 적립 사이클부터 반영됩니다.
 */
const DcaConfig = () => {
  const [budget, setBudget] = useState('');
  const [rows, setRows] = useState([]); // [{ ticker, weight }]
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [notice, setNotice] = useState(null);

  const loadConfig = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch('/api/dca/config');
      if (!res.ok) throw new Error(`설정 조회 실패 (${res.status})`);
      const data = await res.json();
      setBudget(String(data.budgetKrw ?? ''));
      const t = data.targets || {};
      setRows(Object.keys(t).map(k => ({ ticker: k, weight: String(t[k]) })));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadConfig(); }, []);

  const totalWeight = rows.reduce((sum, r) => sum + (Number(r.weight) || 0), 0);

  const updateRow = (idx, field, value) => {
    setRows(rows.map((r, i) => (i === idx ? { ...r, [field]: value } : r)));
  };

  const addRow = () => setRows([...rows, { ticker: '', weight: '' }]);
  const removeRow = (idx) => setRows(rows.filter((_, i) => i !== idx));

  const handleSave = async () => {
    setError(null);
    setNotice(null);

    const targets = {};
    for (const r of rows) {
      const ticker = r.ticker.trim().toUpperCase();
      const weight = Number(r.weight);
      if (!ticker) continue;
      if (!(weight > 0)) {
        setError(`'${ticker || '(빈 종목)'}'의 비중은 0보다 커야 합니다.`);
        return;
      }
      if (targets[ticker]) {
        setError(`중복된 종목이 있습니다: ${ticker}`);
        return;
      }
      targets[ticker] = weight;
    }

    if (Object.keys(targets).length === 0) {
      setError('목표비중을 최소 1개 이상 입력하세요.');
      return;
    }
    if (!(Number(budget) > 0)) {
      setError('예산은 0보다 커야 합니다.');
      return;
    }

    try {
      setSaving(true);
      const res = await fetch('/api/dca/config', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ budgetKrw: Number(budget), targets }),
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

  return (
    <div className="card fade-in fade-in-delay-1" style={{ maxWidth: 720, margin: '0 auto' }}>
      <h2>적립 설정</h2>
      <p style={{ color: 'var(--text-secondary)', fontSize: '0.88rem', lineHeight: 1.6, marginBottom: 24, wordBreak: 'keep-all' }}>
        매 사이클에 투입할 <strong>월 예산</strong>과 종목별 <strong>목표비중</strong>을 설정합니다.
        타이밍 판단 없이, 설정한 비중을 향해 예산만큼 정수 단위로 매수합니다.
        비중은 상대값으로 적용되므로 합계가 꼭 1이 아니어도 됩니다(합계 1 권장).
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

      {/* 목표비중 목록 */}
      <label style={{ display: 'block', fontWeight: 600, marginBottom: 8, fontSize: '0.9rem' }}>목표비중</label>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginBottom: 12 }}>
        {rows.map((row, idx) => (
          <div key={idx} style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <input
              type="text"
              className="input-field"
              value={row.ticker}
              onChange={e => updateRow(idx, 'ticker', e.target.value.toUpperCase())}
              placeholder="종목 (예: QQQM)"
              style={{ flex: 2 }}
            />
            <input
              type="number"
              className="input-field"
              min="0"
              step="0.05"
              value={row.weight}
              onChange={e => updateRow(idx, 'weight', e.target.value)}
              placeholder="비중 (예: 0.4)"
              style={{ flex: 1 }}
            />
            <button
              className="btn btn--outline"
              onClick={() => removeRow(idx)}
              style={{ padding: '8px 12px' }}
              title="삭제"
            >
              ✕
            </button>
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
        padding: '10px 14px',
        background: 'rgba(255,255,255,0.03)',
        borderRadius: 'var(--radius-sm)',
        fontSize: '0.85rem',
        marginBottom: 20,
        color: Math.abs(totalWeight - 1) < 0.001 ? 'var(--profit-green)' : 'var(--text-secondary)'
      }}>
        비중 합계: <strong>{totalWeight.toFixed(2)}</strong>
        {Math.abs(totalWeight - 1) >= 0.001 && ' (1.00 권장)'}
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
