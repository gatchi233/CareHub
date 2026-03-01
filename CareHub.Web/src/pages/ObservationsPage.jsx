function ObservationsPage({
  loading,
  error,
  displayedObservations,
  pagedObservations,
  currentPage,
  pageSize,
  renderSectionTools,
  renderSectionMeta
}) {
  if (loading) {
    return (
      <section className="card">
        <p>Loading observations...</p>
      </section>
    );
  }

  if (error) {
    return (
      <section className="card error">
        <p>{error}</p>
      </section>
    );
  }

  return (
    <section className="card">
      <h3>Observations</h3>
      {renderSectionTools([
        { value: "date", label: "Sort: Date" },
        { value: "summary", label: "Sort: Summary" }
      ])}
      {renderSectionMeta(displayedObservations.length, "observations")}
      {displayedObservations.length === 0 && <p className="empty-state">No observations match this view.</p>}
      {pagedObservations.map((obs, index) => (
        <div className="list-row" key={obs.id}>
          <span className="list-primary">
            <b className="row-index">{(currentPage - 1) * pageSize + index + 1}</b>
            {obs._summary}
          </span>
          <small>{obs._timestamp}</small>
        </div>
      ))}
    </section>
  );
}

export default ObservationsPage;
