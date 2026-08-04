using AutoInvest.Utils;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// KIS (한국투자증권) API OAuth 토큰 관리자.
    /// 발급받은 토큰은 메모리에만 보관하며 만료 시 자동 갱신합니다.
    /// </summary>
    public class KisTokenManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _appKey;
        private readonly string _appSecret;

        private string _accessToken = string.Empty;
        private DateTime _tokenExpiration = DateTime.MinValue;

        /// <summary>토큰 동시 발급 방지 락 — KIS 토큰 발급은 분당 1회 제한이라 경합 시 실패한다.</summary>
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 발급 실패 후 재시도를 보류할 시각. 실패하면 이 시각까지 발급을 아예 시도하지 않는다.
        ///
        /// 발급이 실패하면 토큰이 빈 채로 남아 <see cref="IsExpiringSoon"/>가 계속 true가 되고,
        /// 그러면 API 호출 하나하나가 새 발급을 부른다. KIS는 분당 1회 제한이라 그 재시도가 다시
        /// 실패하고, 실패가 스스로를 재생산해 초당 수 회씩 /oauth2/tokenP를 두드리게 된다
        /// (2026-08-04 배포 중 구·신 인스턴스가 겹치며 실제로 발생).
        /// </summary>
        private DateTime _refreshBlockedUntil = DateTime.MinValue;

        /// <summary>발급 실패 시 보류 시간(초). KIS의 분당 1회 제한이 풀릴 때까지 여유를 둔다.</summary>
        private const int FailureCooldownSeconds = 70;

        public KisTokenManager(HttpClient httpClient, string baseUrl, string appKey, string appSecret)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl;
            _appKey = appKey;
            _appSecret = appSecret;
        }

        /// <summary>
        /// 토큰이 유효한지 확인하고 필요 시 재발급합니다.
        /// </summary>
        public async Task EnsureValidTokenAsync()
        {
            if (!IsExpiringSoon()) return;

            // 동시 요청이 각자 토큰을 발급하면 KIS의 "분당 1회" 제한에 걸려 실패한다.
            // 락으로 직렬화하고, 락 획득 후 재확인해 한 번만 발급하고 나머지는 공유한다.
            await _refreshLock.WaitAsync();
            try
            {
                if (!IsExpiringSoon()) return;

                // 직전 발급이 실패했다면 보류 시간이 지날 때까지 아예 시도하지 않는다.
                // 실패를 그대로 재시도하면 호출 수만큼 발급 요청이 늘어 제한이 계속 갱신된다.
                if (DateTime.Now < _refreshBlockedUntil)
                {
                    int waitSec = (int)Math.Ceiling((_refreshBlockedUntil - DateTime.Now).TotalSeconds);
                    throw new InvalidOperationException(
                        $"직전 토큰 발급이 실패해 {waitSec}초간 재시도를 보류 중입니다 (KIS 분당 1회 제한).");
                }

                await RefreshTokenAsync();
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>토큰이 없거나 만료 10분 전이면 재발급이 필요하다.</summary>
        private bool IsExpiringSoon()
        {
            return string.IsNullOrEmpty(_accessToken) || DateTime.Now >= _tokenExpiration.AddMinutes(-10);
        }

        /// <summary>
        /// 현재 유효한 Access Token을 반환합니다.
        /// </summary>
        public string GetToken()
        {
            return _accessToken;
        }

        private async Task RefreshTokenAsync()
        {
            try
            {
                var url = $"{_baseUrl}/oauth2/tokenP";
                var body = new
                {
                    grant_type = "client_credentials",
                    appkey = _appKey,
                    appsecret = _appSecret
                };

                var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                Logger.Info("[KisToken] KIS API 접근 토큰 발급 요청 중...");
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // 실패 본문에 error_code/error_description이 담긴다. 상태코드만 보면
                    // "분당 1회 제한"과 "앱키 오류"를 구분할 수 없다. 본문에 시크릿은 없다.
                    _refreshBlockedUntil = DateTime.Now.AddSeconds(FailureCooldownSeconds);
                    string detail = responseString.Length > 300 ? responseString.Substring(0, 300) : responseString;
                    Logger.Error($"[KisToken] 토큰 발급 실패 ({(int)response.StatusCode}): {detail} "
                        + $"— {FailureCooldownSeconds}초간 재시도 보류");
                    throw new HttpRequestException($"KIS 토큰 발급 실패 ({(int)response.StatusCode})");
                }

                var json = JsonSerializer.Deserialize<JsonElement>(responseString);

                _accessToken = json.GetProperty("access_token").GetString() ?? "";
                int expiresIn = json.GetProperty("expires_in").GetInt32();
                
                // 유효기간 설정
                _tokenExpiration = DateTime.Now.AddSeconds(expiresIn);
                _refreshBlockedUntil = DateTime.MinValue; // 성공했으니 보류 해제

                Logger.Info($"[KisToken] 토큰 발급 성공 (만료 일시: {_tokenExpiration:yyyy-MM-dd HH:mm:ss})");
            }
            catch (HttpRequestException)
            {
                throw; // 위에서 이미 사유·보류를 기록했다
            }
            catch (Exception ex)
            {
                // 네트워크·파싱 예외도 폭주를 막기 위해 동일하게 보류한다.
                _refreshBlockedUntil = DateTime.Now.AddSeconds(FailureCooldownSeconds);
                Logger.Error($"[KisToken] 토큰 발급 실패: {ex.Message} — {FailureCooldownSeconds}초간 재시도 보류");
                throw;
            }
        }
    }
}
