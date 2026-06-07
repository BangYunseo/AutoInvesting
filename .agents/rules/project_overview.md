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
| DB | SQLite (`System.Data.SQLite`) |
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
│   ├── DBManager.cs                    # SQLite 연결
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
| 4-e | AI 성능 측정 / Token 비용 모니터링 / 다수결 알고리즘 A/B 검증 | 🔜 예정 |