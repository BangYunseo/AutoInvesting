import { useState, useEffect } from 'react';

/**
 * 예상 소요시간 기반 진행바 + 실제 경과 시간을 함께 표시하는 로딩 컴포넌트.
 *
 * 서버가 단계별 진행률을 실시간으로 보내주지 않으므로, 예상 소요시간에 맞춰
 * 진행바를 부드럽게 채우되 95%에서 멈추고(예상 초과 대비), 실제 경과 초를 함께 노출한다.
 * 작업이 끝나면 부모가 이 컴포넌트를 언마운트하여 결과를 표시한다.
 *
 * @param {number} estimatedSeconds 예상 소요 시간(초)
 * @param {string} label 표시할 안내 문구
 */
const ProgressLoader = ({ estimatedSeconds = 10, label = '분석 중입니다...' }) => {
  const [elapsed, setElapsed] = useState(0);

  useEffect(() => {
    const start = Date.now();
    const timer = setInterval(() => {
      setElapsed((Date.now() - start) / 1000);
    }, 200);
    return () => clearInterval(timer);
  }, []);

  // 예상시간 대비 진행률 (최대 95%까지만 — 실제 완료 시 부모가 언마운트)
  const pct = Math.min(95, (elapsed / estimatedSeconds) * 100);
  const overEstimate = elapsed > estimatedSeconds;

  return (
    <div className="progress-loader fade-in">
      <div className="progress-loader__label">
        <span className="progress-loader__spinner" />
        🧠 {label}
      </div>
      <div className="progress-loader__bar">
        <div className="progress-loader__fill" style={{ width: `${pct}%` }} />
      </div>
      <div className="progress-loader__meta">
        {elapsed.toFixed(0)}초 경과 · {overEstimate ? '곧 완료됩니다' : `약 ${estimatedSeconds}초 예상`}
      </div>
    </div>
  );
};

export default ProgressLoader;
