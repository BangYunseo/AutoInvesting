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
API (Controllers/) & Background (BackgroundServices/)
  ↓ (단방향)
Core (Core/, Core/Quant/)
  ↓ (단방향)
Data (Data/, Data/DTO/, Data/DAO/)
  ← Utils (Utils/) — 모든 레이어에서 접근 가능
```

### 의존성 규칙
- **API/Background → Core**: 허용 (컨트롤러나 스케줄러에서 Core 엔진 호출)
- **Core → Data**: 허용 (엔진에서 DAO/DTO 사용)
- **Core → API**: 금지 (Core는 컨트롤러나 백그라운드 서비스를 알지 못함)
- **Data → Core**: 금지 (Data는 Core를 알지 못함)
- **Utils**: 모든 레이어에서 접근 가능한 유틸리티

## 핵심 추상화
- `IBrokerClient` — 증권사 API 추상화 인터페이스
  - 구현체: `SimBrokerClient` (시뮬레이션), `KisBrokerClient` (KIS 실거래, Polly 내결함성 적용)
  - 새 증권사 추가 시 반드시 이 인터페이스를 구현
- `SessionManager` — 브로커 인스턴스 생명주기 관리
  - `IS_PAPER_TRADING` 설정값에 따라 SimBroker 또는 KisBroker 분기
  - `AI_PROVIDER` 설정에 따라 `AiMarketAnalyzer`(Mock) 또는 `GeminiMarketAnalyzer` 분기
- `IMarketAnalyzer` — AI 시장 분석 인터페이스
  - 구현체: `AiMarketAnalyzer` (Mock), `GeminiMarketAnalyzer` (Gemini API, 차트+펀더멘털 이중 에이전트)
- `NotificationService` — 중요 알림(체결 내역, 예외) 외부 발송 (MailKit, Naver SMTP)
- `DailyExecutionService` — 매매 스케줄 실행 진입점 (Scoped, `IServiceScopeFactory` 패턴 필요)
- `AdaptiveThresholdEngine` — 종목별 과거 성과 기반 적응형 임계값 계산 (Phase 5-a)

## 아키텍처 흐름
```
ASP.NET Core Host (Program.cs)
      ├── [REST API 호출] → Controllers (수동 제어, 상태 조회)
      └── [Scoped 실행] → DailyExecutionService (스케줄 실행 진입점)
                                       ↓
                                  SmartOrderEngine
                                       ├── 현재가/가격범위/OHLCV 조회 (IBrokerClient)
                                       ├── QuantIndicator (RSI, MACD, BB 계산)
                                       ├── QuantFilter (전략 유형별 AND 조건)
                                       ├── IMarketAnalyzer (차트AI + 펀더멘털AI 병렬 호출)
                                       ├── CalculateConsensusScore (가중치 확률 합산 → BuyProbability)
                                       ├── 주문 실행 → TradeHistoryDAO (거래 기록 저장)
                                       └── 메일 발송 → NotificationService (성공/오류 알림)
```

## AI 합의 시스템 (Phase 4-e~)
- `CalculateConsensusScore()`: 퀀트(40%) + 차트AI(30%) + 펀더멘털AI(30%) 가중치 × 확신도 합산
- 임계값(`BUY_THRESHOLD`, `SELL_THRESHOLD`) 초과 시에만 매매 실행 (기본값 0.65)
- 가중치/임계값은 `appsettings.json > Consensus` 섹션에서 설정
- `ConsensusScoreDto` — 확률 분해 결과 보관 (BuyProbability, 에이전트별 기여도)

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
