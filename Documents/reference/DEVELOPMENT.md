---
title: 개발 진척도 (CHANGELOG)
date: 2026-07-23
company: [개인]
tags: [개발이력, CHANGELOG, Phase6, DCA적립]
status: draft
---

# 개발 진척도 (CHANGELOG)

## 개요
> AutoInvesting 프로젝트의 개발 진행 상황을 기록하는 변경 이력(CHANGELOG)이다. 새 개발자가 현재 상태와 다음 작업을 파악할 수 있도록 유지한다.

## 현재 상태: Phase 6 완료 — DCA 적립 코어 전환 ✅

- **Phase 1** (기반): ✅ 완료
- **Phase 2** (엔진 코어 + 배분 UI): ✅ 완료
- **Phase 2.5** (퀀트 엔진 모듈): ✅ 완료
- **Phase 2.6** (구조 리팩토링): ✅ 완료
- **Phase 3** (KIS 실거래 연동): ✅ 완료
- **Phase A** (프로젝트 정비/안정화): ✅ 완료
- **Phase B/C** (운영 안정성 및 확장): ✅ 완료
- **Phase 4-a~e** (AI 시장분석 엔진 / 확률 기반 합의 스코어링): ✅ 완료 → ⚠️ **Phase 6에서 제거**
- **Phase 5-a~d** (적응형 임계값 / AI 성과·토큰 모니터링 / 성과 피드백 루프): ✅ 완료 → ⚠️ **Phase 6에서 제거**
- **Phase 6** (판단 레이어 제거, DCA 적립 코어 전환): ✅ **완료**
- **Phase 6+** (이후 추가된 정보·보조 기능): **Auth**(단일 관리자 인증·전역 필터), **Tax**(매도 양도세 추정 — `sell-preview`), **Price**(현재가 조회·티커 검증). ⚠️ Tax는 매수 의사결정에 값을 넘기지 않음(판단 레이어 아님). **Macro**(FRED 거시지표 브리핑)는 화면에 배선되지 않아 소비자가 0이어서 2026-07-30 정리에서 제거됨.

> ⚠️ **Phase 4~5의 판단(타이밍) 기능은 Phase 6에서 전부 제거되었습니다.** 아래 Phase 4~5 변경 이력은
> **역사적 기록(과거에 그렇게 구현되었음)**으로 보존된 것이며, 현재 코드베이스에는 해당 클래스·엔드포인트·화면이
> **존재하지 않습니다.** 현재 동작은 본 문서 최상단의 "Phase 6 상세 변경 이력"을 기준으로 보세요.

## ⚠️ 실거래 전환 (과매수 방지: 구현 완료 / 계좌 전환: 운영 작업)

> 과거 문제: 예산은 월 단위(기본 100만원)인데 `DcaAccumulationEngine.AccumulateAsync`는 호출될 때마다
> 예산 전액을 새로 소진했다. 크론이 매일 돌면 **월 예산을 매일 소진 → 약 30배 과매수**가 된다.

### ✅ 코드로 해결됨 — 월 1회 멱등 가드 (260701)
- `DailyExecutionService.RunDcaCycleAsync`에 **당월 멱등 가드** 추가:
  - `TB_APP_CONFIG`의 `DCA_LAST_RUN_MONTH`("yyyy-MM", KST 기준)와 현재 월을 비교해, 같으면 매수 스킵.
  - **체결 1건 이상 성공 시에만** 마커를 저장 → 그 달 남은 호출은 모두 스킵.
  - 체결 0건(휴장·예수금 부족·전량 실패)이면 마커를 남기지 않아 **다음 날 자동 재시도**.
  - 거래이력이 아니라 전용 마커를 쓰는 이유: 수동 단일 매수가 월 적립을 오판하지 않게 하기 위함.
  - 수동 실행(`POST /api/order/dca-run`)에도 동일 가드 적용. 단 **260804부터 `?force=true`로 우회 가능** — 가드는 크론의 매일 재호출을 막기 위한 것이므로, 사람이 화면에서 추가 적립을 누른 경우(프론트가 항상 `force=true` 전송)는 의도된 중복 매수로 본다. 크론은 이 파라미터를 붙이지 않는다.
- `.github/workflows/daily-run.yml` cron: `40 14 1-31 * *`(매일 KST 23:40).
  - 크론은 매일 호출하지만 실제 매수는 가드가 "월 1회"만 허용 → **월초부터 시도해 처음 성공하는 날 1회만 적립**.
  - "월초 최대한 빨리, 안 되면 될 때까지 매일 재시도" 정책.

### 남은 운영 작업 (실계좌 전환 시 사용자 수행 — 코드 아님)
- Render 환경변수에 **실전** `KIS_APP_KEY`/`KIS_APP_SECRET`/`KIS_ACCOUNT_NO`/`KIS_ACCOUNT_PROD` 설정(모의와 별도 발급).
- `IS_PAPER_TRADING=0` 설정 → `SessionManager`가 실전 도메인·실전 tr_id로 자동 분기.
- 전환 직후 크론을 끄고 `workflow_dispatch` 수동 1회로 소액 체결·잔고 확인 후 자동화 재개 권장.

## Phase 6 상세 변경 이력 — 판단 레이어 제거 & DCA 적립 코어 전환

### 핵심: "퀀트/AI로 타이밍을 판단" → "월별 템플릿의 종목별 고정 수량을 매수하는 적립(DCA)"

정직한 백테스트(2012~현재) 결과 **퀀트/AI 타이밍 판단이 단순 적립식(DCA)에 2.7~4배 열세**였고,
완벽한 타이밍조차 평균 대비 연 +0.3~0.9%에 불과(타이밍은 잘해야 본전)함이 검증되었습니다.
이에 따라 **판단 레이어 전체를 제거**하고, 여러 **매수 템플릿**(종목별 고정 수량 + 예산)을
정의해 **월별로 배정**하고, 현재 월에 배정된 템플릿의 종목별 고정 수량을 매 사이클 그대로 매수하는
**DCA 적립 코어**로 전환했습니다. 시스템의 가치는 "판단"이 아니라 **"자동화"**에 있습니다.

> 참고: 최초 전환은 "목표비중을 향해 정수 단위 매수(DCA_TARGETS)" 모델이었으나, 이후
> "매수 템플릿 + 월별 배정(DCA_TEMPLATES/DCA_MONTH_MAP)" 모델로 발전했습니다. 아래 설명은
> **현재 동작(템플릿 모델)** 기준입니다.

```text
변경 전 (Phase 5):
  DailyExecutionService.RunDailyCycleAsync
    → SmartOrderEngine → 퀀트(QuantIndicator/QuantFilter) + AI(차트/펀더멘털) + 합의 스코어링
    → BuyProbability ≥ 임계값일 때만 매수

변경 후 (Phase 6, 현재):
  DailyExecutionService.RunDcaCycleAsync
    → 월 1회 멱등 가드(DCA_LAST_RUN_MONTH) 확인 — 당월 적립 완료 시 스킵
    → DcaSettings.Load → SelectTemplate(현재 KST 월에 배정된 템플릿 선택)
    → DcaAccumulationEngine.AccumulateAsync → 템플릿의 종목별 고정 수량을 그대로 매수
    → TradeHistoryDAO 기록 + 이메일 보고서  (판단·타이밍 없음)
```

### 6-1. 신규 파일 (3건)

| 파일 | 설명 |
|------|------|
| `Core/DcaAccumulationEngine.cs` | 적립식 매수 엔진. `PlanPurchases`(순수 함수 — 현재가가 있는 종목의 고정 수량 매수 계획 + 총 매수금액 산출) + `AccumulateAsync`(현재가/환율 조회 → 계획 → 주문 → `TradeHistoryDAO` 기록). 판단/타이밍 없음 |
| `Core/DcaSettings.cs` | 매수 템플릿·월배정·예산의 단일 읽기/쓰기 지점. `SelectTemplate`(순수 함수 — 월→템플릿 선택)로 현재 월 템플릿을 고름. 우선순위 DB(`TB_APP_CONFIG`: `DCA_TEMPLATES`/`DCA_MONTH_MAP` JSON) → 레거시 `DCA_QTYS`/`DCA_BUDGET_KRW`/`appsettings.json` `Dca` 섹션 폴백(자동 '기본' 템플릿 이관) |
| `Controllers/DcaController.cs` | `GET/PUT /api/dca/config` — 매수 템플릿·월배정 조회·저장 (GET: templates/monthMap/currentMonth/activeTemplateId, PUT: templates+monthMap). 저장값은 DB 기록, 다음 사이클 반영 |

### 6-2. 수정 파일 (3건)

| 파일 | 변경 내용 |
|------|----------|
| `Core/DailyExecutionService.cs` | `RunDcaCycleAsync`만 유지 — 월 1회 멱등 가드(`DCA_LAST_RUN_MONTH`) → 로그인 → `DcaSettings.Load` → `AccumulateAsync` → 이메일 보고서. (구 `RunDailyCycleAsync`/AI 평가/일일 보고서 제거) |
| `Controllers/OrderController.cs` | `POST /api/order/dca-run`(적립 사이클, 202 즉시 반환) + `POST /api/order/manual`(판단 없는 수동 매수/매도, SELL 시 보유수량·절세 서버 가드) + `GET /api/order/sell-preview`(매도 양도세 프리뷰)만 남김. (구 `execute`/`analyze`/`daily-run` 제거) |
| `appsettings.json` | `Trading`/`Smtp`/`Resend`/`Kis`/`Security`/`Dca`/`Tax` 섹션 유지. `Rebalance`/`Consensus`/`FxAdvisor`/`Ai` 섹션 제거. `Dca = { MonthlyBudgetKrw, Quantities:{SPLG:3,QQQM:2,SCHD:5,GLD:1} }` (레거시 폴백용 — 실동작은 DB의 `DCA_TEMPLATES`/`DCA_MONTH_MAP`). `Smtp`는 `SenderName`/`AdminEmail` 폴백 2개만 유지(발송은 Resend HTTP API) |

### 6-3. 제거된 파일·개념

판단(타이밍) 레이어 전체가 코드베이스에서 삭제되었습니다.

| 분류 | 제거 대상 |
|------|----------|
| Core 엔진/분석 | `SmartOrderEngine`, `Core/Quant/*` 전부(`QuantIndicator`, `QuantFilter`, `AdaptiveThresholdEngine`, `PerformanceFeedbackEngine`, `BacktestEngine`, `RebalancingEngine`, `SellStrategyManager`), `Core/Advisors/*` 전부, `AiMarketAnalyzer`, `GeminiMarketAnalyzer`, `IMarketAnalyzer`, `IMcpDataProvider`, `AllocationEngine`, `Utils/PromptBuilder` |
| Data DAO/DTO | `AiPerformanceDAO`, `MarketSnapshotDAO`, `SellPlanDAO`, `TokenUsageDAO`, `StrategyDAO` 및 관련 DTO(`ConsensusScoreDto`, `IndicatorDto`, `AdvisoryNoteDto`, `AgentAccuracyDto`, `AiPerformanceDto`, `BacktestResultDto`, `MarketSnapshotDto`, `SellPlanDto`, `TokenUsageDto`/`SummaryDto`, `WeightSchemeResultDto`, `StrategyDto`) |
| Controllers | `BacktestController`, `MonitoringController`, `QuantController`, `SellPlanController`, `StrategyController` |
| 프론트 페이지 | `Backtest`, `Monitoring`, `SellPlanManager`, `Strategy` |
| 개념 | AI 투자위원회/3자 합의, `CalculateConsensusScore`, 가중치 임계값(Consensus), 적응형 임계값, 성과 피드백 루프, 토큰 비용 모니터링, 차트AI/펀더멘털AI, 환헤지 어드바이저(FxAdvisor), 리밸런싱 |

### 6-4. 유지된 것 (자동화 인프라)

`IBrokerClient`/`KisBrokerClient`/`SimBrokerClient`, `SessionManager`(이제 브로커 생명주기만 — AI analyzer 분기 제거),
`TradeHistoryDAO`, `NotificationService`(Resend HTTP API — Render의 SMTP 포트 차단 우회), `ExchangeRateService`, `DBManager`/`AppConfigManager`,
`ConfigController`, `PortfolioController`, `HistoryController`, `TestController`(send-test-email만 — 실주문 경로 없음). 외부 크론잡이 `dca-run`을 호출하는 구조.

### 6-5. 프론트엔드 재구성

| 페이지 | 경로 | 설명 |
|--------|------|------|
| Dashboard | `/` | 현황 조회 (유지) |
| DcaConfig | `/dca-config` | 적립 설정 — 매수 템플릿(추가/복제/삭제/종목 수량·티커검증·예산) + 월별 배정 그리드 편집 (신규) |
| Order | `/order` | 적립 실행 + 수동 주문 (재작성) |
| History | `/history` | 거래 내역 (유지) |
| Settings | `/settings` | 환경 설정 (유지) |

네비게이션: **대시보드 / 적립 설정 / 주문·적립 / 거래 내역 / 설정**

### 6-6. 참고 — 레거시 데이터 보존

`TB_MARKET_SNAPSHOT` 테이블은 **과거 데이터 보존을 위해 `Data/sql/create_tables.sql`의 DDL로만 남아 있고,
`MarketSnapshotDAO` 제거에 따라 현재는 어디서도 기록·조회하지 않습니다.** 기존 문서의
"AI 학습용 누적 데이터" 설명은 모두 **"과거(레거시) 데이터, 현재 미사용"**으로 해석하면 됩니다.

`DBManager`의 관련 ALTER 마이그레이션 코드는 2026-07-30 정리에서 제거되었습니다(`create_tables.sql`이
컬럼을 이미 정의해 중복이었음). 현재 마이그레이션 자동 실행 경로는 없습니다.

### 6-7. 이후 보강 (매수 템플릿 · 실거래 가드 · 단위 테스트)

| 날짜 | 내용 |
|------|------|
| 260629 | 단일 목표비중 → **매수 템플릿 + 월별 배정** 모델로 발전(`DcaTemplate` DTO, `DCA_TEMPLATES`/`DCA_MONTH_MAP`, `DcaConfig.jsx` 재작성). 레거시 단일 설정은 '기본' 템플릿으로 자동 이관 |
| 260630 | 수동주문 보유종목 연동(SELL 서버 가드·보유수량 상한), 대시보드 계좌 모드 배지·마스킹 계좌 표시, 요약/보유 새로고침 분리 |
| 260701 | **실거래 전환 대비 월 1회 멱등 가드**(`DCA_LAST_RUN_MONTH`) + 크론 `40 14 1-31 * *`(매일 시도, 처음 성공하는 날 1회 적립) |
| 260702 | **단위 테스트 프로젝트 신설**(`Tests/`, xUnit). `PlanPurchases`(7건)·`SelectTemplate`(5건) 순수 함수 검증. 이를 위해 `DcaSettings`의 월→템플릿 선택 로직을 `SelectTemplate` 순수 함수로 분리(동작 불변) |
| 260730 | **죽은 코드·미배선 기능 정리**. Macro/FRED 스택 일괄 제거(참조 0), `POST /api/test/buy` 제거(`manual`과 중복·실전 자기차단), `Templates/DailyReportTemplate.html`·미사용 프론트 자산(`App.css`·`assets/*`·`icons.svg`) 제거, `DBManager` ALTER 마이그레이션 9건+`RunMigration` 제거, `create_tables.sql`에서 `TB_ASSET_MASTER`·`TB_INVEST_STRATEGY`·죽은 앱설정 시드 제외, 죽은 CSS 클래스 제거, 알림박스 `.alert` 공용 클래스화, `ExchangeRateService` 문자열 파서 2개 → `ParseKrwRate` 순수함수 1개(+테스트 4건, 총 40건). 상세: `Documents/worklog/[2026-07-30] 01_죽은 코드 미배선 기능 정리.md` |
| 260730 | **거래이력 주문번호(`ORDER_NO`) 저장 배선**. 브로커가 준 주문번호(KIS `ODNO`)가 DTO까지 채워졌는데 `TradeHistoryDAO`의 INSERT/SELECT 컬럼에 없어 DB에 저장되지 않고 History 화면 주문번호 칼럼이 항상 빈칸이었다. 컬럼은 스키마에 이미 있어 변경 없이 배선만 추가. 증권사 계좌와 우리 기록을 잇는 유일한 키이며, 지정가 주문(`ORD_DVSN=00`)을 접수 시점에 `FILLED`로 기록하는 현 구조에서 미체결 추적의 실마리이기도 하다 |

> 테스트 실행: `dotnet test Tests/AutoInvest.Tests.csproj` (net8.0, xUnit). 메인 웹 프로젝트는
> `AutoInvest.csproj`에서 `Tests\**`를 컴파일 대상에서 제외해 분리되어 있습니다.

> 📌 **이하 Phase 5-d ~ Phase 4-a 및 그 이전의 변경 이력은 역사적 기록입니다.** 여기서 설명하는
> 퀀트/AI 판단 관련 클래스·엔드포인트·화면은 **Phase 6에서 모두 제거되어 현재 코드베이스에 존재하지
> 않습니다.** 과거 어떤 시도를 했고 왜 접었는지를 이해하기 위한 보존용 기록으로만 참고하세요.
> `DBManager`의 ALTER 마이그레이션 코드도 2026-07-30 정리에서 제거되었으므로, 아래에 등장하는
> 마이그레이션 기록은 **당시 구현이었을 뿐 현재 코드에는 없습니다.**

## Phase 5-d 상세 변경 이력 — 성과 기반 피드백 루프 & 합의 가중치 A/B 검증 (⚠️ Phase 6에서 제거됨)

### 핵심: "수집·시각화에서 멈춰 있던 성과 데이터" → "의사결정에 되먹임하는 학습 루프"

Phase 5-b/c까지 AI 성과·토큰 데이터를 수집·시각화했으나, 누적된 데이터가 의사결정에 피드백되지 않았습니다.
또한 적응형 임계값(5-a)은 매수에만, 그것도 BuyProbability **분포**(백분위)만 사용했고 **실제 승패**는 반영하지 않았습니다.
Phase 5-d에서 (1) 스냅샷에 에이전트별 방향 신호를 보강하고, (2) 실측 적중률·가중치 A/B를 산출하며, (3) 매도 적응형 임계값을 추가해 피드백 루프를 닫았습니다.

```text
[수집·시각화 (5-b/c)]              [피드백 분석 (5-d)]
TB_MARKET_SNAPSHOT ──► (에이전트별 신호 보강) ──► PerformanceFeedbackEngine
  · QUANT_SIGNAL                                    ├── 에이전트별 실측 적중률 (7일 forward return 대조)
  · CHART_AI_SIGNAL                                 └── 합의 가중치 A/B 백테스트 (4개 조합 가상 성과)
  · FUND_AI_SIGNAL                                        ↓ /api/monitoring/agent-accuracy, weight-abtest
AdaptiveThresholdEngine.GetSellThreshold ◄── SELL_PROBABILITY 분포   ─► Monitoring.jsx "가중치 검증" 탭
```

#### 안전 제약 (준수)

- `TB_MARKET_SNAPSHOT`/`TB_AI_PERFORMANCE`는 **읽기 전용** — 수정·삭제 없음
- 기존 `QuantFilter`/`CalculateConsensusScore` **직접 수정 없음** — A/B 재계산은 별도 엔진에서 동일 공식으로 수행
- A/B 결과는 **리포트 전용** — 실제 매매 가중치(appsettings.json)에 자동 반영하지 않음
- DB 변경은 `ALTER TABLE ADD COLUMN`만 (하위 호환, 기존 데이터 보존)

### 5-d 신규 파일 (3건)

| 파일 | 설명 |
|------|------|
| `Core/Quant/PerformanceFeedbackEngine.cs` | 성과 기반 피드백 엔진 — forward return 페어링, 에이전트별 실측 적중률(`GetAgentAccuracy`), 합의 가중치 A/B 백테스트(`RunWeightAbTest`). 읽기 전용 분석 |
| `Data/DTO/AgentAccuracyDto.cs` | 에이전트별 적중률 집계 결과 (BuySignals, SellSignals, SampleCount, HitCount, WinRate) |
| `Data/DTO/WeightSchemeResultDto.cs` | 가중치 조합별 A/B 결과 (가중치, TriggerCount, HitCount, WinRate, AvgForwardReturnPct) |

### 5-d 수정 파일 (8건)

| 파일 | 변경 내용 |
|------|----------|
| `Data/DTO/MarketSnapshotDto.cs` | 에이전트별 방향 신호 3필드 추가 (QuantSignal, ChartAiSignal, FundAiSignal) |
| `Data/DBManager.cs` | Phase 5-d 마이그레이션 3건 — ALTER TABLE ADD COLUMN (QUANT_SIGNAL/CHART_AI_SIGNAL/FUND_AI_SIGNAL, TEXT) |
| `Data/DAO/MarketSnapshotDAO.cs` | Insert/Select에 3개 신호 컬럼 반영, `GetRecentAll`(전 종목 분석용)·`GetHistoricalSellProbabilities`(매도 임계값용)·`MapSnapshot`(공통 매핑) 추가 |
| `Core/Quant/AdaptiveThresholdEngine.cs` | `GetSellThreshold` 추가, 백분위 산출 로직을 `ComputeThreshold`로 추출해 매수/매도 공유 |
| `Core/SmartOrderEngine.cs` | `SmartOrderResult`에 `QuantSignal` 추가, 매도 판정에 적응형 임계값 적용, 스냅샷 저장 시 에이전트별 신호 기록 (의사결정 공식 불변) |
| `Controllers/MonitoringController.cs` | `agent-accuracy`·`weight-abtest`·`adaptive-threshold` 3개 읽기 전용 엔드포인트 추가 |
| `Frontend/src/pages/Monitoring.jsx` | "가중치 검증" 탭 추가 — 에이전트별 적중률 + 가중치 A/B 표 |
| `.agents/rules/project_overview.md`, `.agents/rules/architecture.md` | Phase 표·핵심 추상화 갱신 |

## Phase 5-c 상세 변경 이력 — AI 모니터링 대시보드 UI

### 핵심: "수집만 되던 AI 성과·토큰 데이터" → "대시보드에서 조회·시각화"

Phase 5-b에서 `SmartOrderEngine`/`DailyExecutionService`가 AI 판단 성과(`TB_AI_PERFORMANCE`)와
토큰 사용량(`TB_TOKEN_USAGE`)을 적재하기 시작했으나, 이를 조회하는 API·화면이 없었습니다.
Phase 5-c에서 읽기 전용 조회 경로와 프론트엔드 모니터링 페이지를 신설하여 기능을 완성했습니다.

```text
[데이터 수집 (Phase 5-b)]                [데이터 조회·시각화 (Phase 5-c)]
SmartOrderEngine ──┐                      MonitoringController
DailyExecutionSvc ─┴─► TB_AI_PERFORMANCE  ─► /api/monitoring/performance ─► Monitoring.jsx
                       TB_TOKEN_USAGE      ─► /api/monitoring/tokens/*   ─► (AI 성과 / 토큰 비용 탭)
                                           ─► /api/monitoring/summary
```

#### 비용 추정 공식 (Gemini 1.5 Flash 공식 단가, 128k 이하 컨텍스트 기준)

```text
추정 비용(USD) = 프롬프트 토큰 / 1M × $0.075 + 완성 토큰 / 1M × $0.30
```

### 5-c 신규 파일 (3건)

| 파일 | 설명 |
|------|------|
| `Controllers/MonitoringController.cs` | AI 성과/토큰 비용 조회 API — summary, performance, tokens/by-agent, tokens/daily. Gemini 단가 기반 비용 추정 포함 |
| `Data/DTO/TokenUsageSummaryDto.cs` | 토큰 집계 결과 DTO — `AgentTokenSummaryDto`(에이전트별), `DailyTokenUsageDto`(일자별) |
| `Frontend/src/pages/Monitoring.jsx` | 모니터링 페이지 — 요약 카드 4종 + 탭(AI 성과 / 토큰 비용) |

### 5-c 수정 파일 (3건)

| 파일 | 변경 내용 |
|------|----------|
| `Data/DAO/TokenUsageDAO.cs` | `GetTokenSums`(비용 추정용 합계), `GetUsageByAgent`(에이전트별 집계), `GetDailyUsage`(일자별 추이) 조회 메서드 추가 |
| `Data/DAO/AiPerformanceDAO.cs` | `GetRecent`(최근 성과 목록, 평가 완료/대기 모두 포함) 조회 메서드 추가 |
| `Frontend/src/App.jsx` | `/monitoring` 라우트 및 "AI 모니터링" 네비게이션 링크 추가 |

### 5-c 버그 수정 (1건)

| 파일 | 변경 내용 |
|------|----------|
| `Frontend/src/pages/Strategy.jsx` | 전략 수정 화면 진입 시 미선언 변수(`editStrategyName`) 참조로 발생하던 `ReferenceError`(빈 페이지) 제거, 종목 수 표시 키 `TickerCount` → `tickerCount`(camelCase) 수정 |

## Phase 4-e 상세 변경 이력 — 확률 기반 합의 스코어링 시스템

### 핵심 변경: "3자 만장일치 합의(0 or 1)" → "가중치 × 확신도 확률 합산(0.0~1.0)"

Phase 4-d의 만장일치(CombineSignals) 방식을 확률 기반 가중 합산(CalculateConsensusScore)으로 교체했습니다.
매매 판단의 근거를 수치로 투명하게 추적할 수 있으며, Phase 5 종목별 적응형 임계값의 기초 데이터를 축적합니다.

```text
변경 전 흐름 (Phase 4-d):
  SmartOrderEngine → [퀀트] + [차트AI] + [펀더멘털AI] → 만장일치(CombineSignals) → 0 or 1

변경 후 흐름 (Phase 4-e):
  SmartOrderEngine → [퀀트] + [차트AI] + [펀더멘털AI]
                         ↓ 가중치 × 확신도 합산 (CalculateConsensusScore)
                    BuyProbability = 0.40(퀀트) + 0.30×0.76(차트) + 0.30×0.62(펀더멘털) = 81.4%
                         ↓ ≥ 65% (임계값)?
                    → BUY 실행 ✅
```

#### 확률 합산 공식

```text
BuyProbability = QUANT_WEIGHT(BUY 충족 시 고정) + CHART_AI_WEIGHT × 차트확신도 + FUND_AI_WEIGHT × 펀더멘털확신도

기본 가중치: 퀀트 40% / 차트AI 30% / 펀더멘털AI 30% (appsettings.json 설정)
임계값: BUY_THRESHOLD = 0.65 / SELL_THRESHOLD = 0.65

퀀트 1차 관문 수식 자동 보장:
  퀀트 HOLD → QUANT_WEIGHT=0 → 최대 60% → 임계값(65%) 자동 미달
```

#### 로그 출력 형식

```text
[SmartOrder] [MEAN_REVERSION] QQQ 최종 판정: BUY ✅
  ├── 퀀트       : BUY  → +40.0%
  ├── 차트AI     : BUY (확신도:0.76) → +22.8%
  └── 펀더멘털AI : BUY (확신도:0.62) → +18.6%
  ─────────────────────────────────────
  매수 확률: 81.4% ≥ 65.0% (임계값) → 매수 실행
```

### 4-e 신규 파일 (1건)

| 파일 | 설명 |
|------|------|
| `Data/DTO/ConsensusScoreDto.cs` | 확률 분해 결과 DTO — BuyProbability, SellProbability, 에이전트별 기여도, 임계값 달성 여부 |

### 4-e 수정 파일 (6건)

| 파일 | 변경 내용 |
|------|----------|
| `Core/SmartOrderEngine.cs` | `CombineSignals()` → `CalculateConsensusScore()` 교체, `SmartOrderResult`에 `ConsensusScore` 필드 추가, 확률 분해 로그 형식 적용, `SaveMarketSnapshot()`에 AI 점수 저장 추가 |
| `Data/DTO/MarketSnapshotDto.cs` | `BuyProbability`, `SellProbability`, `ChartAiScore`, `FundAiScore` 필드 추가 |
| `Data/DAO/MarketSnapshotDAO.cs` | Insert/Select SQL에 4개 컬럼 추가, NULL 안전 읽기 (기존 데이터 호환) |
| `Data/DBManager.cs` | Phase 4-e DB 마이그레이션 4건 — ALTER TABLE TB_MARKET_SNAPSHOT ADD COLUMN |
| `appsettings.json` | `Consensus` 설정 섹션 추가 (QuantWeight, ChartAiWeight, FundAiWeight, BuyThreshold, SellThreshold) |
| `Data/AppConfigManager.cs` | Consensus 키 매핑 5개 추가 (QUANT_WEIGHT, CHART_AI_WEIGHT, FUND_AI_WEIGHT, BUY_THRESHOLD, SELL_THRESHOLD) |

## Phase 4-d 상세 변경 이력 — 다중 에이전트(투자 위원회) 구조 및 재무 프롬프트 통합

### 핵심 변경: "단일 AI 에이전트" → "차트+펀더멘털 이중 에이전트 만장일치 합의"

Anthropic의 `financial-services` 레포지토리의 다중 에이전트 구조를 벤치마킹하여,
기존 단일 Gemini 프롬프트를 두 개의 독립된 에이전트로 분리하고 퀀트+AI 2자 합의를 3자 만장일치 합의로 업그레이드했습니다.

```text
변경 전 흐름:
  SmartOrderEngine → [퀀트] + [단일 Gemini AI] → 2자 합의(CombineSignals)

변경 후 흐름 (Phase 4-d):
  SmartOrderEngine → [퀀트] + Task.WhenAll{[차트 AI], [펀더멘털 AI]}
                                  ↓ 각자 독립적 의견 반환 (MultiAgentAnalysisResult)
                              3자 만장일치(CombineSignals) → finalSignal
```

#### 합의 알고리즘 (만장일치, 리스크 오클루전)

| 퀀트 | 차트AI | 펀더멘털AI | 최종 결론 |
|:---:|:---:|:---:|:---:|
| BUY | BUY | BUY | ✅ BUY (만장일치) |
| BUY | BUY | HOLD/SELL | ⚠️ HOLD (펀더멘털 이견) |
| BUY | HOLD/SELL | - | ⚠️ HOLD (차트AI 이견) |
| HOLD | - | - | ❌ HOLD (1차 관문 탈락) |
| SELL | SELL | SELL | ✅ SELL (만장일치) |

#### 로그 출력 예시

```text
[SmartOrder] [MEAN_REVERSION] QQQ 최종 판정: HOLD
  ├── 퀀트       : BUY — RSI(38.2) ≤ 45 AND Position(0.21) ≤ 0.30
  ├── 차트AI     : BUY (확신도:0.78) — 기술적 반등 신호 및 BB 하단 지지 확인
  └── 펀더멘털AI: HOLD (확신도:0.55) — 금리 상승 사이클에서 기술주 ETF 진입 시기 재고 권장
  → 퀀트+차트AI는 BUY에 동의하나, 펀더멘털AI가 이견(HOLD). 펀더멘털AI 이견: ...
```

### 4-d 신규/수정 파일

#### 신규 파일 (2건)

| 파일 | 설명 |
|------|------|
| `Core/IMcpDataProvider.cs` | MCP(Model Context Protocol) 외부 데이터 공급자 인터페이스 골격. 향후 FactSet/Bloomberg 연동 확장점 |

#### 수정 파일 (5건)

| 파일 | 변경 내용 |
|------|----------|
| `Core/IMarketAnalyzer.cs` | `MultiAgentAnalysisResult` 클래스 추가, `AnalyzeAsync()` 반환 타입 변경, `ohlcv` 파라미터 추가 |
| `Utils/PromptBuilder.cs` | `BuildFundamentalSystemPrompt()` + `BuildFundamentalUserPrompt()` 신규 추가. 차트 에이전트 역할 명시 강화 |
| `Core/GeminiMarketAnalyzer.cs` | `Task.WhenAll` 이중 에이전트 병렬 실행 구조로 전면 리팩토링. `CallGeminiAsync()` 내부 메서드 분리 |
| `Core/AiMarketAnalyzer.cs` | Mock 구현체를 `MultiAgentAnalysisResult` 반환으로 업데이트. 차트/펀더멘털 Mock 에이전트 분리 |
| `Core/SmartOrderEngine.cs` | `SmartOrderResult`에 `MultiAgentResult` 필드 추가. `CombineSignals()`를 3자 만장일치 합의로 전면 교체. 상세 3자 판단 로그 추가 |

## Phase B/C 상세 변경 이력 — 내결함성(Polly), 이메일 알림 연동, React 프론트엔드 연동

### 핵심 변경: "React-Router 기반 SPA 프론트엔드 구축 및 운영 안정성 강화"

- **내결함성 (Polly)**: `KisBrokerClient` 내에서 KIS API 호출 시 발생할 수 있는 일시적 네트워크 오류나 Rate Limit(429) 에러에 대응하기 위해 `Polly`의 `AsyncRetryPolicy` 지수 백오프 재시도 로직을 적용했습니다.
- **이메일 알림 (MailKit)**: `NotificationService`를 구현하여 매수/매도 체결 성공 시 또는 퀀트 엔진 예외 발생 시 관리자에게 이메일로 즉각 알림을 전송하도록 연동했습니다.
- **React 프론트엔드 (Vite + SPA)**: 기존의 백엔드 컨트롤러들을 화면으로 제공하기 위해 React 프론트엔드를 신규 구축했습니다.
  - 대시보드 (`Dashboard.jsx`), 전략 관리 (`Strategy.jsx`), 거래 내역 (`History.jsx`), 퀀트 분석 (`Order.jsx`), 백테스트 (`Backtest.jsx`), 설정 (`Settings.jsx`) 총 6개의 핵심 페이지 및 라우팅 구현 완료.
  - 프리미엄 Glassmorphism UI 디자인 시스템(`index.css`)을 적용했습니다.

## Phase A 상세 변경 이력 — 프로젝트 정비 및 안정화

### 핵심 변경: "WinForms 레거시 완전 제거 및 REST API 전환"

- **WinForms 의존성 제거**: `Properties/` 폴더, `packages.config`, `App.config` 등 기존 WinForms 관련 잔재 파일들을 모두 삭제하고 Headless Web API 환경으로 이관했습니다.
- **설정 체계 현대화**: `appsettings.json`을 도입하여 기본 설정값을 관리하고, 민감 정보(API 키, 계좌번호 등)는 환경변수에서만 읽어오도록 보안을 강화했습니다. `AppConfigManager`는 `IConfiguration`과 SQLite DB를 함께 참조하도록 리팩토링되었습니다.
- **의존성 주입(DI) 정비**: `DBManager` 등 핵심 컴포넌트를 `Program.cs`의 DI 컨테이너에 싱글턴으로 등록하고 컨트롤러에 주입하는 패턴을 확립했습니다.
- **API 컨트롤러 도입**: 기존 Panel UI를 대체하는 `HistoryController`, `StrategyController`, `OrderController`, `BacktestController` 등 RESTful API 엔드포인트를 신설하여 외부 제어가 가능해졌습니다.

## Phase 2.6 상세 변경 이력 — 구조 리팩토링

### 핵심 변경: "멀티 Form 팝업" → "단일 창 Panel 전환 (SPA)"

```text
기존 흐름:
  사이드바 버튼 클릭 → new ConfigForm().ShowDialog()  ← 별도 창 팝업

리팩토링 후:
  사이드바 버튼 클릭 → SwitchPanel(new ConfigPanel())  ← 단일 창 내 패널 교체
```

### 2.6-1. UI 구조 전환 (신규 5건)

| 파일 | 설명 |
|------|------|
| `Panels/DashboardPanel.cs` | 대시보드 (카드 + 배분결과 + 로그) |
| `Panels/AllocationPanel.cs` | 배분 설정 (종목/수량 관리) |
| `Panels/HistoryPanel.cs` | 거래 내역 조회 |
| `Panels/ConfigPanel.cs` | 환경 설정 (전략유형 ComboBox) |
| `Panels/LogPanel.cs` | 전체 화면 로그 뷰 |

### 2.6-2. 환율 API 연동 (신규 1건)

| 파일 | 설명 |
|------|------|
| `Utils/ExchangeRateService.cs` | Frankfurter API + ExchangeRate-API fallback |

### 2.6-3. Weight → Qty 통일 (수정 7건)

| 파일 | 변경 내용 |
|------|----------|
| `Data/DTO/StrategyDto.cs` | `Weight(double)` → `Qty(int)` |
| `Data/DAO/StrategyDAO.cs` | CAST(WEIGHT AS INTEGER), Qty 바인딩 |
| `Core/AllocationEngine.cs` | 수량 기반 배분 계산으로 변경 |
| `Core/SmartOrderEngine.cs` | `strategy.Qty` 직접 사용 |
| `Core/Quant/RebalancingEngine.cs` | Qty 기반 비중 계산 |
| `Data/sql/create_tables.sql` | `WEIGHT INTEGER`, 초기 데이터 수량화 |
| `Forms/AllocationSetupForm.cs` | `s.Qty`, `Qty = qty` (이후 삭제됨) |

### 2.6-4. 삭제/정리 (수정 2건)

| 파일 | 변경 내용 |
|------|----------|
| `Forms/MainForm.cs` | SPA 방식 전면 재작성, ShowDialog 제거 |
| `Forms/MainForm.Designer.cs` | btn_login/order/reservation/backtest 제거, pnl_content 추가 |

### 2.6-5. 전략 프리셋 제거 (수정 2건 → 이후 삭제됨)

| 파일 | 변경 내용 |
|------|----------|
| `Forms/ConfigForm.cs` | 안정형/공격형 라디오 → 전략유형 ComboBox (이후 삭제됨) |
| `Forms/ConfigForm.Designer.cs` | rdb_balanced/aggressive 제거, cmb_strategyType 추가 (이후 삭제됨) |

### 2.6-6. 레거시 Form 파일 삭제 (삭제 8건)

| 파일 | 사유 |
|------|------|
| `Forms/ConfigForm.cs` | ConfigPanel로 완전 이전 |
| `Forms/ConfigForm.Designer.cs` | 상동 |
| `Forms/HistoryForm.cs` | HistoryPanel로 완전 이전 |
| `Forms/HistoryForm.Designer.cs` | 상동 |
| `Forms/AllocationSetupForm.cs` | AllocationPanel로 완전 이전 |
| `Forms/AllocationSetupForm.Designer.cs` | 상동 |
| `Forms/AllocationSetupForm.resx` | 상동 |
| `Forms/BacktestForm.cs` | 미사용 (MainForm에서 호출 없음) |

## Phase 2.5 상세 변경 이력 — 퀀트 엔진 모듈

### 핵심 변경: "단순 예약 매수" → "퀀트 조건 판단 후 매수"

```text
기존 흐름:
  오후 10:30 → SmartOrderEngine → Position ≤ 0.10 이면 매수

퀀트 통합 흐름:
  오후 10:30 → SmartOrderEngine
    → OHLCV 조회
    → RSI, MACD, 볼린저밴드 계산
    → 전략 유형별 다중 조건 AND 필터
    → 모든 조건 통과 시에만 매수
    → 판단 근거 상세 로그 + 시장 스냅샷 DB 저장
```

### 2.5-1. 퀀트 지표 계산 레이어 (신규 3건)

| 파일 | 설명 |
|------|------|
| `Core/Quant/QuantIndicator.cs` | RSI(14일), MACD(12,26,9), 볼린저밴드(20일,±2σ) 계산. EMA 내부 구현 포함 |
| `Core/Quant/QuantFilter.cs` | 전략 유형별 다중 조건 AND 필터. FilterResult에 충족/미충족 조건 목록 |
| `Core/Quant/BacktestEngine.cs` | 과거 OHLCV 기반 전략 수익성 검증 (수익률, MDD, 승률) |

### 2.5-2. 리밸런싱 엔진 (신규 1건)

| 파일 | 설명 |
|------|------|
| `Core/Quant/RebalancingEngine.cs` | 보유 비중 vs 목표 비중 편차 계산 → 임계값 초과 시 자동 조정 주문 |

### 2.5-3. 데이터 레이어 확장 (신규 4건)

| 파일 | 분류 | 설명 |
|------|------|------|
| `Data/DTO/OhlcvDto.cs` | DTO | OHLCV 일봉 데이터 (시가/고가/저가/종가/거래량) |
| `Data/DTO/IndicatorDto.cs` | DTO | 퀀트 지표 결과 (RSI, MACD Line/Signal/Histogram, BB Upper/Middle/Lower, Position) |
| `Data/DTO/BacktestResultDto.cs` | DTO | 백테스팅 결과 + 개별 거래 기록 |
| `Data/DTO/MarketSnapshotDto.cs` | DTO | 매매 시점 시장 지표 스냅샷 (Phase 4 AI 학습용) |
| `Data/DAO/MarketSnapshotDAO.cs` | DAO | TB_MARKET_SNAPSHOT CRUD |

### 2.5-4. UI (신규 1건)

| 파일 | 설명 |
|------|------|
| `Forms/BacktestForm.cs` | 백테스팅 폼 — 종목/전략 선택, 기간/투자금 설정, 실행, 결과(수익률·MDD·승률) 표시, 거래 내역 그리드 |

### 2.5-5. 기존 코드 수정 (8건)

| 파일 | 변경 내용 |
|------|----------|
| `Core/IBrokerClient.cs` | `GetOhlcvAsync(ticker, days)` 메서드 추가 |
| `Core/SimBrokerClient.cs` | `GetOhlcvAsync()` 가상 OHLCV 랜덤 워크 구현 |
| `Core/SmartOrderEngine.cs` | 퀀트 지표 계산 + QuantFilter 통합 + 시장 스냅샷 자동 저장 |
| `Core/SchedulerModule.cs` | 리밸런싱 주기 도래 체크 + RebalancingEngine 자동 실행 |
| `Data/DTO/StrategyDto.cs` | `StrategyType` 필드 추가 (MEAN_REVERSION/MOMENTUM/MIXED) |
| `Data/DAO/StrategyDAO.cs` | STRATEGY_TYPE 컬럼 READ/WRITE 반영 |
| `Data/DBManager.cs` | RunMigration() 메서드 + ALTER TABLE 마이그레이션 |
| `Data/sql/create_tables.sql` | TB_MARKET_SNAPSHOT 테이블 + 리밸런싱 설정값 4개 추가 |
| `Utils/Logger.cs` | `LogQuant()` 메서드 + QUANT 로그 레벨 추가 |
| `Forms/MainForm.cs` | 백테스팅 버튼 클릭 핸들러 추가 |
| `Forms/MainForm.Designer.cs` | 사이드바에 "백테스팅" 버튼 추가 |

## Phase 2 상세 변경 이력

### 2-1. 엔진 코어 (신규 파일 7건)

| 파일 | 분류 | 설명 |
|------|------|------|
| `Core/IBrokerClient.cs` | 인터페이스 | 증권사 API 추상화. 로그인, 시세, 잔고, 주문 6개 메서드 정의 |
| `Core/SimBrokerClient.cs` | 구현체 | 시뮬레이션 브로커. 고정 기준가 반환 (GLD=$195, QQQM=$200 등), 환율 1,350원 고정 |
| `Core/SmartOrderEngine.cs` | 엔진 | 스마트 주문 판단. 20일 최저/최고가 기준 하위 10% 매수, 상위 10% 매도 |
| `Core/SchedulerModule.cs` | 스케줄러 | System.Timers 1분 간격. ORDER_SCHEDULE 시각에 SmartOrderEngine 자동 실행 |
| `Core/SessionManager.cs` | 세션 | IS_PAPER_TRADING 설정에 따라 SimBrokerClient 또는 LsBrokerClient(미구현) 분기 |
| `Data/DTO/HoldingDto.cs` | DTO | 보유 종목 정보 (Ticker, Qty, AvgPrice, CurrentPrice, ProfitRate) |
| `Data/DTO/PriceRangeDto.cs` | DTO | N일 가격 범위 (High, Low, Current, Position 0.0~1.0) |

### 2-2. 배분 설정 Form (신규 → 이후 AllocationPanel로 이전, 삭제됨)

| 파일 | 설명 |
|------|------|
| ~~`Forms/AllocationSetupForm.cs`~~ | 배분 설정 비즈니스 로직 (삭제됨) |
| ~~`Forms/AllocationSetupForm.Designer.cs`~~ | UI 레이아웃 (삭제됨) |

## Phase 3 개발 가이드 (KIS API 전환)

### 완료된 핵심 작업
1. **하네스 엔지니어링 가이드 적용**: `.agents/rules/` 디렉토리에 5개의 아키텍처/컨벤션 규칙 파일 신설. (AI 코드 생성 안정성 확보)
2. **`KisTokenManager` 구현**: KIS OAuth 토큰 발급 및 메모리 관리.
3. **`KisBrokerClient` 구현**: IBrokerClient의 KIS 증권사 REST API 구현체.
4. **`SessionManager` 분기**: App.config 설정을 통한 KIS/Sim 분기 로직 반영.

### 진행 상황: 완료 ✅
- **모의투자 환경 통합 검증 완료**: Headless 환경에서 퀀트 조건 판단 및 주문 실행 루프가 정상 동작함을 확인.
- REST API 컨트롤러를 통한 연동 테스트 완료.

### 퀀트 엔진과의 연동 포인트

Phase 3 연동에 따라 `KisBrokerClient.GetOhlcvAsync()`가 KIS [해외주식] 일별 시세 API에서 실제 OHLCV 데이터를 반환하게 됩니다. 이 데이터가 `QuantIndicator`에 입력되어 **실전 시장 데이터 기반의 퀀트 지표 계산**이 작동합니다.

## Phase 4 AI 시장분석 엔진 — 초기(Mock) 구현 완료 ✅

### 핵심 변경: "퀀트 단독 판단" → "퀀트 + AI Mock 신호 합산 (CombineSignals)"

#### 신규 파일

| 파일 | 설명 |
|------|------|
| `Core/IMarketAnalyzer.cs` | AI 분석 엔진 인터페이스 + `AiAnalysisResult` DTO (Signal, ConfidenceScore, Reason) |
| `Core/AiMarketAnalyzer.cs` | Mock 구현체. RSI/Position 기반의 간단한 규칙으로 BUY/SELL/HOLD 신호 + 확신도 반환 |

#### 수정 파일

| 파일 | 변경 내용 |
|------|----------|
| `Core/SmartOrderEngine.cs` | `IMarketAnalyzer _analyzer` 필드 추가, `AnalyzeAsync()`에 `CombineSignals()` 통합 |

#### CombineSignals() 판단 흐름

```text
퀀트 신호 (quantSignal) + AI 신호 (aiResult)
    │
    ├── AI ConfidenceScore < 0.7? → 퀀트 신호 우선 (AI 확신도 부족)
    ├── 퀀트 == AI == BUY/SELL?   → 동일 방향 신호 유지 (적극 진입)
    ├── 퀀트=HOLD, AI=BUY/SELL?   → HOLD 유지 (보수적 — 퀀트 조건 미충족)
    └── 퀀트=BUY/SELL, AI=반대?   → 방어적 HOLD 전환 (리스크 관리)
```

#### 현재 Mock AI 판단 로직 (AiMarketAnalyzer.cs)

| 조건 | 신호 | 확신도 |
|------|------|--------|
| RSI < 30 AND Position < 0.20 | BUY | 0.60 ~ 0.90 (랜덤) |
| RSI > 70 AND Position > 0.80 | SELL | 0.60 ~ 0.90 (랜덤) |
| 그 외 | **HOLD** | 0.30 ~ 0.50 (랜덤) |

> ⚠️ **현재 AI가 항상 HOLD를 반환하는 이유**: 대부분의 종목은 RSI 30 미만이면서 동시에 Position 0.20 미만인 조건(과매도 + 가격 하단 10-20% 이내)을 **동시에** 충족하기 매우 어렵습니다. 따라서 현실적으로는 대부분 `else` 분기(HOLD, 확신도 0.3~0.5)로 떨어집니다. 이 Mock 확신도(최대 0.5)는 `CombineSignals()`의 임계값 0.7보다 낮으므로 AI 신호가 최종 결과에 영향을 주지 않고 **퀀트 신호만 최종 판단**에 사용됩니다.

## Phase 4-b 실물 연동 (Gemini) 및 퀀트 조건 완화 완료 ✅

Mock 환경에서 벗어나 실제 Google Gemini API 연동을 완료하고, 퀀트 조건을 현실화하여 두 지표가 상호작용하도록 개선했습니다.

### 1. 실물 AI(Gemini) 연동 (신규 2건)

| 파일 | 설명 |
|------|------|
| `Utils/PromptBuilder.cs` | 종목의 OHLCV 데이터와 퀀트 지표를 LLM이 이해할 수 있는 시스템/사용자 텍스트 프롬프트로 파싱 |
| `Core/GeminiMarketAnalyzer.cs` | `IMarketAnalyzer` 연동 구현체. Gemini 1.5 Flash API 연동, Polly를 통한 429/5xx 에러 백오프 재시도 및 AI 생성 JSON 응답 안전 파싱 로직 |

### 2. 퀀트 매매 조건 현실화 및 임계값 조정 (수정 3건)

너무 엄격하여 신호가 발생하지 않던 퀀트 진입 기준을 완화하고, AI의 신호가 실제 판단에 반영될 수 있도록 튜닝했습니다.

| 파일 | 변경 내용 |
|------|----------|
| `Core/Quant/QuantFilter.cs` | `MEAN_REVERSION` 매수 조건 완화 (Position ≤ 0.30, RSI ≤ 45), `MIXED` 매수 완화 (RSI < 60) |
| `Core/SmartOrderEngine.cs` | AI 확신도 반영 합산 임계값(`CONFIDENCE_THRESHOLD`)을 기존 0.7에서 0.6으로 하향 |
| `Core/SessionManager.cs` | `AI_PROVIDER` 설정에 따라 `GeminiMarketAnalyzer`와 기존 `AiMarketAnalyzer`(Mock) 분기 로직 적용 |

### 3. API 키 관리 강화 및 보안 적용 (수정 3+건)

| 파일 | 변경 내용 |
|------|----------|
| `Data/AppConfigManager.cs` | `AI_PROVIDER`, `GEMINI_API_KEY` 환경변수 키 매핑 추가 |
| `appsettings.json` | `Ai` 설정 섹션 템플릿(비밀번호 제외) 추가 |
| `.gitignore` | `appsettings.local.json`, `*.secrets.json` 등 시크릿 파일 패턴 추가되어 레포지토리 내 중요정보 반출 방비 안전장치 반영 |
| `appsettings.local.json` | **[추적 제외됨]** 로컬 실행 및 시크릿 환경 변수 처리용 템플릿 |

> 비용/토큰 분석 참고: `Documents/worklog/[2026-06-02] 01_AI엔진 도입 비용 분석.md`

## AI 학습 데이터 축적 구조 (Phase 2.5에서 준비 완료)

```text
매매 시점 → SmartOrderEngine
  → MarketSnapshotDAO.Insert()
    → TB_MARKET_SNAPSHOT에 저장
      • 종목, 가격, Position, RSI, MACD, BB, 신호
      
Phase 4에서 이 데이터를 AI 모델의 Feature로 활용:
  SELECT * FROM TB_MARKET_SNAPSHOT WHERE SIGNAL = 'BUY'
  → 성공한 매수 패턴 학습
```

## 전략 유형 (Phase 2.5에서 추가 — Phase 6에서 퀀트 판단 제거)

> 현재 코드·신규 DB 스키마에 존재하지 않는다. `STRATEGY_TYPE` 앱설정 시드와 `TB_INVEST_STRATEGY` DDL은
> 2026-07-30에 `create_tables.sql`에서 제외되었다. 아래 표는 이력 보존용이다.

| 전략 유형 | 설명 | 매수 조건 |
|-----------|------|-----------|
| `MEAN_REVERSION` | 평균회귀 (기본) | Position ≤ 0.10 AND RSI ≤ 30 AND BB 하단 근접 |
| `MOMENTUM` | 모멘텀 | RSI ≥ 50 AND MACD Histogram > 0 AND MACD Line > 0 |
| `MIXED` | 혼합 | Position ≤ 0.10 AND RSI < 70 |

## 리밸런싱 설정 (Phase 2.5에서 추가 — Phase 6에서 리밸런싱 제거)

> 현재 코드·신규 DB 스키마에 존재하지 않는다. 아래 시드 키들은 2026-07-30에 `create_tables.sql`에서
> 제외되었고 읽는 코드도 없다. 표는 이력 보존용이다.

| 설정 KEY | 기본값 | 설명 |
|----------|--------|------|
| `REBALANCE_ENABLED` | `0` | 1=활성화, 0=비활성화 |
| `REBALANCE_PERIOD` | `MONTHLY` | WEEKLY 또는 MONTHLY |
| `REBALANCE_THRESHOLD` | `0.05` | 편차 5% 초과 시 리밸런싱 |
| `LAST_REBALANCE_DATE` | (빈값) | 마지막 리밸런싱 실행일 |

## 파일 변경 이력 요약

### 신규 파일 (Phase 2.5)
| # | 파일 경로 | 용도 |
|---|----------|------|
| 1 | `Core/Quant/QuantIndicator.cs` | RSI, MACD, 볼린저밴드 계산 |
| 2 | `Core/Quant/QuantFilter.cs` | 전략별 다중 조건 AND 필터 |
| 3 | `Core/Quant/BacktestEngine.cs` | 과거 데이터 기반 전략 검증 |
| 4 | `Core/Quant/RebalancingEngine.cs` | 보유 비중 자동 재조정 |
| 5 | `Data/DTO/OhlcvDto.cs` | OHLCV 일봉 데이터 |
| 6 | `Data/DTO/IndicatorDto.cs` | 퀀트 지표 결과 |
| 7 | `Data/DTO/BacktestResultDto.cs` | 백테스팅 결과 |
| 8 | `Data/DTO/MarketSnapshotDto.cs` | 시장 스냅샷 (AI 학습용) |
| 9 | `Data/DAO/MarketSnapshotDAO.cs` | 스냅샷 CRUD |
| 10 | `Forms/BacktestForm.cs` | 백테스팅 UI |

### 수정 파일 (Phase 2.5)
| # | 파일 경로 | 변경 요약 |
|---|----------|----------|
| 1 | `Core/IBrokerClient.cs` | +GetOhlcvAsync |
| 2 | `Core/SimBrokerClient.cs` | +GetOhlcvAsync 가상 데이터 |
| 3 | `Core/SmartOrderEngine.cs` | 퀀트 필터 통합, 스냅샷 저장, 상세 로그 |
| 4 | `Core/SchedulerModule.cs` | 리밸런싱 주기 체크 + 실행 |
| 5 | `Data/DTO/StrategyDto.cs` | +StrategyType 필드 |
| 6 | `Data/DAO/StrategyDAO.cs` | STRATEGY_TYPE 컬럼 반영 |
| 7 | `Data/DBManager.cs` | +RunMigration(), ALTER TABLE |
| 8 | `Data/sql/create_tables.sql` | +TB_MARKET_SNAPSHOT, +리밸런싱 설정 |
| 9 | `Utils/Logger.cs` | +LogQuant(), +QUANT 레벨 |
| 10 | `Forms/MainForm.cs` | +btn_backtest_Click |
| 11 | `Forms/MainForm.Designer.cs` | +btn_backtest 버튼 |
