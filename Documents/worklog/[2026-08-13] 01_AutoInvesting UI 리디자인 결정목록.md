---
title: AutoInvesting UI 리디자인 결정목록
date: 2026-08-13
company: [개인프로젝트]
tags: [ui, 리디자인, 디자인토큰, 소유권경계, claude-code]
status: in-progress
---

# AutoInvesting UI 리디자인 결정목록

## 개요
> AutoInvesting 화면을 재설계하기 전에 내가 직접 확정해야 할 결정 항목을 모아둔 작업 문서다. 이 문서의 빈칸이 모두 채워진 뒤에 실행계획서로 발전시킨다.

## 배경 / 목적
UI 작업을 코드부터 시작하면 결정되지 않은 항목을 도구가 기본값으로 채우고, 그 기본값이 곧 "AI가 만든 화면"의 인상이 된다. 따라서 코드 작성 전에 빈칸을 먼저 없앤다.

이 문서를 읽는 Claude Code에게 적용되는 규칙은 다음과 같다.

- 빈칸을 대신 채우지 않는다. 값을 제안할 때는 반드시 후보를 복수로 제시하고 멈춘다.
- 후보를 제시할 때는 각 후보의 트레이드오프를 함께 적는다.
- `[조사]` 표시가 붙은 항목만 코드베이스를 읽고 직접 채운다.
- `[결정]` 표시가 붙은 항목은 사용자 답변 없이 진행하지 않는다.

## 본문

### 사전 조사 결과
`[조사]` 항목. 코드 수정 없이 읽기만 해서 채웠다.

> **먼저 읽을 것**: 이 목록의 항목 절반은 "0건"으로 나온다. 값이 없어서가 아니라, **이 목록이 전제한 스택(Next.js + Tailwind + shadcn/ui + TypeScript)이 이 프로젝트에 존재하지 않기 때문**이다. 실제 정체는 Vite + React 19 + 손으로 쓴 단일 전역 CSS다. 항목별로 "그 자리를 대신하는 실제 구조"를 함께 적었고, 어긋난 용어는 아래 `### 용어 정의 불일치` 표에 모았다.

#### 프레임워크와 버전

**Vite + React SPA (Next.js 아님).** 런타임 의존성은 3개뿐이다 — `Frontend/package.json:12-16`.

| 구분 | 패키지 | 선언(package.json) | 실제 설치(package-lock.json) |
|---|---|---|---|
| 빌드 | `vite` | `^8.0.12` (`package.json:27`) | **8.0.15** (`package-lock.json:7315-7317`) |
| 빌드 플러그인 | `@vitejs/plugin-react` | `^6.0.1` (`package.json:22`) | 6.0.2 (`package-lock.json:3214-3215`) |
| 빌드 플러그인 | `vite-plugin-pwa` | `^1.3.0` (`package.json:28`) | 1.3.0 (`package-lock.json:7393-7394`) |
| UI | `react` | `^19.2.6` (`package.json:13`) | **19.2.6** (`package-lock.json:6162-6163`) |
| UI | `react-dom` | `^19.2.6` (`package.json:14`) | 19.2.6 (`package-lock.json:6171-6172`) |
| 라우터 | `react-router-dom` | `^7.16.0` (`package.json:15`) | **7.16.0** (`package-lock.json:6205-6206`) |
| 린트 | `eslint` | `^10.3.0` (`package.json:23`) | 10.4.1 |

- **TypeScript: 없음.** `tsconfig.json` 없음, `typescript` 패키지 미설치, `src/**/*.{ts,tsx}` 0건. 소스는 `.jsx` 10개 + `.js` 1개 + `.css` 1개 = **12개 파일**. ESLint 대상도 `'**/*.{js,jsx}'`(`Frontend/eslint.config.js:10`). `@types/react`(`package.json:19`)·`@types/react-dom`(`package.json:20`)은 devDependencies에 있으나 컴파일 경로가 없어 에디터 힌트용이다.
- React 19 신 API 사용 중: `createRoot` from `react-dom/client`(`Frontend/src/main.jsx:2, 33`), `StrictMode`(`main.jsx:1, 34-36`).
- 라우팅은 `BrowserRouter` 방식(data router 아님) — `Frontend/src/App.jsx:1, 72-76`.
- 상태관리 라이브러리 없음(redux/zustand/jotai/tanstack-query 전부 미설치). HTTP 클라이언트도 없고 `window.fetch`를 전역 몽키패칭한다 — `Frontend/src/main.jsx:9-31`.
- **테스트 러너 없음.** 스크립트는 `dev`/`build`/`lint`/`preview` 4개뿐(`package.json:6-11`), `test` 스크립트·vitest·jest 모두 없다.

#### Tailwind 버전

**없음.** Tailwind가 설치되어 있지 않으므로 v3/v4 판정 대상 자체가 존재하지 않는다.

근거(4중 확인):

- `Frontend/package.json:12-29` 전체에 `tailwindcss` 없음 (dependencies 3 + devDependencies 11이 전부)
- `Frontend/package-lock.json`에 `"node_modules/tailwindcss"`, `"node_modules/@tailwindcss/*"` **0건**
- `tailwind.config.*` / `postcss.config.*` 파일 없음 (git 추적 파일 19개 목록에도 없음)
- `Frontend/src/index.css`에 `@tailwind` 지시어 0건, `@import "tailwindcss"` 0건 — 유일한 `@import`는 `index.css:1`의 Google Fonts 1줄

**그 자리를 대신하는 실제 구조**: 손으로 쓴 vanilla CSS 단일 전역 파일 `Frontend/src/index.css`(**1150줄**) + BEM 유사 시맨틱 클래스(`.btn--primary`, `.summary-card__value`) + `:root` CSS 커스텀 프로퍼티 토큰. JSX의 `var(--)` 참조는 **263건**이다.

`postcss` 8.5.15는 lockfile에 있으나 Vite의 전이 의존성(`"dev": true`, `package-lock.json:7321-7327`)이고 `postcss.config` 파일이 없어 사용자 PostCSS 파이프라인은 구성돼 있지 않다. `sass`/`less`도 Vite의 optional peerDependency 선언일 뿐 미설치다(`package-lock.json:7345-7373`).

#### 라우트별 화면 목록과 파일 경로

라우트 정의는 **한 곳**이다 — `Frontend/src/App.jsx:56-65`의 `<Routes>` 블록. 라우트 설정 파일, lazy loading, 중첩 라우트, `basename` 모두 없다.

| 라우트 | 컴포넌트 | 파일 경로 | 정의 위치 | 줄 수 |
|---|---|---|---|---|
| `/login` | `Login` | `Frontend/src/pages/Login.jsx` | App.jsx:57 | 147 |
| `/` | `Dashboard` | `Frontend/src/pages/Dashboard.jsx` | App.jsx:58 | 245 |
| `/dca-config` | `DcaConfig` | `Frontend/src/pages/DcaConfig.jsx` | App.jsx:59 | **738** |
| `/history` | `History` | `Frontend/src/pages/History.jsx` | App.jsx:60 | 335 |
| `/order` | `Order` | `Frontend/src/pages/Order.jsx` | App.jsx:61 | **1169** |

**라우트 수와 화면 수가 일치하지 않는다.**

- `/history`는 한 라우트 안에 탭 2개(`📋 매매 내역` / `🖥️ 시스템 로그`)가 `activeTab` state로만 갈린다 — `History.jsx:121-134`. URL에 반영되지 않는다.
- `/order`는 한 라우트에 성격이 다른 카드 2개(좌 "적립식 매수 실행" `Order.jsx:708-834` / 우 "수동 주문" `Order.jsx:837-1142`)가 `.order-layout` 2단 그리드로 놓인다(`Order.jsx:706`).
- 화면 단위로 세면 **7개**다.

**라우트에 없는 것**

- **catch-all(`path="*"`) 없음** — `App.jsx:56-65`에 `Route`는 위 5개뿐. 존재하지 않는 경로로 들어오면 네비게이션만 그려지고 `<main>` 안쪽이 빈 화면이 된다. 404 화면 없음.
- `/settings` 라우트는 제거됐고 사유가 주석으로 남아 있다 — `App.jsx:62-64`.
- `/login`은 네비게이션 링크가 없다. `App.jsx:19-21`의 화면 가드와 `main.jsx:26-29`의 401 인터셉터로만 도달한다.

**네비게이션·레이아웃**: `App.jsx` 한 곳에만 있고 페이지 중복 없음. `.app-layout`(App.jsx:24) → `<nav className="app-nav">`(App.jsx:27-51, `!isLogin`일 때만) → `<main className="app-main">`(App.jsx:55). 링크 4개(`/`, `/dca-config`, `/order`, `/history`)이며 아이콘은 유니코드 이모지 문자열이다 — `App.jsx:29`(📈), `:35`(📊), `:39`(🎯), `:43`(⚡), `:47`(📜). **`Layout.jsx`·`Header.jsx`·`PageHeader` 같은 레이아웃 컴포넌트는 존재하지 않는다.**

**페이지별 호출 API (고유 엔드포인트 13개)**

| 라우트 | 호출 |
|---|---|
| `/` | GET `/api/portfolio/summary`(Dashboard.jsx:25) — 1개 |
| `/dca-config` | GET `/api/price/{ticker}`(:54), GET `/api/dca/config`(:79), GET `/api/portfolio/summary`(:112), GET `/api/history/trades?limit=500`(:128), PUT `/api/dca/config`(:306) |
| `/order` | GET `/api/portfolio/holdings`(:278), GET `/api/order/dca-schedule`(:294), GET `/api/history/trades?limit=500`(:301), GET `/api/dca/config`(:313), PUT `/api/dca/config`(:335), POST `/api/order/dca-run?force=true`(:469), POST `/api/order/dca-schedule?reserve=`(:495), GET `/api/price/{ticker}`(:518), GET `/api/order/sell-preview`(:620), POST `/api/order/manual`(:678) — 10개 |
| `/history` | GET `/api/history/trades?limit=`(:27), GET `/api/history/logs?date=&lines=200`(:43) |
| `/login` | GET `/api/auth/status`(:22), POST `/api/auth/setup` 또는 `/api/auth/login`(:51-52) |

**인증 게이트**: ① 화면 가드 — `App.jsx:19-21`이 `localStorage.getItem('auth_token')`을 매 렌더 직접 읽고 없으면 `<Navigate to="/login" replace />`. Context·Provider·훅 없음. ② 401 인터셉터 — `main.jsx:26-29`가 토큰 삭제 후 `window.location.href`로 이동(React Router가 아니라 전체 리로드). ③ 토큰 저장 — `Login.jsx:78-79`. **로그아웃 버튼·메뉴는 어느 파일에도 없다.**

#### `components/ui` 하위 컴포넌트 목록

**없음.** `Frontend/src/components/ui` 디렉터리가 존재하지 않고, 저장소 전체(node_modules 제외)에서 이름이 `ui`인 디렉터리가 **0건**이다. `git ls-files src`와 `find src -type f` 결과가 일치하므로 추적되지 않은 숨은 디렉터리도 없다.

**그 자리를 대신하는 실제 구조 (a) — 평면 컴포넌트 3개**

| 파일 | 줄 수 | 사용처 |
|---|---|---|
| `Frontend/src/components/AllocationDonut.jsx` | 160 | Dashboard 단 1곳 (import `Dashboard.jsx:3`, 사용 `:238`) |
| `Frontend/src/components/HoldingsTable.jsx` | 66 | Dashboard 단 1곳 (import `Dashboard.jsx:2`, 사용 `:225`) |
| `Frontend/src/components/ConfirmDialog.jsx` | 63 | DcaConfig(`:707` 템플릿 삭제) + Order(`:1145` 적립 실행·예약 4종) — **실제로 재사용되는 유일한 컴포넌트** |

`Frontend/src/utils/dcaRuns.js`(62줄)도 2곳에서 공유된다 — DcaConfig.jsx:2(`groupRuns`, `toBuyRows`), Order.jsx:2(`countRunsInMonth`).

**(b) 프리미티브 레이어(Button/Card/Input/Dialog)가 아예 없다.** 페이지가 날 HTML 태그에 전역 클래스를 직접 붙인다(`<button className="btn btn--primary">`, `<div className="card">`). 대응되는 클래스 정의(`index.css` 라인):

- 버튼 `.btn`(472) `.btn--primary`(487) `.btn--outline`(496) `.btn--danger`(507)
- 컨테이너 `.card`(218) `.card-head`(738) `.summary-card`(249) + `__header/__label/__icon/__value/__sub`(282-333) `.summary-grid`(242)
- 폼 `.form-group`(586) `.input-field`(600) `.chip`(671) `.chip--on`(695) `.chip-row`(664) `.day-grid`(800) `.pick-list`(771) `.ccy-toggle`(912)
- 표·상태 `.data-table`(362) `.badge`(941) `.badge-profit--up/--down`(348/352) `.alert`(516) + `--ok/--err/--warn/--info`(524/529/534/542) `.empty-state`(570) `.loading-container`(424) `.error-container`(452)
- 레이아웃 `.app-nav`(134) `.nav-link`(180) `.app-main`(212) `.order-picker`(779) `.manual-order--buy`(757) `.log-cal`(827) `.section-header`(550) `.tabs`/`.tab-btn`

**(c) 모달이 두 종류로 갈라져 있다.** 공용 `ConfirmDialog`(63줄)와 **`Order.jsx` 안에 로컬 정의된 `OrderConfirmModal`**(`Order.jsx:56-224`, 169줄)이 같은 `.modal-overlay`/`.modal-content` 클래스를 각자 쓴다. 같은 파일에 `InfoRow`(`Order.jsx:9-29`)도 로컬 정의돼 있다. 모달 스타일을 한 곳에서 고칠 수 없다.

#### `globals.css` 현재 토큰 전문

**`globals.css`라는 파일은 저장소에 없다.** 그 역할을 하는 실제 파일은 **`Frontend/src/index.css`(1150줄)** 이며, CSS 소스 파일은 저장소 전체에 이것 하나뿐이다(`Frontend/dist/assets/index-*.css`는 빌드 산출물). CSS Modules·CSS-in-JS 없음. 진입점 import는 `Frontend/src/main.jsx:3` 1회.

##### `:root` 블록 전문 (`index.css:3-56`) — 토큰 36개

```css
/* ── 디자인 토큰 ── */
:root {
  /* 색상 */
  --bg-primary: #0b0e14;
  --bg-secondary: #111620;
  --bg-card: rgba(17, 22, 32, 0.85);
  --bg-card-hover: rgba(24, 31, 46, 0.9);
  --bg-elevated: #1a2235;
  --bg-input: #141b2a;

  --text-primary: #e8ecf1;
  --text-secondary: #8892a4;
  --text-muted: #5a6478;

  --border-primary: rgba(255, 255, 255, 0.06);
  --border-subtle: rgba(255, 255, 255, 0.03);

  --accent-blue: #3b82f6;
  --accent-blue-glow: rgba(59, 130, 246, 0.2);
  --accent-cyan: #06b6d4;
  --accent-purple: #8b5cf6;
  --accent-purple-glow: rgba(139, 92, 246, 0.15);

  --profit-green: #10b981;
  --profit-green-bg: rgba(16, 185, 129, 0.1);
  --loss-red: #ef4444;
  --loss-red-bg: rgba(239, 68, 68, 0.1);
  --warn-amber: #f59e0b;

  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.3);
  --shadow-md: 0 4px 12px rgba(0, 0, 0, 0.4);
  --shadow-lg: 0 8px 32px rgba(0, 0, 0, 0.5);
  --shadow-glow-blue: 0 0 20px rgba(59, 130, 246, 0.15);
  --shadow-glow-purple: 0 0 20px rgba(139, 92, 246, 0.1);

  /* 타이포그래피 */
  --font-sans: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  --font-mono: 'JetBrains Mono', 'Fira Code', ui-monospace, monospace;

  /* 간격 */
  --radius-sm: 8px;
  --radius-md: 12px;
  --radius-lg: 16px;
  --radius-xl: 20px;

  /* 트랜지션 */
  --ease-out: cubic-bezier(0.16, 1, 0.3, 1);
  --transition-fast: 150ms var(--ease-out);
  --transition-normal: 250ms var(--ease-out);
  --transition-slow: 400ms var(--ease-out);

  font-family: var(--font-sans);
  color-scheme: dark;
}
```

##### 스코프 오버라이드 전문 (`index.css:754-767`) — 토큰 4개 재정의

```css
/* ── 수동 주문: 매수=빨강 / 매도=파랑 (한국 관례) ──
   칩·버튼·포커스링이 각자 색을 들고 있으면 분기가 늘어난다. 카드 안에서 강조색 변수만
   갈아끼워 전부 따라오게 한다. 매도는 기본 강조색(파랑)이라 별도 선언이 없다. */
.manual-order--buy {
  --accent-blue: var(--loss-red);
  --accent-blue-glow: var(--loss-red-bg);
  --accent-purple: #f97316;
  --shadow-glow-blue: 0 0 20px rgba(239, 68, 68, 0.15);
}

/* btn--primary:hover만 색을 하드코딩하고 있어 변수 교체가 닿지 않는다 */
.manual-order--buy .btn--primary:hover {
  background: #dc2626;
}
```

**커스텀 프로퍼티 선언 총 40개**(`:root` 36 + `.manual-order--buy` 4). 위 두 코드블록이 전문이며 그 밖의 선언 위치는 없다.

##### 토큰에 관한 사실

- **간격 토큰(`--space-*`)·타이포 스케일 토큰(`--font-size-*`) 0개.** `/* 간격 */` 주석(`index.css:42`)이 붙은 블록에는 border-radius 4개만 들어 있다.
- `--radius`(접미사 없는 단일 이름)는 **없다**. 4단계 스케일이며 `--radius-md`는 `index.css:1035`(`.tabs`) 1곳, `--radius-xl`은 **사용처 0건**이다.
- **참조 0건 토큰 5개**: `--bg-secondary`(7), `--bg-card-hover`(9), `--radius-xl`(46), `--shadow-glow-purple`(36), `--transition-slow`(52).
- **다크 모드 처리 없음.** `prefers-color-scheme` 0건, `[data-theme]` 0건, `.dark` 0건. `@media`는 2개뿐이며 둘 다 반응형 브레이크포인트다(`index.css:964` max-width 1024px, `:980` max-width 640px). `color-scheme: dark`(`:55`)는 브라우저 UI 힌트일 뿐 전환 로직이 아니다. `index.html:2`도 `<html lang="ko">`로 `data-theme` 없음.
- **색 표기는 hex + rgba 2종뿐.** `hsl()`/`oklch()`/`lab()`/`lch()`/`color-mix()`/`rgb()` 전부 0건.
- **폰트 로딩**: `index.css:1`의 Google Fonts `@import` 1줄(Inter 300/400/500/600/700/800, `display=swap`). `index.html`에는 폰트 `<link>`도 `preconnect`도 없다(`index.html:1-14`). `@font-face` 0건. **`--font-mono`의 `'JetBrains Mono'`·`'Fira Code'`는 어디서도 로딩하지 않아 실제 렌더는 `ui-monospace`(OS 기본 모노) 폴백이다** — 사용처는 `index.css:1082`(`.log-viewer__content`), `History.jsx:204`(인라인).
- 숫자 정렬 표기가 두 가지로 혼재: `font-feature-settings: 'tnum'`(328, 346, 394) / `font-variant-numeric: tabular-nums`(727, 817, 877).

#### `shadow-*` 사용 횟수와 위치

**Tailwind `shadow-*` 유틸리티 클래스: 0건.** `className` 속성 내부의 `shadow-`는 0건이다.

`shadow-` 문자열 자체는 `src/` 전체에서 12회 나오지만 **전부 CSS 변수 이름(`--shadow-*`)의 일부**이며 모두 `src/index.css`에 있다. 다른 파일 0건. 문자열만 세면 12건을 Tailwind 사용으로 오독하게 된다.

| 구분 | 위치 |
|---|---|
| 토큰 정의 6 | `index.css:32`(`--shadow-sm`) `:33`(`--shadow-md`) `:34`(`--shadow-lg`) `:35`(`--shadow-glow-blue`) `:36`(`--shadow-glow-purple`) `:761`(`--shadow-glow-blue` 오버라이드) |
| 토큰 소비 6 | `index.css:164`(glow-blue) `:225`(sm) `:230`(md) `:275`(md) `:493`(glow-blue) `:1143`(lg) |

**CSS `box-shadow` 속성**: 11회 등장 / **실제 선언 10건**(`index.css:633`은 `transition` 대상 지정이라 선언 아님). 내역 = 토큰 참조 6 + 포커스링 리터럴 2(`:617`, `:704` — 둘 다 `0 0 0 2px var(--accent-blue-glow)`) + `none` 2(`:650`, `:708`). 실제로 그림자를 그리는 건 8건이다. `box-shadow` 값에 색상 리터럴을 직접 쓴 곳은 0건(전부 `var()` 경유). **JSX 인라인 `boxShadow`(camelCase)도 0건.**

#### 하드코딩 색상값 사용 횟수와 위치

**총 91건** (주석 안 1건 포함 기준. 실행 코드만 세면 90건 — `AllocationDonut.jsx:16`의 `#10151e`가 주석 문장 안이다.)

| 형식 | 건수 |
|---|---|
| `#RRGGBB` | 28 |
| `#RGB` | 1 |
| `rgba(` | 62 |
| `rgb(` / `hsl(` / `hsla(` / `oklch(` / `oklab(` / `lab(` / `lch(` / `color-mix(` | **각 0** |
| **합계** | **91** |

키워드 색은 별도로 `white` 3건(`index.css:162, 489, 509`), `transparent` 11건(`index.css:87, 98, 191, 478, 497, 871, 873, 925, 997, 1041, 1043`), SVG data URI 내부 URL 인코딩 hex 3건(`index.css:628` `%238892a4`, `:643` `%233b82f6`, `:651` `%238892a4`)이 있다.

| 파일 | hex | rgba | 계 |
|---|---:|---:|---:|
| `src/index.css` | 18 | 40 | 58 |
| `src/pages/DcaConfig.jsx` | 3 | 15 | 18 |
| `src/components/AllocationDonut.jsx` | 8 | 1 | 9 |
| `src/pages/Dashboard.jsx` | 0 | 3 | 3 |
| `src/pages/Order.jsx` | 0 | 3 | 3 |
| **계** | **29** | **62** | **91** |

`History.jsx`, `Login.jsx`, `ConfirmDialog.jsx`, `HoldingsTable.jsx`, `App.jsx`, `main.jsx`, `dcaRuns.js`는 0건이다.

##### 토큰 정의 vs 새는 리터럴

| 구분 | 건수 |
|---|---:|
| `index.css` `:root` 토큰 정의 라인 | **28** (hex 14 + rgba 14) |
| `index.css` 흩어진 사용처 | **30** (hex 4 + rgba 26) |
| JSX 흩어진 사용처 | **33** (hex 11 + rgba 22) |

**91건 중 28건만 토큰 정의이고 63건은 사용 지점에 직접 박힌 리터럴이다(정의의 2.25배).**

주요 위치:

- 토큰 밖 hex — `index.css:492`(`#2563eb`, `.btn--primary:hover`), `:512`(`#dc2626`), `:766`(`#dc2626`), `:937`, `:760`(`#f97316`)
- 반투명 오버레이 반복 — `--border-primary`(0.06)·`--border-subtle`(0.03)이 있는데도 `rgba(255,255,255,0.02/0.04/0.08/0.1/0.15/0.18/0.2)`가 사용처마다 따로 등장: `index.css:198, 229, 273, 385, 503, 504, 678, 691, 883, 959, 1000, 1004, 1054, 1072` 등
- JSX 누수 예 — `DcaConfig.jsx:412` `background: isSel ? 'rgba(110,168,254,0.08)' : 'rgba(255,255,255,0.02)'`
- 차트 팔레트 — `AllocationDonut.jsx:18` `const SERIES = ['#3987e5', '#d95926', '#199e70', '#c98500', '#d55181', '#008300'];`, `:19` `OTHER_COLOR`, `:20` `MAX_SLICES = 6`
- **알파색 이중 관리** — `--accent-blue: #3b82f6`(`:20`)와 `--accent-blue-glow: rgba(59,130,246,0.2)`(`:21`)가 독립 선언이라 파랑 하나를 바꾸면 `rgba(59,130,246,…)` 리터럴 8곳(21, 35, 87, 204, 544, 545, 1060 등)을 함께 고쳐야 한다.

##### 정의되지 않은 토큰 참조 3건 (현재 깨진 상태)

`var(--accent, #6ea8fe)` 형태가 3곳 있으나 **`--accent`라는 토큰은 코드베이스 어디에도 정의돼 있지 않다**(정의된 것은 `--accent-blue`/`--accent-cyan`/`--accent-purple`뿐 — `index.css:20, 22, 23`). 따라서 항상 폴백 `#6ea8fe`로 고정 렌더되며 `index.css:757-762`의 테마 오버라이드도 받지 못한다. 토큰 참조처럼 보이지만 실제로는 하드코딩이다.

- `Frontend/src/pages/DcaConfig.jsx:411` — `border: isSel ? '1px solid var(--accent, #6ea8fe)' : '1px solid rgba(255,255,255,0.08)'`
- `Frontend/src/pages/DcaConfig.jsx:562` — `border: isLogSel ? '1px solid var(--accent, #6ea8fe)' : …`
- `Frontend/src/pages/DcaConfig.jsx:578` — `color: isCur ? 'var(--accent, #6ea8fe)' : 'var(--text-secondary)'`

#### `space-y-*` 사용 횟수와 위치

**없음.** `src/` 전체에서 `space-y-` 문자열이 0회다.

**그 자리를 대신하는 실제 구조**: flex/grid + `gap`.

| 방식 | 건수 |
|---|---:|
| `index.css`의 `gap:` 선언 | 19 |
| `index.css`의 `flex-direction: column` | 5 |
| JSX 인라인 `gap:` | 31 |
| JSX 인라인 `flexDirection: 'column'` | 8 |

예: `index.css:242-246`(`.summary-grid`이 `display: grid` + `gap: 16px`). **공통 간격 스케일 토큰이 없어 gap 값은 사용처마다 숫자로 들어간다.**

#### `max-w-*xl mx-auto` 사용 횟수와 위치

**없음.** `max-w-` 0건, `mx-auto` 0건(각각 독립적으로도 0).

**그 자리를 대신하는 실제 구조 — 정확히 대응하는 조합 3건**

| 위치 | 코드 | 역할 |
|---|---|---|
| `Frontend/src/index.css:106-107` | `max-width: 1400px;` + `margin: 0 auto;` (`#root`) | 앱 전체 센터링을 여기서 단독 처리 |
| `Frontend/src/pages/DcaConfig.jsx:355` | `style={{ maxWidth: 980, margin: '0 auto' }}` | 로딩 카드 |
| `Frontend/src/pages/DcaConfig.jsx:384` | `style={{ maxWidth: 980, margin: '0 auto' }}` | 본문 카드 |

센터링 없이 폭만 제한한 곳(별도): CSS `index.css:469`(400px), `:1142`(480px) / JSX `ConfirmDialog.jsx:16`(440), `Login.jsx:98`(380), `Order.jsx:71`(460).

**페이지 컨테이너 규칙이 통일돼 있지 않다** — Dashboard·History는 폭 제한 없이 블록 나열, DcaConfig는 인라인 `maxWidth: 980` 중앙정렬, Order는 `.order-layout` 그리드, Login은 `100vh` flex 센터(`Login.jsx:97`).

#### `Card` 컴포넌트 사용 파일과 횟수

**없음.** `import ... Card` 0건, `<Card>` 0건, `</Card>` 0건. 컴포넌트 라이브러리 자체가 없고 로컬 컴포넌트는 `HoldingsTable`·`AllocationDonut`·`ConfirmDialog` 3개뿐이다.

**그 자리를 대신하는 실제 구조**: `<div>`에 CSS 클래스를 직접 붙인다. 정의 위치 — `index.css:218`(`.card`), `:228`(hover), `:233`(`.card h2`), `:249`(`.summary-card`), `:738`(`.card-head`), `.summary-card__*` BEM 자식 6종(`:282-333`), 아이콘 변형 4종(`:307-319`).

**사용 횟수 (className 토큰 정확 일치 기준) — 카드 컨테이너 총 13개**

| 클래스 | 총 | 파일별 위치 |
|---|---:|---|
| `card` | **9** | Dashboard.jsx:215, :230 / DcaConfig.jsx:355, :384 / History.jsx:137, :146, :219 / Login.jsx:98 / Order.jsx:838 |
| `summary-card` | **4** | Dashboard.jsx:156, :168, :187, :200 |
| `card-head` | **1** | Order.jsx:842 |
| `summary-card__header` | 4 | Dashboard |
| `summary-card__label` | 4 | Dashboard |
| `summary-card__icon` | 4 | Dashboard |
| `summary-card__value` | 4 | Dashboard |
| `summary-card__sub` | 2 | Dashboard |

예 — `Frontend/src/pages/Dashboard.jsx:215` → `className="card fade-in"`, `Frontend/src/pages/Order.jsx:838` → ``className={`card fade-in fade-in-delay-2 manual-order manual-order--${orderType === "BUY" ? "buy" : "sell"}`}``

##### 스타일링 방식 판단용 전체 통계

| 항목 | 건수 |
|---|---:|
| `className` 총계 | **208** (문자열형 183 + 표현식형 25) |
| ↳ Tailwind 유틸리티 비율 | **0% (0건)** — 유틸리티 형태 역검사에서 걸린 유일한 토큰 `text-strong`조차 `index.css:397`에 정의된 프로젝트 클래스 |
| ↳ 클래스 토큰 census | 77종 / 256 인스턴스. 상위: `btn` 29, `btn--outline` 23, `fade-in` 20, `alert` 14, `form-group` 10, `card` 9 |
| JSX 인라인 `style={{}}` | **178** |
| `var(--)` 참조 | **263** (index.css 166, DcaConfig 44, Order 31, Dashboard 8, AllocationDonut 5, ConfirmDialog 4, History 4, Login 1) |

**인라인 `style={{}}` 178건의 파일별 분포**

| 파일 | 건수 | `fontSize:` |
|---|---:|---:|
| `src/pages/DcaConfig.jsx` | **75** | 29 |
| `src/pages/Order.jsx` | **47** | 20 |
| `src/pages/History.jsx` | 17 | 6 |
| `src/pages/Dashboard.jsx` | 13 | 4 |
| `src/components/AllocationDonut.jsx` | 11 | 5 |
| `src/pages/Login.jsx` | 9 | 3 |
| `src/components/ConfirmDialog.jsx` | 6 | 1 |
| `src/App.jsx` / `main.jsx` / `HoldingsTable.jsx` / `dcaRuns.js` | 0 | 0 |
| **계** | **178** | **68** |

`className` 208 : 인라인 178로 거의 동률이며, 인라인 178건 중 **122건(69%)이 `DcaConfig.jsx`(75) + `Order.jsx`(47)** 두 파일에 집중돼 있다.

### 용어 정의 불일치

이 표가 **결정 항목보다 먼저 합의되어야 하는 부분**이다. 왼쪽 열의 말을 그대로 쓰면 서로 다른 것을 가리키게 된다.

| 문서에 적힌 말 | 문서가 전제한 것 | 이 프로젝트의 실제 대응물 | 그대로 쓸 수 있는가 |
|---|---|---|---|
| Tailwind 버전 (v3/v4) | Tailwind 유틸리티로 스타일링, 버전 판정 후 문법 교체 | Tailwind 미설치. `index.css` 1150줄 전역 수제 CSS + BEM 유사 클래스 | ✗ — 판정 대상 없음. 도입은 '업그레이드'가 아니라 신규 도입 + 전 화면 마크업 재작성 |
| `shadow-*` 클래스 | Tailwind 그림자 유틸리티 사용량 | `--shadow-sm/md/lg/glow-blue/glow-purple` 토큰 5개 + `box-shadow: var(--shadow-*)` 소비 6회 | ✗ — 문자열 `shadow-` 12건은 전부 변수명이라 그대로 세면 오독 |
| `space-y-*` | 유틸리티로 세로 간격 | flex/grid + `gap` (CSS 19 / JSX 31) | ✗ |
| `max-w-*xl mx-auto` | 유틸리티 조합으로 폭 제한 + 센터링 | `index.css:106-107`의 `#root` 1곳 + JSX 인라인 `maxWidth: 980, margin: '0 auto'` 2곳 | ✗ |
| `Card` 컴포넌트 | shadcn/ui 류 라이브러리의 `<Card>` | CSS 클래스 `.card`(9회) / `.summary-card`(4회)를 `<div>`에 직접 부착 | ✗ — 컴포넌트가 없음 |
| `components/ui` 하위 컴포넌트 | 프리미티브 컴포넌트 레이어 | 디렉터리 없음. `components/` 평면 파일 3개(AllocationDonut 160·HoldingsTable 66·ConfirmDialog 63) | ✗ |
| `components/ui/*.tsx` 수정 범위 (F절) | `.tsx` 파일 단위로 권한 분리 | `.tsx` 0개. TypeScript 미사용, 전부 `.jsx` | ✗ — 파일 확장자·경로 모두 부재 |
| `globals.css` | 전역 스타일 파일 | `Frontend/src/index.css`(1150줄, 저장소 유일 CSS 소스) | △ — 대상을 `index.css`로 바꾸면 성립 |
| `--radius` (단일 반경 토큰) | 반경 값 하나 | `--radius-sm/md/lg/xl` 4단계(8/12/16/20px). `--radius-md`는 1곳, `--radius-xl`은 0곳 사용 | △ — 이름을 4개로 나눠 지정해야 함 |
| 다크 모드 지원 여부 | 라이트/다크 양쪽 선택지 | 다크 전용 고정. `color-scheme: dark`(`index.css:55`) 한 줄뿐이고 `prefers-color-scheme`·`[data-theme]`·`.dark` 전부 0건 | ✗ — '지원 여부' 결정이 아니라 라이트 팔레트 신규 구축 작업 |
| 색 토큰을 OKLCH로 명세 | 함수형 색공간 표기 | hex 29 + rgba 62, `oklch`/`hsl`/`color-mix` 0건. 알파 변형이 원색과 독립 선언 | ✗ — 표기법 전환 자체가 별도 작업 |
| 간격/타이포 스케일 토큰 | 토큰만 갈아끼우면 리디자인 완료 | 간격·타이포 토큰 0개. `index.css` font-size 하드코딩 41줄, padding/gap/margin 72줄, JSX 인라인 178건(fontSize 68) | ✗ — 토큰 교체만으로 반영되지 않는 영역이 큼 |
| 차트 라이브러리 props/테마 | recharts 등 라이브러리 설정 | `AllocationDonut.jsx`(160줄) 자체 SVG. `strokeDasharray`로 조각 그림(`:78-89`), 팔레트 상수 `SERIES` 6색(`:18`) | ✗ — 미사용은 실수가 아니라 명시적 결정(`AllocationDonut.jsx:6-7` 주석) |
| 아이콘 컴포넌트 (`lucide-react` 등) | `<Icon />` 교체·추가 | 유니코드 이모지 문자열 (`App.jsx:29, 35, 39, 43, 47`) | ✗ — '교체'는 문자를 바꾸는 것이거나 라이브러리 신규 도입 결정 |
| `framer-motion` 등 모션 | 라이브러리로 전환 애니메이션 | `index.css` `@keyframes` 4개(`spin`:442, `fadeInUp`:1008, `fadeIn`:1131, `slideUp`:1147) + 인라인 transition 1건(`AllocationDonut.jsx:86`) | ✗ |
| 테마 색상을 CSS 변수 한 곳에서 관리 | 색 변경이 단일 지점 | 이원화. 화면은 `index.css` 토큰, PWA 스플래시/상태바/마스커블 배경은 `vite.config.js:35, 40, 54, 55`에 `#0b0e14` 하드코딩(CSS 변수 못 읽음) | ✗ — 양쪽을 함께 고쳐야 함 |
| Next.js App Router 구조 | 파일 기반 라우팅·서버 컴포넌트·`layout.tsx` | Vite + React SPA. `BrowserRouter`(`App.jsx:72-76`) + 하드코딩 라우트 5개(`App.jsx:56-65`). 공통 레이아웃은 `Shell()` 함수 하나 | ✗ |
| 구버전 전제 (Vite 5~6, React 18) | 최신화가 선행 과제 | Vite 8.0.15 / React·react-dom 19.2.6 / react-router-dom 7.16.0 / ESLint 10.4.1 — 전부 최신 메이저 | ✗ — 과제가 아님 |
| 화면 = 라우트 1:1 (B절) | 라우트마다 한 화면 | 라우트 5개, 실제 화면 7개. `/history` 탭 2개(state), `/order` 카드 2개 | △ — 라우트로 세면 2개 화면이 뭉개짐 |
| 파일 단위 소유권 경계 (F절) | '스타일만 수정' 경계가 파일로 나뉨 | 스타일이 `index.css` 1파일 + 페이지 내 인라인 178건에 양분. 경계가 페이지 파일 **내부를 관통** | ✗ — 아래 `### 커스터마이징 계층` 참조 |
| 컴포넌트별 CSS / CSS Modules | 스코프 격리 | 전역 1파일. 클래스 변경 파급을 JSX grep 호출자 추적으로만 알 수 있음 | ✗ |
| 리디자인 회귀를 테스트로 검증 | 테스트 존재 | 테스트 러너·`test` 스크립트 없음(`package.json:6-11`). `eslint-plugin-jsx-a11y`도 없어 수작업 `role`/`aria-label`(`AllocationDonut.jsx:68-69, 133`) 유실이 자동 감지되지 않음 | ✗ |
| `--font-mono` 지정 = JetBrains Mono 렌더 | 지정한 폰트가 표시됨 | 로딩 코드가 없어 실제로는 `ui-monospace`(OS 기본) 폴백 | ✗ — 기존 결함 |
| `var(--accent)` 토큰 참조 | 정의된 토큰을 가리킴 | `--accent`는 미정의. `DcaConfig.jsx:411, 562, 578`이 항상 폴백 `#6ea8fe`로 고정 렌더 | ✗ — 사실상 하드코딩 |
| 라우트 목록 = 화면 목록 | 목록만 옮기면 됨 | catch-all(404) 없어 미지 경로가 빈 화면(`App.jsx:56-65`), `/login`은 링크 없이 가드·인터셉터로만 도달 | △ — 이 두 화면 상태를 범위에 넣어야 함 |
| 색 표기 통일 필요 | hex/rgb/hsl/oklch 혼재 | hex + rgba 2종뿐으로 이미 일관 | ✗ — 과제가 아님 |

### 커스터마이징 계층 (직접 조작 범위 세분화용 기초자료)

`[조사]` 항목. "내가 직접 커스터마이징할 수 있는 범위"를 나누려면 **파일이 아니라 계층**으로 잘라야 한다. 이 프로젝트는 스타일이 `index.css`와 페이지 JSX 인라인에 양분돼 있어, 파일 단위 경계선이 성립하지 않기 때문이다.

| 계층 | 실제 위치 | 규모 | 바꾸면 닿는 범위 | 로직 접촉 위험 | 되돌리기 |
|---|---|---|---|---|---|
| **L1 토큰값** | `index.css:3-56` `:root` | 선언 36개 | 전체 화면 — 단 색상은 **91건 중 28건만** 이 경로를 탄다 | 없음 | 값 되돌리기 |
| **L2 스코프 오버라이드** | `index.css:754-767` `.manual-order--buy` | 선언 4개 + 하드코딩 hover 1 | 수동 주문 카드 매수 모드 전체 | 없음 (색 의미는 안전장치라 판단 필요) | 값 되돌리기 |
| **L3 컴포넌트 클래스 규칙** | `index.css:60-1150` | 클래스 77종 / 사용 256회 | 그 클래스를 쓰는 모든 화면. 파급은 JSX grep으로만 추적 | 없음 (단 클래스명 변경은 JSX 동반 수정) | 규칙 되돌리기 |
| **L4 JSX 인라인 스타일** | 페이지 7파일 | **178건** (DcaConfig 75 + Order 47 = 69%) | 그 한 요소만 | **있음** — 로직 파일 안을 편집. 조건부 스타일(`isSel ? … : …`)은 state에 붙어 있음 | 파일 단위 diff |
| **L5 마크업 구조** | 페이지 7파일 | 컴포넌트 10개 | 레이아웃·정보 위계 | **높음** — 이벤트 핸들러·조건부 렌더가 같은 JSX에 있음 | 파일 단위 diff |
| **L6 앱 셸 밖** | `vite.config.js:35, 40, 54, 55` / `index.html:1-14` | 하드코딩 색 4곳 | PWA 스플래시·상태바·설치 아이콘 | 없음 (빌드 재실행 필요) | 값 되돌리기 |

**여기서 나오는 사실 세 가지**

1. **L1만 만져서는 리디자인이 완결되지 않는다.** 색상 91건 중 63건(69%)이 사용 지점 리터럴이고, 간격·타이포 토큰은 아예 0개다.
2. **L4가 가장 크고 가장 위험하다.** 인라인 178건 중 122건이 `DcaConfig.jsx`·`Order.jsx` 두 파일에 있는데, 이 두 파일은 각각 738줄·1169줄로 적립 설정과 주문 실행 로직을 담고 있다.
3. **L1을 넓히는 선행 작업(리터럴 → 토큰 승격, 간격·타이포 스케일 신설)을 하면 이후 L1만으로 조작 가능한 범위가 커진다.** 이 선행 작업을 이번 범위에 넣을지가 A절의 실질적 분기점이다.

### A. 범위
`[결정]` 항목.

- 이번 작업 대상 화면:
- 대상에서 제외할 화면:
- 기존 코드 개선인가, 새로 작성인가:
- 절대 수정 금지 영역(로직·API·상태관리):
- 반응형 대상(데스크톱 전용 / 모바일 우선 / 양쪽):
- 다크 모드 지원 여부:
- 브랜치 전략과 롤백 지점:

### B. 정보 구조
`[결정]` 항목. 화면마다 아래 네 가지를 각각 답한다.

- 이 화면에서 3초 안에 전달할 단 하나:
- 표시할 데이터의 종류와 예상 개수:
- 읽기 전용인가, 조작이 있는가:
- 데이터 갱신 주기와 기준 시점 표기 필요 여부:

### C. 시그니처
`[결정]` 항목.

- 특색을 몰아줄 지점(하나만):
- 그 지점을 고른 근거:
- 나머지 영역은 기본값 근처로 둔다는 원칙의 적용 범위:

### D. 토큰 명세
`[결정]` 항목. 확정 수치로 적는다. 값을 정하기 어려운 항목은 "Claude Code가 후보 3개 제시 후 선택"이라고 명시한다.

- 색 토큰(OKLCH, 라이트):
- 색 토큰(OKLCH, 다크):
- `--radius`:
- 본문 폰트와 fallback:
- 숫자용 모노 폰트와 fallback:
- 간격 스케일(단계 수, 이름, 값):
- 타이포 스케일(허용할 폰트 크기 목록):
- 테두리 두께:
- 그림자 사용 여부:

### E. 금지 목록
`[결정]` 항목. 각 줄에 사유를 함께 적는다.

- 금지 항목과 사유:
- 조건부 허용 항목과 그 조건:

### F. 소유권 경계
`[결정]` 항목.

- 사용자 전용(Claude Code 수정 금지) 파일:
- 승인 후 수정 가능 파일:
- Claude Code 위임 파일:
- `components/ui/*.tsx` 수정 허용 범위:

### G. AutoInvesting 고유 결정 항목
`[결정]` 항목. 일반 웹 UI 프레임에 없지만 이 프로젝트 도메인상 반드시 정해야 하는 항목을 Claude Code가 먼저 **질문 형태로** 제시하고, 사용자가 답한다. 아래 14개가 제시된 질문이며 답은 비어 있다.

**G-1. 매수=빨강 / 매도=파랑 한국 관례를 유지하는가?**
`index.css:754-756` 주석에 의도가 명시된 설계이며 `.manual-order--buy`가 `--accent-blue`를 `--loss-red`로 스와핑한다(`:757-762`). 글로벌 관례(매수=초록)와 정반대라 팔레트 결정의 최상위 분기다.
→ 답:

**G-2. 손익 색과 주문 방향 색이 같은 화면에서 충돌하는 문제를 어떻게 처리하는가?**
대시보드에서 빨강은 '손실'(`index.css:352` `.badge-profit--down`)인데 주문 화면에서 빨강은 '매수'다. 색 하나가 두 의미를 갖는 상태를 유지할지 분리할지 정해야 한다.
→ 답:

**G-3. LIVE / PAPER / SIM 계좌 모드 배지의 시각적 위계를 어디까지 올리는가?**
`Dashboard.jsx:78-82, 98-132`에서 🔴/🟡/⚪ 분기와 "⚠️ 실제 자금이 거래됩니다" 문구를 인라인 style만으로 그려 CSS 클래스가 없다. 실전/모의 오인은 곧 실주문이므로 장식이 아니라 안전장치다.
→ 답:

**G-4. 되돌릴 수 없는 액션의 확인 UI를 한 계통으로 통일하는가?**
적립 실행·예약은 공용 `ConfirmDialog`(`Order.jsx:1145`, 4종 스펙 `:389-457`), 수동 주문은 로컬 `OrderConfirmModal`(`Order.jsx:56-224`)로 갈라져 있고 둘이 같은 `.modal-overlay`/`.modal-content`를 각자 쓴다. 주문 확인 화면을 한 곳에서 못 고치는 상태다.
→ 답:

**G-5. 양도세 추정(`sell-preview`) 표시를 매도 확인 흐름 안에 계속 둘 것인가, 얼마나 눈에 띄게 할 것인가?**
`Order.jsx:620`이 `/api/order/sell-preview`를 호출하고 `OrderConfirmModal`이 과세/비과세/취득가불명 5종 헤더(`Order.jsx:32-50`)와 세금 표를 그린다. 프로젝트 규칙상 Tax는 정보 전용이며 매수 의사결정에 값을 흘리면 안 되는 경계가 있어, 시각적으로도 '참고'임이 드러나야 하는지 정해야 한다.
→ 답:

**G-6. 월별 템플릿 배정 12개월 그리드와 집행 회차 마커(`○` / `●` / `●N`) 표현을 유지하는가?**
`DcaConfig.jsx:539-691`의 좌우 2단 구조이며, 네이티브 `select`를 일부러 버리고 칩 라디오그룹으로 만든 이유가 주석(`DcaConfig.jsx:580-582`)에 남아 있다. 체스터튼의 울타리 확인이 필요한 지점이다.
→ 답:

**G-7. 적립 지정일 선택(1~28 + "지정 안 함") `.day-grid` 칩 방식을 유지하는가?**
`Order.jsx:732-786`, 스타일은 `index.css:800`. 말일 보정 안내 문단이 함께 붙어 있어, 날짜 입력을 네이티브 컨트롤로 바꾸면 이 안내의 자리가 사라진다.
→ 답:

**G-8. 집행 가능 판정 한 줄(✅/⚠️/❌ 3분기)을 어떤 강도로 보여줄 것인가?**
`DcaConfig.jsx:247-261`(판정 로직) / `:486-521`(합계·판정 박스). 예산은 초과 경고용 상한일 뿐 수량을 줄이지 않는다는 프로젝트 원칙이라, ⚠️가 '차단'으로 오해되면 안 되는 제약이 있다.
→ 답:

**G-9. 금액 통화 토글(`$ USD` / `₩ KRW`)과 "원화는 현재 환율 환산 · 매입 시점 환율 아님" 경고를 어디에 둘 것인가?**
토글은 `Dashboard.jsx:135-146`(`.ccy-toggle`, `index.css:912`), 경고는 `Dashboard.jsx:215-226`의 카드 부제다. 같은 `.ccy-toggle` 모양을 `Order.jsx:842-866`의 매수/매도 토글이 재사용하고 있어, 한쪽을 바꾸면 다른 쪽 의미가 흔들린다.
→ 답:

**G-10. 숫자(금액·수량·비중) 정렬 규칙을 한 가지 표기로 통일하는가?**
현재 `font-feature-settings: 'tnum'`(`index.css:328, 346, 394`)과 `font-variant-numeric: tabular-nums`(`:727, 817, 877`) 두 표기가 섞여 있다. 자릿수가 흔들리면 잔고·수량 오독으로 이어진다.
→ 답:

**G-11. 시스템 로그 뷰어의 모노스페이스 폰트를 실제로 로딩할 것인가, 지정을 걷어낼 것인가?**
`--font-mono`(`index.css:40`)의 1·2순위 JetBrains Mono / Fira Code를 로딩하는 코드가 없어 실제로는 OS 기본 모노로 렌더된다. 사용처는 `index.css:1082`와 `History.jsx:204`(거래내역 셀)다.
→ 답:

**G-12. 로그 뷰어의 ERROR/WARN 줄 색 분기와 최신 줄 역순 표시를 유지하는가?**
`History.jsx:112-116`(색 분기), `:289-302`(역순 렌더). 장애 확인용 화면이라 가독성 우선순위가 일반 텍스트 블록과 다르다.
→ 답:

**G-13. 직접 그린 로그 달력(42칸 고정)을 유지하는가?**
`History.jsx:244-281`, 스타일 `index.css:827-904`. 네이티브 date input을 버린 이유와 42칸 고정(높이 출렁임 방지) 근거가 주석(`History.jsx:71-72`)에 남아 있다.
→ 답:

**G-14. PWA standalone 모드의 테마색·세이프에어리어를 이번 범위에 포함하는가?**
테마색이 `vite.config.js:35, 40, 54, 55`에 `#0b0e14`로 하드코딩돼 CSS 변수와 이원화돼 있고, `index.html:6`의 viewport에 `viewport-fit=cover`가 없으며 `<meta name="color-scheme">`도 없다(`index.html:3-9`). 홈 화면 설치 앱으로 쓰는 도구라 화면 색만 바꾸면 상태바·스플래시가 어긋난다.
→ 답:

### H. 완료 판정
`[결정]` 항목. 기계적으로 검증 가능한 형태로 적고, 검증 명령을 함께 적는다.

- 자동 검증 항목과 명령:
- 육안 승인 항목:

### I. 작업 순서
`[결정]` 항목.

- 화면 처리 순서와 근거:
- 커밋 단위:
- 단계별 중단 지점(사용자 확인이 필요한 시점):

## 정리 / 결론

### 현재 상태

- `[조사]` 항목 10개 전부 채움. 별도로 `### 용어 정의 불일치`(26행)와 `### 커스터마이징 계층`(L1~L6)을 추가했다.
- `[결정]` 항목(A~F, H, I)은 **한 칸도 채우지 않았다.** G절은 규칙대로 질문 14개만 제시하고 답을 비웠다.

### 조사에서 나온 결론

1. **이 결정목록의 어휘 절반이 이 코드베이스에 존재하지 않는다.** 문서는 Next.js + Tailwind + shadcn/ui + TypeScript를 전제하지만 실제는 Vite 8 + React 19 + 손으로 쓴 `index.css` 1150줄이다. `[결정]` 항목을 채우기 전에 `### 용어 정의 불일치` 표를 먼저 합의해야 한다.
2. **"토큰만 바꾸면 리디자인" 모델이 이 코드베이스에서는 성립하지 않는다.** 색상 91건 중 63건(69%)이 사용 지점 리터럴이고, 간격·타이포 토큰은 0개이며, JSX 인라인 스타일이 178건이다.
3. **소유권 경계를 파일로 그을 수 없다.** 스타일이 `index.css` 1파일과 페이지 JSX 내부에 양분돼 있어 경계가 파일 내부를 관통한다. `### 커스터마이징 계층`의 L1~L6이 파일 대신 쓸 축이다.

### 조사 중 발견한 기존 결함 3건 (리디자인과 별개)

이번 작업 범위에 넣을지는 `[결정]` 대상이다.

| 결함 | 위치 | 현재 증상 |
|---|---|---|
| `--accent` 토큰 미정의 | `DcaConfig.jsx:411, 562, 578` | `var(--accent, #6ea8fe)`가 항상 폴백으로 고정 렌더. 테마 오버라이드가 닿지 않음 |
| `--font-mono` 웹폰트 미로딩 | `index.css:40` 지정 / 로딩 코드 없음 | JetBrains Mono·Fira Code가 실제로는 OS 기본 모노로 대체 |
| catch-all 라우트 없음 | `App.jsx:56-65` | 미지 경로 진입 시 네비게이션만 있고 본문이 빈 화면 |

미사용 토큰 5개(`--bg-secondary`, `--bg-card-hover`, `--radius-xl`, `--shadow-glow-purple`, `--transition-slow`)도 함께 확인됐다.

### 다음 단계

1. `### 용어 정의 불일치` 표 합의 — 특히 Tailwind 도입 여부와 `globals.css` → `index.css` 치환.
2. G절 14개 질문 답변.
3. A~F, H, I 결정 항목 채우기.
4. 빈칸이 모두 없어지면 이 문서를 실행계획서로 발전시킨다.

## 참고

- `Frontend/src/index.css` — 저장소 유일 CSS 소스, 디자인 토큰 정의 지점
- `Frontend/src/App.jsx` — 라우트·네비게이션·인증 가드 단일 지점
- `Frontend/package.json` / `Frontend/vite.config.js` — 스택 버전과 PWA 테마색
- `.agents/rules/worklog.md` — 이 문서의 형식 규칙
- `.agents/rules/recommended_rules.md` — 판단 레이어 재도입 금지 (G-5·G-8의 제약 근거)
