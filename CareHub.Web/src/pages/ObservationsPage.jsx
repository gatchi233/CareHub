import { useState } from "react";
import PageTabs from "../components/PageTabs";

const OBSERVATION_TABS = [
  { key: "timeline", label: "Timeline" },
  { key: "list", label: "Observation List" },
  { key: "flags", label: "Flags" },
  { key: "reports", label: "Reports" }
];

function ObservationsPage({
  loading,
  error,
  authRole,
  currentResident,
  displayedObservations,
  pagedObservations,
  currentPage,
  pageSize,
  renderSectionTools,
  renderSectionMeta
}) {
  const [activeTab, setActiveTab] = useState("timeline");

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
    <section className="page-shell">
      <PageTabs tabs={OBSERVATION_TABS} activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === "timeline" && (
        <section className="page-shell">
          {authRole === "Resident" && currentResident && (
            <article className="card">
              <h3>My Care Team</h3>
              <div className="list-row">
                <span>Primary Doctor / Nurse Practitioner</span>
                <small>{currentResident.doctorName || "Not assigned"}</small>
              </div>
              <div className="list-row">
                <span>Care Team Contact</span>
                <small>{currentResident.doctorContact || "Not available"}</small>
              </div>
            </article>
          )}
          <article className="card">
            <h3>Observations Timeline</h3>
            {renderSectionTools([
              { value: "date", label: "Sort: Date" },
              { value: "summary", label: "Sort: Summary" }
            ])}
            {renderSectionMeta(displayedObservations.length, "observations")}
            {displayedObservations.length === 0 && (
              <p className="empty-state">No observations match this view.</p>
            )}
            {pagedObservations.map((obs, index) => (
              <div className="list-row" key={obs.id}>
                <span className="list-primary">
                  <b className="row-index">{(currentPage - 1) * pageSize + index + 1}</b>
                  {obs._summary}
                </span>
                <small>{obs._timestamp}</small>
              </div>
            ))}
          </article>
        </section>
      )}

      {activeTab === "list" && (
        <section className="card">
          <h3>Observation List</h3>
          {renderSectionTools([
            { value: "date", label: "Sort: Date" },
            { value: "summary", label: "Sort: Summary" }
          ])}
          {renderSectionMeta(displayedObservations.length, "observations")}
          {displayedObservations.length === 0 && (
            <p className="empty-state">No observations available.</p>
          )}
          {pagedObservations.map((obs, index) => (
            <article className="observation-card" key={obs.id}>
              <header>
                <strong>
                  {(currentPage - 1) * pageSize + index + 1}. {obs.type || "Observation"}
                </strong>
                <small>{obs._timestamp}</small>
              </header>
              <p>{obs._summary}</p>
              <div className="observation-meta">
                <span>Resident: {obs.residentName || obs.resident || "N/A"}</span>
                <span>Value: {obs.value || "N/A"}</span>
                <span>Recorded By: {obs.recordedBy || "N/A"}</span>
              </div>
            </article>
          ))}
        </section>
      )}

      {activeTab === "flags" && (
        <section className="card">
          <h3>Priority Flags</h3>
          {displayedObservations.length === 0 && <p>No observations available.</p>}
          {displayedObservations.slice(0, 10).map((obs, index) => (
            <div className="list-row" key={obs.id}>
              <span className="list-primary">
                <b className="row-index">{index + 1}</b>
                {obs._summary}
              </span>
              <small>{obs._timestamp}</small>
            </div>
          ))}
        </section>
      )}

      {activeTab === "reports" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Daily Summary</h3>
            <p>Planned: daily shift observation digest by resident and severity.</p>
          </article>
          <article className="card">
            <h3>Trend Signals</h3>
            <p>Planned: symptom trend lines and recurring event clusters.</p>
          </article>
          <article className="card">
            <h3>Export Center</h3>
            <p>Use top-right CSV export for filtered timeline snapshots.</p>
          </article>
        </section>
      )}
    </section>
  );
}

export default ObservationsPage;
