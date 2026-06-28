# AutoInvesting

> 해외 ETF 자동 적립(DCA) 투자 시스템 — ASP.NET Core Web API (.NET 8.0)

## 📌 프로젝트 개요

설정한 목표비중대로 해외 ETF를 자동으로 적립 매수하는 Headless 백그라운드 서비스입니다.
정직한 백테스트(2012~현재) 결과 **퀀트/AI 타이밍 판단이 단순 적립식(DCA)에 2.7~4배 열세**였고,
완벽한 타이밍조차 평균 대비 연 +0.3~0.9%에 불과(타이밍은 잘해야 본전)함이 검증되었습니다.
이에 따라 **판단(타이밍) 레이어를 전부 제거**하고, 정해진 목표비중을 향해 **정수 단위로 매수만 하는
DCA 적립 코어**로 전환했습니다. 이 시스템의 가치는 "판단"이 아니라 **"자동화"**에 있습니다.

### 핵심 목적

| # | 목적 | 설명 |
|---|------|------|
| 1 | **자동 적립** | 사용자가 직접 주문하지 않아도, 정해진 주기에 목표비중대로 자동 매수 |
| 2 | **판단 배제** | "지금 살까 말까" 같은 타이밍 판단을 제거하고 규칙(목표비중) 기반 매수 |
| 3 | **정수 단위 매수** | 소수점 매수 없이 정수 주수로만 매수, 남는 예산(잔돈)은 다음 사이클로 이월 |

### 증권사 API

| 항목 | 내용 |
|------|------|
| 증권사 | **한국투자증권 (KIS)** |
| API 형태 | REST API (HTTPS) |
| 인증 | OAuth 2.0 (APP KEY / APP SECRET → Access Token) |
| 대상 시장 | 미국 해외주식 (NYSE, NASDAQ) |

> **참고**: KIS Developers 포털 — https://apiportal.koreainvestment.com/

---

## 🏗️ 프로젝트 구조

```
AutoInvesting/
├── Program.cs                          # 앱 진입점 (DI 등록, SPA fallback, 전역 예외 처리)
├── appsettings.json                    # 통합 설정 파일 (Trading / Smtp / Kis / Security / Dca)
├── Dockerfile                          # 단일 컨테이너 (백엔드 + React 정적 서빙)
│
├── Core/                               # 핵심 비즈니스 로직
│   ├── IBrokerClient.cs                # 증권사 API 추상화 인터페이스
│   ├── SimBrokerClient.cs              # 시뮬레이션 구현체 (API 키 불필요)
│   ├── KisBrokerClient.cs              # KIS 실거래 구현체 (Polly 내결함성 적용)
│   ├── KisTokenManager.cs              # KIS OAuth 토큰 발급 + 만료 전 자동 갱신
│   ├── SessionManager.cs               # IBrokerClient(브로커) 생명주기 관리
│   ├── DcaAccumulationEngine.cs        # 적립식 매수 엔진 (판단/타이밍 없음, 정수 매수)
│   ├── DcaSettings.cs                  # 목표비중·예산의 단일 읽기/쓰기 지점 (DB → appsettings 폴백)
│   └── DailyExecutionService.cs        # 적립 사이클 실행 진입점 (RunDcaCycleAsync)
│
├── Data/                               # 데이터 액세스 계층
│   ├── DBManager.cs                    # PostgreSQL 연결 관리 (Npgsql, Singleton + 마이그레이션)
│   ├── AppConfigManager.cs             # appsettings.json + 환경변수 + DB 우선순위 설정
│   ├── sql/
│   │   └── create_tables.sql           # DDL + 초기 마스터 데이터
│   ├── DTO/                            # Data Transfer Objects
│   │   ├── AssetDto.cs                 # 자산 마스터
│   │   ├── TradeHistoryDto.cs          # 거래 내역
│   │   ├── HoldingDto.cs               # 보유 종목 (잔고)
│   │   ├── PriceRangeDto.cs            # N일 가격 범위
│   │   └── OhlcvDto.cs                 # OHLCV 일봉 데이터
│   └── DAO/                            # Data Access Objects
│       ├── AssetDAO.cs                 # TB_ASSET_MASTER 조회
│       └── TradeHistoryDAO.cs          # TB_TRADE_HISTORY CRUD
│
├── Controllers/                        # REST API 컨트롤러
│   ├── ConfigController.cs             # 환경 설정 API
│   ├── HistoryController.cs            # 거래 내역 및 로그 API
│   ├── PortfolioController.cs          # 잔고 조회 API
│   ├── DcaController.cs                # 적립 설정(목표비중·예산) 조회·저장 API
│   ├── OrderController.cs              # 적립 사이클 실행 + 수동 주문 API
│   └── TestController.cs               # 개발/테스트 전용 API (buy / send-test-email)
│
├── Utils/                              # 유틸리티 (모든 레이어 접근 가능)
│   ├── Logger.cs                       # Serilog 래퍼 (Info/Warn/Error/Fatal/LogQuant)
│   ├── NotificationService.cs          # MailKit Naver SMTP 이메일 알림
│   ├── ExchangeRateService.cs          # 환율 API (Frankfurter + fallback, 1시간 캐싱)
│   ├── ApiKeyAuthAttribute.cs          # 전역 x-api-key 인증 필터
│   └── DateTimeHelper.cs               # NYSE 개장시각(KST) 계산 (DST 대응)
│
├── Frontend/                           # React SPA (Vite, Glassmorphism 디자인)
│   └── src/
│       ├── pages/                      # Dashboard, DcaConfig, Order, History, Settings
│       └── components/                 # HoldingsTable 등
│
└── Documents/                          # 프로젝트 문서
    ├── DEVELOPMENT.md                  # 개발 진척도 + 전체 변경 이력
    ├── ONBOARDING_GUIDE.md             # 신규 개발자용 아키텍처 가이드
    └── CODE_READING_GUIDE.md           # DCA 적립 코어 코드 흐름 가이드
```

> **참고 (레거시 데이터)**: `TB_MARKET_SNAPSHOT` 테이블과 `DBManager`의 관련 마이그레이션 코드는
> 과거 데이터 보존을 위해 DB 스키마에는 남아 있으나, 판단 레이어 제거에 따라 **현재는 어디서도
> 기록·조회하지 않습니다** (과거 레거시 데이터, 미사용).

---

## 🖥️ 아키텍처: Headless ASP.NET Core Web API

기존 WinForms 기반에서 **ASP.NET Core Web API** 기반의 Headless 서버로 구조가 개편되었고,
Phase 6에서 판단 레이어를 제거하여 **순수 적립(DCA) 자동화 서버**가 되었습니다.
UI 스레드 종속성을 제거하여 Linux 서버 / Docker 환경에서 24시간 무인으로 동작합니다.

- **적립 실행 진입점**: `DailyExecutionService.RunDcaCycleAsync()`가 로그인 → 목표비중/예산 로드
  (`DcaSettings.Load`) → `DcaAccumulationEngine.AccumulateAsync()` 실행 → 이메일 보고서 발송을 수행
- **외부 크론잡 트리거**: 매수 주기(예: 매월 첫 거래일)에 외부 크론잡이 `POST /api/order/dca-run`을
  호출하여 적립 사이클을 시작 (즉시 202 반환 후 백그라운드 처리)
- **REST API 컨트롤러**: React 웹 대시보드 및 외부 클라이언트에서 적립 설정 편집, 잔고/내역 조회, 수동 주문 제공
- **배포**: 단일 Docker 컨테이너 — ASP.NET Core가 React SPA 빌드 결과를 정적 파일로 서빙 (SPA 라우팅 지원)

---

## 💱 환율 API

| 항목 | 내용 |
|------|------|
| API | **Frankfurter API** (ECB 데이터) |
| URL | `https://api.frankfurter.app/latest?from=USD&to=KRW` |
| API 키 | 불필요 (완전 무료) |
| Fallback | ExchangeRate-API (`https://open.er-api.com/v6/latest/USD`) |
| 캐싱 | 1시간 |
| 사용처 | 적립 매수 시 USD→KRW 환산, 대시보드 환율 카드 |

---

## 📊 적립(DCA) 매수 방식

타이밍 판단을 하지 않고, **목표비중 바스켓**을 향해 정수 단위로 매수합니다.

| 항목 | 설명 |
|------|------|
| **목표비중** | 종목별 목표 비율 (예: `SPLG 0.4 / QQQM 0.3 / SCHD 0.2 / GLD 0.1`) |
| **배분 규칙** | "현재 비중이 목표 대비 가장 부족한 종목"을 1주씩 매수, 더 못 살 때까지 반복 |
| **정수 매수** | 소수점 매수 없음 → 1주 단가가 비싼 종목은 잔돈이 모일 때까지 자연히 건너뜀 |
| **잔돈 이월** | 1주도 못 사는 남은 예산은 다음 사이클로 이월(미체결) |
| **순수 함수** | 배분 계산(`PlanPurchases`)은 외부 I/O 없는 순수 함수로 분리되어 단위 검증 가능 |

---

## ⚙️ 기술 스택

| 분류 | 기술 |
|------|------|
| 언어 (백엔드) | C# |
| 프레임워크 | ASP.NET Core Web API (.NET 8.0) |
| 프론트엔드 | React (Vite, JSX, Glassmorphism 디자인) |
| DB | PostgreSQL (Npgsql) |
| 로깅 | Serilog |
| 내결함성 | Polly (KIS API Retry + 지수 백오프) |
| 이메일 알림 | MailKit (Naver SMTP) |
| 증권사 API | 한국투자증권 (KIS) REST API |
| 환율 API | Frankfurter API (무료, 키 불필요) |
| 배포 | Docker (단일 컨테이너, React 정적 서빙 통합) |
| 빌드 | MSBuild / Visual Studio 2022 |

---

## 🚀 개발 로드맵

### Phase 1 — 기반 (✅ 완료)
- [x] 프로젝트 생성 및 PostgreSQL 연동
- [x] DB 스키마 및 초기 마스터 데이터
- [x] DTO / DAO 레이어
- [x] 메인 대시보드 UI (사이드바, 카드, 로그)
- [x] 설정 폼 / 거래 내역 폼

### Phase 2 ~ 2.6 — 엔진 코어 + 퀀트 모듈 + 구조 리팩토링 (✅ 완료)
- [x] `IBrokerClient` / `SimBrokerClient` / `SmartOrderEngine` / `SessionManager`
- [x] 퀀트 엔진(`QuantIndicator`, `QuantFilter`, `BacktestEngine`, `RebalancingEngine`)
- [x] Weight → Qty(수량 정수) 전환, 무료 환율 API(Frankfurter) 연동
- [x] 멀티 Form → 단일 창 Panel(SPA) UI 전환, 레거시 Form 제거

### Phase 3 — KIS 실거래 연동 (✅ 완료)
- [x] `KisBrokerClient` — KIS REST API 실제 구현
- [x] OAuth 토큰 발급 + 자동 갱신 (`KisTokenManager`)
- [x] 실시간 시세/잔고 조회 및 주문 실행

### Phase A / B / C — Web API 전환 · 운영 안정성 · React 연동 (✅ 완료)
- [x] WinForms 레거시 완전 제거, Headless Web API로 전환
- [x] KIS API 내결함성(Polly 지수 백오프) 적용, MailKit 체결/예외 알림
- [x] React-Router 기반 SPA 프론트엔드 + Glassmorphism 디자인 시스템

### Phase 4 ~ 5 — AI 시장분석 / 적응형 임계값 / 성과 피드백 (✅ 완료 → Phase 6에서 제거)
- [x] Gemini 이중 에이전트(차트+펀더멘털) 합의, 확률 기반 합의 스코어링
- [x] 종목별 적응형 임계값, AI 성과·토큰 비용 모니터링, 성과 피드백 루프
- [x] **검증 결과 타이밍 판단의 실효성이 없음이 드러나 Phase 6에서 전부 제거됨**

### Phase 6 — 판단 레이어 제거, DCA 적립 코어 전환 (✅ 완료)
- [x] 퀀트/AI 판단 레이어(`SmartOrderEngine`, `Core/Quant/*`, `Core/Advisors/*`, AI 분석기) 전체 제거
- [x] `DcaAccumulationEngine` — 목표비중 향한 정수 단위 적립 매수 엔진 (순수함수 `PlanPurchases` + `AccumulateAsync`)
- [x] `DcaSettings` — 목표비중·예산 단일 관리 (DB `TB_APP_CONFIG` → appsettings `Dca` 폴백)
- [x] `DcaController` — `GET/PUT /api/dca/config` 적립 설정 조회·저장
- [x] `DailyExecutionService` → `RunDcaCycleAsync`만 유지 (구 AI 평가/일일 보고서 제거)
- [x] `OrderController` → `POST /api/order/dca-run`(적립 사이클) + `POST /api/order/manual`(수동 주문)
- [x] 프론트 재구성 — 네비: 대시보드 / 적립 설정 / 주문·적립 / 거래 내역 / 설정

---

## 🔧 로컬 실행

1. Visual Studio 2022에서 `AutoInvest.sln` 열기
2. NuGet 패키지 복원
3. `F5`로 디버그 실행 (로컬 PostgreSQL 필요 — 기본 접속: `localhost`, DB명 `autoinvest`. 테이블은 `create_tables.sql`로 자동 생성. 배포 시 `DATABASE_URL` 환경변수 사용)

> 증권사 API 키 없이도 SimBrokerClient(시뮬레이션 모드)로 전체 적립 흐름을 테스트할 수 있습니다.
> 적립 실행은 `POST /api/order/dca-run`(또는 프론트 "주문·적립" 페이지)으로 트리거합니다.
