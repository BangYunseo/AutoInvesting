# 🔍 AutoInvesting 소스 코드 리딩 가이드

개발자가 로직의 흐름을 완벽하게 파악하기 위해, 어떤 파일부터 어떤 순서로 읽어야 하는지 정리한 내비게이션(Navigation) 문서입니다. 아래 소개된 **Step 순서대로 코드를 열어서 흐름을 따라가시면** 가장 이해하기 쉽습니다!

> **핵심 전제 (Phase 6)**: 이 시스템은 더 이상 "언제 살까"를 판단하지 않습니다. 정직한 백테스트 결과
> 타이밍 판단이 단순 적립식(DCA)에 열세임이 검증되어, **퀀트/AI 판단 레이어를 전부 제거**하고
> **정해진 목표비중대로 정수 단위 매수만 하는 적립(DCA) 코어**로 전환했습니다. 가치는 "판단"이 아니라 "자동화"에 있습니다.

---

## Step 1: 프로그램의 시작점 (Entry Point)
가장 먼저 백엔드 서버가 어떻게 켜지고 설정되는지 확인하세요.

1. **`Program.cs`**
   - **핵심 포인트**: ASP.NET Core의 시작점입니다. 여기서 `SessionManager`, `DBManager` 등 시스템에서 하나만 존재하는 싱글턴(Singleton) 객체들이 등록됩니다.
   - **주목할 코드**: `builder.Services.AddScoped<DailyExecutionService>();` (적립 실행 서비스 등록) 및 보안 필터인 `ApiKeyAuthAttribute`가 전역으로 등록되는 모습.

## Step 2: 적립 실행 진입점 (DCA Cycle)
외부 크론잡이 매수 주기에 호출하면 적립 사이클을 시작하는 진입점입니다.

2. **`Core/DailyExecutionService.cs`**
   - **핵심 포인트**: `RunDcaCycleAsync()` 단 하나의 흐름만 가집니다. 로그인 → `DcaSettings.Load()`로 목표비중·예산 로드 → `DcaAccumulationEngine.AccumulateAsync()` 실행 → 결과 이메일 보고서 발송.
   - **주목할 코드**: 로그인 실패·목표비중 없음 같은 조기 종료 시에도 `finally`의 `SendDcaReportAsync()`로 항상 보고서를 보내는 부분. 판단·타이밍 로직이 전혀 없다는 점을 확인하세요.

## Step 3: 적립 매수 엔진 (The Accumulator)
실행 명령이 떨어졌을 때, 목표비중대로 정수 매수 계획을 세우고 주문을 던지는 핵심 클래스입니다.

3. **`Core/DcaAccumulationEngine.cs`**
   - **핵심 포인트**: 가장 핵심 파일입니다! 퀀트/AI 판단을 일절 하지 않고 "예산을 목표비중을 향해 정수 단위로 매수 + 잔돈 이월"만 수행합니다.
   - **읽는 순서**:
     - `PlanPurchases()`: ⭐️ **순수 함수(외부 I/O 없음)!** 목표비중과 예산·환율·현재가·보유수량을 받아, "현재 비중이 목표 대비 가장 부족한 종목"을 1주씩 정수로 매수하는 계획을 계산합니다. 더 이상 1주도 못 사면 종료하고 남는 예산은 `leftoverKrw`로 이월. 외부 의존성이 없어 **단위 테스트로 검증 가능**합니다.
     - `AccumulateAsync()`: 현재가/보유/환율을 브로커에서 조회 → `PlanPurchases()`로 계획 산출 → 계획대로 `PlaceBuyOrderAsync()` 주문 실행 → `TradeHistoryDAO.Insert()`로 기록.

## Step 4: 적립 설정의 단일 진실 원천 (Settings)
"무엇을, 얼마나" 사는지는 전부 이 한 곳에서 읽고 씁니다.

4. **`Core/DcaSettings.cs`**
   - **핵심 포인트**: 목표비중·예산의 단일 읽기/쓰기 지점입니다. 우선순위는 **DB(`TB_APP_CONFIG`: 키 `DCA_TARGETS` JSON, `DCA_BUDGET_KRW`) → `appsettings.json`의 `Dca` 섹션 폴백** 순서입니다.
   - **주목할 코드**: `LoadTargets()`가 DB JSON을 먼저 보고, 없거나 파싱 실패하면 `Dca:Targets`로 폴백하는 부분. `Save()`는 UI에서 편집한 값을 DB에 기록하여 다음 사이클부터 반영합니다.

## Step 5: 증권사 통신 (팔다리)
"매수!" 계획이 섰을 때 실제로 주식시장(한국투자증권)에 주문을 던지는 곳입니다.

5. **`Core/IBrokerClient.cs`**
   - **핵심 포인트**: 모의투자와 실전투자를 하나의 개념으로 묶어주는 통일 인터페이스입니다. 현재가·OHLCV·잔고 조회와 매수/매도 주문을 정의합니다.
6. **`Core/SimBrokerClient.cs`** (먼저 읽기 권장)
   - **핵심 포인트**: API 키 없이도 전체 적립 흐름을 검증할 수 있는 시뮬레이션 구현체입니다. 신규 로직 검증은 항상 여기서 먼저 합니다.
7. **`Core/KisBrokerClient.cs`**
   - **핵심 포인트**: KIS(한국투자증권) 전용 통신 코드입니다. `PlaceBuyOrderAsync` 부분을 보시면 세팅된 API 키와 토큰을 넣고 진짜 HTTP 요청을 KIS로 쏩니다. 일시적 오류(429/5xx)는 `Polly Retry`로 지수 백오프 재시도합니다.
   - KIS 공식 문서와 비교하며 읽으시면 가장 좋습니다.

## Step 6: 사용자가 내리는 명령 (API Controllers)
크론잡이 자동으로 돌기도 하지만, 사용자가 직접 UI에서 트리거하거나 설정을 바꿀 때 호출되는 곳입니다.

8. **`Controllers/OrderController.cs`**
   - **핵심 포인트**: `POST /api/order/dca-run`은 적립 사이클을 백그라운드로 시작하고 **즉시 202**를 반환합니다(처리 결과는 로그·이메일). `POST /api/order/manual`은 신호 판단 없이 즉시 매수/매도하는 수동 주문입니다.
9. **`Controllers/DcaController.cs`**
   - **핵심 포인트**: `GET /api/dca/config`로 현재 적용 중인 목표비중·예산을 조회하고, `PUT /api/dca/config`로 저장합니다(`DcaSettings.Save` → DB 기록 → 다음 사이클 반영).
10. **`Controllers/ConfigController.cs` / `PortfolioController.cs` / `HistoryController.cs`**
    - 각각 환경 설정 변경, 잔고 조회, 거래 내역 조회 API입니다.

---

### 🎉 읽기 꿀팁!

- 코드 에디터(Visual Studio 등)를 여시고, **Step 1(Program.cs)부터 하나씩 F12(정의로 이동) 키를 누르면서 흐름을 따라가 보세요.**
- 가장 먼저 이해할 핵심은 `DcaAccumulationEngine.PlanPurchases()`의 "목표 대비 가장 부족한 종목을 1주씩 채운다"는 배분 규칙입니다. 외부 I/O가 없는 순수 함수이므로, 입력(목표비중·예산·환율·현재가·보유수량)을 직접 넣어보며 출력(매수 수량·잔돈)을 따라가면 금방 머리에 들어옵니다.
- **참고(레거시)**: `TB_MARKET_SNAPSHOT` 테이블은 과거 데이터 보존을 위해 스키마에 남아 있으나, Phase 6 이후 어디서도 기록·조회하지 않는 미사용 데이터입니다. 코드 흐름에는 등장하지 않습니다.
