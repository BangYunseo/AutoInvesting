---
description: 커밋 전 보안(시크릿/개인정보) 및 커밋 컨벤션 점검
allowed-tools: Bash(git status:*), Bash(git diff:*), Read, Grep
---

현재 스테이징/변경 내용에 대해 커밋 전 점검을 수행하세요.

## 1. 보안 점검 (`.agents/rules/security.md` 기준)
- `git diff --cached`와 변경 파일에서 다음이 포함됐는지 확인:
  - AppKey / AppSecret / 계좌번호 / Access Token 등 시크릿
  - 주민번호·휴대폰번호·계정·암호 등 개인정보 (조직 보안정책)
  - `appsettings.local.json`, `*.secrets.json`, `*.key`, `*.env`, `*.db` 등 시크릿 파일의 스테이징 여부
- 하나라도 발견되면 **커밋을 중단하고** 해당 위치와 제거 방법을 보고하세요.

## 2. 커밋 컨벤션 점검 (`.agents/rules/git-conventions.md` 기준)
- 형식 `<type>: <subject>` (scope 금지), 한국어, 제목 50자 이내
- 변경 범위가 여러 기능이면 기능 단위로 분리 커밋을 제안

## 3. 결과 보고
- 이상 없으면 권장 커밋 메시지(한국어, 적절한 type)를 제안하세요.
- 이상 있으면 항목별로 무엇을, 어디서, 어떻게 고칠지 안내하세요.
