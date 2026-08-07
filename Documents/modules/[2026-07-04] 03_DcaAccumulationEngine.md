---
title: DcaAccumulationEngine 모듈 노트
date: 2026-07-04
company: [개인]
tags: [DCA, 적립엔진, Core, 매수주문]
status: done
---

# DcaAccumulationEngine 모듈 노트

## 개요
> 이번 달 템플릿에 정해진 종목별 고정 수량을 그대로 주문하고, 그 **접수**를 `PENDING`으로 기록하는 적립 실행 엔진이다. 타이밍·비중·신호 판단은 일절 없다.

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
| `AccumulateAsync(...)` | 부수효과 | 환율·현재가 조회 → `PlanPurchases` 호출 → **주문 실행 → DB 기록** (메일은 호출부가) |

이 분리 덕에 **"계산이 맞는가"는 실주문 없이 `PlanPurchases`로 검증**할 수 있다(`Tests/DcaAccumulationEngineTests.cs` 7건).

### 입력·처리·출력·부작용 (`AccumulateAsync`)
- **입력**: `quantities`(종목별 수량), `budgetKrw`(예산 상한). 호출부 DailyExecutionService가 `DcaSettings.Load()`로 만들어 넘긴다.
- **처리**: 환율 조회 → 종목별 현재가 조회(실패 종목 제외) → `PlanPurchases`로 계획+총액 산출 → 예산 초과면 경고 → 계획대로 순차 매수.
- **출력**: `DcaCycleResult`(접수 목록·실패 목록·예산경고·총 매수금액·환율). 접수분은 `STATUS='PENDING'`으로 기록된다 — 접수 ≠ 체결이며, 체결 확정은 `DailyExecutionService.ReconcileAsync`가 한다.
- **부작용**: ① `IBrokerClient.PlaceBuyOrderAsync`(실제/모의 주문) ② `TradeHistoryDAO.Insert`(DB `TB_TRADE_HISTORY` write). **메일은 보내지 않는다** — 예산 초과·종목 실패를 결과 객체에 모아 호출부가 종합 보고서 1통으로 발송한다(종목별 메일 난발 방지).

### 코드가 내리는 결정
- **살 종목 거르기**: 수량 ≤ 0 제외, 현재가 없거나 ≤ 0 제외 → 나머지는 **설정 수량 그대로**.
- **중단 조건**: `quantities` 비었으면 스킵 / 환율 ≤ 0이면 중단 / 유효 현재가 종목이 하나도 없으면 중단. (모두 빈 결과 반환)
- **예산 초과**: 총 매수금액 > 예산이면 **경고 로그 + `result.BudgetWarning` 적재만**, **수량은 그대로 진행**(감산 없음).
- **한 종목 주문 실패**: 그 종목만 `catch`로 로그 + `result.Failures` 적재, **나머지 종목은 계속**(부분 체결 허용). 메일은 호출부가 사이클 끝에 종합 1통으로 보낸다.

### 헷갈리기 쉬운 지점 / 함정
- **`OrderNo`는 DB에 저장된다**(2026-07-30 배선). `TradeHistoryDAO.Insert`의 INSERT 컬럼에 `ORDER_NO`가 포함되며, 장 마감 후 `ReconcileAsync`가 `UpdateStatusByOrderNo`로 상태를 갱신하는 근거다. 그 이전에 적재된 행은 `ORDER_NO`가 NULL이다.
- **예산은 상한 경고일 뿐 수량을 못 줄인다.** "예산=100만인데 130만어치" 상황에서도 130만어치 그대로 매수하고 경고만 낸다. (Phase 6 원칙 — 의도된 동작)
- **매수는 순차(`foreach`+`await`)** 다. 엔진 자체엔 Rate-limit 딜레이가 없다(그 처리는 KisBrokerClient 몫). 종목이 많으면 KIS TPS 주의.
- `TradeDate = DateTime.Now`(서버 로컬시각). DcaSettings의 월 판단은 `UtcNow.AddHours(9)`(KST 고정)라 **기준이 다르다**.
- **이 엔진은 "호출될 때마다" 산다.** 2026-08-01부터 실계좌이므로 과매수를 막는 것은 전적으로 `DailyExecutionService`의 월 1회 멱등 가드다(크론은 매일 부르고, 월 1일로 바꿀 필요는 없다). 이 엔진을 다른 경로에서 새로 호출하면 그 가드를 우회한다 — 새 호출부를 만들지 말 것.

### 유지보수 진입점
- **매수 계획 규칙(무엇을 거를지)**: `PlanPurchases`. 바꾸면 반드시 `DcaAccumulationEngineTests` 갱신.
- **예산 초과 시 동작 바꾸기**(예: 감산): `AccumulateAsync`의 예산 블록 — 단 **판단 재도입 금지 원칙**과 충돌하는지 먼저 검토.
- **주문·기록 필드**: 주문 실행 `foreach` 블록 / `TradeHistoryDto` 구성부.

### `PlanPurchases` 정독 (순수 함수)
- `plan` 빈 맵, `totalCostKrw=0` 초기화.
- `quantities` 순회: 수량 ≤ 0 → `continue`. 현재가 없음 또는 ≤ 0 → `continue`.
- 통과분: `plan[티커]=수량`, `totalCostKrw += 수량 × 현재가 × 환율` 누적.
- **외부 호출 0, 시간 의존 0** → 결정적. 이게 검증 가능성의 핵심.

## 정리 / 결론
- 이 엔진은 판단이 없는 **실행기**다. 순수 함수 `PlanPurchases`(계산)와 부수효과 `AccumulateAsync`(주문·기록)를 분리해 실주문 없이 계산을 검증할 수 있게 했다.
- 예산은 수량을 줄이지 않는 **상한 경고**이며, 종목 단위 실패는 전체 사이클을 멈추지 않고 부분 체결을 허용한다.
- `AccumulateAsync`는 정적 DAO·브로커 I/O 의존이라 순수 단위 테스트가 어렵다. 검증은 `PlanPurchases` 단위 테스트 + `IS_PAPER_TRADING`(Sim) 모드로 한다.

## 참고
- 적립 배분 원칙·판단 레이어 금지: `.agents/rules/architecture.md`, `.agents/rules/recommended_rules.md`
- `Documents/modules/[2026-07-04] 04_DcaSettings.md` — 입력 생성
- `Documents/modules/[2026-07-13] 01_DailyExecutionService.md` — 호출·멱등 가드
- `Documents/modules/[2026-07-04] 05_SimBrokerClient.md` — Sim 검증
- `Tests/DcaAccumulationEngineTests.cs` — `PlanPurchases` 단위 테스트 7건
