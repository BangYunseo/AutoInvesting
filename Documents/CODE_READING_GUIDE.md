# 🔍 AutoInvesting 소스 코드 리딩 가이드

개발자가 로직의 흐름을 완벽하게 파악하기 위해, 어떤 파일부터 어떤 순서로 읽어야 하는지 정리한 내비게이션(Navigation) 문서입니다. 아래 소개된 **Step 순서대로 코드를 열어서 흐름을 따라가시면** 가장 이해하기 쉽습니다!

---

## Step 1: 프로그램의 시작점 (Entry Point)
가장 먼저 백엔드 서버가 어떻게 켜지고 설정되는지 확인하세요.

1. **`Program.cs`** 
   - **핵심 포인트**: ASP.NET Core의 시작점입니다. 여기서 `SessionManager`, `DBManager` 등 시스템에서 하나만 존재하는 싱글턴(Singleton) 객체들이 등록됩니다.
   - **주목할 코드**: `builder.Services.AddScoped<DailyExecutionService>();` (매매 실행 서비스 등록) 및 보안 필터인 `ApiKeyAuthAttribute`가 전역으로 등록되는 모습.

## Step 2: 매매 실행 진입점 (Daily Execution)
설정된 시각이 되면 매매 루프를 시작하는 진입점입니다.

2. **`Core/DailyExecutionService.cs`**
   - **핵심 포인트**: 설정된 매매 시각(예: 22:30, `appsettings.json > Trading.OrderSchedule`)에 `SmartOrderEngine`을 호출합니다. `IServiceScopeFactory`를 통해 Scoped 생명주기로 실행됩니다.
   - **주목할 코드**: `ExecuteAsync()` 메서드 내부에서 어떻게 `SmartOrderEngine`을 구성하고 실행하는지 보세요.

## Step 3: 주문의 뇌 (The Orchestrator)
실행 명령이 떨어졌을 때(자동 또는 수동 API), 전체 과정을 지휘하는 핵심 클래스입니다.

3. **`Core/SmartOrderEngine.cs`** 
   - **핵심 포인트**: 가장 핵심 파일입니다! 수십 개의 종목을 순회하며 "조회 → 퀀트 판정 → (환율 코멘트) → 매수/매도 실행"을 관장합니다. **현재 매매 결정은 퀀트 단독**이며 AI 호출은 없습니다.
   - **읽는 순서**: 
     - `ExecuteSmartOrdersAsync()`: 다수 종목 루프 (투자금 배분)
     - `AnalyzeAsync()`: 단일 종목의 가격/OHLCV를 가져와 `QuantFilter`로 매수/매도/보류를 결정하는 로직
     - `FxRateAdvisor`: 정해진 매매 방향에 환율 유불리를 설명·경고로 첨부(매매를 막지 않음)
   - ⚠️ **(휴면)** `CalculateConsensusScore()`(퀀트40%+차트AI30%+펀더멘털AI30% 확률 합산)와 AI 분석 호출은 **주석으로 비활성화(보존)** 되어 있습니다. 읽으면 과거 합의 방식을 이해하는 데 도움이 되지만, 현재 실행 경로는 아닙니다.

## Step 4: 퀀트 분석 엔진 (수학적 판단)
스마트 오더 엔진이 "퀀트 점수 좀 알려줘"라고 할 때 쓰이는 모듈입니다.

4. **`Core/Quant/QuantIndicator.cs`**
   - **핵심 포인트**: 순수 수학 공식 파일입니다. 증권사에서 받아온 과거 OHLCV(일봉) 데이터를 이용해 화면에 그리는 RSI, MACD, 볼린저 밴드를 계산합니다.
5. **`Core/Quant/QuantFilter.cs`**
   - **핵심 포인트**: 계산된 수치가 '내 조건에 맞는지' 검사합니다. 
   - **주목할 코드**: `CheckMeanReversionBuy()` 메서드에서 `Rsi14 <= 45` 등 수치를 비교하여 `Passed`를 `true`로 만드는 과정.

## Step 5: AI 분석 엔진 (Gemini) — ⚠️ 현재 휴면
> 아래 파일들은 Phase 4~6에서 만든 AI 결정 경로입니다. **현재 매매 결정에는 사용되지 않으며**(SmartOrderEngine에서 주석 비활성화),
> 코드는 향후 재활성화를 위해 **삭제하지 않고 보존**되어 있습니다. AI가 어떻게 연동되었는지 이해하려는 목적으로만 읽으세요.

6. **`Utils/PromptBuilder.cs`**
   - **핵심 포인트**: 숫자로 된 OHLCV와 퀀트 지표를, AI가 알아듣기 쉬운 "텍스트"로 예쁘게 포장(프롬프트 엔지니어링)하는 파일입니다.
7. **`Core/GeminiMarketAnalyzer.cs`**
   - **핵심 포인트**: 포장된 텍스트를 들고 구글 서버로 직접 통신을 나갑니다.
   - **주목할 코드**: `GetMarketAnalysisAsync()` 메서드에서 HTTP 요청을 쏘는 부분. 만약 너무 많이 쏴서 에러(429)가 나면 1초 쉬고 다시 쏘는 `Polly Retry` 마법이 들어 있습니다.
8. **`Core/AiMarketAnalyzer.cs`** (번외)
   - 만약 Gemini 키가 안 꽂혀 있으면 `SessionManager`가 대신 이 가짜(Mock) 파일을 구동시킵니다. 내부를 보시면 아주 단순한 if문으로 AI인 척하는 코드입니다.

## Step 6: 증권사 통신 (팔다리)
분석이 끝나고 "매수!" 결정이 났을 때 실제로 주식시장(한국투자증권)에 주문을 던지는 곳입니다.

9. **`Core/IBrokerClient.cs`**
   - **핵심 포인트**: 모의투자와 실전투자를 하나의 개념으로 묶어주는 통일 인터페이스입니다.
10. **`Core/KisBrokerClient.cs`**
   - **핵심 포인트**: KIS(한국투자증권) 전용 통신 코드입니다. `PlaceBuyOrderAsync` 부분을 보시면, 이전에 세팅된 API 키와 토큰을 넣고 진짜 HTTP 요청을 KIS로 쏩니다.
   - KIS 공식 문서와 비교하며 읽으시면 가장 좋습니다.

## Step 7: 사용자가 내리는 수동 명령 (API Controllers)
백그라운드가 혼자 돌기도 하지만, 사용자가 직접 UI 버튼을 눌렀을 때 호출되는 곳입니다.

11. **`Controllers/OrderController.cs`** 
    - **핵심 포인트**: 포스트맨(Postman)이나 개발하신 프론트엔드에서 `/api/Order/execute`를 호출하면 똑같이 `SmartOrderEngine`을 생성해서 매매를 실행합니다.
12. **`Controllers/ConfigController.cs`**
    - 설정값을 바꾸는 API입니다.
13. **`Controllers/BacktestController.cs`**
    - 과거 데이터를 긁어와서 시뮬레이션을 돌려보는 백테스트 전용 API입니다.

---

### 🎉 읽기 꿀팁!

- 코드 에디터(Visual Studio 등)를 여시고, **Step 1(Program.cs)부터 하나씩 F12(정의로 이동) 키를 누르면서 흐름을 따라가 보세요.**
- 도중에 이해가 되지 않는 수식이나 로직(예: `CalculateConsensusScore`에서 퀀트 1차 관문이 수식만으로 자동 보장되는 이유 등)이 있다면 주석 처리되어 있는 설명을 읽어보시거나, AI에게 해당 파일을 보여주면서 질문하시면 금방 구조가 머리에 쏙 들어올 것입니다!
