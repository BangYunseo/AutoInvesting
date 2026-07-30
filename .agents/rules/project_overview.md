# AutoInvesting 프로젝트 개요
 
> 해외 ETF 자동 투자 시스템 — ASP.NET Core Web API (.NET 8.0)
 
## 목적
 
정해진 주기에 자동으로 해외 ETF를 **적립식(DCA)으로 매수**하는 Headless 서비스입니다.
정직한 백테스트(2012~현재) 결과 "퀀트/AI 타이밍 판단"이 단순 적립을 2.7~4배 밑돌고
완벽한 타이밍조차 평균 대비 연 +0.3~0.9%에 그쳐(타이밍은 잘해야 본전), **타이밍 판단 레이어를
전면 제거**했습니다(Phase 6). 가치는 *판단*이 아니라 *자동화*에 있다는 결론에 따라,
여러 **매수 템플릿**(종목별 고정 수량 + 예산)을 정의하고 **월별로 배정**해, 현재 월에 해당하는
템플릿대로 매 사이클 그대로 매수합니다(비중·금액은 수량×현재가로 환산해 표시만 하고, 예산은
초과 경고용 상한). **감정·예측을 배제한 기계적 적립 투자**를 실현합니다.
 
## 기술 스택
 
| 분류 | 기술 |
|------|------|
| 언어 | C# |
| 프레임워크 | ASP.NET Core Web API (.NET 8.0) |
| 통신/내결함성 | HttpClient, Polly (Phase B/C 적용) |
| 알림/이메일 | Resend HTTP API (Render의 SMTP 포트 차단 대응) |
| DB | PostgreSQL (`Npgsql`) — 로컬: localhost, 배포: `DATABASE_URL` 환경변수(Render.com URI) |
| 증권사 API | 한국투자증권 KIS Developers REST API |
| 빌드 | MSBuild / Visual Studio 2022 |
 
## 디렉토리 구조
 
```
AutoInvesting/
├── Program.cs                          # API 호스트 및 DI 설정 컨테이너
│
├── Core/                               # 핵심 비즈니스 로직
│   ├── IBrokerClient.cs                # 증권사 API 추상화
│   ├── KisBrokerClient.cs              # KIS 실거래 연동 모듈 (Polly 적용)
│   ├── KisTokenManager.cs              # KIS OAuth 토큰 발급·메모리 보관·자동 갱신
│   ├── SimBrokerClient.cs              # 가상 모의투자 환경
│   ├── SessionManager.cs               # 모의/실전 브로커 생명주기 관리
│   ├── DcaAccumulationEngine.cs        # 적립식 매수 엔진 (판단 없음 / PlanPurchases 순수함수)
│   ├── DcaSettings.cs                  # 매수 템플릿·월배정·예산 읽기/쓰기 (DB 우선 → appsettings 폴백)
│   ├── DailyExecutionService.cs        # 적립 사이클 진입점 (RunDcaCycleAsync)
│   └── TaxEstimator.cs                 # 매도 양도소득세 추정 (순수함수 / 정보·확인용 — 판단 레이어 아님)
│
├── Controllers/                        # 외부 제어용 REST API 엔드포인트
│   ├── OrderController.cs              # dca-run(적립 사이클), manual(수동 주문), sell-preview(매도 양도세 프리뷰)
│   ├── DcaController.cs               # /api/dca/config 매수 템플릿·월배정 조회·저장 (GET: {templates, monthMap, currentMonth, activeTemplateId} / PUT: {templates, monthMap})
│   ├── PriceController.cs             # /api/price/{ticker} 현재가 조회 겸 티커 검증
│   ├── AuthController.cs              # /api/auth status/setup/login (단일 관리자 인증, 서명 세션 토큰) [PublicEndpoint]
│   ├── ConfigController.cs
│   ├── PortfolioController.cs
│   ├── HistoryController.cs
│   └── TestController.cs              # send-test-email (메일 발송 점검용 — 실주문 경로 없음)
│
├── Data/                               # 데이터 액세스 (DTO/DAO)
│   ├── DBManager.cs                    # PostgreSQL 연결 (Npgsql, DATABASE_URL 지원)
│   ├── AppConfigManager.cs             # 설정값 관리 (TB_APP_CONFIG: DCA_TEMPLATES/DCA_MONTH_MAP 등, 시크릿 암복호화)
│   ├── DTO/                            # Data Transfer Objects
│   │   ├── DcaTemplate.cs              # 매수 템플릿 DTO (Id, Name, BudgetKrw, Quantities)
│   │   ├── TradeHistoryDto.cs          # 거래내역 DTO
│   │   ├── HoldingDto.cs               # 보유종목 DTO
│   │   └── SellTaxEstimateDto.cs       # 매도 양도세 추정 결과 DTO
│   └── DAO/                            # Data Access Objects
│       ├── TradeHistoryDAO.cs          # TB_TRADE_HISTORY 기록·조회
│       └── SystemLogDAO.cs             # TB_SYSTEM_LOG 로그 영구 적재 (Logger.DbSink)
│
├── Utils/                              # 범용 유틸리티
│   ├── Logger.cs                       # Serilog 로깅 래퍼 (콘솔+파일+DB 싱크)
│   ├── ExchangeRateService.cs          # 환율 API (Frankfurter / ExchangeRate-API 폴백)
│   ├── NotificationService.cs          # 이메일 알림 발송 (Resend HTTP API)
│   ├── CryptoUtil.cs                   # 시크릿 AES-GCM 암복호화 + 비밀번호 해시 + 세션 토큰 서명
│   ├── ApiKeyAuthAttribute.cs          # 전역 인증 필터 (Bearer 세션토큰 또는 x-api-key)
│   └── PublicEndpointAttribute.cs      # 인증 면제 마커 (로그인/초기설정용)
│
├── Frontend/                           # React SPA (로그인/대시보드/적립설정/주문·적립/거래내역/설정)
├── appsettings.json                    # 환경 설정 — Trading/Smtp/Resend/Kis/Security/Dca/Tax 섹션
├── README.md
└── Documents/                     # 단일 문서 홈 (프로젝트 문서 전부)
    ├── reference/                  # 상시 참조 문서 (고정 이름)
    │   ├── DEVELOPMENT.md          # 개발 진척도 및 변경 이력
    │   ├── ONBOARDING_GUIDE.md     # 신규 개발자용 아키텍처 가이드
    │   ├── CODE_READING_GUIDE.md   # DCA 적립 코어 코드 흐름 가이드
    │   ├── CODE_MAP.md             # 코드 색인 (regen-codemap.ps1로 재생성)
    │   └── API_REFERENCE.md         # REST API 레퍼런스 (인터랙티브 명세는 /swagger)
    ├── [YYYY-MM-DD] NN_*.md        # 분석·진단 문서 (프로젝트 개요/아키텍처 등)
    ├── modules/                    # 모듈별 이해 문서
    ├── analysis/                   # 백테스트·절세 분석 산출물
    └── worklog/                    # 기능 단위 작업 인계 보고서
```

> Phase 6에서 판단 레이어(SmartOrderEngine, Core/Quant/*, Core/Advisors/*, AI MarketAnalyzer,
> AllocationEngine, RebalancingEngine, 관련 DAO/DTO/Controller·프론트 페이지)는 모두 제거되었습니다.
> `TB_MARKET_SNAPSHOT` 테이블은 과거 데이터 보존 목적으로 스키마에만 남아 있으며 더 이상
> 기록되지 않습니다(레거시). 관련 ALTER 마이그레이션은 중복이라 제거되었습니다.
>
> Phase 6 이후 추가된 보조 기능: **Auth**(단일 관리자 인증 — 전역 필터로 모든 엔드포인트 보호),
> **Tax**(매도 양도세 추정 — 수동 매도 확인용).
> ⚠️ **Tax는 정보/보고 전용으로, `DcaAccumulationEngine`·`DailyExecutionService`의 매수 의사결정에
> 어떤 값도 흘려보내지 않습니다(판단 레이어 재도입 아님).** 이 경계를 깨는 배선은 금지됩니다.
>
> **Macro**(FRED 거시지표 국면 브리핑)는 프론트에 배선되지 않아 소비자가 0이었으므로 2026-07-30에
> 코드째 제거했습니다(`MacroController`/`MacroBriefingService`/`FredClient`/DTO 2종/테스트). 다시
> 필요해지면 화면과 함께 도입하고, 그때도 매수 의사결정에 값을 흘려보내지 않는 경계를 지킵니다.
 
## 핵심 인터페이스: IBrokerClient
 
| 메서드 | 설명 |
|--------|------|
| `LoginAsync()` / `IsLoggedIn` | 로그인 (토큰 발급) / 로그인 상태 |
| `GetCurrentPriceAsync(ticker)` | 현재가 조회 (USD) |
| `GetExchangeRateAsync()` | USD/KRW 환율 조회 |
| `GetHoldingsAsync()` / `GetCashBalanceAsync()` | 보유 잔고 / 예수금 조회 |
| `PlaceBuyOrderAsync(...)` | 매수 주문 |
| `PlaceSellOrderAsync(...)` | 매도 주문 |

> 과거 판단 레이어용 `GetOhlcvAsync`/`GetPriceRangeAsync`는 Phase 6에서 인터페이스·구현체 모두에서 제거됨.
 
## Phase 진행 상태
 
| Phase | 내용 | 상태 |
|-------|------|------|
| 1 ~ 2.6 | 기존 WinForms 기반 기반 개발 | ✅ 완료 |
| 3 | KIS 실거래 클라이언트 연동 | ✅ 완료 |
| **A** | **Web API/Headless로 아키텍처 전면 개편** | ✅ **완료** |
| **B/C** | **내결함성(Polly), 이메일 알림 연동, React 연동** | ✅ **완료** |
| **4-a~c** | **AI Mock → Gemini 실물 연동 → 투자 철학 주입** | ✅ **완료** |
| **4-d** | **다중 에이전트(투자 위원회) 구조, 재무 프롬프트 통합, 3자 만장일치 합의** | ✅ **완료** |
| **4-e** | **확률 기반 합의 스코어링 / 가중치 임계값 / 신호 투명성 강화** | ✅ **완료** |
| **5-a** | **종목별 적응형 임계값 시스템 (BuyProbability 백분위 기반)** | ✅ **완료** |
| **5-b** | **AI 성과 측정 + 토큰 비용 모니터링 데이터 적재** | ✅ **완료** |
| **5-c** | **모니터링 대시보드 UI (성과/비용 조회)** | ✅ **완료** |
| **5-d** | **성과 기반 피드백 루프: 에이전트별 실측 적중률 + 매도 적응형 임계값 + 합의 가중치 A/B 검증** | ✅ **완료** |
| **6** | **백테스트 검증으로 판단 레이어 가치 부재 확인 → 판단 레이어 전면 제거, DCA 적립 코어로 전환 (매수 템플릿 + 월별 배정 / 종목별 고정 수량 매수 / 티커 검증·실시간 가격 기반 DCA 설정 편집기)** | ✅ **완료** |

> ⚠️ Phase 4~5(AI 위원회·합의 스코어링·적응형 임계값·성과 피드백·토큰 모니터링)는 Phase 6에서
> **백테스트 결과 가치가 검증되지 않아 코드째 제거**되었습니다. 위 표는 이력 보존용이며, 현재
> 동작 아키텍처는 Phase 6(DCA)입니다.