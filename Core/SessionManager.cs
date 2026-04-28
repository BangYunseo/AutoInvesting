using AutoInvest.Data;
using AutoInvest.Utils;

namespace AutoInvest.Core
{
    /// <summary>
    /// IBrokerClient 인스턴스의 생명주기를 관리합니다.
    /// 현재는 SimBrokerClient만 지원하며, LS증권 실거래 구현 시
    /// IS_PAPER_TRADING 설정에 따라 구현체를 분기합니다.
    ///
    /// TODO [Phase 3] LS증권 실거래 구현체 분기
    ///   - IS_PAPER_TRADING == "0" → new LsBrokerClient(appKey, appSecret)
    ///   - 토큰 만료(익일 07시) 시 자동 재발급 로직
    ///   - 모의투자 / 실전 서버 URL 분리
    ///
    /// TODO [Phase 4] AI 엔진 인스턴스도 SessionManager에서 관리
    ///   - IMarketAnalyzer 생성 + 모델 로딩
    ///   - SmartOrderEngine에 주입
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

            if (isPaper == "1")
            {
                Logger.Info("[Session] 시뮬레이션 모드 — SimBrokerClient 생성");
                _client = new SimBrokerClient();
            }
            else
            {
                // TODO [Phase 3] LS증권 실거래 클라이언트 생성
                //   var appKey = AppConfigManager.Get("LS_APP_KEY", "");
                //   var appSecret = AppConfigManager.Get("LS_APP_SECRET", "");
                //   _client = new LsBrokerClient(appKey, appSecret);
                Logger.Warn("[Session] 실거래 모드가 선택되었으나 LsBrokerClient 미구현 — SimBroker로 대체");
                _client = new SimBrokerClient();
            }

            return _client;
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
