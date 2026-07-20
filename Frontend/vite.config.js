import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      // 새 배포 시 서비스워커가 자동 갱신되어 옛 화면이 캐시로 남지 않게 함
      registerType: 'autoUpdate',
      injectRegister: 'auto',
      // 기존 favicon.svg 하나로 iOS·안드로이드·마스커블 아이콘을 자동 생성하고
      // apple-touch-icon 등 head 링크를 index.html에 자동 주입한다
      pwaAssets: {
        image: 'public/favicon.svg'
      },
      manifest: {
        name: 'ETF 자동적립',
        short_name: 'ETF적립',
        description: '해외 ETF 자동 적립(DCA) 투자',
        lang: 'ko',
        // 홈 화면 아이콘 탭 시 주소창 없는 전체화면 앱처럼 실행
        display: 'standalone',
        start_url: '/',
        scope: '/',
        // 다크 테마(index.css --bg-primary)에 맞춘 스플래시/상태바 색
        theme_color: '#0b0e14',
        background_color: '#0b0e14'
      },
      workbox: {
        // 앱 셸(정적 자산)만 프리캐시 — API 응답은 캐시하지 않는다
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff,woff2}'],
        maximumFileSizeToCacheInBytes: 3 * 1024 * 1024,
        cleanupOutdatedCaches: true,
        clientsClaim: true,
        // SPA 라우팅은 index.html로 폴백하되, /api 요청은 항상 네트워크로 통과
        // (잔고·거래내역 등 금융 데이터가 캐시로 굳어 옛 값이 보이는 것을 방지)
        navigateFallback: 'index.html',
        navigateFallbackDenylist: [/^\/api/]
      }
    })
  ],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  },
  // 빌드 산출물(PWA 포함)을 로컬에서 검증할 때도 API가 로컬 백엔드로 프록시되도록
  preview: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  }
})
