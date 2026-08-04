/**
 * 거래이력을 "집행 회차"로 묶는 공용 로직.
 *
 * 거래이력 1행은 주문 1건이라, 한 사이클이 2종목을 사면 행이 2개다. 그 수를 그대로 세면
 * 1회 집행이 "2번 매수"로 읽힌다. 한 사이클의 주문들은 Rate limit 간격을 두고 연달아
 * 접수되므로, 앞 주문과의 간격이 벌어지는 지점에서 회차를 끊는다.
 *
 * ponytail: 시간 간격 휴리스틱이다. TB_TRADE_HISTORY에 사이클 식별자 컬럼이 없어서 쓰는
 * 방법이고, 같은 회차가 10분 넘게 걸리거나 서로 다른 회차가 10분 안에 겹치면 어긋난다.
 * 정확히 세야 할 일이 생기면 실행 ID 컬럼을 추가하는 쪽이 맞다.
 *
 * 적립 설정(월별 로그)과 주문·적립(예약 취소 확인)이 같은 숫자를 보여야 하므로 여기 모은다.
 */

/** 같은 집행으로 볼 최대 주문 간격 (밀리초) */
export const RUN_GAP_MS = 10 * 60 * 1000;

/**
 * 거래이력에서 매수 주문만 골라 파싱된 시각(at)을 붙여 반환합니다.
 * @param {Array} trades /api/history/trades 응답의 trades 배열
 */
export function toBuyRows(trades) {
  const rows = [];
  for (const t of trades ?? []) {
    if ((t.orderType || '').toUpperCase() !== 'BUY') continue;
    const at = new Date(t.tradeDate);
    if (Number.isNaN(at.getTime())) continue;
    rows.push({ ...t, at });
  }
  return rows;
}

/**
 * 주문들을 집행 회차별 배열로 묶습니다 (최신 회차가 앞).
 * @param {Array} rows toBuyRows가 만든 행들
 */
export function groupRuns(rows) {
  if (!rows || rows.length === 0) return [];

  const asc = [...rows].sort((a, b) => a.at - b.at);
  const runs = [[asc[0]]];
  for (let i = 1; i < asc.length; i++) {
    if (asc[i].at - asc[i - 1].at > RUN_GAP_MS) runs.push([asc[i]]);
    else runs[runs.length - 1].push(asc[i]);
  }
  return runs.reverse();
}

/**
 * 특정 연-월의 집행 회차 수를 셉니다.
 * @param {Array} trades /api/history/trades 응답의 trades 배열
 * @param {string} yearMonth 'yyyy-MM'
 */
export function countRunsInMonth(trades, yearMonth) {
  const [y, m] = (yearMonth || '').split('-').map(Number);
  if (!y || !m) return 0;

  const rows = toBuyRows(trades).filter(
    r => r.at.getFullYear() === y && r.at.getMonth() + 1 === m,
  );
  return groupRuns(rows).length;
}
