---
trigger: always_on
---

# 보안 규칙

## API 키 관리
- AppKey, AppSecret, 계좌번호를 소스코드에 **절대 하드코딩 금지**
- 설정값은 `App.config`의 `<appSettings>` 또는 환경변수로 관리
- 프로그램 내에서는 `AppConfigManager` 또는 `ConfigurationManager`를 통해 읽기

## 토큰 관리
- Access Token은 메모리에만 보관
- 토큰을 파일, DB, 로그에 저장하지 않음
- 로그 출력 시 토큰 값을 마스킹 처리 (예: `Bearer ***...***`)

## .gitignore 규칙
- 다음 파일/패턴이 반드시 `.gitignore`에 포함되어야 함:
  - `*.user` — 사용자별 설정
  - `*.suo` — VS 솔루션 사용자 옵션
  - `bin/`, `obj/` — 빌드 산출물
  - 민감 정보가 포함된 설정 파일

## 코드 리뷰 체크포인트
- PR/커밋에 API 키, 비밀번호, 토큰이 포함되지 않았는지 확인
- 외부 API 호출 시 HTTPS 사용 여부 확인
- 사용자 입력값 검증 여부 확인
