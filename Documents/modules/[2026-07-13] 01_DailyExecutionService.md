---
title: 모듈 노트 — DailyExecutionService (Core)
date: 2026-07-13
company: [개인]
tags: [DCA, 적립사이클, 멱등가드, 모듈노트]
status: done
---

# 모듈 노트 — DailyExecutionService (Core)

## 개요
> `Core/DailyExecutionService.cs` — 외부 크론이 부르는 적립 사이클의 진입점. "이번 달 이미 샀나?"를 먼저 확인하고, 안 샀으면 로그인→설정로드→엔진 실행→접수되면 "이번 달 샀음" 도장→보고 메일까지 오케스트레이션한다. 장 마감 후 그 도장을 **해제**하는 `ReconcileAsync`도 이 클래스에 있다.

## 배경 / 목적
- 파일: `Core/DailyExecutionService.cs` · 5순위
- 위험도 **2(중)** — 실주문을 직접 하진 않으나, 적립 사이클의 진입점이자 "월 1회만 산다"는 멱등 가드의 주인이다.
- 이 노트는 오너 관점 요약(역할·결정·함정)만 담는다. 행 번호 앵커는 곧 썩으므로 두지 않는다.

## 본문

### 한 문장 역할
외부 크론이 부르는 **적립 사이클의 진입점**. "이번 달 이미 샀나?"를 먼저 확인하고, 안 샀으면 로그인→설정로드→DcaAccumulationEngine 실행→접수되면 "이번 달 샀음" 도장 찍기→보고 메일까지 오케스트레이션한다. **자기가 판단하거나 주문하지 않는다** — 엔진에 위임한다.

### 입력 → 처리 → 출력·부작용 (`RunDcaCycleAsync`)
- **입력**: 없음(파라미터 0). 크론이 `POST /api/order/dca-run`으로 호출 → `OrderController`가 이 메서드를 부른다.
- **처리**: 멱등 가드 확인 → (스킵 아니면) 로그인 → DcaSettings의 `Load()`로 이번 달 템플릿의 `(수량, 예산)` 로드 → `AccumulateAsync` 실행.
- **출력**: 사람이 읽는 상태 문자열 한 줄(체결 주수 또는 스킵/오류 사유). 호출한 컨트롤러가 응답에 쓴다.
- **부작용**: ① 접수 ≥ 1건이면 `DCA_LAST_RUN_MONTH`·`DCA_LAST_RUN_DATE`(표시 전용) write, 예약분이면 `DCA_FORCE_RUN_MONTH` 해제 ② 주문 직전 보유 수량 + 접수 주문을 `DCA_PENDING_SNAPSHOT`에 write(대사용) ③ `try` 블록에 진입한 경우 `finally`에서 보고 메일 ④ 로그 다수.
- **두 번째 공개 메서드 `ReconcileAsync`**: 미장 마감 후 크론(`reconcile.yml`)이 부른다. 스냅샷의 "주문 전 수량"과 현재 보유 수량을 비교해 전량 미체결이면 `DCA_LAST_RUN_MONTH`를 **해제**해 재시도를 허용한다 — 이 마커는 쓰기 전용이 아니다. 판정이 잔고 조회에 전적으로 의존하므로, 조회가 조용히 빈 목록을 주면 이미 체결된 달을 다시 연다. 그래서 `KisBrokerClient.GetHoldingsAsync`는 2026-08-07부터 `rt_cd != "0"`이면 던진다(HTTP 200 + 업무 오류를 성공으로 읽던 구멍).

### 이 코드가 내리는 결정 (평문)
- **가드 두 키는 `AppConfigManager.TryReadDb`로 DB만 읽고, 조회 실패면 매수하지 않는다**(fail-closed, 2026-08-07). `Get`은 "조회 실패"와 "값 없음"을 같은 기본값으로 뭉개고 환경변수를 DB보다 먼저 집으므로, Neon 콜드 스타트 한 번이나 동명 환경변수 하나로 가드가 뚫려 같은 달에 또 매수한다.
- **지정일 전이면 크론 호출을 흘려보낸다**: `DcaSettings.LoadRunDay()`의 `DCA_RUN_DAY` 이전이면 매수 없이 반환(`IsOnOrAfterRunDay`, 순수 함수).
- **이번 달 이미 샀으면 즉시 스킵**: `DCA_LAST_RUN_MONTH`(KST "yyyy-MM")가 이번 달과 같으면 로그인도 안 하고 반환. 단 `force`(사람이 화면에서 누름)와 `DCA_FORCE_RUN_MONTH` 예약은 이 가드를 한 번 넘는다.
- **못 사는 상황이면 조용히 스킵**(오류 아님): 로그인 실패 / 설정 수량 0개 → 매수 없이 반환.
- **"샀음" 도장은 접수 ≥ 1건일 때 찍는다**(체결 아님). 접수 0건이면 마커를 안 남겨 다음 날 재시도하고, 접수됐지만 안 붙은 달은 `ReconcileAsync`가 마감 후 해제한다.
- **메일은 `try` 블록에 진입한 경우에만 보낸다**: 지정일 미도래·당월 적립 완료 스킵은 `finally` 앞에서 반환하므로 메일이 없다. DB 조회 실패 경로만 예외적으로 직접 발송한다.

### 멱등 가드가 핵심인 이유 (⚠️ 실거래 전환)
- 현재 크론은 **매일**(GitHub Actions, KST 00:10 = UTC 15:10) 부른다. 이 가드가 없으면 매일 한 달치를 사서 ~30배 과매수가 된다.
- **2026-08-01부터 실계좌**다. 크론을 매월 1일로 옮길 필요는 없다 — 월초부터 시도해 처음 성공하는 날 1회만 적립되는 것이 의도된 설계다(`recommended_rules.md` "실거래 전환").
- 마커를 **거래이력이 아니라 전용 키**(`DCA_LAST_RUN_MONTH`)로 쓰는 이유: 사용자가 수동으로 1주 산 걸 "이번 달 적립 완료"로 오판하지 않기 위함.

### 헷갈리기 쉬운 지점 / 함정
- **월 판단 기준이 두 곳 모두 KST**로 일치한다: 이 서비스의 `CurrentKstMonth()`(`UtcNow.AddHours(9)`)와 DcaSettings의 `KstNow()`가 같은 방식. (반면 DcaAccumulationEngine의 `TradeDate=DateTime.Now`는 서버 로컬시각이라 기준이 다르다.)
- **DI 수명**: `SessionManager`를 생성자로 주입받는다. `RunDcaCycleAsync`는 `IServiceScopeFactory`로 스코프를 열어 호출되는 게 정상 패턴(호출부 = `OrderController`).
- **반환 문자열은 UI/응답용**일 뿐, 흐름 제어에 쓰이지 않는다. 스킵/오류/성공을 문자열로 구분해 담는다.
- **엔진은 `new DcaAccumulationEngine(client)`로 직접 생성**한다(DI 아님). 클라이언트만 세션에서 얻어 넘긴다.

### 당신이 만질 일이 생기면 여기
- **"월 1회" 규칙 바꾸기**(예: 분기 1회, 매주): 멱등 가드 블록 + 마커 키 형식(`CurrentKstMonth`). 바꾸면 실거래 과매수 위험과 직결되니 반드시 확인 후.
- **스킵 사유·메일 문구**: `statusNote` 문자열들 / `SendDcaReportAsync`.
- **무엇을 살지·수량**: 여기가 아니라 DcaSettings(템플릿·월배정)와 DcaAccumulationEngine(집행).

## 정리 / 결론
- 이 서비스는 판단하지 않는 오케스트레이터다 — 무엇을 살지는 DcaSettings, 어떻게 살지는 DcaAccumulationEngine이 맡고, 여기가 지키는 것은 "같은 달에 두 번 사지 않는다" 하나다.
- 그 가드는 두 방향 모두 fail-closed다: 마커를 **읽지 못하면** 매수하지 않고, 체결을 **확인하지 못하면**(수량 감소·부분 체결) 마커를 되돌리지 않는다.
- 정적 DAO·브로커 I/O 의존으로 순수 단위 테스트가 어렵다. 가드 분기 검증은 `IsOnOrAfterRunDay` 단위 테스트(`Tests/DcaRunDayTests.cs`)와 `IS_PAPER_TRADING`(Sim) 모드로 한다.

## 참고
- 이번 달 수량·예산 결정: `Documents/modules/[2026-07-04] 04_DcaSettings.md`
- 집행(매수 계획·주문): `Documents/modules/[2026-07-04] 03_DcaAccumulationEngine.md`
- Sim 검증: `Documents/modules/[2026-07-04] 05_SimBrokerClient.md`
- 멱등 마커 저장소·설정 키 출처: `Documents/reference/CONFIG_REFERENCE.md`
