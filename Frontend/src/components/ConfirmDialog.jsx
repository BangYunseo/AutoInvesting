import { createPortal } from "react-dom";

/**
 * 범용 확인 모달 — 브라우저 기본 confirm() 대신 앱 테마를 그대로 쓴다.
 * spec: { icon, tone: 'danger' | 'primary', title, body, confirmLabel }
 * 되돌릴 수 없는 동작(삭제·실주문)은 tone='danger'로 띄운다.
 *
 * document.body로 포털 렌더하는 이유: .modal-overlay는 position:fixed로 화면 중앙에 오도록
 * 돼 있는데, 호출부가 .card 안이면 화면이 아니라 그 카드 기준으로 잡혀 페이지 아래쪽에 떴다.
 * .card의 backdrop-filter가 none이 아니면 그 요소가 fixed 자손의 컨테이닝 블록이 되기 때문이다
 * (transform·filter·perspective·will-change도 같은 부작용을 낸다). 호출부마다 JSX 위치를
 * 옮기는 대신, 모든 호출부가 지나가는 이 컴포넌트에서 한 번 벗어난다.
 */
const ConfirmDialog = ({ spec, busy, onCancel, onConfirm }) =>
  createPortal(
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
    </div>,
    document.body,
  );

export default ConfirmDialog;
