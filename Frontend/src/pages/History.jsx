import { useState, useEffect, useCallback } from 'react';

/**
 * 거래 내역 & 시스템 로그 페이지.
 * HistoryController와 연동하여 매매 내역과 날짜별 시스템 로그를 표시합니다.
 */
const History = () => {
  const [activeTab, setActiveTab] = useState('trades');

  // ── 매매 내역 상태 ──
  const [trades, setTrades] = useState([]);
  const [tradeLimit, setTradeLimit] = useState(50);
  const [tradesLoading, setTradesLoading] = useState(false);

  // ── 시스템 로그 상태 ──
  const [logDate, setLogDate] = useState(new Date().toISOString().split('T')[0]);
  const [logData, setLogData] = useState(null);
  const [logsLoading, setLogsLoading] = useState(false);

  const [error, setError] = useState(null);

  // ── 매매 내역 불러오기 ──
  const fetchTrades = useCallback(async () => {
    try {
      setTradesLoading(true);
      setError(null);
      const res = await fetch(`/api/history/trades?limit=${tradeLimit}`);
      if (!res.ok) throw new Error(`서버 오류 (${res.status})`);
      const data = await res.json();
      setTrades(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setTradesLoading(false);
    }
  }, [tradeLimit]);

  // ── 시스템 로그 불러오기 ──
  const fetchLogs = useCallback(async () => {
    try {
      setLogsLoading(true);
      setError(null);
      const res = await fetch(`/api/history/logs?date=${logDate}&lines=200`);
      if (!res.ok) throw new Error(`서버 오류 (${res.status})`);
      const data = await res.json();
      setLogData(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLogsLoading(false);
    }
  }, [logDate]);

  useEffect(() => {
    if (activeTab === 'trades') fetchTrades();
  }, [activeTab, fetchTrades]);

  useEffect(() => {
    if (activeTab === 'logs') fetchLogs();
  }, [activeTab, fetchLogs]);

  const getStatusBadge = (status) => {
    const map = {
      FILLED: { className: 'badge completed', label: '체결' },
      PENDING: { className: 'badge active', label: '대기' },
      FAILED: { className: 'badge cancelled', label: '실패' }
    };
    const info = map[status] || { className: 'badge cancelled', label: status };
    return <span className={info.className}>{info.label}</span>;
  };

  const getOrderTypeBadge = (type) => {
    if (type === 'BUY') return <span className="badge-profit badge-profit--up">매수</span>;
    if (type === 'SELL') return <span className="badge-profit badge-profit--down">매도</span>;
    return <span className="badge cancelled">{type}</span>;
  };

  // ── 로그 라인 색상 ──
  const getLogLineClass = (line) => {
    if (line.includes('[ERROR]') || line.includes('ERROR')) return 'log-line--error';
    if (line.includes('[WARN]') || line.includes('WARN')) return 'log-line--warn';
    if (line.includes('[QUANT]')) return 'log-line--quant';
    return '';
  };

  return (
    <div>
      {/* ── 탭 네비게이션 ── */}
      <div className="tabs fade-in">
        <button
          className={`tab-btn ${activeTab === 'trades' ? 'tab-btn--active' : ''}`}
          onClick={() => setActiveTab('trades')}
        >
          📋 매매 내역
        </button>
        <button
          className={`tab-btn ${activeTab === 'logs' ? 'tab-btn--active' : ''}`}
          onClick={() => setActiveTab('logs')}
        >
          🖥️ 시스템 로그
        </button>
      </div>

      {error && (
        <div className="card fade-in" style={{ marginTop: 16 }}>
          <div className="error-container" style={{ padding: 24 }}>
            <p className="error-text">{error}</p>
          </div>
        </div>
      )}

      {/* ── 탭 1: 매매 내역 ── */}
      {activeTab === 'trades' && (
        <div className="card fade-in" style={{ marginTop: 16 }}>
          <div className="section-header">
            <h2>매매 내역</h2>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <select
                value={tradeLimit}
                onChange={e => setTradeLimit(Number(e.target.value))}
                style={{
                  background: 'var(--bg-input)',
                  border: '1px solid var(--border-primary)',
                  borderRadius: 'var(--radius-sm)',
                  color: 'var(--text-primary)',
                  padding: '6px 10px',
                  fontSize: '0.8rem'
                }}
              >
                <option value={20}>최근 20건</option>
                <option value={50}>최근 50건</option>
                <option value={100}>최근 100건</option>
              </select>
              <button className="btn btn--outline" onClick={fetchTrades} disabled={tradesLoading}>
                {tradesLoading ? '조회 중...' : '🔄'}
              </button>
            </div>
          </div>

          {tradesLoading ? (
            <div className="loading-container" style={{ padding: 40 }}>
              <div className="loading-spinner" />
            </div>
          ) : trades.length === 0 ? (
            <div className="empty-state">
              <div className="empty-state__icon">📭</div>
              <p className="empty-state__text">거래 내역이 없습니다.</p>
            </div>
          ) : (
            <div className="data-table-wrapper">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>거래일시</th>
                    <th>종목</th>
                    <th>유형</th>
                    <th>수량</th>
                    <th>체결가 (USD)</th>
                    <th>상태</th>
                    <th>주문번호</th>
                  </tr>
                </thead>
                <tbody>
                  {trades.map(t => (
                    <tr key={t.tradeId}>
                      <td style={{ fontSize: '0.8rem' }}>
                        {new Date(t.tradeDate).toLocaleString('ko-KR')}
                      </td>
                      <td className="text-strong">{t.ticker}</td>
                      <td>{getOrderTypeBadge(t.orderType)}</td>
                      <td className="text-strong">{t.qty}주</td>
                      <td>${t.price.toFixed(2)}</td>
                      <td>{getStatusBadge(t.status)}</td>
                      <td style={{ fontSize: '0.75rem', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)' }}>
                        {t.orderNo}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* ── 탭 2: 시스템 로그 ── */}
      {activeTab === 'logs' && (
        <div className="card fade-in" style={{ marginTop: 16 }}>
          <div className="section-header">
            <h2>시스템 로그</h2>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <input
                type="date"
                value={logDate}
                onChange={e => setLogDate(e.target.value)}
                style={{
                  background: 'var(--bg-input)',
                  border: '1px solid var(--border-primary)',
                  borderRadius: 'var(--radius-sm)',
                  color: 'var(--text-primary)',
                  padding: '6px 10px',
                  fontSize: '0.8rem'
                }}
              />
              <button className="btn btn--outline" onClick={fetchLogs} disabled={logsLoading}>
                {logsLoading ? '조회 중...' : '조회'}
              </button>
            </div>
          </div>

          {logsLoading ? (
            <div className="loading-container" style={{ padding: 40 }}>
              <div className="loading-spinner" />
            </div>
          ) : logData ? (
            logData.logs ? (
              <div className="log-viewer">
                <div className="log-viewer__header">
                  <span>{logData.date} — {logData.totalLines}줄</span>
                </div>
                <pre className="log-viewer__content">
                  {logData.logs.map((line, i) => (
                    <div key={i} className={`log-line ${getLogLineClass(line)}`}>
                      {line}
                    </div>
                  ))}
                </pre>
              </div>
            ) : (
              <div className="empty-state">
                <div className="empty-state__icon">📄</div>
                <p className="empty-state__text">{logData.message}</p>
                {logData.availableDates && logData.availableDates.length > 0 && (
                  <div style={{ marginTop: 12 }}>
                    <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: 8 }}>
                      사용 가능한 로그 날짜:
                    </p>
                    <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', justifyContent: 'center' }}>
                      {logData.availableDates.slice(0, 10).map(d => (
                        <button
                          key={d}
                          className="btn btn--outline"
                          style={{ fontSize: '0.75rem', padding: '4px 10px' }}
                          onClick={() => setLogDate(d)}
                        >
                          {d}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )
          ) : null}
        </div>
      )}
    </div>
  );
};

export default History;
