# AutoInvesting API 일람표 (한 줄 = 한 API)

> 시트처럼 한눈에 보는 평면 표. 상세 요청/응답 예시는 [`API_REFERENCE.md`](API_REFERENCE.md), 인터랙티브 명세는 `/swagger` 참조.
> 인증: 헬스체크(`/api/health`) 외 **모든 엔드포인트는 `x-api-key` 헤더 필수**.

| # | 그룹 | Method | 경로 | 설명 | 요청 (파라미터/본문) | 주요 응답(200) | 오류 코드 | 비고 |
|---|------|--------|------|------|----------------------|----------------|-----------|------|
| 1 | 주문 | POST | `/api/order/execute` | 활성 전략 스마트 주문 즉시 실행 | 없음 | `{message, results[]}` | 400/503/500 | |
| 2 | 주문 | POST | `/api/order/daily-run` | 일일 전체 사이클 백그라운드 실행 | 없음 | `202 {message}` | — | 외부 크론용·즉시 202·결과는 로그/메일 |
| 3 | 주문 | POST | `/api/order/manual` | 신호 무관 즉시 매수/매도 | body: `ticker*`, `qty*`, `orderType`, `price` | `{message, orderNo, ...}` | 400/503/502/500 | 검증용·체결 시 이력 저장 |
| 4 | 주문 | GET | `/api/order/analyze/{ticker}` | 단일 종목 분석(주문 X) | path: `ticker`; query: `strategy`=MEAN_REVERSION | `{signal, indicators, conditions, advisoryNotes}` | 500 | 합의 신호 포함 |
| 5 | 설정 | GET | `/api/config` | 운영 설정 조회(시크릿 제외) | 없음 | `{IS_PAPER_TRADING, ACTIVE_STRATEGY, ...}` | 500 | |
| 6 | 설정 | POST | `/api/config` | 설정 저장 + 세션 리셋 | body: `{key:value}` | `{message}` | 500 | |
| 7 | 설정 | GET | `/api/config/gemini-models` | 사용 가능 Gemini 모델 목록 | 없음 | `{models[]}` | 500 | 키 미설정 시 `{models:[], error}` |
| 8 | 전략 | GET | `/api/strategy/summary` | 전략 요약 목록 | 없음 | `[summary]` | 500 | |
| 9 | 전략 | GET | `/api/strategy/adaptive-status` | 적응형 임계값 작동 진단 | query: `name` | `{strategy, items[]}` | 500 | 미지정 시 ACTIVE_STRATEGY |
| 10 | 전략 | GET | `/api/strategy/{name}` | 특정 전략 종목 목록 | path: `name`=사용자정의 | `[StrategyDto]` | 500 | |
| 11 | 전략 | POST | `/api/strategy/{name}` | 전략 저장(덮어쓰기) | path: `name`; body: `StrategyDto[]` | `{message}` | 400/500 | |
| 12 | 전략 | DELETE | `/api/strategy/{name}` | 전략 삭제 | path: `name` | `{message}` | 500 | |
| 13 | 모니터링 | GET | `/api/monitoring/summary` | 요약 핵심 지표 | query: `days`=30 | `{todayTotalTokens, evaluatedCount, ...}` | 500 | |
| 14 | 모니터링 | GET | `/api/monitoring/performance` | 최근 AI 판단 성과 | query: `limit`=50 | `[성과]` | 500 | |
| 15 | 모니터링 | GET | `/api/monitoring/tokens/by-agent` | 에이전트별 토큰/비용 | query: `days`=30 | `{periodDays, agents[]}` | 500 | |
| 16 | 모니터링 | GET | `/api/monitoring/tokens/daily` | 일자별 토큰/비용 | query: `days`=14 | `{periodDays, daily[]}` | 500 | |
| 17 | 모니터링 | GET | `/api/monitoring/agent-accuracy` | 에이전트 실측 적중률 | query: `horizonDays`=7 | `{horizonDays, agents}` | 500 | Phase 5-d |
| 18 | 모니터링 | GET | `/api/monitoring/weight-abtest` | 합의 가중치 A/B | query: `horizonDays`=7 | `{schemes, note}` | 500 | ⚠️ 검증용·미반영 |
| 19 | 모니터링 | GET | `/api/monitoring/adaptive-threshold` | 종목 적응형 임계값 근거 | query: `ticker*` | `{buyThreshold, sellThreshold, ...}` | 400/500 | |
| 20 | 이력 | GET | `/api/history/trades` | 매매 내역 | query: `limit`=50 | `[trades]` | 500 | |
| 21 | 이력 | GET | `/api/history/logs` | 시스템 로그 | query: `date`, `lines`=200 | `{date, logs[]}` 또는 `{availableDates[]}` | 500 | |
| 22 | 분할매도 | GET | `/api/sellplan` | 활성 플랜 목록 | 없음 | `[SellPlanDto]` | 500 | |
| 23 | 분할매도 | POST | `/api/sellplan` | 플랜 생성 | body: `SellPlanDto` | `SellPlanDto` (planId) | 500 | status/soldQty 서버 설정 |
| 24 | 분할매도 | DELETE | `/api/sellplan/{id}` | 플랜 취소 | path: `id` | `{Message}` | 404/500 | 대문자 `Message` |
| 25 | 포트폴리오 | GET | `/api/portfolio/holdings` | 보유 종목 | 없음 | `[holdings]` | 500 | |
| 26 | 포트폴리오 | GET | `/api/portfolio/summary` | 보유+예수금+환율 | 없음 | `{holdings, cashBalance, exchangeRate}` | 500 | |
| 27 | 퀀트 | GET | `/api/quant/analyze/{ticker}` | 실시간 퀀트 분석 | path: `ticker`; query: `strategyType` | `{currentPrice, indicators, analysis}` | 400/404/500 | 합의 없이 순수 퀀트 |
| 28 | 백테스트 | POST | `/api/backtest/run` | 과거 데이터 전략 검증 | body: `ticker*`, `strategyType`, `days`, `initialCapital`, `buyThreshold`, `sellThreshold` | `{totalReturnPct, maxDrawdownPct, winRatePct, trades[]}` | 400/500 | days 최대 1000 |
| 29 | 시뮬 | POST | `/api/sim/generate-training-data` | SIM 학습데이터 생성 | body: `GenerateRequest` | `{insertedCount, tickerCount, perTicker}` | 500 | DATA_SOURCE=SIM |
| 30 | 시뮬 | GET | `/api/sim/verify-training-data` | SIM 데이터 적중률/가중치 검증 | query: `horizonDays`=7 | `{snapshotCount, agentAccuracy, weightAbTest}` | 500 | |
| 31 | 테스트 | POST | `/api/test/inject-mock` | QQQ 목업 30건 주입 | 없음 | `string` | — | ⚠️ TB_MARKET_SNAPSHOT QQQ DELETE 후 삽입 |
| 32 | 테스트 | GET | `/api/test/test-adaptive` | 적응형+분석 테스트 | query: `ticker`=QQQ | `{adaptiveThreshold, analysisResult}` | — | |
| 33 | 테스트 | POST | `/api/test/buy` | 즉시 매수 | query: `ticker`=QQQM, `qty`=1 | `{orderNo, ...}` | 400/500 | ⚠️ 실제 주문 발생 |
| 34 | 테스트 | POST | `/api/test/send-report` | 테스트 일일 보고서 메일 | 없음 | `{message}` | 500 | |
| 35 | 테스트 | GET | `/api/test/send-test-email` | 테스트 이메일(원인 노출) | 없음 | `{ok, message}` | 503/500 | reason: CONFIG_MISSING/SEND_ERROR |
| 36 | 테스트 | GET | `/api/test/health` | 의존성+운영모드 헬스체크 | 없음 | `{ok, mode, email, db, broker}` | 503 | 시크릿 미노출 |
| 37 | 헬스 | GET | `/api/health` | 경량 헬스체크 | 없음 | `Healthy` | — | **인증 불요** |

> `*` = 필수 · `=값` = 기본값 · ⚠️ = 운영 주의(실주문/DB변경/검증용)
