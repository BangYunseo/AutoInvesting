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
      //
      // ⚠️ preset을 직접 지정하는 이유 (2026-08-03):
      //   @vite-pwa/assets-generator 기본값이 maskable/apple 에 padding 0.3 + background 'white' 다.
      //   favicon.svg 는 이미 512x512 풀블리드 다크 배경이고 콘텐츠(동전 스택)가
      //   중심에서 최대 171.8px(마스커블 안전원 반지름 204.8px)에 들어와 잘릴 위험이 없다.
      //   그런데 기본 padding 이 그림을 70%로 줄이고 바깥 30%를 흰색으로 칠해서,
      //   안드로이드(One UI) 런처가 squircle 마스크를 씌우면 흰 테두리에 작은 아이콘이 떠 보였다.
      //   → padding 0 + 배경을 앱 테마색으로 지정해 풀블리드를 유지한다.
      //   참고: 모서리가 둥근 것 자체는 런처가 씌우는 마스크이므로 앱에서 정사각형으로 만들 수 없다.
      pwaAssets: {
        image: 'public/favicon.svg',
        preset: {
          transparent: {
            sizes: [64, 192, 512],
            favicons: [[48, 'favicon.ico']],
            padding: 0
          },
          maskable: {
            sizes: [512],
            padding: 0,
            resizeOptions: { background: '#0b0e14' }
          },
          apple: {
            sizes: [180],
            padding: 0,
            resizeOptions: { background: '#0b0e14' }
          }
        }
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
