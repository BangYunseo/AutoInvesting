# AutoInvesting API 정의서

> 해외 ETF 자동투자 시스템 · ASP.NET Core Web API (.NET 8.0)
> 본 문서는 `Controllers/` 의 실제 구현을 기준으로 작성되었습니다. (Phase 8 기준)
> 실행 중 자동 생성되는 OpenAPI 명세는 `/swagger` 에서도 확인할 수 있습니다.
>
> ℹ️ **현재 매매 결정은 퀀트 단독**입니다. AI 결정 경로(합의 스코어링·다중 에이전트·적응형 임계값)는 주석 비활성화(휴면)되어 있고
> 분석/실행 중 AI 호출은 없습니다. 환율(FX) 어드바이저는 매매를 막지 않는 설명·경고로 응답에 첨부됩니다.
> 모니터링/적응형/가중치 A/B 관련 엔드포인트는 **유지되나 신규 데이터가 더 이상 적재되지 않아** 빈/0 값에 가깝습니다(휴면).

---

## 공통 사항

### Base URL
| 환경 | URL |
|------|-----|
| 로컬 | `http://localhost:<port>` |
| 배포 | Render.com 호스트 |

### 인증 (필수)
모든 컨트롤러 엔드포인트는 **전역 API Key 필터(`ApiKeyAuthAttribute`)** 가 적용됩니다.
요청 시 아래 헤더를 반드시 포함해야 합니다.

```
x-api-key: <서버 API_ACCESS_KEY 값>
```

| 상황 | 응답 |
|------|------|
| `x-api-key` 헤더 누락 | `401` `{ "error": "API 키가 누락되었습니다. (헤더에 'x-api-key' 포함 필요)" }` |
| 서버에 `API_ACCESS_KEY` 미설정 | `401` `{ "error": "서버 측에 API Access Key가 설정되지 않았습니다. 관리자에게 문의하세요." }` |
| 키 불일치 | `401` `{ "error": "권한이 없습니다. 유효하지 않은 API 키입니다." }` |

> **예외 (인증 불요)**: `GET /api/health` (HealthCheck 엔드포인트), `/swagger` UI 는 컨트롤러가 아니므로 필터가 적용되지 않습니다.

### 공통 응답 규약
- Content-Type: `application/json`
- 성공: `200 OK` (비동기 트리거는 `202 Accepted`)
- 오류: 본문은 대체로 `{ "error": "<메시지>" }` 형식 (일부 엔드포인트는 문자열만 반환)

### 공통 상태 코드
| 코드 | 의미 |
|------|------|
| `200` | 성공 |
| `202` | 비동기 작업 접수 (백그라운드 실행 시작) |
| `400` | 잘못된 요청 (필수 파라미터 누락/검증 실패) |
| `401` | 인증 실패 (x-api-key) |
| `404` | 리소스 없음 |
| `500` | 서버 내부 오류 |
| `502` | 외부(주문) 거부/응답 없음 |
| `503` | 의존성(브로커 로그인/설정) 미준비 |

---

## 엔드포인트 목록 (요약)

| # | 그룹 | Method | 경로 |
|---|------|--------|------|
| 1 | 주문 | POST | `/api/order/execute` |
| 2 | 주문 | POST | `/api/order/daily-run` |
| 3 | 주문 | POST | `/api/order/manual` |
| 4 | 주문 | GET | `/api/order/analyze/{ticker}` |
| 5 | 설정 | GET | `/api/config` |
| 6 | 설정 | POST | `/api/config` |
| 7 | 설정 | GET | `/api/config/gemini-models` |
| 8 | 전략 | GET | `/api/strategy/summary` |
| 9 | 전략 | GET | `/api/strategy/adaptive-status` |
| 10 | 전략 | GET | `/api/strategy/{name}` |
| 11 | 전략 | POST | `/api/strategy/{name}` |
| 12 | 전략 | DELETE | `/api/strategy/{name}` |
| 13 | 모니터링 | GET | `/api/monitoring/summary` |
| 14 | 모니터링 | GET | `/api/monitoring/performance` |
| 15 | 모니터링 | GET | `/api/monitoring/tokens/by-agent` |
| 16 | 모니터링 | GET | `/api/monitoring/tokens/daily` |
| 17 | 모니터링 | GET | `/api/monitoring/agent-accuracy` |
| 18 | 모니터링 | GET | `/api/monitoring/weight-abtest` |
| 19 | 모니터링 | GET | `/api/monitoring/adaptive-threshold` |
| 20 | 이력 | GET | `/api/history/trades` |
| 21 | 이력 | GET | `/api/history/logs` |
| 22 | 분할매도 | GET | `/api/sellplan` |
| 23 | 분할매도 | POST | `/api/sellplan` |
| 24 | 분할매도 | DELETE | `/api/sellplan/{id}` |
| 25 | 포트폴리오 | GET | `/api/portfolio/holdings` |
| 26 | 포트폴리오 | GET | `/api/portfolio/summary` |
| 27 | 퀀트 | GET | `/api/quant/analyze/{ticker}` |
| 28 | 백테스트 | POST | `/api/backtest/run` |
| 29 | 시뮬레이션 | POST | `/api/sim/generate-training-data` |
| 30 | 시뮬레이션 | GET | `/api/sim/verify-training-data` |
| 31 | 테스트/진단 | POST | `/api/test/inject-mock` |
| 32 | 테스트/진단 | GET | `/api/test/test-adaptive` |
| 33 | 테스트/진단 | POST | `/api/test/buy` |
| 34 | 테스트/진단 | POST | `/api/test/send-report` |
| 35 | 테스트/진단 | GET | `/api/test/send-test-email` |
| 36 | 테스트/진단 | GET | `/api/test/health` |
| — | 헬스체크 | GET | `/api/health` (인증 불요) |

---

## 1. 주문 (`OrderController`, `/api/order`)

> 예약 시각 외 수동 주문 트리거 및 일일 사이클 진입점.

### `POST /api/order/execute`
현재 활성 전략(`ACTIVE_STRATEGY`) 기반으로 스마트 주문을 즉시 실행합니다.

- **요청 본문**: 없음
- **동작**: 브로커 로그인 → 전략 종목 로드 → `INVEST_AMOUNT_KRW` 만큼 `SmartOrderEngine` 실행
- **응답 `200`**
```json
{
  "message": "스마트 주문 3건 실행 완료",
  "results": [
    { "ticker": "QQQM", "signal": "BUY", "reason": "...", "price": 180.25 }
  ]
}
```
- **오류**: `400`(전략에 종목 없음), `503`(브로커 로그인 실패), `500`

### `POST /api/order/daily-run`
일일 전체 사이클(퀀트 단독 매매·리밸런싱·메일 리포트)을 **백그라운드로 실행**합니다. (AI 평가는 현재 휴면) 외부 스케줄러에서 1일 1회 호출하는 용도이며(운영 환경에서는 GitHub Actions 워크플로우 `.github/workflows/daily-run.yml`가 매일 KST 23:40에 호출), 타임아웃 방지를 위해 즉시 `202` 를 반환합니다.

- **요청 본문**: 없음
- **응답 `202`**
```json
{ "message": "일일 사이클을 시작했습니다. 처리 결과는 서버 로그와 이메일로 확인하세요." }
```
> 결과는 응답이 아니라 **서버 로그 + 이메일**로 확인합니다.

### `POST /api/order/manual`
신호 판단(퀀트 필터)을 거치지 않고 즉시 매수/매도합니다. **KIS 모의계좌 연동 검증용** — 실거래 환경에서는 주의.

- **요청 본문** (`ManualOrderRequest`)

| 필드 | 타입 | 필수 | 설명 |
|------|------|------|------|
| `ticker` | string | ✅ | 종목 코드 (예: `QQQM`) |
| `qty` | int | ✅ | 주문 수량 (1 이상) |
| `orderType` | string | | `"BUY"`(기본) 또는 `"SELL"` |
| `price` | decimal? | | 주문 가격(USD). 생략 시 현재가로 주문 |

```json
{ "ticker": "QQQM", "qty": 1, "orderType": "BUY", "price": 180.25 }
```
- **응답 `200`**
```json
{ "message": "수동 BUY 주문이 실행되었습니다.", "ticker": "QQQM", "orderType": "BUY", "qty": 1, "price": 180.25, "orderNo": "0000123456" }
```
- **오류**: `400`(필수값/검증), `503`(로그인 실패), `502`(주문 거부/주문번호 없음), `500`
- **부수효과**: 성공 시 `TradeHistory` 에 체결 기록 저장

### `GET /api/order/analyze/{ticker}`
주문 실행 없이 단일 종목 분석 결과만 조회합니다. (현재 신호는 **퀀트 단독**으로 산출하며, `advisoryNotes`에 환율 유불리 설명·경고가 첨부됩니다.)

- **경로 파라미터**: `ticker` — 종목 코드
- **쿼리 파라미터**: `strategy` (기본 `MEAN_REVERSION`)
- **응답 `200`**
```json
{
  "ticker": "QQQM",
  "signal": "BUY",
  "reason": "...",
  "decisionReason": "...",
  "price": 180.25,
  "indicators": { "position": 0.12, "rsi14": 31.5, "macdLine": 0.0, "macdSignal": 0.0, "macdHistogram": 0.0, "bbUpper": 0, "bbMiddle": 0, "bbLower": 0 },
  "conditions": [ ... ],
  "advisoryNotes": [ { "source": "...", "severity": "INFO", "title": "...", "message": "...", "suggestedAlternatives": [ ... ] } ]
}
```
- **오류**: `500`

---

## 2. 설정 (`ConfigController`, `/api/config`)

### `GET /api/config`
운영에 필요한 설정값을 조회합니다. **시크릿(API 키 등)은 반환하지 않습니다.**

- **응답 `200`** (키-값 딕셔너리)
```json
{
  "IS_PAPER_TRADING": "1",
  "ACTIVE_STRATEGY": "안정형",
  "INVEST_AMOUNT_KRW": "1000000",
  "ORDER_SCHEDULE": "22:30",
  "REBALANCE_THRESHOLD": "0.05",
  "AI_PROVIDER": "mock",
  "GEMINI_MODEL": "gemini-2.0-flash"
}
```

### `POST /api/config`
설정값을 저장합니다. 저장 후 **세션을 리셋**하여 다음 호출부터 새 설정으로 브로커/AI 분석기를 재생성합니다.

- **요청 본문**: 키-값 딕셔너리
```json
{ "ACTIVE_STRATEGY": "공격형", "GEMINI_MODEL": "gemini-2.0-flash" }
```
- **응답 `200`**: `{ "message": "설정이 성공적으로 저장되었습니다." }`

### `GET /api/config/gemini-models`
현재 `GEMINI_API_KEY` 로 사용 가능한 모델 목록(`generateContent` 지원 gemini 계열)을 조회합니다. 설정 화면의 모델 드롭다운용.

- **응답 `200`**
```json
{ "models": ["gemini-2.0-flash", "gemini-2.0-flash-lite", "..."] }
```
- 키 미설정/조회 실패 시: `{ "models": [], "error": "..." }`

---

## 3. 전략 (`StrategyController`, `/api/strategy`)

### `GET /api/strategy/summary`
전체 전략 요약 목록을 조회합니다.
- **응답 `200`**: 전략 요약 배열

### `GET /api/strategy/adaptive-status`
전략 종목들의 적응형 임계값 작동 현황(누적 표본 수, 적용 임계값)을 진단합니다.
- **쿼리 파라미터**: `name` (미지정 시 `ACTIVE_STRATEGY` 사용)
- **응답 `200`**
```json
{ "strategy": "안정형", "items": [ { "ticker": "QQQM", "...": "..." } ] }
```

### `GET /api/strategy/{name}`
특정 전략의 종목 목록을 조회합니다.
- **경로 파라미터**: `name` (기본 `사용자정의`)
- **응답 `200`**: `StrategyDto` 배열

### `POST /api/strategy/{name}`
전략 전체를 저장(덮어쓰기)합니다.
- **경로 파라미터**: `name`
- **요청 본문**: `StrategyDto[]`
```json
[ { "ticker": "QQQM", "qty": 2, "strategyType": "MEAN_REVERSION" } ]
```
- **응답 `200`**: `{ "message": "전략이 성공적으로 저장되었습니다." }`
- **오류**: `400`(본문 null), `500`

### `DELETE /api/strategy/{name}`
전략을 삭제합니다.
- **응답 `200`**: `{ "message": "전략이 삭제되었습니다." }`

---

## 4. 모니터링 (`MonitoringController`, `/api/monitoring`)

> AI 판단 성과 / 토큰 사용량·비용 조회용 **읽기 전용** 엔드포인트 (Phase 5-b~5-d).
> ⚠️ **현재 휴면**: 퀀트 단독 전환(Phase 8) 이후 AI 호출이 없어 **신규 성과·토큰 데이터가 적재되지 않습니다.** 엔드포인트는 유지되며 과거 누적 데이터를 반환합니다.
> 비용 추정 단가: 입력 $0.10, 출력 $0.40 (USD / 1M tokens, gemini-2.0-flash 기준).

### `GET /api/monitoring/summary`
요약 카드용 핵심 지표.
- **쿼리**: `days` (기본 30)
- **응답 `200`**
```json
{
  "todayTotalTokens": 12345,
  "evaluatedCount": 80,
  "averageWinRate": 0.6125,
  "periodDays": 30,
  "periodPromptTokens": 100000,
  "periodCompletionTokens": 40000,
  "periodTotalTokens": 140000,
  "estPeriodCostUsd": 0.026
}
```

### `GET /api/monitoring/performance`
최근 AI 판단 성과 기록.
- **쿼리**: `limit` (기본 50)
- **응답 `200`**: 성과 기록 배열

### `GET /api/monitoring/tokens/by-agent`
에이전트 유형별 토큰 사용량 + 비용 추정.
- **쿼리**: `days` (기본 30)
- **응답 `200`**
```json
{ "periodDays": 30, "agents": [ { "agentType": "CHART", "callCount": 30, "promptTokens": 50000, "completionTokens": 20000, "totalTokens": 70000, "estCostUsd": 0.013 } ] }
```

### `GET /api/monitoring/tokens/daily`
일자별 토큰 사용량 + 비용 추정 (최신순).
- **쿼리**: `days` (기본 14)
- **응답 `200`**
```json
{ "periodDays": 14, "daily": [ { "date": "2026-06-22", "callCount": 5, "promptTokens": 8000, "completionTokens": 3000, "totalTokens": 11000, "estCostUsd": 0.002 } ] }
```

### `GET /api/monitoring/agent-accuracy`
에이전트(퀀트/차트AI/펀더멘털AI)별 실측 적중률 (Phase 5-d).
- **쿼리**: `horizonDays` (신호 이후 경과일, 기본 7)
- **응답 `200`**: `{ "horizonDays": 7, "agents": [ ... ] }`

### `GET /api/monitoring/weight-abtest`
합의 가중치 조합별 가상 매수 성과(A/B 백테스트) (Phase 5-d).
> ⚠️ 검증용 리포트 — 실제 매매 가중치에 자동 반영되지 않습니다.
- **쿼리**: `horizonDays` (기본 7)
- **응답 `200`**: `{ "horizonDays": 7, "note": "검증용 리포트 ...", "schemes": [ ... ] }`

### `GET /api/monitoring/adaptive-threshold`
특정 종목의 현재 적응형 매수/매도 임계값과 산출 근거 (Phase 5-d).
- **쿼리**: `ticker` (필수)
- **응답 `200`**
```json
{ "ticker": "QQQM", "buyThreshold": 0.68, "buyReason": "...", "sellThreshold": 0.90, "sellReason": "..." }
```
- **오류**: `400`(ticker 누락), `500`

---

## 5. 이력 (`HistoryController`, `/api/history`)

### `GET /api/history/trades`
매매 내역 조회.
- **쿼리**: `limit` (기본 50)
- **응답 `200`**: 매매 내역 배열

### `GET /api/history/logs`
시스템 로그 조회.
- **쿼리**: `date` (yyyy-MM-dd, 기본 오늘), `lines` (기본 200)
- **응답 `200`** (로그 있음)
```json
{ "date": "2026-06-22", "totalLines": 120, "logs": [ "..." ] }
```
- **응답 `200`** (해당 날짜 로그 없음)
```json
{ "message": "2026-06-22 날짜의 로그 파일이 없습니다.", "availableDates": ["2026-06-21", "..."] }
```

---

## 6. 분할매도 (`SellPlanController`, `/api/sellplan`)

### `GET /api/sellplan`
활성 분할매도 플랜 목록.
- **응답 `200`**: `SellPlanDto[]`

### `POST /api/sellplan`
분할매도 플랜 생성. (서버에서 `status="ACTIVE"`, `soldQty=0` 자동 설정)
- **요청 본문**: `SellPlanDto` (`ticker`, `strategyType` 등)
- **응답 `200`**: 생성된 `SellPlanDto` (`planId` 포함)
- **오류**: `500`("플랜 생성에 실패했습니다." / "서버 내부 오류가 발생했습니다.")

### `DELETE /api/sellplan/{id}`
분할매도 플랜 취소 (`status="CANCELLED"`).
- **경로 파라미터**: `id`
- **응답 `200`**: `{ "Message": "취소되었습니다." }`
- **오류**: `404`(활성 플랜 없음), `500`

---

## 7. 포트폴리오 (`PortfolioController`, `/api/portfolio`)

### `GET /api/portfolio/holdings`
보유 종목 조회.
- **응답 `200`**: 보유 종목 배열

### `GET /api/portfolio/summary`
대시보드 요약(보유 종목 + 예수금 + 환율)을 한 번에 조회.
- **응답 `200`**
```json
{ "holdings": [ ... ], "cashBalance": 5000.0, "exchangeRate": 1380.5 }
```
- **오류**: `500`

---

## 8. 퀀트 (`QuantController`, `/api/quant`)

### `GET /api/quant/analyze/{ticker}`
현재가 + 120일 OHLCV 기반 실시간 퀀트 분석 의견. (`/api/order/analyze` 와 달리 매매 신호 합의 없이 순수 퀀트 지표/조건 판정만 반환)
- **경로 파라미터**: `ticker`
- **쿼리**: `strategyType` (기본 `MEAN_REVERSION`)
- **응답 `200`**
```json
{
  "ticker": "QQQM",
  "currentPrice": 180.25,
  "strategyType": "MEAN_REVERSION",
  "indicators": { "rsi14": 31.5, "position": 0.12, "macdLine": 0.0, "macdHistogram": 0.0 },
  "analysis": { "buyPassed": true, "buySummary": "...", "sellPassed": false, "sellSummary": "..." }
}
```
- **오류**: `400`(ticker 누락), `404`(데이터 없음), `500`

---

## 9. 백테스트 (`BacktestController`, `/api/backtest`)

### `POST /api/backtest/run`
과거 데이터 기반 전략 수익성 검증.
- **요청 본문** (`BacktestRequest`)

| 필드 | 타입 | 필수 | 기본/제약 |
|------|------|------|-----------|
| `ticker` | string | ✅ | 20자 이내 |
| `strategyType` | string? | | `MEAN_REVERSION`(기본)/`MOMENTUM`/`MIXED` |
| `days` | int | | 기본 120, 최대 1000 (초과 시 1000으로 제한) |
| `initialCapital` | decimal | | 기본 10000 (USD) |
| `buyThreshold` | decimal | | 기본 0.10 |
| `sellThreshold` | decimal | | 기본 0.90 |

```json
{ "ticker": "QQQM", "strategyType": "MEAN_REVERSION", "days": 250, "initialCapital": 10000 }
```
- **응답 `200`**
```json
{
  "ticker": "QQQM", "strategy": "MEAN_REVERSION", "days": 250, "initialCapital": 10000,
  "finalCapital": 11250.0, "totalReturnPct": 12.5, "maxDrawdownPct": -8.3, "winRatePct": 62.5, "totalTrades": 16,
  "trades": [ { "date": "2025-09-01", "type": "BUY", "price": 175.0, "qty": 5, "profitLoss": 0 } ]
}
```
- **오류**: `400`(ticker 누락/길이 초과/유효하지 않은 전략), `500`

---

## 10. 시뮬레이션 학습데이터 (`SimController`, `/api/sim`)

> Phase 6-a: SimBroker 기반 라벨링 스냅샷(`DATA_SOURCE='SIM'`) 생성·검증. **실데이터(REAL)는 건드리지 않습니다.**

### `POST /api/sim/generate-training-data`
SimBroker 시뮬레이션으로 AI 학습데이터를 대량 생성하여 SIM 출처로 저장.
- **요청 본문** (`SimTrainingDataGenerator.GenerateRequest`): 종목 목록 / 종목당 스냅샷 수 / 전략 유형 (생략 시 기본값)
- **응답 `200`**
```json
{ "message": "시뮬레이션 학습데이터 600건 생성 완료 (SIM)", "insertedCount": 600, "tickerCount": 6, "perTicker": 100 }
```

### `GET /api/sim/verify-training-data`
생성된 SIM 데이터만으로 에이전트별 실측 적중률 + 가중치 A/B 산출 (검증용).
- **쿼리**: `horizonDays` (기본 7)
- **응답 `200`**
```json
{ "dataSource": "SIM", "snapshotCount": 600, "horizonDays": 7, "agentAccuracy": { ... }, "weightAbTest": { ... } }
```

---

## 11. 테스트 / 진단 (`TestController`, `/api/test`)

> ⚠️ **개발·진단 전용.** 일부 엔드포인트는 DB를 직접 수정하거나 실제 주문·메일을 발생시킵니다.
> 운영 환경 노출 시 주의하거나 비활성화를 권장합니다.

### `POST /api/test/inject-mock`
QQQ 목업 스냅샷 30건 주입.
> ⚠️ 실행 시 `TB_MARKET_SNAPSHOT` 의 `TICKER='QQQ'` 데이터를 **DELETE** 후 재삽입합니다.
- **응답 `200`**: `"Mock data injected. Range: 0.51 ~ 0.80. Expected 70th Percentile ~ 0.71"`

### `GET /api/test/test-adaptive`
적응형 임계값 + 분석 결과 테스트.
- **쿼리**: `ticker` (기본 `QQQ`)
- **응답 `200`**: `{ "adaptiveThreshold": 0.71, "thresholdReason": "...", "analysisResult": { ... } }`

### `POST /api/test/buy`
즉시 매수 (현재가 기준).
> ⚠️ 실제 주문이 발생합니다.
- **쿼리**: `ticker` (기본 `QQQM`), `qty` (기본 1)
- **응답 `200`**: `{ "message": "매수 주문 성공", "orderNo": "...", "ticker": "QQQM", "qty": 1, "price": 180.25 }`
- **오류**: `400`(현재가 조회 실패), `500`

### `POST /api/test/send-report`
테스트용 일일 운용 보고서 메일 발송.
- **응답 `200`**: `{ "message": "테스트 일일 보고서 메일 발송 성공" }`

### `GET /api/test/send-test-email`
테스트 이메일 발송. **운영 경로와 달리 예외를 삼키지 않고 실패 원인을 응답에 노출**합니다.
- **응답 `200`**: `{ "ok": true, "message": "테스트 이메일 발송 성공. ..." }`
- **오류**: `503`(`reason: "CONFIG_MISSING"`), `500`(`reason: "SEND_ERROR"`)

### `GET /api/test/health`
시스템 핵심 의존성(이메일/DB/브로커) + 운영 모드(실/목업)를 한 번에 점검하는 헬스체크. **시크릿 값은 노출하지 않고 설정 여부·활성 타입만 반환합니다.**
- **응답 `200`**(정상) / `503`(일부 비정상)
```json
{
  "ok": true,
  "mode": {
    "liveBroker": false, "liveAi": false,
    "brokerType": "SimBrokerClient", "kisServer": "vps",
    "kisAppKeySet": true, "kisAccountSet": true,
    "analyzerType": "AiMarketAnalyzer", "aiProvider": "mock",
    "geminiKeySet": false, "activeStrategy": "안정형"
  },
  "email": { "ready": true, "provider": "resend", "apiKeySet": true, "senderEmail": "...", "senderName": "...", "adminEmailSet": true },
  "db": { "ok": true, "error": null },
  "broker": { "ok": true, "error": null, "type": "SimBrokerClient" }
}
```
> `liveBroker` & `liveAi` 가 **둘 다 true** 여야 "실데이터" 분석/주문 상태입니다.

---

## 헬스체크 (`/api/health`)

### `GET /api/health`
ASP.NET Core `MapHealthChecks` 기반 경량 헬스체크. **`x-api-key` 인증이 적용되지 않습니다.** (외부 모니터링/업타임 체크용)
- **응답 `200`**: `Healthy` 등 표준 헬스체크 응답

---

## 참고
- 인터랙티브 명세/시도: 서버 실행 후 `/swagger`
- 본 문서는 `Controllers/` 변경 시 함께 갱신해야 합니다. (응답 스키마는 구현 기준 요약이며, 실제 DTO 필드는 `Data/DTO/` 참조)
