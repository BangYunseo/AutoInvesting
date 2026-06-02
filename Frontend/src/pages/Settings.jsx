import { useState, useEffect, useCallback } from 'react';

/**
 * 시스템 설정 페이지.
 * ConfigController와 연동하여 운영 설정값을 조회하고 변경합니다.
 */
const Settings = () => {
  const [configs, setConfigs] = useState({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);
  const [error, setError] = useState(null);

  const fetchConfigs = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch('/api/config');
      if (!res.ok) throw new Error(`서버 오류 (${res.status})`);
      const data = await res.json();
      setConfigs(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchConfigs();
  }, [fetchConfigs]);

  const handleChange = (key, value) => {
    setConfigs(prev => ({ ...prev, [key]: value }));
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setMessage(null);
      const res = await fetch('/api/config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(configs)
      });
      if (!res.ok) throw new Error(`저장 실패 (${res.status})`);
      setMessage('✅ 설정이 저장되었습니다.');
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage(`❌ ${err.message}`);
    } finally {
      setSaving(false);
    }
  };

  const isPaperTrading = configs['IS_PAPER_TRADING'] === '1';

  if (loading) {
    return (
      <div className="loading-container fade-in">
        <div className="loading-spinner" />
        <span className="loading-text">설정을 불러오는 중...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="error-container fade-in">
        <div className="error-icon">⚠️</div>
        <p className="error-text">{error}</p>
        <button className="btn btn--primary" onClick={fetchConfigs}>다시 시도</button>
      </div>
    );
  }

  return (
    <div>
      <div className="section-header fade-in" style={{ marginBottom: 20 }}>
        <h2 style={{ fontSize: '1.2rem' }}>⚙️ 시스템 설정</h2>
      </div>

      {message && (
        <div className="fade-in" style={{
          padding: '10px 14px',
          borderRadius: 'var(--radius-sm)',
          background: message.startsWith('✅') ? 'var(--profit-green-bg)' : 'var(--loss-red-bg)',
          color: message.startsWith('✅') ? 'var(--profit-green)' : 'var(--loss-red)',
          fontSize: '0.85rem',
          marginBottom: 16
        }}>
          {message}
        </div>
      )}

      <div className="settings-grid">
        {/* ── 거래 모드 ── */}
        <div className="card fade-in fade-in-delay-1">
          <h2>거래 모드</h2>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <div>
              <div style={{ fontSize: '0.95rem', fontWeight: 600, color: 'var(--text-primary)', marginBottom: 4 }}>
                {isPaperTrading ? '🧪 모의투자 모드' : '🔴 실전투자 모드'}
              </div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                {isPaperTrading
                  ? 'SimBrokerClient를 사용하여 가상 매매를 실행합니다.'
                  : 'KisBrokerClient를 통해 실제 주문이 체결됩니다.'}
              </div>
            </div>
            <button
              className="toggle-switch"
              role="switch"
              aria-checked={isPaperTrading}
              onClick={() => handleChange('IS_PAPER_TRADING', isPaperTrading ? '0' : '1')}
            >
              <div className={`toggle-switch__thumb ${isPaperTrading ? 'toggle-switch__thumb--on' : ''}`} />
            </button>
          </div>

          {!isPaperTrading && (
            <div style={{
              marginTop: 12,
              padding: '10px 14px',
              background: 'rgba(239, 68, 68, 0.08)',
              border: '1px solid rgba(239, 68, 68, 0.2)',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.8rem',
              color: 'var(--loss-red)'
            }}>
              ⚠️ 실전 모드에서는 실제 증권 계좌에서 주문이 체결됩니다. 주의하세요.
            </div>
          )}
        </div>

        {/* ── 활성 전략 ── */}
        <div className="card fade-in fade-in-delay-2">
          <h2>활성 전략</h2>
          <div className="form-group" style={{ marginBottom: 0 }}>
            <label>활성 전략명</label>
            <input
              type="text"
              value={configs['ACTIVE_STRATEGY'] || ''}
              onChange={e => handleChange('ACTIVE_STRATEGY', e.target.value)}
              placeholder="예: 사용자정의"
            />
          </div>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: 8 }}>
            TradingBackgroundService가 이 전략의 종목에 대해 자동 매매를 실행합니다.
          </p>
        </div>

        {/* ── 투자금액 ── */}
        <div className="card fade-in fade-in-delay-3">
          <h2>투자 금액</h2>
          <div className="form-group" style={{ marginBottom: 0 }}>
            <label>1회 투자금액 (KRW)</label>
            <input
              type="number"
              min="0"
              step="100000"
              value={configs['INVEST_AMOUNT_KRW'] || ''}
              onChange={e => handleChange('INVEST_AMOUNT_KRW', e.target.value)}
            />
          </div>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: 8 }}>
            스마트 주문 시 종목별 배분에 사용됩니다.
          </p>
        </div>

        {/* ── 자동 주문 시각 ── */}
        <div className="card fade-in fade-in-delay-4">
          <h2>자동 주문 스케줄</h2>
          <div className="form-group" style={{ marginBottom: 0 }}>
            <label>자동 주문 시각 (HH:mm)</label>
            <input
              type="time"
              value={configs['ORDER_SCHEDULE'] || '22:30'}
              onChange={e => handleChange('ORDER_SCHEDULE', e.target.value)}
            />
          </div>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: 8 }}>
            KST 기준. 미국 시장 개장 시간(23:30 KST / 서머타임 22:30)에 맞춰 설정하세요.
          </p>
        </div>

        {/* ── 리밸런싱 임계값 ── */}
        <div className="card fade-in" style={{ animationDelay: '0.25s', opacity: 0 }}>
          <h2>리밸런싱 설정</h2>
          <div className="form-group" style={{ marginBottom: 0 }}>
            <label>리밸런싱 임계값 (편차 비율)</label>
            <input
              type="number"
              step="0.01"
              min="0"
              max="1"
              value={configs['REBALANCE_THRESHOLD'] || '0.05'}
              onChange={e => handleChange('REBALANCE_THRESHOLD', e.target.value)}
            />
          </div>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: 8 }}>
            보유 비중이 목표 대비 이 값 이상 벗어나면 자동 재조정합니다. (예: 0.05 = 5%)
          </p>
        </div>
      </div>

      {/* ── 저장 버튼 ── */}
      <div style={{ marginTop: 24, display: 'flex', justifyContent: 'flex-end' }}>
        <button className="btn btn--primary" onClick={handleSave} disabled={saving} style={{ padding: '12px 32px', fontSize: '0.95rem' }}>
          {saving ? '저장 중...' : '💾 설정 저장'}
        </button>
      </div>
    </div>
  );
};

export default Settings;
