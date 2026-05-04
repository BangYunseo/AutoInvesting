# AutoInvesting

> 해외 ETF 자동 투자 시스템 — WinForms (.NET 8.0)

## 📌 프로젝트 개요

월 정액 투자금을 설정하고, 사전 정의된 전략(안정형 / 공격형 / 사용자정의)에 따라 해외 ETF를 자동으로 매수·매도하는 데스크톱 애플리케이션입니다. **퀀트 엔진**을 통해 RSI, MACD, 볼린저밴드 등 다중 기술적 지표를 분석하고, 모든 조건을 만족할 때만 주문을 실행합니다.

### 핵심 목적

| # | 목적 | 설명 |
|---|------|------|
| 1 | **퀀트 기반 매매** | RSI·MACD·볼린저밴드·Position 등 다중 지표를 AND 조건으로 결합하여 데이터 기반 매매 판단 |
| 2 | **스마트 주문 시스템** | 전략 유형(평균회귀/모멘텀/혼합)에 따라 서로 다른 지표 조건을 적용하여 주문 실행 |
| 3 | **백테스팅** | 과거 데이터를 대입하여 전략의 수익률·MDD·승률을 검증 |
| 4 | **리밸런싱** | 정해진 주기마다 보유 비중을 목표 비중에 맞게 자동 재조정 |
| 5 | **배분 설정** | 목표 금액과 종목별 수량을 설정하면, 실시간 단가 × 수량 × 환율로 금액을 자동 계산 |

### 증권사 API

| 항목 | 내용 |
|------|------|
| 증권사 | **LS증권** |
| API 형태 | REST API (HTTPS) + WebSocket (실시간 시세) |
| 인증 | OAuth 2.0 (APP KEY / APP SECRET → Access Token) |
| 토큰 유효기간 | **익일 07시까지** (매일 재발급 필요) |
| 대상 시장 | 미국 해외주식 (NYSE, NASDAQ) |
| 주요 API 그룹 | [해외주식] 시세, 주문, 계좌, 차트, 실시간 시세 |

> **참고**: LS증권 OPEN API 포털 — https://openapi.ls-sec.co.kr/

---

## 🏗️ 프로젝트 구조

```
AutoInvesting/
├── Program.cs                          # 앱 진입점 (전역 예외 처리)
├── App.config                          # .NET 런타임 설정
├── packages.config                     # NuGet 패키지 (SQLite)
│
├── Core/                               # 핵심 비즈니스 로직
│   ├── IBrokerClient.cs                # 증권사 API 추상화 인터페이스
│   ├── SimBrokerClient.cs              # 시뮬레이션 구현체 (API 키 불필요)
│   ├── SmartOrderEngine.cs             # 스마트 주문 판단 + 퀀트 필터 통합
│   ├── SchedulerModule.cs              # 예약 주문 스케줄러 + 리밸런싱 통합
│   ├── SessionManager.cs               # IBrokerClient 생명주기 관리
│   ├── AllocationEngine.cs             # 투자금 배분 계산 엔진
│   └── Quant/                          # ★ 퀀트 엔진 모듈 (Phase 2.5)
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
│   │   ├── StrategyDto.cs              # 투자 전략 (+ StrategyType)
│   │   ├── TradeHistoryDto.cs          # 거래 내역
│   │   ├── HoldingDto.cs               # 보유 종목 (잔고)
│   │   ├── PriceRangeDto.cs            # N일 가격 범위
│   │   ├── OhlcvDto.cs                 # ★ OHLCV 일봉 데이터
│   │   ├── IndicatorDto.cs             # ★ 퀀트 지표 결과
│   │   ├── BacktestResultDto.cs        # ★ 백테스팅 결과
│   │   └── MarketSnapshotDto.cs        # ★ 시장 스냅샷 (AI 학습용)
│   └── DAO/                            # Data Access Objects
│       ├── AssetDAO.cs                 # TB_ASSET_MASTER 조회
│       ├── StrategyDAO.cs              # TB_INVEST_STRATEGY CRUD
│       ├── TradeHistoryDAO.cs          # TB_TRADE_HISTORY CRUD
│       └── MarketSnapshotDAO.cs        # ★ TB_MARKET_SNAPSHOT CRUD
│
├── Forms/                              # UI (WinForms)
│   ├── MainForm.cs / .Designer.cs      # 메인 대시보드 + 배분 결과 카드 표시
│   ├── AllocationSetupForm.cs / .Designer.cs  # 배분 설정 (종목/수량/금액 계산)
│   ├── ConfigForm.cs / .Designer.cs    # 설정 (투자금, 시각, 전략)
│   ├── HistoryForm.cs / .Designer.cs   # 거래 내역 조회
│   └── BacktestForm.cs                 # ★ 백테스팅 UI (전략 검증)
│
├── Controls/                           # 커스텀 UserControl
│   └── AllocationCardControl.cs        # 종목별 배분 카드 (슬림 한 줄형)
│
├── Utils/                              # 유틸리티
│   ├── Logger.cs                       # 파일 + ListBox 로깅 (퀀트 로그 포함)
│   ├── AppTheme.cs                     # 다크 테마 컬러 상수
│   └── DateTimeHelper.cs              # NYSE 개장시각(KST) 계산 (DST 대응)
│
└── Documents/                          # 프로젝트 문서
    ├── DEVELOPMENT.md                  # 개발 진척도 + 변경 이력
    └── THEME_GUIDE.md                  # 다크 테마 가이드
```

> ★ 표시는 Phase 2.5 (퀀트 엔진)에서 추가된 파일입니다.

---

## 🗄️ 데이터베이스 (SQLite)

| 테이블 | 용도 |
|--------|------|
| `TB_ASSET_MASTER` | 투자 대상 ETF 마스터 (SCHD, QQQM, GLD, JEPI, SPLG) |
| `TB_INVEST_STRATEGY` | 전략별 종목 비중 + 전략 유형 (MEAN_REVERSION/MOMENTUM/MIXED) |
| `TB_TRADE_HISTORY` | 매매 내역 (날짜, 종목, 유형, 수량, 가격, 상태) |
| `TB_APP_CONFIG` | 앱 설정 (투자금, 시각, 전략, 모의투자 여부, 리밸런싱 설정) |
| `TB_MARKET_SNAPSHOT` | ★ 매매 시점 시장 지표 스냅샷 (Phase 4 AI 학습 데이터) |

### 주요 설정값 (TB_APP_CONFIG)

| KEY | 기본값 | 설명 |
|-----|--------|------|
| `IS_PAPER_TRADING` | `1` | 1=시뮬레이션(SimBroker), 0=실거래(LS증권) |
| `INVEST_AMOUNT_KRW` | `1000000` | 월 투자금액 (원) |
| `ACTIVE_STRATEGY` | `안정형` | 현재 활성 전략 이름 |
| `ORDER_SCHEDULE` | `22:30` | 예약 주문 실행 시각 (KST) |
| `REBALANCE_ENABLED` | `0` | 리밸런싱 활성화 (1=ON, 0=OFF) |
| `REBALANCE_PERIOD` | `MONTHLY` | 리밸런싱 주기 (WEEKLY / MONTHLY) |
| `REBALANCE_THRESHOLD` | `0.05` | 리밸런싱 편차 임계값 (5%) |

---

## 📊 퀀트 전략 유형

| 전략 유형 | 철학 | 매수 조건 (AND) |
|-----------|------|-----------------|
| **MEAN_REVERSION** | 싸게 사서 원래 자리로 | Position ≤ 10% + RSI ≤ 30 + BB 하단 근접 |
| **MOMENTUM** | 오르는 놈이 더 간다 | RSI ≥ 50 + MACD 골든크로스 + MACD Line 양수 |
| **MIXED** | 중간 접근 | Position ≤ 10% + RSI < 70 |

### 퀀트 지표 설명

| 지표 | 계산 | 의미 |
|------|------|------|
| **Position** | (현재가 - 20일최저가) / (20일최고가 - 20일최저가) | 0에 가까울수록 저점 |
| **RSI(14)** | 14일간 상승/하락 비율 | 30 이하 = 과매도, 70 이상 = 과매수 |
| **MACD** | 12일 EMA - 26일 EMA | Signal Line과 교차 시 추세 전환 |
| **볼린저밴드** | 20일 SMA ± 2σ | 밴드 밖 = 극단값 |

---

## ⚙️ 기술 스택

| 분류 | 기술 |
|------|------|
| 언어 | C# |
| 프레임워크 | .NET 8.0 / WinForms |
| DB | SQLite (System.Data.SQLite) |
| 증권사 API | LS증권 OPEN API (REST + WebSocket) |
| HTTP 통신 | System.Net.Http.HttpClient |
| 빌드 | MSBuild / Visual Studio 2022 |

---

## 📐 아키텍처 흐름

```
[MainForm UI]
    │
    ├──→ [AllocationSetupForm]  배분 설정 → StrategyDAO → DB
    ├──→ [ConfigForm]           설정 변경 → AppConfigManager → DB
    ├──→ [HistoryForm]          거래 내역 조회 → TradeHistoryDAO → DB
    ├──→ [BacktestForm] ★       백테스팅 → BacktestEngine → 가상 매매 시뮬레이션
    │
    ├──→ [SchedulerModule]      예약 시각 도달 시 →
    │        │
    │        ▼
    │    [SessionManager]       IBrokerClient 로그인 확인 →
    │        │
    │        ▼
    │    [IBrokerClient]        현재가 + OHLCV 조회 →
    │        │
    │        ▼
    │    [QuantIndicator] ★     RSI, MACD, BB 계산 →
    │        │
    │        ▼
    │    [QuantFilter] ★        전략 유형별 AND 조건 필터 →
    │        │
    │        ▼
    │    [SmartOrderEngine]     조건 통과 시 주문 실행 →
    │        │
    │        ├──→ [MarketSnapshotDAO] ★  시장 스냅샷 저장 (AI 학습용)
    │        └──→ [TradeHistoryDAO]      거래 내역 DB 저장
    │
    ├──→ [RebalancingEngine] ★  리밸런싱 주기 도래 시 비중 재조정
    │
    └──→ [즉시 주문 버튼]       동일 흐름 (스케줄러 생략)
```

### IBrokerClient 인터페이스 구현 구조

```
IBrokerClient (인터페이스)
    ├── SimBrokerClient      ← 현재 사용 (시뮬레이션, API 키 불필요)
    └── LsBrokerClient       ← Phase 3에서 구현 예정 (LS증권 실거래)
```

| IBrokerClient 메서드 | LS증권 API 매핑 | 용도 |
|---|---|---|
| `LoginAsync` | OAuth 토큰 발급 | Access Token 획득 |
| `GetCurrentPriceAsync` | [해외주식] 시세 | 현재가 조회 |
| `GetPriceRangeAsync` | [해외주식] 차트 | N일 일봉 → 최고/최저 |
| `GetOhlcvAsync` ★ | [해외주식] 차트 | N일 일봉 OHLCV (퀀트 지표용) |
| `GetExchangeRateAsync` | [해외주식] 시세 | USD/KRW 환율 |
| `GetHoldingsAsync` | [해외주식] 계좌 | 보유 잔고 조회 |
| `PlaceBuyOrderAsync` | [해외주식] 주문 | 매수 주문 |
| `PlaceSellOrderAsync` | [해외주식] 주문 | 매도 주문 |

---

## 🚀 개발 로드맵

### Phase 1 — 기반 (✅ 완료)
- [x] 프로젝트 생성 및 SQLite 연동
- [x] DB 스키마 및 초기 마스터 데이터
- [x] DTO / DAO 레이어
- [x] 메인 대시보드 UI (사이드바, 카드, 로그)
- [x] 설정 폼 / 거래 내역 폼
- [x] AllocationEngine (투자금 배분 계산)
- [x] Logger / DateTimeHelper 유틸리티

### Phase 2 — 엔진 코어 + 배분 UI (✅ 완료)
- [x] `IBrokerClient` — 증권사 API 추상화 인터페이스
- [x] `SimBrokerClient` — 시뮬레이션 구현체 (고정 기준가 반환)
- [x] `SmartOrderEngine` — 스마트 주문 판단 (20일 최저/최고가 기반)
- [x] `SchedulerModule` — 예약 주문 스케줄러 (1분 간격, 중복 방지)
- [x] `SessionManager` — IBrokerClient 생명주기 관리
- [x] `AllocationSetupForm` — 배분 설정 UI (종목/수량/금액 실시간 계산)

### Phase 2.5 — 퀀트 엔진 모듈 (✅ 완료)
- [x] `QuantIndicator` — RSI(14), MACD(12,26,9), 볼린저밴드(20,±2σ) 계산
- [x] `QuantFilter` — 전략 유형별 다중 조건 AND 필터
- [x] `SmartOrderEngine` 고도화 — 퀀트 필터 통합, 상세 판단 근거 로그
- [x] `BacktestEngine` — 과거 데이터 기반 전략 수익성 검증
- [x] `BacktestForm` — 백테스팅 UI (수익률, MDD, 승률 표시)
- [x] `RebalancingEngine` — 보유 비중 자동 재조정
- [x] `SchedulerModule` 확장 — 리밸런싱 주기 체크 + 자동 실행
- [x] `Logger.LogQuant()` — 퀀트 판단 근거 상세 로그
- [x] `TB_MARKET_SNAPSHOT` — 매매 시점 시장 지표 스냅샷 저장 (AI 학습용)
- [x] `StrategyDto.StrategyType` — 전략 유형 구분 (MEAN_REVERSION/MOMENTUM/MIXED)
- [x] DB 마이그레이션 — `RunMigration()` 안전한 스키마 변경

### Phase 3 — LS증권 실거래 연동 (📋 예정)
- [ ] `LsBrokerClient` — LS증권 REST API 실제 구현
- [ ] 로그인 Form — APP KEY / APP SECRET / 계좌번호 입력
- [ ] OAuth 토큰 발급 + 자동 갱신 (익일 07시 만료)
- [ ] 실시간 시세 조회 (현재 SimBroker → LS증권 API)
- [ ] 실시간 환율 조회
- [ ] 주문 실행 흐름 (예약 / 즉시)
- [ ] 모의투자 ↔ 실거래 전환
- [ ] 에러 핸들링 및 재시도 로직
- [ ] TPS 제한 대응 (요청 간 딜레이)

### Phase 4 — AI 시장분석 엔진 (🎯 최종 목표)
- [ ] `IMarketAnalyzer` 인터페이스 도입
- [ ] 차트 데이터 분석 (일봉, 주봉, 기술적 지표)
- [ ] 뉴스 감성 분석 (국내외 금융 뉴스, 중앙은행 발표)
- [ ] 커뮤니티 감성 분석 (Reddit, X, StockTwits 등)
- [ ] 매크로 지표 연동 (금리, VIX 등)
- [ ] AI confidence score + SmartOrderEngine 종합 판단
- [ ] `TB_MARKET_SNAPSHOT` 데이터 활용 모델 학습

---

## 🔧 로컬 실행

1. Visual Studio 2022에서 `AutoInvest.sln` 열기
2. NuGet 패키지 복원
3. `F5`로 디버그 실행 (SQLite DB 자동 생성)

> 증권사 API 키 없이도 SimBrokerClient(시뮬레이션 모드)로 전체 기능을 테스트할 수 있습니다.
> 배분 설정, 스마트 주문 분석, 예약 주문 스케줄러, 백테스팅 등 모든 엔진 로직이 시뮬레이션으로 동작합니다.
