---
trigger: always_on
---

# AutoInvesting 프로젝트 개요

> 해외 ETF 자동 투자 시스템 — WinForms (.NET 8.0)

## 목적

설정한 시각에 자동으로 해외 ETF를 매수·매도하는 데스크톱 애플리케이션.
퀀트 엔진(RSI, MACD, 볼린저밴드)으로 다중 기술적 지표를 분석하고,
모든 조건을 만족할 때만 주문을 실행하여 **감정을 배제한 데이터 기반 투자**를 실현.

## 기술 스택

| 분류 | 기술 |
|------|------|
| 언어 | C# |
| 프레임워크 | .NET 8.0 / WinForms |
| DB | SQLite (`System.Data.SQLite`) |
| 증권사 API | 한국투자증권 KIS Developers (Phase 3 예정) |
| 환율 API | Frankfurter API (무료, 키 불필요) |
| 빌드 | MSBuild / Visual Studio 2022 |

## 디렉토리 구조

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
│   ├── AppConfigManager.cs             # TB_APP_CONFIG CRUD (설정 키-값 관리)
│   ├── sql/
│   │   └── create_tables.sql           # DDL + 초기 마스터 데이터
│   ├── DTO/                            # Data Transfer Objects
│   │   ├── AssetDto.cs
│   │   ├── StrategyDto.cs
│   │   ├── TradeHistoryDto.cs
│   │   ├── HoldingDto.cs
│   │   ├── PriceRangeDto.cs
│   │   ├── OhlcvDto.cs
│   │   ├── IndicatorDto.cs
│   │   ├── BacktestResultDto.cs
│   │   └── MarketSnapshotDto.cs
│   └── DAO/                            # Data Access Objects
│       ├── AssetDAO.cs
│       ├── StrategyDAO.cs
│       ├── TradeHistoryDAO.cs
│       └── MarketSnapshotDAO.cs
│
├── Forms/                              # UI 메인 쉘
│   └── MainForm.cs / .Designer.cs
│
├── Panels/                             # SPA 방식 패널 (UserControl)
│   ├── DashboardPanel.cs
│   ├── AllocationPanel.cs
│   ├── HistoryPanel.cs
│   ├── ConfigPanel.cs
│   └── LogPanel.cs
│
├── Controls/                           # 커스텀 UserControl
│   └── AllocationCardControl.cs
│
├── Utils/                              # 유틸리티
│   ├── Logger.cs
│   ├── AppTheme.cs
│   ├── DateTimeHelper.cs
│   └── ExchangeRateService.cs
│
└── Documents/                          # 프로젝트 문서
    ├── DEVELOPMENT.md
    └── THEME_GUIDE.md
```

## 핵심 인터페이스: IBrokerClient

| 메서드 | 설명 |
|--------|------|
| `LoginAsync()` | 로그인 (토큰 발급) |
| `GetCurrentPriceAsync(ticker)` | 현재가 조회 (USD) |
| `GetPriceRangeAsync(ticker, days)` | N일 최고가/최저가 |
| `GetExchangeRateAsync()` | 환율 조회 (USD→KRW) |
| `GetHoldingsAsync()` | 보유 종목 목록 |
| `GetOhlcvAsync(ticker, days)` | OHLCV 일봉 데이터 |
| `PlaceBuyOrderAsync(ticker, qty, price)` | 매수 주문 |
| `PlaceSellOrderAsync(ticker, qty, price)` | 매도 주문 |

구현체: `SimBrokerClient` (시뮬레이션) / `KisBrokerClient` (Phase 3 예정)

## 퀀트 전략 유형

| 전략 | 매수 조건 (AND) |
|------|-----------------| 
| `MEAN_REVERSION` | Position ≤ 10% + RSI ≤ 30 + BB 하단 근접 |
| `MOMENTUM` | RSI ≥ 50 + MACD 골든크로스 + MACD Line 양수 |
| `MIXED` | Position ≤ 10% + RSI < 70 |

## 설정 관리

`AppConfigManager`를 통해 `TB_APP_CONFIG` 테이블에서 키-값으로 관리:

| 설정 KEY | 기본값 | 설명 |
|----------|--------|------|
| `IS_PAPER_TRADING` | `1` | 1=시뮬레이션, 0=실거래 |
| `ORDER_SCHEDULE` | `22:30` | 자동 주문 시각 (KST) |
| `INVEST_AMOUNT_KRW` | `1000000` | 투자금액 (원) |
| `ACTIVE_STRATEGY` | `안정형` | 활성 전략명 |
| `REBALANCE_ENABLED` | `0` | 리밸런싱 활성화 |

## Phase 진행 상태

| Phase | 내용 | 상태 |
|-------|------|------|
| 1 | 기반 (SQLite, DTO/DAO, 메인 UI) | ✅ 완료 |
| 2 | 엔진 코어 + 배분 UI | ✅ 완료 |
| 2.5 | 퀀트 엔진 모듈 | ✅ 완료 |
| 2.6 | 구조 리팩토링 (SPA 전환) | ✅ 완료 |
| 3 | 한국투자증권 실거래 연동 | 📋 미착수 |
| 4 | AI 시장분석 엔진 | 📋 미착수 |
