import { useState, useEffect, useCallback } from 'react';
import HoldingsTable from '../components/HoldingsTable';

/**
 * 포트폴리오 대시보드 메인 화면.
 * /api/portfolio/summary에서 보유 종목, 예수금, 환율 정보를 가져와서
 * 총 자산, 주식 평가금액, 예수금, 환율을 요약 카드로 표시합니다.
 */
const Dashboard = () => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [lastUpdated, setLastUpdated] = useState(null);

  const fetchSummary = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch('/api/portfolio/summary');

      if (!res.ok) {
        throw new Error(`서버 오류 (${res.status})`);
      }

      const json = await res.json();
      setData(json);
      setLastUpdated(new Date());
    } catch (err) {
      console.error('포트폴리오 요약 조회 실패:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchSummary();
  }, [fetchSummary]);

  // ── 로딩 상태 ──
  if (loading && !data) {
    return (
      <div className="loading-container fade-in">
        <div className="loading-spinner" />
        <span className="loading-text">포트폴리오 데이터를 불러오는 중...</span>
      </div>
    );
  }

  // ── 에러 상태 ──
  if (error && !data) {
    return (
      <div className="error-container fade-in">
        <div className="error-icon">⚠️</div>
        <p className="error-text">{error}</p>
        <button className="btn btn--primary" onClick={fetchSummary}>
          다시 시도
        </button>
      </div>
    );
  }

  if (!data) return null;

  const { holdings, cashBalance, exchangeRate } = data;

  // ── 집계 계산 ──
  const stockEvalUsd = holdings.reduce(
    (sum, h) => sum + h.currentPrice * h.qty,
    0
  );
  const totalCostUsd = holdings.reduce(
    (sum, h) => sum + h.avgPrice * h.qty,
    0
  );
  const totalAssetsUsd = stockEvalUsd + cashBalance;
  const totalAssetsKrw = totalAssetsUsd * exchangeRate;
  const totalProfitUsd = stockEvalUsd - totalCostUsd;
  const totalProfitRate =
    totalCostUsd > 0 ? ((totalProfitUsd / totalCostUsd) * 100).toFixed(2) : '0.00';
  const isOverallProfit = totalProfitUsd >= 0;

  return (
    <div>
      {/* ── 요약 카드 그리드 ── */}
      <div className="summary-grid">
        {/* 총 자산 */}
        <div className="summary-card fade-in fade-in-delay-1">
          <div className="summary-card__header">
            <span className="summary-card__label">총 자산</span>
            <div className="summary-card__icon summary-card__icon--blue">💰</div>
          </div>
          <div className="summary-card__value">
            ${totalAssetsUsd.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div className="summary-card__sub">
            ₩{totalAssetsKrw.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 })}
          </div>
        </div>

        {/* 주식 평가금액 */}
        <div className="summary-card fade-in fade-in-delay-2">
          <div className="summary-card__header">
            <span className="summary-card__label">주식 평가금액</span>
            <div className="summary-card__icon summary-card__icon--purple">📈</div>
          </div>
          <div className="summary-card__value">
            ${stockEvalUsd.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div className="summary-card__sub">
            <span className={`badge-profit ${isOverallProfit ? 'badge-profit--up' : 'badge-profit--down'}`}>
              {isOverallProfit ? '▲' : '▼'} {isOverallProfit ? '+' : ''}{totalProfitRate}%
            </span>
            <span style={{ marginLeft: 8 }}>
              {isOverallProfit ? '+' : ''}${totalProfitUsd.toFixed(2)}
            </span>
          </div>
        </div>

        {/* 예수금 */}
        <div className="summary-card fade-in fade-in-delay-3">
          <div className="summary-card__header">
            <span className="summary-card__label">예수금 (현금)</span>
            <div className="summary-card__icon summary-card__icon--green">💵</div>
          </div>
          <div className="summary-card__value">
            ${cashBalance.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div className="summary-card__sub">
            ₩{(cashBalance * exchangeRate).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 })}
          </div>
        </div>

        {/* 환율 */}
        <div className="summary-card fade-in fade-in-delay-4">
          <div className="summary-card__header">
            <span className="summary-card__label">USD/KRW 환율</span>
            <div className="summary-card__icon summary-card__icon--cyan">💱</div>
          </div>
          <div className="summary-card__value">
            ₩{exchangeRate.toLocaleString(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 })}
          </div>
          <div className="summary-card__sub">1 USD 기준</div>
        </div>
      </div>

      {/* ── 보유 종목 테이블 ── */}
      <div className="card fade-in" style={{ animationDelay: '0.25s', opacity: 0 }}>
        <div className="section-header">
          <h2>보유 종목</h2>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            {lastUpdated && (
              <span className="section-header__sub">
                마지막 업데이트: {lastUpdated.toLocaleTimeString('ko-KR')}
              </span>
            )}
            <button className="btn btn--outline" onClick={fetchSummary} disabled={loading}>
              {loading ? '갱신 중...' : '🔄 새로고침'}
            </button>
          </div>
        </div>
        <HoldingsTable holdings={holdings} exchangeRate={exchangeRate} />
      </div>
    </div>
  );
};

export default Dashboard;
