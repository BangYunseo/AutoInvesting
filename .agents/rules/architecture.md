---
trigger: always_on
---

# 아키텍처 규칙

## 프로젝트 개요
- 해외 ETF 자동 투자 시스템 (ASP.NET Core Web API, .NET 8.0, C#)
- 퀀트 지표 기반 감정 배제 매매
- 24시간 동작하는 Headless 백그라운드 서비스
- 증권사: 한국투자증권 (KIS) REST API

## 레이어 구조 및 의존성 방향
```
API (Controllers/) & 일일 실행 진입점 (DailyExecutionService)
  ↓ (단방향)
Core (Core/, Core/Quant/, Core/Advisors/)
  ↓ (단방향)
Data (Data/, Data/DTO/, Data/DAO/)
  ← Utils (Utils/) — 모든 레이어에서 접근 가능
```

> 참고: 앱 내부에 상시 백그라운드 루프(IHostedService)는 없다. 24시간 자동 매매는 외부 스케줄러(크론)가
> `POST /api/order/daily-run`을 호출 → `OrderController`가 Scoped `DailyExecutionService`를 구동하는 방식이다.

### 의존성 규칙
- **API/실행서비스 → Core**: 허용 (컨트롤러·DailyExecutionService에서 Core 엔진 호출)
- **Core → Data**: 허용 (엔진에서 DAO/DTO 사용)
- **Core → API**: 금지 (Core는 컨트롤러나 실행 서비스를 알지 못함)
- **Data → Core**: 금지 (Data는 Core를 알지 못함)
- **Utils**: 모든 레이어에서 접근 가능한 유틸리티

## 핵심 추상화
- `IBrokerClient` — 증권사 API 추상화 인터페이스
  - 구현체: `SimBrokerClient` (시뮬레이션), `KisBrokerClient` (KIS 실거래, Polly 내결함성 적용)
  - 새 증권사 추가 시 반드시 이 인터페이스를 구현
- `SessionManager` — 브로커 인스턴스 생명주기 관리
  - `IS_PAPER_TRADING` 설정값에 따라 SimBroker 또는 KisBroker 분기
  - (휴면) `AI_PROVIDER`에 따른 `AiMarketAnalyzer`/`GeminiMarketAnalyzer` 분기 코드는 보존되어 있으나 현재 매매 결정 경로에서 사용하지 않음
- `QuantFilter` — **현재 매매 결정의 단일 근거**. 전략 유형별 AND 조건(RSI·MACD·볼린저·Position)으로 매수/매도/보류 판정
- `FxRateAdvisor`(`IContextAdvisor`) — 환율(USD/KRW) 분포상 위치를 보고 매매 유불리를 설명. **매매를 막지 않는 설명·경고 전용**(veto 없음), 단일 종목 분석 응답과 일일 운용 리포트에 첨부
- `NotificationService` — 중요 알림(체결 내역, 예외) 외부 발송 (MailKit, Naver SMTP)
- `DailyExecutionService` — 매매 스케줄 실행 진입점 (Scoped, `IServiceScopeFactory` 패턴 필요)
- (휴면) `IMarketAnalyzer`/`AiMarketAnalyzer`/`GeminiMarketAnalyzer` — AI 시장 분석 추상화·구현. 코드는 보존되나 결정 경로 미사용
- (휴면) `AdaptiveThresholdEngine` — 종목별 적응형 매수·매도 임계값 (Phase 5). 결정 경로 미사용
- (휴면) `PerformanceFeedbackEngine` — TB_MARKET_SNAPSHOT 기반 실측 적중률·가중치 A/B 산출 (Phase 5-d, 읽기 전용 분석)

## 아키텍처 흐름
```
ASP.NET Core Host (Program.cs)
      ├── [REST API 호출] → Controllers (수동 제어, 상태 조회)
      └── [외부 크론 → POST /api/order/daily-run → Scoped 실행] → DailyExecutionService (일일 매매 사이클 진입점)
                                       ↓
                                  SmartOrderEngine
                                       ├── 현재가/가격범위/OHLCV 조회 (IBrokerClient)
                                       ├── QuantIndicator (RSI, MACD, BB 계산)
                                       ├── QuantFilter (전략 유형별 AND 조건) → 매수/매도/보류 결정
                                       ├── FxRateAdvisor (환율 유불리 설명·경고 — veto 없음, 결과에 첨부)
                                       ├── 주문 실행 → TradeHistoryDAO (거래 기록 저장)
                                       └── 메일 발송 → NotificationService (성공/오류 알림 + 환율 코멘트)
   (휴면) IMarketAnalyzer(차트AI+펀더멘털AI) · CalculateConsensusScore(합의 확률) 경로는 주석으로 비활성화·보존
```

## 매매 결정: 퀀트 단독 (현재)
- 매수/매도/보류는 `QuantFilter`의 전략 유형별 AND 조건만으로 결정합니다 (RSI·MACD·볼린저·Position).
- 분석/실행 중 Gemini 등 **AI 호출은 일어나지 않습니다**.
- 환율(FX)은 `FxRateAdvisor`가 매매 방향에 맞춰 유불리를 설명/경고만 합니다 — **매매를 막지 않습니다(veto 없음)**.
  - 매수: 환율 低 → 유리(INFO) / 환율 高 → 환차손 경고(WARNING) + 환헤지 대안 제시
  - 매도: 환율 高 → 원화 환산 유리(INFO) / 환율 低 → 불리 경고(WARNING)
  - 표시 위치: `GET /api/order/analyze/{ticker}` 응답의 `advisoryNotes`, 일일 운용 리포트 이메일

## (휴면) AI 합의 시스템 — Phase 4-e~6 개발 이력
> 아래 합의 스코어링은 **현재 매매에 사용되지 않으며, 코드에서 주석으로 비활성화(보존)** 되어 있습니다.
> 향후 재활성화를 위해 설명만 남깁니다.
- `CalculateConsensusScore()`: 퀀트(40%) + 차트AI(30%) + 펀더멘털AI(30%) 가중치 × 확신도 합산
- 임계값(`BUY_THRESHOLD`, `SELL_THRESHOLD`) 초과 시에만 매매 실행 (기본값 0.65)
- 가중치/임계값은 `appsettings.json > Consensus` 섹션에서 설정
- `ConsensusScoreDto` — 확률 분해 결과 보관 (BuyProbability, 에이전트별 기여도)
- `TB_MARKET_SNAPSHOT`의 AI 컬럼(BuyProbability, ChartAiScore 등)은 **유지하되 더 이상 기록하지 않음(0/빈값)**

## 새 기능 추가 순서
1. DTO → DAO → Core 로직 → API Controller 또는 BackgroundService 순서로 구현
2. 인터페이스-구현체 분리 원칙 유지
3. 비즈니스 로직은 반드시 `Core/` 하위에 배치
4. 외부 연동부(HTTP API, SMTP)는 `Utils` 또는 `Core` 통신 계층에 배치

## 비동기 패턴
- 외부 API/DB I/O 호출은 반드시 `async/await` 사용
- ASP.NET Core 환경이므로 동기 블로킹 호출(`Task.Wait()`, `.Result`)을 절대 사용하지 않음
- `ConfigureAwait(false)`는 ASP.NET Core에서 기본 컨텍스트가 없으므로 굳이 강제하지 않으나, 재사용 라이브러리 작성 시에는 권장됨

## 로깅 규칙

| 메서드 | 용도 |
|--------|------|
| `Logger.Info()` | 일반 정보 — `[SmartOrder] 분석 시작` |
| `Logger.Warn()` | 경고 (비정상이지만 계속 진행, API 재시도 발생 등) |
| `Logger.Error()` | 에러 (처리 실패, 이메일 알림 연동 대상) |
| `Logger.Fatal()` | 치명적 오류 — `Program.cs` 미들웨어 또는 Host 종료 시 |
| `Logger.LogQuant()` | 퀀트 판단 근거 기록 (백테스트/분석용) |

- 로그 메시지 형식: `[모듈명] 메시지` (예: `[KisBrokerClient] 429 응답, 2초 후 재시도`)
- 빈 catch 블록 절대 금지 — 반드시 `Logger.Error()` 포함
