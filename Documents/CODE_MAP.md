# 🗺️ AutoInvesting 코드 맵 (전체 파일 색인)

> "어느 파일에 어느 코드가 있는지" 한눈에 찾는 자동 생성 색인입니다.
> **이 파일을 직접 수정하지 마세요.** 각 소스 파일의 XML `<summary>` 주석이 진실 원천이며,
> `pwsh Documents/regen-codemap.ps1` 실행으로 재생성됩니다.
>
> ⚠️ 표시 = 해당 파일에 클래스 `<summary>` 주석이 없습니다 → 코드에 추가하면 다음 재생성 때 채워집니다.

## 진입점 (Entry Point)

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `Program.cs` | class | 자동 투자 시스템 진입점 (ASP.NET Core Web API). | `Main` |

## Core — 비즈니스 로직

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AiMarketAnalyzer.cs` | class | AI 시장 분석 엔진의 임시(Mock) 구현체입니다 (Phase 4 초기 단계). | `AnalyzeAsync` |
| `DailyExecutionService.cs` | class | 외부 크론잡(Cron-job.org, GitHub Actions 등)에 의해 하루에 한 번 호출되는 일일 사이클 실행기. | `RunDailyCycleAsync` |
| `GeminiMarketAnalyzer.cs` | class | Google Gemini API를 사용하는 다중 에이전트 AI 시장 분석 엔진 구현체 (Phase 4-d). | `AnalyzeAsync` |
| `IBrokerClient.cs` | interface | 증권사 API 추상화 인터페이스. | — |
| `IMarketAnalyzer.cs` | interface | AI 시장 분석 엔진 인터페이스 (Phase 4). | — |
| `KisBrokerClient.cs` | class | KIS (한국투자증권) API 실거래 브로커 클라이언트. | `LoginAsync`, `GetCurrentPriceAsync`, `GetExchangeRateAsync`, `GetHoldingsAsync`, `GetCashBalanceAsync` |
| `KisTokenManager.cs` | class | KIS (한국투자증권) API OAuth 토큰 관리자. | `EnsureValidTokenAsync`, `GetToken` |
| `SessionManager.cs` | class | IBrokerClient 인스턴스의 생명주기를 관리합니다. | `GetClient`, `GetAnalyzer`, `Reset` |
| `SimBrokerClient.cs` | class | 시뮬레이션 브로커 클라이언트. | `LoginAsync`, `GetCurrentPriceAsync`, `GetExchangeRateAsync`, `GetHoldingsAsync`, `GetCashBalanceAsync` |
| `SmartOrderEngine.cs` | class | 스마트 주문 엔진 (Phase 4-e — 퀀트 + 다중 AI 에이전트 확률 기반 합의). | `AnalyzeAsync`, `ExecuteSmartOrdersAsync`, `AnalyzeAndSaveSnapshotAsync` |

## Core/Quant — 퀀트 분석

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AdaptiveThresholdEngine.cs` | class | Phase 5-a: 종목별 적응형 임계값 산출 엔진. | `GetStatus` |
| `BacktestEngine.cs` | class | 백테스팅 엔진. | `RunAsync` |
| `PerformanceFeedbackEngine.cs` | class | Phase 5-d: 성과 기반 피드백 엔진. | `GetAgentAccuracy`, `RunWeightAbTest` |
| `QuantFilter.cs` | class | 퀀트 다중 조건 AND 필터. | `CheckBuyCondition`, `CheckSellCondition` |
| `QuantIndicator.cs` | class | 퀀트 기술적 지표 계산기. | `CalculateRsi`, `CalculateAll` |
| `RebalancingEngine.cs` | class | 리밸런싱 엔진. | `ExecuteAsync`, `IsDue` |
| `SellStrategyManager.cs` | class | ⚠️ (요약 없음) | `ProcessActivePlansAsync` |
| `SimTrainingDataGenerator.cs` | class | Phase 6-a: SimBroker(시뮬레이션) 기반 AI 학습데이터 대량 생성기. | `GenerateAsync` |

## Core/Advisors — 컨텍스트 조언

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AdvisoryContext.cs` | class | 부가 조언 생성에 필요한 매매 컨텍스트 (Phase 5-e). | — |
| `ContextAdvisorService.cs` | class | 등록된 모든 를 실행하여 부가 조언 목록을 수집합니다 (Phase 5-e). | `GatherAsync` |
| `FxRateAdvisor.cs` | class | 환율 기반 매매 컨텍스트 조언 제공자. | `EvaluateAsync` |
| `IContextAdvisor.cs` | interface | 상황 기반 부가 조언 제공자 (Phase 5-e). | — |

## Controllers — REST API

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AuthController.cs` | class | 단일 관리자 로그인 API. | `GetStatus`, `Setup`, `Login` |
| `BacktestController.cs` | class | 백테스트 실행 API 과거 데이터 기반 전략 수익성 검증 | `RunBacktest` |
| `ConfigController.cs` | class | 시스템 설정 값 (API 키, 전략 등)을 조회하고 변경하는 API. | `GetAllConfigs`, `UpdateConfig`, `RevealSecret`, `GetGeminiModels` |
| `HistoryController.cs` | class | 매매 이력과 시스템 로그를 조회하는 API. | `GetTradeHistory`, `GetSystemLogs` |
| `MonitoringController.cs` | class | AI 판단 성과 및 토큰 사용량/비용을 조회하는 모니터링 API (Phase 5-b). | `GetSummary`, `GetPerformance`, `GetTokensByAgent`, `GetTokensDaily`, `GetAgentAccuracy` |
| `OrderController.cs` | class | 수동 주문 트리거 API. | `ExecuteSmartOrders`, `RunDailyCycle`, `PlaceManualOrder`, `AnalyzeTicker` |
| `PortfolioController.cs` | class | 투자 자산 배분 및 잔고를 조회하는 API. | `GetHoldings`, `GetSummary` |
| `QuantController.cs` | class | 실시간 종목 퀀트 분석 API. | `AnalyzeTicker` |
| `SellPlanController.cs` | class | ⚠️ (요약 없음) | `GetActivePlans`, `CreatePlan`, `CancelPlan` |
| `SimController.cs` | class | Phase 6-a: 시뮬레이션 학습데이터 생성·검증 API. | `GenerateTrainingData`, `VerifyTrainingData` |
| `StrategyController.cs` | class | 투자 전략 CRUD API. | `GetStrategySummaries`, `GetAssetMaster`, `GetStrategy`, `GetAdaptiveStatus`, `SaveStrategy` |
| `TestController.cs` | class | ⚠️ (요약 없음) | `InjectMockData`, `TestAdaptive`, `Buy`, `SendDailyReport`, `SendTestEmail` |

## Data/DTO — 데이터 전송 객체

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AdaptiveThresholdStatusDto.cs` | class | 종목별 적응형 임계값 진단 상태. | — |
| `AdvisoryNoteDto.cs` | class | 매매 신호와 별개로, 상황 컨텍스트(환율·변동성 등)에 따라 사용자에게 제공되는 부가 조언 (Phase 5-e). | — |
| `AgentAccuracyDto.cs` | class | Phase 5-d: 에이전트(퀀트/차트AI/펀더멘털AI)별 실측 적중률 집계 결과 DTO. | — |
| `AgentTokenSummaryDto.cs` | class | 에이전트 유형별 토큰 사용량 집계 결과 (모니터링용). | — |
| `AiPerformanceDto.cs` | class | ⚠️ (요약 없음) | — |
| `AssetMasterDto.cs` | class | 자산 마스터(TB_ASSET_MASTER) 한 종목 정보. | — |
| `BacktestResultDto.cs` | class | 백테스팅 결과 DTO. | — |
| `ConsensusScoreDto.cs` | class | 확률 기반 합의 스코어링 결과 DTO (Phase 4-e). | — |
| `DailyTokenUsageDto.cs` | class | 일자별 토큰 사용량 집계 결과 (모니터링 추이 차트용). | — |
| `HoldingDto.cs` | class | 보유 종목(잔고) DTO. | — |
| `IndicatorDto.cs` | class | 퀀트 지표 계산 결과 DTO. | — |
| `MarketSnapshotDto.cs` | class | 매매 시점 시장 지표 스냅샷 DTO. | — |
| `OhlcvDto.cs` | class | OHLCV 일봉 데이터 DTO. | — |
| `PriceRangeDto.cs` | class | N일 가격 범위 DTO. | — |
| `SellPlanDto.cs` | class | ⚠️ (요약 없음) | — |
| `StrategyDto.cs` | class | 투자 전략 DTO. | — |
| `StrategySummaryDto.cs` | class | ⚠️ (요약 없음) | — |
| `TokenUsageDto.cs` | class | ⚠️ (요약 없음) | — |
| `TradeHistoryDto.cs` | class | 거래 내역 DTO. | — |
| `WeightSchemeResultDto.cs` | class | Phase 5-d: 합의 가중치 조합(Scheme) A/B 백테스트 결과 DTO. | — |

## Data/DAO — DB 접근

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AiPerformanceDAO.cs` | class | ⚠️ (요약 없음) | `Insert`, `GetUnevaluated`, `UpdateEvaluation`, `GetRecent` |
| `MarketSnapshotDAO.cs` | class | 시장 스냅샷 DAO. | `Insert`, `GetByTicker`, `GetRecentAll`, `GetHistoricalSellProbabilities`, `GetHistoricalProbabilities` |
| `SellPlanDAO.cs` | class | ⚠️ (요약 없음) | `GetAllActivePlans`, `GetPlansByTicker`, `Insert`, `Update` |
| `StrategyDAO.cs` | class | ⚠️ (요약 없음) | `GetStrategy`, `GetStrategySummaries`, `GetAssetMaster`, `SaveStrategy`, `DeleteStrategy` |
| `TokenUsageDAO.cs` | class | ⚠️ (요약 없음) | `Insert`, `GetTodayTotalTokens`, `GetUsageByAgent`, `GetDailyUsage` |
| `TradeHistoryDAO.cs` | class | ⚠️ (요약 없음) | `Insert`, `GetRecent` |

## Data — DB/설정 관리

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AppConfigManager.cs` | class | 애플리케이션 설정값을 통합 관리합니다. | `Initialize`, `Get`, `Set`, `GetMap` |
| `DBManager.cs` | class | ⚠️ (요약 없음) | `GetConnection` |

## Utils — 유틸리티/통신

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `ApiKeyAuthAttribute.cs` | class | 글로벌 인증 필터. | `OnActionExecutionAsync` |
| `CryptoUtil.cs` | class | 시크릿 암복호화 · 비밀번호 해시 · 세션 토큰 발급/검증을 담당하는 공용 암호화 유틸리티입니다. | `Initialize`, `EncryptSecret`, `DecryptSecret`, `IsEncrypted`, `HashPassword` |
| `ExchangeRateService.cs` | class | 무료 환율 API를 통해 USD/KRW 환율을 조회합니다. | `GetUsdKrwAsync` |
| `Logger.cs` | class | 시스템 로깅 유틸리티 (Serilog 래퍼). | `Initialize`, `Info`, `Error`, `Warn`, `Fatal` |
| `NotificationService.cs` | class | 관리자 알림 메일 발송 서비스. | `Initialize`, `SendEmailAsync`, `SendEmailOrThrowAsync`, `GetConfigStatus` |
| `PromptBuilder.cs` | class | QuantIndicator 결과와 OHLCV 데이터를 Gemini API가 이해할 수 있는 텍스트 프롬프트로 변환합니다. | `BuildSystemPrompt`, `BuildUserPrompt`, `BuildFundamentalSystemPrompt`, `BuildFundamentalUserPrompt`, `BuildCombinedSystemPrompt` |
| `PublicEndpointAttribute.cs` | class | 전역 인증 필터()를 면제하는 마커 어트리뷰트입니다. | — |

---

**총 70개 파일** · 요약 없는 파일 **13개**

<details><summary>⚠️ XML &lt;summary&gt; 보강이 필요한 파일</summary>

- `Core/Quant/SellStrategyManager.cs`
- `Controllers/SellPlanController.cs`
- `Controllers/TestController.cs`
- `Data/DTO/AiPerformanceDto.cs`
- `Data/DTO/SellPlanDto.cs`
- `Data/DTO/StrategySummaryDto.cs`
- `Data/DTO/TokenUsageDto.cs`
- `Data/DAO/AiPerformanceDAO.cs`
- `Data/DAO/SellPlanDAO.cs`
- `Data/DAO/StrategyDAO.cs`
- `Data/DAO/TokenUsageDAO.cs`
- `Data/DAO/TradeHistoryDAO.cs`
- `Data/DBManager.cs`

</details>
