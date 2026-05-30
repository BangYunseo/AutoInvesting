---
trigger: always_on
---

# UI 다크 테마 규칙

## 핵심 원칙

> **MUST**: 모든 색상은 반드시 `AppTheme` 클래스 상수를 사용합니다.  
> **MUST NOT**: `Color.FromArgb()` 직접 호출을 금지합니다.

```csharp
// ✅ 올바른 사용
using AutoInvest.Utils;
this.BackColor = AppTheme.BgMain;
lbl.ForeColor = AppTheme.FgPrimary;

// ❌ 금지 (하드코딩)
this.BackColor = Color.FromArgb(30, 30, 30);
lbl.ForeColor = Color.White;
```

## 색상 팔레트 (AppTheme.cs)

### 배경색

| 상수 | RGB | 용도 |
|------|-----|------|
| `BgMain` | (30, 30, 30) | Form/Panel 배경 (가장 어두운) |
| `BgSidebar` | (38, 50, 56) | 사이드바, 상단바 |
| `BgCard` | (50, 50, 50) | 카드 패널, 섹션 헤더 |
| `BgInput` | (55, 71, 79) | TextBox, ComboBox 배경 |
| `BgContent` | (40, 40, 40) | FlowLayoutPanel, ListView |
| `BgCardRow` | (45, 45, 45) | 개별 카드 행 |

### 글자색

| 상수 | RGB | 용도 |
|------|-----|------|
| `FgPrimary` | (230, 230, 230) | 주요 텍스트 (제목, 값) |
| `FgSecondary` | (180, 200, 210) | 보조 텍스트 (라벨) |
| `FgMuted` | (120, 120, 120) | 비활성/힌트 텍스트 |
| `FgLabel` | (160, 170, 180) | 카드 타이틀 |

### 강조색

| 상수 | RGB | 용도 |
|------|-----|------|
| `Accent` | (0, 150, 136) | Teal — 주요 강조 |
| `Success` | (0, 230, 118) | 초록 — 성공/양수 |
| `Danger` | (183, 28, 28) | 빨강 — 삭제/음수 |
| `Warning` | (230, 180, 0) | 주황 — 모의투자 모드 |
| `BarFill` | (60, 130, 200) | 파란 — 프로그레스 바 |

### 버튼

| 상수 | RGB | 용도 |
|------|-----|------|
| `BtnPrimary` | (0, 150, 136) | 저장/추가 (Teal) |
| `BtnSecondary` | (55, 71, 79) | 취소/보조 |
| `BtnActive` | (60, 80, 90) | 활성 사이드바 메뉴 |
| `BtnBorder` | (80, 100, 110) | 보조 버튼 테두리 |

## 컨트롤별 적용 체크리스트

### Panel / UserControl 배경
```csharp
this.BackColor = AppTheme.BgMain;
```

### Label (제목)
```csharp
lbl.Font = new Font("맑은 고딕", 14F, FontStyle.Bold);
lbl.ForeColor = AppTheme.FgPrimary;
```

### Label (보조)
```csharp
lbl.Font = new Font("맑은 고딕", 10F);
lbl.ForeColor = AppTheme.FgSecondary;
```

### TextBox
```csharp
txt.BackColor = AppTheme.BgInput;
txt.ForeColor = AppTheme.FgPrimary;
txt.BorderStyle = BorderStyle.FixedSingle;
txt.Font = new Font("맑은 고딕", 11F);
```

### 주요 버튼 (저장/추가)
```csharp
btn.BackColor = AppTheme.BtnPrimary;
btn.ForeColor = Color.White;
btn.FlatStyle = FlatStyle.Flat;
btn.FlatAppearance.BorderSize = 0;
btn.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
btn.Cursor = Cursors.Hand;
```

### 보조 버튼 (취소)
```csharp
btn.BackColor = AppTheme.BtnSecondary;
btn.ForeColor = AppTheme.FgSecondary;
btn.FlatStyle = FlatStyle.Flat;
btn.FlatAppearance.BorderColor = AppTheme.BtnBorder;
btn.FlatAppearance.BorderSize = 1;
btn.Cursor = Cursors.Hand;
```

### DataGridView
```csharp
dgv.BackgroundColor = AppTheme.BgSidebar;
dgv.BorderStyle = BorderStyle.None;
dgv.GridColor = AppTheme.Border;

// 헤더
headerStyle.BackColor = AppTheme.BgInput;
headerStyle.ForeColor = AppTheme.FgSecondary;
headerStyle.SelectionBackColor = AppTheme.BgInput;

// 셀
cellStyle.BackColor = AppTheme.BgSidebar;
cellStyle.ForeColor = AppTheme.FgPrimary;
cellStyle.SelectionBackColor = AppTheme.Selection;
```

### ListView
```csharp
lvw.BackColor = AppTheme.BgContent;
lvw.ForeColor = AppTheme.FgPrimary;
lvw.BorderStyle = BorderStyle.None;
```

## 폰트 규칙

- **기본 폰트**: `"맑은 고딕"` (모든 컨트롤)
- **제목**: 14pt Bold
- **본문/라벨**: 10pt Regular
- **입력 필드/버튼**: 11pt
- **MUST NOT**: 다른 폰트 사용 금지
