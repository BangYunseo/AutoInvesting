---
title: AutoInvesting — Claude Code 작업 지침
date: 2026-09-02
company: [개인]
tags: [작업지침, DCA적립, 규칙SSOT, 아키텍처]
status: active
---

# Claude Code 작업 지침

## 규칙 파일

- 세션 시작 시 **전문이 컨텍스트에 로딩**됨
- CLAUDE.md는 로딩 목록
- 규칙은 `.agents/rules/`의 해당 파일에서만 수정
- CLAUDE.md 본문에 작성 금지

@.agents/rules/project_overview.md  
@.agents/rules/architecture.md  
@.agents/rules/code-style-guide.md  
@.agents/rules/security.md  
@.agents/rules/git-conventions.md  
@.agents/rules/kis-api-guide.md  
@.agents/rules/recommended_rules.md  
@.agents/rules/persona.md  
@.agents/rules/worklog.md

## 빌드 · 실행 · 검증

| 작업               | 명령 / 방법                                                                                                              |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------ |
| 빌드               | `dotnet build`                                                                                                           |
| 실행               | `dotnet run` (ASP.NET Core 호스트)                                                                                       |
| 신규 로직 검증     | `appsettings.json`의 `Trading:IsPaperTrading: true`(또는 환경변수 `IS_PAPER_TRADING="1"`)로 **SimBroker 모드 우선 검증** |
| 배분 로직 검증     | `DcaAccumulationEngine.PlanPurchases`(순수 함수)를 입력/기대출력 시나리오로 단위 검증                                    |
| 적립 사이클 트리거 | `POST /api/order/dca-run` (헤더 `x-api-key`, 202 즉시 반환 후 백그라운드 처리)                                           |
| 프론트엔드         | `Frontend/` → `npm install` / `npm run dev`                                                                              |

---

## 서브 에이전트 (위임)

`.claude/agents/`의 `core-developer`·`data-developer`·`api-developer`·`kis-integration`·`quant-analyst`가 `persona.md`의 서브 에이전트 5역할에 1:1로 대응합니다 (변경 시 두 곳을 같은 커밋에서 함께 수정 — 위 "규칙 SSOT" 참조).
