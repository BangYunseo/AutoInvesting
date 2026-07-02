# -*- coding: utf-8 -*-
"""
DCA vs 규칙기반 타이밍 정직한 백테스트 (일회성 분석)
- 미래정보를 쓰지 않는 재현 가능한 전략만 비교
- 데이터: Yahoo Finance chart API (일봉, 최근 15년)
- 목적: "타이밍이 단순 적립(DCA)을 이기는가" + "완벽한 타이밍의 상한"을 실측
주의: 이 스크립트는 프로덕션 코드가 아니라 검증용 분석 도구입니다.
"""
import json, urllib.request, ssl, os, statistics, sys, io
from datetime import datetime, timezone

# Windows 콘솔(cp949)에서도 UTF-8 출력 강제
try:
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
except Exception:
    pass

SYMBOLS = ["VOO", "QQQ", "SPY"]
UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36"
CACHE = os.path.join(os.path.dirname(__file__), "_cache")
os.makedirs(CACHE, exist_ok=True)

def fetch(sym):
    path = os.path.join(CACHE, sym + ".json")
    if os.path.exists(path) and os.path.getsize(path) > 1000:
        return json.load(open(path, encoding="utf-8"))
    url = f"https://query1.finance.yahoo.com/v8/finance/chart/{sym}?range=15y&interval=1d"
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    ctx = ssl.create_default_context()
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE
    with urllib.request.urlopen(req, timeout=40, context=ctx) as r:
        data = r.read().decode("utf-8")
    open(path, "w", encoding="utf-8").write(data)
    return json.loads(data)

def load_daily(sym):
    """returns list of (date, open, high, low, close) with valid closes, sorted by date."""
    d = fetch(sym)
    r = d["chart"]["result"][0]
    ts = r["timestamp"]
    q = r["indicators"]["quote"][0]
    # adjclose 우선 (분배금 반영) — 없으면 close
    adj = r["indicators"].get("adjclose", [{}])[0].get("adjclose")
    rows = []
    for i, t in enumerate(ts):
        c = q["close"][i]
        if c is None:
            continue
        dt = datetime.fromtimestamp(t, tz=timezone.utc).date()
        close = adj[i] if (adj and adj[i] is not None) else c
        lo = q["low"][i] if q["low"][i] is not None else c
        # adjclose 배율을 low에도 적용 (근사): ratio = adjclose/close
        ratio = (adj[i] / c) if (adj and adj[i] and c) else 1.0
        rows.append((dt, close, lo * ratio))
    rows.sort(key=lambda x: x[0])
    return rows  # (date, adjClose, adjLowApprox)

def month_key(dt):
    return (dt.year, dt.month)

def backtest(sym, monthly=1000.0):
    rows = load_daily(sym)
    # 월별 그룹: 각 달의 (첫 거래일 종가, 그 달 최저가, 마지막 거래일 종가)
    months = {}
    for dt, close, low in rows:
        k = month_key(dt)
        if k not in months:
            months[k] = {"first_close": close, "min_low": low, "last_close": close, "first_dt": dt}
        m = months[k]
        m["min_low"] = min(m["min_low"], low)
        m["last_close"] = close
    mkeys = sorted(months.keys())
    # 200일 이평(일봉 기준) — 각 달 첫 거래일 시점의 직전 200 거래일 평균
    closes = [c for _, c, _ in rows]
    dates = [d for d, _, _ in rows]
    sma = [None] * len(closes)
    for i in range(len(closes)):
        if i >= 199:
            sma[i] = sum(closes[i-199:i+1]) / 200.0
    # 각 달 첫 거래일의 인덱스와 그 시점 sma
    first_idx = {}
    seen = set()
    for i, d in enumerate(dates):
        k = month_key(d)
        if k not in seen:
            seen.add(k)
            first_idx[k] = i

    final_close = rows[-1][1]

    def value(shares):
        return shares * final_close

    # 전략별 시뮬레이션 --------------------------------------------------
    # 1) 순수 DCA: 매달 첫 거래일 종가로 monthly 매수
    sh_dca = sum(monthly / months[k]["first_close"] for k in mkeys)

    # 2) 완벽 타이밍(상한): 매달 그 달 최저가로 매수 (사후 정보 — 이론 천장)
    sh_perfect = sum(monthly / months[k]["min_low"] for k in mkeys)

    # 3) 추세추종(200일 이평 위에서만 매수, 아래면 현금 적립 후 이평 위로 복귀하는 첫 달에 누적현금 일괄투입)
    sh_sma = 0.0
    cash = 0.0
    for k in mkeys:
        cash += monthly
        i = first_idx[k]
        s = sma[i]
        px = months[k]["first_close"]
        if s is not None and px > s:
            sh_sma += cash / px
            cash = 0.0
    # 남은 현금은 미투자(마지막에 현금으로 보유) — 종료 시 현금은 주식가치에 안 더함(보수적으로 원금가치)
    sma_leftover_cash = cash

    # 4) 역발상 "떨어지면 더"(밸류애버리징 근사): 목표누적 = 매달 monthly씩 증가.
    #    실제 평가액이 목표보다 낮으면 부족분만큼 추가 매수, 높으면 그 달은 최소 매수(monthly의 일부)만.
    #    현금 무한투입 금지: 그 달 최대 투입 = monthly*3 상한.
    sh_va = 0.0
    target = 0.0
    va_invested = 0.0
    for k in mkeys:
        target += monthly
        px = months[k]["first_close"]
        cur_val = sh_va * px
        need = target - cur_val
        invest = max(0.0, min(need, monthly * 3))
        if invest == 0.0:
            invest = monthly * 0.0  # 목표 초과 시 그 달 스킵(현금 보유)
        sh_va += invest / px
        va_invested += invest

    n = len(mkeys)
    invested_dca = monthly * n
    span_years = (rows[-1][0] - rows[0][0]).days / 365.25

    def cagr(final_val, invested, years):
        # 적립식이라 단순 CAGR은 부정확 → 총 투입 대비 배수와 근사 IRR 대신 배수/연환산 표기
        return None

    return {
        "sym": sym, "months": n, "span_years": round(span_years, 1),
        "invested_dca": invested_dca,
        "dca_value": value(sh_dca),
        "perfect_value": value(sh_perfect),
        "sma_value": value(sh_sma) + sma_leftover_cash,  # 미투자 현금은 명목가치로만 가산
        "sma_leftover_cash": sma_leftover_cash,
        "va_value": value(sh_va),
        "va_invested": va_invested,
    }

def pct(a, b):
    return (a / b - 1.0) * 100.0

def irr_monthly_equal(monthly, n, final_value):
    """매달 monthly씩 n번 납입(월초), 종료 시 final_value일 때의 월수익률 IRR을 이분법으로 구해 연환산."""
    def fv(r):
        # 납입 i(0..n-1)는 (n-i)개월 복리 성장
        return sum(monthly * (1 + r) ** (n - i) for i in range(n))
    lo, hi = -0.9, 1.0
    for _ in range(200):
        mid = (lo + hi) / 2
        if fv(mid) > final_value:
            hi = mid
        else:
            lo = mid
    r = (lo + hi) / 2
    return (1 + r) ** 12 - 1  # 연환산

print("데이터: Yahoo Finance 일봉(adjClose, 분배금 반영), 최근 15년. 월 $1,000 정액 적립 가정.\n")
print(f"{'ETF':<5}{'개월':>5}{'년':>5}{'DCA가치':>11}{'완벽타이밍':>11}{'추세추종':>11}{'밸류AVG':>11}")
rows_out = []
for s in SYMBOLS:
    try:
        r = backtest(s)
    except Exception as e:
        print(s, "ERROR", e); continue
    rows_out.append(r)
    print(f"{r['sym']:<5}{r['months']:>5}{r['span_years']:>5}{r['dca_value']:>11,.0f}"
          f"{r['perfect_value']:>11,.0f}{r['sma_value']:>11,.0f}{r['va_value']:>11,.0f}")

print("\n[1] 연환산 IRR (동일 현금흐름: 매달 $1,000) — DCA vs 완벽타이밍")
print(f"{'ETF':<5}{'DCA IRR':>10}{'완벽 IRR':>10}{'우위(연%p)':>12}")
for r in rows_out:
    dca_irr = irr_monthly_equal(1000.0, r['months'], r['dca_value']) * 100
    prf_irr = irr_monthly_equal(1000.0, r['months'], r['perfect_value']) * 100
    print(f"{r['sym']:<5}{dca_irr:>9.2f}%{prf_irr:>9.2f}%{prf_irr - dca_irr:>11.2f}p")

print("\n[2] 규칙기반 타이밍 vs DCA (같은 자본 관점) — 음수면 DCA에 짐")
print(f"{'ETF':<5}{'추세추종(SMA200)':>16}{'비고':>8}")
for r in rows_out:
    print(f"{r['sym']:<5}{pct(r['sma_value'], r['dca_value']):>15.1f}%   현금미투자 ${r['sma_leftover_cash']:,.0f}")

print("\n[3] 밸류애버리징: 투입원금이 달라 '같은 돈' 비교가 아님 → 투입 대비 배수로 판단")
if rows_out:
    print(f"(참고: DCA는 매 ETF당 ${rows_out[0]['invested_dca']:,.0f} 투입)")
print(f"{'ETF':<5}{'VA투입($)':>11}{'최종가치':>11}{'배수':>7}")
for r in rows_out:
    mult = r['va_value'] / r['va_invested'] if r['va_invested'] else 0
    dca_mult = r['dca_value'] / r['invested_dca']
    print(f"{r['sym']:<5}{r['va_invested']:>10,.0f}{r['va_value']:>11,.0f}{mult:>7.2f}x   vs DCA {dca_mult:.2f}x")
