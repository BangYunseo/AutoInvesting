---
trigger: always_on
---

# 아키텍처 규칙

## 프로젝트 개요
- 해외 ETF 자동 적립(DCA) 투자 시스템 (ASP.NET Core Web API, .NET 8.0, C#)
- **타이밍/퀀트/AI 판단 없는 기계적 적립** — 목표비중을 향해 정수 단위로 매수 (Phase 6 전환)
- 외부 크론잡이 적립 사이클을 호출하는 Headless 서비스
- 증권사: 한국투자증권 (KIS) REST API

## 레이어 구조 및 의존성 방향
```
API (Controllers/)  ← 외부 크론잡이 dca-run 호출 / 사용자 제어·조회
  ↓ (단방향)
Core (Core/)
  ↓ (단방향)
Data (Data/, Data/DTO/, Data/DAO/)
  ← Utils (Utils/) — 모든 레이어에서 접근 가능
```

### 의존성 규칙
- **API → Core**: 허용 (컨트롤러에서 Core 엔진 호출)
- **Core → Data**: 허용 (엔진에서 DAO/DTO 사용)
- **Core → API**: 금지 (Core는 컨트롤러를 알지 못함)
- **Data → Core**: 금지 (Data는 Core를 알지 못함)
- **Utils**: 모든 레이어에서 접근 가능한 유틸리티

## 핵심 추상화
- `IBrokerClient` — 증권사 API 추상화 인터페이스
  - 구현체: `SimBrokerClient` (시뮬레이션), `KisBrokerClient` (KIS 실거래, Polly 내결함성 적용)
  - 새 증권사 추가 시 반드시 이 인터페이스를 구현
- `SessionManager` — 브로커 인스턴스 생명주기 관리
  - `IS_PAPER_TRADING` 설정값에 따라 SimBroker 또는 KisBroker 분기
- `DcaAccumulationEngine` — 적립식 매수 엔진. `PlanPurchases`(순수함수, 외부 I/O 없음 — 목표비중을 향한 정수 매수 계획 산출)와 `AccumulateAsync`(현재가·보유·환율 조회 → 계획 → 주문 → 기록) 분리
- `DcaSettings` — 목표비중·예산의 단일 읽기/쓰기 지점 (DB `TB_APP_CONFIG`: `DCA_TARGETS` JSON / `DCA_BUDGET_KRW` 우선 → `appsettings.json > Dca` 폴백)
- `DailyExecutionService` — 적립 사이클 실행 진입점 (`RunDcaCycleAsync`, Scoped, `IServiceScopeFactory` 패턴)
- `NotificationService` — 중요 알림(체결 내역, 예외) 외부 발송 (MailKit, Naver SMTP)

## 아키텍처 흐름
```
ASP.NET Core Host (Program.cs)
      ├── [REST API 호출] → Controllers (수동 주문, 적립 설정, 상태 조회)
      └── [POST /api/order/dca-run] → DailyExecutionService.RunDcaCycleAsync (외부 크론잡 호출)
                                       ↓
                                  DcaAccumulationEngine.AccumulateAsync
                                       ├── 환율/보유수량/현재가 조회 (IBrokerClient)
                                       ├── PlanPurchases (목표비중 향한 정수 매수 계획 — 순수함수)
                                       ├── 매수 주문 실행 → TradeHistoryDAO (거래 기록 저장)
                                       └── 메일 발송 → NotificationService (적립 보고서)
```

## 적립(DCA) 배분 원칙 (Phase 6)
- `DcaSettings.Load()`로 목표비중·예산을 읽어(`DcaController`에서 편집), `PlanPurchases`가
  "현재 목표비중 대비 가장 부족한 종목"을 1주씩 정수로 매수, 더 못 살 때까지 반복
- 소수점 매수를 하지 않으므로 1주 단가가 비싼 종목은 잔돈이 모일 때까지 자연 스킵, 잔돈은 다음 사이클로 이월
- **타이밍 판단·신호·임계값·합의 스코어링 없음** — 백테스트로 가치 없음이 확인되어 제거됨

## 새 기능 추가 순서
1. DTO → DAO → Core 로직 → API Controller 또는 BackgroundService 순서로 구현
2. 인터페이스-구현체 분리 원칙 유지
3. 비즈니스 로직은 반드시 `Core/` 하위에 배치
4. 외부 연동부(HTTP API, SMTP)는 `Utils` 또는 `Core` 통신 계층에 배치

## 비동기 패턴
- 외부 API/DB I/O 호출은 반드시 `async/await` 사용
- ASP.NET Core 환경이므로 동기 블로킹 호출(`Task.Wait()`, `.Result`)을 절대 사용하지 않음
- `ConfigureAwait(false)`는 ASP.NET Core에서 기본 컨텍스트가 없으므로 굳이 강제하지 않으나, 재사용 라이브러리 작성 시에는 권장됨

## 로깅 규칙

| 메서드 | 용도 |
|--------|------|
| `Logger.Info()` | 일반 정보 — `[DCA] 적립식 매수 시작` |
| `Logger.Warn()` | 경고 (비정상이지만 계속 진행, API 재시도 발생 등) |
| `Logger.Error()` | 에러 (처리 실패, 이메일 알림 연동 대상) |
| `Logger.Fatal()` | 치명적 오류 — `Program.cs` 미들웨어 또는 Host 종료 시 |

- 로그 메시지 형식: `[모듈명] 메시지` (예: `[KisBrokerClient] 429 응답, 2초 후 재시도`)
- 빈 catch 블록 절대 금지 — 반드시 `Logger.Error()` 포함
