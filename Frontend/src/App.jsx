import { BrowserRouter, Routes, Route, NavLink, useLocation, Navigate } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import DcaConfig from './pages/DcaConfig';
import History from './pages/History';
import Order from './pages/Order';
import Settings from './pages/Settings';
import Login from './pages/Login';

/**
 * 앱 셸. 로그인 화면(/login)에서는 상단 네비게이션을 숨기고 로그인 폼만 보여준다.
 * (인터셉터가 401 시 /login으로 보내므로 이 라우트가 반드시 존재해야 한다.)
 */
function Shell() {
  const { pathname } = useLocation();
  const isLogin = pathname === '/login';

  // 토큰이 없으면 아예 로그인 화면으로 보낸다 — 네비게이션이 그려지지 않으므로 탭 전환도 불가.
  // (실제 차단은 서버 전역 인증 필터가 한다. 여기는 빈 화면·의미 없는 401 왕복을 막는 화면 가드다.
  //  만료된 토큰은 여기서 걸러지지 않고 기존 401 인터셉터가 로그인으로 되돌린다.)
  if (!isLogin && !localStorage.getItem('auth_token')) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="app-layout">
      {/* ── 네비게이션 바 (로그인 화면 제외) ── */}
      {!isLogin && (
        <nav className="app-nav">
          <div className="app-nav__brand">
            <div className="app-nav__brand-icon">📈</div>
            <span className="app-nav__brand-text">ETF 적립 매수</span>
          </div>

          <div className="app-nav__links">
            <NavLink to="/" end className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">📊</span>
              대시보드
            </NavLink>
            <NavLink to="/dca-config" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">🎯</span>
              적립 설정
            </NavLink>
            <NavLink to="/order" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">⚡</span>
              주문 설정
            </NavLink>
            <NavLink to="/history" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">📜</span>
              거래 내역
            </NavLink>
            <NavLink to="/settings" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <span className="nav-link__icon">⚙️</span>
              설정
            </NavLink>
          </div>
        </nav>
      )}

      {/* ── 메인 콘텐츠 ── */}
      <main className="app-main">
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/" element={<Dashboard />} />
          <Route path="/dca-config" element={<DcaConfig />} />
          <Route path="/history" element={<History />} />
          <Route path="/order" element={<Order />} />
          <Route path="/settings" element={<Settings />} />
        </Routes>
      </main>
    </div>
  );
}

function App() {
  return (
    <BrowserRouter>
      <Shell />
    </BrowserRouter>
  );
}

export default App;
