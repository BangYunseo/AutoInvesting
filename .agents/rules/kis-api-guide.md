---
trigger: always_on
---

# KIS (한국투자증권) API 가이드

## API 환경
| 구분 | 도메인 |
|------|--------|
| 실전투자 | `https://openapi.koreainvestment.com:9443` |
| 모의투자 | `https://openapivts.koreainvestment.com:29443` |

- TLS 1.2 이상 필수
- 공식 포털: https://apiportal.koreainvestment.com/
- 공식 GitHub: https://github.com/koreainvestment/open-trading-api

## 인증 (OAuth 2.0)
- 토큰 발급: `POST /oauth2/tokenP`
- 요청 Body: `{ "grant_type": "client_credentials", "appkey": "...", "appsecret": "..." }`
- 토큰 유효기간: **24시간**
- 만료 전 자동 갱신 로직 필수 구현 → `security.md` 참조

## REST API 공통 헤더
```
Content-Type: application/json; charset=utf-8
authorization: Bearer {ACCESS_TOKEN}
appkey: {APP_KEY}
appsecret: {APP_SECRET}
tr_id: {TR_CODE}
```

## IBrokerClient ↔ KIS API 매핑
| 메서드 | HTTP | KIS 엔드포인트 | tr_id (실전/모의) |
|--------|------|---------------|-------------------|
| LoginAsync | POST | /oauth2/tokenP | — |
| GetCurrentPriceAsync | GET | /uapi/overseas-price/v1/quotations/price | HHDFS00000300 |
| GetPriceRangeAsync | GET | /uapi/overseas-price/v1/quotations/dailyprice | HHDFS76240000 |
| GetOhlcvAsync | GET | /uapi/overseas-price/v1/quotations/dailyprice | HHDFS76240000 |
| GetExchangeRateAsync | — | Frankfurter API 유지 (ExchangeRateService) | — |
| GetHoldingsAsync | GET | /uapi/overseas-stock/v1/trading/inquire-balance | TTTS3012R/VTTS3012R |
| PlaceBuyOrderAsync | POST | /uapi/overseas-stock/v1/trading/order | TTTT1002U/VTTT1002U |
| PlaceSellOrderAsync | POST | /uapi/overseas-stock/v1/trading/order | TTTT1006U/VTTT1006U |

## 실전 vs 모의투자 분기
`SessionManager`에서 `IS_PAPER_TRADING` 설정으로 분기:

| 설정값 | 구현체 | 도메인 |
|--------|--------|--------|
| `"1"` | `SimBrokerClient` | (로컬 시뮬레이션) |
| `"0"` | `KisBrokerClient` | `openapi.koreainvestment.com:9443` |

## Rate Limit
- API별 초당 호출 제한 존재
- 연속 호출 시 최소 **200ms** 딜레이 삽입
- 429 응답 시 지수 백오프(Exponential Backoff) 적용
- 모의투자 환경은 Rate Limit이 더 낮으므로 주의

## 구현 패턴
```csharp
// HttpClient는 반드시 재사용 (static 또는 싱글턴)
private static readonly HttpClient _httpClient = new HttpClient();

public async Task<decimal> GetCurrentPriceAsync(string ticker)
{
    await _tokenManager.EnsureValidTokenAsync();

    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Add("authorization", $"Bearer {_token}");
    request.Headers.Add("appkey", _appKey);
    request.Headers.Add("appsecret", _appSecret);
    request.Headers.Add("tr_id", "HHDFS00000300");

    var response = await _httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();
    // ...
}
```

## 에러 처리
- 응답의 `rt_cd` 필드로 성공/실패 판단: `"0"` = 성공, 그 외 = 실패
- 실패 시 `msg_cd`와 `msg1` 필드로 에러 내용 확인
- HTTP 4xx/5xx → 로그 + 재시도 (최대 3회)
- 토큰 만료(401) → 자동 재발급 후 재시도
- 네트워크 오류 → `Logger.Error()` + 안전한 실패 처리
- 장 마감 시간대 주문 거부 → 사용자에게 알림
