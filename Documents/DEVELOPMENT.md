# 개발 진척도 (CHANGELOG)

> 이 문서는 AutoInvesting 프로젝트의 개발 진행 상황을 기록합니다.
> 새 개발자가 이 문서를 보고 현재 상태와 다음 작업을 파악할 수 있도록 작성합니다.

---

## 현재 상태: Phase 2 완료 ✅

- **Phase 1** (기반): ✅ 완료
- **Phase 2** (엔진 코어 + 배분 UI): ✅ 완료
- **Phase 3** (LS증권 실거래 연동): 📋 미착수
- **Phase 4** (AI 시장분석): 📋 미착수

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

### 2-2. 배분 설정 Form (신규 파일 2건)

| 파일 | 설명 |
|------|------|
| `Forms/AllocationSetupForm.cs` | 배분 설정 비즈니스 로직 |
| `Forms/AllocationSetupForm.Designer.cs` | UI 레이아웃 (VS Designer 호환) |

**기능**:
- 목표 금액 입력 (천단위 콤마 자동 포맷, 숫자만 입력 가능)
- 종목 추가 → `IBrokerClient.GetCurrentPriceAsync()` 호출 → 단가($) / 단가(₩) 표시
- 수량 컬럼만 편집 가능 → `단가(₩) × 수량 = 금액` 자동 계산
- 잔여금 실시간 표시 (양수=초록, 음수=빨간 경고)
- 가격 새로고침 (전 종목 단가 + 환율 재조회)
- 저장 → `TB_INVEST_STRATEGY`에 "사용자정의" 전략으로 DB 저장

### 2-3. 기존 코드 수정 (5건)

| 파일 | 변경 내용 |
|------|----------|
| `Data/DAO/StrategyDAO.cs` | `SaveStrategy()`, `DeleteStrategy()` 메서드 추가 (트랜잭션 사용) |
| `Forms/MainForm.cs` | 배분 설정 버튼 연결 + `LoadAllocationCards()` 메서드 추가 (대시보드에 배분 결과 표시) |
| `Controls/AllocationCardControl.Designer.cs` | 슬림 리디자인 (200×100 → 645×32, 한 줄 가로형) |
| `Controls/AllocationCardControl.cs` | 프로그레스바 너비 645 기준, 위치 보정 제거 |
| `Utils/Logger.cs` | dead code 수정 (`AppendToListBox` 실제 호출) + 자동 스크롤 |

---

## Phase 3 개발 가이드 (다음 작업)

### 필요한 선행 작업
1. LS증권 OPEN API 포털에서 **APP KEY / APP SECRET 발급**
2. 해외주식 API TR 코드 확인 (시세/주문/계좌/차트)
3. 모의투자 환경 APP KEY 별도 발급

### 구현 순서 (권장)

#### 1단계: 로그인 Form
```
[NEW] Forms/LoginForm.cs
- APP KEY, APP SECRET, 계좌번호 입력
- "로그인" 버튼 → OAuth 토큰 발급 요청
- 성공 시 SessionManager에 LsBrokerClient 등록
- TB_APP_CONFIG에 인증 정보 저장 (암호화 권장)
```

#### 2단계: LsBrokerClient
```
[NEW] Core/LsBrokerClient.cs : IBrokerClient
- LoginAsync: POST /oauth2/token → Access Token
- GetCurrentPriceAsync: 해외주식 현재가 TR
- GetPriceRangeAsync: 해외주식 일봉 차트 TR
- GetExchangeRateAsync: 환율 조회 TR
- GetHoldingsAsync: 해외주식 잔고 조회 TR
- PlaceBuyOrderAsync: 해외주식 매수 주문 TR
- PlaceSellOrderAsync: 해외주식 매도 주문 TR

주의사항:
- Access Token 유효기간: 익일 07시까지 → 자동 갱신 필요
- TR별 TPS 제한 → 요청 간 딜레이 삽입
- Authorization 헤더: "Bearer {ACCESS_TOKEN}"
```

#### 3단계: SessionManager 분기
```
[MODIFY] Core/SessionManager.cs
- IS_PAPER_TRADING == "0" → new LsBrokerClient(appKey, appSecret, accountNo)
- 현재 TODO 주석 위치에 구현
```

#### 4단계: MainForm 로그인 버튼 연결
```
[MODIFY] Forms/MainForm.cs
- btn_login_Click → LoginForm.ShowDialog()
- 로그인 성공 후 대시보드 갱신
```

### SimBrokerClient 기준가 (Phase 3에서 실제 시세로 교체)

| Ticker | SimBroker 기준가 | 비고 |
|--------|-----------------|------|
| SCHD | $27.50 | Schwab US Dividend Equity ETF |
| QQQM | $200.00 | Invesco NASDAQ 100 ETF |
| GLD | $195.00 | SPDR Gold Shares |
| JEPI | $56.00 | JPMorgan Equity Premium Income |
| SPLG | $62.00 | SPDR Portfolio S&P 500 ETF |
| (기타) | $100.00 | 미등록 종목 기본값 |

---

## Phase 4 AI 확장 방향 (TODO 주석 위치)

코드 내 TODO 주석으로 AI 확장 지점이 문서화되어 있습니다:

| 파일 | TODO 내용 |
|------|----------|
| `IBrokerClient.cs` | `AnalyzeMarketSentimentAsync` 메서드 추가 검토 |
| `SmartOrderEngine.cs` | `IMarketAnalyzer` 인터페이스 도입, 차트+뉴스+커뮤니티 감성 분석, `TB_MARKET_FEATURES` 테이블 |
| `SessionManager.cs` | AI 엔진 인스턴스도 SessionManager에서 관리 |
| `SimBrokerClient.cs` | 시뮬레이션 결과를 AI 학습 데이터로 저장 |
| `AllocationSetupForm.cs` | AI 추천 종목/수량 자동 입력 기능 |

### 최종 목표 구조
```
SmartOrderEngine
    ├── position 기반 판단 (현재)
    │   → (현재가 - 최저가) / (최고가 - 최저가)
    │
    └── AI 분석 결과 종합 (Phase 4)
        ├── IMarketAnalyzer.AnalyzeAsync(ticker)
        │   ├── 차트 기술적 지표
        │   ├── 뉴스 감성 분석 (해외 포함)
        │   ├── 커뮤니티 감성 (Reddit, X, StockTwits)
        │   └── 매크로 지표 (금리, VIX)
        │
        └── CombineSignals(positionSignal, aiSignal)
            → 최종 BUY / SELL / HOLD 결정
```

---

## 파일 변경 이력 요약

### 신규 파일 (Phase 2)
| # | 파일 경로 | 용도 |
|---|----------|------|
| 1 | `Core/IBrokerClient.cs` | 증권사 API 인터페이스 |
| 2 | `Core/SimBrokerClient.cs` | 시뮬레이션 구현체 |
| 3 | `Core/SmartOrderEngine.cs` | 스마트 주문 엔진 |
| 4 | `Core/SchedulerModule.cs` | 예약 주문 스케줄러 |
| 5 | `Core/SessionManager.cs` | 세션 관리 |
| 6 | `Data/DTO/HoldingDto.cs` | 보유 종목 DTO |
| 7 | `Data/DTO/PriceRangeDto.cs` | 가격 범위 DTO |
| 8 | `Forms/AllocationSetupForm.cs` | 배분 설정 Form |
| 9 | `Forms/AllocationSetupForm.Designer.cs` | 배분 설정 UI |

### 수정 파일 (Phase 2)
| # | 파일 경로 | 변경 요약 |
|---|----------|----------|
| 1 | `Data/DAO/StrategyDAO.cs` | +SaveStrategy, +DeleteStrategy |
| 2 | `Forms/MainForm.cs` | +LoadAllocationCards, +배분 설정 연결 |
| 3 | `Forms/MainForm.Designer.cs` | KIS→LS증권 (이전 세션) |
| 4 | `Controls/AllocationCardControl.cs` | 슬림 리디자인 |
| 5 | `Controls/AllocationCardControl.Designer.cs` | 645×32 한 줄형 |
| 6 | `Utils/Logger.cs` | AppendToListBox 활용 + 자동 스크롤 |
| 7 | `AutoInvest.csproj` | 신규 파일 등록 |
| 8 | `README.md` | 전체 업데이트 |
