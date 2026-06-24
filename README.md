# AutoInvesting

> 해외 ETF 자동 투자 시스템 — ASP.NET Core Web API (.NET 8.0)

## 📌 프로젝트 개요

설정한 시각에 자동으로 해외 ETF를 매수·매도하는 Headless 백그라운드 서비스입니다.
**퀀트 엔진**을 통해 RSI, MACD, 볼린저밴드, Position 등 다중 기술적 지표를 분석하고,
모든 조건을 만족할 때만 주문을 실행하여 **감정을 배제한 데이터 기반 투자**를 실현합니다.

> **현재 동작(퀀트 단독)**: 매매 결정은 **퀀트 신호(`QuantFilter`)만으로** 이루어집니다.
> Phase 4~6에서 개발한 AI 결정 경로(차트AI+펀더멘털AI 합의, 적응형 임계값, 확률 스코어링)는
> **코드에 주석으로 비활성화(보존)** 되어 현재 매매에 사용되지 않습니다(휴면). 분석/실행 중 AI 호출은 없습니다.
> 환율(USD/KRW)은 매수·매도 시 유불리를 알려주는 **설명·경고 전용 컨텍스트**로 참여하며, 매매를 막지는 않습니다.

### 핵심 목적

| # | 목적 | 설명 |
|---|------|------|
| 1 | **자동 투자** | 사용자가 직접 주문하지 않아도, 설정된 시각에 자동으로 매매 실행 |
| 2 | **감정 배제** | "더 오를거다/내릴거다" 같은 심리적 판단을 제거하고 규칙 기반 매매 |
| 3 | **계산식 기반 투자** | 퀀트 지표·계산식을 활용한 정량적 데이터 기반 매매 (AI 결정 경로는 현재 휴면) |

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
├── appsettings.json                    # 통합 설정 파일
├── Dockerfile                          # 단일 컨테이너 (백엔드 + React 정적 서빙)
│
├── Core/                               # 핵심 비즈니스 로직
│   ├── IBrokerClient.cs                # 증권사 API 추상화 인터페이스
│   ├── SimBrokerClient.cs              # 시뮬레이션 구현체 (API 키 불필요)
│   ├── KisBrokerClient.cs              # KIS 실거래 구현체 (Polly 내결함성 적용)
│   ├── KisTokenManager.cs              # KIS OAuth 토큰 발급 + 만료 전 자동 갱신
│   ├── SessionManager.cs               # IBrokerClient/IMarketAnalyzer 생명주기 관리
│   ├── SmartOrderEngine.cs             # 퀀트 신호(QuantFilter)만으로 매수/매도/보류 판정 (AI 합의 경로는 주석 비활성화·휴면)
│   ├── DailyExecutionService.cs        # 일별 매매 스케줄 실행 진입점 (Scoped)
│   ├── AllocationEngine.cs             # 투자금 배분 계산 엔진
│   ├── Advisors/                       # 컨텍스트 어드바이저 (매매 veto 없이 설명·경고 전용)
│   │   ├── IContextAdvisor.cs          # 어드바이저 인터페이스
│   │   ├── FxRateAdvisor.cs            # 환율(USD/KRW) 유불리 설명·경고 (매수/매도 컨텍스트)
│   │   └── ContextAdvisorService.cs    # 어드바이저 수집·실행
│   ├── IMarketAnalyzer.cs              # AI 시장 분석 인터페이스 (휴면 — 결정 경로 미사용)
│   ├── AiMarketAnalyzer.cs             # Mock AI 구현체 (휴면)
│   ├── GeminiMarketAnalyzer.cs         # Gemini API 이중 에이전트 (휴면 — 현재 AI 호출 안 함)
│   ├── IMcpDataProvider.cs             # MCP 외부 데이터 공급자 인터페이스 (확장점)
│   └── Quant/                          # 퀀트 엔진 모듈 (현재 매매 결정의 단일 근거)
│       ├── QuantIndicator.cs           # RSI, MACD, 볼린저밴드 계산
│       ├── QuantFilter.cs              # 전략 유형별 다중 조건 AND 필터 (매수/매도/보류 결정)
│       ├── BacktestEngine.cs           # 과거 데이터 기반 전략 검증
│       ├── RebalancingEngine.cs        # 보유 비중 자동 재조정
│       ├── SellStrategyManager.cs      # 분할매도 플랜 관리
│       ├── AdaptiveThresholdEngine.cs  # 종목별 적응형 매수/매도 임계값 (Phase 5-a/5-d, 휴면)
│       ├── PerformanceFeedbackEngine.cs # 에이전트별 실측 적중률 + 가중치 A/B 분석 (Phase 5-d, 읽기 전용·휴면)
│       └── SimTrainingDataGenerator.cs # SimBroker+Mock AI 학습데이터 대량 생성 (Phase 6-a)
│
├── Data/                               # 데이터 액세스 계층
│   ├── DBManager.cs                    # PostgreSQL 연결 관리 (Npgsql, Singleton + 마이그레이션)
│   ├── AppConfigManager.cs             # appsettings.json + 환경변수 + DB 우선순위 설정
│   ├── sql/
│   │   └── create_tables.sql           # DDL + 초기 마스터 데이터
│   ├── DTO/                            # Data Transfer Objects
│   │   ├── AssetDto.cs                 # 자산 마스터
│   │   ├── StrategyDto.cs              # 투자 전략 (StrategyType, Qty)
│   │   ├── StrategySummaryDto.cs       # 전략 요약
│   │   ├── TradeHistoryDto.cs          # 거래 내역
│   │   ├── HoldingDto.cs               # 보유 종목 (잔고)
│   │   ├── PriceRangeDto.cs            # N일 가격 범위
│   │   ├── OhlcvDto.cs                 # OHLCV 일봉 데이터
│   │   ├── IndicatorDto.cs             # 퀀트 지표 결과
│   │   ├── BacktestResultDto.cs        # 백테스팅 결과
│   │   ├── MarketSnapshotDto.cs        # 시장 스냅샷 (AI 학습용, 확률 점수 포함)
│   │   ├── ConsensusScoreDto.cs        # 합의 확률 분해 결과 (BuyProbability, 에이전트별 기여)
│   │   ├── AdaptiveThresholdStatusDto.cs # 적응형 임계값 진단 결과 (표본 수/적용 임계값)
│   │   ├── AgentAccuracyDto.cs         # 에이전트별 실측 적중률 집계 (Phase 5-d)
│   │   ├── WeightSchemeResultDto.cs    # 가중치 조합별 A/B 결과 (Phase 5-d)
│   │   ├── TokenUsageSummaryDto.cs     # 토큰 집계 결과 (에이전트별/일자별)
│   │   ├── SellPlanDto.cs              # 분할매도 플랜
│   │   ├── AiPerformanceDto.cs         # AI 판단 성과 기록
│   │   └── TokenUsageDto.cs            # AI API 토큰 사용량
│   └── DAO/                            # Data Access Objects
│       ├── AssetDAO.cs                 # TB_ASSET_MASTER 조회
│       ├── StrategyDAO.cs              # TB_INVEST_STRATEGY CRUD
│       ├── TradeHistoryDAO.cs          # TB_TRADE_HISTORY CRUD
│       ├── MarketSnapshotDAO.cs        # TB_MARKET_SNAPSHOT CRUD (확률 컬럼 포함)
│       ├── SellPlanDAO.cs              # 분할매도 플랜 CRUD
│       ├── AiPerformanceDAO.cs         # AI 성과 기록 CRUD
│       └── TokenUsageDAO.cs            # AI 토큰 사용량 기록
│
├── Controllers/                        # REST API 컨트롤러
│   ├── ConfigController.cs             # 환경 설정 API
│   ├── HistoryController.cs            # 거래 내역 및 로그 API
│   ├── PortfolioController.cs          # 잔고 조회 API
│   ├── StrategyController.cs           # 전략 CRUD API
│   ├── OrderController.cs              # 수동 주문 트리거 API
│   ├── BacktestController.cs           # 백테스트 실행 API
│   ├── QuantController.cs              # 퀀트 지표 조회 API
│   ├── SellPlanController.cs           # 분할매도 플랜 관리 API
│   ├── MonitoringController.cs         # AI 성과/토큰 비용/적중률·가중치 A/B 조회 API (Phase 5-c/5-d)
│   ├── SimController.cs                # SimBroker 학습데이터 생성/검증 API (Phase 6-a)
│   └── TestController.cs               # 개발/테스트 전용 API
│
├── Utils/                              # 유틸리티 (모든 레이어 접근 가능)
│   ├── Logger.cs                       # Serilog 래퍼 (Info/Warn/Error/Fatal/LogQuant)
│   ├── NotificationService.cs          # MailKit Naver SMTP 이메일 알림
│   ├── ExchangeRateService.cs          # 환율 API (Frankfurter + fallback, 1시간 캐싱)
│   ├── ApiKeyAuthAttribute.cs          # 전역 x-api-key 인증 필터
│   ├── DateTimeHelper.cs               # NYSE 개장시각(KST) 계산 (DST 대응)
│   └── PromptBuilder.cs                # Gemini 차트/펀더멘털 프롬프트 생성
│
├── Frontend/                           # React SPA (Vite, Glassmorphism 디자인)
│   └── src/
│       ├── pages/                      # Dashboard, History, Order, Backtest, Strategy, Settings, Monitoring
│       └── components/                 # HoldingsTable, SellPlanManager, ProgressLoader
│
└── Documents/                          # 프로젝트 문서
    ├── DEVELOPMENT.md                  # 개발 진척도 + 전체 변경 이력
    ├── ONBOARDING_GUIDE.md             # 신규 개발자용 아키텍처 가이드
    └── CODE_READING_GUIDE.md           # SmartOrderEngine 코드 흐름 가이드
```

---

## 🖥️ 아키텍처: Headless ASP.NET Core Web API

기존 WinForms 기반에서 **ASP.NET Core Web API** 기반의 Headless 서버로 구조가 개편되었습니다.
UI 스레드 종속성을 제거하여 Linux 서버 / Docker 환경에서 24시간 무인으로 동작합니다.

- **스케줄 실행**: 외부 크론(GitHub Actions 워크플로우 `.github/workflows/daily-run.yml`, 매일 KST 23:40)이 `POST /api/order/daily-run`을 호출 → `OrderController`가 Scoped `DailyExecutionService`를 구동 → `SmartOrderEngine`이 전 종목을 **퀀트 단독**으로 분석·매매
- **REST API 컨트롤러**: React 웹 대시보드 및 외부 클라이언트에서 상태 조회, 원격 제어 제공
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
| 사용처 | 대시보드 환율 카드, 배분 설정 환율 표시 |

---

## 📊 퀀트 전략 유형

| 전략 유형 | 철학 | 매수 조건 (AND) |
|-----------|------|-----------------| 
| **MEAN_REVERSION** | 싸게 사서 원래 자리로 | Position ≤ 10% + RSI ≤ 30 + BB 하단 근접 |
| **MOMENTUM** | 오르는 놈이 더 간다 | RSI ≥ 50 + MACD 골든크로스 + MACD Line 양수 |
| **MIXED** | 중간 접근 | Position ≤ 10% + RSI < 70 |

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
| AI 엔진 | Google Gemini API (차트 + 펀더멘털 이중 에이전트) — **현재 휴면**(매매 결정 미사용, 코드 보존) |
| 환율 컨텍스트 | FxRateAdvisor — 매수/매도 환율 유불리 설명·경고 (veto 없음) |
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

### Phase 2 — 엔진 코어 + 배분 UI (✅ 완료)
- [x] `IBrokerClient` / `SimBrokerClient` / `SmartOrderEngine`
- [x] `SchedulerModule` / `SessionManager`
- [x] `AllocationPanel` — 배분 설정 UI

### Phase 2.5 — 퀀트 엔진 모듈 (✅ 완료)
- [x] `QuantIndicator` / `QuantFilter` / `BacktestEngine` / `RebalancingEngine`
- [x] `SmartOrderEngine` 고도화 — 퀀트 필터 통합
- [x] `TB_MARKET_SNAPSHOT` — AI 학습 데이터 축적

### Phase 2.6 — 구조 리팩토링 (✅ 완료)
- [x] Weight(비중) → Qty(수량 정수) 전환
- [x] 안정형/공격형 사전 전략 제거 → 전략유형(MEAN_REVERSION/MOMENTUM/MIXED)으로 대체
- [x] 불필요 버튼 제거 (로그인, 즉시주문, 예약주문)
- [x] 무료 환율 API 연동 (Frankfurter API)
- [x] 멀티 Form(ShowDialog) → 단일 창 Panel 전환(SPA) UI 전환
- [x] Panels/ 폴더 구조 추가
- [x] 레거시 Form 파일 삭제 (ConfigForm, HistoryForm, AllocationSetupForm, BacktestForm)

### Phase 3 — KIS 실거래 연동 (✅ 완료)
- [x] 하네스 엔지니어링 룰(.agents/rules) 적용
- [x] `KisBrokerClient` — KIS REST API 실제 구현
- [x] OAuth 토큰 발급 + 자동 갱신 (`KisTokenManager`)
- [x] 실시간 시세/잔고 조회 및 주문 실행

### Phase A — 프로젝트 정비 및 안정화 (✅ 완료)
- [x] WinForms 레거시 완전 제거 (Form/Panel 삭제)
- [x] 설정 체계 현대화 (`appsettings.json` + 환경변수)
- [x] 컨트롤러 완성 (전략 CRUD, 수동주문, 백테스트 등 API 도입)
- [x] DI(의존성 주입) 체계 정비

### Phase B/C — 운영 안정성 및 React 프론트엔드 연동 (✅ 완료)
- [x] KIS 실거래 API 내결함성(Polly 지수 백오프) 적용
- [x] MailKit 연동: 매수/매도 체결 및 예외 알림 이메일 발송
- [x] React-Router 기반 SPA 프론트엔드 6개 핵심 페이지 구축 완료
- [x] Glassmorphism 프리미엄 디자인 시스템 적용

### Phase 4 — AI 시장분석 엔진 (✅ 완료)
- [x] Phase 4-a: AI Mock + CombineSignals 아키텍처
- [x] Phase 4-b: Gemini 실물 연동 + 퀀트 조건 현실화
- [x] Phase 4-c: 투자 철학 주입 및 예외처리 고도화
- [x] Phase 4-d: 차트+펀더멘털 이중 에이전트 병렬 합의 구조
- [x] Phase 4-e: 확률 기반 가중치 합산(CalculateConsensusScore) 시스템 — ConsensusScoreDto, BuyProbability

### Phase 5 — 적응형 임계값 · 성과 피드백 · 모니터링 (✅ 완료)
- [x] Phase 5-a: `AdaptiveThresholdEngine` — 과거 BuyProbability 분포 기반 종목별 매수 임계값 자동 조정
- [x] Phase 5-b: AI 판단 성과 측정 + 토큰 비용 모니터링 데이터 적재 (`TB_AI_PERFORMANCE`, `TB_TOKEN_USAGE`)
- [x] Phase 5-c: 모니터링 대시보드 UI — 성과/토큰 비용 조회 (`MonitoringController`, `Monitoring.jsx`)
- [x] Phase 5-d: 성과 기반 피드백 루프 — 에이전트별 실측 적중률 + 매도 적응형 임계값 + 합의 가중치 A/B 검증

### Phase 6 — 실데이터 운영 전환 · AI 호출 최적화 (✅ 완료)
- [x] Phase 6-a: SimBroker 학습데이터 대량 생성 + DATA_SOURCE(SIM/REAL) 출처 분리
- [x] Phase 6-b: 실데이터 운영 전환(Gemini 모델 설정화) + 무료 한도 429 대응(호출 통합·throttle) + AI 모델 선택 UI + 분석 진행바 UX

### Phase 7 — 보안 (✅ 완료)
- [x] 시크릿 키 암호화 저장(AES-256-GCM, MASTER_KEY) + 관리자 로그인 게이트(세션 토큰) — 크론은 x-api-key 유지

### Phase 8 — 퀀트 단독 전환 + 환율 컨텍스트 (✅ 완료)
- [x] 매매 결정을 **퀀트 단독**으로 전환 — AI 결정 경로(다중 에이전트·적응형 임계값·합의 스코어링)는 주석 비활성화(휴면, 코드 보존)
- [x] 환율(FX) 어드바이저를 **설명·경고 전용**으로 매매 컨텍스트(단일 종목 분석/일일 리포트)에 반영 (veto 없음)
- [x] `TB_MARKET_SNAPSHOT` AI 컬럼은 스키마 유지하되 기록 중단(0/빈값), 누적 데이터 보존

---

## 🔧 로컬 실행

1. Visual Studio 2022에서 `AutoInvest.sln` 열기
2. NuGet 패키지 복원
3. `F5`로 디버그 실행 (로컬 PostgreSQL 필요 — 기본 접속: `localhost`, DB명 `autoinvest`. 테이블은 `create_tables.sql`로 자동 생성. 배포 시 `DATABASE_URL` 환경변수 사용)

> 증권사 API 키 없이도 SimBrokerClient(시뮬레이션 모드)로 전체 기능을 테스트할 수 있습니다.
