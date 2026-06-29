# AutoInvesting — Claude Code 작업 지침 (CLAUDE.md)

> 해외 ETF 자동 **적립(DCA)** 투자 시스템 · ASP.NET Core Web API (.NET 8.0, C#) + React SPA
> 외부 크론잡이 적립 사이클을 호출하는 Headless 서비스 · 증권사: 한국투자증권(KIS)
> 현재 **Phase 6 (DCA 적립)** — 백테스트로 가치 없음이 확인된 판단 레이어(퀀트/AI)를 제거하고 기계적 적립으로 전환 완료.

---

## ⚠️ 멀티 하네스 동기화 규칙 (가장 먼저 읽을 것)

이 프로젝트는 **Claude Code**와 **Antigravity** 두 에이전트 하네스를 **동시에** 지원합니다.
어느 쪽도 삭제하지 않으며, 한쪽을 바꾸면 다른 쪽도 동일하게 유지합니다.

- 공유 지식의 **단일 진실 원천(SSOT)** 은 `.agents/rules/*.md` 입니다.
- `CLAUDE.md`는 그 파일들을 `@import`로 재사용하므로, **규칙을 고칠 때는 `.agents/rules/`만 수정**하면 Claude Code(임포트)와 Antigravity(`trigger: always_on`) 양쪽에 자동 반영됩니다. 같은 내용을 두 번 쓰지 마세요.
- 에이전트·명령·설정처럼 도구별 포맷이 다른 구성요소는 **`.agents/rules/harness-sync.md`의 동기화 절차**를 반드시 따라 양쪽을 함께 갱신합니다.

---

## 자동 로딩 컨텍스트 (SSOT 임포트 — 수정 금지, 원본은 `.agents/rules/`)

@.agents/rules/harness-sync.md
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
| 신규 로직 검증 | `appsettings.json`의 `IS_PAPER_TRADING="1"`로 **SimBroker 모드 우선 검증** |
| 배분 로직 검증 | `DcaAccumulationEngine.PlanPurchases`(순수 함수)를 입력/기대출력 시나리오로 단위 검증 |
| 적립 사이클 트리거 | `POST /api/order/dca-run` (헤더 `x-api-key`, 202 즉시 반환 후 백그라운드 처리) |
| 프론트엔드 | `Frontend/` → `npm install` / `npm run dev` |

---

## 커밋 규칙 (요약 — 상세는 `git-conventions.md`)

- 형식: `<type>: <subject>` (scope 금지), 제목 50자 이내, **한국어**로 작성
- 기능 단위로 끊어서 커밋 (한 번에 전체 커밋 금지)
- 커밋 전 `security.md`의 시크릿/개인정보 체크포인트 확인

---

## 서브에이전트 (위임)

`persona.md`의 리드+서브 에이전트 구조를 Claude Code 서브에이전트로 구현해 `.claude/agents/`에 둡니다.
각 에이전트는 `persona.md`와 **동일 내용으로 동기화**되어야 합니다 (`harness-sync.md` 절차 준수).

| 에이전트 | 담당 |
|---------|------|
| `core-developer` | DcaAccumulationEngine, DailyExecutionService, 세션 관리 |
| `data-developer` | DTO/DAO/DBManager, PostgreSQL(Npgsql) |
| `api-developer` | Controllers, React 연동, Polly/알림 |
| `kis-integration` | KisBrokerClient, TokenManager, KIS 연동 |
| `quant-analyst` | DcaAccumulationEngine 배분 로직, 매수 수량 설계, 백테스트 검증 |
