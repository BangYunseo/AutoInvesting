---
title: AutoInvesting API 일람표
date: 2026-07-23
company: [개인]
tags: [API, 일람표, 엔드포인트, 인증]
status: draft
---

# AutoInvesting API 일람표

## 개요
> 한 줄 = 한 API로 한눈에 보는 평면 표(Phase 6 — DCA 적립 코어 기준). 상세 요청/응답 예시는 `Documents/reference/API_REFERENCE.md`, 인터랙티브 명세는 `/swagger` 참조. 판단 레이어(전략/퀀트/AI/모니터링/분할매도/백테스트/시뮬)는 제거되어 관련 엔드포인트는 존재하지 않는다. 거시 브리핑과 점검용 매수도 2026-07-30에 제거되어 총 18개다.

## 본문

### 인증 규약
전역 필터(`ApiKeyAuthAttribute`)가 모든 엔드포인트에 적용되며 **Bearer 세션토큰(사람) 또는 `x-api-key`(크론)** 중 하나로 통과한다. `/api/auth/*`와 `GET /api/health`는 `[PublicEndpoint]`로 면제.

### 엔드포인트

| # | 그룹 | Method | 경로 | 설명 | 요청 (파라미터/본문) | 주요 응답(200) | 오류 코드 | 비고 |
|---|------|--------|------|------|----------------------|----------------|-----------|------|
| 1 | 인증 | GET | `/api/auth/status` | 최초 설정 필요 여부 | 없음 | `{needsSetup}` | — | 인증 면제 |
| 2 | 인증 | POST | `/api/auth/setup` | 관리자 계정 최초 설정 | body: `username*`, `password*`(8자+) | `{message}` | 400/409 | 인증 면제·1회만 |
| 3 | 인증 | POST | `/api/auth/login` | 로그인·세션토큰 발급 | body: `username*`, `password*` | `{token, expiresAt}` | 400/401/500 | 인증 면제·토큰 7일 |
| 4 | 설정 | GET | `/api/config` | 운영 설정 조회(시크릿 제외) | 없음 | `{IS_PAPER_TRADING, KIS_SERVER, KIS_APP_KEY_SET, ...}` | 500 | 시크릿은 `_SET` 여부만 |
| 5 | 설정 | POST | `/api/config` | 설정 저장 + 세션 리셋 | body: `{key:value}` | `{message}` | 500 | 시크릿 빈값=미변경 |
| 6 | 설정 | GET | `/api/config/secret/{key}` | 시크릿 평문 단건 조회 | path: `key` | `{key, value, set}` | 400/500 | 화이트리스트(KIS 키/계좌)만 |
| 7 | 적립설정 | GET | `/api/dca/config` | 템플릿+월배정+현재월 조회 | 없음 | `{templates, monthMap, currentMonth, activeTemplateId}` | 500 | |
| 8 | 적립설정 | PUT | `/api/dca/config` | 템플릿·월배정 저장 | body: `templates[]*`, `monthMap` | `{message, templates, monthMap}` | 400/500 | id 중복·예산·수량 검증 |
| 9 | 주문 | POST | `/api/order/dca-run` | 적립(DCA) 사이클 백그라운드 실행 | 없음 | `202 {message}` | — | 외부 크론용·즉시 202·결과는 로그/메일 |
| 10 | 주문 | POST | `/api/order/manual` | 신호 무관 즉시 매수/매도 | body: `ticker*`, `qty*`, `orderType`, `price`, `acknowledgeTax`, `ytdRealizedGainKrw` | `{message, orderNo, ...}` | 400/409/502/503/500 | ⚠️ 유일한 실주문 경로 · 매도 보유·절세 가드(409=과세 미확인, `taxEstimate` 첨부) |
| 11 | 주문 | GET | `/api/order/sell-preview` | 매도 예상 양도세 계산(주문 X) | query: `ticker*`, `qty*`, `price`, `ytd` | `SellTaxEstimateDto` | 400/503/500 | 정보 제공용 |
| 12 | 시세 | GET | `/api/price/{ticker}` | 현재가+환율 환산 | path: `ticker` | `{ticker, priceUsd, exchangeRate, priceKrw}` | 400/404/500 | 404=미존재/조회 실패(티커 검증 겸용) |
| 13 | 포트폴리오 | GET | `/api/portfolio/holdings` | 보유 종목 | 없음 | `{holdings}` | 500 | |
| 14 | 포트폴리오 | GET | `/api/portfolio/summary` | 보유+예수금+환율+계좌모드 | 없음 | `{holdings, cashBalance, exchangeRate, accountMode, accountMasked}` | 500 | accountMode: SIM/PAPER/LIVE |
| 15 | 이력 | GET | `/api/history/trades` | 매매 내역 | query: `limit`=50 | `{trades}` | 500 | |
| 16 | 이력 | GET | `/api/history/logs` | 시스템 로그(TB_SYSTEM_LOG) | query: `date`, `lines`=200 | `{date, totalLines, logs}` 또는 `{message, availableDates}` | 500 | |
| 17 | 점검 | GET | `/api/test/send-test-email` | 메일 발송 설정 점검 | 없음 | 평문 문자열 | 500 | Resend · 실패 원인이 500 본문에 담김 |
| 18 | 헬스 | GET | `/api/health` | 경량 헬스체크 | 없음 | `Healthy` | — | **인증 불요** |

> `*` = 필수 · `=값` = 기본값 · ⚠️ = 운영 주의(실주문·가드 동작)
>
> 2026-07-30 제거: `GET /api/macro/briefing`(화면 미배선으로 소비자 0), `POST /api/test/buy`(10번 `manual`에서
> 가드만 뺀 중복 경로이자 실전 모드에서 자기 차단). 실주문 경로는 10번 `manual` 하나뿐이다.

## 참고
- 상세 요청/응답: `Documents/reference/API_REFERENCE.md`
- 인터랙티브 명세: `/swagger`
- 본 문서는 `Controllers/` 변경 시 함께 갱신한다.
