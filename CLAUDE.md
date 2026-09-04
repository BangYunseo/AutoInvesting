---
title: AutoInvesting
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

| 작업       | 명령 / 방법                                                                                                                          |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| 빌드       | 저장소 루트에서 `dotnet build` (오류 0 확인)                                                                                         |
| 테스트     | 저장소 루트에서 `dotnet test` — `AutoInvest.sln`이 웹·테스트 두 프로젝트를 함께 빌드하고 xUnit을 돌린다 (`Tests/`는 웹 빌드에서 제외) |
| 실행       | `dotnet run` — **선행조건: `MASTER_KEY`(base64 32바이트)가 환경변수 또는 `appsettings.local.json`에 있어야 한다. 없으면 기동 즉시 `[FTL]`로 중단된다.** 기본 리스닝 `http://localhost:5000` (`launchSettings.json` 없음 — Kestrel 기본값) |
| 프론트엔드 | `Frontend/` → `npm install` / `npm run dev` — dev 서버가 `/api`를 `http://localhost:5000`으로 프록시하므로 **백엔드를 먼저 띄운다**   |

> 로직 검증 절차(배분 로직 단위 검증, SimBroker 사이클 확인)는 `/sim-verify` 스킬과
> `.agents/rules/recommended_rules.md`(테스트 규칙)에 있다 — 여기에 다시 쓰지 않는다.
> 적립 사이클 트리거(`POST /api/order/dca-run`)·크론 구성은 `.agents/rules/architecture.md`와
> `recommended_rules.md`(실거래 전환)에 있다.

## 서브 에이전트 (위임)

`.claude/agents/`의 `core-developer`·`data-developer`·`api-developer`·`kis-integration`·`quant-analyst`가 `persona.md`의 서브 에이전트 5역할에 1:1로 대응합니다 (변경 시 두 곳을 같은 커밋에서 함께 수정 — 위 "규칙 SSOT" 참조).
