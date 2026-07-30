---
description: SimBroker(모의) 모드로 신규/변경 로직 검증
allowed-tools: Read, Grep, Glob, Bash(dotnet build:*), Bash(dotnet run:*)
---

신규/변경한 적립 매수 로직을 실거래 전에 안전하게 검증하세요. (`.agents/rules/recommended_rules.md`의 테스트 규칙 기준)

## 절차
1. `appsettings.json`에서 `IS_PAPER_TRADING` 값을 확인합니다. 실거래(`"0"`)면 검증 중에는 `"1"`(SimBroker)로 둘 것을 안내하세요. **값을 임의로 영구 변경하지 말고 사용자에게 확인받으세요.**
2. `dotnet build`로 컴파일을 확인합니다.
3. 배분 로직을 건드렸으면 순수 함수 `DcaAccumulationEngine.PlanPurchases`를 입력/기대출력 시나리오로 검증하세요(`dotnet test Tests/AutoInvest.Tests.csproj`). 시나리오: 현재 월 템플릿 선택, 지정 수량 그대로 매수, 현재가 없는 종목 제외, 총 매수금액 합산, 수량 0 제외, 월 배정 없을 때 첫 템플릿, 템플릿 없는 달 스킵.
4. `SimBrokerClient` 경로로 사이클을 돌려 로그에 종목별 매수 수량·총 매수금액·예산 초과 경고가 올바르게 남는지 확인하세요.

> 판단 레이어(신호·임계값·백테스트 엔진·퀀트 로그)는 Phase 6에서 제거되었습니다. 관련 검증을 되살리지 마세요.

## 검증 대상 (인자: $ARGUMENTS)
$ARGUMENTS 에 지정된 종목/템플릿/기능이 있으면 그 범위에 집중하세요. 없으면 최근 변경분(`git diff`) 기준으로 검증 대상을 추론하세요.

## 보고
- 검증 통과/실패, 실패 시 원인과 수정 제안을 요약하세요.
- 실거래 전환 전 체크리스트(수량·예산·계좌 모드)를 마지막에 안내하세요.
