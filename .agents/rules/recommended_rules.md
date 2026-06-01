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


## Phase 4 AI 엔진 도입 규칙
 
### 인터페이스 우선 설계
- `IMarketAnalyzer` 인터페이스를 먼저 정의하고, `SmartOrderEngine`은 인터페이스에만 의존
- AI 판단 결과는 `CombineSignals()`를 통해 기존 퀀트 신호와 합산 — 기존 퀀트 로직 직접 수정 금지
- AI 엔진 인스턴스 생명주기는 `SessionManager`에서 관리 (기존 브로커 분기 패턴 동일하게 적용)

### TB_MARKET_SNAPSHOT 데이터 보호
- `TB_MARKET_SNAPSHOT`은 AI 학습용 축적 데이터 — 임의 수정 및 삭제 절대 금지
- Phase 2.5부터 매매 시점마다 자동 저장되고 있으므로 데이터 연속성 유지가 중요
- 스키마 변경이 필요한 경우 기존 컬럼 유지 + 신규 컬럼 추가(ALTER TABLE)만 허용

### 기존 퀀트 흐름 보호
- `QuantFilter`, `QuantIndicator`의 기존 로직은 수정하지 않고 AI 신호를 별도 레이어로 추가
- AI confidence score가 낮거나 없을 경우 기존 퀀트 조건만으로 동작하는 fallback 유지 필수
---
 
## 테스트 규칙
- 신규 알고리즘이나 기능 추가 시, `appsettings.json`의 설정값을 변경하여 모의투자(`SimBrokerClient`) 모드로 먼저 로직을 검증합니다.
- 퀀트 판단 로직은 `BacktestEngine`을 통해 기존 데이터셋으로 의도된 매매가 일어나는지 확인합니다.