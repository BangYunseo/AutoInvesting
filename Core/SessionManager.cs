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
        /// 운영용 기본 생성자. DI(AddSingleton) 및 실행 경로에서 사용합니다.
        /// GetClient() 호출 시 설정(IS_PAPER_TRADING·KIS 키)에 따라 브로커를 생성합니다.
        /// </summary>
        public SessionManager()
        {
        }

        /// <summary>
        /// 테스트 전용 생성자. 미리 만든 브로커 클라이언트를 주입해 GetClient()가 이를 그대로 반환하게 합니다.
        /// (실계좌·네트워크·DB 없이 컨트롤러 배선을 검증하기 위한 seam — 운영 코드는 파라미터리스 생성자만 사용)
        /// </summary>
        /// <param name="preset">GetClient()가 반환할 브로커 클라이언트(예: FakeBrokerClient)</param>
        public SessionManager(IBrokerClient preset)
        {
            _client = preset;
        }

        /// <summary>
        /// 활성 브로커 클라이언트 반환
        /// </summary>
        public IBrokerClient GetClient()
        {
            if (_client != null) return _client;

            // 0 실전투자, 1 모의투자
            bool isPaperTrading;
            if (AppConfigManager.Get("IS_PAPER_TRADING", "1") == "0")
            {
                isPaperTrading = false;
                Logger.Info("[Session] 거래 모드: 실전투자");
            }
            else
            {
                isPaperTrading = true;
                Logger.Info("[Session] 거래 모드: 모의투자");
            }
            
            // KIS API 키 미확인 시 시뮬레이션 동작(실거래 불가)
            var kisAppKey = AppConfigManager.Get("KIS_APP_KEY", "");
            if (string.IsNullOrEmpty(kisAppKey))
            {
                Logger.Info("[Session] KIS API 키가 없습니다. 시뮬레이션 모드로 동작합니다.");
                _client = new SimBrokerClient();
                return _client;
            }

            var appSecret = AppConfigManager.Get("KIS_APP_SECRET", "");         // 자격증명
            var accountNo = AppConfigManager.Get("KIS_ACCOUNT_NO", "");         // 계좌
            var accountProd = AppConfigManager.Get("KIS_ACCOUNT_PROD", "01");   // 계좌상품코드

            string baseUrl;
            if (isPaperTrading) baseUrl = "https://openapivts.koreainvestment.com:29443";
            else baseUrl = "https://openapi.koreainvestment.com:9443";
            
            Logger.Info($"[Session] KIS API 클라이언트 생성 ({(isPaperTrading ? "모의" : "실전")} 모드)");
            _client = new KisBrokerClient(baseUrl, kisAppKey, appSecret, accountNo, accountProd, isPaperTrading);
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

            // 거래 모드(IS_PAPER_TRADING) 단일 기준: "0"=실전(LIVE), 그 외=모의(PAPER)
            var isPaperTrading = AppConfigManager.Get("IS_PAPER_TRADING", "1") != "0";
            var accountNo = AppConfigManager.Get("KIS_ACCOUNT_NO", "");
            var mode = isPaperTrading ? "PAPER" : "LIVE";
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
