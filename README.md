# AutoInvesting

> 해외 ETF 자동 투자 시스템 — WinForms (.NET 8.0)

## 📌 프로젝트 개요

설정한 시각에 자동으로 해외 ETF를 매수·매도하는 데스크톱 애플리케이션입니다.
**퀀트 엔진**을 통해 RSI, MACD, 볼린저밴드 등 다중 기술적 지표를 분석하고,
모든 조건을 만족할 때만 주문을 실행하여 **감정을 배제한 데이터 기반 투자**를 실현합니다.

### 핵심 목적

| # | 목적 | 설명 |
|---|------|------|
| 1 | **자동 투자** | 사용자가 직접 주문하지 않아도, 설정된 시각에 자동으로 매매 실행 |
| 2 | **감정 배제** | "더 오를거다/내릴거다" 같은 심리적 판단을 제거하고 규칙 기반 매매 |
| 3 | **계산식/AI 기반 투자** | 퀀트 지표, 계산식, AI를 활용한 정량적 데이터 기반 매매 |

### 증권사 API

| 항목 | 내용 |
|------|------|
| 증권사 | **LS증권** |
| API 형태 | REST API (HTTPS) + WebSocket (실시간 시세) |
| 인증 | OAuth 2.0 (APP KEY / APP SECRET → Access Token) |
| 대상 시장 | 미국 해외주식 (NYSE, NASDAQ) |

> **참고**: LS증권 OPEN API 포털 — https://openapi.ls-sec.co.kr/

---

## 🏗️ 프로젝트 구조

```
AutoInvesting/
├── Program.cs                          # 앱 진입점 (전역 예외 처리)
│
├── Core/                               # 핵심 비즈니스 로직
│   ├── IBrokerClient.cs                # 증권사 API 추상화 인터페이스
│   ├── SimBrokerClient.cs              # 시뮬레이션 구현체 (API 키 불필요)
│   ├── SmartOrderEngine.cs             # 스마트 주문 판단 + 퀀트 필터 통합
│   ├── SchedulerModule.cs              # 예약 주문 스케줄러 + 리밸런싱 통합
│   ├── SessionManager.cs               # IBrokerClient 생명주기 관리
│   ├── AllocationEngine.cs             # 투자금 배분 계산 엔진
│   └── Quant/                          # 퀀트 엔진 모듈
│       ├── QuantIndicator.cs           # RSI, MACD, 볼린저밴드 계산
│       ├── QuantFilter.cs              # 전략 유형별 다중 조건 AND 필터
│       ├── BacktestEngine.cs           # 과거 데이터 기반 전략 검증
│       └── RebalancingEngine.cs        # 보유 비중 자동 재조정
│
├── Data/                               # 데이터 액세스 계층
│   ├── DBManager.cs                    # SQLite 연결 관리 (Singleton + 마이그레이션)
│   ├── AppConfigManager.cs             # TB_APP_CONFIG CRUD
│   ├── sql/
│   │   └── create_tables.sql           # DDL + 초기 마스터 데이터
│   ├── DTO/                            # Data Transfer Objects
│   │   ├── AssetDto.cs                 # 자산 마스터
│   │   ├── StrategyDto.cs              # 투자 전략 (+ StrategyType, Qty)
│   │   ├── TradeHistoryDto.cs          # 거래 내역
│   │   ├── HoldingDto.cs               # 보유 종목 (잔고)
│   │   ├── PriceRangeDto.cs            # N일 가격 범위
│   │   ├── OhlcvDto.cs                 # OHLCV 일봉 데이터
│   │   ├── IndicatorDto.cs             # 퀀트 지표 결과
│   │   ├── BacktestResultDto.cs        # 백테스팅 결과
│   │   └── MarketSnapshotDto.cs        # 시장 스냅샷 (AI 학습용)
│   └── DAO/                            # Data Access Objects
│       ├── AssetDAO.cs                 # TB_ASSET_MASTER 조회
│       ├── StrategyDAO.cs              # TB_INVEST_STRATEGY CRUD
│       ├── TradeHistoryDAO.cs          # TB_TRADE_HISTORY CRUD
│       └── MarketSnapshotDAO.cs        # TB_MARKET_SNAPSHOT CRUD
│
├── Forms/                              # UI (WinForms) — 메인 쉘
│   ├── MainForm.cs / .Designer.cs      # 메인 쉘 (사이드바 + Panel 전환)
│   ├── AllocationSetupForm.cs          # (레거시 — AllocationPanel로 이전됨)
│   ├── ConfigForm.cs                   # (레거시 — ConfigPanel로 이전됨)
│   ├── HistoryForm.cs                  # (레거시 — HistoryPanel로 이전됨)
│   └── BacktestForm.cs                # 백테스팅 UI (Phase 3까지 숨김)
│
├── Panels/                             # ★ SPA 방식 패널 (UserControl)
│   ├── DashboardPanel.cs               # 대시보드 (카드 + 배분결과 + 로그)
│   ├── AllocationPanel.cs              # 배분 설정 (종목/수량 관리)
│   ├── HistoryPanel.cs                 # 거래 내역 조회
│   ├── ConfigPanel.cs                  # 환경 설정 (전략유형/투자금/시각)
│   └── LogPanel.cs                     # 전체 화면 로그 뷰
│
├── Controls/                           # 커스텀 UserControl
│   └── AllocationCardControl.cs        # 종목별 배분 카드 (슬림 한 줄형)
│
├── Utils/                              # 유틸리티
│   ├── Logger.cs                       # 파일 + ListBox 로깅 (퀀트 로그 포함)
│   ├── AppTheme.cs                     # 다크 테마 컬러 상수
│   ├── DateTimeHelper.cs               # NYSE 개장시각(KST) 계산 (DST 대응)
│   └── ExchangeRateService.cs          # ★ 무료 환율 API (Frankfurter + fallback)
│
└── Documents/                          # 프로젝트 문서
    ├── DEVELOPMENT.md                  # 개발 진척도 + 변경 이력
    └── THEME_GUIDE.md                  # 다크 테마 가이드
```

---

## 🖥️ UI 아키텍처: 단일 창 Panel 전환 (SPA)

```
MainForm
  ├── pnl_sidebar (좌측 고정 — 메뉴 버튼)
  ├── pnl_topbar  (상단 고정 — 타이틀)
  └── pnl_content (Dock=Fill — 패널 교체 영역)
        ├── DashboardPanel   ← 대시보드
        ├── AllocationPanel  ← 배분 설정
        ├── HistoryPanel     ← 거래 내역
        ├── ConfigPanel      ← 환경 설정
        └── LogPanel         ← 시스템 로그
```

> 사이드바 메뉴 클릭 → `SwitchPanel()` → pnl_content에 새 UserControl 로드
> 별도 창 팝업 없이 단일 창 내에서 모든 기능 전환

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
| 언어 | C# |
| 프레임워크 | .NET 8.0 / WinForms |
| DB | SQLite (System.Data.SQLite) |
| 증권사 API | LS증권 OPEN API (Phase 3 예정) |
| 환율 API | Frankfurter API (무료, 키 불필요) |
| 빌드 | MSBuild / Visual Studio 2022 |

---

## 🚀 개발 로드맵

### Phase 1 — 기반 (✅ 완료)
- [x] 프로젝트 생성 및 SQLite 연동
- [x] DB 스키마 및 초기 마스터 데이터
- [x] DTO / DAO 레이어
- [x] 메인 대시보드 UI (사이드바, 카드, 로그)
- [x] 설정 폼 / 거래 내역 폼

### Phase 2 — 엔진 코어 + 배분 UI (✅ 완료)
- [x] `IBrokerClient` / `SimBrokerClient` / `SmartOrderEngine`
- [x] `SchedulerModule` / `SessionManager`
- [x] `AllocationSetupForm` — 배분 설정 UI

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

### Phase 3 — LS증권 실거래 연동 (📋 예정)
- [ ] `LsBrokerClient` — LS증권 REST API 실제 구현
- [ ] OAuth 토큰 발급 + 자동 갱신
- [ ] 실시간 시세/환율 조회
- [ ] 주문 실행 + 에러 핸들링

### Phase 4 — AI 시장분석 엔진 (🎯 최종 목표)
- [ ] AI 기반 주식 분류 (안정적/공격적)
- [ ] 차트 데이터 분석 + 뉴스 감성 분석
- [ ] AI confidence score + SmartOrderEngine 종합 판단

---

## 🔧 로컬 실행

1. Visual Studio 2022에서 `AutoInvest.sln` 열기
2. NuGet 패키지 복원
3. `F5`로 디버그 실행 (SQLite DB 자동 생성)

> 증권사 API 키 없이도 SimBrokerClient(시뮬레이션 모드)로 전체 기능을 테스트할 수 있습니다.
