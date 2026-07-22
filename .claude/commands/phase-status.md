---
description: 현재 Phase 진척도 확인 및 project_overview.md 갱신
allowed-tools: Read, Edit, Grep, Glob, Bash(git log:*)
---

프로젝트의 Phase 진척 상태를 점검하고 문서를 동기화하세요.

## 절차
1. `.agents/rules/project_overview.md`의 "Phase 진행 상태" 표를 읽습니다.
2. 최근 커밋(`git log --oneline -30`)과 `Documents/DEVELOPMENT.md`를 비교해 실제 진척과 표가 일치하는지 확인합니다.
3. 불일치가 있으면 `project_overview.md`의 Phase 표를 실제 상태로 갱신합니다. (이 파일은 SSOT이므로 여기만 고치면 `CLAUDE.md` 임포트에 자동 반영됨)
4. 다음 우선 작업(예: Phase 4-e, Phase 5)을 요약해 제시합니다.

## 주의
- 규칙 내용을 `CLAUDE.md`에 복붙하지 말고 `project_overview.md`만 수정하세요.
- 변경 시 `docs:` 타입의 한국어 커밋 메시지를 제안하세요.
