import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'

// ── Global Fetch Interceptor ──
// 로그인으로 발급받은 Bearer 세션 토큰을 모든 API 요청에 자동으로 붙이고,
// 401(인증 만료/없음) 응답 시 로그인 화면으로 이동시킵니다.
const originalFetch = window.fetch;
window.fetch = async (...args) => {
  let [resource, config] = args;
  config = config || {};

  const url = typeof resource === 'string' ? resource : (resource?.url || '');
  const isAuthApi = url.includes('/api/auth/');

  const token = localStorage.getItem('auth_token') || '';
  config.headers = {
    ...config.headers,
    ...(token ? { Authorization: `Bearer ${token}` } : {})
  };

  const response = await originalFetch(resource, config);

  // 인증 실패 → 로그인 화면으로 (로그인 API 자체의 401은 폼에서 직접 처리하므로 제외)
  if (response.status === 401 && !isAuthApi && window.location.pathname !== '/login') {
    localStorage.removeItem('auth_token');
    window.location.href = '/login';
  }
  return response;
};

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
