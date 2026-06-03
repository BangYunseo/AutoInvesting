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
      │                            └── [ Core/SmartOrderEngine ] ──▶ 퀀트 엔진 + AI 엔진 구동
      │
      └── (데이터 단순 조회) ─▶ [ Data/DAO ] ──▶ SQLite DB
```

- **백그라운드 루프 (TradingBackgroundService)**: `Controllers`의 개입 없이 `IHostedService`를 통해 1분 간격으로 동작하며, 설정된 시간(예: 오후 10시 30분)이 되면 스스로 `SmartOrderEngine`을 호출하여 전 종목 자동분석 및 매매를 수행합니다.

## 2. 생명주기와 의존성 주입 (Dependency Injection)

프로젝트 핵심 인스턴스들은 `Program.cs`에서 **싱글턴(Singleton)**으로 선언되어 시스템 전역에서 생명기가 하나로 관리됩니다.

- `SessionManager`: 앱 내에서 브로커 세션(토큰 등)과 AI 엔진의 상태를 관리합니다. Controllers는 DI를 통해 주입받아 사용합니다.
- `DBManager`: SQLite 커넥션 관리를 책임집니다.
- **예시 흐름 (`OrderController` 수동 실행 시)**:
  `OrderController`가 `execute` 엔드포인트 수신 → DI로 주입받은 `SessionManager`에서 API/브로커 상태 수령 → `SmartOrderEngine`에 브로커/AI엔진을 넣고 로직 실행.

## 3. 핵심 마법: 브로커 환경 분기 전략 (`IS_PAPER_TRADING`)

AutoInvesting 엔진은 자신이 **가짜 돈(모의)을 쓰는지 진짜 돈(실전)을 쓰는지 모릅니다.** 브로커 추상화 인터페이스(`IBrokerClient`)를 사용하기 때문입니다.

- `SessionManager`는 `IS_PAPER_TRADING` 환경변수(또는 `appsettings.json`의 값)가 `1`이거나, 설정된 API Key가 없으면 기본적으로 **`SimBrokerClient` (가상 모의투자 환경)**를 주입합니다.
- 반대로 키가 정상적으로 존재하면 **`KisBrokerClient` (한국투자증권 실거래망/모의망 연결)**를 주입합니다.
  - KIS 연동에서도 `Kis:Server` 값을 통해 KIS 실전망(prod)과 KIS 모의투자망(vps)으로 한 번 더 분기할 수 있습니다.

## 4. 판단 로직: 퀀트 + AI 합산 (CombineSignals)

매매 결정 로직은 철저하게 데이터와 알고리즘 기반으로 이루어집니다. `SmartOrderEngine` 내 로직의 흐름을 살펴봅시다.

1. **퀀트 지표 계산 (`QuantIndicator`)**: OHLCV(일봉) 데이터를 이용해 `RSI`, `MACD`, `Bollinger Bands`, 그리고 가격의 위치치(`Position`)를 도출합니다.
2. **퀀트 필터 통과 (`QuantFilter`)**: 설정된 전략(`MEAN_REVERSION`, `MOMENTUM`, `MIXED`)에 따라 위 수치들을 AND 조합으로 검사합니다.
   - 예: "고점 대비 하위 30% 이내이면서, RSI가 45 이하인가?" -> 둘 다 맞으면 BUY 유력.
3. **AI 엔진 평가 (`AiMarketAnalyzer`)**: 
   - `Ai:Provider`가 `gemini`로 설정되어 있으면, `PromptBuilder`를 통해 지표 수치와 차트가 자연어("지금 차트와 보조지표가 이런데 판단해줘")로 변환되어 Google Gemini API로 전송됩니다.
   - Gemini는 `{ "signal": "BUY", "confidence": 0.8 }` 같은 JSON 포맷으로 대답합니다.
   - 폴백(Fallback) 방어: AI API 호출에 실패하거나, `Provider`가 `mock`이면 코드 내부의 Mock 로직(단순 분기)이 호출됩니다.
4. **결론 합산 (`CombineSignals`)**:
   - AI 확신도가 낮거나(예: 0.6 미만), 퀀트/AI가 반대 의견을 가지면 방어적으로 **HOLD** 처리합니다. 완전히 같은 방향일 때만 매수/매도를 실행합니다.

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
