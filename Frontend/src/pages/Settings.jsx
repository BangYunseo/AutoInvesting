import { useState, useEffect, useCallback } from 'react';

/** 눈 아이콘 (값 보기) */
const EyeIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
    <circle cx="12" cy="12" r="3" />
  </svg>
);

/** 눈에 사선 아이콘 (값 숨기기) */
const EyeOffIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
    <line x1="1" y1="1" x2="23" y2="23" />
  </svg>
);

/** 설정됨/미설정 배지 */
const SetBadge = ({ set }) => (
  <span style={{
    fontSize: '0.68rem', padding: '1px 7px', borderRadius: 10,
    background: set ? 'var(--profit-green-bg)' : 'var(--loss-red-bg)',
    color: set ? 'var(--profit-green)' : 'var(--loss-red)'
  }}>
    {set ? '설정됨' : '미설정'}
  </span>
);

// 모달에서 보기/변경할 시크릿 정의 (KIS 인증 정보 — AI 판단 레이어 제거로 Gemini 키는 더 이상 사용하지 않음)
const SECRET_DEFS = [
  { key: 'KIS_APP_KEY', label: 'KIS App Key' },
  { key: 'KIS_APP_SECRET', label: 'KIS App Secret' },
  { key: 'KIS_ACCOUNT_NO', label: 'KIS 계좌번호' },
];

const iconBtnStyle = {
  display: 'flex', alignItems: 'center', justifyContent: 'center',
  width: 38, height: 38, flexShrink: 0,
  background: 'var(--bg-card)', border: '1px solid var(--border-primary)',
  borderRadius: 'var(--radius-sm)', color: 'var(--text-secondary)', cursor: 'pointer'
};

/**
 * API 키 / 계좌 정보 관리 모달.
 * 저장된 시크릿 값을 눈 아이콘으로 보기/숨기기(서버에서 복호화 조회)하고, 새 값으로 변경합니다.
 */
const SecretManagerModal = ({ open, onClose, configs, onSaved }) => {
  const [edits, setEdits] = useState({});            // { KEY: 새 값 }
  const [revealed, setRevealed] = useState({});       // { KEY: bool }
  const [revealedValues, setRevealedValues] = useState({}); // { KEY: 복호화 평문 }
  const [loadingKey, setLoadingKey] = useState(null);
  const [server, setServer] = useState(configs['KIS_SERVER'] || 'vps');
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState(null);

  // 모달이 "열릴 때만" 임시 상태 초기화 (저장 후 configs 갱신으로 메시지가 지워지지 않도록
  // configs는 의존성에서 제외 — 열리는 시점의 최신 값을 그대로 사용).
  useEffect(() => {
    if (open) {
      setEdits({});
      setRevealed({});
      setRevealedValues({});
      setLoadingKey(null);
      setServer(configs['KIS_SERVER'] || 'vps');
      setMsg(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  if (!open) return null;

  const toggleReveal = async (key) => {
    // 이미 보이는 상태면 숨김
    if (revealed[key]) {
      setRevealed(prev => ({ ...prev, [key]: false }));
      return;
    }
    // 값을 아직 안 받았으면 서버에서 복호화 값을 조회
    if (revealedValues[key] === undefined) {
      try {
        setLoadingKey(key);
        const res = await fetch(`/api/config/secret/${key}`);
        if (!res.ok) throw new Error(`조회 실패 (${res.status})`);
        const data = await res.json();
        setRevealedValues(prev => ({ ...prev, [key]: data.value || '' }));
      } catch (err) {
        setMsg(`❌ ${err.message}`);
        return;
      } finally {
        setLoadingKey(null);
      }
    }
    setRevealed(prev => ({ ...prev, [key]: true }));
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setMsg(null);
      // 변경 입력이 있는 시크릿만 포함(빈 값은 서버가 미변경 처리) + KIS 서버
      const payload = { KIS_SERVER: server };
      for (const { key } of SECRET_DEFS) {
        if (edits[key] && edits[key].trim() !== '') payload[key] = edits[key];
      }
      const res = await fetch('/api/config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      if (!res.ok) throw new Error(`저장 실패 (${res.status})`);
      setMsg('✅ 저장되었습니다.');
      // 갱신된 설정 여부/값을 다시 받도록 부모에 알리고, 본 값 캐시 초기화
      setEdits({});
      setRevealed({});
      setRevealedValues({});
      onSaved?.();
    } catch (err) {
      setMsg(`❌ ${err.message}`);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={e => e.stopPropagation()} style={{ maxWidth: 540 }}>
        <h3 style={{ marginBottom: 8, borderBottom: '1px solid var(--border-primary)', paddingBottom: 12 }}>
          🔐 API 키 / 계좌 정보
        </h3>
        <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', margin: '0 0 18px' }}>
          저장된 값은 서버에서 암호화되어 보관됩니다. 눈 아이콘으로 입력한 값이 맞는지 확인하고,
          <strong> 변경하려면 새 값을 입력 후 저장</strong>하세요. (빈 칸은 기존 값 유지)
        </p>

        {msg && (
          <div style={{
            padding: '8px 12px', borderRadius: 'var(--radius-sm)', marginBottom: 14, fontSize: '0.82rem',
            background: msg.startsWith('✅') ? 'var(--profit-green-bg)' : 'var(--loss-red-bg)',
            color: msg.startsWith('✅') ? 'var(--profit-green)' : 'var(--loss-red)'
          }}>
            {msg}
          </div>
        )}

        {SECRET_DEFS.map(({ key, label }) => {
          const isSet = configs[`${key}_SET`] === '1';
          const isRevealed = !!revealed[key];
          const shownValue = isRevealed ? (revealedValues[key] ?? '') : (isSet ? '••••••••••••' : '미설정');
          return (
            <div className="form-group" key={key} style={{ marginBottom: 16 }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                {label} <SetBadge set={isSet} />
              </label>
              {/* 저장된 값 보기 */}
              <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <input
                  type="text"
                  readOnly
                  value={shownValue}
                  style={{
                    flex: 1, fontFamily: 'monospace', fontSize: '0.85rem',
                    color: isRevealed ? 'var(--text-primary)' : 'var(--text-muted)'
                  }}
                />
                <button
                  type="button"
                  style={{ ...iconBtnStyle, opacity: isSet ? 1 : 0.45, cursor: isSet ? 'pointer' : 'not-allowed' }}
                  disabled={!isSet || loadingKey === key}
                  onClick={() => toggleReveal(key)}
                  title={isRevealed ? '숨기기' : '저장된 값 보기'}
                  aria-label={isRevealed ? '숨기기' : '저장된 값 보기'}
                >
                  {loadingKey === key ? '…' : isRevealed ? <EyeOffIcon /> : <EyeIcon />}
                </button>
              </div>
              {/* 변경 입력 */}
              <input
                type="password"
                autoComplete="new-password"
                placeholder={isSet ? '변경하려면 새 값 입력' : '값 입력'}
                value={edits[key] || ''}
                onChange={e => setEdits(prev => ({ ...prev, [key]: e.target.value }))}
                style={{ marginTop: 6 }}
              />
            </div>
          );
        })}

        {/* KIS 서버 (계좌 환경) */}
        <div className="form-group" style={{ marginBottom: 20 }}>
          <label>KIS 서버</label>
          <select value={server} onChange={e => setServer(e.target.value)}>
            <option value="vps">모의투자 (vps)</option>
            <option value="prod">실전투자 (prod)</option>
          </select>
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
          <button className="btn btn--outline" onClick={onClose}>닫기</button>
          <button className="btn btn--primary" onClick={handleSave} disabled={saving}>
            {saving ? '저장 중...' : '💾 저장'}
          </button>
        </div>
      </div>
    </div>
  );
};

/**
 * 설정 페이지.
 * ConfigController와 연동하여 운영에 필요한 설정값(거래 모드, KIS 인증 정보)을 조회하고 변경합니다.
 * 판단 레이어(퀀트/AI/리밸런싱) 제거(Phase 6)에 따라 관련 설정은 더 이상 노출하지 않습니다.
 */
const Settings = () => {
  const [configs, setConfigs] = useState({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);
  const [error, setError] = useState(null);
  const [secretModalOpen, setSecretModalOpen] = useState(false);

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
      // 읽기 전용 상태 플래그(*_SET)와 시크릿 키는 저장 대상에서 제외
      //  (시크릿/계좌 정보는 전용 모달에서 관리)
      const secretKeys = new Set(['KIS_APP_KEY', 'KIS_APP_SECRET', 'KIS_ACCOUNT_NO', 'KIS_SERVER']);
      const payload = Object.fromEntries(
        Object.entries(configs).filter(([k]) => !k.endsWith('_SET') && !secretKeys.has(k))
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
        <h2 style={{ fontSize: '1.2rem' }}>⚙️ 설정</h2>
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

        {/* ── 증권사 API 키 / 계좌 정보 (전용 모달에서 관리) ── */}
        <div className="card fade-in fade-in-delay-2" style={{ gridColumn: '1 / -1' }}>
          <h2>🔐 API 키 / 계좌 정보</h2>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: -4, marginBottom: 14 }}>
            보안을 위해 키 값은 이 화면에 표시하지 않습니다. 아래 버튼을 눌러 별도 창에서
            저장된 값을 확인하거나 변경하세요.
          </p>
          <div style={{ display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
            <button className="btn btn--outline" onClick={() => setSecretModalOpen(true)}>
              🔐 API 키 / 계좌 정보 관리
            </button>
            <div style={{ display: 'flex', gap: 14, flexWrap: 'wrap', alignItems: 'center' }}>
              {SECRET_DEFS.map(({ key, label }) => (
                <span key={key} style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                  {label} <SetBadge set={configs[`${key}_SET`] === '1'} />
                </span>
              ))}
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

      <SecretManagerModal
        open={secretModalOpen}
        onClose={() => setSecretModalOpen(false)}
        configs={configs}
        onSaved={fetchConfigs}
      />
    </div>
  );
};

export default Settings;
