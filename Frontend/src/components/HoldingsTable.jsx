/**
 * 보유 종목 테이블 컴포넌트.
 * HoldingDto 목록을 받아 종목별 수량, 단가, 평가금액, 수익률을 시각화합니다.
 * 비중은 이 표에서 빼고 대시보드 하단의 `AllocationDonut`이 전체 100% 기준으로 보여줍니다.
 *
 * ⚠️ 원화 표기는 전부 <b>현재 환율로 환산한 값</b>입니다. 실제 매입 시점의 원화 단가가 아닙니다 —
 * `TB_TRADE_HISTORY`에 체결 시점 환율 컬럼이 없어 진짜 원화 매입원가는 계산할 수 없습니다.
 * 정확한 원화 원가가 필요해지면 거래이력에 체결 환율을 함께 적재하는 것이 먼저입니다.
 */
const HoldingsTable = ({ holdings, format, formatAlt }) => {
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
            <th>매입가</th>
            <th>현재가</th>
            <th>평가금액</th>
            <th>수익률</th>
          </tr>
        </thead>
        <tbody>
          {holdings.map((h, idx) => {
            const evalUsd = h.currentPrice * h.qty;
            const profitPct = (h.profitRate * 100).toFixed(2);
            const isProfit = h.profitRate >= 0;

            return (
              <tr key={h.ticker}>
                <td>
                  <span className="ticker-badge">
                    <span className={`ticker-dot ticker-dot--${idx % 5}`} />
                    {h.ticker}
                  </span>
                </td>
                <td className="text-strong">{h.qty.toLocaleString()}주</td>
                {/* 상단 자산 요약과 같은 통화 토글을 따른다: 선택 통화가 주 표기, 반대 통화가 보조 */}
                <td>
                  {format(h.avgPrice)}
                  <div className="cell-sub">{formatAlt(h.avgPrice)}</div>
                </td>
                <td className="text-strong">
                  {format(h.currentPrice)}
                  <div className="cell-sub">{formatAlt(h.currentPrice)}</div>
                </td>
                <td className="text-strong">
                  {format(evalUsd)}
                  <div className="cell-sub">{formatAlt(evalUsd)}</div>
                </td>
                <td>
                  <span className={`badge-profit ${isProfit ? 'badge-profit--up' : 'badge-profit--down'}`}>
                    {isProfit ? '▲' : '▼'} {isProfit ? '+' : ''}{profitPct}%
                  </span>
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
