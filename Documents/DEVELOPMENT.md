# 개발 진척도 (CHANGELOG)

> 이 문서는 AutoInvesting 프로젝트의 개발 진행 상황을 기록합니다.
> 새 개발자가 이 문서를 보고 현재 상태와 다음 작업을 파악할 수 있도록 작성합니다.

---

## 현재 상태: Phase 2.6 완료 ✅

- **Phase 1** (기반): ✅ 완료
- **Phase 2** (엔진 코어 + 배분 UI): ✅ 완료
- **Phase 2.5** (퀀트 엔진 모듈): ✅ 완료
- **Phase 2.6** (구조 리팩토링): ✅ 완료
- **Phase 3** (KIS 실거래 연동): 🚀 진행 중
- **Phase 4** (AI 시장분석): 📋 미착수

---

## Phase 2.6 상세 변경 이력 — 구조 리팩토링

### 핵심 변경: "멀티 Form 팝업" → "단일 창 Panel 전환 (SPA)"

```
기존 흐름:
  사이드바 버튼 클릭 → new ConfigForm().ShowDialog()  ← 별도 창 팝업

리팩토링 후:
  사이드바 버튼 클릭 → SwitchPanel(new ConfigPanel())  ← 단일 창 내 패널 교체
```

### 2.6-1. UI 구조 전환 (신규 5건)

| 파일 | 설명 |
|------|------|
| `Panels/DashboardPanel.cs` | 대시보드 (카드 + 배분결과 + 로그) |
| `Panels/AllocationPanel.cs` | 배분 설정 (종목/수량 관리) |
| `Panels/HistoryPanel.cs` | 거래 내역 조회 |
| `Panels/ConfigPanel.cs` | 환경 설정 (전략유형 ComboBox) |
| `Panels/LogPanel.cs` | 전체 화면 로그 뷰 |

### 2.6-2. 환율 API 연동 (신규 1건)

| 파일 | 설명 |
|------|------|
| `Utils/ExchangeRateService.cs` | Frankfurter API + ExchangeRate-API fallback |

### 2.6-3. Weight → Qty 통일 (수정 7건)

| 파일 | 변경 내용 |
|------|----------|
| `Data/DTO/StrategyDto.cs` | `Weight(double)` → `Qty(int)` |
| `Data/DAO/StrategyDAO.cs` | CAST(WEIGHT AS INTEGER), Qty 바인딩 |
| `Core/AllocationEngine.cs` | 수량 기반 배분 계산으로 변경 |
| `Core/SmartOrderEngine.cs` | `strategy.Qty` 직접 사용 |
| `Core/Quant/RebalancingEngine.cs` | Qty 기반 비중 계산 |
| `Data/sql/create_tables.sql` | `WEIGHT INTEGER`, 초기 데이터 수량화 |
| `Forms/AllocationSetupForm.cs` | `s.Qty`, `Qty = qty` (이후 삭제됨) |

### 2.6-4. 삭제/정리 (수정 2건)

| 파일 | 변경 내용 |
|------|----------|
| `Forms/MainForm.cs` | SPA 방식 전면 재작성, ShowDialog 제거 |
| `Forms/MainForm.Designer.cs` | btn_login/order/reservation/backtest 제거, pnl_content 추가 |

### 2.6-5. 전략 프리셋 제거 (수정 2건 → 이후 삭제됨)

| 파일 | 변경 내용 |
|------|----------|
| `Forms/ConfigForm.cs` | 안정형/공격형 라디오 → 전략유형 ComboBox (이후 삭제됨) |
| `Forms/ConfigForm.Designer.cs` | rdb_balanced/aggressive 제거, cmb_strategyType 추가 (이후 삭제됨) |

### 2.6-6. 레거시 Form 파일 삭제 (삭제 8건)

| 파일 | 사유 |
|------|------|
| `Forms/ConfigForm.cs` | ConfigPanel로 완전 이전 |
| `Forms/ConfigForm.Designer.cs` | 상동 |
| `Forms/HistoryForm.cs` | HistoryPanel로 완전 이전 |
| `Forms/HistoryForm.Designer.cs` | 상동 |
| `Forms/AllocationSetupForm.cs` | AllocationPanel로 완전 이전 |
| `Forms/AllocationSetupForm.Designer.cs` | 상동 |
| `Forms/AllocationSetupForm.resx` | 상동 |
| `Forms/BacktestForm.cs` | 미사용 (MainForm에서 호출 없음) |

---

## Phase 2.5 상세 변경 이력 — 퀀트 엔진 모듈

### 핵심 변경: "단순 예약 매수" → "퀀트 조건 판단 후 매수"

```
기존 흐름:
  오후 10:30 → SmartOrderEngine → Position ≤ 0.10 이면 매수

퀀트 통합 흐름:
  오후 10:30 → SmartOrderEngine
    → OHLCV 조회
    → RSI, MACD, 볼린저밴드 계산
    → 전략 유형별 다중 조건 AND 필터
    → 모든 조건 통과 시에만 매수
    → 판단 근거 상세 로그 + 시장 스냅샷 DB 저장
```

### 2.5-1. 퀀트 지표 계산 레이어 (신규 3건)

| 파일 | 설명 |
|------|------|
| `Core/Quant/QuantIndicator.cs` | RSI(14일), MACD(12,26,9), 볼린저밴드(20일,±2σ) 계산. EMA 내부 구현 포함 |
| `Core/Quant/QuantFilter.cs` | 전략 유형별 다중 조건 AND 필터. FilterResult에 충족/미충족 조건 목록 |
| `Core/Quant/BacktestEngine.cs` | 과거 OHLCV 기반 전략 수익성 검증 (수익률, MDD, 승률) |

### 2.5-2. 리밸런싱 엔진 (신규 1건)

| 파일 | 설명 |
|------|------|
| `Core/Quant/RebalancingEngine.cs` | 보유 비중 vs 목표 비중 편차 계산 → 임계값 초과 시 자동 조정 주문 |

### 2.5-3. 데이터 레이어 확장 (신규 4건)

| 파일 | 분류 | 설명 |
|------|------|------|
| `Data/DTO/OhlcvDto.cs` | DTO | OHLCV 일봉 데이터 (시가/고가/저가/종가/거래량) |
| `Data/DTO/IndicatorDto.cs` | DTO | 퀀트 지표 결과 (RSI, MACD Line/Signal/Histogram, BB Upper/Middle/Lower, Position) |
| `Data/DTO/BacktestResultDto.cs` | DTO | 백테스팅 결과 + 개별 거래 기록 |
| `Data/DTO/MarketSnapshotDto.cs` | DTO | 매매 시점 시장 지표 스냅샷 (Phase 4 AI 학습용) |
| `Data/DAO/MarketSnapshotDAO.cs` | DAO | TB_MARKET_SNAPSHOT CRUD |

### 2.5-4. UI (신규 1건)

| 파일 | 설명 |
|------|------|
| `Forms/BacktestForm.cs` | 백테스팅 폼 — 종목/전략 선택, 기간/투자금 설정, 실행, 결과(수익률·MDD·승률) 표시, 거래 내역 그리드 |

### 2.5-5. 기존 코드 수정 (8건)

| 파일 | 변경 내용 |
|------|----------|
| `Core/IBrokerClient.cs` | `GetOhlcvAsync(ticker, days)` 메서드 추가 |
| `Core/SimBrokerClient.cs` | `GetOhlcvAsync()` 가상 OHLCV 랜덤 워크 구현 |
| `Core/SmartOrderEngine.cs` | 퀀트 지표 계산 + QuantFilter 통합 + 시장 스냅샷 자동 저장 |
| `Core/SchedulerModule.cs` | 리밸런싱 주기 도래 체크 + RebalancingEngine 자동 실행 |
| `Data/DTO/StrategyDto.cs` | `StrategyType` 필드 추가 (MEAN_REVERSION/MOMENTUM/MIXED) |
| `Data/DAO/StrategyDAO.cs` | STRATEGY_TYPE 컬럼 READ/WRITE 반영 |
| `Data/DBManager.cs` | RunMigration() 메서드 + ALTER TABLE 마이그레이션 |
| `Data/sql/create_tables.sql` | TB_MARKET_SNAPSHOT 테이블 + 리밸런싱 설정값 4개 추가 |
| `Utils/Logger.cs` | `LogQuant()` 메서드 + QUANT 로그 레벨 추가 |
| `Forms/MainForm.cs` | 백테스팅 버튼 클릭 핸들러 추가 |
| `Forms/MainForm.Designer.cs` | 사이드바에 "백테스팅" 버튼 추가 |

---

## Phase 2 상세 변경 이력

### 2-1. 엔진 코어 (신규 파일 7건)

| 파일 | 분류 | 설명 |
|------|------|------|
| `Core/IBrokerClient.cs` | 인터페이스 | 증권사 API 추상화. 로그인, 시세, 잔고, 주문 6개 메서드 정의 |
| `Core/SimBrokerClient.cs` | 구현체 | 시뮬레이션 브로커. 고정 기준가 반환 (GLD=$195, QQQM=$200 등), 환율 1,350원 고정 |
| `Core/SmartOrderEngine.cs` | 엔진 | 스마트 주문 판단. 20일 최저/최고가 기준 하위 10% 매수, 상위 10% 매도 |
| `Core/SchedulerModule.cs` | 스케줄러 | System.Timers 1분 간격. ORDER_SCHEDULE 시각에 SmartOrderEngine 자동 실행 |
| `Core/SessionManager.cs` | 세션 | IS_PAPER_TRADING 설정에 따라 SimBrokerClient 또는 LsBrokerClient(미구현) 분기 |
| `Data/DTO/HoldingDto.cs` | DTO | 보유 종목 정보 (Ticker, Qty, AvgPrice, CurrentPrice, ProfitRate) |
| `Data/DTO/PriceRangeDto.cs` | DTO | N일 가격 범위 (High, Low, Current, Position 0.0~1.0) |

### 2-2. 배분 설정 Form (신규 → 이후 AllocationPanel로 이전, 삭제됨)

| 파일 | 설명 |
|------|------|
| ~~`Forms/AllocationSetupForm.cs`~~ | 배분 설정 비즈니스 로직 (삭제됨) |
| ~~`Forms/AllocationSetupForm.Designer.cs`~~ | UI 레이아웃 (삭제됨) |

---

## Phase 3 개발 가이드 (KIS API 전환)

### 완료된 핵심 작업
1. **하네스 엔지니어링 가이드 적용**: `.agents/rules/` 디렉토리에 5개의 아키텍처/컨벤션 규칙 파일 신설. (AI 코드 생성 안정성 확보)
2. **`KisTokenManager` 구현**: KIS OAuth 토큰 발급 및 메모리 관리.
3. **`KisBrokerClient` 구현**: IBrokerClient의 KIS 증권사 REST API 구현체.
4. **`SessionManager` 분기**: App.config 설정을 통한 KIS/Sim 분기 로직 반영.

### 진행 예정 작업
- 모의투자 환경 통합 검증 및 시연
- LoginForm을 통한 인증 정보 세팅 UI 반영 (필요 시)

### 퀀트 엔진과의 연동 포인트

Phase 3 연동에 따라 `KisBrokerClient.GetOhlcvAsync()`가 KIS [해외주식] 일별 시세 API에서 실제 OHLCV 데이터를 반환하게 됩니다. 이 데이터가 `QuantIndicator`에 입력되어 **실전 시장 데이터 기반의 퀀트 지표 계산**이 작동합니다.

---

## Phase 4 AI 확장 방향

코드 내 TODO 주석으로 AI 확장 지점이 문서화되어 있습니다:

| 파일 | TODO 내용 |
|------|------------|
| `IBrokerClient.cs` | `AnalyzeMarketSentimentAsync` 메서드 추가 검토 |
| `SmartOrderEngine.cs` | `IMarketAnalyzer` 인터페이스 도입, `CombineSignals()` |
| `SessionManager.cs` | AI 엔진 인스턴스도 SessionManager에서 관리 |
| `SimBrokerClient.cs` | 시뮬레이션 결과를 AI 학습 데이터로 저장 |

### AI 학습 데이터 축적 구조 (Phase 2.5에서 준비 완료)

```
매매 시점 → SmartOrderEngine
  → MarketSnapshotDAO.Insert()
    → TB_MARKET_SNAPSHOT에 저장
      • 종목, 가격, Position, RSI, MACD, BB, 신호
      
Phase 4에서 이 데이터를 AI 모델의 Feature로 활용:
  SELECT * FROM TB_MARKET_SNAPSHOT WHERE SIGNAL = 'BUY'
  → 성공한 매수 패턴 학습
```

---

## 전략 유형 (Phase 2.5에서 추가)

| 전략 유형 | 설명 | 매수 조건 |
|-----------|------|-----------|
| `MEAN_REVERSION` | 평균회귀 (기본) | Position ≤ 0.10 AND RSI ≤ 30 AND BB 하단 근접 |
| `MOMENTUM` | 모멘텀 | RSI ≥ 50 AND MACD Histogram > 0 AND MACD Line > 0 |
| `MIXED` | 혼합 | Position ≤ 0.10 AND RSI < 70 |

---

## 리밸런싱 설정 (Phase 2.5에서 추가)

| 설정 KEY | 기본값 | 설명 |
|----------|--------|------|
| `REBALANCE_ENABLED` | `0` | 1=활성화, 0=비활성화 |
| `REBALANCE_PERIOD` | `MONTHLY` | WEEKLY 또는 MONTHLY |
| `REBALANCE_THRESHOLD` | `0.05` | 편차 5% 초과 시 리밸런싱 |
| `LAST_REBALANCE_DATE` | (빈값) | 마지막 리밸런싱 실행일 |

---

## 파일 변경 이력 요약

### 신규 파일 (Phase 2.5)
| # | 파일 경로 | 용도 |
|---|----------|------|
| 1 | `Core/Quant/QuantIndicator.cs` | RSI, MACD, 볼린저밴드 계산 |
| 2 | `Core/Quant/QuantFilter.cs` | 전략별 다중 조건 AND 필터 |
| 3 | `Core/Quant/BacktestEngine.cs` | 과거 데이터 기반 전략 검증 |
| 4 | `Core/Quant/RebalancingEngine.cs` | 보유 비중 자동 재조정 |
| 5 | `Data/DTO/OhlcvDto.cs` | OHLCV 일봉 데이터 |
| 6 | `Data/DTO/IndicatorDto.cs` | 퀀트 지표 결과 |
| 7 | `Data/DTO/BacktestResultDto.cs` | 백테스팅 결과 |
| 8 | `Data/DTO/MarketSnapshotDto.cs` | 시장 스냅샷 (AI 학습용) |
| 9 | `Data/DAO/MarketSnapshotDAO.cs` | 스냅샷 CRUD |
| 10 | `Forms/BacktestForm.cs` | 백테스팅 UI |

### 수정 파일 (Phase 2.5)
| # | 파일 경로 | 변경 요약 |
|---|----------|----------|
| 1 | `Core/IBrokerClient.cs` | +GetOhlcvAsync |
| 2 | `Core/SimBrokerClient.cs` | +GetOhlcvAsync 가상 데이터 |
| 3 | `Core/SmartOrderEngine.cs` | 퀀트 필터 통합, 스냅샷 저장, 상세 로그 |
| 4 | `Core/SchedulerModule.cs` | 리밸런싱 주기 체크 + 실행 |
| 5 | `Data/DTO/StrategyDto.cs` | +StrategyType 필드 |
| 6 | `Data/DAO/StrategyDAO.cs` | STRATEGY_TYPE 컬럼 반영 |
| 7 | `Data/DBManager.cs` | +RunMigration(), ALTER TABLE |
| 8 | `Data/sql/create_tables.sql` | +TB_MARKET_SNAPSHOT, +리밸런싱 설정 |
| 9 | `Utils/Logger.cs` | +LogQuant(), +QUANT 레벨 |
| 10 | `Forms/MainForm.cs` | +btn_backtest_Click |
| 11 | `Forms/MainForm.Designer.cs` | +btn_backtest 버튼 |
