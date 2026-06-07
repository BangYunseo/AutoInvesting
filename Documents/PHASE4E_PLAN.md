# Phase 4-e 개발 계획 — 확률 기반 합의 스코어링 시스템

> **작성일**: 2026-06-07  
> **현재 단계**: Phase 4-d 완료 후 다음 개발 예정  
> **목표**: 만장일치 합의(0 or 1)를 확률 기반 연속 점수(0.0~1.0)로 대체하여, 매매 신호 투명성과 신호 발생 빈도를 동시에 개선

---

## 배경 및 동기

### 현재 시스템(Phase 4-d)의 한계

Phase 4-d에서 구현된 **3자 만장일치 합의**는 방어적 투자 측면에서 우수하나, 실운용 시 아래 문제가 예상됩니다.

```
케이스: 퀀트=BUY, 차트AI=BUY(확신도:0.78), 펀더멘털AI=HOLD(확신도:0.55)
  → 현재: 펀더멘털AI 한 명이 이견이므로 무조건 HOLD
  → 문제: "78% 확신 + 40% 퀀트 기여"라는 합산 근거가 있어도 신호 자체가 발생하지 않음
```

퀀트 BUY 발생 후 두 AI 에이전트가 모두 BUY에 동의하는 케이스는 **실제 운용에서 드문 사건**입니다. 특히 TLT(장기 채권), QQQ(기술주 금리 민감) 계열은 펀더멘털AI가 자주 HOLD를 내므로 만장일치 달성률이 25~45% 수준으로 추정됩니다.

### 개선 방향

각 에이전트의 의견을 **가중치 × 확신도**로 환산하여 합산하고, 합산 확률이 설정된 임계값을 초과할 때 매매를 실행합니다. "왜 이 시점에 매수했는가"를 수치로 추적 가능하게 합니다.

---

## 핵심 설계: 가중치 확률 합산 (Weighted Probability Scoring)

### 계산 공식

```
BuyProbability  = 퀀트기여 + 차트AI기여 + 펀더멘털AI기여
SellProbability = (동일 구조, SELL 신호 기준 계산)

퀀트기여        = QUANT_WEIGHT           (BUY 충족 시 고정값, 미충족 시 0)
차트AI기여      = CHART_AI_WEIGHT × 차트AI확신도  (차트AI = BUY 신호일 때만)
펀더멘털AI기여  = FUND_AI_WEIGHT  × 펀더멘털AI확신도 (펀더멘털AI = BUY 신호일 때만)

기본 가중치:
  QUANT_WEIGHT     = 0.40  (40%)
  CHART_AI_WEIGHT  = 0.30  (30%)
  FUND_AI_WEIGHT   = 0.30  (30%)
  합계             = 1.00  (100%)

임계값:
  BUY_THRESHOLD  = 0.65  (매수 실행 조건)
  SELL_THRESHOLD = 0.65  (매도 실행 조건)
```

### 판단 흐름

```
BuyProbability  >= BUY_THRESHOLD  → BUY 실행
SellProbability >= SELL_THRESHOLD → SELL 실행
둘 다 미달                        → HOLD (확률 로그 기록)
```

### 퀀트 1차 관문은 유지

퀀트가 HOLD면 AI 에이전트 의견에 무관하게 **BuyProbability 최대값이 60%**(AI 에이전트 기여 합산 최대)로 제한됩니다. 임계값(65%) 미달로 자동 방어되므로, 별도의 if 분기 없이 수식만으로 퀀트 1차 관문이 유지됩니다.

```
예: 퀀트=HOLD, 차트=BUY(0.9), 펀더멘털=BUY(0.8)
BuyProbability = 0 + 0.30×0.9 + 0.30×0.8 = 27% + 24% = 51% → HOLD
```

---

## ETF 유형별 예상 확률 시뮬레이션

> 퀀트 BUY 조건이 충족된 상황에서 Gemini가 낼 법한 현실적 확신도 범위를 기준으로 계산

### 시나리오 A: 두 AI 모두 BUY 동의

| ETF | 차트AI 확신도 | 펀더멘털AI 확신도 | BuyProbability | 0.65 | 0.70 |
|-----|:---:|:---:|:---:|:---:|:---:|
| QQQ (기술주, 금리 우호) | 0.76 | 0.62 | **81.4%** | ✅ | ✅ |
| SPY (광범위 시장)       | 0.70 | 0.68 | **81.4%** | ✅ | ✅ |
| GLD (금, 인플레이션)    | 0.68 | 0.75 | **82.9%** | ✅ | ✅ |
| TLT (채권, 금리 피크)   | 0.73 | 0.68 | **82.3%** | ✅ | ✅ |

### 시나리오 B: 차트AI=BUY, 펀더멘털AI=HOLD (현재 만장일치에서는 모두 HOLD)

| ETF | 차트AI 확신도 | 펀더멘털AI | BuyProbability | 0.65 | 0.70 |
|-----|:---:|:---:|:---:|:---:|:---:|
| QQQ (금리 인상 우려)      | 0.72 | HOLD | **61.6%** | ❌ | ❌ |
| QQQ (금리 중립)           | 0.85 | HOLD | **65.5%** | ✅ | ❌ |
| SPY (경기 침체 초반)      | 0.65 | HOLD | **59.5%** | ❌ | ❌ |
| GLD (달러 강세 우려)      | 0.70 | HOLD | **61.0%** | ❌ | ❌ |
| TLT (금리 방향 불확실)    | 0.70 | HOLD | **61.0%** | ❌ | ❌ |

> **시나리오 B 인사이트**: 차트AI가 확신도 0.83 이상일 때만 펀더멘털 HOLD를 극복 가능.
> 현실적으로 한 에이전트 HOLD = 대부분 필터링되는 준만장일치 수준.

### ETF 유형별 0.65 임계값 달성률 (퀀트 BUY 기준)

| ETF 유형 | 0.65 달성률 | 0.70 달성률 | 특징 |
|----------|:-----------:|:-----------:|------|
| QQQ / QQQM (기술주) | ~65% | ~45% | 금리 환경에 따라 펀더멘털AI 반응 큰 편차 |
| SPY (광범위)         | ~70% | ~50% | 가장 안정적, 신호 발생 빈도 높음 |
| GLD (금/원자재)      | ~68% | ~52% | 인플레이션 우호 시 두 AI 모두 적극적 |
| TLT (장기채권)       | ~40% | ~25% | 금리 불확실성에 펀더멘털AI가 자주 HOLD |

---

## 임계값 선택 가이드

### 0.65 (보통, 권장 시작값)

```
장점:
  - 차트AI + 펀더멘털AI 모두 BUY(평균 0.65 확신도) 시 안정적 통과
  - 한 AI가 HOLD여도 차트AI 확신도가 높으면(0.83+) 간신히 통과
  - 5개 종목 기준 일주일에 1~3회 수준의 신호 예상

단점:
  - 한 에이전트가 매우 확신도 높은 BUY를 낼 경우 단독으로도 영향
```

### 0.70 (보수)

```
장점:
  - 두 AI 모두 확신도 0.67 이상 BUY여야 통과 → 진입 신뢰도 극도로 높음
  - TLT, 채권 계열은 사실상 신호 차단

단점:
  - 5개 종목 기준 주 0~1회 신호 발생 가능성
  - 종목이 적을수록 장기간 매매 없는 구간 발생 위험
```

### 운용 권장 시나리오

```
1단계 (초기 2주): BUY_THRESHOLD = 0.65
   → 로그에서 발생 빈도와 에이전트 기여 분포 모니터링

2단계 (관찰 후): 주 3회 이상 신호 발생 시 → 0.68로 상향
                  주 0~1회 이하 신호 발생 시 → 0.62로 하향

3단계 (Phase 4-e 검증): 6주 수익률 비교 후 최적값 확정
```

---

## 로그 출력 형식 (Phase 4-e 구현 후)

```
[SmartOrder] [MEAN_REVERSION] QQQ 최종 판정: BUY ✅
  ├── 퀀트       : BUY  → +40.0%
  ├── 차트AI     : BUY (확신도:0.76) → +22.8%
  └── 펀더멘털AI : BUY (확신도:0.62) → +18.6%
  ─────────────────────────────────────
  매수 확률: 81.4% ≥ 65.0% (임계값) → 매수 실행

[SmartOrder] [MEAN_REVERSION] TLT 최종 판정: HOLD ⚠️
  ├── 퀀트       : BUY  → +40.0%
  ├── 차트AI     : BUY (확신도:0.70) → +21.0%
  └── 펀더멘털AI : HOLD(확신도:0.58) →  +0.0%
  ─────────────────────────────────────
  매수 확률: 61.0% < 65.0% (임계값) → HOLD
  미달 원인: 펀더멘털AI 미동의 — "금리 방향 불확실, 채권 진입 재고 권장"
```

---

## 구현 대상 파일

| 분류 | 파일 | 변경 내용 |
|------|------|----------|
| **[NEW] DTO** | `Data/DTO/ConsensusScoreDto.cs` | 확률 분해 결과 보관 (BuyProbability, SellProbability, 각 에이전트 기여도, 임계값 달성 여부) |
| **[MODIFY]** | `Core/SmartOrderEngine.cs` | `CombineSignals()` → `CalculateConsensusScore()` 대체. 가중치·임계값 설정값 참조 |
| **[MODIFY]** | `Core/SmartOrderResult` | `ConsensusScore` 필드 추가 |
| **[MODIFY]** | `appsettings.json` | `QUANT_WEIGHT`, `CHART_AI_WEIGHT`, `FUND_AI_WEIGHT`, `BUY_THRESHOLD`, `SELL_THRESHOLD` 키 추가 |
| **[MODIFY]** | `Documents/DEVELOPMENT.md` | Phase 4-e 변경 이력 추가 |

---

## ConsensusScoreDto 설계 (참고)

```csharp
public class ConsensusScoreDto
{
    /// <summary>최종 매수 확률 (0.0 ~ 1.0)</summary>
    public decimal BuyProbability { get; set; }

    /// <summary>최종 매도 확률 (0.0 ~ 1.0)</summary>
    public decimal SellProbability { get; set; }

    /// <summary>퀀트 기여도</summary>
    public decimal QuantContribution { get; set; }

    /// <summary>차트 AI 에이전트 기여도</summary>
    public decimal ChartAiContribution { get; set; }

    /// <summary>펀더멘털 AI 에이전트 기여도</summary>
    public decimal FundamentalAiContribution { get; set; }

    /// <summary>적용된 임계값</summary>
    public decimal Threshold { get; set; }

    /// <summary>임계값 달성 여부</summary>
    public bool ThresholdMet => BuyProbability >= Threshold || SellProbability >= Threshold;

    /// <summary>임계값 미달 시 부족분 (얼마나 아쉬운지)</summary>
    public decimal BuyGap => Threshold - BuyProbability;
}
```

---

## 검증 계획

### 시뮬레이션 검증 (구현 직후)
- `IS_PAPER_TRADING=1`, `AI_PROVIDER=gemini` 환경에서 2주 운영
- 로그에서 BuyProbability 분포 집계: 평균값, 최빈값, 임계값 미달 원인 비율

### A/B 비교 (2주 후)
- `BUY_THRESHOLD=0.65` vs `BUY_THRESHOLD=0.70` 번갈아 적용
- 신호 발생 빈도와 시뮬레이션 수익률 비교

### 알고리즘 고도화 검토 (Phase 5)
- TB_MARKET_SNAPSHOT 누적 데이터에서 **실제 성공한 BUY의 BuyProbability 분포** 분석
- "80% 이상이었던 BUY가 실제 수익률이 얼마였는가" 피드백 루프 구축
- 가중치 자동 튜닝(강화학습 방향) 연구

---

## 관련 문서

- [DEVELOPMENT.md](./DEVELOPMENT.md) — 전체 개발 이력
- [CODE_READING_GUIDE.md](./CODE_READING_GUIDE.md) — SmartOrderEngine 코드 흐름

---

*이 문서는 Phase 4-e 구현 시작 시 Implementation Plan으로 전환됩니다.*
