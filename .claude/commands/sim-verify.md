---
description: SimBroker(모의) 모드로 신규/변경 로직 검증
allowed-tools: Read, Grep, Glob, Bash(dotnet build:*), Bash(dotnet run:*)
---

신규/변경한 매매·퀀트 로직을 실거래 전에 안전하게 검증하세요. (`.agents/rules/recommended_rules.md`의 테스트 규칙 기준)

## 절차
1. `appsettings.json`에서 `IS_PAPER_TRADING` 값을 확인합니다. 실거래(`"0"`)면 검증 중에는 `"1"`(SimBroker)로 둘 것을 안내하세요. **값을 임의로 영구 변경하지 말고 사용자에게 확인받으세요.**
2. `dotnet build`로 컴파일을 확인합니다.
3. 변경 대상이 퀀트 판단 로직이면 `BacktestEngine`을 통해 과거 데이터셋으로 의도된 매매가 일어나는지 회귀 확인 방법을 제시/실행하세요.
4. `SimBrokerClient` 경로로 동작 시 로그(`Logger.LogQuant()` 등)에서 판단 근거가 올바르게 남는지 확인하세요.

## 검증 대상 (인자: $ARGUMENTS)
$ARGUMENTS 에 지정된 종목/전략/기능이 있으면 그 범위에 집중하세요. 없으면 최근 변경분(`git diff`) 기준으로 검증 대상을 추론하세요.

## 보고
- 검증 통과/실패, 실패 시 원인과 수정 제안을 요약하세요.
- 실거래 전환 전 체크리스트(임계값·수량·계좌)를 마지막에 안내하세요.
