# AutoInvesting 프로젝트 온보딩 가이드 🚀

이 문서는 개발자가 AutoInvesting 프로젝트의 전체 흐름과 각 구성요소를 쉽게 이해하고 즉시 기여할 수 있도록 돕기 위해 작성되었습니다.

## 1. 프로젝트 아키텍처 개요 (Overview)

본 프로젝트는 ASP.NET Core 기반의 **Headless 백그라운드 서비스 및 REST API 서버**입니다.
크게 다음과 같은 흐름으로 동작합니다.

```
[외부 요청 (Web UI, API Client)]
      │ (x-api-key 인증 통과 시)
      ▼
[ Controllers ] (REST API Endpoint)
      │
      ├── (주문/분석 요청) ──▶ [ Core/SessionManager ] ──▶ [ 브로커 클라이언트 분기 (Sim/KIS) ]
      │                            └── [ Core/SmartOrderEngine ] ──▶ 퀀트 엔진(QuantFilter) 단독 판정 + 환율 어드바이저(설명·경고)
      │
      └── (데이터 단순 조회) ─▶ [ Data/DAO ] ──▶ PostgreSQL DB
```

> **현재 동작(퀀트 단독)**: 매수/매도/보류는 퀀트 신호만으로 결정합니다. Phase 4~6에서 만든 AI 결정 경로
> (차트AI+펀더멘털AI 합의, 적응형 임계값, 확률 스코어링)는 **코드에 주석으로 비활성화(보존)** 되어 매매에 쓰이지 않습니다(휴면).
> 분석/실행 중 Gemini 등 AI 호출은 일어나지 않습니다. 아래 4번 섹션의 합의 스코어링 설명은 **휴면 코드의 과거 동작 기록**입니다.

- **일일 매매 사이클 (DailyExecutionService)**: 앱 내부에 상시 동작하는 백그라운드 루프(`IHostedService`)는 없습니다. 외부 스케줄러(크론)가 `POST /api/order/daily-run`을 호출하면 `OrderController`가 Scoped 수명으로 `DailyExecutionService`를 구동하고, 이 서비스가 `SmartOrderEngine`을 호출하여 전 종목 자동분석 및 매매를 수행합니다. (과거 `TradingBackgroundService`(IHostedService, 1분 간격) 구조를 대체)

## 2. 생명주기와 의존성 주입 (Dependency Injection)

프로젝트 핵심 인스턴스들은 `Program.cs`에서 **싱글턴(Singleton)**으로 선언되어 시스템 전역에서 생명기가 하나로 관리됩니다.

- `SessionManager`: 앱 내에서 브로커 세션(토큰 등)을 관리합니다. (AI 엔진 분기 코드는 보존되어 있으나 현재 매매 결정에 사용되지 않음) Controllers는 DI를 통해 주입받아 사용합니다.
- `DBManager`: PostgreSQL 커넥션(Npgsql) 관리를 책임집니다.
- **예시 흐름 (`OrderController` 수동 실행 시)**:
  `OrderController`가 `execute` 엔드포인트 수신 → DI로 주입받은 `SessionManager`에서 API/브로커 상태 수령 → `SmartOrderEngine`에 브로커/AI엔진을 넣고 로직 실행.

## 3. 핵심 마법: 브로커 환경 분기 전략 (`IS_PAPER_TRADING`)

AutoInvesting 엔진은 자신이 **가짜 돈(모의)을 쓰는지 진짜 돈(실전)을 쓰는지 모릅니다.** 브로커 추상화 인터페이스(`IBrokerClient`)를 사용하기 때문입니다.

- `SessionManager`는 `IS_PAPER_TRADING` 환경변수(또는 `appsettings.json`의 값)가 `1`이거나, 설정된 API Key가 없으면 기본적으로 **`SimBrokerClient` (가상 모의투자 환경)**를 주입합니다.
- 반대로 키가 정상적으로 존재하면 **`KisBrokerClient` (한국투자증권 실거래망/모의망 연결)**를 주입합니다.
  - KIS 연동에서도 `Kis:Server` 값을 통해 KIS 실전망(prod)과 KIS 모의투자망(vps)으로 한 번 더 분기할 수 있습니다.

## 4. 판단 로직: 퀀트 단독 + 환율 컨텍스트 (현재)

매매 결정 로직은 철저하게 데이터와 알고리즘 기반으로 이루어집니다. `SmartOrderEngine` 내 로직의 흐름을 살펴봅시다.

1. **퀀트 지표 계산 (`QuantIndicator`)**: OHLCV(일봉) 데이터를 이용해 `RSI`, `MACD`, `Bollinger Bands`, 그리고 가격 위치(`Position`)를 도출합니다.
2. **퀀트 필터 판정 (`QuantFilter`)**: 설정된 전략(`MEAN_REVERSION`, `MOMENTUM`, `MIXED`)에 따라 AND 조합으로 검사하여 **매수/매도/보류를 최종 결정**합니다.
   - 예: "고점 대비 하위 30% 이내이면서, RSI가 45 이하인가?" → 모든 조건 충족 시 매수.
3. **환율(FX) 컨텍스트 (`FxRateAdvisor`)**: 매매 방향이 정해지면 USD/KRW 환율의 최근 분포상 위치를 보고 유불리를 **설명·경고**합니다. **매매를 막지는 않습니다(veto 없음).**
   - 매수: 환율 低 → 유리(INFO) / 환율 高 → 환차손 경고(WARNING) + 환헤지 대안 제시
   - 매도: 환율 高 → 원화 환산 유리(INFO) / 환율 低 → 불리 경고(WARNING)
   - 이 코멘트는 `GET /api/order/analyze/{ticker}` 응답의 `advisoryNotes`와 일일 운용 리포트 이메일에 표시됩니다.

> ℹ️ **(휴면) 과거 AI 합의 스코어링** — 아래는 Phase 4-e~6에서 동작하던 방식으로, **현재는 주석으로 비활성화(보존)** 되어
> 매매에 사용되지 않습니다. 향후 재활성화를 위해 기록만 남깁니다.
>
> - AI 이중 에이전트 평가(`GeminiMarketAnalyzer`): `Task.WhenAll`로 차트/펀더멘털 에이전트를 병렬 호출, `{ "signal": "BUY", "confidence": 0.76 }` JSON 응답
> - 확률 기반 합산(`CalculateConsensusScore`):
>   ```
>   BuyProbability = 퀀트기여(40%) + 차트AI확신도×30% + 펀더멘털AI확신도×30%
>   BuyProbability ≥ BUY_THRESHOLD(기본 0.65) → 매수 실행
>   ```
> - 판단 근거는 `ConsensusScoreDto`에 보관되었고, `TB_MARKET_SNAPSHOT`의 AI 컬럼은 스키마는 유지되나 **현재 기록되지 않습니다(0/빈값)**.

## 5. 보안 정책: 내 로컬 API 자격 증명 다루기

최근 깃허브(GitHub) 등 코드 저장소에 API 키가 유출되는 사고를 방지하기 위해 이중 보안 구조를 적용해 두었습니다.

### 로컬 환경에서 시크릿 관리하기
1. 프로젝트 루트에 `appsettings.local.json` 파일을 만듭니다. (이 파일은 `.gitignore`에 등록되어 있어 **절대 커밋되지 않습니다.**)
2. 발급받은 비밀 정보(Gemini API 키, KIS 토큰, 계좌번호 등)를 이곳에 입력합니다.

```json
{
  "Kis": { "AppKey": "나의_진짜_앱키", "AppSecret": "나의_진짜_앱시크릿", "AccountNo": "12345678" },
  "Ai": { "Provider": "gemini", "GeminiApiKey": "AI_API_키" },
  "Security": { "ApiAccessKey": "아무도모르는_나만의_서버_암호" }
}
```

### 서버 보호: API Key Authentication
개발된 백엔드 기능을 타사 프론트엔드 등에서 호출하려면 반드시 위에서 설정한 `Security:ApiAccessKey` 값을 HTTP 헤더 **`x-api-key`** 단에 담아 요청해야 합니다. 그렇지 않으면 `401 Unauthorized` 오류가 발생하여 비인가 접근을 원천 차단합니다.
