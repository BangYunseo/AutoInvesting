---
title: 개발 진척도 (CHANGELOG)
date: 2026-07-23
company: [개인]
tags: [개발이력, CHANGELOG, Phase6, DCA적립]
status: draft
---

# 개발 진척도 (CHANGELOG)

## 개요
> AutoInvesting 프로젝트의 개발 진행 상황을 기록하는 변경 이력(CHANGELOG)이다. 새 개발자가 현재 상태와 다음 작업을 파악할 수 있도록 유지한다.

## 현재 상태: Phase 6 완료 — DCA 적립 코어 전환 ✅

- **Phase 1** (기반): ✅ 완료
- **Phase 2** (엔진 코어 + 배분 UI): ✅ 완료
- **Phase 2.5** (퀀트 엔진 모듈): ✅ 완료
- **Phase 2.6** (구조 리팩토링): ✅ 완료
- **Phase 3** (KIS 실거래 연동): ✅ 완료
- **Phase A** (프로젝트 정비/안정화): ✅ 완료
- **Phase B/C** (운영 안정성 및 확장): ✅ 완료
- **Phase 4-a~e** (AI 시장분석 엔진 / 확률 기반 합의 스코어링): ✅ 완료 → ⚠️ **Phase 6에서 제거**
- **Phase 5-a~d** (적응형 임계값 / AI 성과·토큰 모니터링 / 성과 피드백 루프): ✅ 완료 → ⚠️ **Phase 6에서 제거**
- **Phase 6** (판단 레이어 제거, DCA 적립 코어 전환): ✅ **완료**
- **Phase 6+** (이후 추가된 정보·보조 기능): **Auth**(단일 관리자 인증·전역 필터), **Tax**(매도 양도세 추정 — `sell-preview`), **Price**(현재가 조회·티커 검증). ⚠️ Tax는 매수 의사결정에 값을 넘기지 않음(판단 레이어 아님). **Macro**(FRED 거시지표 브리핑)는 화면에 배선되지 않아 소비자가 0이어서 2026-07-30 정리에서 제거됨.

> ⚠️ **Phase 2~5의 판단(타이밍) 기능은 Phase 6에서 코드째 제거되었습니다.** 현재 동작은 아래 "Phase 6 상세 변경 이력"과 그 위의 날짜별 항목을 기준으로 보세요.

## 2026-08-07 — 같은 달 중복 매수 경로 2건 차단 (fail-closed)

실자금으로 같은 달에 두 번 매수하게 되는 경로 둘을 닫았다. 둘 다 "조용히 빈 값·빈 목록이 되어 그대로 통과한다"는 같은 형태의 결함이다.

- **월 적립 가드**: `RunDcaCycleAsync`가 `DCA_LAST_RUN_MONTH`·`DCA_FORCE_RUN_MONTH`를 `AppConfigManager.Get`으로 읽어, Neon 콜드 스타트 한 번이나 동명 환경변수 하나로 가드가 뚫렸다(`Get`은 "조회 실패"와 "값 없음"을 같은 기본값으로 뭉개고, DB보다 환경변수를 먼저 본다). 두 키 모두 DB 전용이므로 `TryReadDb`로 바꾸고 **조회 실패 시 매수하지 않는다(fail-closed)**. 이 정지는 `?force=true`로도 넘어가지 않으며, 스킵 경로 중 유일하게 보고 메일을 발송한다.
- **보유 잔고 조회**: `KisBrokerClient.GetHoldingsAsync`가 `rt_cd`를 검사하지 않아 KIS 업무 오류(HTTP 200 + `rt_cd`≠`"0"`)가 "보유 0건"으로 통과했다(Polly는 예외·5xx·429·408만 재시도해 이 경로를 잡지 못한다). 그 결과 `ReconcileAsync`가 이미 체결된 달의 마커를 해제해, 다음 크론이 템플릿 전량을 실자금으로 재매수할 수 있었다. 이제 예외를 던지고 호출부의 `catch`가 스냅샷을 보존해 다음 실행에서 재시도한다.
- **`appsettings.example.json` 삭제**: 자신을 `appsettings.local.json`으로 복사하라고 안내하면서 `Dca.Quantities` 예시 수량을 담고 있어, DB 조회가 실패하면 예시가 실제 매수 수량이 될 수 있었다. 설정 키의 단일 진실 원천은 `Documents/reference/CONFIG_REFERENCE.md`다.
- 함께 갱신: `CONFIG_REFERENCE.md`(DB 전용 키·동명 환경변수 위험), `.agents/rules/architecture.md`(가드 `TryReadDb` 규칙 + 인앱 스케줄러 금지). 근거는 각 문서에 있으므로 되풀이하지 않는다.

### 기각한 대안 (트리거·스케줄러)
- **GitLab CI 이전**: GitHub Actions 크론과 기능 차이가 없는데 저장소·시크릿을 이중 관리해야 한다. 검토 경위는 `Documents/worklog/[2026-08-07] 01_AutoInvesting GitLab CI 이전 검토.md`.
- **인앱 `BackgroundService` 타이머**: 구현했다가 전량 되돌렸다. Render 무료 인스턴스는 유휴 시 프로세스가 멈춰 타이머가 오류 없이 죽는다 — 적립이 조용히 누락된다.
- **cron-job.org**: 외부 의존을 하나 더 늘리면서 얻는 것이 없다. 트리거는 `.github/workflows/` 2개(`daily-run`·`reconcile`)로 유지한다.

## 2026-08-06 — 설정 화면·ConfigController 제거 (동작하지 않는 UI 정리)

### 제거한 것
- `Controllers/ConfigController.cs` 파일 전체 삭제 → `GET /api/config`(운영 설정 조회), `POST /api/config`(설정 저장 + 세션 리셋), `GET /api/config/secret/{key}`(시크릿 평문 단건 조회) 세 엔드포인트가 사라졌다.
- `Frontend/src/pages/Settings.jsx` 삭제 + `App.jsx`의 import·`/settings` 라우트·네비 링크 제거 → 화면은 **로그인 / 대시보드 / 적립 설정 / 주문 설정 / 거래 내역** 5개로 줄었다.
- `Utils/ApiKeyAuthAttribute.cs`에 같은 날 추가했던 `AuthKind` 표식(`HttpContext.Items`)도 소비자가 없어져 제거. 필터 동작(Bearer 세션 토큰 또는 `x-api-key` 중 하나로 통과, `[PublicEndpoint]`만 면제)은 그대로다.
- `Data/AppConfigManager.cs` 주석에서 "값 확인은 `GET /api/config`로 한다"는 문장 제거.

### 제거한 이유 — 읽히지 않는 설정을 저장하던 화면
Render 배포에는 운영 설정 10개가 **전부 환경변수로** 주입되어 있다(이름만: `ADMIN_EMAIL`, `API_ACCESS_KEY`, `AUTH_TOKEN_SECRET`, `DATABASE_URL`, `IS_PAPER_TRADING`, `KIS_ACCOUNT_NO`, `KIS_APP_KEY`, `KIS_APP_SECRET`, `MASTER_KEY`, `RESEND_API_KEY`). `AppConfigManager.Get()`은 **환경변수 → DB → appsettings** 순으로 읽으므로 환경변수에 값이 있으면 DB를 아예 보지 않는다. 즉 설정 화면에서 거래 모드나 KIS 자격증명을 저장해도 실제 동작에는 반영되지 않는 "동작하지 않는 UI"였다.

### 함께 사라진 위험 3건
- **임의 키 기록**: `POST /api/config`에 키 화이트리스트가 없어, 인증을 통과한 요청이 `DCA_LAST_RUN_MONTH`(월 1회 적립 멱등 가드)·`ADMIN_PASSWORD_HASH`를 포함한 임의 키를 `TB_APP_CONFIG`에 쓸 수 있었다. `Documents/reference/CONFIG_REFERENCE.md`에 알려진 문제로 기록돼 있던 항목이며, 엔드포인트 제거로 해소됐다.
- **시크릿 평문 열람**: `GET /api/config/secret/{key}`는 크론용 `x-api-key`만으로도 앱키·시크릿·계좌번호 평문을 열람할 수 있었다(같은 날 세션 토큰 전용으로 좁혔다가, 엔드포인트 자체를 없앴다).
- **시크릿 DB 저장 경로**: 시크릿을 DB에 적재하는 쓰기 경로 자체가 사라졌다.

### 앞으로의 설정 변경 경로
- **Render 환경변수 수정 + 재배포**가 유일한 경로다. 화면에서 바꾸는 수단은 없다.
- 계좌 모드(LIVE/PAPER/SIM)와 마스킹된 계좌번호는 `GET /api/portfolio/summary`의 `accountMode`/`accountMasked`로 대시보드 상단 배지가 이미 보여준다.

### 같은 날 선행 작업 (기동·저장 경로 강화)
- `MASTER_KEY`가 없으면 경고만 남기고 뜨던 것을 **기동 거부**로 바꿨다(`Main`을 `int` 반환으로 전환해 종료 코드 전달). 키 없이 떠 있으면 암호문을 복호화하지 못해 빈 값이 되고, `SessionManager`가 "앱키 없음"으로 판단해 조용히 `SimBrokerClient`로 폴백한다 — 화면에는 체결이 찍히지만 실제로는 아무것도 사지 않는다.
- `AppConfigManager.Set`의 **평문 저장 분기 삭제 → 저장 거부**. 한 번 DB에 들어간 평문은 스냅샷 때문에 회수할 수 없고, 계좌번호는 개인정보 취급 대상이다.

### 같은 날 신규 — 매월 적립 지정일(`DCA_RUN_DAY`)
- 크론은 매일 돌고 가드는 "월 1회"만 보장하므로 그 달의 첫 성공일이 곧 집행일이었다. 사람이 날짜를 고를 수 있게 `DCA_RUN_DAY`(KST, `1`~`DcaSettings.MaxRunDay`(31), `0`=해제)를 추가했다. `DailyExecutionService.IsOnOrAfterRunDay`가 지정일 전 크론 호출을 흘려보내고(짧은 달은 말일로 보정), 사람이 누른 즉시 실행(`force`)·추가 적립 예약(`reserved`)은 명시적 의사이므로 통과시킨다.
- 편집·조회는 `PUT`/`GET /api/dca/config`의 `runDay`(응답에 `maxRunDay` 동봉)로 한다. 지정일 저장 실패는 삼키지 않고 `500`으로 알린다 — 조용히 실패하면 크론이 월초부터 매수해 사람이 고른 날보다 이르게 실자금이 나간다.
- 집행 일자(`DCA_LAST_RUN_DATE`)는 **표시 전용**으로 기록해 `GET /api/order/dca-schedule`이 `lastRunDate`로 내려준다. 이번 달 것이 아니면 빈 값이다 — 지난달 날짜를 그대로 내려보내면 화면이 "이번 달 그 날 샀다"로 읽힌다.

## 2026-08-04 — 실계좌 운영 첫 주 결함 정리와 화면 개편

실계좌(LIVE)로 값이 실제로 채워지면서 "조용히 0이거나 비어 있어도 화면이 그럴듯하던" 결함들이 한꺼번에 드러난 날이다. 상세는 `Documents/worklog/[2026-08-04] 01·02`에 있다.

### 인증 경계 (worklog 01)
- `AuthController`의 클래스 레벨 `[PublicEndpoint]` 제거 → **면제는 `status`·`login` 둘뿐**. `setup`은 전역 필터를 받아 `x-api-key`가 필요하다. 관리자 자리가 비어 보이는 순간 누구나 관리자를 선점해 실주문까지 낼 수 있던 경로를 닫았다.
- `AppConfigManager.TryReadDb` 추가 — 조회 실패와 값 없음을 구분한다. `status`/`setup`/`login`이 같은 판정을 쓰고, DB 조회 실패면 `503`(fail-closed).
- `AppConfigManager.Set`을 `void` → `bool`로. 저장 실패를 삼켜 거짓 성공을 만들던 경로를 호출부가 감지한다.
- `LoginThrottle` 추가 — 로그인 실패 **전역 속도 상한**(분당 20회). 상한 검사는 PBKDF2 앞에 둬 추측과 CPU 소모를 같은 지점에서 막는다. 유효한 `x-api-key`는 면제(소유자 탈출구).
- Swagger를 개발 환경으로 제한. 전역 필터는 MVC 액션 필터라 미들웨어인 Swagger에 걸리지 않아 프로덕션에서 API 표면이 익명 노출되고 있었다.
- 회귀 테스트: `PublicEndpointExposureTests`(면제 목록 리플렉션 고정), `LoginThrottleTests`, `AuthControllerThrottleWiringTests`(배선 고정).

> ⚠️ `API_ACCESS_KEY`가 크론용에서 **부트스트랩 필수**로 성격이 바뀌었다. 없으면 새 환경에서 관리자를 만들 수 없다.

### 적립 실행·예약 (worklog 02)
- `RunDcaCycleAsync(force)` + `POST /api/order/dca-run?force=true` — 화면에서 누른 추가 적립이 당월 가드에 막히던 문제. 크론은 파라미터를 붙이지 않아 월 1회 동작 유지.
- `DCA_FORCE_RUN_MONTH` 예약 마커 + `GET·POST /api/order/dca-schedule` — 한국 낮에는 미국장이 닫혀 즉시 실행이 거부되므로, 이미 개장 직후에 도는 크론이 1회만 가드를 넘도록 예약한다. 값이 **월**이라 달을 넘기면 저절로 무효가 된다(다음 달 2회 매수 방지).
- 확인창을 상태 인식형으로 + 브라우저 `confirm()` → 앱 모달. 적용될 템플릿과 수량을 서버(`SelectTemplate`, 엔진과 동일 로직)에서 받아 붉은 굵은 글씨로 표시.

### KIS 연동 수정
- **예수금**: `frcr_dncl_amt_2`는 체결기준현재잔고(`inquire-present-balance`, `CTRP6504R`)의 `output2`에만 있는 필드인데 잔고조회(`TTTS3012R`)에서 찾고 있어 항상 $0이었다. 엔드포인트 교체 + `crcy_cd`로 USD 행 선택 + 실패 로그에 `rt_cd`/`msg1` 기록.
- **토큰 폭주**: 발급 실패 후 쿨다운이 없어, 실패하면 API 호출 하나하나가 새 발급을 불러 403이 스스로를 재생산했다. 실패 시 70초 보류 + 응답 본문 로깅.

### 화면
- 대시보드: 총 자산 = 주식 평가액 **+ 예수금**(이전엔 주식만 담아 두 카드가 같은 값), 보유 종목 비중을 표에서 빼 **의존성 없는 SVG 도넛**으로 분리.
- 적립 설정: 월별 배정 좌측 리스트 + 우측 **집행 회차별** 실행 로그(연도 이동), 월 예산 천 단위 표기, 종목 없는 템플릿이 전체 저장을 막던 문제 해결.
- 네이티브 `select` 4곳 → 선택 칩(팝업 제거), 시스템 로그 날짜 입력 → **로그가 있는 날만 활성화되는 자체 달력**.
- 로그인 전 탭 전환 차단, 모바일 줄바꿈 보정.

### 체결 대사 (같은 날 추가)
- 접수만으로 그 달을 완료로 세면, 지정가가 안 붙어 장 마감에 소멸해도 완료로 남아 적립이 조용히 누락됐다. `POST /api/order/reconcile` + `.github/workflows/reconcile.yml`(매일 UTC 21:30, 미장 마감 후) 신설.
- 판정은 **주문 전후 보유 수량 차이**로 한다. 체결내역 API를 쓰지 않아 새 tr_id·파라미터를 맞출 필요가 없다. 근거 데이터는 `DCA_PENDING_SNAPSHOT`(주문 직전 보유 수량 + 접수 주문)이며 적립 사이클이 기록한다.
- 전량 미체결이면 `DCA_LAST_RUN_MONTH`를 해제해 재시도를 허용한다. **부분 체결이거나 수량이 줄어든 종목이 있으면 마커를 건드리지 않는다** — 되돌리면 이미 체결된 종목을 또 산다.
- 매수 지정가 버퍼는 **넣지 않았다.** 관측된 미체결이 0건이라 근거가 추측뿐이었고, 대사 경로가 생겨 미체결이 실제로 발생하는지 데이터로 볼 수 있게 됐다. 데이터를 보고 결정한다.

### 남은 일
- CI에서 테스트가 실행되지 않는다(회귀 방지 테스트는 로컬 `dotnet test`에서만 동작).
- `dotnet test`가 `DATABASE_URL`이 가리키는 실제 DB를 읽는다.

## ⚠️ 실거래 전환 — 과매수 방지(코드)·계좌 LIVE 전환 모두 완료

- 문제: 예산은 월 단위(기본 100만원)인데 `DcaAccumulationEngine.AccumulateAsync`가 호출마다 전액을 새로 소진해, 매일 도는 크론이 월 예산을 약 30배로 집행했다.
- 260701: `DailyExecutionService.RunDcaCycleAsync`에 당월 멱등 가드(`DCA_LAST_RUN_MONTH`, "yyyy-MM", KST) 추가. **접수(주문 수락) 1건 이상일 때만** 마커를 남겨 그 달 남은 호출을 스킵하고, 접수 0건인 날은 마커가 없어 다음 날 자동 재시도한다. 체결 여부는 장 마감 후 체결 대사가 판정한다(접수≠체결). 260804부터 사람이 누른 `?force=true`는 가드를 우회하고, 260807부터 마커 조회 실패는 fail-closed(매수 중단)다.
- 마커를 거래이력이 아니라 **전용 키**로 두는 이유: 사람이 수동으로 1주 산 것을 "이번 달 적립 완료"로 오판하지 않기 위함이다(이 판정을 `TB_TRADE_HISTORY`에서 파생시키지 말 것).
- 260803: 크론 시각 `40 14`(KST 23:40) → `10 15 1-31 * *`(매일 KST 00:10). 크론 지연이 KST 자정을 넘겨 월 판정이 뒤집힌 사고 때문.
- 2026-08-01: 실계좌(LIVE) 첫 집행 완료. 전환 스위치는 `IS_PAPER_TRADING=0` 하나뿐이다. 현재 동작 규칙은 `.agents/rules/recommended_rules.md`, 환경변수 이름·출처·순서는 `Documents/reference/CONFIG_REFERENCE.md`와 `RECOVERY.md`가 단일 출처다.

## Phase 6 상세 변경 이력 — 판단 레이어 제거 & DCA 적립 코어 전환

### 핵심: "퀀트/AI로 타이밍을 판단" → "월별 템플릿의 종목별 고정 수량을 매수하는 적립(DCA)"

정직한 백테스트(2012~현재) 결과 **퀀트/AI 타이밍 판단이 단순 적립식(DCA)에 2.7~4배 열세**였고,
완벽한 타이밍조차 평균 대비 연 +0.3~0.9%에 불과(타이밍은 잘해야 본전)함이 검증되었습니다.
이에 따라 **판단 레이어 전체를 제거**하고, 여러 **매수 템플릿**(종목별 고정 수량 + 예산)을
정의해 **월별로 배정**하고, 현재 월에 배정된 템플릿의 종목별 고정 수량을 매 사이클 그대로 매수하는
**DCA 적립 코어**로 전환했습니다. 시스템의 가치는 "판단"이 아니라 **"자동화"**에 있습니다.

> 참고: 최초 전환은 "목표비중을 향해 정수 단위 매수(DCA_TARGETS)" 모델이었으나, 이후
> "매수 템플릿 + 월별 배정(DCA_TEMPLATES/DCA_MONTH_MAP)" 모델로 발전했습니다. 아래 설명은
> **현재 동작(템플릿 모델)** 기준입니다.

```text
변경 전 (Phase 5):
  DailyExecutionService.RunDailyCycleAsync
    → SmartOrderEngine → 퀀트(QuantIndicator/QuantFilter) + AI(차트/펀더멘털) + 합의 스코어링
    → BuyProbability ≥ 임계값일 때만 매수

변경 후 (Phase 6, 현재):
  DailyExecutionService.RunDcaCycleAsync
    → 월 1회 멱등 가드(DCA_LAST_RUN_MONTH) 확인 — 당월 적립 완료 시 스킵
    → DcaSettings.Load → SelectTemplate(현재 KST 월에 배정된 템플릿 선택)
    → DcaAccumulationEngine.AccumulateAsync → 템플릿의 종목별 고정 수량을 그대로 매수
    → TradeHistoryDAO 기록 + 이메일 보고서  (판단·타이밍 없음)
```

### 6-1. 신규 파일 (3건)

| 파일 | 설명 |
|------|------|
| `Core/DcaAccumulationEngine.cs` | 적립식 매수 엔진. `PlanPurchases`(순수 함수 — 현재가가 있는 종목의 고정 수량 매수 계획 + 총 매수금액 산출) + `AccumulateAsync`(현재가/환율 조회 → 계획 → 주문 → `TradeHistoryDAO` 기록). 판단/타이밍 없음 |
| `Core/DcaSettings.cs` | 매수 템플릿·월배정·예산의 단일 읽기/쓰기 지점. `SelectTemplate`(순수 함수 — 월→템플릿 선택)로 현재 월 템플릿을 고름. 우선순위 DB(`TB_APP_CONFIG`: `DCA_TEMPLATES`/`DCA_MONTH_MAP` JSON) → 레거시 `DCA_QTYS`/`DCA_BUDGET_KRW`/`appsettings.json` `Dca` 섹션 폴백(자동 '기본' 템플릿 이관) |
| `Controllers/DcaController.cs` | `GET/PUT /api/dca/config` — 매수 템플릿·월배정 조회·저장 (GET: templates/monthMap/currentMonth/activeTemplateId, PUT: templates+monthMap). 저장값은 DB 기록, 다음 사이클 반영 |

### 6-2. 수정 파일 (3건)

| 파일 | 변경 내용 |
|------|----------|
| `Core/DailyExecutionService.cs` | `RunDcaCycleAsync`만 유지 — 월 1회 멱등 가드(`DCA_LAST_RUN_MONTH`) → 로그인 → `DcaSettings.Load` → `AccumulateAsync` → 이메일 보고서. (구 `RunDailyCycleAsync`/AI 평가/일일 보고서 제거) |
| `Controllers/OrderController.cs` | `POST /api/order/dca-run`(적립 사이클, 202 즉시 반환) + `POST /api/order/manual`(판단 없는 수동 매수/매도, SELL 시 보유수량·절세 서버 가드) + `GET /api/order/sell-preview`(매도 양도세 프리뷰)만 남김. (구 `execute`/`analyze`/`daily-run` 제거) |
| `appsettings.json` | `Trading`/`Smtp`/`Resend`/`Kis`/`Security`/`Dca`/`Tax` 섹션 유지. `Rebalance`/`Consensus`/`FxAdvisor`/`Ai` 섹션 제거. `Dca = { MonthlyBudgetKrw, Quantities:{SPLG:3,QQQM:2,SCHD:5,GLD:1} }` (레거시 폴백용 — 실동작은 DB의 `DCA_TEMPLATES`/`DCA_MONTH_MAP`). `Smtp`는 `SenderName`/`AdminEmail` 폴백 2개만 유지(발송은 Resend HTTP API) |

### 6-3. 제거된 파일·개념

판단(타이밍) 레이어 전체가 코드베이스에서 삭제되었습니다.

| 분류 | 제거 대상 |
|------|----------|
| Core 엔진/분석 | `SmartOrderEngine`, `Core/Quant/*` 전부(`QuantIndicator`, `QuantFilter`, `AdaptiveThresholdEngine`, `PerformanceFeedbackEngine`, `BacktestEngine`, `RebalancingEngine`, `SellStrategyManager`), `Core/Advisors/*` 전부, `AiMarketAnalyzer`, `GeminiMarketAnalyzer`, `IMarketAnalyzer`, `IMcpDataProvider`, `AllocationEngine`, `Utils/PromptBuilder` |
| Data DAO/DTO | `AiPerformanceDAO`, `MarketSnapshotDAO`, `SellPlanDAO`, `TokenUsageDAO`, `StrategyDAO` 및 관련 DTO(`ConsensusScoreDto`, `IndicatorDto`, `AdvisoryNoteDto`, `AgentAccuracyDto`, `AiPerformanceDto`, `BacktestResultDto`, `MarketSnapshotDto`, `SellPlanDto`, `TokenUsageDto`/`SummaryDto`, `WeightSchemeResultDto`, `StrategyDto`) |
| Controllers | `BacktestController`, `MonitoringController`, `QuantController`, `SellPlanController`, `StrategyController` |
| 프론트 페이지 | `Backtest`, `Monitoring`, `SellPlanManager`, `Strategy` |
| 개념 | AI 투자위원회/3자 합의, `CalculateConsensusScore`, 가중치 임계값(Consensus), 적응형 임계값, 성과 피드백 루프, 토큰 비용 모니터링, 차트AI/펀더멘털AI, 환헤지 어드바이저(FxAdvisor), 리밸런싱 |

### 6-4. 유지된 것 (자동화 인프라)

`IBrokerClient`/`KisBrokerClient`/`SimBrokerClient`, `SessionManager`(이제 브로커 생명주기만 — AI analyzer 분기 제거),
`TradeHistoryDAO`, `NotificationService`(Resend HTTP API — Render의 SMTP 포트 차단 우회), `ExchangeRateService`, `DBManager`/`AppConfigManager`,
`ConfigController`(→ 2026-08-06 제거), `PortfolioController`, `HistoryController`, `TestController`(send-test-email만 — 실주문 경로 없음). 외부 크론잡이 `dca-run`을 호출하는 구조.

### 6-5. 프론트엔드 재구성

| 페이지 | 경로 | 설명 |
|--------|------|------|
| Dashboard | `/` | 현황 조회 (유지) |
| DcaConfig | `/dca-config` | 적립 설정 — 매수 템플릿(추가/복제/삭제/종목 수량·티커검증·예산) + 월별 배정 그리드 편집 (신규) |
| Order | `/order` | 적립 실행 + 수동 주문 (재작성) |
| History | `/history` | 거래 내역 (유지) |
| Settings | `/settings` | 환경 설정 (유지 → **2026-08-06 제거**) |

네비게이션: **대시보드 / 적립 설정 / 주문·적립 / 거래 내역 / 설정**
(이 시점 기록입니다. 설정은 2026-08-06에 제거되어 현재 네비게이션은 4개입니다.)

### 6-6. 참고 — 레거시 데이터 보존

`TB_MARKET_SNAPSHOT` 테이블은 **과거 데이터 보존을 위해 `Data/sql/create_tables.sql`의 DDL로만 남아 있고,
`MarketSnapshotDAO` 제거에 따라 현재는 어디서도 기록·조회하지 않습니다.** 기존 문서의
"AI 학습용 누적 데이터" 설명은 모두 **"과거(레거시) 데이터, 현재 미사용"**으로 해석하면 됩니다.

`DBManager`의 관련 ALTER 마이그레이션 코드는 2026-07-30 정리에서 제거되었습니다(`create_tables.sql`이
컬럼을 이미 정의해 중복이었음). 현재 마이그레이션 자동 실행 경로는 없습니다.

### 6-7. 이후 보강 (매수 템플릿 · 실거래 가드 · 단위 테스트)

| 날짜 | 내용 |
|------|------|
| 260629 | 단일 목표비중 → **매수 템플릿 + 월별 배정** 모델로 발전(`DcaTemplate` DTO, `DCA_TEMPLATES`/`DCA_MONTH_MAP`, `DcaConfig.jsx` 재작성). 레거시 단일 설정은 '기본' 템플릿으로 자동 이관 |
| 260630 | 수동주문 보유종목 연동(SELL 서버 가드·보유수량 상한), 대시보드 계좌 모드 배지·마스킹 계좌 표시, 요약/보유 새로고침 분리 |
| 260701 | **실거래 전환 대비 월 1회 멱등 가드**(`DCA_LAST_RUN_MONTH`) + 크론 `40 14 1-31 * *`(매일 시도, 처음 성공하는 날 1회 적립) |
| 260702 | **단위 테스트 프로젝트 신설**(`Tests/`, xUnit). `PlanPurchases`(7건)·`SelectTemplate`(5건) 순수 함수 검증. 이를 위해 `DcaSettings`의 월→템플릿 선택 로직을 `SelectTemplate` 순수 함수로 분리(동작 불변) |
| 260730 | **죽은 코드·미배선 기능 정리**. Macro/FRED 스택 일괄 제거(참조 0), `POST /api/test/buy` 제거(`manual`과 중복·실전 자기차단), `Templates/DailyReportTemplate.html`·미사용 프론트 자산(`App.css`·`assets/*`·`icons.svg`) 제거, `DBManager` ALTER 마이그레이션 9건+`RunMigration` 제거, `create_tables.sql`에서 `TB_ASSET_MASTER`·`TB_INVEST_STRATEGY`·죽은 앱설정 시드 제외, 죽은 CSS 클래스 제거, 알림박스 `.alert` 공용 클래스화, `ExchangeRateService` 문자열 파서 2개 → `ParseKrwRate` 순수함수 1개(+테스트 4건, 총 40건). 상세: `Documents/worklog/[2026-07-30] 01_죽은 코드 미배선 기능 정리.md` |
| 260730 | **거래이력 주문번호(`ORDER_NO`) 저장 배선**. 브로커가 준 주문번호(KIS `ODNO`)가 DTO까지 채워졌는데 `TradeHistoryDAO`의 INSERT/SELECT 컬럼에 없어 DB에 저장되지 않고 History 화면 주문번호 칼럼이 항상 빈칸이었다. 컬럼은 스키마에 이미 있어 변경 없이 배선만 추가. 증권사 계좌와 우리 기록을 잇는 유일한 키이며, 지정가 주문(`ORD_DVSN=00`)을 접수 시점에 `FILLED`로 기록하는 현 구조에서 미체결 추적의 실마리이기도 하다 |

> 테스트 실행: `dotnet test Tests/AutoInvest.Tests.csproj` (net8.0, xUnit). 메인 웹 프로젝트는
> `AutoInvest.csproj`에서 `Tests\**`를 컴파일 대상에서 제외해 분리되어 있습니다.

> 📌 **Phase 5-d ~ Phase 2 시절의 상세 변경 이력(파일별 신규/수정 표, 합의 스코어링 공식, 퀀트 전략·리밸런싱 표, WinForms Panel 구조)은 2026-08-07에 삭제했습니다.** Phase 2~5는 "퀀트 지표 + AI 위원회 합의로 매수 타이밍을 판단한다"는 시도였고, 정직한 백테스트에서 단순 적립에 열세임이 확인돼 Phase 6에서 판단 레이어를 코드째 제거했습니다. 거기서 설명하던 클래스·엔드포인트·화면·DB 컬럼은 현재 코드베이스에 하나도 없어, 문단마다 "지금은 없다"는 경고를 달아야만 오해를 막을 수 있었습니다.
> - 판단 레이어를 접은 근거(백테스트 수치): 위 "Phase 6 상세 변경 이력" 도입부
> - 당시 코드 실물: `git log`(Phase 6 이전 커밋) / AI 엔진 비용·토큰 분석: `Documents/worklog/[2026-06-02] 01_AI엔진 도입 비용 분석.md`
