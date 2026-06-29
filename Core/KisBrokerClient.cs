using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
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

        public async Task<decimal> GetCurrentPriceAsync(string ticker)
        {
            await _tokenManager.EnsureValidTokenAsync();
            await Task.Delay(400); // Rate limit 방지 (신규 키 초당 3건 제한)
            
            // 해외주식 현재가 조회: HHDFS00000300
            string path = $"/uapi/overseas-price/v1/quotations/price?AUTH=&EXCD=NAS&SYMB={ticker}";
            var response = await SendWithRetryAsync(() => CreateRequest(HttpMethod.Get, path, "HHDFS00000300"));

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseString);
            
            if (json.TryGetProperty("output", out var output) && output.TryGetProperty("last", out var lastStr))
            {
                if (decimal.TryParse(lastStr.GetString(), out decimal price))
                {
                    Logger.Info($"[KisBroker] 현재가 조회: {ticker} = ${price}");
                    return price;
                }
            }
            
            Logger.Warn($"[KisBroker] {ticker} 현재가 조회 실패. 기본값 반환.");
            return 0m;
        }

        public async Task<(decimal High, decimal Low)> GetPriceRangeAsync(string ticker, int days)
        {
            var ohlcvList = await GetOhlcvAsync(ticker, days);
            
            if (ohlcvList.Count == 0) return (0m, 0m);

            decimal high = 0m;
            decimal low = decimal.MaxValue;

            foreach (var item in ohlcvList)
            {
                if (item.High > high) high = item.High;
                if (item.Low < low) low = item.Low;
            }

            Logger.Info($"[KisBroker] {days}일 가격범위: {ticker} High=${high} Low=${low}");
            return (high, low);
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
                        && int.TryParse(qtyProp.GetString(), out int qty) && qty > 0)
                    {
                        decimal avgPrice = 0m, currentPrice = 0m, profitRate = 0m;
                        if (item.TryGetProperty("pchs_avg_pric", out var ap)) decimal.TryParse(ap.GetString(), out avgPrice);
                        if (item.TryGetProperty("now_pric2", out var cp)) decimal.TryParse(cp.GetString(), out currentPrice);
                        if (item.TryGetProperty("evlu_pfls_rt", out var pr)) decimal.TryParse(pr.GetString(), out profitRate);

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
                    if (decimal.TryParse(cashProp.GetString(), out decimal cash))
                    {
                        Logger.Info($"[KisBroker] 예수금 조회: ${cash:N2}");
                        return cash;
                    }
                }
            }

            Logger.Warn("[KisBroker] 예수금 조회 실패. 기본값 0 반환.");
            return 0m;
        }

        public async Task<List<OhlcvDto>> GetOhlcvAsync(string ticker, int days)
        {
            await _tokenManager.EnsureValidTokenAsync();
            await Task.Delay(400); // Rate limit 방지 (초당 3건 제한)

            string path = $"/uapi/overseas-price/v1/quotations/dailyprice?AUTH=&EXCD=NAS&SYMB={ticker}&GUBN=0&BYMD=&MODP=1";
            var response = await SendWithRetryAsync(() => CreateRequest(HttpMethod.Get, path, "HHDFS76240000"));

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseString);

            var result = new List<OhlcvDto>();
            if (json.TryGetProperty("output2", out var output2) && output2.ValueKind == JsonValueKind.Array)
            {
                int count = 0;
                foreach (var item in output2.EnumerateArray())
                {
                    if (count >= days) break;

                    string dateStr = item.TryGetProperty("xymd", out var d1) ? d1.GetString() ?? "" 
                        : (item.TryGetProperty("stck_bsop_date", out var d2) ? d2.GetString() ?? "" : "");
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    {
                        decimal.TryParse(item.GetProperty("open").GetString(), out decimal open);
                        decimal.TryParse(item.GetProperty("high").GetString(), out decimal high);
                        decimal.TryParse(item.GetProperty("low").GetString(), out decimal low);
                        decimal.TryParse(item.GetProperty("clos").GetString(), out decimal close);
                        long.TryParse(item.GetProperty("tvol").GetString(), out long volume);

                        result.Add(new OhlcvDto
                        {
                            Date = date,
                            Open = open,
                            High = high,
                            Low = low,
                            Close = close,
                            Volume = volume
                        });
                        count++;
                    }
                }
            }
            
            result.Reverse();

            Logger.Info($"[KisBroker] OHLCV 조회: {ticker} {result.Count}일치");
            return result;
        }

        public async Task<string> PlaceBuyOrderAsync(string ticker, int qty, decimal price)
        {
            return await PlaceOrderAsync(ticker, qty, price, true);
        }

        public async Task<string> PlaceSellOrderAsync(string ticker, int qty, decimal price)
        {
            return await PlaceOrderAsync(ticker, qty, price, false);
        }

        private async Task<string> PlaceOrderAsync(string ticker, int qty, decimal price, bool isBuy)
        {
            await _tokenManager.EnsureValidTokenAsync();
            await Task.Delay(400); // Rate limit 방지 (초당 3건 제한)

            string trId = isBuy 
                ? (_isPaperTrading ? "VTTT1002U" : "TTTT1002U") 
                : (_isPaperTrading ? "VTTT1006U" : "TTTT1006U");

            string path = "/uapi/overseas-stock/v1/trading/order";
            
            var body = new
            {
                CANO = _accountNoPrefix,
                ACNT_PRDT_CD = _accountNoSuffix,
                OVRS_EXCG_CD = "NAS",
                PDNO = ticker,
                ORD_QTY = qty.ToString(),
                OVRS_ORD_UNPR = price.ToString("0.00"),
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
