using System;

namespace AutoInvest.Utils
{
    /// <summary>
    /// NYSE(뉴욕증권거래소) 개장 시각을 KST(한국시간)로 변환하는 유틸리티.
    /// 미국 서머타임(DST) 적용 여부에 따라 시각이 자동 조정됩니다.
    ///
    /// 시간대 규칙:
    ///   서머타임 O (3월~11월): 한국 22:30 = 뉴욕 09:30
    ///   서머타임 X (11월~3월): 한국 23:30 = 뉴욕 09:30
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// NYSE 개장 시각을 KST TimeSpan으로 반환합니다.
        /// </summary>
        /// <returns>서머타임 O → 22:30, 서머타임 X → 23:30</returns>
        public static TimeSpan GetNYSEOpenTimeKST()
        {
            // "Eastern Standard Time" = 미국 동부 표준시 (UTC-5, DST 시 UTC-4)
            var nyTz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            // 현재 UTC 기준으로 서머타임 적용 여부 확인
            bool isDST = nyTz.IsDaylightSavingTime(DateTime.UtcNow);

            // DST=true → KST 22:30 (UTC+9 기준, 뉴욕 UTC-4 → 차이 13시간)
            // DST=false → KST 23:30 (UTC+9 기준, 뉴욕 UTC-5 → 차이 14시간)
            return isDST ? new TimeSpan(22, 30, 0) : new TimeSpan(23, 30, 0);
        }

        /// <summary>
        /// 다음 NYSE 개장 시각을 KST DateTime으로 반환합니다.
        /// 오늘 시각이 지났으면 내일로 계산합니다.
        /// </summary>
        public static DateTime GetNextNYSEOpen()
        {
            var open = GetNYSEOpenTimeKST();
            var now = DateTime.Now;

            // 오늘의 개장 시각 계산
            var today = new DateTime(now.Year, now.Month, now.Day, open.Hours, open.Minutes, 0);

            // 현재 시각이 오늘 개장 시각 이전이면 오늘, 이후이면 내일
            return now < today ? today : today.AddDays(1);
        }
    }
}