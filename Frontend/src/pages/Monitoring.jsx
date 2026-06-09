import { useState, useEffect, useCallback } from 'react';

/**
 * AI 모니터링 페이지 (Phase 5-b).
 * MonitoringController와 연동하여 AI 판단 성과와 토큰 사용량/비용을 표시합니다.
 *  - 요약 카드: 평가 건수, 평균 승률, 오늘 토큰, 기간 누적 추정 비용
 *  - 탭 1: AI 성과 기록 (종목/신호/시점가/평가가/승률)
 *  - 탭 2: 토큰 비용 (에이전트별 집계 / 일자별 추이)
 */
const Monitoring = () => {
  const [activeTab, setActiveTab] = useState('performance');
  const [days, setDays] = useState(30);

  const [summary, setSummary] = useState(null);
  const [performance, setPerformance] = useState([]);
  const [byAgent, setByAgent] = useState([]);
  const [daily, setDaily] = useState([]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchAll = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [sumRes, perfRes, agentRes, dailyRes] = await Promise.all([
        fetch(`/api/monitoring/summary?days=${days}`),
        fetch('/api/monitoring/performance?limit=50'),
        fetch(`/api/monitoring/tokens/by-agent?days=${days}`),
        fetch('/api/monitoring/tokens/daily?days=14')
      ]);

      for (const r of [sumRes, perfRes, agentRes, dailyRes]) {
        if (!r.ok) throw new Error(`서버 오류 (${r.status})`);
      }

      setSummary(await sumRes.json());
      setPerformance(await perfRes.json());
      setByAgent((await agentRes.json()).agents ?? []);
      setDaily((await dailyRes.json()).daily ?? []);
    } catch (err) {
      console.error('모니터링 데이터 조회 실패:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [days]);

  useEffect(() => {
    fetchAll();
  }, [fetchAll]);

  // ── 포맷 헬퍼 ──
  const fmtInt = (n) => Number(n ?? 0).toLocaleString();
  const fmtUsd = (n) => `$${Number(n ?? 0).toLocaleString(undefined, { minimumFractionDigits: 4, maximumFractionDigits: 4 })}`;
  const fmtPct = (n) => `${(Number(n ?? 0) * 100).toFixed(1)}%`;

  const getSignalBadge = (signal) => {
    if (signal === 'BUY') return <span className="badge-profit badge-profit--up">매수</span>;
    if (signal === 'SELL') return <span className="badge-profit badge-profit--down">매도</span>;
    return <span className="badge cancelled">{signal}</span>;
  };

  const getWinRateBadge = (winRate) => {
    if (winRate === null || winRate === undefined) {
      return <span className="badge active">평가 대기</span>;
    }
    const up = Number(winRate) >= 0;
    return (
      <span className={`badge-profit ${up ? 'badge-profit--up' : 'badge-profit--down'}`}>
        {up ? '▲' : '▼'} {up ? '+' : ''}{(Number(winRate) * 100).toFixed(1)}%
      </span>
    );
  };

  return (
    <div>
      {/* ── 요약 카드 ── */}
      <div className="summary-grid">
        <div className="summary-card fade-in fade-in-delay-1">
          <div className="summary-card__header">
            <span className="summary-card__label">평가 완료 건수</span>
            <div className="summary-card__icon summary-card__icon--blue">🧠</div>
          </div>
          <div className="summary-card__value">{fmtInt(summary?.evaluatedCount)}건</div>
          <div className="summary-card__sub">AI 판단 후 결과가 평가된 건수</div>
        </div>

        <div className="summary-card fade-in fade-in-delay-2">
          <div className="summary-card__header">
            <span className="summary-card__label">평균 승률</span>
            <div className="summary-card__icon summary-card__icon--purple">🎯</div>
          </div>
          <div className="summary-card__value">{fmtPct(summary?.averageWinRate)}</div>
          <div className="summary-card__sub">평가 완료 건의 평균 수익률</div>
        </div>

        <div className="summary-card fade-in fade-in-delay-3">
          <div className="summary-card__header">
            <span className="summary-card__label">오늘 토큰 사용량</span>
            <div className="summary-card__icon summary-card__icon--blue">🔢</div>
          </div>
          <div className="summary-card__value">{fmtInt(summary?.todayTotalTokens)}</div>
          <div className="summary-card__sub">금일 누적 토큰 (전 에이전트)</div>
        </div>

        <div className="summary-card fade-in fade-in-delay-4">
          <div className="summary-card__header">
            <span className="summary-card__label">추정 비용 ({summary?.periodDays ?? days}일)</span>
            <div className="summary-card__icon summary-card__icon--purple">💵</div>
          </div>
          <div className="summary-card__value">{fmtUsd(summary?.estPeriodCostUsd)}</div>
          <div className="summary-card__sub">
            누적 {fmtInt(summary?.periodTotalTokens)} 토큰 · Gemini 1.5 Flash 단가 기준
          </div>
        </div>
      </div>

      {/* ── 탭 + 기간 선택 ── */}
      <div className="tabs fade-in" style={{ marginTop: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            className={`tab-btn ${activeTab === 'performance' ? 'tab-btn--active' : ''}`}
            onClick={() => setActiveTab('performance')}
          >
            🧠 AI 성과
          </button>
          <button
            className={`tab-btn ${activeTab === 'tokens' ? 'tab-btn--active' : ''}`}
            onClick={() => setActiveTab('tokens')}
          >
            💵 토큰 비용
          </button>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <select
            value={days}
            onChange={e => setDays(Number(e.target.value))}
            style={{
              background: 'var(--bg-input)',
              border: '1px solid var(--border-primary)',
              borderRadius: 'var(--radius-sm)',
              color: 'var(--text-primary)',
              padding: '6px 10px',
              fontSize: '0.8rem'
            }}
          >
            <option value={7}>최근 7일</option>
            <option value={30}>최근 30일</option>
            <option value={90}>최근 90일</option>
          </select>
          <button className="btn btn--outline" onClick={fetchAll} disabled={loading}>
            {loading ? '조회 중...' : '🔄'}
          </button>
        </div>
      </div>

      {error && (
        <div className="card fade-in" style={{ marginTop: 16 }}>
          <div className="error-container" style={{ padding: 24 }}>
            <p className="error-text">{error}</p>
          </div>
        </div>
      )}

      {/* ── 탭 1: AI 성과 ── */}
      {activeTab === 'performance' && (
        <div className="card fade-in" style={{ marginTop: 16 }}>
          <div className="section-header">
            <h2>AI 판단 성과 기록</h2>
          </div>
          {loading && performance.length === 0 ? (
            <div className="loading-container" style={{ padding: 40 }}>
              <div className="loading-spinner" />
            </div>
          ) : performance.length === 0 ? (
            <div className="empty-state">
              <div className="empty-state__icon">📭</div>
              <p className="empty-state__text">AI 성과 기록이 아직 없습니다.</p>
            </div>
          ) : (
            <div className="data-table-wrapper">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>판단일시</th>
                    <th>종목</th>
                    <th>신호</th>
                    <th>판단 시점가 (USD)</th>
                    <th>평가 시점가 (USD)</th>
                    <th>승률/수익률</th>
                  </tr>
                </thead>
                <tbody>
                  {performance.map(p => (
                    <tr key={p.perfId}>
                      <td style={{ fontSize: '0.8rem' }}>
                        {new Date(p.createdAt).toLocaleString('ko-KR')}
                      </td>
                      <td className="text-strong">{p.ticker}</td>
                      <td>{getSignalBadge(p.signal)}</td>
                      <td>${Number(p.priceAtSignal).toFixed(2)}</td>
                      <td>{p.priceLater != null ? `$${Number(p.priceLater).toFixed(2)}` : '—'}</td>
                      <td>{getWinRateBadge(p.winRate)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* ── 탭 2: 토큰 비용 ── */}
      {activeTab === 'tokens' && (
        <>
          <div className="card fade-in" style={{ marginTop: 16 }}>
            <div className="section-header">
              <h2>에이전트별 토큰 사용량 (최근 {days}일)</h2>
            </div>
            {loading && byAgent.length === 0 ? (
              <div className="loading-container" style={{ padding: 40 }}>
                <div className="loading-spinner" />
              </div>
            ) : byAgent.length === 0 ? (
              <div className="empty-state">
                <div className="empty-state__icon">📭</div>
                <p className="empty-state__text">토큰 사용 기록이 아직 없습니다.</p>
              </div>
            ) : (
              <div className="data-table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>에이전트</th>
                      <th>호출 수</th>
                      <th>프롬프트 토큰</th>
                      <th>완성 토큰</th>
                      <th>총 토큰</th>
                      <th>추정 비용</th>
                    </tr>
                  </thead>
                  <tbody>
                    {byAgent.map(a => (
                      <tr key={a.agentType}>
                        <td className="text-strong">{a.agentType}</td>
                        <td>{fmtInt(a.callCount)}</td>
                        <td>{fmtInt(a.promptTokens)}</td>
                        <td>{fmtInt(a.completionTokens)}</td>
                        <td className="text-strong">{fmtInt(a.totalTokens)}</td>
                        <td>{fmtUsd(a.estCostUsd)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card fade-in" style={{ marginTop: 16 }}>
            <div className="section-header">
              <h2>일자별 토큰 추이 (최근 14일)</h2>
            </div>
            {daily.length === 0 ? (
              <div className="empty-state">
                <div className="empty-state__icon">📊</div>
                <p className="empty-state__text">일자별 사용 기록이 아직 없습니다.</p>
              </div>
            ) : (
              <div className="data-table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>날짜</th>
                      <th>호출 수</th>
                      <th>프롬프트 토큰</th>
                      <th>완성 토큰</th>
                      <th>총 토큰</th>
                      <th>추정 비용</th>
                    </tr>
                  </thead>
                  <tbody>
                    {daily.map(d => (
                      <tr key={d.date}>
                        <td style={{ fontSize: '0.8rem' }}>{d.date}</td>
                        <td>{fmtInt(d.callCount)}</td>
                        <td>{fmtInt(d.promptTokens)}</td>
                        <td>{fmtInt(d.completionTokens)}</td>
                        <td className="text-strong">{fmtInt(d.totalTokens)}</td>
                        <td>{fmtUsd(d.estCostUsd)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default Monitoring;
