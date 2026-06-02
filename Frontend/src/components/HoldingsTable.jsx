import { useMemo } from 'react';

/**
 * 보유 종목 테이블 컴포넌트.
 * HoldingDto 목록을 받아 종목별 수량, 평가금액, 수익률을 시각화합니다.
 */
const HoldingsTable = ({ holdings, exchangeRate }) => {
  // ── 전체 평가금액 합계 (비중 계산용) ──
  const totalEvaluation = useMemo(() => {
    return holdings.reduce((sum, h) => sum + h.currentPrice * h.qty, 0);
  }, [holdings]);

  if (holdings.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-state__icon">📭</div>
        <p className="empty-state__text">현재 보유 중인 종목이 없습니다.</p>
      </div>
    );
  }

  return (
    <div className="data-table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>종목</th>
            <th>수량</th>
            <th>매입가 (USD)</th>
            <th>현재가 (USD)</th>
            <th>평가금액 (USD)</th>
            <th>평가금액 (KRW)</th>
            <th>수익률</th>
            <th>비중</th>
          </tr>
        </thead>
        <tbody>
          {holdings.map((h, idx) => {
            const evalUsd = h.currentPrice * h.qty;
            const evalKrw = evalUsd * exchangeRate;
            const profitPct = (h.profitRate * 100).toFixed(2);
            const isProfit = h.profitRate >= 0;
            const weight = totalEvaluation > 0
              ? ((evalUsd / totalEvaluation) * 100).toFixed(1)
              : '0.0';

            return (
              <tr key={h.ticker}>
                <td>
                  <span className="ticker-badge">
                    <span className={`ticker-dot ticker-dot--${idx % 5}`} />
                    {h.ticker}
                  </span>
                </td>
                <td className="text-strong">{h.qty.toLocaleString()}주</td>
                <td>${h.avgPrice.toFixed(2)}</td>
                <td className="text-strong">${h.currentPrice.toFixed(2)}</td>
                <td className="text-strong">${evalUsd.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                <td>₩{evalKrw.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 })}</td>
                <td>
                  <span className={`badge-profit ${isProfit ? 'badge-profit--up' : 'badge-profit--down'}`}>
                    {isProfit ? '▲' : '▼'} {isProfit ? '+' : ''}{profitPct}%
                  </span>
                </td>
                <td>
                  <div className="weight-bar-container">
                    <div className="weight-bar">
                      <div
                        className="weight-bar__fill"
                        style={{ width: `${weight}%` }}
                      />
                    </div>
                    <span className="weight-bar__text">{weight}%</span>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};

export default HoldingsTable;
