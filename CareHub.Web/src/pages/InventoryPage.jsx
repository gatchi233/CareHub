function InventoryPage({
  loading,
  error,
  displayedInventory,
  pagedInventory,
  currentPage,
  pageSize,
  renderSectionTools,
  renderSectionMeta
}) {
  if (loading) {
    return (
      <section className="card">
        <p>Loading inventory...</p>
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
      <h3>Inventory</h3>
      {renderSectionTools([
        { value: "name", label: "Sort: Name" },
        { value: "stock", label: "Sort: Stock" }
      ])}
      {renderSectionMeta(displayedInventory.length, "inventory items")}
      {displayedInventory.length === 0 && <p className="empty-state">No inventory items match this view.</p>}
      {pagedInventory.map((med, index) => (
        <div className={`list-row ${med._isLow ? "row-alert" : ""}`} key={med.id}>
          <span className="list-primary">
            <b className="row-index">{(currentPage - 1) * pageSize + index + 1}</b>
            {med._name}
          </span>
          <small>
            {med._stock} in stock
            {med.reorderLevel != null ? ` | Reorder at ${med._reorder}` : ""}
            {med._isLow ? " | LOW" : ""}
          </small>
        </div>
      ))}
    </section>
  );
}

export default InventoryPage;
