using System.Drawing;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 앱 전체 다크 테마 컬러 상수
    /// </summary>
    public static class AppTheme
    {
        // ─── 배경색 ──────────────────────────────────────────
        /// <summary>메인 Form 배경 (가장 어두운)</summary>
        public static readonly Color BgMain = Color.FromArgb(30, 30, 30);
        /// <summary>사이드바, 상단바, 하단바</summary>
        public static readonly Color BgSidebar = Color.FromArgb(38, 50, 56);
        /// <summary>카드, 패널 배경</summary>
        public static readonly Color BgCard = Color.FromArgb(50, 50, 50);
        /// <summary>입력 필드 배경</summary>
        public static readonly Color BgInput = Color.FromArgb(55, 71, 79);
        /// <summary>섹션 헤더 배경</summary>
        public static readonly Color BgHeader = Color.FromArgb(50, 50, 50);
        /// <summary>콘텐츠 영역 (FlowLayout, ListView 등)</summary>
        public static readonly Color BgContent = Color.FromArgb(40, 40, 40);
        /// <summary>카드 행 배경 (살짝 밝은)</summary>
        public static readonly Color BgCardRow = Color.FromArgb(45, 45, 45);

        // ─── 글자색 ──────────────────────────────────────────
        /// <summary>주요 텍스트 (흰색)</summary>
        public static readonly Color FgPrimary = Color.FromArgb(230, 230, 230);
        /// <summary>보조 텍스트 (밝은 회색)</summary>
        public static readonly Color FgSecondary = Color.FromArgb(180, 200, 210);
        /// <summary>비활성 텍스트 (어두운 회색)</summary>
        public static readonly Color FgMuted = Color.FromArgb(120, 120, 120);
        /// <summary>카드 타이틀 (section label)</summary>
        public static readonly Color FgLabel = Color.FromArgb(160, 170, 180);

        // ─── 강조색 ──────────────────────────────────────────
        /// <summary>주요 강조 (Teal 계열)</summary>
        public static readonly Color Accent = Color.FromArgb(0, 150, 136);
        /// <summary>경고/위험 (빨간색)</summary>
        public static readonly Color Danger = Color.FromArgb(183, 28, 28);
        /// <summary>성공/긍정 (초록색)</summary>
        public static readonly Color Success = Color.FromArgb(0, 230, 118);
        /// <summary>모의투자 모드 (주황색)</summary>
        public static readonly Color Warning = Color.FromArgb(230, 180, 0);
        /// <summary>프로그레스 바 (파란색)</summary>
        public static readonly Color BarFill = Color.FromArgb(60, 130, 200);

        // ─── 버튼 ────────────────────────────────────────────
        /// <summary>주요 버튼 (Teal)</summary>
        public static readonly Color BtnPrimary = Color.FromArgb(0, 150, 136);
        /// <summary>보조 버튼</summary>
        public static readonly Color BtnSecondary = Color.FromArgb(55, 71, 79);
        /// <summary>활성 메뉴 버튼</summary>
        public static readonly Color BtnActive = Color.FromArgb(60, 80, 90);
        /// <summary>버튼 테두리</summary>
        public static readonly Color BtnBorder = Color.FromArgb(80, 100, 110);

        // ─── 기타 ────────────────────────────────────────────
        /// <summary>구분선, 그리드선</summary>
        public static readonly Color Border = Color.FromArgb(60, 60, 60);
        /// <summary>DataGridView 선택 행</summary>
        public static readonly Color Selection = Color.FromArgb(60, 80, 90);
    }
}
