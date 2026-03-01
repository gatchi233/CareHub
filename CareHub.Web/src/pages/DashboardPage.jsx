import StatCard from "../components/StatCard";

function DashboardPage({
  loading,
  error,
  residentsCount,
  medicationsCount,
  observationsCount,
  lowStock,
  lowStockRate,
  occupiedRooms,
  showAllReorders,
  onToggleReorders,
  recentObservations,
  onNavigate
}) {
  return (
    <section className="dashboard-grid">
      {loading && <article className="card">Loading dashboard...</article>}
      {error && <article className="card error">{error}</article>}

      {!loading && !error && (
        <>
          <StatCard title="Total Residents" value={residentsCount} />
          <StatCard title="Total Medications" value={medicationsCount} />
          <StatCard title="Observations Logged" value={observationsCount} />
          <StatCard
            title="Low Stock Alerts"
            value={lowStock.length}
            tone="warning"
            caption={`${lowStockRate}% of medication records`}
          />
          <StatCard title="Occupied Rooms" value={occupiedRooms} />

          <article className="card">
            <h3>Quick Actions</h3>
            <div className="action-row">
              <button type="button" className="ghost-button" onClick={() => onNavigate("Residents")}>
                View Residents
              </button>
              <button type="button" className="ghost-button" onClick={() => onNavigate("Inventory")}>
                View Inventory
              </button>
              <button type="button" className="ghost-button" onClick={() => onNavigate("Observations")}>
                View Observations
              </button>
              <button type="button" className="ghost-button" onClick={() => onNavigate("Staff")}>
                View Staff
              </button>
            </div>
          </article>

          <article className="card">
            <h3>Inventory Reorder List</h3>
            {lowStock.length === 0 && <p>No low-stock items.</p>}
            {lowStock.slice(0, showAllReorders ? lowStock.length : 6).map((m, index) => (
              <div className="list-row" key={m.id}>
                <span className="list-primary">
                  <b className="row-index">{index + 1}</b>
                  {m.medName}
                </span>
                <small>
                  {m.stockQuantity} / {m.reorderLevel}
                </small>
              </div>
            ))}
            {lowStock.length > 6 && (
              <button type="button" className="ghost-button" onClick={onToggleReorders}>
                {showAllReorders ? "Show less" : `Show all (${lowStock.length})`}
              </button>
            )}
          </article>

          <article className="card">
            <h3>Recent Observations</h3>
            {recentObservations.slice(0, 5).map((obs) => (
              <div key={obs.id} className="recent-row">
                <span>{obs._summary}</span>
                <small>{obs._timestamp}</small>
              </div>
            ))}
            {recentObservations.length === 0 && <p>No observations available.</p>}
          </article>
        </>
      )}
    </section>
  );
}

export default DashboardPage;
