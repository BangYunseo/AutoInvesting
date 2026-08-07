# 아키텍처 규칙

## 프로젝트 개요
- 해외 ETF 자동 적립(DCA) 투자 시스템 (ASP.NET Core Web API, .NET 8.0, C#)
- **타이밍/퀀트/AI 판단 없는 기계적 적립** — 여러 매수 템플릿을 정의하고 월별로 배정해, 현재 월에 해당하는 템플릿의 종목별 고정 수량을 매 사이클 그대로 매수 (Phase 6 전환)
- 외부 크론잡이 적립 사이클을 호출하는 Headless 서비스
- 증권사: 한국투자증권 (KIS) REST API

## 레이어 구조 및 의존성 방향
```
[전역 인증 필터: ApiKeyAuthAttribute] — Bearer 세션토큰(사람) 또는 x-api-key(크론), [PublicEndpoint]만 면제
  ↓
API (Controllers/)  ← 외부 크론잡이 dca-run 호출 / 사용자 제어·조회
  ↓ (단방향)
Core (Core/)
  ↓ (단방향)
Data (Data/, Data/DTO/, Data/DAO/)
  ← Utils (Utils/) — 모든 레이어에서 접근 가능
```

### 의존성 규칙
- **API → Core**: 허용 (컨트롤러에서 Core 엔진 호출)
- **Core → Data**: 허용 (엔진에서 DAO/DTO 사용)
- **Core → API**: 금지 (Core는 컨트롤러를 알지 못함)
- **Data → Core**: 금지 (Data는 Core를 알지 못함)
- **Utils**: 모든 레이어에서 접근 가능한 유틸리티

## 핵심 추상화
- `IBrokerClient` — 증권사 API 추상화 인터페이스
  - 구현체: `SimBrokerClient` (시뮬레이션), `KisBrokerClient` (KIS 실거래, Polly 내결함성 적용)
  - 새 증권사 추가 시 반드시 이 인터페이스를 구현
- `SessionManager` — 브로커 인스턴스 생명주기 관리
  - `IS_PAPER_TRADING` 설정값에 따라 SimBroker 또는 KisBroker 분기
- `DcaAccumulationEngine` — 적립식 매수 엔진. `PlanPurchases`(순수함수, 외부 I/O 없음 — 현재 월 템플릿의 종목별 고정 수량 매수 계획 + 총 매수금액 산출)와 `AccumulateAsync`(현재가·환율 조회 → 계획 → 주문 → 기록) 분리
- `DcaSettings` — 매수 템플릿 목록·월별 배정·예산의 단일 읽기/쓰기 지점 (DB `TB_APP_CONFIG`: `DCA_TEMPLATES` JSON / `DCA_MONTH_MAP` 우선 → 레거시 `DCA_QTYS`/`DCA_BUDGET_KRW`/`appsettings.json > Dca` 폴백, 자동 이관)
- `DailyExecutionService` — 적립 사이클 실행 진입점 (`RunDcaCycleAsync`, Scoped, `IServiceScopeFactory` 패턴)
  - 월 1회 멱등 가드(`DCA_LAST_RUN_MONTH`)와 추가 적립 예약(`DCA_FORCE_RUN_MONTH`)은 `AppConfigManager.TryReadDb`로 **DB에서만** 읽는다. `Get`은 조회 실패와 값 없음을 같은 기본값으로 뭉개고 환경변수를 DB보다 먼저 집으므로, DB 조회 한 번의 실패나 동명 환경변수 하나로 가드가 뚫려 같은 달에 중복 매수가 난다. **조회 실패 시 매수하지 않는다(fail-closed).**
- 🚫 **인앱 스케줄러(`BackgroundService`)를 도입하지 말 것.** Render 무료 인스턴스는 유휴 시 프로세스가 멈춰 타이머도 멈추고, 아무 오류도 남지 않는다 — "켜져 있는데 안 도는" 기능이 된다. 그래서 트리거는 외부 크론이며, 워크플로가 `/api/health`로 먼저 깨운다. 잠들지 않는(유료) 인스턴스로 옮기는 경우에만 재검토하고, 그때도 외부 크론과 발화 시각을 최소 2시간 띄운다 — `RunDcaCycleAsync`에 락이 없고 `dca-run`은 202를 먼저 반환하므로 겹치면 양쪽이 마커를 빈 값으로 읽어 **둘 다 매수한다**(2026-08-07 검토·보류)
- `NotificationService` — 중요 알림(체결 내역, 예외) 외부 발송 (Resend HTTP API, 443 포트 — Render SMTP 차단 우회)
- `ApiKeyAuthAttribute` — 전역 인증 필터. 모든 컨트롤러 **액션**에 적용되며 Bearer 세션 토큰(사람) 또는 `x-api-key`(크론) 중 하나로 통과. `[PublicEndpoint]` 표시 액션만 면제이며, 면제 대상은 **`/api/auth/status`와 `/api/auth/login` 둘뿐**이다
  - 🚫 **`[PublicEndpoint]`를 컨트롤러 클래스에 붙이지 말 것.** 클래스에 붙이면 그 안의 모든 액션이 한꺼번에 열린다. 과거 `AuthController`가 그 상태여서 `setup`까지 미인증 공개였고, 관리자 자리가 비어 보이는 순간 누구나 관리자를 선점해 실주문을 낼 수 있었다(2026-08-04 수정). `Tests/PublicEndpointExposureTests.cs`가 면제 목록을 리플렉션으로 고정한다
  - 이 필터는 **MVC 액션에만** 걸린다. 미들웨어 경로(`/swagger`, 정적 파일)와 `MapHealthChecks`, 그리고 앞으로 추가할 Minimal API(`app.MapGet` 등)에는 적용되지 않으므로 별도 보호가 필요하다
- `LoginThrottle` — 로그인 실패 **속도 상한**(전역 카운터, 분당 20회). 호출자별 IP/헤더 카운터는 쓰지 않는다 — 프록시 뒤라 신뢰할 수 있는 발신지가 없고 `X-Forwarded-For`는 클라이언트가 정하는 값이라 우회·표적 잠금·메모리 증가가 모두 성립한다. 상한 검사는 반드시 비밀번호 검증(PBKDF2) **앞**에 둔다
- 관리자 해시가 "비었는지" 판정하는 곳(`status`/`setup`/`login`)은 `AppConfigManager.TryReadDb`로 **조회 실패와 값 없음을 구분**하고, 조회 실패면 `503`으로 거부한다(fail-closed). `AppConfigManager.Get`은 둘을 기본값으로 뭉개므로 보안 판정에 쓰지 않는다
- `TaxEstimator` — **정보·확인 전용 보조 기능**. 매도 양도세 추정(수동 매도 확인용). ⚠️ `DcaAccumulationEngine`/`DailyExecutionService`의 매수 의사결정에 값을 흘려보내지 않는다(판단 레이어 재도입 아님)

## 아키텍처 흐름
```
ASP.NET Core Host (Program.cs)
      ├── [REST API 호출] → Controllers (수동 주문, 적립 설정, 상태 조회)
      └── [POST /api/order/dca-run] → DailyExecutionService.RunDcaCycleAsync (외부 크론잡 호출)
                                       ↓
                                  DcaAccumulationEngine.AccumulateAsync
                                       ├── 환율/현재가 조회 (IBrokerClient)
                                       ├── PlanPurchases (종목별 고정 수량 매수 계획 — 순수함수)
                                       ├── 매수 주문 실행 → TradeHistoryDAO (거래 기록 저장)
                                       └── 메일 발송 → NotificationService (적립 보고서)
```

## 적립(DCA) 배분 원칙 (Phase 6)
- `DcaSettings.Load()`로 템플릿 목록·월별 배정을 읽어(`DcaController`에서 편집), 현재 월(KST=UTC+9)에 배정된 템플릿을 선택
  - 월 배정이 비어 있으면 첫 번째 템플릿을 사용 (기존 동작 유지)
  - 배정된 달에 해당 템플릿이 없으면 매수 스킵
- `PlanPurchases`가 선택된 템플릿의 종목별 고정 수량을 **그대로** 매수 계획에 담고 총 매수금액을 산출
- 비중(%)·매수금액은 사람이 정하지 않는다 — 수량×현재가로 환산해 화면에서 보여주는 표시용 값
- 템플릿별 예산은 **초과 경고용 상한**일 뿐 수량을 줄이지 않는다(초과 시 경고 로그·메일만)
- **타이밍 판단·신호·임계값·합의 스코어링 없음** — 백테스트로 가치 없음이 확인되어 제거됨

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
| `Logger.Info()` | 일반 정보 — `[DCA] 적립식 매수 시작` |
| `Logger.Warn()` | 경고 (비정상이지만 계속 진행, API 재시도 발생 등) |
| `Logger.Error()` | 에러 (처리 실패, 이메일 알림 연동 대상) |
| `Logger.Fatal()` | 치명적 오류 — `Program.cs` 미들웨어 또는 Host 종료 시 |

- 로그 메시지 형식: `[모듈명] 메시지` (예: `[KisBrokerClient] 429 응답, 2초 후 재시도`)
- 빈 catch 블록 절대 금지 — 반드시 `Logger.Error()` 포함
