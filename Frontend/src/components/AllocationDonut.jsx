import { useMemo, useState } from 'react';

/**
 * 보유 종목 비중 도넛.
 *
 * 외부 차트 라이브러리를 쓰지 않는다 — 종목 몇 개짜리 도넛 하나를 위해 200KB를 더할 이유가 없다.
 * SVG 원 하나에 stroke-dasharray로 조각을 그린다(호 path 계산 불필요).
 *
 * 3D로 그리지 않는 이유: 원근이 들어가면 앞쪽 조각이 실제 비중보다 커 보인다.
 * 비중을 읽으라고 만든 그림이 비중을 왜곡하면 안 된다.
 *
 * 색은 조각 순서가 아니라 종목에 붙는다(정렬이 바뀌어도 같은 종목은 같은 색).
 * 조각 사이 2px 간격과 범례의 정확한 수치가 색만으로 구분하지 않게 하는 보조 채널이다.
 */

// 카테고리 색 — 다크 표면(#10151e)에서 명도대·채도·색각 분리·대비 검증을 통과한 순서.
// 순서 자체가 색각 안전 장치이므로 임의로 섞지 말 것. 7번째부터는 '기타'로 접는다.
const SERIES = ['#3987e5', '#d95926', '#199e70', '#c98500', '#d55181', '#008300'];
const OTHER_COLOR = '#5a6478'; // --text-muted 계열 중립색
const MAX_SLICES = 6;

const R = 54; // 반지름
const SW = 20; // 도넛 두께
const CIRC = 2 * Math.PI * R;
const GAP = 2; // 조각 사이 표면 간격(px, 원주 기준)

const AllocationDonut = ({ holdings, format }) => {
  const [active, setActive] = useState(null);

  const { slices, total } = useMemo(() => {
    const valued = (holdings || [])
      .map(h => ({ ticker: h.ticker, value: h.currentPrice * h.qty }))
      .filter(h => h.value > 0)
      .sort((a, b) => b.value - a.value);

    const sum = valued.reduce((s, h) => s + h.value, 0);
    if (sum <= 0) return { slices: [], total: 0 };

    // 7종목 이상이면 색을 새로 만들지 않고 꼬리를 '기타'로 접는다.
    const head = valued.slice(0, MAX_SLICES);
    const tail = valued.slice(MAX_SLICES);
    const list = tail.length
      ? [...head, { ticker: '기타', value: tail.reduce((s, h) => s + h.value, 0), tickers: tail.map(t => t.ticker) }]
      : head;

    return {
      total: sum,
      slices: list.map((h, i) => ({
        ...h,
        pct: (h.value / sum) * 100,
        color: h.ticker === '기타' ? OTHER_COLOR : SERIES[i % SERIES.length],
      })),
    };
  }, [holdings]);

  if (slices.length === 0) return null;

  const shown = active != null ? slices[active] : null;
  // 도넛 안쪽 지름은 viewBox 기준 88 — 원화처럼 자릿수가 긴 표기가 넘치지 않게 글자를 줄인다
  const totalText = format(total);
  const totalFont = totalText.length > 12 ? 9 : totalText.length > 9 ? 11 : 13;

  return (
    <div style={{ display: 'flex', gap: 24, alignItems: 'center', flexWrap: 'wrap', justifyContent: 'center' }}>
      <svg
        viewBox="0 0 140 140"
        style={{ width: 180, height: 180, flexShrink: 0 }}
        role="img"
        aria-label={`보유 종목 비중: ${slices.map(s => `${s.ticker} ${s.pct.toFixed(1)}%`).join(', ')}`}
      >
        <g transform="rotate(-90 70 70)">
          {slices.reduce(
            (acc, s, i) => {
              const len = (s.pct / 100) * CIRC;
              // 조각이 간격보다 짧으면 사라지므로 최소 길이를 남긴다
              const dash = Math.max(len - GAP, 0.75);
              acc.nodes.push(
                <circle
                  key={s.ticker}
                  cx="70" cy="70" r={R}
                  fill="none"
                  stroke={s.color}
                  strokeWidth={active === i ? SW + 4 : SW}
                  strokeDasharray={`${dash} ${CIRC - dash}`}
                  strokeDashoffset={-acc.offset}
                  style={{ transition: 'stroke-width 120ms ease', cursor: 'default' }}
                  onMouseEnter={() => setActive(i)}
                  onMouseLeave={() => setActive(null)}
                />,
              );
              acc.offset += len;
              return acc;
            },
            { nodes: [], offset: 0 },
          ).nodes}
        </g>

        {/* 가운데: 기본은 총 평가금액, 조각을 가리키면 그 종목 */}
        <text
          x="70" y={shown ? 64 : 66}
          textAnchor="middle"
          style={{ fill: 'var(--text-muted)', fontSize: 9 }}
        >
          {shown ? shown.ticker : '총 평가금액'}
        </text>
        <text
          x="70" y={shown ? 80 : 82}
          textAnchor="middle"
          style={{ fill: 'var(--text-primary)', fontSize: shown ? 16 : totalFont, fontWeight: 700 }}
        >
          {shown ? `${shown.pct.toFixed(1)}%` : totalText}
        </text>
      </svg>

      {/* 범례 — 색만으로 구분하지 않도록 정확한 비중·금액을 함께 적는다(표 역할 겸함) */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6, minWidth: 190, flex: '1 1 190px' }}>
        {slices.map((s, i) => (
          <div
            key={s.ticker}
            tabIndex={0}
            onMouseEnter={() => setActive(i)}
            onMouseLeave={() => setActive(null)}
            onFocus={() => setActive(i)}
            onBlur={() => setActive(null)}
            title={s.tickers ? s.tickers.join(', ') : s.ticker}
            style={{
              display: 'flex', alignItems: 'center', gap: 8, padding: '5px 8px',
              borderRadius: 'var(--radius-sm)', outline: 'none',
              background: active === i ? 'rgba(255,255,255,0.05)' : 'transparent',
            }}
          >
            <span
              aria-hidden="true"
              style={{ width: 10, height: 10, borderRadius: 3, background: s.color, flexShrink: 0 }}
            />
            <span style={{ fontWeight: 600, fontSize: '0.85rem', minWidth: 52 }}>{s.ticker}</span>
            <span
              style={{
                marginLeft: 'auto', fontSize: '0.85rem',
                fontVariantNumeric: 'tabular-nums', color: 'var(--text-primary)',
              }}
            >
              {s.pct.toFixed(1)}%
            </span>
            <span
              style={{
                fontSize: '0.78rem', color: 'var(--text-secondary)',
                fontVariantNumeric: 'tabular-nums', minWidth: 84, textAlign: 'right',
              }}
            >
              {format(s.value)}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
};

export default AllocationDonut;
