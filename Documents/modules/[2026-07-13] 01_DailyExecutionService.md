---
title: 모듈 노트 — DailyExecutionService (Core)
date: 2026-07-13
company: [개인]
tags: [DCA, 적립사이클, 멱등가드, 모듈노트]
status: done
---

# 모듈 노트 — DailyExecutionService (Core)

## 개요
> `Core/DailyExecutionService.cs` — 외부 크론이 부르는 적립 사이클의 진입점. "이번 달 이미 샀나?"를 먼저 확인하고, 안 샀으면 로그인→설정로드→엔진 실행→성공 시 "이번 달 샀음" 도장→항상 보고 메일까지 오케스트레이션한다.

## 배경 / 목적
- 파일: `Core/DailyExecutionService.cs` · 5순위
- 위험도 **2(중)** — 실주문을 직접 하진 않으나, 적립 사이클의 진입점이자 "월 1회만 산다"는 멱등 가드의 주인이다.
- 이 노트는 오너 관점 요약(역할·결정·함정)과 라인 바이 라인 정독을 함께 담아, 이 모듈을 처음 만지는 사람이 흐름과 위험을 빠르게 파악하도록 돕는다.

## 본문

### 한 문장 역할
외부 크론이 부르는 **적립 사이클의 진입점**. "이번 달 이미 샀나?"를 먼저 확인하고, 안 샀으면 로그인→설정로드→DcaAccumulationEngine 실행→성공 시 "이번 달 샀음" 도장 찍기→항상 보고 메일까지 오케스트레이션한다. **자기가 판단하거나 주문하지 않는다** — 엔진에 위임한다.

### 입력 → 처리 → 출력·부작용 (`RunDcaCycleAsync`)
- **입력**: 없음(파라미터 0). 크론이 `POST /api/order/dca-run`으로 호출 → `OrderController`가 이 메서드를 부른다.
- **처리**: 멱등 가드 확인 → (스킵 아니면) 로그인 → DcaSettings의 `Load()`로 이번 달 템플릿의 `(수량, 예산)` 로드 → `AccumulateAsync` 실행.
- **출력**: 사람이 읽는 상태 문자열 한 줄(체결 주수 또는 스킵/오류 사유). 호출한 컨트롤러가 응답에 쓴다.
- **부작용**: ① 성공 시 `AppConfigManager.Set(DCA_LAST_RUN_MONTH, "yyyy-MM")` (DB `TB_APP_CONFIG` write) ② 항상 보고 메일(`NotificationService`, `finally`) ③ 로그 다수.

### 이 코드가 내리는 결정 (평문)
- **이번 달 이미 샀으면 즉시 스킵**: `DCA_LAST_RUN_MONTH`(KST "yyyy-MM")가 이번 달과 같으면 로그인도 안 하고 바로 반환.
- **못 사는 상황이면 조용히 스킵**(오류 아님): 로그인 실패 / 설정 수량 0개 → 매수 없이 반환.
- **"샀음" 도장은 체결 ≥ 1주일 때만 찍는다**: 체결 0건(전량 실패·장마감 등)이면 마커를 **안 남긴다** → 다음 날 호출 시 자동 재시도.
- **메일은 무슨 일이 있어도 보낸다**: 스킵·오류·부분 체결 전부 `finally`에서 보고.

### 멱등 가드가 핵심인 이유 (⚠️ 실거래 전환)
- 현재 크론은 **매일**(GitHub Actions, KST 23:40) 부른다. 이 가드가 없으면 매일 한 달치를 사서 ~30배 과매수가 된다.
- 지금은 **모의계좌**라 매일 사도 무방하지만, 가드 덕에 실계좌에서도 "그 달 처음 성공한 날 1회만" 사도록 이미 설계돼 있다.
- 단, 실거래 전환 시엔 크론 주기 변경(월 1일)과 이 가드를 **함께** 두는 게 안전(상세: `recommended_rules.md` "실거래 전환 시 필수 변경").
- 마커를 **거래이력이 아니라 전용 키**(`DCA_LAST_RUN_MONTH`)로 쓰는 이유: 사용자가 수동으로 1주 산 걸 "이번 달 적립 완료"로 오판하지 않기 위함.

### 헷갈리기 쉬운 지점 / 함정
- **월 판단 기준이 두 곳 모두 KST**로 일치한다: 이 서비스의 `CurrentKstMonth()`(`UtcNow.AddHours(9)`)와 DcaSettings의 `KstNow()`가 같은 방식. (반면 DcaAccumulationEngine의 `TradeDate=DateTime.Now`는 서버 로컬시각이라 기준이 다르다 — 그건 그 모듈의 불명확 항목.)
- **DI 수명**: `SessionManager`를 생성자로 주입받는다. `RunDcaCycleAsync`는 `IServiceScopeFactory`로 스코프를 열어 호출되는 게 정상 패턴(호출부 = `OrderController`).
- **반환 문자열은 UI/응답용**일 뿐, 흐름 제어에 쓰이지 않는다. 스킵/오류/성공을 문자열로 구분해 담는다.
- **엔진은 `new DcaAccumulationEngine(client)`로 직접 생성**한다(DI 아님). 클라이언트만 세션에서 얻어 넘긴다.

### 당신이 만질 일이 생기면 여기
- **"월 1회" 규칙 바꾸기**(예: 분기 1회, 매주): 멱등 가드 블록 + 마커 키 형식(`CurrentKstMonth`). 바꾸면 실거래 과매수 위험과 직결되니 반드시 확인 후.
- **스킵 사유·메일 문구**: `statusNote` 문자열들 / `SendDcaReportAsync`.
- **무엇을 살지·수량**: 여기가 아니라 DcaSettings(템플릿·월배정)와 DcaAccumulationEngine(집행).

### 라인 바이 라인 정독
오케스트레이션 위주라 대부분 자명. 핵심 분기만 짚는다.

#### `RunDcaCycleAsync()` (45~115행)
- **멱등 가드**(52~59): `CurrentKstMonth()` vs `AppConfigManager.Get(LastRunMonthKey)`. 같으면 스킵 메시지 반환(로그인 전).
- **로그인**(63~73): 이미 로그인 상태가 아니면 `LoginAsync`. 실패면 스킵 반환(→ `finally`에서 메일).
- **설정 로드**(76): DcaSettings의 `Load()` → `(quantities, budget)`. 내부에서 이번 달 템플릿 선택까지 끝난 결과다.
- **수량 0 가드**(78~82): 비었으면 경고 로그 + 스킵 메시지(매수 안 함).
- **엔진 실행**(84~101): `AccumulateAsync(quantities, budget)` → `filled`. **체결 > 0이면** 마커 저장(93), **0이면** 미표시 + 다음 날 재시도 안내(98).
- **catch**(103~107): 예외를 `statusNote`에 담고 에러 로그(빈 catch 아님).
- **finally**(108~111): `SendDcaReportAsync(filled, statusNote)` — 어떤 경로로 끝나든 항상 발송.
- **반환**(114): `statusNote` 있으면 그걸, 없으면 체결 요약.

#### `SendDcaReportAsync(filled, statusNote)` (120~147행)
- `statusNote` 있으면 안내 배너 HTML 생성 → 체결 0건이면 "매수 없음", 있으면 티커별 묶어 주수·단가 리스트 → `NotificationService.SendEmailAsync`.
- 자체 `try/catch`로 감싸 **메일 실패가 사이클을 깨지 않게** 한다(에러 로그만).

## 정리 / 결론

### 리팩토링 노트 (2026-07-13)
- **코드 변경 없음.** 이 서비스는 이미 Phase 6 원칙에 부합한다 — 판단 없음(엔진 위임), 멱등 가드로 실거래 과매수 방지, 빈 catch 없음, 스킵/오류/성공을 명확히 구분해 항상 보고. 실주문과 가까운 오케스트레이터라 **동작 보존**을 위해 구조를 건드리지 않았다.
- 안전망: `AccumulateAsync`처럼 정적 DAO(`AppConfigManager`)·브로커 I/O에 의존해 순수 단위 테스트가 어렵다. 멱등 가드/스킵 분기 검증은 `IS_PAPER_TRADING`(Sim) 모드로 대체(동작 변경 없이 유닛화하려면 DI 리팩토링이 필요 → 하지 않음).

### 불명확 항목
- 없음(새로 발견한 것 없음). 관련 불명확 항목(`TradeDate` 시각 기준 등)은 DcaAccumulationEngine 노트에 이미 기록됨 → Data 레이어 단계에서 처리.

## 참고
- 이번 달 수량·예산 결정: `Documents/modules/[2026-07-04] 04_DcaSettings.md`
- 집행(매수 계획·주문): `Documents/modules/[2026-07-04] 03_DcaAccumulationEngine.md`
- Sim 검증: `Documents/modules/[2026-07-04] 05_SimBrokerClient.md`
- 멱등 마커 저장소: `Documents/modules/[2026-07-04] 02_AppConfigManager.md`
