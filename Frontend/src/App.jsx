import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import DcaConfig from './pages/DcaConfig';
import History from './pages/History';
import Order from './pages/Order';
import Settings from './pages/Settings';

function App() {
  return (
    <BrowserRouter>
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

        {/* ── 메인 콘텐츠 ── */}
        <main className="app-main">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/dca-config" element={<DcaConfig />} />
            <Route path="/history" element={<History />} />
            <Route path="/order" element={<Order />} />
            <Route path="/settings" element={<Settings />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;
