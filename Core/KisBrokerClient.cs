using AutoInvest.Data.DTO;
using AutoInvest.Utils;
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

        public async Task<decimal> GetCurrentPriceAsync(string ticker)
        {
            await _tokenManager.EnsureValidTokenAsync();
            
            // 해외주식 현재가 조회: HHDFS00000300
            string path = $"/uapi/overseas-price/v1/quotations/price?AUTH=&EXCD=NAS&SYMB={ticker}";
            var request = CreateRequest(HttpMethod.Get, path, "HHDFS00000300");

            var response = await _httpClient.SendAsync(request);
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
            await Task.Delay(200); // Rate limit 방지

            string trId = _isPaperTrading ? "VTTS3012R" : "TTTS3012R";
            string path = $"/uapi/overseas-stock/v1/trading/inquire-balance?CANO={_accountNoPrefix}&ACNT_PRDT_CD={_accountNoSuffix}&OVRS_EXCG_CD=NAS&TR_CRCY_CD=USD&CTX_AREA_FK200=&CTX_AREA_NK200=";
            
            var request = CreateRequest(HttpMethod.Get, path, trId);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseString);
            
            var list = new List<HoldingDto>();
            if (json.TryGetProperty("output1", out var output1) && output1.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in output1.EnumerateArray())
                {
                    var ticker = item.GetProperty("ovrs_pdno").GetString() ?? "";
                    if (int.TryParse(item.GetProperty("ccld_qty_smtl1").GetString(), out int qty) && qty > 0)
                    {
                        decimal.TryParse(item.GetProperty("pchs_avg_pric").GetString(), out decimal avgPrice);
                        decimal.TryParse(item.GetProperty("now_pric2").GetString(), out decimal currentPrice);
                        decimal.TryParse(item.GetProperty("evlu_pfls_rt").GetString(), out decimal profitRate);
                        
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

        public async Task<List<OhlcvDto>> GetOhlcvAsync(string ticker, int days)
        {
            await _tokenManager.EnsureValidTokenAsync();
            await Task.Delay(200);

            string path = $"/uapi/overseas-price/v1/quotations/dailyprice?AUTH=&EXCD=NAS&SYMB={ticker}&GUBN=0&BYMD=&MODP=1";
            var request = CreateRequest(HttpMethod.Get, path, "HHDFS76240000");

            var response = await _httpClient.SendAsync(request);
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

                    string dateStr = item.GetProperty("stck_bsop_date").GetString() ?? "";
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
            await Task.Delay(200);

            string trId = isBuy 
                ? (_isPaperTrading ? "VTTT1002U" : "TTTT1002U") 
                : (_isPaperTrading ? "VTTT1006U" : "TTTT1006U");

            string path = "/uapi/overseas-stock/v1/trading/order";
            var request = CreateRequest(HttpMethod.Post, path, trId);

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

            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
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
