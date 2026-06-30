using System;
using AutoInvest.Data;
using AutoInvest.Utils;

namespace AutoInvest.Core
{
    /// <summary>
    /// IBrokerClient 인스턴스의 생명주기를 관리합니다.
    /// KIS증권(한국투자증권) 실거래 구현체(KisBrokerClient)와 SimBrokerClient를 분기합니다.
    /// </summary>
    public class SessionManager
    {
        private IBrokerClient? _client;

        /// <summary>
        /// 현재 활성 브로커 클라이언트를 반환합니다.
        /// 없으면 설정에 따라 새로 생성합니다.
        /// </summary>
        public IBrokerClient GetClient()
        {
            if (_client != null)
                return _client;

            var isPaper = AppConfigManager.Get("IS_PAPER_TRADING", "1");
            var kisAppKey = AppConfigManager.Get("KIS_APP_KEY", "");

            if (isPaper == "1" && string.IsNullOrEmpty(kisAppKey))
            {
                Logger.Info("[Session] KIS API 키가 없어 시뮬레이션 모드(SimBrokerClient)로 시작합니다.");
                _client = new SimBrokerClient();
                return _client;
            }

            if (!string.IsNullOrEmpty(kisAppKey))
            {
                var appSecret = AppConfigManager.Get("KIS_APP_SECRET", "");
                var accountNo = AppConfigManager.Get("KIS_ACCOUNT_NO", "");
                var accountProd = AppConfigManager.Get("KIS_ACCOUNT_PROD", "01");
                var server = AppConfigManager.Get("KIS_SERVER", "vps"); // vps=모의, prod=실전

                string baseUrl = server == "prod" 
                    ? "https://openapi.koreainvestment.com:9443" 
                    : "https://openapivts.koreainvestment.com:29443";
                
                bool isPaperTrading = (server == "vps");

                Logger.Info($"[Session] KIS API 클라이언트 생성 (서버: {server})");
                _client = new KisBrokerClient(baseUrl, kisAppKey, appSecret, accountNo, accountProd, isPaperTrading);
            }
            else
            {
                Logger.Warn("[Session] API 설정이 없어 SimBrokerClient를 생성합니다.");
                _client = new SimBrokerClient();
            }

            return _client;
        }

        /// <summary>
        /// 현재 활성 계좌의 모드와 마스킹된 계좌번호를 반환합니다.
        /// 대시보드의 모의/실거래 구분 표시에 사용합니다.
        /// </summary>
        /// <returns>(Mode: "SIM"|"PAPER"|"LIVE", MaskedAccount: 마스킹된 계좌번호 또는 안내문)</returns>
        public (string Mode, string MaskedAccount) GetAccountInfo()
        {
            var kisAppKey = AppConfigManager.Get("KIS_APP_KEY", "");
            if (string.IsNullOrEmpty(kisAppKey))
            {
                return ("SIM", "시뮬레이션 (로컬)");
            }

            var server = AppConfigManager.Get("KIS_SERVER", "vps"); // vps=모의, prod=실전
            var accountNo = AppConfigManager.Get("KIS_ACCOUNT_NO", "");
            var mode = server == "prod" ? "LIVE" : "PAPER";
            return (mode, MaskAccount(accountNo));
        }

        /// <summary>
        /// 계좌번호를 앞 4자리·끝 2자리만 남기고 마스킹합니다 (로그·응답 노출 방지).
        /// </summary>
        private static string MaskAccount(string account)
        {
            var digits = (account ?? "").Trim();
            if (string.IsNullOrEmpty(digits))
            {
                return "(미설정)";
            }
            if (digits.Length <= 4)
            {
                return new string('*', digits.Length);
            }

            var head = digits.Substring(0, 4);
            var tail = digits.Length >= 6 ? digits.Substring(digits.Length - 2) : "";
            var maskedLen = Math.Max(0, digits.Length - head.Length - tail.Length);
            return $"{head}{new string('*', maskedLen)}{tail}";
        }

        /// <summary>
        /// 클라이언트를 초기화합니다 (설정 변경 시 호출).
        /// </summary>
        public void Reset()
        {
            _client = null;
            Logger.Info("[Session] 세션 초기화");
        }
    }
}
