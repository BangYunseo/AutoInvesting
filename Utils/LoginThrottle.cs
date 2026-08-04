using System;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 로그인 실패 속도를 <b>서비스 전체에서 하나의 창(window)으로</b> 제한합니다.
    ///
    /// 호출자별(IP·헤더) 카운터를 쓰지 않습니다. 이 서비스는 리버스 프록시 뒤에 있어 신뢰할 수 있는
    /// 발신지 식별자가 없고, <c>X-Forwarded-For</c>는 클라이언트가 정하는 값이라 다음이 모두 성립합니다.
    ///   (1) 매 요청 헤더를 바꾸면 카운터가 오르지 않아 제한이 무력해진다.
    ///   (2) 소유자 IP로 위조하면 소유자만 골라 잠글 수 있다(표적 DoS).
    ///   (3) 키를 무한히 만들 수 있어 추적 항목이 증가한다.
    /// 관리자 계정이 하나뿐이므로 전역 카운터 하나면 충분하며, 위조할 키 자체가 없습니다.
    ///
    /// 하드 잠금이 아니라 <b>속도 상한</b>이라 창이 지나면 저절로 풀립니다(소유자 영구 차단 방지).
    /// 상한에 걸린 요청은 반드시 비밀번호 검증(PBKDF2 12만 회) <b>이전에</b> 잘라내야
    /// 온라인 추측과 CPU 소모 공격을 같은 지점에서 막습니다.
    ///
    /// 현재 시각을 내부에서 읽지 않고 인자로 받으므로 단위 검증이 가능합니다.
    ///
    /// ponytail: 고정 창이라 창 경계에서 최대 2배까지 몰릴 수 있다. 단일 계정 개인 서비스에서는 무해하다.
    /// 인메모리라 인스턴스가 재시작하면 초기화된다(단일 인스턴스 전제 — 늘리면 공유 저장소 필요).
    /// </summary>
    public static class LoginThrottle
    {
        /// <summary>한 창 안에서 허용하는 최대 로그인 실패 횟수.</summary>
        public const int MaxFailsPerWindow = 20;

        /// <summary>실패 횟수를 세는 창의 길이. 창이 지나면 카운터가 0으로 돌아갑니다.</summary>
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        private static readonly object _gate = new object();
        private static int _fails;
        private static DateTime _windowStartUtc = DateTime.MinValue;

        /// <summary>
        /// 지금 로그인 시도를 거부해야 하는지 확인합니다. 비밀번호 검증 전에 호출하세요.
        /// </summary>
        /// <param name="nowUtc">현재 UTC 시각</param>
        /// <param name="retryAfter">현재 창이 끝날 때까지 남은 시간 (제한 중이 아니면 <see cref="TimeSpan.Zero"/>)</param>
        /// <returns>상한을 초과해 거부해야 하면 true</returns>
        public static bool IsRateLimited(DateTime nowUtc, out TimeSpan retryAfter)
        {
            lock (_gate)
            {
                if (nowUtc - _windowStartUtc >= Window || _fails < MaxFailsPerWindow)
                {
                    retryAfter = TimeSpan.Zero;
                    return false;
                }

                retryAfter = _windowStartUtc + Window - nowUtc;
                if (retryAfter < TimeSpan.Zero) retryAfter = TimeSpan.Zero;
                return true;
            }
        }

        /// <summary>로그인 실패 1회를 기록합니다. 창이 지났으면 카운터를 먼저 0으로 되돌립니다.</summary>
        /// <param name="nowUtc">현재 UTC 시각</param>
        public static void RegisterFailure(DateTime nowUtc)
        {
            lock (_gate)
            {
                if (nowUtc - _windowStartUtc >= Window)
                {
                    _windowStartUtc = nowUtc;
                    _fails = 0;
                }
                _fails++;
            }
        }

        /// <summary>실패 카운터를 비웁니다 (로그인 성공 시, 그리고 테스트 격리용).</summary>
        public static void Reset()
        {
            lock (_gate)
            {
                _fails = 0;
                _windowStartUtc = DateTime.MinValue;
            }
        }
    }
}
