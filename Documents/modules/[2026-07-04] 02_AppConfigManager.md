---
title: AppConfigManager (Data)
date: 2026-07-04
company: [개인]
tags: [설정관리, 암호화, 우선순위폴백, 데이터계층]
status: done
---

# AppConfigManager (Data)

## 개요
> AutoInvesting의 모든 설정값을 환경변수 → DB → appsettings.json → 기본값의 단일 우선순위로 조회·저장해 통일하는 데이터 계층 모듈이다. 민감 키(KIS 키·시크릿·계좌·Resend·API 접근키)는 저장 시 `MASTER_KEY`로 자동 암호화하고 읽을 때 복호화한다.

## 배경 / 목적
- 위치: `Data/AppConfigManager.cs`
- 일자: 2026.07.04

여러 소스(배포 환경변수, 런타임 DB, 파일 기본값)에 흩어진 설정을 하나의 진입점으로 모아 조회 규칙을 통일하고, 민감 키 암호화를 강제하기 위한 모듈이다. 호출자는 `SNAKE_CASE` 키 문자열 하나만 알면 어느 소스에 값이 있든 동일한 방식으로 값을 얻고, 저장 시 민감 키는 자동으로 암호화되어 DB에 들어간다.

## 본문

### 역할과 데이터 흐름
역할은 **설정값 통일**이다.

입력 → 처리 → 출력·부작용은 다음과 같다.

- **입력**: 설정 키 문자열 (예: `"KIS_APP_KEY"`, `"IS_PAPER_TRADING"`)
- **처리**: 우선순위대로 값 탐색 / 저장 시 민감 키 암호화
- **출력**: 문자열 값 (없으면 호출자가 준 기본값)
- **부작용**: `Set()`은 **DB(`TB_APP_CONFIG`)에 write**. `Get()`은 read만(단, DB 접근은 함).

### 값 탐색 우선순위
소스코드의 주요 로직은 값 탐색 순서 결정과 **우선순위**에 따른 값 설정이며, **Render 환경변수가 최우선**이다.

```text
① 환경변수
→ ② DB(TB_APP_CONFIG)
→ ③ appsettings.json
→ ④ 기본값
```

- **민감 키(KIS 키·시크릿·계좌·Resend·API 접근키)**: 저장할 때 `MASTER_KEY`로 **암호화(AES-GCM)** 해서 DB에 넣고, 읽을 때 자동 복호화. `MASTER_KEY`가 없으면 평문 저장 + 경고.
- DB에 저장된 값은 항상 `appsettings.json` 기본값을 덮어씁니다(런타임에 UI로 바꾼 값이 이김).

### 헷갈리기 쉬운 지점 / 함정
- **DB가 죽어도 앱은 안 죽습니다.** `TryGetFromDb`가 예외 시 `null`을 반환해 다음 단계(파일/기본값)로 조용히 폴백하고 경고만 남깁니다.
- **평문 vs 암호문 구분**은 값 앞의 `enc:v1:` 접두사로 판단합니다(`CryptoUtil.IsEncrypted`). 접두사가 없으면 레거시 평문으로 간주해 그대로 통과.
- **키 이름 규칙**: 코드는 `SNAKE_CASE`(예: `IS_PAPER_TRADING`)로 부르고, `appsettings.json`은 계층형(`Trading:IsPaperTrading`)입니다. 이 둘을 이어주는 게 `ResolveFromConfiguration`의 매핑표입니다.

### 로직 변경 가능 구간
- **새 설정 키 추가**: `ResolveFromConfiguration`의 `switch`에 `"NEW_KEY" => "Section:Field"` 한 줄 + `appsettings.json`에 해당 섹션.
- **새 민감 키 추가**: 위 매핑 + `SensitiveKeys` 집합에 키 추가(자동 암호화 대상이 됨).
- **DB 테이블**: 설정은 `TB_APP_CONFIG (CONFIG_KEY, CONFIG_VALUE)` 한 곳.

### 라인 바이 라인 정독

#### Get(key, defaultValue) — 값 조회
우선순위대로 훑어 **처음 잡히는 값**을 반환한다. 전체가 `try/catch`라 조회가 실패해도 앱은 죽지 않고 `defaultValue`로 폴백한다.

```csharp
string? envValue = Environment.GetEnvironmentVariable(key);
if (!string.IsNullOrEmpty(envValue)) return envValue;
```
① **환경변수** 최우선 — 배포(Render) 실제 값이 여기 있다. 잡히면 즉시 반환.

```csharp
string? dbValue = TryGetFromDb(key);
if (!string.IsNullOrEmpty(dbValue)) return dbValue;
```
② **DB(TB_APP_CONFIG)** — UI로 저장한 런타임 값. `appsettings.json` 기본값을 덮어쓴다.

```csharp
string? configValue = ResolveFromConfiguration(key);
if (!string.IsNullOrEmpty(configValue)) return configValue;  // ③ appsettings.json
return defaultValue;                                          // ④ 기본값
```
③ **appsettings.json** — DB에 없는 키의 초기 기본값(`SNAKE_CASE`→계층 키 매핑). ④ 다 없으면 호출자가 준 `defaultValue`.

> ℹ️ `Get()`은 자주 호출되므로 히트 경로엔 로그를 남기지 않고 조용히 반환한다(로그 폭주·DB 싱크 부하 방지). 실패만 `catch`에서 `Logger.Error`로 남긴다.

#### TryGetFromDb(key) — DB 한 건 조회
```csharp
using var cmd = new NpgsqlCommand(
    "SELECT CONFIG_VALUE FROM TB_APP_CONFIG WHERE CONFIG_KEY=@k", conn);
cmd.Parameters.AddWithValue("@k", key);
string? value = cmd.ExecuteScalar()?.ToString();
```
파라미터 바인딩(`@k`)으로 SQL 인젝션을 막고 값 한 건을 조회.

```csharp
if (CryptoUtil.IsEncrypted(value ?? string.Empty))
    return CryptoUtil.DecryptSecret(value!);
return value;
```
`enc:v1:` 접두사가 붙은 암호문이면 복호화, 아니면 평문 그대로. 행이 없거나 DB 오류면 `catch`에서 `null`을 반환 → 상위 `Get()`이 파일/기본값으로 폴백(경고 로그만).

#### Set(key, value) — 저장 (upsert)
```csharp
if (SensitiveKeys.Contains(key) && !string.IsNullOrEmpty(value))
{
    if (CryptoUtil.IsConfigured) storedValue = CryptoUtil.EncryptSecret(value);
    else Logger.Warn($"[AppConfig] MASTER_KEY 미설정 ... 민감 키 평문 저장 [{key}]");
}
```
민감 키는 저장 직전 `MASTER_KEY`로 암호화(AES-GCM). 키가 없으면 평문 저장 + 경고.

```csharp
int affected = cmd.ExecuteNonQuery();   // UPDATE ... WHERE CONFIG_KEY=@k
if (affected == 0) { /* INSERT */ }
```
`UPDATE` 후 영향 행이 0이면 `INSERT` — 있으면 갱신, 없으면 추가하는 upsert.

```csharp
if (SensitiveKeys.Contains(key))
    Logger.Info($"[AppConfig] 저장: {key} = ****...");
```
민감 키는 값 대신 `****...`만 로그에 남긴다(마스킹 규칙, `security.md`).

#### GetMap(path) — 섹션 통째로 조회
```csharp
foreach (var child in section.GetChildren())
    if (!string.IsNullOrEmpty(child.Value)) map[child.Key] = child.Value;
```
`appsettings.json`의 한 섹션(예: `Dca:Quantities`) 아래 **값이 있는 직속 하위 항목만** 딕셔너리로. `DcaSettings`가 사용.

#### ResolveFromConfiguration(key) — SNAKE_CASE → 계층 키 매핑
```csharp
string? mappedPath = key switch
{
    "IS_PAPER_TRADING" => "Trading:IsPaperTrading",
    "KIS_APP_KEY"      => "Kis:AppKey",
    // ... (KIS/Resend/Security 키)
    _ => null
};
```
코드가 부르는 `SNAKE_CASE` 키를 `appsettings.json`의 계층 경로로 변환. 매핑 없으면 `null`.

```csharp
if (value != null && key == "IS_PAPER_TRADING")
    if (bool.TryParse(value, out bool boolVal)) return boolVal ? "1" : "0";
```
`IS_PAPER_TRADING`만 `true/false` → `"1"/"0"`으로 변환(레거시 호환).

### 리팩토링 노트 (2026-07-04)
**무엇을**: 제거된 판단 레이어(Phase 6) 관련 죽은 설정 키 매핑을 삭제.

- `ResolveFromConfiguration`에서 제거: `INVEST_AMOUNT_KRW`, `ACTIVE_STRATEGY`, `STRATEGY_TYPE`, `ORDER_SCHEDULE`, `REBALANCE_*`, `LAST_REBALANCE_DATE`, `AI_PROVIDER`, `GEMINI_*`, `QUANT_WEIGHT`, `CHART_AI_WEIGHT`, `FUND_AI_WEIGHT`, `BUY_THRESHOLD`, `SELL_THRESHOLD`, `FX_ADVISOR_*`.
- `SensitiveKeys`에서 `GEMINI_API_KEY` 제거(AI 제거로 사망).
- `appsettings.json > Trading`에서 `InvestAmountKrw/ActiveStrategy/StrategyType/OrderSchedule` 제거(`IsPaperTrading`만 유지).

**왜**: 코드 어디서도 이 키들을 `Get()`하지 않음(grep로 호출처 0 확인). `ConfigController`도 주석으로 "Phase 6에서 노출 중단"이라 명시. 남겨두면 "이 설정 아직 쓰나?" 혼란만 유발.

**동작 보존 근거**: 호출처가 없으므로 실행 경로 불변. 제거 전/후 `dotnet build`(경고0·오류0) + `dotnet test`(12/12) 동일 통과.

## 정리 / 결론
- 조회는 환경변수 → DB(`TB_APP_CONFIG`) → `appsettings.json` → 호출자 기본값 순으로 처음 잡히는 값을 반환한다.
- 민감 키는 저장 시 `MASTER_KEY`로 AES-GCM 암호화, 조회 시 `enc:v1:` 접두사를 보고 자동 복호화한다.
- DB 장애 시에도 조용히 파일/기본값으로 폴백해 앱이 죽지 않는다.
- 새 설정 키는 `ResolveFromConfiguration` 매핑 + `appsettings.json` 섹션으로, 새 민감 키는 추가로 `SensitiveKeys`에 넣어 확장한다.
- Phase 6에서 판단 레이어용 죽은 키 매핑을 제거했으며, 호출처가 없어 동작은 불변임을 빌드·테스트로 확인했다.

## 참고
- [2026-07-04] 04_DcaSettings.md — `GetMap`을 사용하는 설정 소비 모듈
