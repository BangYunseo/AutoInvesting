using AutoInvest.Data;
using AutoInvest.Utils;

namespace AutoInvest.Core
{
    /// <summary>
    /// IBrokerClient 인스턴스의 생명주기를 관리합니다.
    /// KIS증권(한국투자증권) 실거래 구현체(KisBrokerClient)와 SimBrokerClient를 분기합니다.
    ///
    /// TODO [Phase 4] AI 엔진 인스턴스도 SessionManager에서 관리
    ///   - IMarketAnalyzer 생성 + 모델 로딩
    ///   - SmartOrderEngine에 주입
    /// </summary>
    public class SessionManager
    {
        private IBrokerClient? _client;
        private IMarketAnalyzer? _analyzer;

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
        /// 현재 활성 AI 시장분석 엔진을 반환합니다.
        /// AI_PROVIDER 설정값에 따라 Gemini 실물 또는 Mock으로 분기합니다.
        /// </summary>
        public IMarketAnalyzer GetAnalyzer()
        {
            if (_analyzer != null)
                return _analyzer;

            string provider = AppConfigManager.Get("AI_PROVIDER", "mock").ToLower();
            string apiKey = AppConfigManager.Get("GEMINI_API_KEY", "");

            if (provider == "gemini" && !string.IsNullOrWhiteSpace(apiKey))
            {
                Logger.Info("[Session] AI 엔진: GeminiMarketAnalyzer (실물 API 모드)");
                _analyzer = new GeminiMarketAnalyzer(apiKey);
            }
            else
            {
                if (provider == "gemini" && string.IsNullOrWhiteSpace(apiKey))
                    Logger.Warn("[Session] AI_PROVIDER=gemini이나 GEMINI_API_KEY가 없습니다. Mock 모드로 실행합니다.");
                else
                    Logger.Info("[Session] AI 엔진: AiMarketAnalyzer (Mock 모드)");

                _analyzer = new AiMarketAnalyzer();
            }

            return _analyzer;
        }

        /// <summary>
        /// 클라이언트를 초기화합니다 (설정 변경 시 호출).
        /// </summary>
        public void Reset()
        {
            _client = null;
            _analyzer = null;
            Logger.Info("[Session] 세션 초기화");
        }
    }
}
