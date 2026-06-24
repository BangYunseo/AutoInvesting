import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

/**
 * 로그인 / 최초 비밀번호 설정 페이지.
 * GET /api/auth/status로 setup 필요 여부를 판단하고,
 * 로그인 성공 시 발급받은 세션 토큰을 localStorage('auth_token')에 저장합니다.
 */
const Login = () => {
  const navigate = useNavigate();
  const [needsSetup, setNeedsSetup] = useState(false);
  const [checking, setChecking] = useState(true);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(null);
  const [notice, setNotice] = useState(null);

  useEffect(() => {
    (async () => {
      try {
        const res = await fetch('/api/auth/status');
        const data = await res.json();
        setNeedsSetup(!!data.needsSetup);
      } catch {
        setError('서버 상태를 확인하지 못했습니다.');
      } finally {
        setChecking(false);
      }
    })();
  }, []);

  const submit = async (e) => {
    e.preventDefault();
    setError(null);
    setNotice(null);

    if (!username.trim() || !password) {
      setError('아이디와 비밀번호를 입력하세요.');
      return;
    }

    setBusy(true);
    try {
      const endpoint = needsSetup ? '/api/auth/setup' : '/api/auth/login';
      const res = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: username.trim(), password })
      });
      const data = await res.json().catch(() => ({}));

      if (!res.ok) {
        setError(data.error || `요청 실패 (${res.status})`);
        return;
      }

      if (needsSetup) {
        // 설정 완료 → 로그인 모드로 전환
        setNeedsSetup(false);
        setPassword('');
        setNotice('✅ 관리자 계정이 설정되었습니다. 로그인하세요.');
        return;
      }

      // 로그인 성공
      localStorage.setItem('auth_token', data.token);
      navigate('/', { replace: true });
    } catch (err) {
      setError(err.message || '네트워크 오류');
    } finally {
      setBusy(false);
    }
  };

  if (checking) {
    return (
      <div className="loading-container fade-in">
        <div className="loading-spinner" />
        <span className="loading-text">확인 중...</span>
      </div>
    );
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20 }}>
      <div className="card fade-in" style={{ width: '100%', maxWidth: 380 }}>
        <div style={{ textAlign: 'center', marginBottom: 20 }}>
          <div className="app-nav__brand-icon" style={{ margin: '0 auto 12px' }}>AI</div>
          <h2 style={{ fontSize: '1.15rem' }}>{needsSetup ? '관리자 계정 설정' : 'AutoInvesting 로그인'}</h2>
          <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: 6 }}>
            {needsSetup
              ? '최초 1회 관리자 아이디와 비밀번호를 설정하세요.'
              : '계정으로 로그인하면 설정한 키로 자동매매가 동작합니다.'}
          </p>
        </div>

        {notice && (
          <div style={{
            padding: '10px 14px', borderRadius: 'var(--radius-sm)',
            background: 'var(--profit-green-bg)', color: 'var(--profit-green)',
            fontSize: '0.82rem', marginBottom: 14
          }}>{notice}</div>
        )}
        {error && (
          <div style={{
            padding: '10px 14px', borderRadius: 'var(--radius-sm)',
            background: 'var(--loss-red-bg)', color: 'var(--loss-red)',
            fontSize: '0.82rem', marginBottom: 14
          }}>❌ {error}</div>
        )}

        <form onSubmit={submit}>
          <div className="form-group">
            <label>아이디</label>
            <input
              type="text"
              autoComplete="username"
              value={username}
              onChange={e => setUsername(e.target.value)}
              placeholder="관리자 아이디"
            />
          </div>
          <div className="form-group">
            <label>비밀번호{needsSetup ? ' (8자 이상)' : ''}</label>
            <input
              type="password"
              autoComplete={needsSetup ? 'new-password' : 'current-password'}
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="비밀번호"
            />
          </div>
          <button
            type="submit"
            className="btn btn--primary"
            disabled={busy}
            style={{ width: '100%', padding: '12px', marginTop: 6, fontSize: '0.95rem' }}
          >
            {busy ? '처리 중...' : (needsSetup ? '계정 설정' : '로그인')}
          </button>
        </form>
      </div>
    </div>
  );
};

export default Login;
