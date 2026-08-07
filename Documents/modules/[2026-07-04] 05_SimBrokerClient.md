---
title: 모듈 노트 — SimBrokerClient (Core)
date: 2026-07-04
company: [개인]
tags: [SimBrokerClient, 모의투자, IBrokerClient, 모듈노트]
status: done
---

# 모듈 노트 — SimBrokerClient (Core)

## 개요
> KIS API 키 없이도 전체 적립 사이클을 돌려볼 수 있게, 증권사인 척하는 가짜 브로커다. `IBrokerClient`를 구현해 실제 `KisBrokerClient`와 완전히 교체 가능하다.

## 배경 / 목적
- 파일: `Core/SimBrokerClient.cs` · Phase 3 · 3순위
- 작성일: 2026-07-04 · 위험도 1(최저) — 실계좌·실주문 없음

오너 관점에서 이 모듈이 무엇을 하고, 어떤 결정을 내리며, 어디를 만지면 되는지 정리한 노트다(행 번호 앵커는 곧 썩으므로 두지 않는다).

## 본문

### 실전과의 분기
- **`KIS_APP_KEY`가 비어 있으면** SessionManager가 모드와 무관하게 `KisBrokerClient` 대신 이 클래스를 주입한다. `IS_PAPER_TRADING`은 Sim/KIS 선택이 아니라 KIS 접속망 prod(`:9443`)/vps(`:29443`)만 고르므로, 키가 있는데 `1`이면 이 파일이 아니라 실제 KIS 모의계좌로 간다.
- 엔진·사이클·컨트롤러는 자기가 진짜 브로커를 쓰는지 가짜를 쓰는지 모른다(인터페이스 뒤에 숨겨짐).

### 입출력과 부작용

| 메서드 | 입력 | 처리 | 출력 | 부작용 |
|---|---|---|---|---|
| `LoginAsync()` | — | 플래그만 true로 | `true` | `_isLoggedIn` 세팅, 로그 |
| `GetCurrentPriceAsync(ticker)` | 티커 | 기준가 표 조회 | 기준가(USD) | 로그 |
| `GetExchangeRateAsync()` | — | — | 상수 1350 KRW | 로그 |
| `GetHoldingsAsync()` | — | 가상 잔고 → DTO 변환(평가손익 계산) | `List<HoldingDto>` | 로그 |
| `GetCashBalanceAsync()` | — | — | 상수 $10,000 | 로그 |
| `PlaceBuyOrderAsync(ticker,qty,price)` | 종목·수량·가격 | 가상 잔고에 가산(평단 재계산) | 가짜 주문번호 | `_holdings` 변경, 로그 |
| `PlaceSellOrderAsync(ticker,qty,price)` | 〃 | 가상 잔고에서 차감(0↓이면 제거) | 가짜 주문번호 | `_holdings` 변경, 로그 |

- 모든 메서드가 `Task.FromResult`로 즉시 완료 — 실제 네트워크 I/O 없음. `async` 키워드도 없다(이미 완료된 Task 반환).
- 상태는 프로세스 메모리에만 존재(`_holdings`, `_isLoggedIn`). 앱 재시작 시 잔고 초기화, DB 저장 안 함.

### 코드가 내리는 결정
- 현재가: 종목별 고정 기준가를 그대로 돌려준다. `SCHD 32.39 / QQQM 293.42 / GLD 378.13 / JEPI 56.71 / SPLG 80.00`(2026-07 기준 수동 갱신 스냅샷 — 실시간 시세 아님), 표에 없는 티커는 일괄 $100.
- 매수 시 평단: 기존 보유가 있으면 `(기존평단×기존수량 + 체결가×수량) / 총수량`을 소수 2자리 반올림. 없으면 체결가가 곧 평단.
- 매도 시: 수량을 빼고, 남은 수량이 0 이하면 종목을 잔고에서 완전 제거. 평단은 유지.
- 주문번호: `Guid`에서 앞 12자리 대문자(형식만 흉내, 의미 없음). 매수/매도 항상 "체결 성공"으로 처리(실패 시나리오 없음).

### 함정과 주의점
- 환율·예수금은 상수다(1350, $10,000). 실제 환율(ExchangeRateService)이나 실제 예수금이 아니다. 사이클 계산이 "동작하는지"만 보는 용도.
- 가상 잔고는 이 인스턴스가 살아있는 동안만 유지된다. SessionManager가 브로커 인스턴스를 어떻게 관리하느냐(싱글턴/스코프)에 따라 잔고 지속 범위가 달라짐 → SessionManager 볼 때 확인.
- 매도는 보유 없어도 예외 없이 주문번호를 반환한다(가상 잔고엔 변화 없음). 실전 KIS라면 거부될 상황이 시뮬에선 조용히 성공.

### 수정 진입점
- 시뮬 종목/기준가 추가·조정: `_basePrices` 딕셔너리.
- 시뮬 예수금/환율 바꾸기: `GetCashBalanceAsync`(상수 10000), `GetExchangeRateAsync`(상수 1350).
- 더 현실적인 시뮬(가격 변동·주문 실패)이 필요하면: 여기지만, 엔진 검증 목적상 결정적(deterministic) 동작이 유리하므로 신중히.

### 안전망 (적용 완료)
- `Tests/SimBrokerClientTests.cs`(10건 — 가중평균 평단·연속 매수 누적·전량 매도 제거·미등록 티커 $100 폴백·상수 확인)이 이 시뮬의 결정적 동작을 못박아 둔다. 기준가·상수를 바꾸면 이 테스트가 먼저 깨진다.

## 정리 / 결론
- 이 모듈은 `KIS_APP_KEY`가 비었을 때 주입되는 결정적(deterministic) 가짜 브로커로, 실계좌·실주문이 없어 위험도 최저다.
- 현재가·환율·예수금은 모두 상수/고정 기준가이며, 가상 잔고는 메모리에만 유지된다.

## 참고
- `_rng` 삭제 등 죽은 코드 정리 경위: `Documents/reference/DEVELOPMENT.md`
- SessionManager — Sim/실전 분기 (모듈 문서 미작성)
- ExchangeRateService — 실제 환율 서비스 (모듈 문서 미작성)
