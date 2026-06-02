import { useState } from 'react';

const STRATEGY_TYPES = ['MEAN_REVERSION', 'MOMENTUM', 'MIXED'];

/**
 * 백테스팅 페이지.
 * BacktestController와 연동하여 과거 데이터 기반 전략 수익성을 검증합니다.
 */
const Backtest = () => {
  // ── 설정 폼 ──
  const [ticker, setTicker] = useState('QQQM');
  const [strategyType, setStrategyType] = useState('MEAN_REVERSION');
  const [days, setDays] = useState(120);
  const [initialCapital, setInitialCapital] = useState(10000);
  const [buyThreshold, setBuyThreshold] = useState(0.10);
  const [sellThreshold, setSellThreshold] = useState(0.90);

  // ── 결과 상태 ──
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const handleRun = async () => {
    try {
      setRunning(true);
      setError(null);
      setResult(null);

      const body = {
        ticker: ticker.toUpperCase(),
        strategyType,
        days: Number(days),
        initialCapital: Number(initialCapital),
        buyThreshold: Number(buyThreshold),
        sellThreshold: Number(sellThreshold)
      };

      const res = await fetch('/api/backtest/run', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });

      if (!res.ok) throw new Error(`백테스트 실패 (${res.status})`);
      const data = await res.json();
      setResult(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setRunning(false);
    }
  };

  const isProfit = result && result.totalReturnPct >= 0;

  return (
    <div>
      {/* ── 설정 폼 카드 ── */}
      <div className="card fade-in">
        <h2>백테스트 설정</h2>

        <div className="backtest-form">
          <div className="form-group">
            <label>종목 코드</label>
            <input
              type="text"
              value={ticker}
              onChange={e => setTicker(e.target.value.toUpperCase())}
              placeholder="예: QQQM"
            />
          </div>
          <div className="form-group">
            <label>전략 유형</label>
            <select value={strategyType} onChange={e => setStrategyType(e.target.value)}>
              {STRATEGY_TYPES.map(t => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>검증 기간 (일)</label>
            <input type="number" min="30" max="365" value={days} onChange={e => setDays(e.target.value)} />
          </div>
          <div className="form-group">
            <label>초기 투자금 (USD)</label>
            <input type="number" min="100" value={initialCapital} onChange={e => setInitialCapital(e.target.value)} />
          </div>
          <div className="form-group">
            <label>매수 임계값</label>
            <input type="number" step="0.01" min="0" max="1" value={buyThreshold} onChange={e => setBuyThreshold(e.target.value)} />
          </div>
          <div className="form-group">
            <label>매도 임계값</label>
            <input type="number" step="0.01" min="0" max="1" value={sellThreshold} onChange={e => setSellThreshold(e.target.value)} />
          </div>
        </div>

        <div style={{ marginTop: 16 }}>
          <button className="btn btn--primary" onClick={handleRun} disabled={running} style={{ width: '100%', padding: 14, fontSize: '1rem' }}>
            {running ? '⏳ 백테스트 실행 중...' : '🧪 백테스트 실행'}
          </button>
        </div>
      </div>

      {error && (
        <div className="card fade-in" style={{ marginTop: 16 }}>
          <div style={{ padding: '10px 14px', background: 'var(--loss-red-bg)', color: 'var(--loss-red)', borderRadius: 'var(--radius-sm)', fontSize: '0.85rem' }}>
            ❌ {error}
          </div>
        </div>
      )}

      {/* ── 결과 ── */}
      {result && (
        <>
          {/* 요약 카드 4개 */}
          <div className="summary-grid" style={{ marginTop: 20 }}>
            <div className="summary-card fade-in fade-in-delay-1">
              <div className="summary-card__header">
                <span className="summary-card__label">총 수익률</span>
                <div className={`summary-card__icon ${isProfit ? 'summary-card__icon--green' : 'summary-card__icon--blue'}`}>
                  {isProfit ? '📈' : '📉'}
                </div>
              </div>
              <div className={`summary-card__value ${isProfit ? 'text-profit' : 'text-loss'}`}>
                {isProfit ? '+' : ''}{result.totalReturnPct.toFixed(2)}%
              </div>
              <div className="summary-card__sub">
                {result.ticker} · {result.strategy}
              </div>
            </div>

            <div className="summary-card fade-in fade-in-delay-2">
              <div className="summary-card__header">
                <span className="summary-card__label">최종 자산</span>
                <div className="summary-card__icon summary-card__icon--blue">💰</div>
              </div>
              <div className="summary-card__value">
                ${result.finalCapital.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </div>
              <div className="summary-card__sub">
                초기 ${result.initialCapital.toLocaleString()}
              </div>
            </div>

            <div className="summary-card fade-in fade-in-delay-3">
              <div className="summary-card__header">
                <span className="summary-card__label">최대 낙폭 (MDD)</span>
                <div className="summary-card__icon summary-card__icon--purple">📊</div>
              </div>
              <div className="summary-card__value text-loss">
                {result.maxDrawdownPct.toFixed(2)}%
              </div>
              <div className="summary-card__sub">최대 드로다운</div>
            </div>

            <div className="summary-card fade-in fade-in-delay-4">
              <div className="summary-card__header">
                <span className="summary-card__label">승률</span>
                <div className="summary-card__icon summary-card__icon--cyan">🎯</div>
              </div>
              <div className="summary-card__value">
                {result.winRatePct.toFixed(1)}%
              </div>
              <div className="summary-card__sub">총 {result.totalTrades}건 거래</div>
            </div>
          </div>

          {/* 매매 내역 테이블 */}
          <div className="card fade-in" style={{ marginTop: 16, animationDelay: '0.25s', opacity: 0 }}>
            <h2>백테스트 매매 내역</h2>

            {result.trades && result.trades.length > 0 ? (
              <div className="data-table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>날짜</th>
                      <th>유형</th>
                      <th>가격 (USD)</th>
                      <th>수량</th>
                      <th>손익</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.trades.map((t, i) => {
                      const isBuy = t.type === 'BUY';
                      const hasPl = t.profitLoss !== null && t.profitLoss !== undefined && t.profitLoss !== 0;
                      return (
                        <tr key={i}>
                          <td style={{ fontSize: '0.8rem' }}>
                            {new Date(t.date).toLocaleDateString('ko-KR')}
                          </td>
                          <td>
                            <span className={`badge-profit ${isBuy ? 'badge-profit--up' : 'badge-profit--down'}`}>
                              {isBuy ? '매수' : '매도'}
                            </span>
                          </td>
                          <td className="text-strong">${t.price.toFixed(2)}</td>
                          <td>{t.qty}주</td>
                          <td>
                            {hasPl ? (
                              <span className={t.profitLoss >= 0 ? 'text-profit' : 'text-loss'}>
                                {t.profitLoss >= 0 ? '+' : ''}${t.profitLoss.toFixed(2)}
                              </span>
                            ) : (
                              <span style={{ color: 'var(--text-muted)' }}>—</span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="empty-state">
                <div className="empty-state__icon">📭</div>
                <p className="empty-state__text">해당 기간 중 매매가 발생하지 않았습니다.</p>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default Backtest;
