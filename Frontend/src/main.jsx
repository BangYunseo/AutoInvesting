import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'

// ── Global Fetch Interceptor ──
// 모든 API 요청에 자동으로 localStorage의 x-api-key를 붙이고, 401 응답 시 키 입력을 요청합니다.
const originalFetch = window.fetch;
window.fetch = async (...args) => {
  let [resource, config] = args;
  config = config || {};
  
  // URL이 상대경로(우리 서버 API)일 경우에만 키 주입 (필요시)
  config.headers = {
    ...config.headers,
    'x-api-key': localStorage.getItem('api_access_key') || ''
  };
  
  const response = await originalFetch(resource, config);
  
  // 401 Unauthorized 발생 시 사용자에게 키 입력 프롬프트 띄우기 (중복 호출 방지)
  if (response.status === 401 && !window.__isPromptingKey) {
    window.__isPromptingKey = true;
    const newKey = window.prompt("🔒 백엔드 보안 접근을 위해 서버의 API_ACCESS_KEY를 입력해주세요:");
    if (newKey !== null && newKey.trim() !== '') {
      localStorage.setItem('api_access_key', newKey.trim());
      window.location.reload(); // 키 저장 후 자동 새로고침
    } else {
      window.__isPromptingKey = false;
    }
  }
  return response;
};

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
