---
title: DcaSettings 모듈 노트
date: 2026-07-04
company: [개인]
tags: [dca, 적립설정, 매수템플릿, 월배정]
status: done
---

# DcaSettings 모듈 노트

## 개요
> "이번 달에 **무엇을 몇 주** 살지"를 정해주는 설정 창구. 매수 실행(엔진)과 분리된 **읽기/쓰기 단일 지점**이다. (`Core/DcaSettings.cs`)

## 배경 / 목적
Phase 3 · 2순위 모듈에 대한 이해 문서다. 이 모듈의 역할·결정 로직·함정만 기록한다(행 번호 앵커는 곧 썩으므로 두지 않는다).

## 본문

### 핵심 개념 — 템플릿 + 월배정
- **매수 템플릿(`DcaTemplate`)**: 이름 붙은 "예산 + 종목별 고정 수량" 묶음. 예: `{ Id:"core", Name:"코어", BudgetKrw:100만, Quantities:{SPLG:3, QQQM:2} }`.
- **월배정(`monthMap`)**: 1~12월 각각에 어떤 템플릿을 쓸지 지정. 예: `{ "1":"core", "7":"gold" }`.
- 적립 사이클은 **현재(KST) 월에 배정된 템플릿의 고정 수량을 그대로** 산다. (비중·타이밍 판단 없음)

### 입력 → 처리 → 출력·부작용
- **입력**: 없음(진입점 `Load()`는 인자 없이 현재 월 기준). 저장은 `SaveTemplates(templates)` / `SaveMonthMap(monthMap)`으로 나뉘어 있다(`SaveConfig` 단일 메서드는 없다).
- **처리**: DB에서 템플릿·월배정 JSON을 읽어 현재 월 템플릿 선택.
- **출력**: `Load()` → `(종목별 수량 Dictionary, 예산)`. 살 게 없으면 `(빈 맵, 0)`.
- **부작용**: `SaveTemplates`/`SaveMonthMap`/`SaveRunDay`는 DB(`TB_APP_CONFIG`)에 write. `Load`/`LoadTemplates`/`LoadMonthMap`/`LoadRunDay`는 read.
- **적립 지정일도 이 모듈이 소유한다**: `DCA_RUN_DAY`(`1`~`MaxRunDay`=31, `0`=미설정)를 `LoadRunDay`/`SaveRunDay`로 읽고 쓴다. 그 날 이전이면 크론 호출이 매수 없이 반환되며, 판정은 `DailyExecutionService.IsOnOrAfterRunDay`(순수 함수)가 한다.

### 이 코드가 내리는 결정

#### 템플릿을 어디서 읽나 (`LoadTemplates`)
우선순위:

```text
① DB의 DCA_TEMPLATES(JSON)  →  ② 레거시 단일설정 DCA_QTYS/DCA_BUDGET_KRW  →  ③ appsettings.json > Dca
```

- 레거시 설정만 있으면 자동으로 "기본" 템플릿 1개로 이관해서 읽음(하위 호환).

#### 이번 달 어떤 템플릿을 쓰나 (`SelectTemplate`, 순수 함수)
- 이번 달이 월배정에 있으면 → 그 Id의 템플릿 (Id가 목록에 없으면 `null` → **매수 스킵**)
- 월배정이 아예 비어 있으면 → **첫 템플릿을 매월 사용** (기존 단일 설정 동작 유지)
- 월배정은 있는데 이번 달 배정이 없으면 → `null` → **매수 스킵**

### 헷갈리기 쉬운 지점 / 함정
- **`SelectTemplate`은 외부 I/O 없는 순수 함수**라 단위 테스트 대상(`Tests/DcaSettingsTests.cs`). 규칙상 "판단 계산은 순수 함수로" 원칙의 실제 사례.
- **비중(%)은 저장하지 않습니다.** 수량×현재가로 화면에서 환산해 보여줄 뿐, 입력이 아님.
- **예산(BudgetKrw)은 상한 경고용**이지 수량을 줄이지 않음. 실제 감산 로직 없음(엔진이 초과를 결과에 담고, 호출부가 보고 메일에 싣는다).
- 월(KST) 기준 = `DateTime.UtcNow.AddHours(9)`. 서버 시간대와 무관하게 항상 한국 시간 월.
- `SaveTemplates`/`SaveMonthMap`은 저장 전에 정제: Id 없는 템플릿 제거, 수량 0 이하 제거, 티커 대문자화, 존재하지 않는 템플릿 Id를 가리키는 월배정 제거.

### 당신이 만질 일이 생기면 여기
- **살 종목/수량 바꾸기**: 런타임은 프론트 "적립 설정"(→ `DcaController` → `SaveTemplates`)에서. 코드 기본 시드는 `appsettings.json > Dca`.
- **월별로 다른 템플릿 쓰기**: `monthMap` 채우기(프론트에서).
- **기본 예산 상수**: `DefaultBudgetKrw`(100만원).

## 정리 / 결론
- 살 종목/수량·월배정·지정일 변경은 프론트 "적립 설정"·"주문·적립" → `DcaController` → `SaveTemplates`/`SaveMonthMap`/`SaveRunDay` 경로로 이뤄지며, 코드 기본 시드는 `appsettings.json > Dca`다.
- `SelectTemplate`은 순수 함수라 단위 검증이 쉽고(`Tests/DcaSettingsTests.cs`), 비중은 표시용, 예산은 상한 경고용이라는 경계가 이 모듈의 핵심 설계다.

## 참고
- 설정 값의 출처/우선순위: `Documents/reference/CONFIG_REFERENCE.md`
