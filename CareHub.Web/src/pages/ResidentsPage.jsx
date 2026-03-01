function ResidentsPage({
  loading,
  error,
  displayedResidents,
  pagedResidents,
  currentPage,
  pageSize,
  renderSectionTools,
  renderSectionMeta
}) {
  if (loading) {
    return (
      <section className="card">
        <p>Loading residents...</p>
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
      <h3>Residents</h3>
      {renderSectionTools([
        { value: "name", label: "Sort: Name" },
        { value: "room", label: "Sort: Room" }
      ])}
      {renderSectionMeta(displayedResidents.length, "residents")}
      {displayedResidents.length === 0 && <p className="empty-state">No residents match this view.</p>}
      {pagedResidents.map((resident, index) => (
        <div className="list-row" key={resident.id}>
          <span className="list-primary">
            <b className="row-index">{(currentPage - 1) * pageSize + index + 1}</b>
            {resident._name}
          </span>
          <small>Room {resident._room || "N/A"}</small>
        </div>
      ))}
    </section>
  );
}

export default ResidentsPage;
