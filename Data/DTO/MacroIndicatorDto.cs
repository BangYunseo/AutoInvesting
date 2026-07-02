using System;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 거시경제 지표 하나의 조회 결과 (표시 전용 — 매수 판단에 사용하지 않음).
    /// FRED(물가·유가·금리·고용) 또는 환율 조회 결과를 담아 화면·리포트에 표시합니다.
    /// 조회 실패 시 예외 대신 <see cref="Error"/>에 사유를 담아 '일부만 실패'를 표현합니다.
    /// </summary>
    public class MacroIndicatorDto
    {
        /// <summary>내부 키 (예: "CPI", "WTI", "FX").</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>사람이 읽을 이름 (예: "소비자물가지수(CPI)").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>가장 최근 값 (지수값·달러·% 등).</summary>
        public decimal? LatestValue { get; set; }

        /// <summary>가장 최근 값의 날짜 (YYYY-MM-DD).</summary>
        public string? LatestDate { get; set; }

        /// <summary>표시 단위 힌트: "$"(접두) / "%"(접미) / "원" / "".</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>전년 동월 대비 상승률(%) — 지수형(CPI·PCE)만.</summary>
        public decimal? YoyPercent { get; set; }

        /// <summary>이번 YoY − 직전 YoY (%포인트, 지수형) — 가속/둔화 판단용.</summary>
        public decimal? YoyDelta { get; set; }

        /// <summary>비교 대상이 된 직전 값 (값형).</summary>
        public decimal? PrevValue { get; set; }

        /// <summary>직전 대비 변화량 (값형).</summary>
        public decimal? ChangeAbs { get; set; }

        /// <summary>직전 대비 변화율(%) (값형).</summary>
        public decimal? ChangePct { get; set; }

        /// <summary>등락 방향: "up" / "down" / "flat" — 화면 색상·화살표 결정.</summary>
        public string? Direction { get; set; }

        /// <summary>조회 실패 시 사유 (성공 시 null).</summary>
        public string? Error { get; set; }
    }
}
