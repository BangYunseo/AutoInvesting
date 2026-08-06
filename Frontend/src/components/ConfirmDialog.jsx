/**
 * 범용 확인 모달 — 브라우저 기본 confirm() 대신 앱 테마를 그대로 쓴다.
 * spec: { icon, tone: 'danger' | 'primary', title, body, confirmLabel }
 * 되돌릴 수 없는 동작(삭제·실주문)은 tone='danger'로 띄운다.
 */
const ConfirmDialog = ({ spec, busy, onCancel, onConfirm }) => (
  <div
    className="modal-overlay"
    onClick={() => {
      if (!busy) onCancel();
    }}
  >
    <div
      className="modal-content"
      onClick={(ev) => ev.stopPropagation()}
      style={{ maxWidth: 440 }}
    >
      <h3
        style={{
          marginBottom: 14,
          borderBottom: "1px solid var(--border-primary)",
          paddingBottom: 12,
          color:
            spec.tone === "danger" ? "var(--loss-red)" : "var(--text-primary)",
        }}
      >
        {spec.icon} {spec.title}
      </h3>

      <p
        style={{
          fontSize: "0.9rem",
          lineHeight: 1.7,
          color: "var(--text-secondary)",
          wordBreak: "keep-all",
        }}
      >
        {spec.body}
      </p>

      <div style={{ display: "flex", gap: 10, marginTop: 18 }}>
        <button
          className="btn btn--outline"
          style={{ flex: 1 }}
          onClick={onCancel}
          disabled={busy}
        >
          취소
        </button>
        <button
          className={`btn ${spec.tone === "danger" ? "btn--danger" : "btn--primary"}`}
          style={{ flex: 1 }}
          onClick={onConfirm}
          disabled={busy}
        >
          {busy ? "처리 중..." : spec.confirmLabel}
        </button>
      </div>
    </div>
  </div>
);

export default ConfirmDialog;
