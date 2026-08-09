---
title: AutoInvesting 설정 레퍼런스
date: 2026-08-03
company: [개인]
tags: [설정, 환경변수, 레퍼런스, 보안, 실전전환]
status: draft
---

# AutoInvesting 설정 레퍼런스

## 개요
> 이 프로젝트의 모든 설정 키가 **어디서 읽히고 어느 층에 있어야 하는지**를 한곳에 모은 상시 참조 문서다. 설정은 4층(환경변수 → DB → appsettings.json → 코드 기본값)에 흩어져 있고 관문을 우회하는 경로가 3개 있어, 이 표 없이는 "지금 이 값이 어디서 왔는지" 추적이 어렵다. **값은 한 줄도 적지 않는다** — 키 이름과 위치까지만이다.

## 배경 / 목적

설정 키 목록이 코드 다섯 곳(`AppConfigManager.ResolveFromConfiguration` 매핑, `SensitiveKeys`, `ConfigController` 응답 딕셔너리, `README` 표, `Settings.jsx` secretKeys)에 흩어져 단일 진실 원천이 없었다. 그 결과 죽은 키가 남고, 문서가 무효한 전환 절차를 지시하고, 환경변수 우선순위 때문에 UI 저장이 조용히 무효화되는 문제가 생겼다.

그중 두 곳(`ConfigController`, `Settings.jsx`)은 2026-08-06에 **코드째 제거**됐다. 운영 설정이 전부 Render 환경변수로 주입돼 있어 화면 저장이 애초에 읽히지 않는 무동작 UI였기 때문이다. 이제 설정 변경 수단은 **Render 환경변수 수정 + 재배포** 하나뿐이며(적립 설정 화면이 다루는 DB 전용 키는 예외), 이 문서가 그 유일한 목록 문서다.

실계좌 전환 후에는 설정 하나가 잘못 놓이면 매수가 모의망으로 빠지거나 크론이 차단된다. 이 문서는 그 판단의 기준선이다.

## 본문

### 조회 우선순위

설정 관문은 `Data/AppConfigManager.cs`의 `Get()` 하나다. 위층에서 값을 찾으면 아래는 보지 않는다.

```text
환경변수 (키 이름 그대로)
  → DB TB_APP_CONFIG.CONFIG_VALUE  (enc:v1: 접두사면 AES-GCM 복호화)
    → appsettings.json (ResolveFromConfiguration 매핑에 등록된 키만)
      → 호출부가 넘긴 defaultValue
```

`ResolveFromConfiguration`에 매핑이 없는 키는 **appsettings 층을 건너뛰고** 곧바로 `defaultValue`로 떨어진다.

`IS_PAPER_TRADING`만 appsettings 경로에서 `bool` → `"1"`/`"0"` 변환을 받는다. **환경변수 경로에는 이 변환이 없다** — 원시 문자열이 그대로 반환된다.

### 관문을 우회하는 경로 3개

| 경로 | 위치 | 읽는 층 |
|---|---|---|
| DB 접속 | `Data/DBManager.cs` | 환경변수 → 하드코딩 로컬 폴백 |
| 메일 | `Utils/NotificationService.cs` | 환경변수 → appsettings (**DB 불가**) |
| 암호화 키 | `Utils/CryptoUtil.cs` | IConfiguration → 환경변수 (**DB 불가**) |

`Program.cs`가 `appsettings.local.json`을 optional로 추가 로드하므로 `MASTER_KEY`·`AUTH_TOKEN_SECRET`은 로컬 파일에 넣어도 인식된다.

### 부트스트랩 키

아래 3개는 **환경변수여야 한다.** DB에 둘 수 없는 구조적 이유가 있다 — DB에 접속하려면 접속 문자열이 필요하고, DB의 암호화 시크릿을 풀려면 복호화 키가 필요하다.

| 키 | 읽는 곳 | 없으면 |
|---|---|---|
| `DATABASE_URL` | `Data/DBManager.cs` | 로컬 기본 접속 문자열로 폴백 (배포 DB는 **Neon**, `*.neon.tech`) |
| `MASTER_KEY` | `Utils/CryptoUtil.cs` | 시크릿을 평문 저장하고 경고만 남김. 기동은 계속 |
| `AUTH_TOKEN_SECRET` | `Utils/CryptoUtil.cs` | `MASTER_KEY` 파생으로 대체. 둘 다 없으면 세션 토큰 서명 불가 → 로그인 불가 |

`MASTER_KEY`는 **재암호화(rekey) 경로가 코드에 없다.** 분실과 교체가 같은 비용이며, 분실하면 DB의 `enc:v1:` 값은 복구 수단이 없다. 오프라인 백업이 필요하다.

### 운영 시크릿

전부 환경변수에 둔다. `security.md`가 환경변수를 1순위로 명시하고, DB에 두면 DB 덤프·백업이 곧 시크릿 유출이 된다.

| 키 | 읽는 곳 | 없으면 | DB 저장 시 암호화 |
|---|---|---|---|
| `KIS_APP_KEY` | `Core/SessionManager.cs` | **`SimBrokerClient`로 강제 전환** (실거래 불가) | O |
| `KIS_APP_SECRET` | `Core/SessionManager.cs` | 토큰 발급 실패 | O |
| `KIS_ACCOUNT_NO` | `Core/SessionManager.cs` | 주문 API 거부 | O |
| `API_ACCESS_KEY` | `Utils/ApiKeyAuthAttribute.cs` | **x-api-key 경로 전면 401** (크론 차단) | O |
| `RESEND_API_KEY` | `Utils/NotificationService.cs` | 메일 발송 비활성 | O (그러나 읽히지 않음 — 아래 참조) |

### 운영 설정

| 키 | 읽는 곳 | 기본값 | 비고 |
|---|---|---|---|
| `IS_PAPER_TRADING` | `Core/SessionManager.cs` | `"1"` | **`"0"`만 실전.** `"false"`는 모의 |
| `KIS_ACCOUNT_PROD` | `Core/SessionManager.cs` | `"01"` | 값이 `01`이면 환경변수 불필요 |
| `ADMIN_EMAIL` | `Utils/NotificationService.cs` | appsettings 폴백 | 개인정보 — 소스에 두지 않음 |
| `ASPNETCORE_URLS` | `Dockerfile` | .NET 기본 | 로컬은 Vite 프록시 대상 포트에 맞춰 수동 설정 |
`IS_PAPER_TRADING`을 DB로 옮기면 **DB 조회 실패 시 appsettings 기본값(모의)으로 조용히 추락**한다. 실전 운영 중에는 환경변수가 안전한 쪽이다. 같은 이유로 `API_ACCESS_KEY`도 환경변수에 둔다 — DB 장애가 곧 크론 401이 되면 원인과 증상이 멀어져 진단이 어렵다.

### DB 전용 키

아래는 DB(`TB_APP_CONFIG`)에만 있어야 한다. **동명 환경변수를 만들면 DB 값이 영구히 가려진다** — `Get()`이 환경변수를 먼저 집으므로 적립 설정 화면 저장이나 앱의 자동 기록이 조용히 무효화된다.

| 키 | 읽고 쓰는 곳 | 관리 경로 |
|---|---|---|
| `DCA_TEMPLATES` | `Core/DcaSettings.cs` | 적립 설정 화면 (`PUT /api/dca/config`) |
| `DCA_MONTH_MAP` | `Core/DcaSettings.cs` | 적립 설정 화면 (`PUT /api/dca/config`) |
| `DCA_RUN_DAY` | `Core/DcaSettings.cs` | 적립 설정 화면 (`PUT /api/dca/config`) |
| `DCA_LAST_RUN_MONTH` | `Core/DailyExecutionService.cs` | **없음 — 앱이 자동 관리** |
| `DCA_FORCE_RUN_MONTH` | `Core/DailyExecutionService.cs` | **없음 — 앱이 자동 관리** (추가 적립 예약) |
| `DCA_PENDING_SNAPSHOT` | `Core/DailyExecutionService.cs` | **없음 — 앱이 자동 관리** (체결 대사 기준점) |
| `DCA_LAST_RUN_DATE` | `Core/DailyExecutionService.cs` | **없음 — 앱이 자동 관리** (표시 전용) |
| `ADMIN_USERNAME` | `Controllers/AuthController.cs` | `POST /api/auth/setup` (최초 1회, 화면 없음) |
| `ADMIN_PASSWORD_HASH` | `Controllers/AuthController.cs` | `POST /api/auth/setup` (최초 1회, 화면 없음) |

#### 동명 환경변수 금지 — 가드를 무력화하는 두 키

`DCA_LAST_RUN_MONTH`와 `DCA_FORCE_RUN_MONTH`는 **동명 환경변수를 만들면 월 1회 멱등 가드가 영구 무력화된다.** 둘 다 `Set`은 DB에 기록되고 성공 로그까지 남으므로 **증상만 보면 가드가 동작하는 것처럼 보인다.**

| 키 | 동명 환경변수가 있으면 |
|---|---|
| `DCA_LAST_RUN_MONTH` | 값이 당월이면 **적립이 영구 스킵**(메일도 안 옴 — 스킵 경로가 보고서 발송 위에서 return한다). 값이 당월이 아니면 크론이 **매일 매수** |
| `DCA_FORCE_RUN_MONTH` | 값이 당월이면 지정일 게이트와 월 마커를 **둘 다** 건너뛰어 크론이 **매일 매수** |

> **2026-08-07 수정 이후**: `RunDcaCycleAsync`의 가드는 이 두 키를 `AppConfigManager.TryReadDb`로 **DB에서만** 읽는다. 따라서 동명 환경변수는 더 이상 가드를 가리지 못하고, DB 조회가 실패하면 매수하지 않는다(fail-closed). 그 전에는 `Get`이 환경변수를 먼저 집었고, 조회 실패와 값 없음을 같은 기본값으로 뭉개 **Neon 콜드 스타트 한 번으로 같은 달에 중복 매수**가 가능했다.
>
> 플랫폼 이전·CI 변수 설정처럼 환경변수를 대량으로 만드는 작업 중에는 이 표의 키 이름을 그대로 쓰지 않는지 확인한다.

### appsettings 섹션 전용

`GetMap`으로 섹션째 읽으므로 환경변수·DB 오버라이드가 불가능하다. 변경에 재배포가 필요하다.

| 경로 | 읽는 곳 | 기본값 |
|---|---|---|
| `Dca:Quantities` | `Core/DcaSettings.cs` | 레거시 폴백 |
| `Dca:MonthlyBudgetKrw` | `Core/DcaSettings.cs` | 100만원 |
| `Tax:AnnualDeductionKrw` | `Core/TaxEstimator.cs` | 250만원 |
| `Tax:Rate` | `Core/TaxEstimator.cs` | 0.22 |
| `Tax:EstimatedSellFeeRate` | `Core/TaxEstimator.cs` | 0.0025 |
| `Resend:SenderEmail` | `Utils/NotificationService.cs` | Resend 기본 테스트 도메인 |
| `Resend:SenderName` | `Utils/NotificationService.cs` | `AutoInvesting System` |

`Resend:SenderEmail`을 자체 도메인으로 바꾸려면 Resend에 도메인 인증이 먼저 필요하다. 기본 테스트 도메인은 Resend 계정에 등록된 본인 메일로만 수신된다.

### 죽은 설정

| 키 | 상태 |
|---|---|
| `KIS_SERVER` / `Kis:Server` | **소비자 0건.** 유일한 독자였던 `ConfigController`가 2026-08-06에 제거되어 이제 `AppConfigManager`의 appsettings 매핑에만 이름이 남아 있다. 도메인 분기는 예전부터 `IS_PAPER_TRADING` 단독이므로 **`prod`로 바꿔도 실전으로 가지 않는다** |
| `DCA_QTYS` / `DCA_BUDGET_KRW` | 레거시 폴백. `DCA_TEMPLATES`가 있으면 도달 불가 |
| `AI_PROVIDER` / `GEMINI_API_KEY` | Phase 6에서 판단 레이어 제거 시 코드 참조 0건. 2026-08-03에 Render 환경변수·DB에서 삭제 |

### 실전 전환 절차

실제 스위치는 **하나**다.

```text
IS_PAPER_TRADING = 0
```

문자열 `0`이어야 한다. 판정이 `!= "0"`이므로 `false`·`False`·`no`·앞뒤 공백은 전부 모의로 떨어진다. 실패 방향이 안전한 쪽이라 사고는 나지 않지만 "왜 모의인가"로 시간을 태우는 함정이다.

함께 확인할 것.

1. `KIS_APP_KEY`·`KIS_APP_SECRET`·`KIS_ACCOUNT_NO`가 **실전 계좌** 자격증명인가. 실전망은 실전 앱키만 받는다
2. 대시보드 배지가 `LIVE`인가. `SIM`이면 `KIS_APP_KEY`가 비었다는 뜻이다
3. 기동 로그에 `[Session] KIS API 클라이언트 생성 (모드: 실전(prod))`가 남았는가

`KIS_SERVER`는 건드릴 필요가 없다.

### 실효값 확인 방법

소스만으로는 알 수 없다. 실행 중인 앱에 물어본다. **모든 설정값을 한 번에 되돌려주는 엔드포인트는 없다** — `GET /api/config`가 그 역할이었으나 2026-08-06에 제거됐다(무동작 UI 전용이었고, 인증만 통과하면 임의 키 쓰기·시크릿 평문 열람까지 붙어 있었다).

| 방법 | 위치 | 확인되는 것 |
|---|---|---|
| 대시보드 계좌 배지 | `Core/SessionManager.cs`의 `GetAccountInfo()` → `GET /api/portfolio/summary`의 `accountMode`·`accountMasked` | 거래 모드(`SIM`/`PAPER`/`LIVE`)와 마스킹 계좌번호 |
| 기동 로그 | `TB_SYSTEM_LOG`(`GET /api/history/logs`) 또는 Render 로그 | `[Session] KIS API 클라이언트 생성 (모드: ...)` 등 실제 분기 결과 |
| 적립 설정 화면 | `GET /api/dca/config` | DB 전용 키(`DCA_TEMPLATES`·`DCA_MONTH_MAP`)의 실효값 |
| Render 환경변수 목록 | Render 대시보드 | 어떤 키가 환경변수 층에 박혀 있는지 |

나머지 키(시크릿·부트스트랩)는 **설정 여부조차 앱에 물어볼 수단이 없다.** 값이 잘못됐는지는 증상으로 역추적한다 — 배지가 `SIM`이면 `KIS_APP_KEY`가 비었고, 크론이 전부 `401`이면 `API_ACCESS_KEY`가 어긋났고, 로그인이 안 되면 `MASTER_KEY`/`AUTH_TOKEN_SECRET`이 없다.

`render.yaml`이 저장소에 없으므로 프로덕션 환경변수 목록은 **Render 대시보드가 유일한 진실**이다. 현재 배포에 설정된 환경변수는 10개다 — `ADMIN_EMAIL`, `API_ACCESS_KEY`, `AUTH_TOKEN_SECRET`, `DATABASE_URL`, `IS_PAPER_TRADING`, `KIS_ACCOUNT_NO`, `KIS_APP_KEY`, `KIS_APP_SECRET`, `MASTER_KEY`, `RESEND_API_KEY`. `KIS_ACCOUNT_PROD`는 기본값 `01`이 맞아 설정하지 않았다.

### 로컬 개발 최소 기동 조건

| 항목 | 필수 여부 | 비고 |
|---|---|---|
| PostgreSQL (localhost / `autoinvest`) | **필수** | `DBManager`가 실패 시 `Logger.Fatal` + rethrow → 기동 불가 |
| .NET 8 SDK | **필수** | |
| Node + `npm install` | 프론트 사용 시 | 백엔드를 Vite 프록시 대상 포트에 바인딩 |
| `MASTER_KEY` | 권장 | 없으면 시크릿 평문 저장. `AUTH_TOKEN_SECRET`까지 없으면 로그인 불가 |
| KIS 키 | 불필요 | 없으면 `SimBrokerClient` |
| `API_ACCESS_KEY` | **사실상 필수** | 크론 트리거뿐 아니라 **최초 관리자 설정(`POST /api/auth/setup`)의 유일한 통과 수단**이다. 관리자가 없는 새 환경에서 이 값이 없으면 Bearer도 못 받고 `x-api-key`도 못 써 관리자를 만들 수 없다(복구: 환경변수/`appsettings.local.json`에 추가 후 재기동, 또는 `TB_APP_CONFIG`에 행 직접 삽입) |
| Resend 키 | 불필요 | 알림 미사용 시 |

로컬 시크릿은 `appsettings.local.json`에 둔다. `.gitignore`와 `.dockerignore` 양쪽에서 제외된다. 템플릿 파일은 없다 — 위 표의 키만 넣으면 되고, 필요한 최소 항목은 `MASTER_KEY` 하나다.

> `appsettings.example.json`은 2026-08-07에 삭제했다. 스스로를 `appsettings.local.json`으로 복사하라고 지시하면서 `Dca.Quantities`에 실제 종목·수량(`SPLG`/`QQQ`)을 담고 있었다 — 복사하면 로드되는 파일이 되고, DB 조회 실패 시 그 값으로 폴백해 의도하지 않은 종목을 실계좌에 매수할 수 있었다. 키 목록은 이 문서가 더 정확하므로 템플릿을 따로 두지 않는다.

**로컬에 `DATABASE_URL`을 운영 DB 주소로 넣지 않는다.** 넣으면 DB에서 `IS_PAPER_TRADING="0"`과 실전 KIS 키를 읽어 **로컬 실행이 실계좌에 주문**할 수 있다. 로컬 개발용 `MASTER_KEY`는 운영 값과 다른 값을 쓴다.

### 배포 DB는 Neon — 특유 제약 4개

`DATABASE_URL`이 가리키는 배포 DB는 **Neon**이다. Render Postgres가 아니므로 아래가 운영 판단에 직접 영향을 준다.

| 특성 | 영향 |
|---|---|
| **autosuspend (scale to zero)** | idle이면 컴퓨트가 정지하고 첫 쿼리에 콜드 스타트가 붙는다. 조회 실패 시 `TryGetFromDb`가 `Logger.Warn`으로 삼키고 `null`을 반환해 **폴백 경로로 조용히 넘어간다** |
| **DB 브랜치** | SQL Editor에서 브랜치를 잘못 고르면 다른 데이터를 본다. 운영 브랜치를 확인할 것 |
| **웹 SQL Editor** | `console.neon.tech` → SQL Editor. `psql` 없이 브라우저에서 쿼리 가능 |
| **브랜치 스냅샷** | `DELETE`·`ALTER` 전에 브랜치를 떠두면 완전한 롤백 수단이 된다 |

첫 번째가 실제 사고로 이어질 수 있었다. `DCA_TEMPLATES` 조회가 실패하면 레거시 폴백을 타고 의도하지 않은 종목·수량으로 실계좌 매수가 나갈 수 있었다. 2026-08-03에 DB 레거시 키(`DCA_QTYS`·`DCA_BUDGET_KRW`·`DCA_TARGETS`)를 삭제하고 `appsettings.json`의 `Dca:Quantities`를 비워 **폴백이 "잘못 사기"가 아니라 "안 사기"로 끝나게** 바꿨다. 삭제 직전 실측한 `DCA_QTYS` 값은 `{"GLD":1,"VTI":1,"VOO":1}`로 당월 템플릿과 종목·수량이 전부 달랐다 — 폴백이 발동했다면 의도와 다른 바스켓을 샀을 것이다.

### 알려진 함정

`RESEND_API_KEY`와 `ADMIN_EMAIL`은 **DB(`TB_APP_CONFIG`)에 넣어도 읽히지 않는다.** `NotificationService`가 `AppConfigManager`를 경유하지 않고 환경변수와 appsettings만 읽기 때문이다. 이 둘은 반드시 환경변수(또는 로컬 `appsettings.local.json`)에 둔다. `API_ACCESS_KEY`는 `AppConfigManager.Get` 경유라 DB 값도 읽히므로 동작이 갈린다.

`NotificationService.Initialize`는 기동 시 1회만 호출된다. `SessionManager.Reset()`도 유일한 호출자였던 `ConfigController`가 사라져 **현재 호출자가 0건**이다 — 즉 런타임에 설정을 다시 읽는 경로가 없고, 어떤 설정이든 반영하려면 재배포(재기동)해야 한다.

**(해소됨 2026-08-06)** `POST /api/config`에 키 화이트리스트가 없었다. 인증을 통과한 요청은 `DCA_LAST_RUN_MONTH`·`ADMIN_PASSWORD_HASH`를 포함한 임의 키를 DB에 쓸 수 있었고, `DcaSettings.SaveConfig`의 티커·수량·Id 검증을 우회한 원시 JSON 주입도 가능했다. 읽기는 화이트리스트로 막혀 있어 방향이 비대칭이었다. 함께 있던 `GET /api/config/secret/{key}`는 크론용 `x-api-key`만으로 앱키·시크릿·계좌번호 **평문**을 열람할 수 있었다. 세 엔드포인트를 모두 제거해 이 쓰기·읽기 표면이 사라졌다 — 화이트리스트를 추가하는 대신 무동작 UI 전체를 걷어낸 결과다. 남은 DB 쓰기 경로는 검증이 붙은 `PUT /api/dca/config`와 `POST /api/auth/setup`뿐이다.

`AppConfigManager.Get`은 호출마다 DB 커넥션을 새로 연다. `ApiKeyAuthAttribute`가 모든 인증 요청에서 이를 호출하므로 요청당 최소 1회 DB 왕복이 발생하고, DB 장애가 곧 `API_ACCESS_KEY` 빈 값 → 크론 401로 나타난다.

DB 스키마 변경은 자동 적용 경로가 없다. `DBManager.RunMigration`이 2026-07-30에 제거되어 기동 시 `create_tables.sql`만 실행하며, 그 파일은 `CREATE TABLE IF NOT EXISTS`이므로 컬럼 추가가 기존 DB에 반영되지 않는다. ALTER는 `Data/sql/`에 별도 스크립트로 두고 수동 적용한다.

## 정리 / 결론

핵심 규칙 넷이다.

1. **부트스트랩 3개(`DATABASE_URL`·`MASTER_KEY`·`AUTH_TOKEN_SECRET`)와 운영 시크릿·설정은 환경변수에 둔다.** 시크릿을 DB로 옮기면 노출 표면이 늘고 `MASTER_KEY` 의존이 커진다.
2. **도메인 데이터와 런타임 상태는 DB에만 둔다.** 특히 `DCA_LAST_RUN_MONTH`를 환경변수로 만들면 과매수 방지 가드가 무력화된다.
3. **실전 전환 스위치는 `IS_PAPER_TRADING="0"` 하나다.** `KIS_SERVER`는 죽은 설정이다.
4. **실효값은 실행 중인 앱에 묻는다.** 소스와 대시보드 어느 쪽도 단독으로는 답이 아니다. 다만 전체 설정을 되돌려주는 API는 없다 — 배지·기동 로그·Render 대시보드를 합쳐 판단한다.
5. **설정을 바꾸는 방법은 Render 환경변수 수정 + 재배포 하나다.** 화면에서 바꾸는 경로는 2026-08-06에 제거됐다(적립 설정 화면이 다루는 `DCA_TEMPLATES`·`DCA_MONTH_MAP`만 예외).

## 참고

- `Data/AppConfigManager.cs`, `Data/DBManager.cs`, `Data/sql/create_tables.sql`
- `Core/SessionManager.cs`, `Core/DcaSettings.cs`, `Core/DailyExecutionService.cs`, `Core/TaxEstimator.cs`
- `Utils/CryptoUtil.cs`, `Utils/NotificationService.cs`, `Utils/ApiKeyAuthAttribute.cs`
- `Controllers/AuthController.cs`, `Controllers/DcaController.cs`, `Controllers/PortfolioController.cs`
- `appsettings.json`, `Dockerfile`, `.gitignore`, `.dockerignore`
- `.agents/rules/security.md`, `.agents/rules/recommended_rules.md`
- `Documents/worklog/[2026-08-03] 01_실전 전환 첫날 준비와 설정 표면 정리.md`
- `Documents/worklog/[2026-08-03] 02_크론 지연으로 월 경계를 넘긴 첫 적립 집행.md`
