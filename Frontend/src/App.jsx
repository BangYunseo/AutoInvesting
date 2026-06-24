import { useState } from 'react';
import { BrowserRouter, Routes, Route, NavLink, Navigate, useNavigate } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import Strategy from './pages/Strategy';
import History from './pages/History';
import Order from './pages/Order';
import Backtest from './pages/Backtest';
import Settings from './pages/Settings';
import Login from './pages/Login';
import SellPlanManager from './components/SellPlanManager';

/** 토큰이 없으면 로그인 화면으로 보내는 보호 래퍼. */
function RequireAuth({ children }) {
  const token = localStorage.getItem('auth_token');
  if (!token) return <Navigate to="/login" replace />;
  return children;
}

/** 로그아웃: 확인 모달 → 토큰 제거 후 로그인 화면으로. */
function LogoutButton() {
  const navigate = useNavigate();
  const [confirmOpen, setConfirmOpen] = useState(false);

  const logout = () => {
    localStorage.removeItem('auth_token');
    navigate('/login', { replace: true });
  };

  return (
    <>
      <button
        className="nav-link"
        onClick={() => setConfirmOpen(true)}
        style={{ width: '100%', textAlign: 'left', background: 'none', border: 'none', cursor: 'pointer' }}
      >
        <span className="nav-link__icon">🚪</span>
        로그아웃
      </button>

      {confirmOpen && (
        <div className="modal-overlay" onClick={() => setConfirmOpen(false)}>
          <div className="modal-content" onClick={e => e.stopPropagation()} style={{ maxWidth: 380, textAlign: 'center' }}>
            <div style={{ fontSize: '2rem', marginBottom: 12 }}>🚪</div>
            <h3 style={{ marginBottom: 8 }}>정말 로그아웃 하시겠습니까?</h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginBottom: 24 }}>
              다시 이용하려면 로그인이 필요합니다.
            </p>
            <div style={{ display: 'flex', justifyContent: 'center', gap: 10 }}>
              <button className="btn btn--outline" onClick={() => setConfirmOpen(false)}>취소</button>
              <button className="btn btn--primary" onClick={logout}>로그아웃</button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

/** 네비게이션 + 메인 콘텐츠(보호 라우트). */
function Layout() {
  return (
    <div className="app-layout">
      {/* ── 네비게이션 바 ── */}
      <nav className="app-nav">
        <div className="app-nav__brand">
          <div className="app-nav__brand-icon">AI</div>
          <span className="app-nav__brand-text">AutoInvesting</span>
        </div>

        <div className="app-nav__links">
            <NavLink to="/" end className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">📊</span>
              대시보드
            </NavLink>
            <NavLink to="/strategy" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">🎯</span>
              전략 관리
            </NavLink>
            <NavLink to="/order" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">⚡</span>
              퀀트 분석
            </NavLink>
            <NavLink to="/backtest" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">🧪</span>
              백테스팅
            </NavLink>
            <NavLink to="/history" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">📜</span>
              거래 내역
            </NavLink>
            <NavLink to="/sell-plans" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">📋</span>
              분할매도
            </NavLink>
            <NavLink to="/settings" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">⚙️</span>
              설정
            </NavLink>
            <LogoutButton />
          </div>
        </nav>

        {/* ── 메인 콘텐츠 ── */}
        <main className="app-main">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/strategy" element={<Strategy />} />
            <Route path="/history" element={<History />} />
            <Route path="/order" element={<Order />} />
            <Route path="/backtest" element={<Backtest />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="/sell-plans" element={<SellPlanManager />} />
          </Routes>
        </main>
      </div>
  );
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/*" element={<RequireAuth><Layout /></RequireAuth>} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
