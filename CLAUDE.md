---
title: AutoInvesting — Claude Code 작업 지침
date: 2026-07-23
company: [개인]
tags: [작업지침, DCA적립, 규칙SSOT, 아키텍처]
status: draft
---

# AutoInvesting — Claude Code 작업 지침

## 개요
> 해외 ETF 자동 **적립(DCA)** 투자 시스템 · ASP.NET Core Web API (.NET 8.0, C#) + React SPA. 외부 크론잡이 적립 사이클을 호출하는 Headless 서비스이며 증권사는 한국투자증권(KIS)이다. 현재 **Phase 6 (DCA 적립)** — 백테스트로 가치 없음이 확인된 판단 레이어(퀀트/AI)를 제거하고 기계적 적립으로 전환 완료.

## 규칙 SSOT (가장 먼저 읽을 것)

- 공유 지식의 **단일 진실 원천(SSOT)** 은 `.agents/rules/*.md` 입니다. `CLAUDE.md`는 그 파일들을 `@import`로 로딩합니다.
- **규칙을 고칠 때는 `.agents/rules/`의 해당 파일만 수정**하세요 — CLAUDE.md 본문에 같은 내용을 복붙하지 않습니다.
- 에이전트 역할은 `.agents/rules/persona.md`(역할·책임 명세)와 `.claude/agents/<role>.md`(실행 정의)를 **같은 커밋에서 동일 내용으로 함께** 갱신합니다.

---

## 자동 로딩 컨텍스트 (SSOT 임포트 — 수정 금지, 원본은 `.agents/rules/`)

@.agents/rules/project_overview.md
@.agents/rules/architecture.md
@.agents/rules/code-style-guide.md
@.agents/rules/security.md
@.agents/rules/git-conventions.md
@.agents/rules/kis-api-guide.md
@.agents/rules/recommended_rules.md
@.agents/rules/persona.md
@.agents/rules/worklog.md

---

## 🚫 절대 금지 (요약 — 상세 근거는 위 임포트 파일 참조)

| 항목 | 설명 |
|------|------|
| 🚫 시크릿 노출 | API Key/Secret/계좌번호/토큰을 소스·로그·커밋에 포함 금지 → `appsettings.local.json`/환경변수 사용 (`security.md`) |
| 🚫 개인정보·DB접속정보 | 주민번호·휴대폰번호·계정·암호·DB 접속정보를 프롬프트/자료/커밋에 올리지 않음 (조직 보안정책) |
| 🚫 동기 블로킹 I/O | `Task.Wait()` / `.Result` 절대 금지 (교착상태) |
| 🚫 빈 catch 블록 | `catch { }` 금지 → 최소 `Logger.Error()` + 필요시 알림 |
| 🚫 IBrokerClient 우회 | 증권사 API 직접 호출 금지 → 인터페이스를 통해서만 |
| 🚫 레이어 역방향 의존 | Core → Controllers, Data → Core 참조 금지 |
| 🚫 누적 데이터 훼손 | `TB_MARKET_SNAPSHOT`(레거시 누적 데이터) 임의 수정·삭제 금지 (`recommended_rules.md`) |
| 🚫 판단 레이어 재도입 | 신호/임계값/합의 스코어링/AI 분석/적응형 임계값/리밸런싱 재추가 금지 — 백테스트로 가치 없음 확인됨 (`recommended_rules.md`) |

---

## 빌드 · 실행 · 검증

| 작업 | 명령 / 방법 |
|------|------------|
| 빌드 | `dotnet build` |
| 실행 | `dotnet run` (ASP.NET Core 호스트) |
| 신규 로직 검증 | `appsettings.json`의 `Trading:IsPaperTrading: true`(또는 환경변수 `IS_PAPER_TRADING="1"`)로 **SimBroker 모드 우선 검증** |
| 배분 로직 검증 | `DcaAccumulationEngine.PlanPurchases`(순수 함수)를 입력/기대출력 시나리오로 단위 검증 |
| 적립 사이클 트리거 | `POST /api/order/dca-run` (헤더 `x-api-key`, 202 즉시 반환 후 백그라운드 처리) |
| 프론트엔드 | `Frontend/` → `npm install` / `npm run dev` |

---

## 서브에이전트 (위임)

`.claude/agents/`의 `core-developer`·`data-developer`·`api-developer`·`kis-integration`·`quant-analyst`가 `persona.md`의 서브 에이전트 5역할에 1:1로 대응합니다 (변경 시 두 곳을 같은 커밋에서 함께 수정 — 위 "규칙 SSOT" 참조).
