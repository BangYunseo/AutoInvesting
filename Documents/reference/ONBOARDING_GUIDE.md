---
title: AutoInvesting 프로젝트 온보딩 가이드
date: 2026-07-23
company: [개인]
tags: [온보딩, 아키텍처, DCA적립, 브로커분기]
status: draft
---

# AutoInvesting 프로젝트 온보딩 가이드

## 개요
> 개발자가 AutoInvesting의 전체 흐름과 각 구성요소를 빠르게 이해하고 즉시 기여할 수 있도록 돕는 문서다.
>
> **먼저 알아둘 것 (Phase 6)**: 타이밍 판단 레이어는 백테스트로 가치 없음이 확인돼 전부 제거됐습니다. 매수는 **현재 월에 배정된 매수 템플릿의 종목별 고정 수량**으로만 결정됩니다 — 배경·수치는 `.agents/rules/project_overview.md`, 지켜야 할 규칙은 `.agents/rules/recommended_rules.md`.

## 1. 프로젝트 아키텍처 개요 (Overview)

본 프로젝트는 ASP.NET Core 기반의 **Headless 백그라운드 서비스 및 REST API 서버**입니다.
크게 다음과 같은 흐름으로 동작합니다.

```text
[외부 크론잡 (매일 호출)]                   [외부 요청 (Web UI, API Client)]
      │ POST /api/order/dca-run                │ (x-api-key 인증 통과 시)
      ▼                                        ▼
[ OrderController ] ──(202 즉시 반환)     [ Controllers ] (적립 설정 편집·잔고/내역 조회)
      │ 백그라운드 Task                         │
      ▼                                        └── (데이터 조회) ─▶ [ Data/DAO ] ─▶ PostgreSQL
[ DailyExecutionService.RunDcaCycleAsync ]
      │  월1회 가드·지정일 게이트(DB 전용 마커, 조회 실패 시 매수 중단) → 로그인 → DcaSettings.Load → AccumulateAsync → 이메일 보고서
      ▼
[ Core/DcaAccumulationEngine ] ──▶ [ SessionManager → 브로커(Sim/KIS) ] ──▶ 고정 수량 매수 + 기록
```

- **적립 사이클 트리거**: **외부 크론잡(GitHub Actions) 2개**가 전부입니다 — `daily-run.yml`이 **매일**(KST 00:10) `POST /api/order/dca-run`을, `reconcile.yml`이 미장 마감 후 `POST /api/order/reconcile`(체결 대사)을 호출합니다. 컨트롤러는 즉시 202를 반환하고 실제 처리는 백그라운드에서 진행됩니다. **매일 호출인데 월 1회만 집행되는 이유는 엔진의 멱등 가드와 지정일 게이트**입니다 (`CODE_READING_GUIDE.md` Step 2 참조).
- 🚫 **인앱 스케줄러(`BackgroundService`)를 넣지 마세요** — Render 무료 인스턴스는 유휴 시 프로세스가 멈춰 타이머가 오류 없이 죽습니다. 근거와 예외 조건은 `.agents/rules/architecture.md`.

## 2. 생명주기와 의존성 주입 (Dependency Injection)

프로젝트 핵심 인스턴스들은 `Program.cs`에서 등록되어 시스템 전역에서 생명주기가 관리됩니다.

- `SessionManager` (싱글턴): 앱 내에서 브로커 세션(토큰 등)의 생명주기를 관리합니다. Controllers는 DI를 통해 주입받아 사용합니다.
- `DBManager` (싱글턴): PostgreSQL 커넥션(Npgsql) 관리를 책임집니다.
- `DailyExecutionService` (Scoped): 적립 사이클 실행 진입점. `OrderController`가 `IServiceScopeFactory`로 Scope를 만들어 호출합니다.
- **예시 흐름 (`/api/order/dca-run` 호출 시)**:
  `OrderController`가 요청 수신 → 백그라운드 `Task` 시작 + 즉시 202 반환 → Scope 생성 후 `DailyExecutionService.RunDcaCycleAsync()` 실행 → `SessionManager`에서 브로커 수령 → `DcaAccumulationEngine`이 매수 실행.

## 3. 핵심 마법: 브로커 환경 분기 전략 (`IS_PAPER_TRADING`)

AutoInvesting 엔진은 자신이 **가짜 돈(모의)을 쓰는지 진짜 돈(실전)을 쓰는지 모릅니다.** 브로커 추상화 인터페이스(`IBrokerClient`)를 사용하기 때문입니다.

- `SessionManager.GetClient()`는 **KIS 앱키(`KIS_APP_KEY`)가 없으면** — `IS_PAPER_TRADING` 값과 무관하게 — **`SimBrokerClient` (가상 모의투자 환경)**를 주입합니다. 키 없이는 실거래가 불가능하기 때문입니다.
- 키가 존재하면 항상 **`KisBrokerClient` (한국투자증권 연결)**를 주입하되, **실전망(prod)/모의망(vps) 분기는 `IS_PAPER_TRADING`이 결정**합니다 — `"0"`이면 실전망(`openapi…:9443`), 그 외 값이면 모의망(`openapivts…:29443`). 즉 **Sim/KIS 선택 기준은 "키 유무", prod/vps 선택 기준은 `IS_PAPER_TRADING`**입니다(`Kis:Server` 값은 분기에 쓰이지 않습니다).

## 4. 적립 로직: 월별 템플릿의 고정 수량 매수 (DcaAccumulationEngine)

매수 결정은 타이밍 판단이 아니라 **현재 월에 배정된 매수 템플릿**으로만 이루어집니다. `DcaAccumulationEngine`의 흐름을 살펴봅시다.

1. **설정 로드 (`DcaSettings.Load`)**: 여러 매수 템플릿(명명된 예산 + 종목별 고정 수량) 중 현재(KST) 월에 배정된 템플릿을 골라, 그 템플릿의 종목별 수량과 예산을 가져옵니다.
   - 템플릿 선택 규칙(`SelectTemplate`)은 `.agents/rules/architecture.md`의 «적립(DCA) 배분 원칙», 설정 층별 조회 우선순위(환경변수 → DB → appsettings)는 `Documents/reference/CONFIG_REFERENCE.md`에 한 번만 적어 둡니다.
   - UI(`DcaController` → `PUT /api/dca/config`)에서 저장하면 DB에 기록되어 다음 사이클부터 반영됩니다.
2. **시세 수집 (`AccumulateAsync`)**: 브로커에서 환율(USD→KRW)과 종목별 현재가를 조회합니다. 현재가를 못 가져오는 종목은 자동 제외하며, 나머지 종목은 비중 재조정 없이 설정된 고정 수량 그대로 매수합니다.
3. **순수 매수 계획 (`PlanPurchases`)**:
   ```text
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
2. 발급받은 비밀 정보(KIS 앱키/시크릿/계좌번호, Resend API 키, 서버 접근키 등)를 이곳에 입력합니다. 메일 발송은 SMTP가 아니라 Resend HTTP API를 사용하므로 SMTP 계정·비밀번호는 필요하지 않습니다.

```json
{
  "Kis": { "AppKey": "나의_앱키", "AppSecret": "나의_앱시크릿", "AccountNo": "계좌번호" },
  "Security": { "ApiAccessKey": "아무도모르는_나만의_서버_암호" }
}
```

> 시크릿은 절대 소스코드·로그·커밋·문서에 넣지 마세요. 위 예시의 값은 모두 자리표시자입니다.

### 서버 보호: 전역 인증 필터
모든 엔드포인트는 전역 필터(`ApiKeyAuthAttribute`)로 보호됩니다. 통과 방법은 두 가지입니다.
- **사람/Web UI**: `AuthController`(`/api/auth/login`)로 로그인해 받은 **서명된 세션 토큰**을 `Authorization: Bearer <token>` 헤더로 전송합니다. 프론트엔드는 이 경로를 사용합니다.
- **외부 크론잡**: 위에서 설정한 `Security:ApiAccessKey` 값을 HTTP 헤더 **`x-api-key`**에 담아 전송합니다.

둘 중 하나로 통과하며, 어느 쪽도 없으면 `401 Unauthorized`입니다. 면제는 **`/api/auth/status`와 `/api/auth/login` 둘뿐**이고 초기설정(`/api/auth/setup`)은 면제가 아니므로, 새 환경의 최초 관리자 생성에는 `x-api-key`(`API_ACCESS_KEY`)가 반드시 필요합니다. `[PublicEndpoint]`를 컨트롤러 클래스에 붙이지 말아야 하는 이유는 `.agents/rules/architecture.md`에 있습니다.

## 6. 프론트엔드 화면 구성

React SPA는 다음 네비게이션으로 구성됩니다.

| 메뉴 | 경로 | 역할 |
|------|------|------|
| 대시보드 | `/` | 잔고·환율 등 현황 조회 |
| 적립 설정 | `/dca-config` | 매수 템플릿·월별 배정 편집 (`GET/PUT /api/dca/config`) |
| 주문·적립 | `/order` | 적립 지정일(`DCA_RUN_DAY`)·추가 적립 예약 설정(`/api/order/dca-schedule`), 적립 사이클 강제 실행(`/api/order/dca-run?force=true`), 수동 매수/매도(`/api/order/manual`) |
| 거래 내역 | `/history` | 체결 내역 조회 |

설정 화면은 없습니다. 운영 설정은 전부 Render 환경변수로 주입되며 변경은 **환경변수 수정 + 재배포**로 합니다
(2026-08-06 제거 — 자세한 경위는 `Documents/reference/DEVELOPMENT.md`, 복구 절차는 `RECOVERY.md`).
