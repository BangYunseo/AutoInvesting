import { useState } from 'react';
import ProgressLoader from '../components/ProgressLoader';

const STRATEGY_TYPES = ['MEAN_REVERSION', 'MOMENTUM', 'MIXED'];

// 예상 소요시간(초) — 진행바 추정용
const ANALYZE_EST_SEC = 24;    // 단일 종목 분석: 시세·OHLCV 조회 + AI 통합 1회 (실측 ~24초)
const EXECUTE_EST_SEC = 45;    // 전체 실행: 종목별 순차 분석 + 호출 간격(throttle)

/**
 * 퀀트 분석 & 수동 주문 페이지.
 * OrderController와 연동하여 종목 분석과 스마트 주문을 실행합니다.
 */
const Order = () => {
  // ── 분석 상태 ──
  const [ticker, setTicker] = useState('QQQM');
  const [strategy, setStrategy] = useState('MEAN_REVERSION');
  const [analyzing, setAnalyzing] = useState(false);
  const [analysisResult, setAnalysisResult] = useState(null);
  const [analysisError, setAnalysisError] = useState(null);

  // ── 수동 주문 상태 ──
  const [executing, setExecuting] = useState(false);
  const [orderResult, setOrderResult] = useState(null);
  const [orderError, setOrderError] = useState(null);

  const handleAnalyze = async () => {
    try {
      setAnalyzing(true);
      setAnalysisError(null);
      setAnalysisResult(null);
      const res = await fetch(`/api/order/analyze/${encodeURIComponent(ticker)}?strategy=${strategy}`);
      if (!res.ok) throw new Error(`분석 실패 (${res.status})`);
      const data = await res.json();
      setAnalysisResult(data);
    } catch (err) {
      setAnalysisError(err.message);
    } finally {
      setAnalyzing(false);
    }
  };

  const handleExecute = async () => {
    if (!confirm('현재 활성 전략 기반으로 스마트 주문을 즉시 실행합니다.\n정말 진행하시겠습니까?')) return;
    try {
      setExecuting(true);
      setOrderError(null);
      setOrderResult(null);
      const res = await fetch('/api/order/execute', { method: 'POST' });
      if (!res.ok) throw new Error(`주문 실행 실패 (${res.status})`);
      const data = await res.json();
      setOrderResult(data);
    } catch (err) {
      setOrderError(err.message);
    } finally {
      setExecuting(false);
    }
  };

  const getSignalStyle = (signal) => {
    if (signal === 'BUY') return { bg: 'var(--profit-green-bg)', color: 'var(--profit-green)', label: '📈 매수 신호' };
    if (signal === 'SELL') return { bg: 'var(--loss-red-bg)', color: 'var(--loss-red)', label: '📉 매도 신호' };
    return { bg: 'rgba(255,255,255,0.05)', color: 'var(--text-muted)', label: '⏸️ 관망 (HOLD)' };
  };

  // 부가 조언(어드바이저리) 심각도별 스타일
  const getAdvisoryStyle = (severity) => {
    if (severity === 'WARNING') return { icon: '⚠️', bg: 'var(--loss-red-bg)', border: 'rgba(239, 68, 68, 0.3)', color: 'var(--loss-red)' };
    if (severity === 'CAUTION') return { icon: '🔔', bg: 'rgba(245, 158, 11, 0.08)', border: 'rgba(245, 158, 11, 0.25)', color: 'var(--warn-amber)' };
    return { icon: 'ℹ️', bg: 'rgba(59, 130, 246, 0.08)', border: 'rgba(59, 130, 246, 0.25)', color: 'var(--text-secondary)' };
  };

  const renderGauge = (label, value, min, max, unit = '') => {
    const range = max - min;
    const pct = range > 0 ? Math.max(0, Math.min(100, ((value - min) / range) * 100)) : 50;
    return (
      <div className="gauge-item">
        <div className="gauge-item__header">
          <span className="gauge-item__label">{label}</span>
          <span className="gauge-item__value">{typeof value === 'number' ? value.toFixed(2) : value}{unit}</span>
        </div>
        <div className="gauge-bar">
          <div className="gauge-bar__fill" style={{ width: `${pct}%` }} />
        </div>
      </div>
    );
  };

  return (
    <div className="order-layout">
      {/* ── 좌측: 퀀트 분석 ── */}
      <div className="card fade-in fade-in-delay-1">
        <h2>종목 퀀트 분석</h2>

        {/* 입력 폼 */}
        <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end', marginBottom: 20 }}>
          <div className="form-group" style={{ marginBottom: 0, flex: 1 }}>
            <label>종목 코드</label>
            <input
              type="text"
              value={ticker}
              onChange={e => setTicker(e.target.value.toUpperCase())}
              placeholder="예: QQQM"
            />
          </div>
          <div className="form-group" style={{ marginBottom: 0, flex: 1 }}>
            <label>전략 유형</label>
            <select value={strategy} onChange={e => setStrategy(e.target.value)}>
              {STRATEGY_TYPES.map(t => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
          </div>
          <button className="btn btn--primary" onClick={handleAnalyze} disabled={analyzing}>
            {analyzing ? '분석 중...' : '🔍 분석'}
          </button>
        </div>

        {analysisError && (
          <div style={{ padding: '10px 14px', background: 'var(--loss-red-bg)', color: 'var(--loss-red)', borderRadius: 'var(--radius-sm)', fontSize: '0.85rem', marginBottom: 16 }}>
            ❌ {analysisError}
          </div>
        )}

        {/* 분석 진행 표시 */}
        {analyzing && (
          <ProgressLoader estimatedSeconds={ANALYZE_EST_SEC} label="AI가 종목을 분석 중입니다..." />
        )}

        {/* 분석 결과 */}
        {!analyzing && analysisResult && (
          <div className="fade-in">
            {/* 신호 배지 */}
            {(() => {
              const style = getSignalStyle(analysisResult.signal);
              return (
                <div style={{
                  background: style.bg,
                  color: style.color,
                  padding: '16px 20px',
                  borderRadius: 'var(--radius-md)',
                  marginBottom: 16,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between'
                }}>
                  <span style={{ fontSize: '1.1rem', fontWeight: 700 }}>{style.label}</span>
                  <span style={{ fontSize: '1.2rem', fontWeight: 800 }}>{analysisResult.ticker}</span>
                </div>
              );
            })()}

            {/* 분석 이유 (강조) */}
            <div style={{ marginBottom: 20 }}>
              <div style={{ 
                padding: '18px 20px', 
                background: 'rgba(255, 255, 255, 0.05)', 
                borderLeft: `4px solid ${getSignalStyle(analysisResult.signal).color}`,
                borderRadius: '6px',
                boxShadow: '0 4px 6px rgba(0,0,0,0.1)'
              }}>
                <h3 style={{ fontSize: '1.05rem', margin: '0 0 10px 0', color: 'var(--text-primary)', display: 'flex', alignItems: 'center', gap: '8px' }}>
                  🧠 핵심 판단 근거
                </h3>
                <p style={{ fontSize: '1rem', color: 'var(--text-secondary)', lineHeight: 1.6, margin: 0, wordBreak: 'keep-all' }}>
                  {analysisResult.reason}
                </p>
              </div>
            </div>

            {/* 상황 기반 부가 조언 (환율 등) */}
            {analysisResult.advisoryNotes && analysisResult.advisoryNotes.length > 0 && (
              <div style={{ marginBottom: 20, display: 'flex', flexDirection: 'column', gap: 10 }}>
                {analysisResult.advisoryNotes.map((note, i) => {
                  const sev = getAdvisoryStyle(note.severity);
                  return (
                    <div key={i} style={{
                      padding: '14px 16px',
                      background: sev.bg,
                      border: `1px solid ${sev.border}`,
                      borderRadius: 'var(--radius-sm)'
                    }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                        <span style={{ fontSize: '1rem' }}>{sev.icon}</span>
                        <span style={{ fontSize: '0.9rem', fontWeight: 700, color: sev.color }}>
                          [{note.source}] {note.title}
                        </span>
                      </div>
                      <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', lineHeight: 1.6, margin: 0, wordBreak: 'keep-all' }}>
                        {note.message}
                      </p>
                      {note.suggestedAlternatives && note.suggestedAlternatives.length > 0 && (
                        <ul style={{ margin: '8px 0 0 0', paddingLeft: 18, fontSize: '0.82rem', color: 'var(--text-primary)' }}>
                          {note.suggestedAlternatives.map((alt, j) => (
                            <li key={j} style={{ marginBottom: 2 }}>💡 {alt}</li>
                          ))}
                        </ul>
                      )}
                    </div>
                  );
                })}
              </div>
            )}

            {/* 현재가 */}
            <div style={{
              padding: '12px 16px',
              background: 'rgba(255,255,255,0.03)',
              borderRadius: 'var(--radius-sm)',
              marginBottom: 16,
              fontSize: '0.9rem'
            }}>
              💲 현재가: <strong style={{ color: 'var(--text-primary)' }}>${analysisResult.price?.toFixed(2) ?? 'N/A'}</strong>
            </div>

            {/* 퀀트 지표 게이지 */}
            {analysisResult.indicators && (
              <div>
                <h3 style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: 12 }}>퀀트 지표</h3>
                <div className="gauge-grid">
                  {renderGauge('Position', analysisResult.indicators.position, 0, 1)}
                  {renderGauge('RSI (14)', analysisResult.indicators.rsi14, 0, 100)}
                  {renderGauge('MACD Line', analysisResult.indicators.macdLine, -5, 5)}
                  {renderGauge('MACD Signal', analysisResult.indicators.macdSignal, -5, 5)}
                  {renderGauge('MACD Histogram', analysisResult.indicators.macdHistogram, -3, 3)}
                  {renderGauge('BB Upper', analysisResult.indicators.bbUpper, 0, 500, '$')}
                  {renderGauge('BB Middle', analysisResult.indicators.bbMiddle, 0, 500, '$')}
                  {renderGauge('BB Lower', analysisResult.indicators.bbLower, 0, 500, '$')}
                </div>
              </div>
            )}

            {/* 조건 목록 */}
            {analysisResult.conditions && analysisResult.conditions.length > 0 && (
              <div style={{ marginTop: 16 }}>
                <h3 style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: 8 }}>판단 조건</h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                  {analysisResult.conditions.map((c, i) => (
                    <div key={i} style={{
                      padding: '6px 10px',
                      background: c.met ? 'var(--profit-green-bg)' : 'var(--loss-red-bg)',
                      color: c.met ? 'var(--profit-green)' : 'var(--loss-red)',
                      borderRadius: 'var(--radius-sm)',
                      fontSize: '0.78rem',
                      fontFamily: 'var(--font-mono)'
                    }}>
                      {c.met ? '✅' : '❌'} {c.description || c}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {/* ── 우측: 수동 주문 ── */}
      <div className="card fade-in fade-in-delay-2">
        <h2>수동 스마트 주문</h2>

        <div style={{
          padding: '14px 16px',
          background: 'rgba(245, 158, 11, 0.08)',
          border: '1px solid rgba(245, 158, 11, 0.2)',
          borderRadius: 'var(--radius-sm)',
          marginBottom: 20,
          fontSize: '0.82rem',
          color: 'var(--warn-amber)'
        }}>
          ⚠️ 현재 활성화된 전략의 모든 종목에 대해 퀀트 분석 후 조건 충족 시 실제 주문이 실행됩니다.
        </div>

        <button
          className="btn btn--primary"
          onClick={handleExecute}
          disabled={executing}
          style={{ width: '100%', padding: '14px', fontSize: '1rem' }}
        >
          {executing ? '⏳ 실행 중...' : '⚡ 스마트 주문 즉시 실행'}
        </button>

        {/* 실행 진행 표시 */}
        {executing && (
          <div style={{ marginTop: 16 }}>
            <ProgressLoader estimatedSeconds={EXECUTE_EST_SEC} label="전략 종목을 순차 분석 중입니다..." />
          </div>
        )}

        {orderError && (
          <div style={{ marginTop: 16, padding: '10px 14px', background: 'var(--loss-red-bg)', color: 'var(--loss-red)', borderRadius: 'var(--radius-sm)', fontSize: '0.85rem' }}>
            ❌ {orderError}
          </div>
        )}

        {orderResult && (
          <div className="fade-in" style={{ marginTop: 20 }}>
            <div style={{
              padding: '10px 14px',
              background: 'var(--profit-green-bg)',
              color: 'var(--profit-green)',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.85rem',
              marginBottom: 16
            }}>
              ✅ {orderResult.message}
            </div>

            {orderResult.results && orderResult.results.length > 0 && (() => {
              // 신호별 건수 집계 — "N건 실행 완료"가 실제 매매 건수로 오해되지 않도록 명확화
              const buyCount = orderResult.results.filter(r => r.signal === 'BUY').length;
              const sellCount = orderResult.results.filter(r => r.signal === 'SELL').length;
              const holdCount = orderResult.results.filter(r => r.signal !== 'BUY' && r.signal !== 'SELL').length;
              return (
              <>
                <div style={{ display: 'flex', gap: 8, marginBottom: 12, flexWrap: 'wrap' }}>
                  <span style={{ fontSize: '0.78rem', padding: '4px 10px', borderRadius: 6, background: 'var(--profit-green-bg)', color: 'var(--profit-green)', fontWeight: 600 }}>매수 {buyCount}</span>
                  <span style={{ fontSize: '0.78rem', padding: '4px 10px', borderRadius: 6, background: 'var(--loss-red-bg)', color: 'var(--loss-red)', fontWeight: 600 }}>매도 {sellCount}</span>
                  <span style={{ fontSize: '0.78rem', padding: '4px 10px', borderRadius: 6, background: 'rgba(255,255,255,0.05)', color: 'var(--text-muted)', fontWeight: 600 }}>관망 {holdCount}</span>
                </div>
                <div className="data-table-wrapper">
                  <table className="data-table order-result-table">
                    <colgroup>
                      <col style={{ width: '20%' }} />
                      <col style={{ width: '24%' }} />
                      <col style={{ width: '36%' }} />
                      <col style={{ width: '20%' }} />
                    </colgroup>
                    <thead>
                      <tr>
                        <th>종목</th>
                        <th>신호</th>
                        <th>이유</th>
                        <th>가격</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orderResult.results.map((r, i) => {
                        const style = getSignalStyle(r.signal);
                        return (
                          <tr key={i}>
                            <td className="text-strong">{r.ticker}</td>
                            <td>
                              <span style={{
                                display: 'inline-block',
                                padding: '3px 8px',
                                borderRadius: 6,
                                fontSize: '0.75rem',
                                fontWeight: 600,
                                background: style.bg,
                                color: style.color
                              }}>
                                {r.signal}
                              </span>
                            </td>
                            <td className="col-reason">{r.reason}</td>
                            <td>${r.price?.toFixed(2) ?? 'N/A'}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </>
              );
            })()}
          </div>
        )}
      </div>
    </div>
  );
};

export default Order;
