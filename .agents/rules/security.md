---
trigger: always_on
---

# 보안 규칙

## API 키 관리
- AppKey, AppSecret, 계좌번호를 소스코드에 **절대 하드코딩 금지**
- 설정값은 아래 방법 중 하나로 관리

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
반드시 `.gitignore`에 추가: `appsettings.local.json`, `*.local.json`, `*.secrets.json`

### 방법 3: AppConfigManager + DB 저장
```csharp
// 저장 시 암호화, 읽기 시 복호화
AppConfigManager.Set("KIS_APP_KEY", Encrypt(appKey));
var appKey = Decrypt(AppConfigManager.Get("KIS_APP_KEY", ""));
```

## 토큰 관리
- Access Token은 **메모리에만 보관** — 파일/DB 저장 금지
- 로그 출력 시 토큰 값 마스킹 필수

```csharp
// ❌ 금지
Logger.Info($"토큰 발급 완료: {accessToken}");

// ✅ 올바른 방법 (마스킹)
Logger.Info($"토큰 발급 완료: {accessToken[..8]}****");
```

- 만료 30분 전 자동 갱신 패턴 필수 구현
```csharp
private async Task EnsureTokenValidAsync()
{
    if (DateTime.Now >= _tokenExpiry.AddMinutes(-30))
        await RefreshTokenAsync();
}
```

## .gitignore 필수 항목
```gitignore
# 시크릿 파일
appsettings.local.json
*.local.json
*.secrets.json
*.key
*.env
*.db

# VS 사용자 설정
*.user
*.suo

# 빌드 산출물
bin/
obj/
```

## 코드 리뷰 체크포인트
- PR/커밋에 API 키, 비밀번호, 토큰이 포함되지 않았는지 확인
- 외부 API 호출 시 HTTPS 사용 여부 확인
- 사용자 입력값 검증 여부 확인
