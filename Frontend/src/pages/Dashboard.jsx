import { useState, useEffect, useCallback } from 'react';
import HoldingsTable from '../components/HoldingsTable';
import AllocationDonut from '../components/AllocationDonut';

/**
 * 포트폴리오 대시보드 메인 화면.
 * 상단 요약(총자산·주식평가금액·예수금·환율 + 계좌 모드 배지)과
 * 하단 보유 종목 테이블을 /api/portfolio/summary 단일 응답으로 함께 렌더링합니다.
 *  - /api/portfolio/summary 가 예수금·환율·계좌 모드 + 보유종목을 한 번에 반환하므로,
 *    별도의 /api/portfolio/holdings 조회 없이 요약 응답의 보유종목을 테이블에 재사용합니다
 *    (KIS 잔고조회 왕복 1회 + Rate limit 대기 400ms 절감).
 */
const Dashboard = () => {
  // ── 요약 + 보유 종목(summary 단일 응답) ──
  const [summary, setSummary] = useState(null);
  const [summaryLoading, setSummaryLoading] = useState(true);
  const [summaryError, setSummaryError] = useState(null);
  const [summaryUpdated, setSummaryUpdated] = useState(null);
  const [currency, setCurrency] = useState('USD'); // 금액 카드 표시 통화 — 'USD' | 'KRW'

  const fetchSummary = useCallback(async () => {
    try {
      setSummaryLoading(true);
      setSummaryError(null);
      const res = await fetch('/api/portfolio/summary');
      if (!res.ok) throw new Error(`서버 오류 (${res.status})`);
      const json = await res.json();
      setSummary(json);
      setSummaryUpdated(new Date());
    } catch (err) {
      console.error('포트폴리오 요약 조회 실패:', err);
      setSummaryError(err.message);
    } finally {
      setSummaryLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchSummary();
  }, [fetchSummary]);

  // ── 상단 요약 첫 로딩 (페이지 단위) ──
  if (summaryLoading && !summary) {
    return (
      <div className="loading-container fade-in">
        <div className="loading-spinner" />
        <span className="loading-text">포트폴리오 데이터를 불러오는 중...</span>
      </div>
    );
  }

  if (summaryError && !summary) {
    return (
      <div className="error-container fade-in">
        <div className="error-icon">⚠️</div>
        <p className="error-text">{summaryError}</p>
        <button className="btn btn--primary" onClick={fetchSummary}>
          다시 시도
        </button>
      </div>
    );
  }

  if (!summary) return null;

  const { cashBalance, exchangeRate, accountMode, accountMasked } = summary;
  // 카드 집계는 요약 응답의 보유종목 스냅샷을 사용(테이블과 독립 새로고침).
  const summaryHoldings = Array.isArray(summary.holdings) ? summary.holdings : [];

  // ── 통화 토글 포맷터 ──
  // 내부 계산은 USD 기준. 표시만 현재 환율로 USD↔KRW 전환한다.
  const fmtUsd = (usd) => '$' + usd.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const fmtKrw = (usd) => '₩' + (usd * exchangeRate).toLocaleString(undefined, { maximumFractionDigits: 0 });
  const fmtMoney = (usd) => (currency === 'KRW' ? fmtKrw(usd) : fmtUsd(usd)); // 선택 통화(주 표시)
  const fmtMoneyAlt = (usd) => (currency === 'KRW' ? fmtUsd(usd) : fmtKrw(usd)); // 반대 통화(서브 표시)

  // ── 계좌 모드 배지 표기 (실거래 전환 가시화) ──
  const accountBadge = {
    LIVE: { label: '실거래 계좌', color: 'var(--loss-red)', bg: 'var(--loss-red-bg)', icon: '🔴' },
    PAPER: { label: '모의투자 (KIS)', color: 'var(--warn-amber)', bg: 'rgba(245, 158, 11, 0.1)', icon: '🟡' },
    SIM: { label: '시뮬레이션', color: 'var(--text-muted)', bg: 'rgba(148, 163, 184, 0.12)', icon: '⚪' },
  }[accountMode] ?? { label: accountMode ?? '알 수 없음', color: 'var(--text-muted)', bg: 'rgba(148, 163, 184, 0.12)', icon: '⚪' };

  // ── 상단 카드 집계 계산 ──
  const stockEvalUsd = summaryHoldings.reduce((sum, h) => sum + h.currentPrice * h.qty, 0);
  const totalCostUsd = summaryHoldings.reduce((sum, h) => sum + h.avgPrice * h.qty, 0);
  // 총 자산 = 주식 평가액 + 예수금.
  // 예전에는 예수금을 뺀 주식 평가액만 담아 '총 자산'과 '주식 평가금액' 두 카드가 같은 값을
  // 보여줬다. 예수금 조회가 늘 0을 반환하던 동안은 우연히 맞아떨어져 드러나지 않았다.
  const totalAssetsUsd = stockEvalUsd + cashBalance;
  const totalProfitUsd = stockEvalUsd - totalCostUsd;
  const totalProfitRate =
    totalCostUsd > 0 ? ((totalProfitUsd / totalCostUsd) * 100).toFixed(2) : '0.00';
  const isOverallProfit = totalProfitUsd >= 0;

  return (
    <div>
      {/* ── 계좌 모드 배지 (실거래/모의/시뮬 구분) ── */}
      <div
        className="fade-in"
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 10,
          padding: '10px 16px',
          marginBottom: 16,
          background: accountBadge.bg,
          border: `1px solid ${accountBadge.color}`,
          borderRadius: 'var(--radius-sm)',
        }}
      >
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8, fontWeight: 600, color: accountBadge.color }}>
          {accountBadge.icon} {accountBadge.label}
          {accountMasked && (
            <span style={{ marginLeft: 6, fontWeight: 400, color: 'var(--text-secondary)', fontSize: '0.85rem' }}>
              계좌 {accountMasked}
            </span>
          )}
        </span>
        <span style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          {accountMode === 'LIVE' && (
            <span style={{ fontSize: '0.8rem', color: 'var(--loss-red)' }}>⚠️ 실제 자금이 거래됩니다</span>
          )}
          {summaryUpdated && (
            <span className="section-header__sub">
              {summaryUpdated.toLocaleTimeString('ko-KR')} 기준
            </span>
          )}
        </span>
      </div>

      {/* ── 요약 카드 그리드 ── */}
      <div className="section-header" style={{ marginBottom: 12 }}>
        <h2 style={{ fontSize: '1.05rem' }}>자산 요약</h2>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <div className="ccy-toggle" role="group" aria-label="표시 통화">
            <button className={currency === 'USD' ? 'active' : ''} onClick={() => setCurrency('USD')}>$ USD</button>
            <button className={currency === 'KRW' ? 'active' : ''} onClick={() => setCurrency('KRW')}>₩ KRW</button>
          </div>
          <button className="btn btn--outline" onClick={fetchSummary} disabled={summaryLoading} style={{ padding: '4px 12px', fontSize: '0.85rem' }}>
            {summaryLoading ? '갱신 중...' : '🔄 새로고침'}
          </button>
        </div>
      </div>

      {summaryError && (
        <div className="alert alert--err" style={{ marginBottom: 12 }}>
          ❌ 요약 갱신 실패: {summaryError}
        </div>
      )}

      <div className="summary-grid">
        {/* 총 자산 */}
        <div className="summary-card fade-in fade-in-delay-1">
          <div className="summary-card__header">
            <span className="summary-card__label">총 자산</span>
            <div className="summary-card__icon summary-card__icon--blue">💰</div>
          </div>
          <div className="summary-card__value">
            {fmtMoney(totalAssetsUsd)}
          </div>
          <div className="summary-card__sub">
            주식 {fmtMoney(stockEvalUsd)} + 예수금 {fmtMoney(cashBalance)}
          </div>
        </div>

        {/* 주식 평가금액 */}
        <div className="summary-card fade-in fade-in-delay-2">
          <div className="summary-card__header">
            <span className="summary-card__label">주식 평가금액</span>
            <div className="summary-card__icon summary-card__icon--purple">📈</div>
          </div>
          <div className="summary-card__value">
            {fmtMoney(stockEvalUsd)}
          </div>
          <div className="summary-card__sub">
            <span className={`badge-profit ${isOverallProfit ? 'badge-profit--up' : 'badge-profit--down'}`}>
              {isOverallProfit ? '▲' : '▼'} {isOverallProfit ? '+' : ''}{totalProfitRate}%
            </span>
            <span style={{ marginLeft: 8 }}>
              {isOverallProfit ? '+' : ''}{fmtMoney(totalProfitUsd)}
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
            {fmtMoney(cashBalance)}
          </div>
          <div className="summary-card__sub">
            {fmtMoneyAlt(cashBalance)}
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

      {/* ── 보유 종목 테이블 (요약 응답의 보유종목 재사용 — 별도 조회 없이 왕복 1회 절감) ── */}
      <div className="card fade-in" style={{ animationDelay: '0.25s', opacity: 0 }}>
        <div className="section-header">
          <h2>보유 종목</h2>
          {summaryUpdated && (
            <span className="section-header__sub">
              원화는 현재 환율(₩{exchangeRate.toLocaleString(undefined, { maximumFractionDigits: 1 })}) 환산 · 매입 시점 환율 아님
            </span>
          )}
        </div>

        <HoldingsTable holdings={summaryHoldings} format={fmtMoney} />
      </div>

      {/* ── 비중 (표에서 빼서 전체 100% 기준으로 한눈에) ── */}
      {summaryHoldings.length > 0 && (
        <div className="card fade-in" style={{ animationDelay: '0.3s', opacity: 0, marginTop: 20 }}>
          <div className="section-header">
            <h2>자산 비중</h2>
            <span className="section-header__sub">
              보유 종목 평가금액 기준 · 예수금 제외
            </span>
          </div>

          <AllocationDonut holdings={summaryHoldings} format={fmtMoney} />
        </div>
      )}
    </div>
  );
};

export default Dashboard;
