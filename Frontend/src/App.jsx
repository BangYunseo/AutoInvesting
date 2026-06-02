import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import Strategy from './pages/Strategy';
import History from './pages/History';
import Order from './pages/Order';
import Backtest from './pages/Backtest';
import Settings from './pages/Settings';
import SellPlanManager from './components/SellPlanManager';

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
          </div>

          <div className="app-nav__status">
            <span className="status-dot" />
            시스템 가동 중
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
    </BrowserRouter>
  );
}

export default App;
