# C# 코딩 컨벤션

## 네이밍 규칙

> 예시는 현재 코드베이스에 실제로 존재하는 심볼만 씁니다. Phase 6에서 제거된 판단 레이어
> (`SmartOrderEngine`·`QuantFilter`·`Core/Quant/`·`Core/Advisors/` 등)를 예시로 되살리지 마세요.

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스/구조체 | PascalCase | `DcaAccumulationEngine`, `DcaTemplate` |
| 인터페이스 | `I` + PascalCase | `IBrokerClient` |
| public 메서드/프로퍼티 | PascalCase | `GetCurrentPriceAsync()`, `IsLoggedIn` |
| private 필드 | `_camelCase` | `_broker`, `_rangeDays` |
| 지역 변수/파라미터 | camelCase | `exchangeRate`, `investAmount` |
| 상수 / static readonly | PascalCase | `DcaSettings.TemplatesKey` |
| enum 값 | UPPER_SNAKE_CASE | `ORDER_TYPE_BUY` |
| 비동기 메서드 | `~Async` 접미사 | `LoginAsync()`, `PlaceBuyOrderAsync()` |

## 파일 배치 규칙 (MUST)

| 분류 | 경로 | 예시 |
|------|------|------|
| DTO (데이터 전송 객체) | `Data/DTO/` | `HoldingDto.cs` |
| DAO (DB 접근) | `Data/DAO/` | `TradeHistoryDAO.cs` |
| 비즈니스 로직/엔진 | `Core/` | `DcaAccumulationEngine.cs` |
| REST API 컨트롤러 | `Controllers/` | `OrderController.cs` |
| 일일 실행 진입점 | `Core/` | `DailyExecutionService.cs` |
| 유틸리티 / 통신 | `Utils/` | `Logger.cs`, `NotificationService.cs` |

## 파일 구조
- 파일당 하나의 public 클래스/인터페이스
- 파일명 = 클래스명 (예: `DcaAccumulationEngine.cs`)
- 네임스페이스는 폴더 구조를 반영 (예: `AutoInvest.Controllers`)
- `using` 문은 네임스페이스 밖에 배치

## 코드 스타일
- `var`는 타입이 명확한 경우에만 사용
- 중괄호는 별도 줄에 배치 (Allman 스타일)
- nullable 참조 타입 (`?`) 적극 활용
- 문자열 보간(`$""`)을 `string.Format()` 대신 사용
- 매직 넘버 대신 상수 또는 `appsettings.json` 설정값 활용

## 주석 규칙

### XML 주석 (MUST)
모든 `public` 클래스, 메서드, 프로퍼티(특히 Controller의 액션 메서드)에 XML 주석을 작성합니다.

```csharp
/// <summary>
/// 설정한 종목별 고정 수량을 매수하고 체결분을 거래이력에 기록합니다.
/// </summary>
/// <param name="quantities">종목별 매수 수량 맵</param>
/// <param name="budgetKrw">이번 사이클 예산 (원, 초과 경고용 상한)</param>
public async Task<DcaCycleResult> AccumulateAsync(Dictionary<string, int> quantities, decimal budgetKrw)
```

### 인라인 주석
코드 블록의 목적을 설명할 때 `// ── 제목 ──` 형식을 사용합니다.

```csharp
// ── 순수 매수 계획 산출 (고정 수량) ──
var plan = PlanPurchases(quantities, exchangeRate, priceUsd, out decimal totalCostKrw);
```

### TODO 주석
```csharp
// TODO [운영] 실거래 전환 시 KIS Server를 실전으로 변경
```

## DTO 작성 규칙
- 순수 데이터 객체 — 비즈니스 로직 포함 금지
- 프로퍼티는 auto-property 사용
- 기본값 지정 권장 (`= string.Empty`, `= 0`)

```csharp
public class HoldingDto
{
    public string Ticker { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal AvgPrice { get; set; }
}
```

## DAO 작성 규칙
- `static` 메서드로 작성 (Singleton DBManager 사용)
- 모든 DB 연결은 `using` 블록으로 관리
- SQL 파라미터 바인딩 필수 (SQL Injection 방지)

```csharp
public static List<TradeHistoryDto> GetRecent(int count = 50)
{
    var list = new List<TradeHistoryDto>();
    using (var conn = DBManager.Instance.GetConnection())
    using (var cmd = new NpgsqlCommand(
        "SELECT TRADE_ID, TRADE_DATE, TICKER, ORDER_TYPE, QTY, PRICE, STATUS " +
        "FROM TB_TRADE_HISTORY ORDER BY TRADE_DATE DESC LIMIT @count", conn))
    {
        cmd.Parameters.AddWithValue("@count", count);
        using (var rdr = cmd.ExecuteReader())
            while (rdr.Read())
                list.Add(new TradeHistoryDto
                {
                    TradeId = rdr.GetInt32(0),
                    TradeDate = DateTime.Parse(rdr.GetString(1)),
                    Ticker = rdr.GetString(2),
                    OrderType = rdr.GetString(3),
                    Qty = rdr.GetInt32(4),
                    Price = rdr.GetDecimal(5),
                    Status = rdr.GetString(6)
                });
    }
    return list;
}
```

## 예외 처리 패턴

| 계층 | 처리 방법 |
|------|-----------| 
| DAO | catch → `Logger.Error()` + 빈 결과 반환 또는 재throw |
| Core/Engine | catch → `Logger.Error()` + 안전한 기본값 반환 + 필요시 NotificationService 연동 |
| Controllers | 전역 예외 처리 미들웨어 사용 또는 catch → HTTP 500 응답 반환 |
| Background | catch → `Logger.Error()` 후 다음 주기 실행까지 대기 |
| Program.cs | 전역 catch → `Logger.Fatal()` |

```csharp
// ❌ 절대 금지
catch { }
catch (Exception) { return null; }

// ✅ 표준 패턴 (API Controller)
catch (Exception ex)
{
    Logger.Error($"[API] 수동 주문 실패: {ex.Message}");
    return StatusCode(500, "서버 내부 오류가 발생했습니다.");
}
```
