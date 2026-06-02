import { useState, useEffect, useCallback } from 'react';

const STRATEGY_TYPES = ['MEAN_REVERSION', 'MOMENTUM', 'MIXED'];

/**
 * 투자 전략 관리 페이지.
 * StrategyController와 연동하여 종목별 수량, 전략 유형을 CRUD합니다.
 */
const Strategy = () => {
  const [strategyName, setStrategyName] = useState('사용자정의');
  const [strategies, setStrategies] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [message, setMessage] = useState(null);

  // ── 새 종목 추가 폼 상태 ──
  const [newTicker, setNewTicker] = useState('');
  const [newQty, setNewQty] = useState(1);
  const [newType, setNewType] = useState('MEAN_REVERSION');

  const fetchStrategies = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch(`/api/strategy/${encodeURIComponent(strategyName)}`);
      if (!res.ok) throw new Error(`서버 오류 (${res.status})`);
      const data = await res.json();
      setStrategies(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [strategyName]);

  useEffect(() => {
    fetchStrategies();
  }, [fetchStrategies]);

  const handleAdd = () => {
    if (!newTicker.trim()) return;
    if (strategies.some(s => s.ticker === newTicker.toUpperCase())) {
      setMessage('이미 추가된 종목입니다.');
      setTimeout(() => setMessage(null), 2000);
      return;
    }
    setStrategies(prev => [
      ...prev,
      {
        strategyId: 0,
        strategyName,
        ticker: newTicker.toUpperCase(),
        qty: Number(newQty),
        strategyType: newType
      }
    ]);
    setNewTicker('');
    setNewQty(1);
  };

  const handleRemove = (ticker) => {
    setStrategies(prev => prev.filter(s => s.ticker !== ticker));
  };

  const handleFieldChange = (ticker, field, value) => {
    setStrategies(prev =>
      prev.map(s =>
        s.ticker === ticker ? { ...s, [field]: field === 'qty' ? Number(value) : value } : s
      )
    );
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setMessage(null);
      const res = await fetch(`/api/strategy/${encodeURIComponent(strategyName)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(strategies)
      });
      if (!res.ok) throw new Error(`저장 실패 (${res.status})`);
      setMessage('✅ 전략이 성공적으로 저장되었습니다.');
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage(`❌ ${err.message}`);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm(`'${strategyName}' 전략을 삭제하시겠습니까?`)) return;
    try {
      const res = await fetch(`/api/strategy/${encodeURIComponent(strategyName)}`, {
        method: 'DELETE'
      });
      if (!res.ok) throw new Error(`삭제 실패 (${res.status})`);
      setStrategies([]);
      setMessage('🗑️ 전략이 삭제되었습니다.');
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage(`❌ ${err.message}`);
    }
  };

  return (
    <div>
      {/* ── 전략명 헤더 ── */}
      <div className="card fade-in">
        <div className="section-header">
          <h2>투자 전략 관리</h2>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <input
              type="text"
              value={strategyName}
              onChange={e => setStrategyName(e.target.value)}
              style={{
                background: 'var(--bg-input)',
                border: '1px solid var(--border-primary)',
                borderRadius: 'var(--radius-sm)',
                color: 'var(--text-primary)',
                padding: '8px 12px',
                fontSize: '0.85rem',
                width: 160
              }}
              placeholder="전략명"
            />
            <button className="btn btn--outline" onClick={fetchStrategies} disabled={loading}>
              불러오기
            </button>
            <button className="btn btn--danger" onClick={handleDelete} style={{ fontSize: '0.8rem', padding: '8px 12px' }}>
              삭제
            </button>
          </div>
        </div>

        {message && (
          <div style={{
            padding: '10px 14px',
            borderRadius: 'var(--radius-sm)',
            background: message.startsWith('✅') || message.startsWith('🗑️')
              ? 'var(--profit-green-bg)'
              : 'var(--loss-red-bg)',
            color: message.startsWith('✅') || message.startsWith('🗑️')
              ? 'var(--profit-green)'
              : 'var(--loss-red)',
            fontSize: '0.85rem',
            marginBottom: 16
          }}>
            {message}
          </div>
        )}

        {/* ── 로딩 / 에러 ── */}
        {loading && (
          <div className="loading-container" style={{ padding: 40 }}>
            <div className="loading-spinner" />
          </div>
        )}
        {error && !loading && (
          <div className="error-container" style={{ padding: 40 }}>
            <p className="error-text">{error}</p>
          </div>
        )}

        {/* ── 종목 테이블 ── */}
        {!loading && !error && (
          <>
            {strategies.length === 0 ? (
              <div className="empty-state">
                <div className="empty-state__icon">📭</div>
                <p className="empty-state__text">등록된 종목이 없습니다. 아래에서 추가하세요.</p>
              </div>
            ) : (
              <div className="data-table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>종목 코드</th>
                      <th>매수 수량</th>
                      <th>전략 유형</th>
                      <th style={{ width: 80 }}>관리</th>
                    </tr>
                  </thead>
                  <tbody>
                    {strategies.map((s, idx) => (
                      <tr key={s.ticker}>
                        <td>
                          <span className="ticker-badge">
                            <span className={`ticker-dot ticker-dot--${idx % 5}`} />
                            {s.ticker}
                          </span>
                        </td>
                        <td>
                          <input
                            type="number"
                            min="1"
                            value={s.qty}
                            onChange={e => handleFieldChange(s.ticker, 'qty', e.target.value)}
                            style={{
                              background: 'var(--bg-input)',
                              border: '1px solid var(--border-primary)',
                              borderRadius: 'var(--radius-sm)',
                              color: 'var(--text-primary)',
                              padding: '6px 10px',
                              fontSize: '0.85rem',
                              width: 80
                            }}
                          />
                        </td>
                        <td>
                          <select
                            value={s.strategyType}
                            onChange={e => handleFieldChange(s.ticker, 'strategyType', e.target.value)}
                            style={{
                              background: 'var(--bg-input)',
                              border: '1px solid var(--border-primary)',
                              borderRadius: 'var(--radius-sm)',
                              color: 'var(--text-primary)',
                              padding: '6px 10px',
                              fontSize: '0.85rem'
                            }}
                          >
                            {STRATEGY_TYPES.map(t => (
                              <option key={t} value={t}>{t}</option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <button
                            className="btn btn--danger"
                            style={{ fontSize: '0.75rem', padding: '4px 10px' }}
                            onClick={() => handleRemove(s.ticker)}
                          >
                            삭제
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {/* ── 새 종목 추가 ── */}
            <div style={{
              display: 'flex',
              gap: 10,
              alignItems: 'flex-end',
              marginTop: 20,
              paddingTop: 16,
              borderTop: '1px solid var(--border-primary)'
            }}>
              <div className="form-group" style={{ marginBottom: 0, flex: 1 }}>
                <label>종목 코드</label>
                <input
                  type="text"
                  value={newTicker}
                  onChange={e => setNewTicker(e.target.value.toUpperCase())}
                  placeholder="예: QQQM"
                />
              </div>
              <div className="form-group" style={{ marginBottom: 0, width: 100 }}>
                <label>수량</label>
                <input
                  type="number"
                  min="1"
                  value={newQty}
                  onChange={e => setNewQty(e.target.value)}
                />
              </div>
              <div className="form-group" style={{ marginBottom: 0, width: 180 }}>
                <label>전략 유형</label>
                <select value={newType} onChange={e => setNewType(e.target.value)}>
                  {STRATEGY_TYPES.map(t => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </select>
              </div>
              <button className="btn btn--outline" onClick={handleAdd} style={{ marginBottom: 0 }}>
                + 추가
              </button>
            </div>

            {/* ── 저장 버튼 ── */}
            <div style={{ marginTop: 20, display: 'flex', justifyContent: 'flex-end' }}>
              <button className="btn btn--primary" onClick={handleSave} disabled={saving}>
                {saving ? '저장 중...' : '💾 전략 저장'}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default Strategy;
