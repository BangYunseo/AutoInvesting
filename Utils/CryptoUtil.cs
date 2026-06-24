using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 시크릿 암복호화 · 비밀번호 해시 · 세션 토큰 발급/검증을 담당하는 공용 암호화 유틸리티입니다.
    /// 외부 NuGet 없이 <c>System.Security.Cryptography</c>만 사용합니다.
    ///
    /// - 시크릿 저장: AES-256-GCM (마스터 키 = 환경변수/appsettings.local.json의 <c>MASTER_KEY</c>, base64 32바이트)
    /// - 비밀번호: PBKDF2(SHA256)
    /// - 세션 토큰: HMAC-SHA256 서명 (stateless)
    /// </summary>
    public static class CryptoUtil
    {
        private const string EncPrefix = "enc:v1:";   // 암호문 식별 접두사 (없으면 레거시 평문으로 간주)
        private const int PbkdfIterations = 120_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;               // AES-256
        private const int NonceSize = 12;             // GCM 표준 nonce
        private const int TagSize = 16;               // GCM 인증 태그

        private static IConfiguration? _config;
        private static byte[]? _masterKey;
        private static byte[]? _tokenKey;
        private static bool _tokenKeyResolved;

        /// <summary>
        /// IConfiguration을 주입합니다. Program.cs에서 1회 호출.
        /// 환경변수와 appsettings.local.json을 모두 포괄하므로 MASTER_KEY를 어느 쪽에 두어도 인식됩니다.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            _config = configuration;
        }

        // ── 설정값 조회 (IConfiguration → 환경변수) ──
        private static string? GetConfigValue(string key)
            => _config?[key] ?? Environment.GetEnvironmentVariable(key);

        /// <summary>
        /// 마스터 키(32바이트)를 반환합니다. 미설정·형식오류 시 null.
        /// 성공한 값만 캐시하여, Initialize 이전 호출로 인한 영구 null 캐시를 피합니다.
        /// </summary>
        private static byte[]? MasterKey
        {
            get
            {
                if (_masterKey != null) return _masterKey;

                string? raw = GetConfigValue("MASTER_KEY");
                if (string.IsNullOrWhiteSpace(raw)) return null;

                try
                {
                    byte[] bytes = Convert.FromBase64String(raw.Trim());
                    if (bytes.Length != KeySize)
                    {
                        Logger.Error($"[Crypto] MASTER_KEY 길이 오류: {bytes.Length}바이트 (32 필요). 암호화 비활성.");
                        return null;
                    }
                    _masterKey = bytes;
                    return _masterKey;
                }
                catch (FormatException)
                {
                    Logger.Error("[Crypto] MASTER_KEY base64 디코딩 실패. 암호화 비활성.");
                    return null;
                }
            }
        }

        /// <summary>마스터 키가 설정되어 암호화가 가능한지 여부.</summary>
        public static bool IsConfigured => MasterKey != null;

        // ─────────────────────────────────────────────────────────────
        //  시크릿 암복호화 (AES-256-GCM)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 평문 시크릿을 암호화해 "enc:v1:..." 형식으로 반환합니다.
        /// 마스터 키가 없으면 평문을 그대로 반환합니다(호출부가 경고 로깅).
        /// </summary>
        public static string EncryptSecret(string plaintext)
        {
            byte[]? key = MasterKey;
            if (key == null || string.IsNullOrEmpty(plaintext)) return plaintext;

            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] pt = Encoding.UTF8.GetBytes(plaintext);
            byte[] ct = new byte[pt.Length];
            byte[] tag = new byte[TagSize];

            using (var gcm = new AesGcm(key, TagSize))
                gcm.Encrypt(nonce, pt, ct, tag);

            byte[] blob = new byte[NonceSize + TagSize + ct.Length];
            Buffer.BlockCopy(nonce, 0, blob, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, blob, NonceSize, TagSize);
            Buffer.BlockCopy(ct, 0, blob, NonceSize + TagSize, ct.Length);

            return EncPrefix + Convert.ToBase64String(blob);
        }

        /// <summary>
        /// "enc:v1:..." 형식이면 복호화하고, 아니면(레거시 평문) 그대로 반환합니다.
        /// 복호화 실패 시 빈 문자열을 반환해 암호문이 시크릿으로 오용되지 않게 합니다.
        /// </summary>
        public static string DecryptSecret(string stored)
        {
            if (string.IsNullOrEmpty(stored) || !stored.StartsWith(EncPrefix, StringComparison.Ordinal))
                return stored;

            byte[]? key = MasterKey;
            if (key == null)
            {
                Logger.Error("[Crypto] 암호문을 발견했으나 MASTER_KEY가 없어 복호화할 수 없습니다.");
                return string.Empty;
            }

            try
            {
                byte[] blob = Convert.FromBase64String(stored.Substring(EncPrefix.Length));
                var nonce = blob.AsSpan(0, NonceSize);
                var tag = blob.AsSpan(NonceSize, TagSize);
                var ct = blob.AsSpan(NonceSize + TagSize);
                byte[] pt = new byte[ct.Length];

                using (var gcm = new AesGcm(key, TagSize))
                    gcm.Decrypt(nonce, ct, tag, pt);

                return Encoding.UTF8.GetString(pt);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Crypto] 시크릿 복호화 실패: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>값이 암호화된 시크릿인지 여부.</summary>
        public static bool IsEncrypted(string value)
            => !string.IsNullOrEmpty(value) && value.StartsWith(EncPrefix, StringComparison.Ordinal);

        // ─────────────────────────────────────────────────────────────
        //  비밀번호 해시 (PBKDF2-SHA256)
        // ─────────────────────────────────────────────────────────────

        /// <summary>비밀번호를 "pbkdf2:&lt;iter&gt;:&lt;salt&gt;:&lt;hash&gt;" 형식으로 해시합니다.</summary>
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, PbkdfIterations, HashAlgorithmName.SHA256, 32);
            return $"pbkdf2:{PbkdfIterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        /// <summary>저장된 해시와 비밀번호를 상수 시간 비교로 검증합니다.</summary>
        public static bool VerifyPassword(string password, string stored)
        {
            try
            {
                string[] parts = stored.Split(':');
                if (parts.Length != 4 || parts[0] != "pbkdf2") return false;

                int iter = int.Parse(parts[1]);
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, iter, HashAlgorithmName.SHA256, expected.Length);

                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Crypto] 비밀번호 해시 검증 오류: {ex.Message}");
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  세션 토큰 (HMAC-SHA256 서명, stateless)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 토큰 서명 키를 반환합니다. AUTH_TOKEN_SECRET이 있으면 그것을, 없으면 MASTER_KEY에서 파생합니다.
        /// 둘 다 없으면 null(토큰 발급/검증 불가).
        /// </summary>
        private static byte[]? GetTokenKey()
        {
            if (_tokenKeyResolved && _tokenKey != null) return _tokenKey;
            _tokenKeyResolved = true;

            string? secret = GetConfigValue("AUTH_TOKEN_SECRET");
            if (!string.IsNullOrWhiteSpace(secret))
            {
                _tokenKey = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
                return _tokenKey;
            }

            byte[]? mk = MasterKey;
            if (mk != null)
            {
                _tokenKey = Rfc2898DeriveBytes.Pbkdf2(
                    mk, Encoding.UTF8.GetBytes("auth-token-signing-v1"),
                    10_000, HashAlgorithmName.SHA256, 32);
                return _tokenKey;
            }

            return null;
        }

        /// <summary>서명된 세션 토큰을 발급합니다. 서명 키가 없으면 null.</summary>
        public static string? IssueToken(string subject, DateTime expiresUtc)
        {
            byte[]? key = GetTokenKey();
            if (key == null) return null;

            long exp = new DateTimeOffset(expiresUtc.ToUniversalTime()).ToUnixTimeSeconds();
            string payloadJson = JsonSerializer.Serialize(new { sub = subject, exp });
            string payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            string sig = Base64UrlEncode(HmacSha256(key, Encoding.UTF8.GetBytes(payload)));
            return payload + "." + sig;
        }

        /// <summary>토큰 서명·만료를 검증합니다. 유효하면 subject(사용자명)를 돌려줍니다.</summary>
        public static bool TryValidateToken(string token, out string subject)
        {
            subject = string.Empty;
            try
            {
                byte[]? key = GetTokenKey();
                if (key == null || string.IsNullOrEmpty(token)) return false;

                string[] parts = token.Split('.');
                if (parts.Length != 2) return false;

                string expectedSig = Base64UrlEncode(HmacSha256(key, Encoding.UTF8.GetBytes(parts[0])));
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expectedSig), Encoding.UTF8.GetBytes(parts[1])))
                    return false;

                string json = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
                using var doc = JsonDocument.Parse(json);
                long exp = doc.RootElement.GetProperty("exp").GetInt64();
                if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow) return false;

                subject = doc.RootElement.GetProperty("sub").GetString() ?? string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Crypto] 토큰 검증 실패: {ex.Message}");
                return false;
            }
        }

        // ── 내부 헬퍼 ──
        private static byte[] HmacSha256(byte[] key, byte[] data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        private static string Base64UrlEncode(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string s)
        {
            string b64 = s.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4)
            {
                case 2: b64 += "=="; break;
                case 3: b64 += "="; break;
            }
            return Convert.FromBase64String(b64);
        }
    }
}
