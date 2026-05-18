---
trigger: always_on
---

# C# 코딩 컨벤션

## 네이밍 규칙
- **클래스/인터페이스**: PascalCase (`SmartOrderEngine`, `IBrokerClient`)
- **메서드/프로퍼티**: PascalCase (`GetCurrentPriceAsync`, `IsLoggedIn`)
- **private 필드**: _camelCase (`_broker`, `_timer`, `_isLoggedIn`)
- **로컬 변수/파라미터**: camelCase (`ticker`, `exchangeRate`)
- **상수**: PascalCase (`DefaultTimeoutMs`) 또는 UPPER_SNAKE (`MAX_RETRY_COUNT`)
- **인터페이스**: 반드시 `I` 접두사 (`IBrokerClient`, `IMarketAnalyzer`)
- **비동기 메서드**: 반드시 `Async` 접미사 (`LoginAsync`, `GetHoldingsAsync`)

## 파일 구조
- 파일당 하나의 public 클래스/인터페이스
- 파일명 = 클래스명 (예: `SmartOrderEngine.cs`)
- 네임스페이스는 폴더 구조를 반영 (예: `AutoInvest.Core.Quant`)

## XML 문서 주석
- 모든 public 클래스, 인터페이스, 메서드에 `<summary>` 주석 필수
- 파라미터에 `<param>` 태그 사용
- TODO 주석은 `// TODO [Phase N]` 형식 사용

## 코드 스타일
- `var`는 타입이 명확한 경우에만 사용
- 중괄호는 별도 줄에 배치 (Allman 스타일)
- `using` 문은 네임스페이스 밖에 배치
- nullable 참조 타입 (`?`) 적극 활용
- 문자열 보간(`$""`)을 `string.Format()` 대신 사용
- 매직 넘버 대신 상수 또는 설정값 사용

## WinForms 특수 규칙
- 디자이너 생성 컨트롤명: `{타입약어}_{이름}` (예: `btn_save`, `pnl_content`, `lbl_title`)
- 이벤트 핸들러: `{컨트롤명}_{이벤트}` (예: `btn_save_Click`)
- IDE1006 네이밍 경고 비활성화 (`.editorconfig`에서 처리됨)
