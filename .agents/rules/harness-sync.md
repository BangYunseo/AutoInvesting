---
trigger: always_on
---

# 하네스 동기화 규칙 (Claude Code ↔ Antigravity)

이 프로젝트는 **Claude Code**와 **Antigravity** 두 에이전트 하네스를 병행 지원한다.
**어느 쪽도 삭제하지 않으며**, 구성요소를 변경할 때는 양쪽을 항상 동일하게 유지한다.
(이 파일은 Antigravity에서는 `trigger: always_on`으로, Claude Code에서는 `CLAUDE.md`의 `@import`로 로딩된다.)

## 단일 진실 원천(SSOT) 매핑

| 카테고리 | SSOT(원본 1곳) | Claude Code 연결 | Antigravity 연결 | 동기화 |
|---------|----------------|------------------|------------------|--------|
| 규칙(rules) | `.agents/rules/*.md` | `CLAUDE.md`의 `@import` | `trigger: always_on` 자동 로딩 | **자동** — 1곳만 수정 |
| 에이전트 역할 | `.agents/rules/persona.md` (역할·책임·위임 명세) | `.claude/agents/<role>.md` (실행 정의 + 도구 권한) | `persona.md` 오케스트레이션 | **수동 미러** |
| 슬래시 명령/워크플로우 | `.claude/commands/*.md` | 네이티브 슬래시 명령 | (Antigravity 워크플로우로 대응 문서화) | **수동 미러** |
| 권한·환경·훅 | `.claude/settings.json` | 네이티브 | (대응 없음 — Claude Code 전용) | — |

## 변경 절차 (MUST)

1. **규칙 내용 변경**: `.agents/rules/`의 해당 파일만 수정한다. CLAUDE.md/Antigravity 양쪽에 자동 반영되므로 복사하지 않는다.
2. **에이전트 역할/책임 변경**: `.agents/rules/persona.md`와 `.claude/agents/<role>.md`를 **같은 커밋에서 동일한 내용으로 함께 수정**한다.
3. **신규 에이전트 추가**: `persona.md`의 에이전트 표와 `.claude/agents/`의 파일을 **둘 다** 추가한다.
4. **신규 규칙 파일 추가**: `.agents/rules/`에 만들고, `CLAUDE.md`의 임포트 목록에 한 줄 추가한다 (Antigravity는 `trigger: always_on`이면 자동 인식).
5. **불일치 상태로 커밋 금지**: 한쪽만 바뀐 채로 커밋하지 않는다.

## 변경 후 점검 체크리스트

- [ ] 규칙 변경 시 `.agents/rules/`만 고쳤는가? (CLAUDE.md 본문에 규칙 내용을 복붙하지 않았는가?)
- [ ] 에이전트 수정 시 `persona.md` 표와 `.claude/agents/`가 일치하는가?
- [ ] 새 규칙 파일을 만들었다면 `CLAUDE.md` 임포트 목록에 추가했는가?
- [ ] 양쪽 변경을 한 커밋에 담았는가?
