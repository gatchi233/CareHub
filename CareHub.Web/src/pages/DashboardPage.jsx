import { useState } from "react";
import PageTabs from "../components/PageTabs";
import StatCard from "../components/StatCard";

const DASHBOARD_TABS = [
  { key: "overview", label: "Overview" },
  { key: "operations", label: "Operations" },
  { key: "activity", label: "Activity" }
];

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
  onNavigate,
  availableSections = []
}) {
  const [activeTab, setActiveTab] = useState("overview");

  if (loading) {
    return <article className="card">Loading dashboard...</article>;
  }
  if (error) {
    return <article className="card error">{error}</article>;
  }

  return (
    <section className="page-shell">
      <PageTabs tabs={DASHBOARD_TABS} activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === "overview" && (
        <section className="dashboard-grid">
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
        </section>
      )}

      {activeTab === "operations" && (
        <section className="dashboard-grid">
          <article className="card">
            <h3>Quick Actions</h3>
            <div className="action-row">
              {availableSections.includes("Residents") && (
                <button type="button" className="ghost-button" onClick={() => onNavigate("Residents")}>
                  View Residents
                </button>
              )}
              {availableSections.includes("Inventory") && (
                <button type="button" className="ghost-button" onClick={() => onNavigate("Inventory")}>
                  View Inventory
                </button>
              )}
              {availableSections.includes("Observations") && (
                <button type="button" className="ghost-button" onClick={() => onNavigate("Observations")}>
                  View Observations
                </button>
              )}
              {availableSections.includes("Staff") && (
                <button type="button" className="ghost-button" onClick={() => onNavigate("Staff")}>
                  View Staff
                </button>
              )}
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
        </section>
      )}

      {activeTab === "activity" && (
        <section className="dashboard-grid">
          <article className="card">
            <h3>Recent Observations</h3>
            {recentObservations.slice(0, 10).map((obs) => (
              <div key={obs.id} className="recent-row">
                <span>{obs._summary}</span>
                <small>{obs._timestamp}</small>
              </div>
            ))}
            {recentObservations.length === 0 && <p>No observations available.</p>}
          </article>
        </section>
      )}
    </section>
  );
}

export default DashboardPage;
