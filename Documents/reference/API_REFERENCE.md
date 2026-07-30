---
title: AutoInvesting API 정의서
date: 2026-07-23
company: [개인]
tags: [API, 레퍼런스, 엔드포인트, 인증]
status: draft
---

# AutoInvesting API 정의서

## 개요
> `Controllers/`의 실제 구현(Phase 6 — DCA 적립 코어)을 기준으로 한 REST API 레퍼런스다. 판단 레이어(전략/퀀트/AI/모니터링/분할매도/백테스트/시뮬)는 Phase 6에서 제거되어 관련 엔드포인트는 **더 이상 존재하지 않는다.** 거시 브리핑(`/api/macro/briefing`)과 점검용 매수(`POST /api/test/buy`)도 2026-07-30에 제거되었다(각각 화면 미배선으로 소비자 0 / `manual`과 중복). 실행 중 자동 생성되는 OpenAPI 명세는 `/swagger`에서도 볼 수 있다.

## 공통 사항

### Base URL
| 환경 | URL |
|------|-----|
| 로컬 | `http://localhost:<port>` |
| 배포 | Render.com 호스트 (`https://<앱>.onrender.com`) |

### 인증
전역 필터(`ApiKeyAuthAttribute`)가 **모든 컨트롤러 엔드포인트**에 적용된다. 아래 **둘 중 하나**로 통과한다.

| 호출 주체 | 헤더 | 값 |
|-----------|------|-----|
| 사람 / Web UI | `Authorization: Bearer <token>` | `/api/auth/login`으로 발급받은 서명 세션 토큰(7일) |
| 외부 크론잡 | `x-api-key: <key>` | 서버 `Security:ApiAccessKey`(`API_ACCESS_KEY`) 값 |

- 둘 다 없거나 유효하지 않으면 `401`.
- **인증 면제(`[PublicEndpoint]`)**: `/api/auth/status`·`/api/auth/setup`·`/api/auth/login`(닭-달걀 방지), `GET /api/health`, `/swagger`.

### 공통 응답 규약
- Content-Type: `application/json` (단 `GET /api/test/send-test-email`은 평문 문자열).
- 성공: `200 OK` (비동기 트리거는 `202 Accepted`).
- 오류: 대체로 `{ "error": "<메시지>" }`.

### 공통 상태 코드
| 코드 | 의미 |
|------|------|
| `200` | 성공 |
| `202` | 비동기 접수(백그라운드 시작) |
| `400` | 잘못된 요청(필수 파라미터/검증 실패) |
| `401` | 인증 실패(세션토큰·x-api-key 모두 없음/불일치) |
| `404` | 리소스 없음(티커 현재가 없음 등) |
| `409` | 충돌(과세 매도 미확인, 관리자 계정 중복 설정) |
| `500` | 서버 내부 오류 |
| `502` | 외부(주문) 거부/주문번호 없음 |
| `503` | 의존성(브로커 로그인) 미준비 |

### 엔드포인트 목록
| 그룹 | Method | 경로 | 인증 |
|------|--------|------|------|
| 인증 | GET | `/api/auth/status` | 면제 |
| 인증 | POST | `/api/auth/setup` | 면제 |
| 인증 | POST | `/api/auth/login` | 면제 |
| 설정 | GET | `/api/config` | 필요 |
| 설정 | POST | `/api/config` | 필요 |
| 설정 | GET | `/api/config/secret/{key}` | 필요 |
| 적립설정 | GET | `/api/dca/config` | 필요 |
| 적립설정 | PUT | `/api/dca/config` | 필요 |
| 주문 | POST | `/api/order/dca-run` | 필요 |
| 주문 | POST | `/api/order/manual` | 필요 |
| 주문 | GET | `/api/order/sell-preview` | 필요 |
| 시세 | GET | `/api/price/{ticker}` | 필요 |
| 포트폴리오 | GET | `/api/portfolio/holdings` | 필요 |
| 포트폴리오 | GET | `/api/portfolio/summary` | 필요 |
| 이력 | GET | `/api/history/trades` | 필요 |
| 이력 | GET | `/api/history/logs` | 필요 |
| 점검 | GET | `/api/test/send-test-email` | 필요 |
| 헬스 | GET | `/api/health` | 면제 |

## 본문

### 인증 (`AuthController`, `/api/auth`)
단일 관리자 로그인. 비밀번호 검증 후 서명된 세션 토큰(7일)을 발급한다. 모든 액션은 `[PublicEndpoint]`로 전역 인증 필터를 면제받는다.

**`GET /api/auth/status`** — 최초 설정 필요 여부 조회.
- 응답 `200`: `{ "needsSetup": true }` (관리자 비밀번호 해시가 없으면 true)

**`POST /api/auth/setup`** — 최초 1회 관리자 계정 설정.
- 요청 본문: `{ "username": "...", "password": "..." }` (비밀번호 8자 이상)
- 응답 `200`: `{ "message": "관리자 계정이 설정되었습니다. 로그인하세요." }`
- 오류: `409`(이미 설정됨), `400`(입력 누락/8자 미만)

**`POST /api/auth/login`** — 로그인, 세션 토큰 발급.
- 요청 본문: `{ "username": "...", "password": "..." }`
- 응답 `200`: `{ "token": "<서명 토큰>", "expiresAt": "<UTC 만료시각>" }`
- 오류: `400`(미설정 시 `{error, needsSetup:true}` / 입력 누락), `401`(자격증명 불일치), `500`(서명 키 `MASTER_KEY` 부재)

### 설정 (`ConfigController`, `/api/config`)

**`GET /api/config`** — 운영 설정 조회. 시크릿은 값 대신 설정 여부(`_SET`)만 반환.
- 응답 `200`:
```json
{
  "IS_PAPER_TRADING": "1",
  "KIS_SERVER": "vps",
  "KIS_APP_KEY_SET": "1",
  "KIS_APP_SECRET_SET": "1",
  "KIS_ACCOUNT_NO_SET": "1"
}
```
- 오류: `500`

**`POST /api/config`** — 설정 저장 + 세션 리셋(다음 호출부터 새 설정으로 브로커 재생성). 시크릿 키(`KIS_APP_KEY`·`KIS_APP_SECRET`·`KIS_ACCOUNT_NO`·`RESEND_API_KEY`·`API_ACCESS_KEY`)는 **빈 값으로 들어오면 기존 값 유지**(미변경).
- 요청 본문: 키-값 딕셔너리, 예: `{ "IS_PAPER_TRADING": "0" }`
- 응답 `200`: `{ "message": "설정이 성공적으로 저장되었습니다." }`
- 오류: `500`

**`GET /api/config/secret/{key}`** — 저장된 시크릿 평문 단건 조회(UI 눈 아이콘 확인용). 화이트리스트(`KIS_APP_KEY`·`KIS_APP_SECRET`·`KIS_ACCOUNT_NO`)만 허용하며 값은 로그에 남기지 않는다.
- 응답 `200`: `{ "key": "KIS_APP_KEY", "value": "<평문>", "set": true }`
- 오류: `400`(화이트리스트 밖 키), `500`

### 적립 설정 (`DcaController`, `/api/dca`)
여러 매수 템플릿(예산 + 종목별 고정 수량)과 월(1~12)별 템플릿 배정을 편집한다. 적립 사이클은 현재(KST) 월에 배정된 템플릿대로 매수한다.

**`GET /api/dca/config`** — 템플릿 목록 + 월배정 + 현재 월/활성 템플릿 조회.
- 응답 `200`:
```json
{
  "templates": [
    { "id": "core", "name": "코어", "budgetKrw": 1000000, "quantities": { "SPLG": 2, "QQQM": 1 } }
  ],
  "monthMap": { "1": "core", "2": "core" },
  "currentMonth": 7,
  "activeTemplateId": "core"
}
```
- 오류: `500`

**`PUT /api/dca/config`** — 템플릿·월배정 저장(다음 사이클부터 반영).
- 요청 본문:
```json
{
  "templates": [
    { "id": "core", "name": "코어", "budgetKrw": 1000000, "quantities": { "SPLG": 2, "QQQM": 1 } }
  ],
  "monthMap": { "1": "core", "7": "core" }
}
```
- 검증: 템플릿 1개 이상, `id` 필수·중복 불가, `budgetKrw > 0`, 각 종목 수량 1 이상.
- 응답 `200`: `{ "message": "적립 설정이 저장되었습니다. 다음 사이클부터 반영됩니다.", "templates": [...], "monthMap": {...} }`
- 오류: `400`(검증 실패), `500`

### 주문 (`OrderController`, `/api/order`)

**`POST /api/order/dca-run`** — 적립(DCA) 사이클을 백그라운드로 실행하고 **즉시 202** 반환. 외부 크론잡이 매수 주기에 호출한다(운영: `.github/workflows/daily-run.yml`, 매일 KST 23:40 — 엔진의 월 1회 멱등 가드가 당월 1회만 집행).
- 요청 본문: 없음
- 응답 `202`: `{ "message": "적립식 매수 사이클을 시작했습니다. 처리 결과는 서버 로그와 이메일로 확인하세요." }`
- 결과는 응답이 아니라 **서버 로그 + 이메일**로 확인.

**`POST /api/order/manual`** — 신호 판단 없이 즉시 매수/매도(KIS 연동 검증용). 매도 시 **보유 가드**(보유 수량 범위 내)와 **절세 가드**(과세 예상 매도인데 미확인 시 차단)가 적용된다.
- 요청 본문 (`ManualOrderRequest`):

| 필드 | 타입 | 필수 | 설명 |
|------|------|------|------|
| `ticker` | string | ✅ | 종목 코드 |
| `qty` | int | ✅ | 수량(1 이상) |
| `orderType` | string | | `"BUY"`(기본) 또는 `"SELL"` |
| `price` | decimal? | | 주문 단가(USD). 생략 시 현재가 |
| `acknowledgeTax` | bool | | (매도) 과세 예상을 확인했는지. false면 과세 매도 차단 |
| `ytdRealizedGainKrw` | decimal | | (매도) 올해 이미 실현한 양도차익(원). 공제 계산용, 기본 0 |

- 응답 `200`: `{ "message": "수동 BUY 주문이 실행되었습니다.", "ticker": "QQQM", "orderType": "BUY", "qty": 1, "price": 180.25, "orderNo": "..." }`
- 오류: `400`(검증/보유 초과/미보유 매도), `409`(과세 매도 미확인 — 본문에 `taxEstimate` 포함), `502`(주문 거부/주문번호 없음), `503`(브로커 로그인 실패), `500`
- 부수효과: 성공 시 `TB_TRADE_HISTORY`에 체결 기록.

**`GET /api/order/sell-preview`** — 매도 예정 정보로 예상 양도소득세를 미리 계산(주문 없음, 정보 제공). ⚠️ 판단/타이밍 아님 — 세금 산수 기반 정보.
- 쿼리: `ticker`(필수, 보유 종목), `qty`(필수), `price`(생략 시 현재가), `ytd`(올해 실현 양도차익, 기본 0)
- 응답 `200`: `SellTaxEstimateDto`(`IsTaxable`·`EstimatedTaxKrw` 등 — 상세 필드는 `Data/DTO/SellTaxEstimateDto.cs`)
- 오류: `400`(티커 누락/미보유), `503`(로그인 실패), `500`

### 시세 (`PriceController`, `/api/price`)

**`GET /api/price/{ticker}`** — 현재가(USD) + 환율 환산 원화가. 적립 설정 화면에서 티커 검증 겸용.
- 응답 `200`: `{ "ticker": "QQQM", "priceUsd": 180.25, "exchangeRate": 1380.5, "priceKrw": 248835.1 }`
- 오류: `400`(티커 누락), `404`(현재가 0 이하 = 미존재/조회 실패, 본문 `{error, ticker}`), `500`

### 포트폴리오 (`PortfolioController`, `/api/portfolio`)

**`GET /api/portfolio/holdings`** — 보유 종목 목록.
- 응답 `200`: `{ "holdings": [ ... ] }`
- 오류: `500`

**`GET /api/portfolio/summary`** — 대시보드 요약.
- 응답 `200`: `{ "holdings": [...], "cashBalance": 5000.0, "exchangeRate": 1380.5, "accountMode": "SIM", "accountMasked": "시뮬레이션 (로컬)" }` (`accountMode`: `SIM`/`PAPER`/`LIVE`)
- 오류: `500`

### 이력 (`HistoryController`, `/api/history`)

**`GET /api/history/trades`** — 매매 내역.
- 쿼리: `limit`(기본 50)
- 응답 `200`: `{ "trades": [ ... ] }`
- 오류: `500`

**`GET /api/history/logs`** — 시스템 로그(`TB_SYSTEM_LOG`, 재시작에도 보존).
- 쿼리: `date`(yyyy-MM-dd, 기본 오늘), `lines`(기본 200)
- 응답 `200`(있음): `{ "date": "2026-07-23", "totalLines": 120, "logs": [ ... ] }`
- 응답 `200`(없음): `{ "message": "... 로그가 없습니다.", "availableDates": [ ... ] }`
- 오류: `500`

### 점검 (`TestController`, `/api/test`)
⚠️ 진단 전용. 실제 메일을 발송한다. 실주문 경로는 두지 않는다 — 매수/매도는 가드가 있는 `/api/order/manual`만 사용한다.

**`GET /api/test/send-test-email`** — 이메일 발송 설정(Resend)이 동작하는지 확인 메일 1통 발송. 실패 원인을 응답으로 확인해야 하므로 예외를 삼키지 않는다.
- 응답 `200`: 평문 문자열 `"테스트 이메일 발송 완료. 수신 여부를 확인하세요."`
- 오류: `500`(발송 실패 — 본문에 실패 원인 포함)

### 헬스체크 (`/api/health`)

**`GET /api/health`** — ASP.NET Core `MapHealthChecks` 기반 경량 헬스체크. **인증 불요**(외부 업타임 체크용).
- 응답 `200`: `Healthy` 등 표준 헬스체크 응답

## 참고
- 인터랙티브 명세/시도: 서버 실행 후 `/swagger`
- 응답 스키마는 구현 기준 요약이며, 실제 DTO 필드는 `Data/DTO/`(`HoldingDto`·`TradeHistoryDto`·`SellTaxEstimateDto`·`DcaTemplate`) 참조.
- 인증 필터: `Utils/ApiKeyAuthAttribute.cs`, 면제 마커: `Utils/PublicEndpointAttribute.cs`.
- 한눈에 보는 평면 표는 별도 유지하지 않는다(2026-07-30 `API_REFERENCE_TABLE.md` 삭제 — 이 문서의 진부분집합이라 손으로 정합을 맞추다 드리프트가 반복됨). 요약 조회는 위 "엔드포인트 목록" 표 또는 `/swagger`를 쓴다.
- 본 문서는 `Controllers/` 변경 시 함께 갱신한다.
