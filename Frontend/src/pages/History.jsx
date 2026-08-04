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
      setTrades(Array.isArray(data?.trades) ? data.trades : []);
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

  // 상태 라벨. 배지 색상 modifier(completed/active/cancelled)는 CSS에 정의된 적이 없어
  // 어차피 .badge 단일 스타일로 렌더링되므로 클래스 분기를 두지 않는다.
  // PENDING = 접수됐으나 체결 미확인 (지정가 주문이라 미체결로 끝날 수 있다)
  // PARTIAL = 일부만 체결 / FILLED = 전량 체결 확인
  const STATUS_LABEL = { FILLED: '체결', PARTIAL: '부분체결', PENDING: '접수', FAILED: '실패' };

  const getStatusBadge = (status) => <span className="badge">{STATUS_LABEL[status] ?? status}</span>;

  // ── 로그 달력 ──
  // 네이티브 날짜 입력의 달력은 브라우저가 그려 테마가 안 먹고, 로그가 없는 날까지 전부 고를 수
  // 있어 헛클릭을 부른다. 서버가 주는 "로그가 있는 날짜"만 활성화한 달력을 직접 그린다.
  const today = new Date().toISOString().split('T')[0];
  const availableSet = new Set([...(logData?.availableDates ?? []), logDate]);

  // 달력이 보여줄 연-월 ('yyyy-MM'). 선택 날짜가 다른 달로 바뀌면 따라 이동한다.
  // 이펙트로 맞추면 한 번 더 렌더된 뒤에야 따라오므로, 렌더 중에 바로 보정한다(React 권장 패턴).
  const [calMonth, setCalMonth] = useState(logDate.slice(0, 7));
  const [calSyncedDate, setCalSyncedDate] = useState(logDate);
  if (calSyncedDate !== logDate) {
    setCalSyncedDate(logDate);
    setCalMonth(logDate.slice(0, 7));
  }

  const calYear = Number(calMonth.slice(0, 4));
  const calMon = Number(calMonth.slice(5, 7)); // 1~12

  const shiftMonth = (delta) => {
    const d = new Date(calYear, calMon - 1 + delta, 1);
    setCalMonth(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`);
  };

  // 앞쪽 빈 칸(1일의 요일) + 그 달의 날짜들 + 뒤쪽 빈 칸.
  // 항상 6줄(42칸)로 채운다 — 달마다 줄 수가 달라지면 달을 넘길 때 카드 높이가 출렁인다.
  const leadingBlanks = new Date(calYear, calMon - 1, 1).getDay();
  const daysInMonth = new Date(calYear, calMon, 0).getDate();
  const calCells = [
    ...Array(leadingBlanks).fill(null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ];
  while (calCells.length < 42) calCells.push(null);

  const dayKey = (day) => `${calMonth}-${String(day).padStart(2, '0')}`;

  const getOrderTypeBadge = (type) => {
    if (type === 'BUY') return <span className="badge-profit badge-profit--up">매수</span>;
    if (type === 'SELL') return <span className="badge-profit badge-profit--down">매도</span>;
    return <span className="badge">{type}</span>;
  };

  // ── 로그 라인 색상 ──
  const getLogLineClass = (line) => {
    if (line.includes('[ERROR]') || line.includes('ERROR')) return 'log-line--error';
    if (line.includes('[WARN]') || line.includes('WARN')) return 'log-line--warn';
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
              <div className="chip-row" role="radiogroup" aria-label="조회 건수" style={{ flex: 'none' }}>
                {[20, 50, 100].map(n => (
                  <label key={n} className={`chip ${tradeLimit === n ? 'chip--on' : ''}`}>
                    <input
                      type="radio"
                      name="trade-limit"
                      checked={tradeLimit === n}
                      onChange={() => setTradeLimit(n)}
                    />
                    {n}건
                  </label>
                ))}
              </div>
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
                    {/* 접수 시점에 기록되는 주문 지정가다 — 체결가가 아니다 */}
                    <th>주문가 (USD)</th>
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
                        {/* 2026-07-30 주문번호 저장 배선 이전 행은 값이 없다 — 빈칸이 렌더 오류로 보이지 않게 표시 */}
                        {t.orderNo || '—'}
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
              <span style={{ fontSize: '0.82rem', fontVariantNumeric: 'tabular-nums', color: 'var(--text-primary)' }}>
                {logDate}
              </span>
              {logDate !== today && (
                <button
                  className="btn btn--outline"
                  onClick={() => setLogDate(today)}
                  disabled={logsLoading}
                  style={{ padding: '4px 10px', fontSize: '0.8rem' }}
                >
                  오늘
                </button>
              )}
              <button className="btn btn--outline" onClick={fetchLogs} disabled={logsLoading}>
                {logsLoading ? '조회 중...' : '🔄'}
              </button>
            </div>
          </div>

          {/* 로그가 있는 날만 누를 수 있는 달력. 팝업이 아니라 카드 안에 그대로 둔다 —
              바깥 클릭·포커스 트랩 같은 팝업 처리를 만들 필요가 없다. */}
          <div className="log-cal">
            <div className="log-cal__head">
              <button className="btn btn--outline" onClick={() => shiftMonth(-1)} title="이전 달">◀</button>
              <strong>{calYear}년 {calMon}월</strong>
              <button className="btn btn--outline" onClick={() => shiftMonth(1)} title="다음 달">▶</button>
            </div>

            <div className="log-cal__grid">
              {['일', '월', '화', '수', '목', '금', '토'].map(d => (
                <div key={d} className="log-cal__dow">{d}</div>
              ))}

              {calCells.map((day, i) => {
                if (day === null) return <div key={`b${i}`} />;
                const key = dayKey(day);
                const has = availableSet.has(key);
                const cls = 'log-cal__day'
                  + (has ? ' log-cal__day--has' : '')
                  + (key === logDate ? ' log-cal__day--sel' : '')
                  + (key === today ? ' log-cal__day--today' : '');
                return (
                  <button
                    key={key}
                    className={cls}
                    disabled={!has || logsLoading}
                    onClick={() => setLogDate(key)}
                    title={has ? `${key} 로그 보기` : `${key} 로그 없음`}
                  >
                    {day}
                  </button>
                );
              })}
            </div>

            <div className="log-cal__note">
              로그가 남아 있는 날만 선택할 수 있습니다. 로그는 90일이 지나면 정리됩니다.
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
