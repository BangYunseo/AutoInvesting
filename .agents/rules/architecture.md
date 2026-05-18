---
trigger: always_on
---

# 아키텍처 규칙

## 프로젝트 개요
- 해외 ETF 자동 투자 시스템 (WinForms, .NET 8.0, C#)
- 퀀트 지표 기반 감정 배제 매매
- 증권사: 한국투자증권 (KIS) REST API

## 레이어 구조 및 의존성 방향
```
UI (Forms/, Panels/, Controls/)
  ↓ (단방향)
Core (Core/, Core/Quant/)
  ↓ (단방향)
Data (Data/, Data/DTO/, Data/DAO/)
  ← Utils (Utils/) — 모든 레이어에서 접근 가능
```

### 의존성 규칙
- **UI → Core**: 허용 (Panels에서 Core 엔진 호출)
- **Core → Data**: 허용 (엔진에서 DAO/DTO 사용)
- **Core → UI**: 금지 (Core는 UI를 알지 못함)
- **Data → Core**: 금지 (Data는 Core를 알지 못함)
- **Utils**: 모든 레이어에서 접근 가능한 유틸리티

## 핵심 추상화
- `IBrokerClient` — 증권사 API 추상화 인터페이스
  - 구현체: `SimBrokerClient` (시뮬레이션), `KisBrokerClient` (KIS 실거래)
  - 새 증권사 추가 시 반드시 이 인터페이스를 구현
- `SessionManager` — 브로커 인스턴스 생명주기 관리
  - 설정값에 따라 SimBroker 또는 KisBroker 분기

## 새 기능 추가 가이드
1. DTO → DAO → Core 로직 → UI 순서로 구현
2. 인터페이스-구현체 분리 원칙 유지
3. 비즈니스 로직은 반드시 Core/ 하위에 배치
4. UI 로직과 비즈니스 로직을 혼합하지 않음
5. 새 Panel 추가 시 `Panels/` 폴더에 UserControl로 생성

## 비동기 패턴
- 외부 API 호출은 반드시 `async/await` 사용
- `Task.Run()`은 CPU-bound 작업에만 사용
- `ConfigureAwait(false)`는 라이브러리 코드에서 사용, UI 코드에서는 생략

## 로깅 규칙
- 모든 주요 동작에 `Logger.Info()` 로그 남기기
- 에러 발생 시 `Logger.Error()` 사용
- 경고 상황은 `Logger.Warn()` 사용
- 퀀트 판단 결과는 `Logger.LogQuant()` 사용
- 로그 메시지 형식: `[모듈명] 메시지` (예: `[SmartOrder] 매수 완료`)
