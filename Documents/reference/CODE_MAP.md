---
title: AutoInvesting 코드 맵
date: 2026-08-06
company: [개인]
tags: [코드맵, 파일색인, 자동생성]
status: draft
---

# AutoInvesting 코드 맵

## 개요
> "어느 파일에 어느 코드가 있는지" 한눈에 찾는 자동 생성 색인이다. 각 소스 파일의 XML `<summary>` 주석이 진실 원천이며, `powershell -File Documents/regen-codemap.ps1` 실행으로 재생성된다. **이 파일을 직접 수정하지 마세요.** (⚠️ 표시 = 클래스 `<summary>` 주석이 없는 파일)

## 본문

### 진입점 (Entry Point)

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `Program.cs` | class | 자동 투자 시스템 24시간 자동 매매 | `Main` |

### Core — 비즈니스 로직

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `DailyExecutionService.cs` | class | 외부 크론잡(Cron-job.org, GitHub Actions 등)에 의해 매수 주기마다 호출되는 적립식 사이클 실행기. | `CurrentKstMonth`, `IsOnOrAfterRunDay`, `RunDcaCycleAsync`, `ReconcileAsync` |
| `DcaAccumulationEngine.cs` | class | 적립식(DCA) 자동 매수 엔진. | `PlanPurchases`, `AccumulateAsync` |
| `DcaSettings.cs` | class | 적립식(DCA) 설정의 단일 읽기/쓰기 지점. | `SelectTemplate`, `LoadTemplates`, `LoadMonthMap`, `SaveTemplates`, `SaveMonthMap` |
| `IBrokerClient.cs` | interface | 증권사 API 추상화 인터페이스. | — |
| `KisBrokerClient.cs` | class | KIS (한국투자증권) API 실거래 브로커 클라이언트. | `LoginAsync`, `GetCurrentPriceAsync`, `GetExchangeRateAsync`, `GetHoldingsAsync`, `GetCashBalanceAsync` |
| `KisTokenManager.cs` | class | KIS (한국투자증권) API OAuth 토큰 관리자. | `EnsureValidTokenAsync`, `GetToken` |
| `SessionManager.cs` | class | IBrokerClient 인스턴스의 생명주기를 관리합니다. | `GetClient`, `Reset` |
| `SimBrokerClient.cs` | class | 시뮬레이션 브로커 클라이언트. | `LoginAsync`, `GetCurrentPriceAsync`, `GetExchangeRateAsync`, `GetHoldingsAsync`, `GetCashBalanceAsync` |
| `TaxEstimator.cs` | class | 해외 ETF(미국 상장 직접투자) 매도 시 예상 양도소득세·수수료를 계산하는 세금 추정기. | `Estimate`, `Load` |

### Controllers — REST API

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AuthController.cs` | class | 단일 관리자 로그인 API. | `GetStatus`, `Setup`, `Login` |
| `DcaController.cs` | class | 적립식(DCA) 설정 조회·저장 API. | `GetConfig`, `UpdateConfig` |
| `HistoryController.cs` | class | 매매 이력과 시스템 로그를 조회하는 API. | `GetTradeHistory`, `GetSystemLogs` |
| `OrderController.cs` | class | 수동 주문 트리거 API. | `RunDcaCycle`, `RunReconcile`, `GetDcaSchedule`, `SetDcaSchedule`, `PlaceManualOrder` |
| `PortfolioController.cs` | class | 보유 잔고·예수금·대시보드 요약을 조회하는 API. | `GetHoldings`, `GetSummary` |
| `PriceController.cs` | class | 종목 현재가 조회 API. | `GetPrice` |
| `TestController.cs` | class | 운영 점검용 API. | `SendTestEmail` |

### Data/DTO — 데이터 전송 객체

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `DcaBuyFailure.cs` | class | 적립식 사이클에서 매수에 실패한 종목 1건(종목·수량·사유). | — |
| `DcaCycleResult.cs` | class | 적립식(DCA) 사이클 1회 실행 결과 집계. | — |
| `DcaTemplate.cs` | class | 적립 매수 템플릿 — 명명된 매수 구성(예산 + 종목별 고정 수량). | — |
| `HoldingDto.cs` | class | 보유 종목(잔고) DTO. | — |
| `SellTaxEstimateDto.cs` | class | 매도 시 예상 양도소득세·수수료 추정 결과 (순수 계산 결과 — 판단/타이밍 아님). | — |
| `TradeHistoryDto.cs` | class | 거래 내역 DTO. | — |

### Data/DAO — DB 접근

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `SystemLogDAO.cs` | class | 시스템 로그를 PostgreSQL(TB_SYSTEM_LOG)에 영구 저장/조회하는 DAO. | `Insert`, `GetByDate`, `GetAvailableDates`, `PruneOlderThan` |
| `TradeHistoryDAO.cs` | class | TB_TRADE_HISTORY 기록·조회. | `Insert`, `UpdateStatusByOrderNo`, `GetRecent` |

### Data — DB/설정 관리

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `AppConfigManager.cs` | class | 애플리케이션 설정값 통합 관리 우선순위 : 환경변수 > DB 테이블(TB_APP_CONFIG) > appsettings.json 민감정보 : (KIS_APP_… | `Initialize`, `Get`, `TryReadDb`, `Set`, `GetMap` |
| `DBManager.cs` | class | ⚠️ (요약 없음) | `GetConnection` |

### Utils — 유틸리티/통신

| 파일 | 타입 | 책임 요약 | 핵심 멤버 |
|------|------|-----------|-----------|
| `ApiKeyAuthAttribute.cs` | class | 글로벌 인증 필터. | `OnActionExecutionAsync` |
| `CryptoUtil.cs` | class | 시크릿 암복호화 · 비밀번호 해시 · 세션 토큰 발급/검증을 담당하는 공용 암호화 유틸리티입니다. | `Initialize`, `EncryptSecret`, `DecryptSecret`, `IsEncrypted`, `HashPassword` |
| `ExchangeRateService.cs` | class | 무료 환율 API를 통해 USD/KRW 환율을 조회합니다. | `GetUsdKrwAsync`, `ParseKrwRate` |
| `Logger.cs` | class | 시스템 로깅 유틸리티 (Serilog 래퍼). | `Initialize`, `Info`, `Error`, `Warn`, `Fatal` |
| `LoginThrottle.cs` | class | 로그인 실패 속도를 서비스 전체에서 하나의 창(window)으로 제한합니다. | `IsRateLimited`, `RegisterFailure`, `Reset` |
| `NotificationService.cs` | class | 관리자 알림 메일 발송 서비스. | `Initialize`, `SendEmailAsync`, `SendEmailOrThrowAsync` |
| `PublicEndpointAttribute.cs` | class | 전역 인증 필터()를 면제하는 마커 어트리뷰트입니다. | — |

## 정리

**총 34개 파일** · 요약 없는 파일 **1개**

<details><summary>⚠️ XML &lt;summary&gt; 보강이 필요한 파일</summary>

- `Data/DBManager.cs`

</details>

## 참고
- 재생성: `powershell -File Documents/regen-codemap.ps1`
- 요약 원천: 각 .cs 파일 클래스 선언 위의 XML `<summary>` 주석 (code-style-guide.md)
