import { useState, useEffect, useCallback } from 'react';

/**
 * 시크릿 입력 필드. 저장 여부 배지를 보여주고, 값은 절대 화면에 표시하지 않습니다.
 * 빈 입력은 기존 값 유지(서버가 빈 값 미변경 처리).
 */
const SecretField = ({ label, set, onChange }) => (
  <div className="form-group" style={{ marginBottom: 0 }}>
    <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      {label}
      <span style={{
        fontSize: '0.68rem', padding: '1px 7px', borderRadius: 10,
        background: set ? 'var(--profit-green-bg)' : 'var(--loss-red-bg)',
        color: set ? 'var(--profit-green)' : 'var(--loss-red)'
      }}>
        {set ? '설정됨' : '미설정'}
      </span>
    </label>
    <input
      type="password"
      autoComplete="new-password"
      placeholder={set ? '변경하려면 새 값 입력' : '값 입력'}
      onChange={e => onChange(e.target.value)}
    />
  </div>
);

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

  // ── AI 모델 목록 (Gemini ListModels 조회 결과) ──
  const [geminiModels, setGeminiModels] = useState([]);
  const [modelsNote, setModelsNote] = useState(null);

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

  const fetchGeminiModels = useCallback(async () => {
    try {
      const res = await fetch('/api/config/gemini-models');
      if (!res.ok) throw new Error();
      const data = await res.json();
      setGeminiModels(data.models || []);
      setModelsNote(data.error || null);
    } catch {
      setModelsNote('모델 목록을 불러오지 못했습니다.');
    }
  }, []);

  useEffect(() => {
    fetchConfigs();
    fetchGeminiModels();
  }, [fetchConfigs, fetchGeminiModels]);

  const handleChange = (key, value) => {
    setConfigs(prev => ({ ...prev, [key]: value }));
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setMessage(null);
      // 읽기 전용 상태 플래그(*_SET)는 저장 대상에서 제외
      const payload = Object.fromEntries(
        Object.entries(configs).filter(([k]) => !k.endsWith('_SET'))
      );
      const res = await fetch('/api/config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
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

        {/* ── AI 분석 모델 ── */}
        <div className="card fade-in" style={{ animationDelay: '0.3s', opacity: 0 }}>
          <h2>AI 분석 모델</h2>
          {configs['AI_PROVIDER'] === 'gemini' ? (
            <>
              <div className="form-group" style={{ marginBottom: 0 }}>
                <label>Gemini 모델</label>
                <select
                  value={configs['GEMINI_MODEL'] || ''}
                  onChange={e => handleChange('GEMINI_MODEL', e.target.value)}
                >
                  {/* 현재 설정값이 목록에 없으면 직접 포함 */}
                  {configs['GEMINI_MODEL'] && !geminiModels.includes(configs['GEMINI_MODEL']) && (
                    <option value={configs['GEMINI_MODEL']}>{configs['GEMINI_MODEL']} (현재)</option>
                  )}
                  {geminiModels.map(m => (
                    <option key={m} value={m}>{m}</option>
                  ))}
                </select>
              </div>
              <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: 8 }}>
                {geminiModels.length > 0
                  ? `사용 가능한 모델 ${geminiModels.length}개. 변경 후 저장하면 다음 분석부터 적용됩니다.`
                  : (modelsNote || '모델 목록을 불러오는 중...')}
              </p>
            </>
          ) : (
            <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
              현재 AI 공급자가 <strong>Mock 모드</strong>입니다. 실제 모델 선택은 Gemini 모드에서만 가능합니다.
              (AI_PROVIDER 환경변수를 <code>gemini</code>로 설정하세요)
            </p>
          )}
        </div>

        {/* ── 증권사/AI API 키 ── */}
        <div className="card fade-in" style={{ animationDelay: '0.35s', opacity: 0, gridColumn: '1 / -1' }}>
          <h2>🔐 API 키 / 계좌 정보</h2>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: -4, marginBottom: 14 }}>
            저장 시 서버에서 암호화되어 보관됩니다. 보안상 저장된 값은 화면에 표시되지 않으며,
            <strong> 빈 칸으로 두면 기존 값이 유지</strong>됩니다.
          </p>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: 16 }}>
            <SecretField label="KIS App Key" set={configs['KIS_APP_KEY_SET'] === '1'}
              onChange={v => handleChange('KIS_APP_KEY', v)} />
            <SecretField label="KIS App Secret" set={configs['KIS_APP_SECRET_SET'] === '1'}
              onChange={v => handleChange('KIS_APP_SECRET', v)} />
            <SecretField label="KIS 계좌번호" set={configs['KIS_ACCOUNT_NO_SET'] === '1'}
              onChange={v => handleChange('KIS_ACCOUNT_NO', v)} />
            <SecretField label="Gemini API Key" set={configs['GEMINI_API_KEY_SET'] === '1'}
              onChange={v => handleChange('GEMINI_API_KEY', v)} />

            <div className="form-group" style={{ marginBottom: 0 }}>
              <label>KIS 서버</label>
              <select
                value={configs['KIS_SERVER'] || 'vps'}
                onChange={e => handleChange('KIS_SERVER', e.target.value)}
              >
                <option value="vps">모의투자 (vps)</option>
                <option value="prod">실전투자 (prod)</option>
              </select>
            </div>
          </div>
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
