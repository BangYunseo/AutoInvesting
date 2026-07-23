---
title: AutoInvesting
date: 2026-07-23
company: [개인]
tags: [프로젝트개요, DCA적립, ETF, KIS]
status: draft
---

# AutoInvesting

## 개요
> 해외 ETF 자동 적립(DCA) 투자 시스템 — ASP.NET Core Web API (.NET 8.0). 여러 매수 템플릿을 월별로 배정해 현재 월 템플릿대로 기계적으로 적립 매수하는 Headless 서비스다.

## 📌 프로젝트 개요

- **해외 ETF 자동 적립 매수 프로그램**
- 여러 매수 템플릿(종목별 고정 수량 + 예산)을 정의하고 월별로 배정해, 현재 월 템플릿대로 자동 적립 매수하는 Headless 백그라운드 서비스
- "판단"이 아니라 **"자동화"** 프로그램

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
├── appsettings.json                    # 통합 설정 파일 (Trading / Smtp / Resend / Kis / Security / Dca)
├── Dockerfile                          # 단일 컨테이너 (백엔드 + React 정적 서빙)
│
├── Core/                               # 핵심 비즈니스 로직
│   ├── IBrokerClient.cs                # 증권사 API 추상화 인터페이스
│   ├── SimBrokerClient.cs              # 시뮬레이션 구현체 (API 키 불필요)
│   ├── KisBrokerClient.cs              # KIS 실거래 구현체 (Polly 내결함성 적용)
│   ├── KisTokenManager.cs              # KIS OAuth 토큰 발급 + 만료 전 자동 갱신
│   ├── SessionManager.cs               # IBrokerClient(브로커) 생명주기 관리
│   ├── DcaAccumulationEngine.cs        # 적립식 매수 엔진 (판단/타이밍 없음, 정수 매수)
│   ├── DcaSettings.cs                  # 매수 템플릿·월배정·예산의 단일 읽기/쓰기 지점 (DB → appsettings 폴백)
│   └── DailyExecutionService.cs        # 적립 사이클 실행 진입점 (RunDcaCycleAsync)
│
├── Data/                               # 데이터 액세스 계층
│   ├── DBManager.cs                    # PostgreSQL 연결 관리 (Npgsql, Singleton + 마이그레이션)
│   ├── AppConfigManager.cs             # appsettings.json + 환경변수 + DB 우선순위 설정
│   ├── sql/
│   │   └── create_tables.sql           # DDL + 초기 마스터 데이터
│   ├── DTO/                            # Data Transfer Objects
│   │   ├── DcaTemplate.cs              # 매수 템플릿 DTO (Id, Name, BudgetKrw, Quantities)
│   │   ├── AssetMasterDto.cs           # 자산 마스터
│   │   ├── TradeHistoryDto.cs          # 거래 내역
│   │   ├── HoldingDto.cs               # 보유 종목 (잔고)
│   │   ├── PriceRangeDto.cs            # N일 가격 범위
│   │   ├── OhlcvDto.cs                 # OHLCV 일봉 데이터
│   │   ├── StrategySummaryDto.cs       # (레거시) 전략 요약 — 판단 레이어 잔재, 미사용
│   │   ├── AdaptiveThresholdStatusDto.cs # (레거시) 적응형 임계값 상태 — Phase 5 잔재, 미사용
│   │   └── DailyTokenUsageDto.cs       # (레거시) AI 토큰 사용량 — Phase 5 잔재, 미사용
│   └── DAO/                            # Data Access Objects
│       ├── TradeHistoryDAO.cs          # TB_TRADE_HISTORY CRUD
│       └── SystemLogDAO.cs             # TB_SYSTEM_LOG 조회 (시스템 로그)
│
├── Controllers/                        # REST API 컨트롤러
│   ├── AuthController.cs               # 단일 관리자 로그인 + 세션 토큰(7일) 발급
│   ├── ConfigController.cs             # 환경 설정 API
│   ├── HistoryController.cs            # 거래 내역 및 로그 API
│   ├── PortfolioController.cs          # 잔고 조회 API
│   ├── PriceController.cs              # 현재가 조회 겸 티커 검증 (/api/price/{ticker})
│   ├── DcaController.cs                # 적립 설정(매수 템플릿·월배정) 조회·저장 API
│   ├── OrderController.cs              # 적립 사이클 실행 + 수동 주문 API
│   └── TestController.cs               # 개발/테스트 전용 API (buy / send-test-email)
│
├── Utils/                              # 유틸리티 (모든 레이어 접근 가능)
│   ├── Logger.cs                       # Serilog 래퍼 (Info/Warn/Error/Fatal)
│   ├── NotificationService.cs          # 이메일 알림 (Resend HTTP API — Render의 SMTP 포트 차단 대응)
│   ├── ExchangeRateService.cs          # 환율 API (Frankfurter + fallback, 1시간 캐싱)
│   ├── CryptoUtil.cs                   # 시크릿 AES-256-GCM 암복호화 · 비밀번호 PBKDF2 해시 · 세션 토큰
│   ├── ApiKeyAuthAttribute.cs          # 전역 x-api-key 인증 필터
│   └── PublicEndpointAttribute.cs      # 전역 인증 필터 면제 표시 (로그인 등 공개 엔드포인트)
│
├── Frontend/                           # React SPA (Vite, Glassmorphism 디자인)
│   └── src/
│       ├── pages/                      # Login, Dashboard, DcaConfig, Order, History, Settings
│       └── components/                 # HoldingsTable, ProgressLoader
│
└── Documents/                          # 단일 문서 홈 (프로젝트 문서 전부)
    ├── DEVELOPMENT.md                  # 개발 진척도 + 전체 변경 이력
    ├── ONBOARDING_GUIDE.md             # 신규 개발자용 아키텍처 가이드
    ├── CODE_READING_GUIDE.md           # DCA 적립 코어 코드 흐름 가이드
    ├── CODE_MAP.md                     # 코드 맵 (regen-codemap.ps1로 재생성)
    ├── API_REFERENCE.md                # REST API 레퍼런스
    ├── API_REFERENCE_TABLE.md          # REST API 요약 표
    ├── modules/                        # 모듈별 이해 문서
    ├── analysis/                       # 백테스트·절세 분석 산출물
    └── worklog/                        # 기능 단위 작업 인계 보고서
```

> **참고 (레거시 데이터)**: `TB_MARKET_SNAPSHOT` 테이블과 `DBManager`의 관련 마이그레이션 코드는
> 과거 데이터 보존을 위해 DB 스키마에는 남아 있으나, 판단 레이어 제거에 따라 **현재는 어디서도
> 기록·조회하지 않습니다** (과거 레거시 데이터, 미사용).

---

## 🖥️ 아키텍처: Headless ASP.NET Core Web API

- **ASP.NET Core Web API** 기반의 Headless 서버 구조
- **순수 적립(DCA) 자동화 서버**
- Linux 서버 / Docker 환경에서 24시간 무인 동작

### 적립 실행 진입점

- `DailyExecutionService.RunDcaCycleAsync()`가 로그인 → 매수 템플릿·월배정 로드
- (`DcaSettings.Load` → 현재 월 템플릿 선택) → `DcaAccumulationEngine.AccumulateAsync()` 실행 → 이메일 보고서 발송을 수행

### 외부 크론잡 트리거

- 매수 주기(예: 매월 첫 거래일)에 외부 크론잡이 `POST /api/order/dca-run`을 호출하여 적립 사이클을 시작 (즉시 202 반환 후 백그라운드 처리)

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
- [x] `DcaAccumulationEngine` — 종목별 고정 수량 적립 매수 엔진 (순수함수 `PlanPurchases` + `AccumulateAsync`)
- [x] `DcaSettings` — 매수 템플릿(`DCA_TEMPLATES`)·월별 배정(`DCA_MONTH_MAP`)·예산 단일 관리 (DB `TB_APP_CONFIG` → 레거시 키/appsettings `Dca` 폴백, 자동 이관)
- [x] `DcaController` — `GET/PUT /api/dca/config` 매수 템플릿·월별 배정 조회·저장
- [x] `DcaTemplate` DTO — 매수 템플릿 (Id, Name, BudgetKrw, Quantities)
- [x] `DailyExecutionService` → `RunDcaCycleAsync`만 유지 (구 AI 평가/일일 보고서 제거)
- [x] `OrderController` → `POST /api/order/dca-run`(적립 사이클) + `POST /api/order/manual`(수동 주문)
- [x] 프론트 재구성 — 네비: 대시보드 / 적립 설정 / 주문·적립 / 거래 내역 / 설정
- [x] 적립 설정 모델 전환 — 종목별 고정 수량(주) 직접 지정, 티커 실시간 검증·현재가 표시(`/api/price/{ticker}`), 비중·금액 자동 계산(읽기 전용)

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

> 증권사 API 키 없이도 `SimBrokerClient`(시뮬레이션 모드 — `IS_PAPER_TRADING` 기본 켜짐)로 전체 적립 흐름을 테스트할 수 있습니다.
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

- 실제 값은 아래 환경변수 표를 참고해 `-e 이름="값"`으로 주입합니다(값은 커밋 금지).

### 4) 환경변수 (이름만 — 값은 여기에 적지 말 것)

> ⚠️ **보안(필수)**: API 키·시크릿·계좌번호·토큰·DB 접속문자열 등 **실제 값은 소스·커밋·이 문서에 절대 넣지 않습니다.**
> 값은 환경변수 또는 `appsettings.local.json`(gitignore 대상)에만 두고, 아래는 **변수 이름**만 정리한 것입니다. (`.agents/rules/security.md`)

| 변수 이름 | 용도 | 필수 여부 |
| --- | --- | --- |
| `DATABASE_URL` | PostgreSQL 접속 URI (미설정 시 `localhost` 기본 접속) | 선택 (로컬 기본값 사용 시 생략) |
| `MASTER_KEY` | 시크릿 AES-256-GCM 암복호화 + 세션 토큰 서명 키 (base64 32바이트) | 권장 (미설정 시 시크릿 평문 저장·로그인 불가) |
| `AUTH_TOKEN_SECRET` | 세션 토큰 서명 전용 키 (미설정 시 `MASTER_KEY`에서 파생) | 선택 |
| `API_ACCESS_KEY` | 크론이 보내는 `x-api-key` 헤더를 검증하는 서버 측 키 | 크론 트리거 사용 시 필수 |
| `IS_PAPER_TRADING` | 모의(`1`)/실전(`0`) 분기 (미설정 시 `appsettings.json > Trading:IsPaperTrading` = 모의) | 선택 |
| `KIS_APP_KEY` | 한국투자증권 APP KEY | 실전 전환 시 필수 |
| `KIS_APP_SECRET` | 한국투자증권 APP SECRET | 실전 전환 시 필수 |
| `KIS_ACCOUNT_NO` | KIS 계좌번호 (개인정보 — 소스 금지) | 실전 전환 시 필수 |
| `KIS_ACCOUNT_PROD` | KIS 계좌 상품코드 (기본 `01`) | 선택 |
| `KIS_SERVER` | KIS 서버 구분 (기본 `vps`) | 선택 |
| `RESEND_API_KEY` | 이메일 알림(Resend) API 키 | 선택 (알림 사용 시) |
| `ADMIN_EMAIL` | 알림 수신자 이메일 (개인정보 — 소스 금지) | 선택 (알림 사용 시) |
| `FRED_API_KEY` | FRED 거시지표 브리핑 조회 키 (표시 전용) | 선택 |

> `x-api-key` 값은 GitHub Actions에서 시크릿 `CRON_API_KEY`로 보관해 헤더로 전송하며, 서버는 이를 위 `API_ACCESS_KEY`와 비교합니다(두 값이 같아야 통과).

**설정 예시** (값은 자리표시자 — 본인 값으로 교체, 커밋 금지):

```powershell
# Windows PowerShell — 현재 세션에만 적용
$env:MASTER_KEY     = "<your-base64-32byte-key>"
$env:DATABASE_URL   = "<your-postgres-connection-uri>"
$env:API_ACCESS_KEY = "<your-cron-api-key>"
```

또는 `appsettings.local.json`(gitignore 대상)의 `Kis`/`Resend`/`Security` 섹션에 둘 수 있습니다.
