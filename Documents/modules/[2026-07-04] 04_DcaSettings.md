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
Phase 3 · 2순위 모듈에 대한 이해 문서다. 코드 변경 없이(2026-07-04 기준) 모듈의 역할·결정 로직·정독 내용을 기록한다. 이 모듈은 이미 Phase 6 DCA 원칙(고정수량·순수함수 분리)에 맞게 정리돼 있어, 코드 수정 없이 이해 문서만 작성했다.

## 본문

### 핵심 개념 — 템플릿 + 월배정
- **매수 템플릿(`DcaTemplate`)**: 이름 붙은 "예산 + 종목별 고정 수량" 묶음. 예: `{ Id:"core", Name:"코어", BudgetKrw:100만, Quantities:{SPLG:3, QQQM:2} }`.
- **월배정(`monthMap`)**: 1~12월 각각에 어떤 템플릿을 쓸지 지정. 예: `{ "1":"core", "7":"gold" }`.
- 적립 사이클은 **현재(KST) 월에 배정된 템플릿의 고정 수량을 그대로** 산다. (비중·타이밍 판단 없음)

### 입력 → 처리 → 출력·부작용
- **입력**: 없음(진입점 `Load()`는 인자 없이 현재 월 기준). 저장은 `SaveConfig(templates, monthMap)`.
- **처리**: DB에서 템플릿·월배정 JSON을 읽어 현재 월 템플릿 선택.
- **출력**: `Load()` → `(종목별 수량 Dictionary, 예산)`. 살 게 없으면 `(빈 맵, 0)`.
- **부작용**: `SaveConfig`는 DB(`TB_APP_CONFIG`)에 write. `Load`/`LoadTemplates`는 read.

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
- **예산(BudgetKrw)은 상한 경고용**이지 수량을 줄이지 않음. 실제 감산 로직 없음(엔진에서 초과 시 경고 메일만).
- 월(KST) 기준 = `DateTime.UtcNow.AddHours(9)`. 서버 시간대와 무관하게 항상 한국 시간 월.
- `SaveConfig`는 저장 전에 정제: Id 없는 템플릿 제거, 수량 0 이하 제거, 티커 대문자화, 존재하지 않는 템플릿 Id를 가리키는 월배정 제거.

### 당신이 만질 일이 생기면 여기
- **살 종목/수량 바꾸기**: 런타임은 프론트 "적립 설정"(→ `DcaController` → `SaveConfig`)에서. 코드 기본 시드는 `appsettings.json > Dca`.
- **월별로 다른 템플릿 쓰기**: `monthMap` 채우기(프론트에서).
- **기본 예산 상수**: `DefaultBudgetKrw`(100만원).

### 코드 정독 (라인 바이 라인)

#### `Load()` — 엔진 진입점 (43~64행)
- `LoadTemplates()` + `LoadMonthMap()` + `KstNow().Month`로 재료 수집.
- `SelectTemplate(...)` → 선택된 템플릿 `chosen`.
- `chosen == null`이면 경고 로그 + `(빈 맵, 0)` 반환 → 호출부(엔진)가 매수 스킵.
- 아니면 수량 맵 구성(값 > 0인 것만), 예산은 템플릿 예산(0 이하면 `DefaultBudgetKrw`).

#### `SelectTemplate(templates, monthMap, month)` — 순수 함수 (77~91)
- 템플릿 목록 비었으면 `null`.
- 월배정에 이번 달 있으면 → 그 Id의 템플릿(`FirstOrDefault`, 없으면 `null`).
- 월배정 전체가 비었으면 → 첫 템플릿.
- 그 외(월배정 있는데 이번 달 없음) → `null`.

#### `LoadTemplates()` (94~125)
- DB `DCA_TEMPLATES` JSON 역직렬화 → Id 있는 것만 필터, 1개↑면 반환.
- 파싱 실패 시 `Logger.Error` + 레거시 폴백.
- 폴백: 레거시 수량/예산을 `{ Id:"default", Name:"기본" }` 템플릿 하나로.

#### `LoadMonthMap()` (128~147)
- DB `DCA_MONTH_MAP` JSON(`{"월":"템플릿Id"}`) 역직렬화 → 1~12 범위 + 값 있는 것만 채택.

#### `SaveConfig(templates, monthMap)` (152~175)
- 템플릿 정제(Id trim, 이름 기본값, 수량 0↓ 제거, 티커 대문자) → `DCA_TEMPLATES`로 저장.
- 월배정은 **존재하는 템플릿 Id만** 남기고 → `DCA_MONTH_MAP`로 저장.

#### 레거시 로더 (`LoadLegacyQuantities`, `LoadLegacyBudget`)
- DB `DCA_QTYS`/`DCA_BUDGET_KRW` → 없으면 `appsettings.json > Dca:Quantities`/`Dca:MonthlyBudgetKrw` → 없으면 상수.

#### `KstNow()`
- `DateTime.UtcNow.AddHours(9)`.

## 정리 / 결론
- 코드 변경 없음. 이 모듈은 이미 Phase 6 DCA 원칙(고정수량·순수함수 분리)에 맞게 정리돼 있어 이해 문서만 작성했다.
- 살 종목/수량·월배정 변경은 프론트 "적립 설정" → `DcaController` → `SaveConfig` 경로로 이뤄지며, 코드 기본 시드는 `appsettings.json > Dca`다.
- `SelectTemplate`은 순수 함수라 단위 검증이 쉽고, 비중은 표시용, 예산은 상한 경고용이라는 경계가 이 모듈의 핵심 설계다.

## 참고
- 설정 값의 출처/우선순위: `Documents/modules/[2026-07-04] 02_AppConfigManager.md`
