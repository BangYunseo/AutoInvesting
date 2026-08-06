---
title: 운영 복구 절차
date: 2026-08-06
company: [개인]
tags: [복구, 환경변수, 배포, 크론, 백업]
status: draft
---

# 운영 복구 절차

## 개요
> Render 서비스나 계정을 잃었을 때 이 시스템을 처음부터 다시 세우는 절차입니다. **값은 한 줄도 담지 않고** 항목 이름·출처·형식 제약·순서만 기록합니다.

## 배경 / 목적

운영 설정이 전부 Render 환경변수에 있고, 화면에서 바꿀 수 없습니다(설정 화면·`ConfigController`는 2026-08-06에 제거). 그래서 "무엇이 어디서 오는지"를 아는 것이 곧 복구 능력입니다.

값 자체는 저장소·문서·프롬프트·커밋에 남기지 않습니다(조직 보안정책). 이 문서는 **어디서 다시 얻는지**만 알려주는 지도입니다.

## 본문

### 외부 계정

| 서비스 | 역할 | 잃었을 때 |
|--------|------|-----------|
| Render | 앱 호스팅, 환경변수 보관 | 서비스 재생성 + 환경변수 재주입 |
| Neon | PostgreSQL (거래이력·시스템로그·설정) | **Render와 독립** — Render를 잃어도 함께 사라지지 않음 |
| KIS Developers | 앱키·시크릿 발급 | 포털에서 재발급 |
| 한국투자증권 | 계좌번호·상품코드 | 증권사 앱에서 확인 |
| Resend | 알림 메일 발송 API 키 | 재발급 |
| GitHub | 저장소 + Actions 크론(`daily-run`, `reconcile`) | Secrets 재설정 + 워크플로의 호출 URL 수정 |

### 환경변수

`AppConfigManager.Get()`은 **환경변수 → DB(`TB_APP_CONFIG`) → `appsettings.json`** 순으로 읽습니다. 환경변수에 값이 있으면 DB를 보지 않습니다.

| 이름 | 쓰이는 곳 | 형식·제약 | 없으면 |
|------|-----------|-----------|--------|
| `MASTER_KEY` | 시크릿 AES-256-GCM 암복호화, 토큰 서명 폴백 (`Utils/CryptoUtil.cs`) | **base64 32바이트** (아니면 무효 처리) | **기동 거부** (`Program.cs` — 종료 코드 1) |
| `AUTH_TOKEN_SECRET` | 세션 토큰 HMAC-SHA256 서명 (`CryptoUtil.GetTokenKey`) | 임의 문자열 | `MASTER_KEY`에서 파생. 바꾸면 기존 로그인 세션 전부 무효 |
| `API_ACCESS_KEY` | 크론·머신 인증(`x-api-key`) (`Utils/ApiKeyAuthAttribute.cs`) | 임의 문자열 | 크론 호출이 401 |
| `DATABASE_URL` | Neon 연결 (`Data/DBManager.cs`) | PostgreSQL URL (SSL 필수) | `localhost` 기본값으로 폴백 → 운영 데이터에 접근하지 못함 |
| `IS_PAPER_TRADING` | 실전·모의 분기 (`Core/SessionManager.cs`) | **정확히 문자열 `0`(실전) 또는 `1`(모의)**. `false`·`False`·공백은 전부 모의로 떨어짐 | 미설정 시 `1`(모의) |
| `KIS_APP_KEY` | KIS 인증 | 실전·모의 앱키가 다름 (모의 앱키는 실전망에서 인증 실패) | **비면 `SimBrokerClient`로 조용히 폴백** — 주문이 시뮬레이션이 됨 |
| `KIS_APP_SECRET` | KIS 인증 | 위와 동일 | 인증 실패 |
| `KIS_ACCOUNT_NO` | 주문·잔고의 `CANO` | 개인정보 취급 대상 | 인증 실패 |
| `KIS_ACCOUNT_PROD` | 주문·잔고의 `ACNT_PRDT_CD` | 미설정 시 기본 `01` | 기본값 `01`로 동작 (Render에 없어도 대개 문제 없음) |
| `RESEND_API_KEY` | 알림 메일 발송 (`Utils/NotificationService.cs`) | Resend API 키 | 적립 보고서·예외 알림 메일이 발송되지 않음 |
| `ADMIN_EMAIL` | 알림 수신 주소 | 개인정보 취급 대상 | `appsettings`의 `Resend:AdminEmail` → `Smtp:AdminEmail` 순으로 폴백 |

`KIS_SERVER`는 화면 표시용 죽은 설정이었고 소비자가 사라졌습니다. 도메인 분기는 `IS_PAPER_TRADING` 하나로만 결정됩니다.

### 재구축 순서

1. **Neon 확인.** 살아 있으면 그대로 씁니다. 새로 만들었다면 빈 DB로 두면 되고, 기동 시 `Data/sql/create_tables.sql`이 실행되어 스키마가 생성됩니다(자동 마이그레이션 경로는 없습니다).
2. **Render 서비스 생성** 후 GitHub 저장소를 연결합니다.
3. **환경변수 주입.** 위 표의 항목을 넣습니다. `IS_PAPER_TRADING`은 먼저 `1`(모의)로 두고 시작하는 편이 안전합니다.
4. **기동 로그 확인.**
   - `MASTER_KEY` 관련 `Fatal`이 없어야 합니다(있으면 기동 자체가 안 됩니다).
   - `[Session] KIS API 클라이언트 생성 (모드: ...)` 줄이 의도한 모드인지 봅니다.
5. **관리자 초기설정.** `GET /api/auth/status`로 관리자 등록 여부를 확인하고, 비어 있으면 `POST /api/auth/setup`으로 등록합니다. 이 엔드포인트는 인증이 필요하므로 `x-api-key`(`API_ACCESS_KEY` 값) 헤더를 씁니다.
6. **GitHub Actions 재설정.**
   - Secrets: `CRON_API_KEY` (= 서버의 `API_ACCESS_KEY`와 같은 값)
   - `.github/workflows/daily-run.yml`·`reconcile.yml`의 `BASE_URL`은 **시크릿이 아니라 워크플로에 직접 적힌 값**입니다. Render URL이 바뀌면 이 두 파일을 고쳐야 합니다.
7. **검증.** 로그인 → 대시보드 배지(`LIVE`/`PAPER`/`SIM`)와 마스킹 계좌번호 → 예수금·보유 종목 조회 → 적립 설정 화면의 템플릿·월배정·지정일이 그대로인지 확인. 마지막으로 크론 워크플로를 수동 트리거해 응답 코드를 봅니다.
8. **실전 전환은 마지막에.** `IS_PAPER_TRADING`을 `0`으로 바꾸고 재배포한 뒤 4·7단계를 다시 확인합니다.

### 백업 원칙

값은 **개인 비밀번호 관리자에만** 둡니다. 저장소·문서·프롬프트·커밋에는 넣지 않습니다.

| 구분 | 항목 |
|------|------|
| 재발급 불가 | `MASTER_KEY` — DB에 남은 KIS 암호문을 여는 유일한 열쇠. 이 사본을 정리했다면 중요도는 내려갑니다 |
| 재발급 가능 | KIS 앱키·시크릿, `RESEND_API_KEY`, `API_ACCESS_KEY`, `AUTH_TOKEN_SECRET`(바꾸면 기존 세션 무효) |
| 원본이 외부에 있음 | 계좌번호(증권사 앱), `ADMIN_EMAIL` |
| 별도 서비스에 있음 | 거래이력·시스템로그·적립 설정(Neon) — Render 상실과 무관 |

### 설정을 바꾸는 방법

화면에 설정 편집 UI는 없습니다. **Render 환경변수 수정 + 재배포**가 유일한 경로입니다.

재배포 마찰은 결함이 아니라 가드입니다. 재배포가 프로세스 재시작을 강제하므로, 캐시된 브로커 인스턴스나 진행 중인 적립 사이클이 옛 설정으로 끝까지 도는 혼동이 물리적으로 생기지 않습니다.

## 정리 / 결론

- 잃으면 진짜 곤란한 것은 `MASTER_KEY` 하나이고, 나머지는 원본이 외부(KIS·증권사·Resend)에 있습니다.
- Neon은 Render와 별개이므로 거래이력·설정은 Render를 잃어도 남습니다.
- `IS_PAPER_TRADING`은 문자열 `0`/`1`만 인정합니다. 이 한 글자가 실전·모의를 가릅니다.
- 크론의 호출 URL은 시크릿이 아니라 워크플로 파일에 있습니다. URL이 바뀌면 코드를 고쳐야 합니다.

## 참고

- `.agents/rules/security.md` — 시크릿 관리 원칙
- `.agents/rules/recommended_rules.md` — 실거래 전환 절차
- `Documents/reference/CONFIG_REFERENCE.md` — 설정 키별 출처와 실효값
- `Documents/reference/DEVELOPMENT.md` — 변경 이력
