using System;

namespace AutoInvest.Utils
{
    public static class DateTimeHelper
    {
        public static TimeSpan GetNYSEOpenTimeKST()
        {
            var nyTz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            bool isDST = nyTz.IsDaylightSavingTime(DateTime.UtcNow);
            return isDST ? new TimeSpan(22, 30, 0) : new TimeSpan(23, 30, 0);
        }

        public static DateTime GetNextNYSEOpen()
        {
            var open = GetNYSEOpenTimeKST();
            var now = DateTime.Now;
            var today = new DateTime(now.Year, now.Month, now.Day, open.Hours, open.Minutes, 0);
            return now < today ? today : today.AddDays(1);
        }
    }
}