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
| GetExchangeRateAsync | — | Frankfurter API → ExchangeRate-API 폴백 (ExchangeRateService) | — |
| GetHoldingsAsync | GET | /uapi/overseas-stock/v1/trading/inquire-balance | TTTS3012R/VTTS3012R |
| GetCashBalanceAsync | GET | /uapi/overseas-stock/v1/trading/inquire-present-balance | CTRP6504R (모의는 미지원 → $0) |
| PlaceBuyOrderAsync | POST | /uapi/overseas-stock/v1/trading/order | TTTT1002U/VTTT1002U |
| PlaceSellOrderAsync | POST | /uapi/overseas-stock/v1/trading/order | TTTT1006U/VTTT1006U |

> 시세 조회용 `GetOhlcvAsync`·`GetPriceRangeAsync`(일봉 `HHDFS76240000`)는 판단 레이어 전용이었고
> Phase 6에서 인터페이스·구현체 모두에서 제거되었습니다. 다시 추가하지 마세요.
>
> **예수금은 잔고조회에 없습니다.** 외화 예수금 `frcr_dncl_amt_2`는 체결기준현재잔고
> (`inquire-present-balance`, `CTRP6504R`)의 `output2`에만 있습니다. 잔고조회
> (`inquire-balance`, `TTTS3012R`)의 `output2`는 평가 요약(매입금액·손익·수익률)이라 이 필드가 없어,
> 거기서 찾으면 조용히 $0이 됩니다(2026-08-04 수정). `output2`는 통화별 배열이므로 `crcy_cd`로
> USD 행을 골라야 합니다. 매수가능금액이 필요하면 `inquire-psamount`(`TTTS3007R`)로, 예수금과는
> 미체결 주문에 묶인 금액만큼 다릅니다.
>
> 주문 시 거래소 코드는 하드코딩하지 않습니다. 현재가 조회에서 확인된 `EXCD`(NAS/NYS/AMS)를
> 주문용 `OVRS_EXCG_CD`(NASD/NYSE/AMEX)로 매핑합니다 — 매핑을 건너뛰면 "해당종목정보가 없습니다"로 거부됩니다.

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
