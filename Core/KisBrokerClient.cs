using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// KIS (한국투자증권) API 실거래 브로커 클라이언트.
    /// REST API를 통해 해외주식 시세 조회, 잔고 조회, 주문 등을 수행합니다.
    /// </summary>
    public class KisBrokerClient : IBrokerClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(r => !r.IsSuccessStatusCode && ((int)r.StatusCode >= 500 || (int)r.StatusCode == 429 || (int)r.StatusCode == 408))
            .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt), 30)),
                (outcome, timeSpan, retryCount, context) =>
                {
                    string reason = outcome.Exception != null ? outcome.Exception.Message : $"상태코드: {outcome.Result?.StatusCode}";
                    Logger.Warn($"[KisBroker] API 호출 실패 ({reason}). {retryCount}/10회 재시도 대기: {timeSpan.TotalSeconds}초");
                });

        private readonly KisTokenManager _tokenManager;
        
        private readonly string _baseUrl;
        private readonly string _appKey;
        private readonly string _appSecret;
        private readonly string _accountNoPrefix;
        private readonly string _accountNoSuffix;
        private readonly bool _isPaperTrading;

        private bool _isLoggedIn = false;
        public bool IsLoggedIn => _isLoggedIn;

        public KisBrokerClient(string baseUrl, string appKey, string appSecret, string accountNo, string accountProd, bool isPaperTrading)
        {
            _baseUrl = baseUrl;
            _appKey = appKey;
            _appSecret = appSecret;
            _accountNoPrefix = accountNo;
            _accountNoSuffix = accountProd;
            _isPaperTrading = isPaperTrading;

            _tokenManager = new KisTokenManager(_httpClient, _baseUrl, _appKey, _appSecret);
        }

        public async Task<bool> LoginAsync()
        {
            try
            {
                await _tokenManager.EnsureValidTokenAsync();
                _isLoggedIn = true;
                Logger.Info("[KisBroker] KIS 로그인 (토큰 확인) 성공");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[KisBroker] KIS 로그인 실패: {ex.Message}");
                _isLoggedIn = false;
                return false;
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path, string trId)
        {
            var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
            request.Headers.Add("authorization", $"Bearer {_tokenManager.GetToken()}");
            request.Headers.Add("appkey", _appKey);
            request.Headers.Add("appsecret", _appSecret);
            request.Headers.Add("tr_id", trId);
            return request;
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = requestFactory();
                return await _httpClient.SendAsync(request);
            });
        }

        /// <summary>KIS 현재가 API의 EXCD(거래소) 후보 — 미국 주요 거래소 순회용.</summary>
        private static readonly string[] UsPriceExchanges = { "NAS", "NYS", "AMS" };

        /// <summary>
        /// 현재가 API의 EXCD → 주문/잔고 API의 OVRS_EXCG_CD 매핑.
        /// KIS는 시세 조회(HHDFS00000300)와 주문(TTTT1002U 등)에서 거래소 코드 체계가 다르다.
        /// (예: NYSE Arca ETF인 GLD/SCHD는 시세=AMS, 주문=AMEX. 주문에 시세 코드 "NAS"를 그대로
        ///  쓰면 "해당종목정보가 없습니다"로 거부된다.)
        /// </summary>
        private static readonly Dictionary<string, string> PriceToOrderExchange = new()
        {
            ["NAS"] = "NASD",
            ["NYS"] = "NYSE",
            ["AMS"] = "AMEX",
        };

        /// <summary>종목별로 현재가 조회 시 확인된 EXCD 캐시 — 주문 시 올바른 거래소 코드 결정에 재사용.</summary>
        private readonly ConcurrentDictionary<string, string> _tickerPriceExchange = new();

        public async Task<decimal> GetCurrentPriceAsync(string ticker)
        {
            await _tokenManager.EnsureValidTokenAsync();

            // KIS 현재가 API는 거래소 코드(EXCD)가 필요하다. 종목이 어느 거래소에 있는지
            // 모르므로 NAS(나스닥)→NYS(뉴욕)→AMS(아멕스/NYSE Arca) 순으로 조회해 가격이
            // 잡히는 거래소를 찾는다. (예: GLD는 NAS에 없고 AMS에서 조회됨)
            foreach (var excd in UsPriceExchanges)
            {
                // 한 거래소 조회가 실패해도 전체를 중단(500)하지 않고 다음 거래소를 시도한다.
                try
                {
                    await Task.Delay(400); // Rate limit 방지 (신규 키 초당 3건 제한)

                    // 해외주식 현재가 조회: HHDFS00000300
                    string path = $"/uapi/overseas-price/v1/quotations/price?AUTH=&EXCD={excd}&SYMB={ticker}";
                    var response = await SendWithRetryAsync(() => CreateRequest(HttpMethod.Get, path, "HHDFS00000300"));
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warn($"[KisBroker] {ticker} {excd} 현재가 HTTP {(int)response.StatusCode} — 다음 거래소 시도");
                        continue;
                    }

                    var responseString = await response.Content.ReadAsStringAsync();
                    var json = JsonSerializer.Deserialize<JsonElement>(responseString);

                    // KIS 응답의 숫자는 항상 "293.42" 형식 문자열이다. 소수점이 쉼표인 로케일에서
                    // CurrentCulture로 파싱하면 29342로 읽히므로 InvariantCulture를 반드시 명시한다.
                    if (json.TryGetProperty("output", out var output) &&
                        output.TryGetProperty("last", out var lastStr) &&
                        decimal.TryParse(lastStr.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) && price > 0)
                    {
                        // 이 종목이 확인된 거래소(EXCD)를 캐시해 두어, 주문 시 올바른 OVRS_EXCG_CD로 매핑한다.
                        _tickerPriceExchange[ticker] = excd;
                        Logger.Info($"[KisBroker] 현재가 조회: {ticker} = ${price} (EXCD={excd})");
                        return price;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[KisBroker] {ticker} {excd} 현재가 조회 중 예외({ex.Message}) — 다음 거래소 시도");
                }
            }

            Logger.Warn($"[KisBroker] {ticker} 현재가 조회 실패 (NAS/NYS/AMS 모두 미조회). 0 반환.");
            return 0m;
        }

        public async Task<decimal> GetExchangeRateAsync()
        {
            return await ExchangeRateService.GetUsdKrwAsync();
        }

        public async Task<List<HoldingDto>> GetHoldingsAsync()
        {
            await _tokenManager.EnsureValidTokenAsync();
            await Task.Delay(400); // Rate limit 방지 (초당 3건 제한)

            string trId = _isPaperTrading ? "VTTS3012R" : "TTTS3012R";
            // ⚠️ OVRS_EXCG_CD=NASD 의미는 환경별로 다르다(KIS 공식 스펙 확인):
            //   · 실전(TTTS3012R): NASD='미국전체' → 단일 호출로 NASDAQ+NYSE+AMEX 보유 전량 반환(SPLG/SCHD/GLD 포함) → 정상.
            //   · 모의(VTTS3012R): NASD='나스닥'만 → NYSE/AMEX 보유분(SPLG/SCHD/GLD) 누락. 모의에서 전량 조회가 필요하면
            //     NASD/NYSE/AMEX를 나눠 호출 후 종목코드(ovrs_pdno) 기준 병합해야 한다. (현재 실전 운영이면 단일 NASD로 정상)
            string path = $"/uapi/overseas-stock/v1/trading/inquire-balance?CANO={_accountNoPrefix}&ACNT_PRDT_CD={_accountNoSuffix}&OVRS_EXCG_CD=NASD&TR_CRCY_CD=USD&CTX_AREA_FK200=&CTX_AREA_NK200=";
            
            var response = await SendWithRetryAsync(() => CreateRequest(HttpMethod.Get, path, trId));
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseString);
            
            var list = new List<HoldingDto>();
            if (json.TryGetProperty("output1", out var output1) && output1.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in output1.EnumerateArray())
                {
                    // 보유수량: inquire-balance(TTTS3012R/VTTS3012R)의 정식 필드는 ovrs_cblc_qty(해외잔고수량).
                    // (기존 ccld_qty_smtl1은 '체결기준현재잔고'(CTRP6504R) 전용 필드라 이 응답엔 없어 항상 0건으로 조회되었다)
                    var ticker = item.TryGetProperty("ovrs_pdno", out var tk) ? (tk.GetString() ?? "") : "";
                    if (item.TryGetProperty("ovrs_cblc_qty", out var qtyProp)
                        && int.TryParse(qtyProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) && qty > 0)
                    {
                        decimal avgPrice = 0m, currentPrice = 0m, profitRate = 0m;
                        if (item.TryGetProperty("pchs_avg_pric", out var ap)) decimal.TryParse(ap.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out avgPrice);
                        if (item.TryGetProperty("now_pric2", out var cp)) decimal.TryParse(cp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out currentPrice);
                        if (item.TryGetProperty("evlu_pfls_rt", out var pr)) decimal.TryParse(pr.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out profitRate);

                        list.Add(new HoldingDto
                        {
                            Ticker = ticker,
                            Qty = qty,
                            AvgPrice = avgPrice,
                            CurrentPrice = currentPrice,
                            ProfitRate = profitRate / 100m
                        });
                    }
                }
            }
            
            Logger.Info($"[KisBroker] 보유 종목 {list.Count}건 조회");
            return list;
        }

        public async Task<decimal> GetCashBalanceAsync()
        {
            await _tokenManager.EnsureValidTokenAsync();
            await Task.Delay(400); // Rate limit 방지 (초당 3건 제한)

            // KIS API는 모의투자 환경에서 해외주식 예수금 조회(VTTS3014R)를 미지원하며,
            // 잔고 조회(VTTS3012R)에서도 예수금 필드를 반환하지 않습니다.
            // 가상 잔고로 우회하지 않고, 모의투자 시에는 항상 예수금 $0을 반환합니다.
            if (_isPaperTrading)
            {
                Logger.Info("[KisBroker] KIS 모의투자는 해외주식 예수금 조회를 미지원하므로 예수금 $0 반환");
                return 0m;
            }

            string trId = "TTTS3012R";
            string path = $"/uapi/overseas-stock/v1/trading/inquire-balance?CANO={_accountNoPrefix}&ACNT_PRDT_CD={_accountNoSuffix}&OVRS_EXCG_CD=NASD&TR_CRCY_CD=USD&CTX_AREA_FK200=&CTX_AREA_NK200=";

            var response = await SendWithRetryAsync(() => CreateRequest(HttpMethod.Get, path, trId));
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseString);

            // output2에서 외화 예수금액 파싱
            if (json.TryGetProperty("output2", out var output2))
            {
                // output2가 배열인 경우 첫 번째 요소 사용
                JsonElement target = output2;
                if (output2.ValueKind == JsonValueKind.Array)
                {
                    var enumerator = output2.EnumerateArray();
                    if (enumerator.MoveNext())
                        target = enumerator.Current;
                    else
                    {
                        Logger.Warn("[KisBroker] 예수금 조회: output2 배열이 비어있음. 0 반환.");
                        return 0m;
                    }
                }

                if (target.TryGetProperty("frcr_dncl_amt_2", out var cashProp))
                {
                    if (decimal.TryParse(cashProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cash))
                    {
                        Logger.Info($"[KisBroker] 예수금 조회: ${cash:N2}");
                        return cash;
                    }
                }
            }

            Logger.Warn("[KisBroker] 예수금 조회 실패. 기본값 0 반환.");
            return 0m;
        }

        public async Task<string> PlaceBuyOrderAsync(string ticker, int qty, decimal price)
        {
            return await PlaceOrderAsync(ticker, qty, price, true);
        }

        public async Task<string> PlaceSellOrderAsync(string ticker, int qty, decimal price)
        {
            return await PlaceOrderAsync(ticker, qty, price, false);
        }

        /// <summary>
        /// 주문에 사용할 거래소 코드(OVRS_EXCG_CD)를 종목별로 결정합니다.
        /// 현재가 조회에서 확인된 EXCD가 있으면 매핑해 쓰고, 없으면 현재가를 1회 조회해 확인합니다.
        /// 그래도 확인되지 않으면 나스닥(NASD)을 기본값으로 사용합니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        private async Task<string> ResolveOrderExchangeAsync(string ticker)
        {
            if (!_tickerPriceExchange.TryGetValue(ticker, out var excd))
            {
                // 아직 이 종목의 거래소가 확인되지 않았다면 현재가 조회로 캐시를 채운다(부수효과).
                await GetCurrentPriceAsync(ticker);
                _tickerPriceExchange.TryGetValue(ticker, out excd);
            }

            if (!string.IsNullOrEmpty(excd) && PriceToOrderExchange.TryGetValue(excd, out var orderExcg))
            {
                return orderExcg;
            }

            Logger.Warn($"[KisBroker] {ticker} 거래소 미확인 — 주문 거래소 코드를 기본값 NASD로 적용");
            return "NASD";
        }

        private async Task<string> PlaceOrderAsync(string ticker, int qty, decimal price, bool isBuy)
        {
            await _tokenManager.EnsureValidTokenAsync();

            // 종목이 실제 상장된 거래소 코드로 주문한다(하드코딩 금지). 시세=EXCD → 주문=OVRS_EXCG_CD 매핑.
            string ovrsExcgCd = await ResolveOrderExchangeAsync(ticker);

            await Task.Delay(400); // Rate limit 방지 (초당 3건 제한)

            string trId = isBuy
                ? (_isPaperTrading ? "VTTT1002U" : "TTTT1002U")
                : (_isPaperTrading ? "VTTT1006U" : "TTTT1006U");

            string path = "/uapi/overseas-stock/v1/trading/order";

            var body = new
            {
                CANO = _accountNoPrefix,
                ACNT_PRDT_CD = _accountNoSuffix,
                OVRS_EXCG_CD = ovrsExcgCd,
                PDNO = ticker,
                ORD_QTY = qty.ToString(),
                OVRS_ORD_UNPR = price.ToString("0.00", CultureInfo.InvariantCulture),
                ORD_SVR_DVSN_CD = "0",
                ORD_DVSN = "00"
            };

            var response = await SendWithRetryAsync(() => 
            {
                var request = CreateRequest(HttpMethod.Post, path, trId);
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                return request;
            });

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseString);

            string rtCd = json.GetProperty("rt_cd").GetString() ?? "";
            string msg = json.GetProperty("msg1").GetString() ?? "";

            if (rtCd != "0")
            {
                throw new Exception($"주문 에러: {msg}");
            }

            string orderNo = "";
            if (json.TryGetProperty("output", out var output))
            {
                orderNo = output.GetProperty("ODNO").GetString() ?? Guid.NewGuid().ToString("N").Substring(0, 12);
            }

            string orderType = isBuy ? "매수" : "매도";
            Logger.Info($"[KisBroker] {orderType} 주문 체결: {ticker} {qty}주 @ ${price} (주문번호: {orderNo})");
            return orderNo;
        }
    }
}
