using AutoInvest.Utils;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
            // 만료 10분 전이면 재발급
            if (string.IsNullOrEmpty(_accessToken) || DateTime.Now >= _tokenExpiration.AddMinutes(-10))
            {
                await RefreshTokenAsync();
            }
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
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(responseString);

                _accessToken = json.GetProperty("access_token").GetString() ?? "";
                int expiresIn = json.GetProperty("expires_in").GetInt32();
                
                // 유효기간 설정
                _tokenExpiration = DateTime.Now.AddSeconds(expiresIn);
                
                Logger.Info($"[KisToken] 토큰 발급 성공 (만료 일시: {_tokenExpiration:yyyy-MM-dd HH:mm:ss})");
            }
            catch (Exception ex)
            {
                Logger.Error($"[KisToken] 토큰 발급 실패: {ex.Message}");
                throw;
            }
        }
    }
}
