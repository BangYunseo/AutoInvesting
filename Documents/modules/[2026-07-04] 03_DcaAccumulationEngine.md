---
title: DcaAccumulationEngine 모듈 노트
date: 2026-07-04
company: [개인]
tags: [DCA, 적립엔진, Core, 매수주문]
status: done
---

# DcaAccumulationEngine 모듈 노트

## 개요
> 이번 달 템플릿에 정해진 종목별 고정 수량을 그대로 사고, 그 체결을 기록하는 적립 실행 엔진이다. 타이밍·비중·신호 판단은 일절 없다.

## 배경 / 목적
- 파일: `Core/DcaAccumulationEngine.cs` · Phase 3 · 4순위
- 작성일 2026-07-04 · 위험도 **3(최고)** — 실제 매수 주문이 발생하는 적립 심장부

이 엔진이 하지 않는 것(중요):

- ❌ "지금 살까 말까" 판단 없음. ❌ 비중(%)을 계산해서 수량을 정하기 없음. ❌ 예산을 맞추려 수량을 깎기 없음.
- 백테스트로 타이밍 판단이 무가치함이 확인돼 제거된 결과다(Phase 6). 이 엔진은 **설정된 수량을 집행하는 실행기**일 뿐이다.

## 본문
### 순수/부수효과 분리
두 개의 메서드가 순수 계산과 부수효과를 나눈다.

| 메서드 | 성격 | 하는 일 |
|---|---|---|
| `PlanPurchases(...)` | **순수 함수**(외부 I/O 0) | "무엇을 몇 주, 총 얼마어치" 계산만. 단위 테스트 대상 |
| `AccumulateAsync(...)` | 부수효과 | 환율·현재가 조회 → `PlanPurchases` 호출 → **주문 실행 → DB 기록 → 메일** |

이 분리 덕에 **"계산이 맞는가"는 실주문 없이 `PlanPurchases`로 검증**할 수 있다(`Tests/DcaAccumulationEngineTests.cs` 7건).

### 입력·처리·출력·부작용 (`AccumulateAsync`)
- **입력**: `quantities`(종목별 수량), `budgetKrw`(예산 상한). 호출부 DailyExecutionService가 `DcaSettings.Load()`로 만들어 넘긴다.
- **처리**: 환율 조회 → 종목별 현재가 조회(실패 종목 제외) → `PlanPurchases`로 계획+총액 산출 → 예산 초과면 경고 → 계획대로 순차 매수.
- **출력**: 체결된 `TradeHistoryDto` 목록(`filled`). 아무것도 못 사면 빈 목록.
- **부작용**: ① `IBrokerClient.PlaceBuyOrderAsync`(실제/모의 주문) ② `TradeHistoryDAO.Insert`(DB `TB_TRADE_HISTORY` write) ③ `NotificationService.SendEmailAsync`(예산 초과·종목 실패 시, **await 안 함 = fire-and-forget**).

### 코드가 내리는 결정
- **살 종목 거르기**: 수량 ≤ 0 제외, 현재가 없거나 ≤ 0 제외 → 나머지는 **설정 수량 그대로**.
- **중단 조건**: `quantities` 비었으면 스킵 / 환율 ≤ 0이면 중단 / 유효 현재가 종목이 하나도 없으면 중단. (모두 빈 목록 반환)
- **예산 초과**: 총 매수금액 > 예산이면 **경고 로그 + 경고 메일만**, **수량은 그대로 진행**(감산 없음).
- **한 종목 주문 실패**: 그 종목만 `catch`로 로그+실패 메일, **나머지 종목은 계속**(부분 체결 허용).

### 헷갈리기 쉬운 지점 / 함정
- ⚠️ **`OrderNo`가 DB에 저장되지 않는다.** `PlaceBuyOrderAsync`가 준 주문번호를 DTO엔 담지만, `TradeHistoryDAO.Insert`의 INSERT 컬럼은 `(TRADE_DATE,TICKER,ORDER_TYPE,QTY,PRICE,STATUS)`로 `ORDER_NO`가 없다. 즉 주문번호는 **반환 목록(메일 보고서용)에만** 살아있고 DB엔 안 남는다. → 아래 불명확 항목.
- **예산은 상한 경고일 뿐 수량을 못 줄인다.** "예산=100만인데 130만어치" 상황에서도 130만어치 그대로 매수하고 경고만 낸다. (Phase 6 원칙 — 의도된 동작)
- **매수는 순차(`foreach`+`await`)** 다. 엔진 자체엔 Rate-limit 딜레이가 없다(그 처리는 KisBrokerClient 몫). 종목이 많으면 KIS TPS 주의.
- **메일은 fire-and-forget**(`_ = SendEmailAsync(...)`). 사이클을 막지 않으려는 의도지만, 메일 발송 예외는 관측되지 않는다.
- `TradeDate = DateTime.Now`(서버 로컬시각). DcaSettings의 월 판단은 `UtcNow.AddHours(9)`(KST 고정)라 **기준이 다르다** → 불명확 항목.
- **실거래 전환 시 위험**: 이 엔진은 "호출될 때마다" 산다. 일 크론 + 실계좌면 월 예산을 매일 소진해 과매수. 크론을 월 1회로 바꾸거나 멱등 가드가 필요(현재 멱등 가드는 DailyExecutionService에 있음).

### 유지보수 진입점
- **매수 계획 규칙(무엇을 거를지)**: `PlanPurchases`. 바꾸면 반드시 `DcaAccumulationEngineTests` 갱신.
- **예산 초과 시 동작 바꾸기**(예: 감산): `AccumulateAsync`의 예산 블록 — 단 **판단 재도입 금지 원칙**과 충돌하는지 먼저 검토.
- **주문·기록 필드**: 주문 실행 `foreach` 블록 / `TradeHistoryDto` 구성부.

### 불명확 항목 (사용자 확인 필요)
1. **`OrderNo`를 DB에 남기지 않는 게 의도인가?** 체결 추적/대사(reconciliation) 관점에선 저장이 유용. `TB_TRADE_HISTORY`에 `ORDER_NO` 컬럼 추가 여부는 **Data 레이어(DBManager) 단계에서** 스키마와 함께 판단 → 일단 보류.
2. **`TradeDate`의 시각 기준**: `DateTime.Now`(로컬) vs KST 고정 중 어느 쪽이 맞나? 배포 서버(Render) 타임존에 따라 기록 시각이 흔들릴 수 있음.

위 두 항목은 **동작 변경**이라 지금 손대지 않는다. Data/DBManager 단계 또는 별도 확인에서 처리한다.

### 라인 바이 라인 정독
#### `PlanPurchases(quantities, exchangeRate, priceUsd, out totalCostKrw)` (43~63행)
- `plan` 빈 맵, `totalCostKrw=0` 초기화.
- `quantities` 순회: 수량 ≤ 0 → `continue`(56). 현재가 없음 또는 ≤ 0 → `continue`(56).
- 통과분: `plan[티커]=수량`, `totalCostKrw += 수량 × 현재가 × 환율` 누적.
- **외부 호출 0, 시간 의존 0** → 결정적. 이게 검증 가능성의 핵심.

#### `AccumulateAsync(quantities, budgetKrw)` (72~159)
- **가드**(78~82): `quantities` null/빈 → 경고 로그 + 빈 목록 반환.
- **환율**(84~89): `GetExchangeRateAsync`. ≤ 0이면 에러 로그 + 중단.
- **현재가 수집**(91~102): 종목별 `GetCurrentPriceAsync`. ≤ 0인 종목은 경고 후 제외(`priceUsd`에 안 담음).
- **현재가 전무 가드**(104~108): 유효 현재가 0개면 에러 로그 + 중단.
- **계획 산출**(111): `PlanPurchases` 호출로 `plan`+`totalCostKrw`.
- **시작 로그**(113): 예산·환율·종목수.
- **예산 초과 경고**(116~122): `budgetKrw>0 && 총액>예산`이면 경고 로그 + 경고 메일(fire-and-forget). **수량 불변.**
- **주문 루프**(125~152): 종목별 `PlaceBuyOrderAsync` → `TradeHistoryDto` 구성 → `TradeHistoryDAO.Insert`(DB) → `filled`에 추가 → 완료 로그. 예외 시 그 종목만 에러 로그 + 실패 메일, 루프는 계속.
- **요약 로그**(154~156): 총 주수·종목별·총액.
- **반환**(158): `filled`.

### 리팩토링 노트 (2026-07-04)
- **코드 변경 없음.** 이 엔진은 이미 Phase 6 DCA 원칙(판단 없음·순수/부수효과 분리·예산 상한 경고)에 정확히 맞고, 실주문 위험이 최고라 **동작 보존**을 위해 구조를 건드리지 않았다.
- 발견한 두 지점(`OrderNo` 미저장, `TradeDate` 시각 기준)은 **동작 변경**이라 위 "불명확 항목"으로 남기고 Data 레이어 단계/별도 확인으로 미룸.
- 안전망: `PlanPurchases`는 `Tests/DcaAccumulationEngineTests.cs`(7건)로 이미 고정됨. `AccumulateAsync`는 정적 DAO·브로커 I/O 의존이라 단위 테스트하려면 의존성 주입 리팩토링이 필요 → **동작 변경 수반**이므로 지금은 하지 않고, 검증은 `IS_PAPER_TRADING`(Sim) 모드로 대체.

## 정리 / 결론
- 이 엔진은 판단이 없는 **실행기**다. 순수 함수 `PlanPurchases`(계산)와 부수효과 `AccumulateAsync`(주문·기록·메일)를 분리해 실주문 없이 계산을 검증할 수 있게 했다.
- 예산은 수량을 줄이지 않는 **상한 경고**이며, 종목 단위 실패는 전체 사이클을 멈추지 않고 부분 체결을 허용한다.
- 위험도가 최고라 동작 보존을 우선했고, `OrderNo` 미저장·`TradeDate` 시각 기준 두 지점은 동작 변경이므로 Data 레이어/별도 확인 단계로 미뤘다.

## 참고
- `Documents/modules/[2026-07-04] 01_죽은 코드 제거.md`
- `Documents/modules/[2026-07-04] 04_DcaSettings.md` — 입력 생성
- `Documents/modules/[2026-07-13] 01_DailyExecutionService.md` — 호출·멱등 가드
- `Documents/modules/[2026-07-04] 05_SimBrokerClient.md` — Sim 검증
- `Tests/DcaAccumulationEngineTests.cs` — `PlanPurchases` 단위 테스트 7건
