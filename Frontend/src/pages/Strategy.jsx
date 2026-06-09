import { useState, useEffect, useCallback, useRef } from 'react';

// 미국 인기 주식 및 ETF (검증 및 자동완성용 하드코딩 리스트)
const POPULAR_ETFS = [
  'SPY', 'IVV', 'VOO', 'QQQ', 'VTI', 'VEA', 'IEFA', 'VWO', 'IEMG', 'BND',
  'AGG', 'GLD', 'VIG', 'SCHD', 'VYM', 'SDY', 'QQQM', 'TQQQ', 'SQQQ', 'SOXX',
  'SOXL', 'SOXS', 'ARKK', 'ARKG', 'ARKW', 'VNQ', 'VNQI', 'TLT', 'IEF', 'SHY',
  'LQD', 'HYG', 'JNK', 'XLF', 'XLK', 'XLV', 'XLE', 'XLY', 'XLP', 'XLI',
  'XLU', 'XLB', 'XLRE', 'DIA', 'IWM', 'IJR', 'IJH', 'MDY', 'SPYG', 'SPYV',
  'AAPL', 'MSFT', 'GOOGL', 'AMZN', 'NVDA', 'META', 'TSLA', 'BRK.B', 'UNH', 'JNJ'
];

// 초보자 맞춤 전략 명칭 및 설명 매핑
const STRATEGY_TYPES = [
  {
    id: 'MEAN_REVERSION',
    label: '하락 매수형',
    description: "주가가 단기적으로 지나치게 많이 떨어져서 '싸다'고 판단될 때 사고, 단기 급등하면 이익을 실현하는 전략입니다."
  },
  {
    id: 'MOMENTUM',
    label: '상승 추세형',
    description: "주가가 본격적으로 상승세를 탔다고 판단될 때만 따라 들어가서 수익을 극대화하는 전략입니다."
  },
  {
    id: 'MIXED',
    label: '안전 혼합형',
    description: "위의 두 가지 전략의 조건을 모두 만족할 때만 들어가는 아주 보수적이고 안전한 전략입니다."
  }
];

const Strategy = () => {
  // ── 뷰 모드: 'list' (전략 목록) | 'edit' (전략 수정) ──
  const [viewMode, setViewMode] = useState('list');
  
  // ── [List View] 상태 ──
  const [strategySummaries, setStrategySummaries] = useState([]);
  const [searchQuery, setSearchQuery] = useState('');
  
  // ── [Edit View] 상태 ──
  const [currentStrategyName, setCurrentStrategyName] = useState('');
  const [currentStrategyType, setCurrentStrategyType] = useState('MEAN_REVERSION');
  const [strategies, setStrategies] = useState([]); // 현재 전략에 속한 종목 목록
  
  // ── 공통 상태 ──
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [message, setMessage] = useState(null);
  
  // ── 모달 상태 ──
  const [isAddStrategyModalOpen, setIsAddStrategyModalOpen] = useState(false);
  const [isAddTickerModalOpen, setIsAddTickerModalOpen] = useState(false);
  
  // ── 폼 입력 상태 ──
  const [newStrategyName, setNewStrategyName] = useState('');
  const [newStrategyType, setNewStrategyType] = useState('MEAN_REVERSION');
  const [newTicker, setNewTicker] = useState('');
  const [newQty, setNewQty] = useState(1);
  
  // ── 자동완성 상태 ──
  const [suggestions, setSuggestions] = useState([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [activeSuggestionIndex, setActiveSuggestionIndex] = useState(0);
  const autocompleteRef = useRef(null);

  // -------------------------------------------------------------
  // API Fetching
  // -------------------------------------------------------------
  const fetchStrategySummaries = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch('/api/strategy/summary');
      if (!res.ok) throw new Error(`서버 오류 (${res.status})`);
      const data = await res.json();
      setStrategySummaries(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchStrategyDetails = async (name, type) => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch(`/api/strategy/${encodeURIComponent(name)}`);
      if (!res.ok) throw new Error(`서버 오류 (${res.status})`);
      const data = await res.json();
      
      setCurrentStrategyName(name);
      setCurrentStrategyType(type);
      setStrategies(data);
      setViewMode('edit');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (viewMode === 'list') {
      fetchStrategySummaries();
    }
  }, [viewMode, fetchStrategySummaries]);

  // -------------------------------------------------------------
  // Autocomplete Logic
  // -------------------------------------------------------------
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (autocompleteRef.current && !autocompleteRef.current.contains(event.target)) {
        setShowSuggestions(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleTickerChange = (e) => {
    const val = e.target.value.toUpperCase();
    setNewTicker(val);
    if (val.length > 0) {
      const filtered = POPULAR_ETFS.filter(t => t.includes(val));
      setSuggestions(filtered);
      setShowSuggestions(true);
      setActiveSuggestionIndex(0);
    } else {
      setSuggestions([]);
      setShowSuggestions(false);
    }
  };

  const handleTickerKeyDown = (e) => {
    if (!showSuggestions) return;

    if (e.key === 'ArrowDown') {
      setActiveSuggestionIndex(prev => Math.min(prev + 1, suggestions.length - 1));
      e.preventDefault();
    } else if (e.key === 'ArrowUp') {
      setActiveSuggestionIndex(prev => Math.max(prev - 1, 0));
      e.preventDefault();
    } else if (e.key === 'Enter') {
      if (suggestions.length > 0 && activeSuggestionIndex >= 0) {
        selectSuggestion(suggestions[activeSuggestionIndex]);
      }
      e.preventDefault();
    }
  };

  const selectSuggestion = (ticker) => {
    setNewTicker(ticker);
    setShowSuggestions(false);
  };

  // -------------------------------------------------------------
  // Modal Handlers
  // -------------------------------------------------------------
  const openAddStrategyModal = () => {
    setNewStrategyName('');
    setNewStrategyType('MEAN_REVERSION');
    setNewTicker('');
    setNewQty(1);
    setShowSuggestions(false);
    setIsAddStrategyModalOpen(true);
  };

  const openAddTickerModal = () => {
    setNewTicker('');
    setNewQty(1);
    setShowSuggestions(false);
    setIsAddTickerModalOpen(true);
  };

  // -------------------------------------------------------------
  // Validation & Actions
  // -------------------------------------------------------------
  const validateTicker = (ticker) => {
    if (!POPULAR_ETFS.includes(ticker)) {
      alert("존재하지 않는 종목코드입니다. 올바른 코드를 입력해주세요.");
      return false;
    }
    return true;
  };

  const handleCreateNewStrategy = async () => {
    if (!newStrategyName.trim()) return alert("전략명을 입력해주세요.");
    if (!newTicker.trim()) return alert("초기 종목 코드를 입력해주세요.");
    
    // Validate
    if (!validateTicker(newTicker)) return;

    // Check if strategy name already exists
    if (strategySummaries.some(s => s.strategyName === newStrategyName.trim())) {
      return alert("이미 존재하는 전략명입니다.");
    }

    const newStrategyData = [{
      strategyId: 0,
      strategyName: newStrategyName.trim(),
      ticker: newTicker,
      qty: Number(newQty),
      strategyType: newStrategyType
    }];

    try {
      setSaving(true);
      const res = await fetch(`/api/strategy/${encodeURIComponent(newStrategyName.trim())}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newStrategyData)
      });
      if (!res.ok) throw new Error("전략 생성 실패");
      
      // Navigate to Edit View
      setCurrentStrategyName(newStrategyName.trim());
      setCurrentStrategyType(newStrategyType);
      setStrategies(newStrategyData);
      setIsAddStrategyModalOpen(false);
      setViewMode('edit');
      showMessage('✅ 새 전략이 생성되었습니다.');
    } catch (err) {
      alert(`생성 실패: ${err.message}`);
    } finally {
      setSaving(false);
    }
  };

  const handleAddTickerToCurrent = () => {
    if (!newTicker.trim()) return alert("종목 코드를 입력해주세요.");
    if (strategies.some(s => s.ticker === newTicker)) return alert("이미 추가된 종목입니다.");
    if (!validateTicker(newTicker)) return;

    setStrategies(prev => [
      ...prev,
      {
        strategyId: 0,
        strategyName: currentStrategyName,
        ticker: newTicker,
        qty: Number(newQty),
        strategyType: currentStrategyType
      }
    ]);
    setIsAddTickerModalOpen(false);
  };

  const handleRemoveTicker = (ticker) => {
    setStrategies(prev => prev.filter(s => s.ticker !== ticker));
  };

  const handleQtyChange = (ticker, value) => {
    setStrategies(prev => prev.map(s => s.ticker === ticker ? { ...s, qty: Number(value) } : s));
  };

  const handleSaveCurrentStrategy = async () => {
    try {
      setSaving(true);
      const res = await fetch(`/api/strategy/${encodeURIComponent(currentStrategyName)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(strategies)
      });
      if (!res.ok) throw new Error("저장 실패");
      showMessage('✅ 전략이 성공적으로 저장되었습니다.');
    } catch (err) {
      showMessage(`❌ ${err.message}`);
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteCurrentStrategy = async () => {
    if (!confirm(`'${currentStrategyName}' 전략을 정말 삭제하시겠습니까?`)) return;
    try {
      const res = await fetch(`/api/strategy/${encodeURIComponent(currentStrategyName)}`, {
        method: 'DELETE'
      });
      if (!res.ok) throw new Error("삭제 실패");
      showMessage('🗑️ 전략이 삭제되었습니다.');
      setViewMode('list');
    } catch (err) {
      showMessage(`❌ ${err.message}`);
    }
  };

  const showMessage = (msg) => {
    setMessage(msg);
    setTimeout(() => setMessage(null), 3000);
  };

  return (
    <div>
      {/* ── 상단 메시지 알림 ── */}
      {message && (
        <div style={{
          padding: '10px 14px',
          borderRadius: 'var(--radius-sm)',
          background: message.includes('✅') || message.includes('🗑️') ? 'var(--profit-green-bg)' : 'var(--loss-red-bg)',
          color: message.includes('✅') || message.includes('🗑️') ? 'var(--profit-green)' : 'var(--loss-red)',
          fontSize: '0.85rem',
          marginBottom: 16
        }}>
          {message}
        </div>
      )}

      {/* ── 뷰 1: 전략 목록 화면 (List View) ── */}
      {viewMode === 'list' && (
        <div className="card fade-in">
          <div className="section-header" style={{ marginBottom: 20 }}>
            <h2>투자 전략 관리</h2>
            <div style={{ display: 'flex', gap: 12 }}>
              <div style={{ display: 'flex', gap: 6 }}>
                <input 
                  type="text" 
                  placeholder="존재하는 전략명 검색..." 
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  onKeyDown={e => {
                    if (e.key === 'Enter') {
                      const found = strategySummaries.find(s => s.strategyName.toLowerCase() === searchQuery.toLowerCase());
                      if (found) fetchStrategyDetails(found.strategyName, found.strategyType);
                      else alert('해당 이름의 전략이 존재하지 않습니다.');
                    }
                  }}
                  style={{
                    background: 'var(--bg-input)',
                    border: '1px solid var(--border-primary)',
                    borderRadius: 'var(--radius-sm)',
                    color: 'var(--text-primary)',
                    padding: '8px 12px',
                    fontSize: '0.85rem',
                    width: 220
                  }}
                />
                <button 
                  className="btn btn--outline" 
                  onClick={() => {
                    const found = strategySummaries.find(s => s.strategyName.toLowerCase() === searchQuery.toLowerCase());
                    if (found) fetchStrategyDetails(found.strategyName, found.strategyType);
                    else alert('해당 이름의 전략이 존재하지 않습니다.');
                  }}
                >
                  🔍 검색
                </button>
              </div>
              <button className="btn btn--green" onClick={openAddStrategyModal}>
                + 새 전략 추가
              </button>
            </div>
          </div>

          {loading ? (
            <div className="loading-container"><div className="loading-spinner" /></div>
          ) : error ? (
            <div className="error-container"><p className="error-text">{error}</p></div>
          ) : (
            <>
              {strategySummaries.length === 0 ? (
                <div className="empty-state">
                  <div className="empty-state__icon">📭</div>
                  <p className="empty-state__text">검색된 전략이 없습니다. 새 전략을 추가해 보세요.</p>
                </div>
              ) : (
                <div className="strategy-card-grid">
                  {strategySummaries.map((s, idx) => (
                    <div key={idx} className="strategy-card fade-in" onClick={() => fetchStrategyDetails(s.strategyName, s.strategyType)}>
                      <div className="strategy-card__header">
                        <h3 className="strategy-card__title">{s.strategyName}</h3>
                        <span className="strategy-card__type">
                          {STRATEGY_TYPES.find(t => t.id === s.strategyType)?.label || s.strategyType}
                        </span>
                      </div>
                      <div className="strategy-card__stats">
                        포함된 종목 수: <strong>{s.tickerCount}</strong>개
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* ── 뷰 2: 전략 수정 화면 (Edit View) ── */}
      {viewMode === 'edit' && (
        <div className="card fade-in">
          <div className="section-header" style={{ borderBottom: '1px solid var(--border-primary)', paddingBottom: 16, marginBottom: 20 }}>
            <div>
              <h2 style={{ fontSize: '1.4rem', margin: 0, padding: 0, border: 'none', display: 'flex', alignItems: 'center', gap: 10 }}>
                {currentStrategyName}
                <span style={{ fontSize: '0.8rem', padding: '4px 8px', background: 'var(--bg-input)', borderRadius: 4, color: 'var(--text-secondary)' }}>
                  {STRATEGY_TYPES.find(t => t.id === currentStrategyType)?.label}
                </span>
                <div className="tooltip-container">
                  <span className="tooltip-icon">?</span>
                  <div className="tooltip-text">
                    {STRATEGY_TYPES.find(t => t.id === currentStrategyType)?.description}
                  </div>
                </div>
              </h2>
            </div>
            <button className="btn btn--outline" onClick={() => setViewMode('list')}>
              ⬅️ 목록으로
            </button>
          </div>

          <div className="data-table-wrapper">
            <table className="data-table fixed-layout">
              <thead>
                <tr>
                  <th style={{ width: '40%' }}>종목 코드</th>
                  <th style={{ width: '40%' }}>매수 수량</th>
                  <th style={{ width: '20%', textAlign: 'center' }}>관리</th>
                </tr>
              </thead>
              <tbody>
                {strategies.length === 0 ? (
                  <tr>
                    <td colSpan="3" style={{ textAlign: 'center', padding: '40px 0' }}>등록된 종목이 없습니다.</td>
                  </tr>
                ) : (
                  strategies.map((s, idx) => (
                    <tr key={s.ticker}>
                      <td>
                        <span className="ticker-badge">
                          <span className={`ticker-dot ticker-dot--${idx % 5}`} />
                          {s.ticker}
                        </span>
                      </td>
                      <td>
                        <input
                          type="number"
                          min="1"
                          value={s.qty}
                          onChange={e => handleQtyChange(s.ticker, e.target.value)}
                          style={{
                            background: 'var(--bg-input)',
                            border: '1px solid var(--border-primary)',
                            borderRadius: 'var(--radius-sm)',
                            color: 'var(--text-primary)',
                            padding: '6px 10px',
                            fontSize: '0.85rem',
                            width: 80
                          }}
                        />
                      </td>
                      <td style={{ textAlign: 'center' }}>
                        <button
                          className="btn btn--danger"
                          style={{ fontSize: '0.75rem', padding: '4px 10px' }}
                          onClick={() => handleRemoveTicker(s.ticker)}
                        >
                          삭제
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div style={{ marginTop: 24, display: 'flex', justifyContent: 'space-between' }}>
            <button className="btn btn--outline" onClick={openAddTickerModal}>
              + 종목 추가
            </button>
            <div style={{ display: 'flex', gap: 10 }}>
              <button className="btn btn--danger" onClick={handleDeleteCurrentStrategy}>
                🗑️ 전략 전체 삭제
              </button>
              <button className="btn btn--primary" onClick={handleSaveCurrentStrategy} disabled={saving}>
                {saving ? '저장 중...' : '💾 전략 저장'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── 모달 1: 새 전략 추가 ── */}
      {isAddStrategyModalOpen && (
        <div className="modal-overlay" onClick={() => setIsAddStrategyModalOpen(false)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <h3 style={{ marginBottom: 20, borderBottom: '1px solid var(--border-primary)', paddingBottom: 12 }}>새 전략 생성</h3>
            
            <div className="form-group">
              <label>전략명</label>
              <input type="text" value={newStrategyName} onChange={e => setNewStrategyName(e.target.value)} placeholder="예: 나의 기술주 포트폴리오" />
            </div>

            <div className="form-group">
              <label>전략 유형 (모든 종목에 공통 적용됨)</label>
              <div style={{ display: 'flex', alignItems: 'center' }}>
                <select value={newStrategyType} onChange={e => setNewStrategyType(e.target.value)}>
                  {STRATEGY_TYPES.map(t => (
                    <option key={t.id} value={t.id}>{t.label}</option>
                  ))}
                </select>
                <div className="tooltip-container">
                  <span className="tooltip-icon">?</span>
                  <div className="tooltip-text">
                    {STRATEGY_TYPES.find(t => t.id === newStrategyType)?.description}
                  </div>
                </div>
              </div>
            </div>

            <hr style={{ border: 'none', borderTop: '1px solid var(--border-primary)', margin: '20px 0' }} />
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: 12 }}>전략에 포함될 첫 번째 종목을 입력해주세요.</p>

            <div className="grid-2">
              <div className="form-group" style={{ position: 'relative' }} ref={autocompleteRef}>
                <label>종목 코드 (Ticker)</label>
                <input
                  type="text"
                  value={newTicker}
                  onChange={handleTickerChange}
                  onKeyDown={handleTickerKeyDown}
                  placeholder="예: QQQ"
                  autoComplete="off"
                />
                {showSuggestions && suggestions.length > 0 && (
                  <ul className="autocomplete-list">
                    {suggestions.map((ticker, idx) => (
                      <li 
                        key={ticker} 
                        className={`autocomplete-item ${idx === activeSuggestionIndex ? 'active' : ''}`}
                        onClick={() => selectSuggestion(ticker)}
                      >
                        {ticker}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <div className="form-group">
                <label>매수 수량</label>
                <input type="number" min="1" value={newQty} onChange={e => setNewQty(e.target.value)} />
              </div>
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 16 }}>
              <button className="btn btn--outline" onClick={() => setIsAddStrategyModalOpen(false)}>취소</button>
              <button className="btn btn--green" onClick={handleCreateNewStrategy}>생성하기</button>
            </div>
          </div>
        </div>
      )}

      {/* ── 모달 2: 기존 전략에 종목 추가 ── */}
      {isAddTickerModalOpen && (
        <div className="modal-overlay" onClick={() => setIsAddTickerModalOpen(false)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <h3 style={{ marginBottom: 20, borderBottom: '1px solid var(--border-primary)', paddingBottom: 12 }}>[{currentStrategyName}] 종목 추가</h3>
            
            <div className="form-group" style={{ position: 'relative' }} ref={autocompleteRef}>
              <label>종목 코드 (Ticker)</label>
              <input
                type="text"
                value={newTicker}
                onChange={handleTickerChange}
                onKeyDown={handleTickerKeyDown}
                placeholder="예: SPY"
                autoComplete="off"
              />
              {showSuggestions && suggestions.length > 0 && (
                <ul className="autocomplete-list">
                  {suggestions.map((ticker, idx) => (
                    <li 
                      key={ticker} 
                      className={`autocomplete-item ${idx === activeSuggestionIndex ? 'active' : ''}`}
                      onClick={() => selectSuggestion(ticker)}
                    >
                      {ticker}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="form-group">
              <label>매수 수량</label>
              <input type="number" min="1" value={newQty} onChange={e => setNewQty(e.target.value)} />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 24 }}>
              <button className="btn btn--outline" onClick={() => setIsAddTickerModalOpen(false)}>취소</button>
              <button className="btn btn--green" onClick={handleAddTickerToCurrent}>추가하기</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Strategy;
