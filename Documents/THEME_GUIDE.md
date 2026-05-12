# 다크 테마 가이드 (AppTheme)

> 이 문서는 AutoInvesting 프로젝트의 UI 다크 테마 규칙을 정의합니다.
> 새 Form/Control을 만들 때 반드시 이 가이드를 따릅니다.

---

## 테마 상수 클래스

`Utils/AppTheme.cs`에 모든 색상 상수가 정의되어 있습니다.

```csharp
using AutoInvest.Utils;

// 사용 예시
this.BackColor = AppTheme.BgMain;        // Form 배경
lbl.ForeColor = AppTheme.FgPrimary;      // 주요 텍스트
btn.BackColor = AppTheme.BtnPrimary;     // 주요 버튼
txtInput.BackColor = AppTheme.BgInput;   // 입력 필드
```

---

## 색상 팔레트

### 배경색

| 상수 | RGB | 용도 |
|------|-----|------|
| `BgMain` | (30, 30, 30) | **Form 배경** (가장 어두운) |
| `BgSidebar` | (38, 50, 56) | 사이드바, 상단바, 하단바 |
| `BgCard` | (50, 50, 50) | 카드 패널, 섹션 헤더 |
| `BgInput` | (55, 71, 79) | TextBox, ComboBox 배경 |
| `BgContent` | (40, 40, 40) | FlowLayoutPanel, ListView 영역 |
| `BgCardRow` | (45, 45, 45) | 개별 카드 행 배경 |

### 글자색

| 상수 | RGB | 용도 |
|------|-----|------|
| `FgPrimary` | (230, 230, 230) | **주요 텍스트** (제목, 값) |
| `FgSecondary` | (180, 200, 210) | 보조 텍스트 (라벨, 설명) |
| `FgMuted` | (120, 120, 120) | 비활성/힌트 텍스트 |
| `FgLabel` | (160, 170, 180) | 카드 타이틀 라벨 |

### 강조색

| 상수 | RGB | 용도 |
|------|-----|------|
| `Accent` | (0, 150, 136) | Teal — 주요 강조 |
| `Success` | (0, 230, 118) | 초록 — 성공/양수 잔여금 |
| `Danger` | (183, 28, 28) | 빨강 — 삭제 버튼/음수 잔여금 |
| `Warning` | (230, 180, 0) | 주황 — 모의투자 모드 |
| `BarFill` | (60, 130, 200) | 파란 — 프로그레스 바 |

### 버튼

| 상수 | RGB | 용도 |
|------|-----|------|
| `BtnPrimary` | (0, 150, 136) | **저장/추가** 버튼 (Teal) |
| `BtnSecondary` | (55, 71, 79) | 취소/보조 버튼 |
| `BtnActive` | (60, 80, 90) | 활성 사이드바 메뉴 |
| `BtnBorder` | (80, 100, 110) | 보조 버튼 테두리 |

---

## 새 Form 생성 체크리스트

### 1. Form 기본 속성
```csharp
// Designer.cs
this.BackColor = System.Drawing.Color.FromArgb(33, 33, 33);
this.FormBorderStyle = FormBorderStyle.FixedDialog;
this.MaximizeBox = false;
this.MinimizeBox = false;
this.StartPosition = FormStartPosition.CenterParent;
```

### 2. 제목 라벨
```csharp
this.lbl_title.Font = new Font("맑은 고딕", 14F, FontStyle.Bold);
this.lbl_title.ForeColor = Color.FromArgb(230, 230, 230);
```

### 3. 일반 라벨
```csharp
lbl.ForeColor = Color.FromArgb(180, 200, 210);
lbl.Font = new Font("맑은 고딕", 10F);
```

### 4. TextBox
```csharp
txt.BackColor = Color.FromArgb(55, 71, 79);
txt.ForeColor = Color.White;
txt.BorderStyle = BorderStyle.FixedSingle;
txt.Font = new Font("맑은 고딕", 11F);
```

### 5. 주요 버튼 (저장, 추가)
```csharp
btn.BackColor = Color.FromArgb(0, 150, 136);
btn.ForeColor = Color.White;
btn.FlatStyle = FlatStyle.Flat;
btn.FlatAppearance.BorderSize = 0;
btn.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
btn.Cursor = Cursors.Hand;
btn.UseVisualStyleBackColor = false;
```

### 6. 보조 버튼 (취소)
```csharp
btn.BackColor = Color.FromArgb(55, 71, 79);
btn.ForeColor = Color.FromArgb(180, 200, 210);
btn.FlatStyle = FlatStyle.Flat;
btn.FlatAppearance.BorderColor = Color.FromArgb(80, 100, 110);
btn.FlatAppearance.BorderSize = 1;
btn.Cursor = Cursors.Hand;
btn.UseVisualStyleBackColor = false;
```

### 7. DataGridView
```csharp
dgv.BackgroundColor = Color.FromArgb(38, 50, 56);
dgv.BorderStyle = BorderStyle.None;
dgv.GridColor = Color.FromArgb(60, 60, 60);

// 헤더
headerStyle.BackColor = Color.FromArgb(55, 71, 79);
headerStyle.ForeColor = Color.FromArgb(180, 200, 210);
headerStyle.SelectionBackColor = Color.FromArgb(55, 71, 79);

// 셀
cellStyle.BackColor = Color.FromArgb(38, 50, 56);
cellStyle.ForeColor = Color.White;
cellStyle.SelectionBackColor = Color.FromArgb(60, 80, 90);
```

### 8. ListView
```csharp
lvw.BackColor = Color.FromArgb(40, 40, 40);
lvw.ForeColor = Color.FromArgb(230, 230, 230);
lvw.BorderStyle = BorderStyle.None;
```

### 9. RadioButton / CheckBox
```csharp
rdb.ForeColor = Color.FromArgb(230, 230, 230);
chk.ForeColor = Color.FromArgb(230, 230, 230);
```

---

## 현재 적용 현황

| Form/Panel | 다크 테마 | 비고 |
|------|:---------:|------|
| MainForm | ✅ | 배경, 카드, 섹션 헤더, 로그 모두 다크 |
| DashboardPanel | ✅ | 카드, 배분결과, 로그 영역 다크 |
| AllocationPanel | ✅ | DGV, 입력 필드, 버튼 모두 다크 |
| ConfigPanel | ✅ | 입력 필드, ComboBox, 체크박스 다크 |
| HistoryPanel | ✅ | ListView, 새로고침 버튼 다크 |
| LogPanel | ✅ | ListBox 로그 뷰 다크 |
| AllocationCardControl | ✅ | 카드 행 배경 (45, 45, 45) |
