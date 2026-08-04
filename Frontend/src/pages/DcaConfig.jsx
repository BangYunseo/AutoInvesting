import { useEffect, useMemo, useState } from 'react';

/**
 * 적립 설정 페이지.
 * 여러 "매수 템플릿"(예산 + 종목별 고정 수량)을 만들고, 1~12월에 템플릿을 배정합니다.
 * 적립 사이클은 현재(KST) 월에 배정된 템플릿대로 매수합니다(월배정이 비면 첫 템플릿을 매월 사용).
 *
 * - 사람이 정하는 값: 템플릿 구성(종목·수량)·예산, 월별 템플릿 배정.
 * - 자동 계산(읽기 전용): 매수금액(수량×현재가)·예산 대비 비중.
 * - 티커는 /api/price/{ticker}로 실시간 검증 — 현재가가 확인된 종목만 저장됩니다.
 */
const MONTHS = ['1월', '2월', '3월', '4월', '5월', '6월', '7월', '8월', '9월', '10월', '11월', '12월'];

const DcaConfig = () => {
  // templates: [{ id, name, budget, rows: [{ ticker, qty, status, price, error }] }]
  // status: 'saved'(서버 저장값) | 'idle'(검증 필요) | 'checking' | 'valid' | 'invalid'
  const [templates, setTemplates] = useState([]);
  const [monthMap, setMonthMap] = useState({}); // { '1': templateId, ... }
  const [selectedId, setSelectedId] = useState(null);
  const [currentMonth, setCurrentMonth] = useState(0);
  const [exchangeRate, setExchangeRate] = useState(0);
  const [cashUsd, setCashUsd] = useState(0); // 예수금(현금 잔고, USD) — /api/portfolio/summary
  const [accountMode, setAccountMode] = useState(''); // 'SIM' | 'PAPER' | 'LIVE' — 폴백 문구 분기용
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [notice, setNotice] = useState(null);
  // 월별 실행 로그: 매수 기록(TB_TRADE_HISTORY)과 오른쪽에서 볼 연·월
  const [trades, setTrades] = useState([]);
  const [logMonth, setLogMonth] = useState(0);
  const [logYear, setLogYear] = useState(new Date().getFullYear());

  const won = (n) => '₩' + Math.round(n || 0).toLocaleString('ko-KR');
  const newId = () => 't' + Date.now();

  // ── 티커 검증 + 현재가 조회 (특정 템플릿의 특정 행) ──
  const validateRow = async (tid, rowIdx, tickerRaw) => {
    const ticker = (tickerRaw ?? '').trim().toUpperCase();
    const patch = (fn) =>
      setTemplates(ts => ts.map(t => (t.id === tid ? { ...t, rows: t.rows.map((r, i) => (i === rowIdx ? fn(r) : r)) } : t)));

    if (!ticker) {
      patch(r => ({ ...r, status: 'idle', price: 0, error: null }));
      return;
    }
    patch(r => ({ ...r, ticker, status: 'checking', error: null }));
    try {
      const res = await fetch(`/api/price/${encodeURIComponent(ticker)}`);
      if (res.status === 404) {
        patch(r => ({ ...r, status: 'invalid', price: 0, error: '존재하지 않는 티커' }));
        return;
      }
      if (!res.ok) throw new Error(`가격 조회 실패 (${res.status})`);
      const data = await res.json();
      if (data.exchangeRate > 0) setExchangeRate(data.exchangeRate);
      patch(r => ({ ...r, ticker, status: 'valid', price: data.priceUsd, error: null }));
    } catch (err) {
      patch(r => ({ ...r, status: 'invalid', price: 0, error: err.message }));
    }
  };

  // 템플릿의 검증 안 된 행들을 순차 검증 (동시 호출 폭주 방지)
  const validateTemplate = async (tid, rows) => {
    for (let i = 0; i < rows.length; i++) {
      if (rows[i].ticker && rows[i].status !== 'valid') await validateRow(tid, i, rows[i].ticker);
    }
  };

  const loadConfig = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch('/api/dca/config');
      if (!res.ok) throw new Error(`설정 조회 실패 (${res.status})`);
      const data = await res.json();

      const tpls = (data.templates || []).map(t => ({
        id: t.id,
        name: t.name || t.id,
        budget: String(t.budgetKrw ?? ''),
        rows: Object.keys(t.quantities || {}).map(k => ({
          ticker: k, qty: String(t.quantities[k]), status: 'saved', price: 0, error: null,
        })),
      }));
      setTemplates(tpls);
      setMonthMap(data.monthMap || {});
      setCurrentMonth(data.currentMonth || 0);
      setLogMonth(m => m || data.currentMonth || 1);
      const sel = data.activeTemplateId || tpls[0]?.id || null;
      setSelectedId(sel);
      // 선택된 템플릿의 현재가만 조회
      const selTpl = tpls.find(t => t.id === sel);
      if (selTpl) validateTemplate(selTpl.id, selTpl.rows);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  // ── 예수금(현금 잔고) 조회 ──
  // 설정 로드와 독립적으로(비차단) 호출한다. 실패하거나 0이면 예산 기준 표시로 폴백하므로
  // 화면을 막지 않는다. (모의투자 계좌는 KIS가 해외주식 예수금 조회를 미지원 → 항상 $0)
  const loadCash = async () => {
    try {
      const res = await fetch('/api/portfolio/summary');
      if (!res.ok) return; // 조회 실패 → cashUsd=0 유지 → 예산 폴백
      const data = await res.json();
      if (typeof data.cashBalance === 'number') setCashUsd(data.cashBalance);
      if (data.exchangeRate > 0) setExchangeRate(er => (er > 0 ? er : data.exchangeRate));
      if (data.accountMode) setAccountMode(data.accountMode);
    } catch {
      // 예수금 조회 실패는 치명적이지 않다 — 예산 기준으로 폴백(별도 에러 배너 표시 안 함)
    }
  };

  // ── 올해 매수 기록 조회 (월별 "실제 실행됐는지" 확인용) ──
  // 전용 테이블을 새로 만들지 않고 기존 거래이력을 월로 묶어 쓴다. 다만 이 테이블에는
  // 주문 출처 컬럼이 없어 적립 사이클과 수동 매수를 구분하지 못한다(화면에 그대로 밝힌다).
  const loadTrades = async () => {
    try {
      const res = await fetch('/api/history/trades?limit=500');
      if (!res.ok) return; // 로그는 보조 정보 — 실패해도 설정 화면을 막지 않는다
      const data = await res.json();
      setTrades(Array.isArray(data.trades) ? data.trades : []);
    } catch {
      // 조회 실패 시 로그 패널만 비어 보인다(별도 에러 배너 없음)
    }
  };

  useEffect(() => { loadConfig(); loadCash(); loadTrades(); }, []);

  // ── 매수 기록을 연-월로 묶기 ──
  // 연도로 나눠 담는다. 해가 바뀌어도 지난해 기록이 사라지지 않고 연도를 넘겨 보면 그대로 남는다.
  const buysByYearMonth = useMemo(() => {
    const map = {};
    for (const t of trades) {
      if ((t.orderType || '').toUpperCase() !== 'BUY') continue;
      const d = new Date(t.tradeDate);
      if (Number.isNaN(d.getTime())) continue;
      ((map[d.getFullYear()] ??= {})[d.getMonth() + 1] ??= []).push({ ...t, at: d });
    }
    for (const y of Object.keys(map))
      for (const m of Object.keys(map[y])) map[y][m].sort((a, b) => b.at - a.at);
    return map;
  }, [trades]);

  // 기록이 있는 연도 + 올해를 합쳐 오름차순으로 (◀ ▶ 이동 범위)
  const logYears = useMemo(() => {
    const years = new Set(Object.keys(buysByYearMonth).map(Number));
    years.add(new Date().getFullYear());
    return [...years].sort((a, b) => a - b);
  }, [buysByYearMonth]);

  const buysByMonth = buysByYearMonth[logYear] || {};
  const yearIdx = logYears.indexOf(logYear);

  // ── 주문을 "집행 회차"로 묶기 ──
  // 거래이력 1행 = 주문 1건이라, 한 사이클이 2종목을 사면 행이 2개다. 그 수를 그대로 세면
  // "2번 실행"으로 읽힌다. 한 사이클의 주문들은 Rate limit 간격을 두고 연달아 접수되므로,
  // 앞 주문과의 간격이 벌어지는 지점에서 회차를 끊는다.
  //
  // ponytail: 시간 간격 휴리스틱이다. 거래이력에 사이클 식별자 컬럼이 없어서 쓰는 방법이고,
  // 같은 회차가 10분 넘게 걸리거나 서로 다른 회차가 10분 안에 겹치면 어긋난다.
  // 정확히 세야 할 일이 생기면 TB_TRADE_HISTORY에 실행 ID 컬럼을 추가하는 쪽이 맞다.
  const RUN_GAP_MS = 10 * 60 * 1000;
  const groupRuns = (rows) => {
    if (!rows || rows.length === 0) return [];
    const asc = [...rows].sort((a, b) => a.at - b.at);
    const runs = [[asc[0]]];
    for (let i = 1; i < asc.length; i++) {
      const prev = asc[i - 1];
      if (asc[i].at - prev.at > RUN_GAP_MS) runs.push([asc[i]]);
      else runs[runs.length - 1].push(asc[i]);
    }
    return runs.reverse(); // 최신 회차가 위로
  };

  const runCountOf = (m) => groupRuns(buysByMonth[m]).length;

  const selected = templates.find(t => t.id === selectedId) || null;
  const budgetNum = selected ? Number(selected.budget) || 0 : 0;

  // ── 선택 템플릿 편집 헬퍼 ──
  const updateSelected = (fn) => setTemplates(ts => ts.map(t => (t.id === selectedId ? fn(t) : t)));
  const updateRow = (rowIdx, fn) => updateSelected(t => ({ ...t, rows: t.rows.map((r, i) => (i === rowIdx ? fn(r) : r)) }));

  const setTicker = (rowIdx, val) => updateRow(rowIdx, r => ({ ...r, ticker: val.toUpperCase(), status: 'idle', price: 0, error: null }));
  const setQty = (rowIdx, val) => {
    if (val === '') { updateRow(rowIdx, r => ({ ...r, qty: '' })); return; }
    let n = parseInt(val, 10);
    if (Number.isNaN(n)) return;
    updateRow(rowIdx, r => ({ ...r, qty: String(Math.max(1, n)) }));
  };
  const stepQty = (rowIdx, delta) => updateRow(rowIdx, r => ({ ...r, qty: String(Math.max(1, (parseInt(r.qty, 10) || 0) + delta)) }));
  const addRow = () => updateSelected(t => ({ ...t, rows: [...t.rows, { ticker: '', qty: '1', status: 'idle', price: 0, error: null }] }));
  const removeRow = (rowIdx) => updateSelected(t => ({ ...t, rows: t.rows.filter((_, i) => i !== rowIdx) }));
  const setName = (val) => updateSelected(t => ({ ...t, name: val }));
  const setBudget = (val) => updateSelected(t => ({ ...t, budget: val }));

  // ── 템플릿 목록 조작 ──
  const selectTemplate = (id) => {
    setSelectedId(id);
    const t = templates.find(x => x.id === id);
    if (t) validateTemplate(t.id, t.rows);
  };
  const addTemplate = () => {
    const id = newId();
    setTemplates(ts => [...ts, { id, name: '새 템플릿', budget: selected?.budget || '1000000', rows: [] }]);
    setSelectedId(id);
  };
  const duplicateTemplate = (id) => {
    const src = templates.find(t => t.id === id);
    if (!src) return;
    const nid = newId();
    setTemplates(ts => [...ts, { ...src, id: nid, name: src.name + ' 복사', rows: src.rows.map(r => ({ ...r })) }]);
    setSelectedId(nid);
  };
  const deleteTemplate = (id) => {
    if (templates.length <= 1) { setError('템플릿은 최소 1개 이상 있어야 합니다.'); return; }
    const remaining = templates.filter(t => t.id !== id);
    setTemplates(remaining);
    // 해당 템플릿을 가리키던 월배정 제거
    setMonthMap(mm => Object.fromEntries(Object.entries(mm).filter(([, v]) => v !== id)));
    if (selectedId === id) setSelectedId(remaining[0]?.id || null);
  };

  const assignMonth = (monthNum, tid) => {
    setMonthMap(mm => {
      const next = { ...mm };
      if (tid) next[String(monthNum)] = tid;
      else delete next[String(monthNum)];
      return next;
    });
  };

  // ── 계산 (읽기 전용) ──
  const rowAmount = (r) => (r.status === 'valid' && r.price > 0 ? (parseInt(r.qty, 10) || 0) * r.price * exchangeRate : 0);
  const totalCost = selected ? selected.rows.reduce((s, r) => s + rowAmount(r), 0) : 0;
  const overBudget = budgetNum > 0 && totalCost > budgetNum;
  // 예수금 대비 표시용(읽기 전용). 예수금 0(모의투자)이면 hasCash=false → 예산 기준으로 폴백.
  const cashKrw = cashUsd * exchangeRate;
  const hasCash = cashUsd > 0 && exchangeRate > 0;
  const overCash = hasCash && totalCost > cashKrw;

  const handleSave = async () => {
    setError(null);
    setNotice(null);

    const payloadTemplates = [];
    for (const t of templates) {
      const quantities = {};
      for (const r of t.rows) {
        const ticker = r.ticker.trim().toUpperCase();
        const qty = parseInt(r.qty, 10);
        if (!ticker) continue;
        if (r.status === 'invalid') { setError(`'${t.name}' 템플릿의 '${ticker}'는 검증 실패한 티커입니다.`); return; }
        if (r.status === 'idle' || r.status === 'checking') { setError(`'${t.name}' 템플릿의 '${ticker}'를 먼저 검증하세요(엔터/🔍).`); return; }
        if (!(qty > 0)) { setError(`'${t.name}' 템플릿의 '${ticker}' 수량은 1 이상이어야 합니다.`); return; }
        if (quantities[ticker]) { setError(`'${t.name}' 템플릿에 중복 종목: ${ticker}`); return; }
        quantities[ticker] = qty;
      }
      if (Object.keys(quantities).length === 0) { setError(`'${t.name}' 템플릿에 유효 종목이 최소 1개 필요합니다.`); return; }
      const b = Number(t.budget);
      if (!(b > 0)) { setError(`'${t.name}' 템플릿의 예산은 0보다 커야 합니다.`); return; }
      payloadTemplates.push({ id: t.id, name: t.name.trim() || t.id, budgetKrw: b, quantities });
    }

    try {
      setSaving(true);
      const res = await fetch('/api/dca/config', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ templates: payloadTemplates, monthMap }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || `저장 실패 (${res.status})`);
      setNotice(data.message || '저장되었습니다.');
      await loadConfig();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    // 주문 설정과 동일하게 "카드가 떠오르는" 인트로를 유지하기 위해
    // 별도 로딩 박스 대신, 같은 카드 안에서 스피너를 보여준다(교체 깜빡임 제거).
    return (
      <div className="card fade-in fade-in-delay-1" style={{ maxWidth: 980, margin: '0 auto' }}>
        <h2>적립 설정</h2>
        <div className="loading-container" style={{ padding: '48px 20px' }}>
          <div className="loading-spinner" />
          <span className="loading-text">설정을 불러오는 중...</span>
        </div>
      </div>
    );
  }

  const scheduleEmpty = Object.keys(monthMap).length === 0;

  const statusLine = (r) => {
    if (r.status === 'checking') return <span style={{ color: 'var(--text-muted)' }}>검증 중…</span>;
    if (r.status === 'invalid') return <span style={{ color: 'var(--loss-red)' }}>✕ {r.error || '확인 불가'}</span>;
    if (r.status === 'valid') {
      const amount = rowAmount(r);
      const weight = budgetNum > 0 ? (amount / budgetNum) * 100 : 0;
      return (
        <span style={{ color: 'var(--text-secondary)' }}>
          <span style={{ color: 'var(--profit-green)' }}>✓ ${r.price.toFixed(2)}</span>
          {' · '}매수금액 {won(amount)}{' · '}비중 {weight.toFixed(1)}%
        </span>
      );
    }
    return <span style={{ color: 'var(--text-muted)' }}>엔터(또는 🔍)로 현재가를 확인하세요</span>;
  };

  return (
    <div className="card fade-in fade-in-delay-1" style={{ maxWidth: 980, margin: '0 auto' }}>
      <h2>적립 설정</h2>
      <p style={{ color: 'var(--text-secondary)', fontSize: '0.88rem', lineHeight: 1.6, marginBottom: 20, wordBreak: 'keep-all' }}>
        여러 <strong>매수 템플릿</strong>을 만들어 두고 <strong>월별로 배정</strong>하면, 그 달의 적립 사이클은
        해당 템플릿대로 매수합니다. 매달 다른 구성·예산으로 적립할 수 있습니다.
      </p>

      {error && <div className="alert alert--err" style={{ marginBottom: 16 }}>❌ {error}</div>}
      {notice && <div className="alert alert--ok" style={{ marginBottom: 16 }}>✅ {notice}</div>}

      {/* 템플릿 목록 */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <label style={{ fontWeight: 600, fontSize: '0.9rem' }}>ETF 매수 템플릿</label>
        <button className="btn btn--outline" onClick={addTemplate} style={{ padding: '6px 12px', fontSize: '0.8rem' }}>+ 새 템플릿</button>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginBottom: 20 }}>
        {templates.map(t => {
          const isSel = t.id === selectedId;
          const isActive = scheduleEmpty ? templates[0]?.id === t.id : monthMap[String(currentMonth)] === t.id;
          return (
            <div
              key={t.id}
              onClick={() => selectTemplate(t.id)}
              style={{
                display: 'flex', alignItems: 'center', gap: 8, padding: '10px 12px', cursor: 'pointer',
                borderRadius: 'var(--radius-sm)',
                border: isSel ? '1px solid var(--accent, #6ea8fe)' : '1px solid rgba(255,255,255,0.08)',
                background: isSel ? 'rgba(110,168,254,0.08)' : 'rgba(255,255,255,0.02)',
              }}
            >
              <span style={{ color: isActive ? 'var(--profit-green)' : 'var(--text-muted)' }}>{isActive ? '●' : '○'}</span>
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>
                  {t.name}{isActive && <span style={{ color: 'var(--profit-green)', fontSize: '0.75rem', marginLeft: 6 }}>(이번 달 적용)</span>}
                </div>
                <div style={{ color: 'var(--text-muted)', fontSize: '0.76rem' }}>
                  종목 {t.rows.filter(r => r.ticker).length}개 · 예산 {won(Number(t.budget) || 0)}
                </div>
              </div>
              <button className="btn btn--outline" onClick={(e) => { e.stopPropagation(); duplicateTemplate(t.id); }} style={{ padding: '5px 9px', fontSize: '0.75rem' }} title="복제">⧉</button>
              <button className="btn btn--outline" onClick={(e) => { e.stopPropagation(); deleteTemplate(t.id); }} style={{ padding: '5px 9px', fontSize: '0.75rem' }} title="삭제">✕</button>
            </div>
          );
        })}
      </div>

      {/* 선택 템플릿 편집기 */}
      {selected && (
        <div style={{ border: '1px solid rgba(255,255,255,0.08)', borderRadius: 'var(--radius-sm)', padding: 16, marginBottom: 20 }}>
          <div className="form-group">
            <label>템플릿 이름</label>
            <input type="text" value={selected.name} onChange={e => setName(e.target.value)} placeholder="예: 공격형 70:30" />
          </div>
          <div className="form-group">
            <label>월 예산 (원)</label>
            <input type="number" min="0" step="10000" value={selected.budget} onChange={e => setBudget(e.target.value)} placeholder="예: 1000000" />
          </div>

          <label style={{ display: 'block', fontWeight: 600, marginBottom: 8, fontSize: '0.9rem' }}>ETF 설정</label>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 12 }}>
            {selected.rows.map((row, idx) => (
              <div key={idx} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                {/* 좁은 화면에서는 티커 줄과 수량 줄로 접힌다 — wrap이 없으면 티커 입력이 찌그러진다 */}
                <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                  <input
                    type="text" className="input-field" value={row.ticker}
                    onChange={e => setTicker(idx, e.target.value)}
                    onKeyDown={e => { if (e.key === 'Enter') validateRow(selectedId, idx, row.ticker); }}
                    onBlur={() => { if (row.ticker && (row.status === 'idle' || row.status === 'saved')) validateRow(selectedId, idx, row.ticker); }}
                    placeholder="티커 (예: QQQM)" style={{ flex: '2 1 150px', minWidth: 130 }}
                  />
                  <button className="btn btn--outline" onClick={() => validateRow(selectedId, idx, row.ticker)} style={{ padding: '8px 12px', flexShrink: 0 }} title="현재가 확인">🔍</button>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 4, flexShrink: 0 }}>
                    <button className="btn btn--outline" onClick={() => stepQty(idx, -1)} style={{ padding: '8px 12px' }} title="감소">−</button>
                    <input type="number" className="input-field" min="1" step="1" value={row.qty} onChange={e => setQty(idx, e.target.value)} style={{ width: 60, textAlign: 'center' }} />
                    <button className="btn btn--outline" onClick={() => stepQty(idx, 1)} style={{ padding: '8px 12px' }} title="증가">+</button>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>주</span>
                  </div>
                  <button className="btn btn--outline" onClick={() => removeRow(idx)} style={{ padding: '8px 12px', flexShrink: 0, marginLeft: 'auto' }} title="삭제">✕</button>
                </div>
                <div style={{ fontSize: '0.78rem', paddingLeft: 2 }}>{statusLine(row)}</div>
              </div>
            ))}
            {selected.rows.length === 0 && (
              <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>등록된 종목이 없습니다. 아래에서 추가하세요.</p>
            )}
          </div>
          <button className="btn btn--outline" onClick={addRow} style={{ marginBottom: 12 }}>+ 종목 추가</button>

          <div style={{ padding: '12px 14px', background: 'rgba(255,255,255,0.03)', borderRadius: 'var(--radius-sm)', fontSize: '0.85rem', display: 'flex', flexDirection: 'column', gap: 6 }}>
            <div style={{ color: overBudget ? 'var(--loss-red)' : 'var(--text-secondary)' }}>
              총 매수금액: <strong>{won(totalCost)}</strong> / 예산 {won(budgetNum)}
              {overBudget && ` · ⚠ 예산 초과 (${won(totalCost - budgetNum)})`}
            </div>
            <div style={{ color: overBudget ? 'var(--loss-red)' : 'var(--text-secondary)' }}>
              비중 합계 (예산 대비): <strong>{(budgetNum > 0 ? (totalCost / budgetNum) * 100 : 0).toFixed(1)}%</strong>
            </div>
            {hasCash ? (
              <div style={{ color: overCash ? 'var(--loss-red)' : 'var(--text-secondary)', paddingTop: 4, borderTop: '1px solid rgba(255,255,255,0.06)' }}>
                예수금 대비: 예수금 <strong>{won(cashKrw)}</strong>
                {' · '}소진율 <strong>{(cashKrw > 0 ? (totalCost / cashKrw) * 100 : 0).toFixed(1)}%</strong>
                {overCash && ` · ⚠ 예수금 초과 (${won(totalCost - cashKrw)})`}
              </div>
            ) : (
              <div style={{ color: 'var(--text-muted)', fontSize: '0.8rem', paddingTop: 4, borderTop: '1px solid rgba(255,255,255,0.06)' }}>
                {accountMode === 'PAPER'
                  ? '모의투자는 예수금 조회를 미지원 — 예산 기준으로 표시 중'
                  : '예수금이 0원이거나 조회되지 않음 — 예산 기준으로 표시 중'}
              </div>
            )}
          </div>
        </div>
      )}

      {/* 월별 템플릿 배정 */}
      <label style={{ display: 'block', fontWeight: 600, marginBottom: 8, fontSize: '0.9rem' }}>월별 템플릿 배정</label>
      {scheduleEmpty && (
        <p style={{ color: 'var(--text-muted)', fontSize: '0.78rem', marginBottom: 8 }}>
          ※ 배정이 비어 있으면 첫 템플릿(<strong>{templates[0]?.name}</strong>)이 매월 적용됩니다.
        </p>
      )}
      <div style={{ display: 'flex', gap: 16, alignItems: 'flex-start', flexWrap: 'wrap', marginBottom: 24 }}>
        {/* 좌: 월별 배정 리스트 (행을 누르면 오른쪽 로그가 그 달로 바뀜) */}
        <div style={{ flex: '1 1 320px', display: 'flex', flexDirection: 'column', gap: 6 }}>
          {MONTHS.map((label, i) => {
            const m = i + 1;
            const isCur = m === currentMonth && logYear === new Date().getFullYear();
            const isLogSel = m === logMonth;
            const runCount = runCountOf(m);
            const orderCount = (buysByMonth[m] || []).length;
            return (
              <div
                key={m}
                onClick={() => setLogMonth(m)}
                style={{
                  display: 'flex', alignItems: 'center', gap: 6, padding: '6px 8px', cursor: 'pointer',
                  borderRadius: 'var(--radius-sm)',
                  background: isLogSel ? 'rgba(110,168,254,0.14)' : isCur ? 'rgba(110,168,254,0.06)' : 'transparent',
                  border: isLogSel ? '1px solid var(--accent, #6ea8fe)' : isCur ? '1px solid rgba(110,168,254,0.35)' : '1px solid rgba(255,255,255,0.06)',
                }}
              >
                <span
                  title={
                    runCount > 0
                      ? `${logYear}년 ${m}월 ${runCount}회 집행 · 주문 ${orderCount}건`
                      : `${logYear}년 ${m}월 매수 기록 없음`
                  }
                  style={{
                    minWidth: 22, textAlign: 'center', flexShrink: 0, fontSize: '0.7rem',
                    color: runCount > 0 ? 'var(--profit-green)' : 'var(--text-muted)',
                  }}
                >
                  {runCount === 0 ? '○' : runCount === 1 ? '●' : `●${runCount}`}
                </span>
                <span style={{ width: 34, fontSize: '0.8rem', color: isCur ? 'var(--accent, #6ea8fe)' : 'var(--text-secondary)' }}>{label}</span>

                {/* 배정 선택 — 네이티브 select의 펼침 목록은 OS가 그려서 테마가 안 먹는다.
                    라디오를 숨기고 라벨만 칩으로 그리면 팝업 자체가 없어지고,
                    같은 name의 라디오 그룹이라 화살표 이동·스크린리더 낭독은 그대로 남는다. */}
                <div
                  className="chip-row"
                  onClick={e => e.stopPropagation()}
                  role="radiogroup"
                  aria-label={`${label} 배정 템플릿`}
                >
                  {[{ id: '', name: '미배정' }, ...templates].map(t => {
                    const on = (monthMap[String(m)] || '') === t.id;
                    return (
                      <label key={t.id || '_none'} className={`chip ${on ? 'chip--on' : ''}`}>
                        <input
                          type="radio"
                          name={`month-${m}`}
                          checked={on}
                          onChange={() => assignMonth(m, t.id)}
                        />
                        {t.name}
                      </label>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>

        {/* 우: 선택한 달의 실제 매수 기록 */}
        <div style={{ flex: '1 1 340px', border: '1px solid rgba(255,255,255,0.08)', borderRadius: 'var(--radius-sm)', padding: 14, alignSelf: 'stretch' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8, flexWrap: 'wrap', marginBottom: 4 }}>
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
              <button
                className="btn btn--outline"
                onClick={() => setLogYear(logYears[yearIdx - 1])}
                disabled={yearIdx <= 0}
                style={{ padding: '2px 8px', fontSize: '0.75rem' }}
                title="이전 연도"
              >◀</button>
              <strong style={{ fontSize: '0.9rem' }}>{logYear}년 {logMonth}월 실행 기록</strong>
              <button
                className="btn btn--outline"
                onClick={() => setLogYear(logYears[yearIdx + 1])}
                disabled={yearIdx < 0 || yearIdx >= logYears.length - 1}
                style={{ padding: '2px 8px', fontSize: '0.75rem' }}
                title="다음 연도"
              >▶</button>
            </span>
            <button className="btn btn--outline" onClick={loadTrades} style={{ padding: '4px 10px', fontSize: '0.75rem' }}>🔄 새로고침</button>
          </div>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.74rem', marginBottom: 10, wordBreak: 'keep-all' }}>
            거래이력의 매수 기록을 <strong>집행 회차</strong>로 묶은 것입니다. 한 회차가 여러 종목을 사면 주문은 여러 건입니다.
            회차는 주문 시각 간격(10분)으로 추정하며, 이 표에는 주문 출처 컬럼이 없어
            <strong>적립 사이클과 수동 매수를 구분하지 않습니다</strong>. 지정가 주문이므로 접수(PENDING)가 곧 체결은 아닙니다.
          </p>

          {(() => {
            const rows = buysByMonth[logMonth] || [];
            if (rows.length === 0) {
              return (
                <div style={{ color: 'var(--text-muted)', fontSize: '0.83rem', padding: '18px 0', textAlign: 'center' }}>
                  이 달에는 매수 기록이 없습니다.
                </div>
              );
            }
            const runs = groupRuns(rows);
            const totalQty = rows.reduce((s, r) => s + (r.qty || 0), 0);
            const totalUsd = rows.reduce((s, r) => s + (r.qty || 0) * (r.price || 0), 0);
            const stamp = (d) =>
              `${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')} ` +
              `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
            return (
              <>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12, maxHeight: 360, overflowY: 'auto' }}>
                  {runs.map((run, ri) => {
                    const runUsd = run.reduce((s, r) => s + (r.qty || 0) * (r.price || 0), 0);
                    return (
                      <div key={run[0].tradeId} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                        {/* 회차 머리글 — 주문 여러 건이 한 번의 집행임을 드러낸다 */}
                        <div style={{ display: 'flex', alignItems: 'baseline', gap: 8, fontSize: '0.76rem', color: 'var(--text-muted)' }}>
                          <strong style={{ color: 'var(--text-secondary)' }}>
                            {runs.length - ri}회차
                          </strong>
                          <span>{stamp(run[0].at)}</span>
                          <span style={{ marginLeft: 'auto' }}>
                            주문 {run.length}건 · ${runUsd.toFixed(2)}
                          </span>
                        </div>
                        {run.map(r => (
                          <div key={r.tradeId} style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: '0.78rem', padding: '6px 8px', background: 'rgba(255,255,255,0.03)', borderRadius: 'var(--radius-sm)' }}>
                            <span style={{ color: 'var(--text-muted)', width: 40, flexShrink: 0 }}>
                              {String(r.at.getHours()).padStart(2, '0')}:{String(r.at.getMinutes()).padStart(2, '0')}
                            </span>
                            <span style={{ fontWeight: 600, width: 58, flexShrink: 0 }}>{r.ticker}</span>
                            <span style={{ flex: 1 }}>{r.qty}주 · ${Number(r.price).toFixed(2)}</span>
                            <span style={{ color: r.status === 'FILLED' ? 'var(--profit-green)' : 'var(--warn-amber)', flexShrink: 0 }}>{r.status}</span>
                          </div>
                        ))}
                      </div>
                    );
                  })}
                </div>
                <div style={{ marginTop: 10, paddingTop: 8, borderTop: '1px solid rgba(255,255,255,0.06)', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                  <strong>{runs.length}회 집행</strong> · 주문 {rows.length}건 · {totalQty}주 · ${totalUsd.toFixed(2)}
                  {exchangeRate > 0 && ` (${won(totalUsd * exchangeRate)})`}
                </div>
              </>
            );
          })()}
        </div>
      </div>

      <button className="btn btn--primary" onClick={handleSave} disabled={saving} style={{ width: '100%', padding: '14px', fontSize: '1rem' }}>
        {saving ? '⏳ 저장 중...' : '💾 적립 설정 저장'}
      </button>
    </div>
  );
};

export default DcaConfig;
