import { useState, useEffect } from 'react';

const SellPlanManager = () => {
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);

  // Form state
  const [ticker, setTicker] = useState('QQQM');
  const [strategyType, setStrategyType] = useState('PRICE');
  const [targetQty, setTargetQty] = useState(10);
  const [trancheQty, setTrancheQty] = useState(2);
  
  // Specific params
  const [targetPrice, setTargetPrice] = useState(250);
  const [nextExecutionDate, setNextExecutionDate] = useState(new Date().toISOString().split('T')[0]);
  const [condition, setCondition] = useState('MA20_BREAK');

  const fetchPlans = async () => {
    try {
      setLoading(true);
      const res = await fetch('/api/sellplan');
      if (res.ok) {
        const data = await res.json();
        setPlans(data);
      }
    } catch (err) {
      console.error('Failed to fetch plans', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPlans();
  }, []);

  const handleCreatePlan = async (e) => {
    e.preventDefault();
    let params = {};
    if (strategyType === 'PRICE') {
      params = { TargetPrice: Number(targetPrice), TrancheQty: Number(trancheQty) };
    } else if (strategyType === 'TIME') {
      params = { NextExecutionDate: nextExecutionDate, TrancheQty: Number(trancheQty) };
    } else if (strategyType === 'CHART') {
      params = { Condition: condition, TrancheQty: Number(trancheQty) };
    }

    const newPlan = {
      ticker,
      strategyType,
      targetQty: Number(targetQty),
      params: JSON.stringify(params)
    };

    try {
      const res = await fetch('/api/sellplan', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newPlan)
      });
      if (res.ok) {
        fetchPlans();
      } else {
        alert('Failed to create plan');
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleCancel = async (id) => {
    if (!confirm('정말로 이 분할매도 플랜을 취소하시겠습니까?')) return;
    try {
      const res = await fetch(`/api/sellplan/${id}`, { method: 'DELETE' });
      if (res.ok) {
        fetchPlans();
      }
    } catch (err) {
      console.error(err);
    }
  };

  const getStrategyLabel = (type) => {
    switch (type) {
      case 'PRICE': return '가격 익절';
      case 'TIME': return '기간 익절';
      case 'CHART': return '차트 익절';
      default: return type;
    }
  };

  const renderParams = (type, paramsStr) => {
    try {
      const params = JSON.parse(paramsStr);
      if (type === 'PRICE') {
        return (
          <>
            <div style={{ marginBottom: '4px' }}>종목 주가 : {params.TargetPrice}</div>
            <div>종목 개수 : {params.TrancheQty}주</div>
          </>
        );
      } else if (type === 'TIME') {
        return (
          <>
            <div style={{ marginBottom: '4px' }}>첫 매도일 : {params.NextExecutionDate}</div>
            <div>종목 개수 : {params.TrancheQty}주</div>
          </>
        );
      } else if (type === 'CHART') {
        const conditionText = params.Condition === 'MA20_BREAK' ? '20일 이평선 이탈' : params.Condition;
        return (
          <>
            <div style={{ marginBottom: '4px' }}>이탈 조건 : {conditionText}</div>
            <div>종목 개수 : {params.TrancheQty}주</div>
          </>
        );
      }
      return paramsStr;
    } catch (e) {
      return paramsStr;
    }
  };

  return (
    <div>
      {/* ── 새 분할매도 설정 ── */}
      <div className="card fade-in">
        <div className="section-header">
          <h2>새 분할매도 설정</h2>
          <span className="section-header__sub">목표 수량을 정해진 단위로 나눠 자동 매도합니다</span>
        </div>
        <form onSubmit={handleCreatePlan}>
          <div className="settings-grid">
            <div className="form-group">
              <label>종목명 (Ticker)</label>
              <input type="text" value={ticker} onChange={e => setTicker(e.target.value.toUpperCase())} required />
            </div>
            <div className="form-group">
              <label>전략 종류</label>
              <select value={strategyType} onChange={e => setStrategyType(e.target.value)}>
                <option value="PRICE">가격 익절 (목표가 도달 시)</option>
                <option value="TIME">기간 익절 (지정일 도달 시)</option>
                <option value="CHART">차트 익절 (지지선 이탈 시)</option>
              </select>
            </div>
            <div className="form-group">
              <label>목표 총 매도 수량</label>
              <input type="number" value={targetQty} onChange={e => setTargetQty(e.target.value)} required min="1" />
            </div>
            <div className="form-group">
              <label>1회 분할 수량</label>
              <input type="number" value={trancheQty} onChange={e => setTrancheQty(e.target.value)} required min="1" />
            </div>

            {strategyType === 'PRICE' && (
              <div className="form-group">
                <label>목표 가격 ($)</label>
                <input type="number" step="0.01" value={targetPrice} onChange={e => setTargetPrice(e.target.value)} required />
              </div>
            )}
            {strategyType === 'TIME' && (
              <div className="form-group">
                <label>첫 매도일</label>
                <input type="date" value={nextExecutionDate} onChange={e => setNextExecutionDate(e.target.value)} required />
              </div>
            )}
            {strategyType === 'CHART' && (
              <div className="form-group">
                <label>이탈 조건</label>
                <select value={condition} onChange={e => setCondition(e.target.value)}>
                  <option value="MA20_BREAK">20일 이평선 이탈 (MA20_BREAK)</option>
                </select>
              </div>
            )}
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 8 }}>
            <button type="submit" className="btn btn--primary">플랜 생성</button>
          </div>
        </form>
      </div>

      {/* ── 활성 분할매도 플랜 ── */}
      <div className="card fade-in" style={{ marginTop: 16 }}>
        <div className="section-header">
          <h2>활성 분할매도 플랜</h2>
          <span className="section-header__sub">{plans.length}건 진행 중</span>
        </div>
        {loading ? (
          <div className="loading-container" style={{ padding: 40 }}>
            <div className="loading-spinner" />
            <p className="loading-text">분할매도 플랜을 불러오는 중...</p>
          </div>
        ) : plans.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state__icon">📭</div>
            <p className="empty-state__text">현재 진행 중인 분할매도 플랜이 없습니다.</p>
          </div>
        ) : (
          <div className="data-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>종목</th>
                  <th>전략</th>
                  <th>상태 / 진행률</th>
                  <th>설정 파라미터</th>
                  <th>관리</th>
                </tr>
              </thead>
              <tbody>
                {plans.map((p, index) => {
                  const progress = Math.min(100, Math.round((p.soldQty / p.targetQty) * 100));
                  return (
                    <tr key={p.planId}>
                      <td>{index + 1}</td>
                      <td className="text-strong">{p.ticker}</td>
                      <td><span className="badge active">{getStrategyLabel(p.strategyType)}</span></td>
                      <td style={{ minWidth: 160 }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 5 }}>
                          <span>{p.soldQty} / {p.targetQty} 주</span>
                          <span style={{ color: 'var(--text-muted)' }}>{progress}%</span>
                        </div>
                        <div className="progress-container">
                          <div className="progress-bar" style={{ width: `${progress}%` }}></div>
                        </div>
                      </td>
                      <td style={{ whiteSpace: 'normal', lineHeight: 1.6 }}>
                        {renderParams(p.strategyType, p.params)}
                      </td>
                      <td>
                        <button className="btn btn--danger" onClick={() => handleCancel(p.planId)}>취소</button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default SellPlanManager;
