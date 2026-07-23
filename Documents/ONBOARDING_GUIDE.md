# AutoInvesting 프로젝트 온보딩 가이드 🚀

이 문서는 개발자가 AutoInvesting 프로젝트의 전체 흐름과 각 구성요소를 쉽게 이해하고 즉시 기여할 수 있도록 돕기 위해 작성되었습니다.

> **먼저 알아둘 것 (Phase 6 — DCA 적립 코어 전환)**: 이 시스템은 더 이상 "지금 사야 할까"를 판단하지
> 않습니다. 정직한 백테스트(2012~현재) 결과 퀀트/AI 타이밍 판단이 단순 적립식(DCA)에 2.7~4배 열세였고,
> 완벽한 타이밍조차 평균 대비 연 +0.3~0.9%에 불과(타이밍은 잘해야 본전)함이 드러났습니다. 그래서
> **판단 레이어(SmartOrderEngine, 퀀트 엔진, AI 분석기, 합의 스코어링 등)를 전부 제거**하고, **현재 월에
> 배정된 매수 템플릿대로 종목별 고정 수량을 매수하는 적립(DCA) 코어**로 전환했습니다. 핵심 가치는 "판단"이 아니라 **"자동화"**입니다.

## 1. 프로젝트 아키텍처 개요 (Overview)

본 프로젝트는 ASP.NET Core 기반의 **Headless 백그라운드 서비스 및 REST API 서버**입니다.
크게 다음과 같은 흐름으로 동작합니다.

```
[외부 크론잡 (매수 주기에 호출)]            [외부 요청 (Web UI, API Client)]
      │ POST /api/order/dca-run                │ (x-api-key 인증 통과 시)
      ▼                                        ▼
[ OrderController ] ──(202 즉시 반환)     [ Controllers ] (적립 설정 편집·잔고/내역 조회)
      │ 백그라운드 Task                         │
      ▼                                        └── (데이터 조회) ─▶ [ Data/DAO ] ─▶ PostgreSQL
[ DailyExecutionService.RunDcaCycleAsync ]
      │  로그인 → DcaSettings.Load → DcaAccumulationEngine.AccumulateAsync → 이메일 보고서
      ▼
[ Core/DcaAccumulationEngine ] ──▶ [ SessionManager → 브로커(Sim/KIS) ] ──▶ 정수 단위 매수 + 기록
```

- **적립 사이클 트리거**: 백그라운드 타이머 대신 **외부 크론잡**이 매수 주기(예: 매월 첫 거래일)에
  `POST /api/order/dca-run`을 호출하는 구조입니다. 컨트롤러는 즉시 202를 반환하고 실제 처리는 백그라운드에서 진행됩니다.

## 2. 생명주기와 의존성 주입 (Dependency Injection)

프로젝트 핵심 인스턴스들은 `Program.cs`에서 등록되어 시스템 전역에서 생명주기가 관리됩니다.

- `SessionManager` (싱글턴): 앱 내에서 브로커 세션(토큰 등)의 생명주기를 관리합니다. Controllers는 DI를 통해 주입받아 사용합니다. *(Phase 6에서 AI analyzer 분기 책임은 제거되어, 이제 브로커 생명주기만 담당합니다.)*
- `DBManager` (싱글턴): PostgreSQL 커넥션(Npgsql) 관리를 책임집니다.
- `DailyExecutionService` (Scoped): 적립 사이클 실행 진입점. `OrderController`가 `IServiceScopeFactory`로 Scope를 만들어 호출합니다.
- **예시 흐름 (`/api/order/dca-run` 호출 시)**:
  `OrderController`가 요청 수신 → 백그라운드 `Task` 시작 + 즉시 202 반환 → Scope 생성 후 `DailyExecutionService.RunDcaCycleAsync()` 실행 → `SessionManager`에서 브로커 수령 → `DcaAccumulationEngine`이 매수 실행.

## 3. 핵심 마법: 브로커 환경 분기 전략 (`IS_PAPER_TRADING`)

AutoInvesting 엔진은 자신이 **가짜 돈(모의)을 쓰는지 진짜 돈(실전)을 쓰는지 모릅니다.** 브로커 추상화 인터페이스(`IBrokerClient`)를 사용하기 때문입니다.

- `SessionManager`는 `IS_PAPER_TRADING` 환경변수(또는 `appsettings.json`의 값)가 `1`이거나, 설정된 API Key가 없으면 기본적으로 **`SimBrokerClient` (가상 모의투자 환경)**를 주입합니다.
- 반대로 키가 정상적으로 존재하면 **`KisBrokerClient` (한국투자증권 실거래망/모의망 연결)**를 주입합니다.
  - KIS 연동에서도 `Kis:Server` 값을 통해 KIS 실전망(prod)과 KIS 모의투자망(vps)으로 한 번 더 분기할 수 있습니다.

## 4. 적립 로직: 월별 템플릿의 고정 수량 매수 (DcaAccumulationEngine)

매수 결정은 타이밍 판단이 아니라 **현재 월에 배정된 매수 템플릿**으로만 이루어집니다. `DcaAccumulationEngine`의 흐름을 살펴봅시다.

1. **설정 로드 (`DcaSettings.Load`)**: 여러 매수 템플릿(명명된 예산 + 종목별 고정 수량) 중 현재(KST) 월에 배정된 템플릿을 골라, 그 템플릿의 종목별 수량과 예산을 가져옵니다.
   - 템플릿 선택(`SelectTemplate`, 순수 함수) 규칙: **월배정에 이번 달이 있으면 그 Id의 템플릿을 사용**(Id가 목록에 없으면 매수 스킵), **월배정이 비어 있으면 첫(기본) 템플릿을 매월 사용**(기존 단일 설정 동작 유지), **월배정은 있으나 이번 달 배정이 없으면 매수 스킵.**
   - 우선순위: **DB(`TB_APP_CONFIG`의 `DCA_TEMPLATES` JSON, `DCA_MONTH_MAP` JSON) → 레거시 단일 설정(`DCA_QTYS`/`DCA_BUDGET_KRW`) → `appsettings.json`의 `Dca` 섹션 폴백.** 레거시 설정은 '기본' 템플릿 하나로 자동 이관되어 읽힙니다.
   - UI(`DcaController` → `PUT /api/dca/config`)에서 저장하면 DB에 기록되어 다음 사이클부터 반영됩니다.
2. **시세 수집 (`AccumulateAsync`)**: 브로커에서 환율(USD→KRW)과 종목별 현재가를 조회합니다. 현재가를 못 가져오는 종목은 자동 제외하며, 나머지 종목은 비중 재조정 없이 설정된 고정 수량 그대로 매수합니다.
3. **순수 매수 계획 (`PlanPurchases`)**:
   ```
   for (템플릿의 종목별 (ticker, qty)):
       qty <= 0 이면 제외
       현재가가 없거나 0 이하이면 제외
       그 외에는 지정 수량(qty)을 그대로 계획에 담고
       총 매수금액(totalCostKrw) += qty × 현재가 × 환율
   ```
   - 외부 I/O가 없는 **순수 함수**라 입력만 넣으면 출력(종목별 수량·총 매수금액)을 단위 테스트로 검증할 수 있습니다.
   - **예산은 이 계획 단계에서 고려하지 않습니다** — 초과 여부는 호출부(`AccumulateAsync`)에서 판단하며, 예산은 수량을 줄이지 않는 **초과 경고용 상한**일 뿐입니다. 비중(%)·매수금액은 사람이 정하는 입력이 아니라 수량×현재가로 환산되는 표시용 값입니다.
4. **주문 실행 + 기록**: 계획대로 `PlaceBuyOrderAsync()`를 호출하고 체결분을 `TradeHistoryDAO`에 기록합니다. 총 매수금액이 예산을 초과하면 수량은 그대로 진행하되 경고만 남깁니다. 사이클 종료 시 `NotificationService`로 적립 보고서 이메일을 발송합니다.

## 5. 보안 정책: 내 로컬 API 자격 증명 다루기

코드 저장소에 API 키가 유출되는 사고를 방지하기 위해 이중 보안 구조를 적용해 두었습니다.

### 로컬 환경에서 시크릿 관리하기
1. 프로젝트 루트에 `appsettings.local.json` 파일을 만듭니다. (이 파일은 `.gitignore`에 등록되어 있어 **절대 커밋되지 않습니다.**)
2. 발급받은 비밀 정보(KIS 앱키/시크릿/계좌번호, SMTP 비밀번호, 서버 접근키 등)를 이곳에 입력합니다.

```json
{
  "Kis": { "AppKey": "나의_앱키", "AppSecret": "나의_앱시크릿", "AccountNo": "계좌번호" },
  "Security": { "ApiAccessKey": "아무도모르는_나만의_서버_암호" }
}
```

> 시크릿은 절대 소스코드·로그·커밋·문서에 넣지 마세요. 위 예시의 값은 모두 자리표시자입니다.

### 서버 보호: API Key Authentication
개발된 백엔드 기능을 타사 프론트엔드 등에서 호출하려면 반드시 위에서 설정한 `Security:ApiAccessKey` 값을 HTTP 헤더 **`x-api-key`**에 담아 요청해야 합니다. 그렇지 않으면 `401 Unauthorized` 오류가 발생하여 비인가 접근을 원천 차단합니다.

## 6. 프론트엔드 화면 구성

React SPA는 다음 네비게이션으로 구성됩니다.

| 메뉴 | 경로 | 역할 |
|------|------|------|
| 대시보드 | `/` | 잔고·환율 등 현황 조회 |
| 적립 설정 | `/dca-config` | 매수 템플릿·월별 배정 편집 (`GET/PUT /api/dca/config`) |
| 주문·적립 | `/order` | 적립 사이클 실행(`/api/order/dca-run`) + 수동 매수/매도(`/api/order/manual`) |
| 거래 내역 | `/history` | 체결 내역 조회 |
| 설정 | `/settings` | 환경 설정 |

## 7. 참고: 레거시 데이터 (TB_MARKET_SNAPSHOT)

과거 AI 학습용으로 적재하던 `TB_MARKET_SNAPSHOT` 테이블과 `DBManager`의 관련 마이그레이션 코드는
**과거 데이터 보존을 위해 DB 스키마에는 남아 있으나, Phase 6 이후 어디서도 기록·조회하지 않습니다**
(레거시 데이터, 현재 미사용). 신규 개발 시 참조할 필요가 없습니다.
