---
trigger: always_on
---

# 추가 개발 규칙 & 권장사항
 
## Phase 간 호환성 규칙
 
### 하위 호환성 유지 (MUST)
- 새 Phase 기능이 기존 기능을 깨뜨리면 안 됨
- `IBrokerClient` 인터페이스에 메서드 추가 시, `SimBrokerClient`와 `KisBrokerClient` 모두에 구현
- DB 스키마 변경 시 기존 데이터가 보존되도록 ALTER TABLE 마이그레이션 스크립트를 작성하여 반영

## 백그라운드 서비스 및 API 개발 규칙
 
### API Controller 작성 수칙
1. 엔드포인트는 RESTful 규칙을 따릅니다 (예: `GET /api/orders`, `POST /api/config`).
2. 비즈니스 로직은 컨트롤러에 직접 구현하지 말고 `Core` 레이어의 엔진을 DI로 주입받아 호출합니다.
3. 요청/응답 형식은 표준 JSON으로 통일합니다.

### 안정성 확보 (Polly 및 재시도)
- KIS 증권사 API 호출 등 외부 의존성이 있는 곳은 반드시 `Polly` 기반의 `AsyncRetryPolicy`를 적용하여 429 에러(Rate Limit)나 일시적 네트워크 오류 시 자동 재시도되도록 구성합니다.
- 실패가 누적될 경우 `NotificationService`를 통해 관리자에게 경고 이메일을 발송합니다.


## 성능 규칙
 
### API 호출 최소화 및 캐싱
- 불필요한 시장가 조회를 피하기 위해 반복 호출이 일어나는 데이터(환율 등)는 In-Memory(메모리 캐시)를 활용하여 캐싱(예: 1시간)합니다.
- 거래소 TPS 제한을 초과하지 않도록, `KisBrokerClient` 내에서 연속 호출 시 백오프 로직을 마련합니다.

### 스레드 보호 및 비동기 처리
- 장시간 소요되는 I/O 처리(주문 로직, 메일 발송 등)는 메인 흐름을 방해하지 않도록 완전 비동기로 설계합니다. (Wait(), Result 사용 절대 금지)
- 백그라운드 서비스 루프 내부 예외 발생 시 서비스 전체가 종료되지 않도록 `try-catch`로 감싸고 다음 주기로 넘어가게 처리합니다.


## 매매 결정 규칙 (현재 — 퀀트 단독)

### 퀀트 단독 결정
- 매수/매도/보류는 `QuantFilter`(전략 유형별 AND 조건)만으로 결정한다. **AI 호출 없음.**
- 환율(FX)은 `FxRateAdvisor`로 **설명·경고만** 한다 — 매매를 막지 않는다(veto 없음).
- `QuantFilter`, `QuantIndicator`의 기존 로직 변경 시 반드시 `BacktestEngine`으로 회귀 확인.

### AI 코드 휴면 처리 규칙 (보존)
- Phase 4~6에서 개발한 AI 결정 경로(`IMarketAnalyzer`/`AiMarketAnalyzer`/`GeminiMarketAnalyzer`,
  `CalculateConsensusScore`, `AdaptiveThresholdEngine`, `PerformanceFeedbackEngine`, `MonitoringController` 등)는
  **삭제하지 않고 주석으로 비활성화(보존)** 한다. 향후 재활성화 가능하도록 구조를 깨지 않는다.
- AI 컬럼(`BuyProbability`, `ChartAiScore` 등)은 **스키마 유지하되 더 이상 기록하지 않는다(0/빈값)**.

### (참고) 과거 AI 엔진 도입 시 설계 규칙 — 재활성화 시 준수
- `IMarketAnalyzer` 인터페이스에만 의존하고, AI 판단은 별도 레이어로 합산 (기존 퀀트 로직 직접 수정 금지)
- AI 엔진 인스턴스 생명주기는 `SessionManager`에서 관리 (브로커 분기 패턴과 동일)
- AI confidence가 낮거나 없으면 **퀀트 조건만으로 동작하는 fallback 유지**

### TB_MARKET_SNAPSHOT 데이터 보호 (현행 유지 — MUST)
- `TB_MARKET_SNAPSHOT`은 누적 데이터 — **임의 수정 및 삭제 절대 금지** (AI 컬럼 포함)
- Phase 2.5부터 축적된 데이터 연속성 유지가 중요
- 스키마 변경이 필요한 경우 기존 컬럼 유지 + 신규 컬럼 추가(ALTER TABLE)만 허용
---
 
## 테스트 규칙
- 신규 알고리즘이나 기능 추가 시, `appsettings.json`의 설정값을 변경하여 모의투자(`SimBrokerClient`) 모드로 먼저 로직을 검증합니다.
- 퀀트 판단 로직은 `BacktestEngine`을 통해 기존 데이터셋으로 의도된 매매가 일어나는지 확인합니다.