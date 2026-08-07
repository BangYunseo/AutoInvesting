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
| DB | PostgreSQL (`Npgsql`) — 로컬: localhost, 배포: **Neon**(`DATABASE_URL` 환경변수, `*.neon.tech`, SSL 필수) |
| 증권사 API | 한국투자증권 KIS Developers REST API |
| 빌드 | MSBuild / Visual Studio 2022 |
 
## 디렉토리 구조
 
```
AutoInvesting/
├── Program.cs                 # API 호스트·DI 구성
├── Core/                      # 비즈니스 로직 (브로커 추상화·DCA 엔진·사이클 진입점·세션·세금 추정)
├── Controllers/               # REST API (order·dca·price·auth·portfolio·history·test)
├── Data/                      # DBManager·AppConfigManager·DTO/·DAO/·sql/
├── Utils/                     # Logger·환율·알림·암복호화·인증 필터·로그인 스로틀
├── Tests/                     # xUnit (별도 AutoInvest.Tests.csproj — 웹 빌드에서 제외)
├── Frontend/                  # React SPA (로그인/대시보드/적립설정/주문·적립/거래내역)
├── .github/workflows/         # 유일한 트리거 — daily-run(매일 KST 00:10)·reconcile(체결 대사)·gitleaks
├── appsettings.json           # Trading/Smtp/Resend/Kis/Security/Dca/Tax 섹션
└── Documents/                 # 문서 홈 — reference/·modules/·analysis/·worklog/
    └── reference/             # CONFIG_REFERENCE(설정 키 SSOT)·API_REFERENCE·CODE_MAP(자동생성·수정금지)
                              #  ·CODE_READING_GUIDE·ONBOARDING_GUIDE·DEVELOPMENT·RECOVERY
```

> 파일 단위 색인(책임 요약·핵심 멤버)은 `Documents/reference/CODE_MAP.md`가 소스의 XML `<summary>`에서
> 자동 생성합니다 — 이 트리에 파일을 다시 나열하지 않습니다.

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
>
> **설정(Config)** — 설정 화면(`Frontend/src/pages/Settings.jsx`)과 `ConfigController`(`/api/config`, 시크릿 단건
> 조회 포함)는 2026-08-06에 코드째 제거했습니다. 운영 설정이 전부 Render 환경변수라 화면 저장이 읽히지 않는
> 무동작 UI였고, 임의 키 쓰기·시크릿 평문 열람 표면이 함께 사라졌습니다. 설정 변경은 이제 **Render 환경변수
> 수정 + 재배포**뿐입니다 (키별 위치·경위는 `Documents/reference/CONFIG_REFERENCE.md`).
 
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
| **4 ~ 5** | AI 위원회·합의 스코어링·적응형 임계값·성과 피드백·토큰 모니터링 | ❌ Phase 6에서 코드째 제거 (백테스트로 가치 부재 확인) |
| **6** | **판단 레이어 제거 → DCA 적립 코어 (매수 템플릿 + 월별 배정 / 종목별 고정 수량 매수 / 지정일 게이트)** | ✅ **완료 — 현재 동작 아키텍처** |