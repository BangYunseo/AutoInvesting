---
title: AutoInvesting
date: 2026-07-23
company: [개인]
tags: [프로젝트개요, DCA적립, ETF, KIS]
status: done
---

# AutoInvesting

## 개요
> 해외 ETF 자동 적립(DCA) 투자 시스템 — ASP.NET Core Web API (.NET 8.0). 여러 매수 템플릿(종목별 고정 수량 + 예산)을 월별로 배정해, 현재 월 템플릿대로 기계적으로 적립 매수하는 Headless 서비스다. **"판단"이 아니라 "자동화"** 프로그램.

**▶ [소개 페이지 — 화면·동작 흐름](https://bangyunseo.github.io/AutoInvesting/)**
`docs/`를 GitHub Pages로 서빙합니다 (저장소 public 전환 + Pages 활성화 후 열립니다). 스크린샷의 금액·수량은 마스킹 처리된 것입니다.

### 핵심 목적

| #   | 목적              | 설명                                                                                                                               |
| --- | ----------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| 1   | **자동 적립**     | 사용자가 직접 주문하지 않아도, 정해진 주기에 설정 수량대로 자동 매수                                                               |
| 2   | **판단 배제**     | "지금 살까 말까" 같은 타이밍 판단을 제거하고 규칙(고정 수량) 기반 매수                                                             |
| 3   | **템플릿·월배정** | 여러 매수 템플릿을 정의하고 월별로 배정 — 현재 월 템플릿의 종목별 고정 수량을 매 사이클 그대로 매수 (비중·금액은 표시용 자동 계산) |

### 증권사 API

| 항목      | 내용                                            |
| --------- | ----------------------------------------------- |
| 증권사    | **한국투자증권 (KIS)**                          |
| API 형태  | REST API (HTTPS)                                |
| 인증      | OAuth 2.0 (APP KEY / APP SECRET → Access Token) |
| 대상 시장 | 미국 해외주식 (NYSE, NASDAQ)                    |

> **참고**: KIS Developers 포털 — https://apiportal.koreainvestment.com/

---

## 🏗️ 프로젝트 구조

```
AutoInvesting/
├── Program.cs                          # 앱 진입점 (DI 등록, SPA fallback, 전역 예외 처리)
├── appsettings.json                    # 통합 설정 파일 (Trading / Smtp / Resend / Kis / Security / Dca / Tax)
├── Dockerfile                          # 단일 컨테이너 (백엔드 + React 정적 서빙)
│
├── .github/workflows/                  # 유일한 실행 트리거 (순수 curl — checkout 없음)
│   ├── daily-run.yml                   # 매일 KST 00:10 → POST /api/order/dca-run (월 1회는 엔진 가드가 보장)
│   ├── reconcile.yml                   # 매일 UTC 21:30(미장 마감 후) → POST /api/order/reconcile
│   └── gitleaks.yml                    # 시크릿 스캔 (트리거 아님)
│
├── Core/                               # 핵심 비즈니스 로직
│   ├── IBrokerClient.cs                # 증권사 API 추상화 인터페이스
│   ├── SimBrokerClient.cs              # 시뮬레이션 구현체 (API 키 불필요)
│   ├── KisBrokerClient.cs              # KIS 실거래 구현체 (Polly 내결함성 적용)
│   ├── KisTokenManager.cs              # KIS OAuth 토큰 발급 + 만료 전 자동 갱신
│   ├── SessionManager.cs               # IBrokerClient(브로커) 생명주기 관리
│   ├── DcaAccumulationEngine.cs        # 적립식 매수 엔진 (판단/타이밍 없음, 정수 매수)
│   ├── DcaSettings.cs                  # 매수 템플릿·월배정·예산의 단일 읽기/쓰기 지점 (DB → appsettings 폴백)
│   ├── DailyExecutionService.cs        # 적립 사이클 실행 진입점 (RunDcaCycleAsync)
│   └── TaxEstimator.cs                 # 매도 양도소득세 추정 (순수함수 / 정보·확인용)
│
├── Data/                               # 데이터 액세스 계층
│   ├── DBManager.cs                    # PostgreSQL 연결 관리 (Npgsql, Singleton + 기동 시 create_tables.sql 적용)
│   ├── AppConfigManager.cs             # appsettings.json + 환경변수 + DB 우선순위 설정
│   ├── sql/
│   │   └── create_tables.sql           # DDL(TB_TRADE_HISTORY/TB_APP_CONFIG/TB_MARKET_SNAPSHOT/TB_SYSTEM_LOG) + IS_PAPER_TRADING 기본값
│   ├── DTO/                            # Data Transfer Objects
│   │   ├── DcaTemplate.cs              # 매수 템플릿 DTO (Id, Name, BudgetKrw, Quantities)
│   │   ├── DcaCycleResult.cs           # 적립 사이클 결과 (체결·실패·예산경고)
│   │   ├── DcaBuyFailure.cs            # 적립 매수 실패 1건 (종목·수량·사유)
│   │   ├── TradeHistoryDto.cs          # 거래 내역
│   │   ├── HoldingDto.cs               # 보유 종목 (잔고)
│   │   └── SellTaxEstimateDto.cs       # 매도 양도세 추정 결과
│   └── DAO/                            # Data Access Objects
│       ├── TradeHistoryDAO.cs          # TB_TRADE_HISTORY CRUD
│       └── SystemLogDAO.cs             # TB_SYSTEM_LOG 조회 (시스템 로그)
│
├── Controllers/                        # REST API 컨트롤러
│   ├── AuthController.cs               # 단일 관리자 로그인 + 세션 토큰(7일) 발급
│   ├── HistoryController.cs            # 거래 내역 및 로그 API
│   ├── PortfolioController.cs          # 잔고 조회 API
│   ├── PriceController.cs              # 현재가 조회 겸 티커 검증 (/api/price/{ticker})
│   ├── DcaController.cs                # 적립 설정(매수 템플릿·월배정) 조회·저장 API
│   ├── OrderController.cs              # dca-run(적립) · reconcile(체결 대사) · dca-schedule(당월 상태·추가적립 예약) · manual(수동 주문) · sell-preview(양도세 프리뷰)
│   └── TestController.cs               # 개발/테스트 전용 API (send-test-email — 실주문 경로 없음)
│
├── Utils/                              # 유틸리티 (모든 레이어 접근 가능)
│   ├── Logger.cs                       # Serilog 래퍼 (Info/Warn/Error/Fatal)
│   ├── NotificationService.cs          # 이메일 알림 (Resend HTTP API — Render의 SMTP 포트 차단 대응)
│   ├── ExchangeRateService.cs          # 환율 API (Frankfurter → ExchangeRate-API 폴백, 1시간 캐싱)
│   ├── CryptoUtil.cs                   # 시크릿 AES-256-GCM 암복호화 · 비밀번호 PBKDF2 해시 · 세션 토큰
│   ├── ApiKeyAuthAttribute.cs          # 전역 인증 필터 (Bearer 세션토큰 또는 x-api-key)
│   ├── LoginThrottle.cs                # 로그인 실패 전역 속도 상한 (분당 20회)
│   └── PublicEndpointAttribute.cs      # 인증 면제 표시 (/api/auth/status·login 둘뿐)
│
├── Tests/                              # xUnit 테스트 (별도 AutoInvest.Tests.csproj — 웹 빌드에서 제외)
│
├── Frontend/                           # React SPA (Vite, Glassmorphism 디자인)
│   └── src/
│       ├── pages/                      # Login, Dashboard, DcaConfig, Order, History
│       ├── components/                 # HoldingsTable, AllocationDonut, ConfirmDialog
│       └── utils/                      # dcaRuns.js (적립 실행 상태 조회)
│
├── docs/                               # GitHub Pages 소개 페이지 (index.html — 스크린샷 인라인, 금액 마스킹)
│
└── Documents/                          # 단일 문서 홈 (프로젝트 문서 전부)
    ├── reference/                      # 상시 참조 문서 (고정 이름)
    │   ├── DEVELOPMENT.md              # 개발 진척도 + 전체 변경 이력
    │   ├── ONBOARDING_GUIDE.md         # 신규 개발자용 아키텍처 가이드
    │   ├── CODE_READING_GUIDE.md       # DCA 적립 코어 코드 흐름 가이드
    │   ├── CODE_MAP.md                 # 코드 맵 (regen-codemap.ps1로 재생성)
    │   ├── CONFIG_REFERENCE.md         # 설정 키 단일 진실 원천 (환경변수·DB 전용 키·실전 전환)
    │   ├── RECOVERY.md                 # 운영 복구 절차 (이름·출처·순서 — 값 없음)
    │   └── API_REFERENCE.md            # REST API 레퍼런스 (인터랙티브 명세는 /swagger)
    ├── modules/                        # 모듈별 이해 문서
    ├── analysis/                       # 백테스트·절세 분석 산출물
    └── worklog/                        # 기능 단위 작업 인계 보고서
```

---

## 🖥️ 아키텍처: Headless ASP.NET Core Web API

- **ASP.NET Core Web API** 기반의 Headless 서버 구조
- **순수 적립(DCA) 자동화 서버**
- Linux 서버 / Docker 단일 컨테이너로 동작하되 **상주 타이머(인앱 스케줄러)는 두지 않습니다** — 배포처(Render 무료 인스턴스)는 유휴 시 프로세스가 멈춰 `BackgroundService` 타이머가 오류 없이 죽습니다 (`.agents/rules/architecture.md`)

### 적립 실행 진입점

- `DailyExecutionService.RunDcaCycleAsync()`가 **월 1회 멱등 가드**(DB 전용 `DCA_LAST_RUN_MONTH` — 조회 실패 시 매수 중단) → **적립 지정일 게이트**(`DCA_RUN_DAY`) → 로그인 → 매수 템플릿·월배정 로드
- (`DcaSettings.Load` → 현재 월 템플릿 선택) → `DcaAccumulationEngine.AccumulateAsync()` 실행 → 이메일 보고서 발송을 수행

### 외부 크론잡 트리거 (GitHub Actions — 유일한 실행 경로)

- `daily-run.yml` — **매일** KST 00:10(`10 15 1-31 * *`)에 `/api/health`로 잠든 인스턴스를 깨운 뒤 `POST /api/order/dca-run` 호출 (즉시 202 반환 후 백그라운드 처리)
- **월 1회는 크론이 아니라 코드가 보장합니다** — 위 멱등 가드가 당월 1회만 통과시킵니다. 접수 0건인 날은 마커를 남기지 않아 다음 날 자동 재시도되므로, 크론 주기를 "매월 1일"로 바꾸지 마세요
- `reconcile.yml` — 매일 UTC 21:30(미장 마감 후) `POST /api/order/reconcile` → `ReconcileAsync()`가 주문 전후 보유 수량 차이로 체결을 판정하고, **전량 미체결일 때만** 마커를 해제해 재시도를 허용

### REST API 컨트롤러

- React 웹 대시보드 및 외부 클라이언트에서 적립 설정 편집, 잔고/내역 조회, 수동 주문 제공

### 배포

- 단일 Docker 컨테이너 — ASP.NET Core가 React SPA 빌드 결과를 정적 파일로 서빙 (SPA 라우팅 지원)

---

## 💱 환율 API

| 항목     | 내용                                                       |
| -------- | ---------------------------------------------------------- |
| API      | **Frankfurter API** (ECB 데이터)                           |
| URL      | `https://api.frankfurter.app/latest?from=USD&to=KRW`       |
| API 키   | 불필요 (무료)                                              |
| Fallback | ExchangeRate-API (`https://open.er-api.com/v6/latest/USD`) |
| 캐싱     | 1시간                                                      |
| 사용처   | 적립 매수 시 USD→KRW 환산, 대시보드 환율 카드              |

---

## 📊 적립(DCA) 매수 방식

- **여러 매수 템플릿**을 정의하고 **월별로 배정**해, 현재 월 템플릿의 **종목별 고정 수량**을 매 사이클 그대로 매수

| 항목                  | 설명                                                                                                      |
| --------------------- | --------------------------------------------------------------------------------------------------------- |
| **매수 템플릿**       | 종목별 고정 수량 + 예산을 묶은 단위 (`DcaTemplate`: Id, Name, BudgetKrw, Quantities) — 여러 개 정의 가능  |
| **월별 배정**         | 1~12월 각각에 템플릿을 배정(`DCA_MONTH_MAP`) — 배정 없으면 첫 템플릿 사용, 템플릿 없는 달은 매수 스킵     |
| **매수 수량**         | 현재 월 템플릿의 종목별 매수 주수 (예: `QQQM 2주 / SPLG 3주 / SCHD 5주`) — 적립 설정 페이지에서 직접 지정 |
| **비중·금액(표시용)** | 비중(%)·매수금액은 수량 × 현재가로 자동 계산되어 **표시만** 됨 (사람이 조절 불가)                         |
| **티커 검증**         | `GET /api/price/{ticker}`로 현재가가 확인된 종목만 등록·저장 (우측에 실시간 가격 표시)                    |
| **예산**              | 템플릿별 예산은 **초과 경고용 상한** — 총 매수금액이 예산을 넘으면 경고만(수량은 그대로 매수)             |
| **순수 함수**         | 매수 계획(`PlanPurchases`)은 외부 I/O 없는 순수 함수로 분리되어 단위 검증 가능                            |

---

## ⚙️ 기술 스택

| 분류          | 기술                                         |
| ------------- | -------------------------------------------- |
| 언어 (백엔드) | C#                                           |
| 프레임워크    | ASP.NET Core Web API (.NET 8.0)              |
| 프론트엔드    | React (Vite, JSX, Glassmorphism 디자인)      |
| DB            | PostgreSQL (Npgsql)                          |
| 로깅          | Serilog                                      |
| 내결함성      | Polly (KIS API Retry + 지수 백오프)          |
| 이메일 알림   | Resend HTTP API (Render의 SMTP 포트 차단 대응) |
| 증권사 API    | 한국투자증권 (KIS) REST API                  |
| 환율 API      | Frankfurter API (무료, 키 불필요)            |
| 배포          | Docker (단일 컨테이너, React 정적 서빙 통합) |
| 빌드          | MSBuild / Visual Studio 2022                 |

---

## 🚀 개발 진척도

Phase 1~5(WinForms 기반 → 퀀트/AI 판단 레이어)와 Phase 6(판단 레이어 전면 제거 → DCA 적립 코어) 전체 이력은 `Documents/reference/DEVELOPMENT.md`에 있습니다. **현재 동작 아키텍처는 Phase 6 하나뿐이며, Phase 2~5의 산출물(`SmartOrderEngine`, `Core/Quant/*`, `Core/Advisors/*`, AI 분석기, 리밸런싱, WinForms)은 코드에 존재하지 않습니다.**

---

## 🔧 로컬 실행

로컬 PostgreSQL이 필요합니다 — 기본 접속 `localhost`, DB명 `autoinvest`. 테이블은 최초 실행 시 `Data/sql/create_tables.sql`로 자동 생성됩니다(배포 시 `DATABASE_URL` 환경변수 사용).

### 1) 백엔드 실행

**Visual Studio 2022**

1. `AutoInvest.sln` 열기
2. NuGet 패키지 복원
3. `F5`로 디버그 실행

**CLI (.NET 8 SDK)**

```bash
dotnet restore
dotnet build
dotnet run          # ASP.NET Core 호스트 기동
```

> 프론트 개발 서버(Vite)는 `/api` 요청을 `http://localhost:5000`으로 프록시합니다. 프론트와 함께 쓰려면 백엔드를 `:5000`으로 띄우세요 — 환경변수 `ASPNETCORE_URLS`를 `http://localhost:5000`으로 설정.

> `KIS_APP_KEY`가 비어 있으면 모드와 무관하게 `SimBrokerClient`(로컬 시뮬레이션)로 떨어지므로, 증권사 API 키 없이 전체 적립 흐름을 테스트할 수 있습니다. `IS_PAPER_TRADING`은 Sim/KIS 선택이 아니라 KIS 접속망 prod(`:9443`)/vps(`:29443`)만 고릅니다 — 키가 있는데 `1`이면 로컬 시뮬레이터가 아니라 실제 KIS 모의계좌로 주문이 갑니다.
> 적립 실행은 `POST /api/order/dca-run`(또는 프론트 "주문·적립" 페이지)으로 트리거합니다.

### 2) 프론트엔드 (`Frontend/`)

```bash
cd Frontend
npm install
npm run dev        # Vite 개발 서버 — /api 요청을 http://localhost:5000(백엔드)으로 프록시
# 또는
npm run build      # 운영 정적 산출물(Frontend/dist) 생성 — 배포 시 백엔드 wwwroot로 서빙
```

- **개발 중(권장)**: 백엔드(`:5000`)와 Vite 개발 서버를 함께 띄우고, 브라우저는 Vite 개발 서버 주소로 접속합니다(`/api`는 자동 프록시).
- **통합 서빙 확인**: `npm run build`로 만든 `Frontend/dist`를 백엔드 `wwwroot/`로 복사하면 백엔드 단독으로 SPA까지 서빙합니다(Docker는 이 복사를 자동 수행).

### 3) Docker (단일 컨테이너 — 운영과 동일)

프론트 빌드 → 백엔드 빌드 → 정적 서빙을 하나의 컨테이너에 담습니다(`Dockerfile`). 컨테이너는 `:5000`을 노출하고 타임존은 KST(Asia/Seoul)로 고정됩니다.

```bash
docker build -t autoinvesting .
docker run --rm -p 5000:5000 \
  -e DATABASE_URL="<your-postgres-connection-uri>" \
  -e MASTER_KEY="<your-base64-32byte-key>" \
  autoinvesting
```

- 실제 값은 아래 환경변수 절과 `Documents/reference/CONFIG_REFERENCE.md`를 참고해 `-e 이름="값"`으로 주입합니다(값은 커밋 금지).

### 4) 환경변수 (이름만 — 값은 여기에 적지 말 것)

> ⚠️ **보안(필수)**: API 키·시크릿·계좌번호·토큰·DB 접속문자열의 **실제 값은 소스·커밋·이 문서에 절대 넣지 않습니다.** 값은 환경변수 또는 `appsettings.local.json`(gitignore 대상)에만 둡니다. (`.agents/rules/security.md`)

로컬 기동에 필요한 최소 3개만 아래에 둡니다. **전체 설정 키 목록·조회 우선순위(환경변수 → DB → appsettings)·실전 전환 절차는 `Documents/reference/CONFIG_REFERENCE.md`가 단일 진실 원천입니다** — 키를 추가·삭제할 때는 그 문서만 고칩니다.

| 변수 이름 | 용도 | 필수 여부 |
| --- | --- | --- |
| `DATABASE_URL` | PostgreSQL 접속 URI (미설정 시 `localhost` 기본 접속) | 선택 |
| `MASTER_KEY` | 시크릿 AES-256-GCM 암복호화 + 세션 토큰 서명 키 (base64 32바이트) | 권장 (미설정 시 로그인 불가) |
| `API_ACCESS_KEY` | 크론의 `x-api-key` 검증 키. 최초 관리자 설정(`POST /api/auth/setup`)의 유일한 통과 수단 | 크론·부트스트랩 시 필수 |

```powershell
# Windows PowerShell — 현재 세션에만 적용 (값은 자리표시자)
$env:MASTER_KEY     = "<your-base64-32byte-key>"
$env:DATABASE_URL   = "<your-postgres-connection-uri>"
$env:API_ACCESS_KEY = "<your-cron-api-key>"
```

> 운영 설정 변경 경로는 **Render 환경변수 수정 + 재배포** 하나뿐입니다(설정 화면·설정 API는 2026-08-06 제거). 현재 계좌 모드(LIVE/PAPER/SIM)는 대시보드 상단 배지에서 확인합니다.
