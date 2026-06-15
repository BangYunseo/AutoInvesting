---
trigger: always_on
---

# AutoInvesting 프로젝트 개요
 
> 해외 ETF 자동 투자 시스템 — ASP.NET Core Web API (.NET 8.0)
 
## 목적
 
설정한 시각에 자동으로 해외 ETF를 매수·매도하는 Headless 백그라운드 서비스입니다.
퀀트 엔진(RSI, MACD, 볼린저밴드)으로 다중 기술적 지표를 분석하고,
모든 조건을 만족할 때만 주문을 실행하여 **감정을 배제한 데이터 기반 투자**를 실현합니다.
 
## 기술 스택
 
| 분류 | 기술 |
|------|------|
| 언어 | C# |
| 프레임워크 | ASP.NET Core Web API (.NET 8.0) |
| 통신/내결함성 | HttpClient, Polly (Phase B/C 적용) |
| 알림/이메일 | MailKit, MimeKit (Phase B/C 적용) |
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
│   ├── SimBrokerClient.cs              # 가상 모의투자 환경
│   ├── SmartOrderEngine.cs             # 퀀트 필터 통과 시 실거래 주문 실행
│   ├── SessionManager.cs               # 모의/실전 브로커 생명주기 관리
│   ├── AllocationEngine.cs             # 자산 배분 비중 계산
│   ├── BackgroundServices/             # IHostedService 구현체 
│   │   └── TradingBackgroundService.cs # 24시간 백그라운드 루프 (1분 간격)
│   └── Quant/                          # 퀀트 분석 모듈
│       ├── QuantIndicator.cs           # 지표 생성(RSI, BB, MACD)
│       └── QuantFilter.cs              # 전략 조건 판단 로직
│
├── Controllers/                        # 외부 제어용 REST API 엔드포인트
│   ├── OrderController.cs
│   ├── ConfigController.cs
│   └── StrategyController.cs
│
├── Data/                               # 데이터 액세스 (DTO/DAO)
│   ├── DBManager.cs                    # PostgreSQL 연결 (Npgsql, DATABASE_URL 지원)
│   ├── AppConfigManager.cs             # 설정값 관리
│   ├── DTO/                            # Data Transfer Objects
│   └── DAO/                            # Data Access Objects (MarketSnapshotDAO 등)
│
├── Utils/                              # 범용 유틸리티
│   ├── Logger.cs                       # Serilog/File 로깅 래퍼
│   ├── ExchangeRateService.cs          # 환율 API (Frankfurter)
│   └── NotificationService.cs          # SMTP 이메일 발송 관리 (Phase B/C)
│
├── appsettings.json                    # 환경 설정 및 DB/SMTP 정보 등
├── README.md
└── Documents/
    ├── DEVELOPMENT.md              # 개발 진척도 및 변경 이력
    └── THEME_GUIDE.md              # ⚠️ 레거시 — WinForms 제거로 무효. 참조 금지
```
 
## 핵심 인터페이스: IBrokerClient
 
| 메서드 | 설명 |
|--------|------|
| `LoginAsync()` | 로그인 (토큰 발급) |
| `GetCurrentPriceAsync(ticker)` | 현재가 조회 (USD) |
| `GetOhlcvAsync(ticker, days)` | OHLCV 일봉 데이터 |
| `PlaceBuyOrderAsync(...)` | 매수 주문 |
| `PlaceSellOrderAsync(...)` | 매도 주문 |
 
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
| **6-a** | **SimBroker 학습데이터 대량 생성 + DATA_SOURCE(SIM/REAL) 출처 분리** | ✅ **완료** |