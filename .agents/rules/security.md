# 보안 규칙

## API 키 관리
- AppKey, AppSecret, 계좌번호를 소스코드에 **절대 하드코딩 금지**
- 설정값은 아래 두 방법으로만 관리

### 방법 1: 환경변수 (권장)
```csharp
var appKey = Environment.GetEnvironmentVariable("KIS_APP_KEY");
var appSecret = Environment.GetEnvironmentVariable("KIS_APP_SECRET");
var accountNo = Environment.GetEnvironmentVariable("KIS_ACCOUNT_NO");
```

### 방법 2: appsettings.local.json (로컬 파일)
```json
{
  "KIS": {
    "AppKey": "발급받은_APP_KEY",
    "AppSecret": "발급받은_APP_SECRET",
    "AccountNo": "계좌번호"
  }
}
```
이 파일들(`appsettings.local.json`·`*.local.json`·`*.secrets.json`·`*.key`·`*.env`·`*.db`)은 저장소 `.gitignore`에 이미 제외돼 있다 — 새 시크릿 파일 형식을 추가할 때만 `.gitignore`를 손댄다.

> 🚫 **시크릿을 DB(`TB_APP_CONFIG`)에 저장하지 않는다.** 쓰던 경로(설정 화면·`ConfigController`)가 2026-08-06에 제거돼 쓰는 코드가 없고, DB 덤프·백업이 곧 유출이 된다. 배포는 환경변수로만 주입한다.

## 토큰 관리
- Access Token은 **메모리에만 보관** — 파일/DB 저장 금지
- 로그 출력 시 토큰 값 마스킹 필수

```csharp
// ❌ 금지
Logger.Info($"토큰 발급 완료: {accessToken}");

// ✅ 올바른 방법 (마스킹)
Logger.Info($"토큰 발급 완료: {accessToken[..8]}****");
```

- 만료 **10분 전** 자동 갱신 (`Core/KisTokenManager.cs`의 `EnsureValidTokenAsync` — 토큰 24시간, 판정은 `_tokenExpiration.AddMinutes(-10)`)

## 코드 리뷰 체크포인트
- PR/커밋에 API 키, 비밀번호, 토큰이 포함되지 않았는지 확인
- 외부 API 호출 시 HTTPS 사용 여부 확인
- 사용자 입력값 검증 여부 확인
