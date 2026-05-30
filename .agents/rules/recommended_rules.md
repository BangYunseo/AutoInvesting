---
trigger: always_on
---

# 추가 개발 규칙 & 권장사항

## Phase 간 호환성 규칙

### 하위 호환성 유지 (MUST)
- 새 Phase 기능이 기존 기능을 깨뜨리면 안 됨
- `IBrokerClient` 인터페이스에 메서드 추가 시, `SimBrokerClient`에도 반드시 구현
- DB 스키마 변경 시 `RunMigration()` 사용 (기존 데이터 보존)

```csharp
// ✅ 마이그레이션 패턴 (DBManager.cs)
RunMigration(conn, "ALTER TABLE TB_INVEST_STRATEGY ADD COLUMN NEW_FIELD TEXT DEFAULT ''");
```

### Phase 3 개발 시 주의사항
- `SimBrokerClient`는 삭제하지 않음 (시뮬레이션 모드 유지)
- `KisBrokerClient`를 별도 파일로 추가
- `SessionManager`에서 `IS_PAPER_TRADING` 설정으로 분기

## Panel 개발 규칙

### 새 Panel 추가 체크리스트
1. `Panels/` 폴더에 `{Name}Panel.cs` 파일 생성
2. `UserControl` 상속
3. `AppTheme` 다크 테마 적용 (`ui_theme.md` 참조)
4. `MainForm.SwitchPanel()` 연동 확인
5. 사이드바 메뉴 버튼 추가 (필요 시)

```csharp
public class NewPanel : UserControl
{
    public NewPanel()
    {
        this.BackColor = AppTheme.BgMain;
        this.Dock = DockStyle.Fill;
        InitializeComponents();
    }
}
```

## 성능 규칙

### API 호출 최소화
- 동일 데이터 반복 조회 방지 (캐시 활용)
- 환율은 `ExchangeRateService`에서 1시간 캐싱
- 연속 API 호출 시 200ms 딜레이 (TPS 제한 준수)

### UI 스레드 보호
- 장시간 작업은 반드시 `Task.Run()` 또는 `async/await`
- UI 업데이트는 `Invoke()` 사용

```csharp
// ✅ UI 스레드 안전한 업데이트
_listBox?.Invoke(new Action(() => _listBox.Items.Add(logMsg)));
```

## 테스트 규칙

- `SimBrokerClient`로 전체 엔진 로직 테스트 가능
- 새 기능 추가 시 시뮬레이션 모드에서 먼저 검증
- 퀀트 지표 계산은 알려진 데이터로 결과 검증

## 문서 유지 규칙

- 새 파일 추가/삭제 시 `project_overview.md`의 디렉토리 구조 업데이트
- Phase 완료 시 `Documents/DEVELOPMENT.md` 업데이트
- DB 스키마 변경 시 `Data/sql/create_tables.sql` 및 문서 동기화
