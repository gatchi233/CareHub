function SectionMetaPager({
  from,
  to,
  totalItems,
  itemLabel,
  currentPage,
  totalPages,
  onPrev,
  onNext,
  onJump
}) {
  return (
    <div className="section-meta">
      <p>
        Showing {from}-{to} of {totalItems} {itemLabel}
      </p>
      <div className="pager">
        <button type="button" className="ghost-button" onClick={onPrev} disabled={currentPage === 1}>
          Prev
        </button>
        <small>
          Page {currentPage} / {totalPages}
        </small>
        {totalPages > 1 ? (
          <select value={currentPage} onChange={(event) => onJump(Number(event.target.value))}>
            {Array.from({ length: totalPages }, (_, idx) => idx + 1).map((page) => (
              <option key={page} value={page}>
                Go to {page}
              </option>
            ))}
          </select>
        ) : null}
        <button type="button" className="ghost-button" onClick={onNext} disabled={currentPage === totalPages}>
          Next
        </button>
      </div>
    </div>
  );
}

export default SectionMetaPager;
