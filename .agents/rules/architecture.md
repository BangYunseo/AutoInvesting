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
  - `IS_PAPER_TRADING` 설정값에 따라 SimBroker 또는 KisBroker 분기

## 아키텍처 흐름
```
사용자 → MainForm (SPA 패널 전환)
              ↓
         SessionManager → IBrokerClient (SimBroker 또는 KisBroker)
              ↓
         SchedulerModule (1분 간격 타이머)
              ↓ (예약 시각 도달)
         SmartOrderEngine
              ├── 현재가/가격범위 조회 (IBrokerClient)
              ├── OHLCV 데이터 조회
              ├── QuantIndicator (RSI, MACD, BB 계산)
              ├── QuantFilter (전략 유형별 AND 조건)
              └── 주문 실행 → TradeHistoryDAO (거래 기록)
                            → MarketSnapshotDAO (AI 학습 데이터)
```

## 새 기능 추가 순서
1. DTO → DAO → Core 로직 → UI 순서로 구현
2. 인터페이스-구현체 분리 원칙 유지
3. 비즈니스 로직은 반드시 `Core/` 하위에 배치
4. UI 로직과 비즈니스 로직을 혼합하지 않음
5. 새 Panel 추가 시 `Panels/` 폴더에 UserControl로 생성

## 비동기 패턴
- 외부 API/DB I/O 호출은 반드시 `async/await` 사용
- `Task.Run()`은 CPU-bound 작업에만 사용
- `async void`는 이벤트 핸들러에서만 허용
- `ConfigureAwait(false)`는 라이브러리 코드에서 사용, UI 코드에서는 생략

## 로깅 규칙

| 메서드 | 용도 |
|--------|------|
| `Logger.Info()` | 일반 정보 — `[SmartOrder] 분석 시작` |
| `Logger.Warn()` | 경고 (비정상이지만 계속 진행) |
| `Logger.Error()` | 에러 (처리 실패) |
| `Logger.Fatal()` | 치명적 오류 — `Program.cs` 전역 예외 |
| `Logger.LogQuant()` | 퀀트 판단 근거 기록 |

- 로그 메시지 형식: `[모듈명] 메시지` (예: `[SmartOrder] 매수 완료`)
- 빈 catch 블록 절대 금지 — 반드시 `Logger.Error()` 포함
