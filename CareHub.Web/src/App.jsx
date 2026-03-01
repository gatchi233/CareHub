import { useEffect, useMemo, useState } from "react";
import { API_BASE, api } from "./api";

const SECTIONS = ["Dashboard", "Residents", "Medications", "Observations", "Staff"];
const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";
const DEFAULT_PAGE_SIZE = 8;

function App() {
  const [activeSection, setActiveSection] = useState("Dashboard");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [residents, setResidents] = useState([]);
  const [medications, setMedications] = useState([]);
  const [observations, setObservations] = useState([]);
  const [query, setQuery] = useState("");
  const [sortKey, setSortKey] = useState("name");
  const [sortDirection, setSortDirection] = useState("asc");
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => {
    async function loadDashboard() {
      try {
        setLoading(true);
        setError("");
        const [resData, medData, obsData] = await Promise.all([
          api.get("/residents"),
          api.get("/medications"),
          api.get("/observations")
        ]);
        setResidents(Array.isArray(resData) ? resData : []);
        setMedications(Array.isArray(medData) ? medData : []);
        setObservations(Array.isArray(obsData) ? obsData : []);
      } catch (err) {
        setError(`Failed to load API data from ${API_BASE}. ${err.message}`);
      } finally {
        setLoading(false);
      }
    }

    loadDashboard();
  }, []);

  const lowStock = useMemo(() => {
    return medications.filter((m) => {
      const unassigned =
        !m.residentId || m.residentId === EMPTY_GUID;
      return unassigned && Number(m.stockQuantity) <= Number(m.reorderLevel);
    });
  }, [medications]);

  useEffect(() => {
    setQuery("");
    setCurrentPage(1);
    setPageSize(DEFAULT_PAGE_SIZE);
    if (activeSection === "Observations") {
      setSortKey("date");
      setSortDirection("desc");
      return;
    }
    if (activeSection === "Medications") {
      setSortKey("name");
      setSortDirection("asc");
      return;
    }
    if (activeSection === "Residents") {
      setSortKey("name");
      setSortDirection("asc");
    }
  }, [activeSection]);

  const displayedResidents = useMemo(() => {
    const filtered = residents
      .map((resident) => {
        const name =
          resident.fullName ||
          `${resident.firstName || ""} ${resident.lastName || ""}`.trim() ||
          resident.name ||
          "Unnamed resident";
        const room = resident.roomNumber || resident.room || "";
        return { ...resident, _name: name, _room: room };
      })
      .filter((resident) => {
        const term = query.trim().toLowerCase();
        if (!term) {
          return true;
        }
        return (
          resident._name.toLowerCase().includes(term) ||
          String(resident._room).toLowerCase().includes(term)
        );
      });

    filtered.sort((a, b) => {
      if (sortKey === "room") {
        const roomCompare = Number(a._room || 0) - Number(b._room || 0);
        if (roomCompare !== 0) {
          return roomCompare;
        }
      }
      return a._name.localeCompare(b._name);
    });

    if (sortDirection === "desc") {
      filtered.reverse();
    }

    return filtered;
  }, [residents, query, sortKey, sortDirection]);

  const displayedMedications = useMemo(() => {
    const filtered = medications
      .map((med) => {
        const name = med.medName || med.name || "Unnamed medication";
        const stock = Number(med.stockQuantity ?? 0);
        const reorder = Number(med.reorderLevel ?? 0);
        return { ...med, _name: name, _stock: stock, _reorder: reorder };
      })
      .filter((med) => {
        const term = query.trim().toLowerCase();
        if (!term) {
          return true;
        }
        return med._name.toLowerCase().includes(term);
      });

    filtered.sort((a, b) => {
      if (sortKey === "stock") {
        const stockCompare = a._stock - b._stock;
        if (stockCompare !== 0) {
          return stockCompare;
        }
      }
      return a._name.localeCompare(b._name);
    });

    if (sortDirection === "desc") {
      filtered.reverse();
    }

    return filtered;
  }, [medications, query, sortKey, sortDirection]);

  const displayedObservations = useMemo(() => {
    const filtered = observations
      .map((obs) => {
        const summary = obs.summary || obs.note || "Observation entry";
        const timestamp = obs.observedAt || obs.createdAt || "";
        const dateValue = Date.parse(timestamp);
        return {
          ...obs,
          _summary: summary,
          _timestamp: timestamp || "No timestamp",
          _timeValue: Number.isNaN(dateValue) ? 0 : dateValue
        };
      })
      .filter((obs) => {
        const term = query.trim().toLowerCase();
        if (!term) {
          return true;
        }
        return obs._summary.toLowerCase().includes(term);
      });

    filtered.sort((a, b) => {
      if (sortKey === "summary") {
        return a._summary.localeCompare(b._summary);
      }
      return a._timeValue - b._timeValue;
    });

    if (sortDirection === "desc") {
      filtered.reverse();
    }

    return filtered;
  }, [observations, query, sortKey, sortDirection]);

  const pagedResidents = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return displayedResidents.slice(start, start + pageSize);
  }, [displayedResidents, currentPage, pageSize]);

  const pagedMedications = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return displayedMedications.slice(start, start + pageSize);
  }, [displayedMedications, currentPage, pageSize]);

  const pagedObservations = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return displayedObservations.slice(start, start + pageSize);
  }, [displayedObservations, currentPage, pageSize]);

  const activeTotalItems =
    activeSection === "Residents"
      ? displayedResidents.length
      : activeSection === "Medications"
        ? displayedMedications.length
        : activeSection === "Observations"
          ? displayedObservations.length
          : 0;

  const totalPages = Math.max(1, Math.ceil(activeTotalItems / pageSize));

  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [currentPage, totalPages]);

  function renderSectionTools(sortOptions) {
    return (
      <div className="section-tools">
        <input
          type="search"
          value={query}
          placeholder="Filter list..."
          onChange={(event) => setQuery(event.target.value)}
        />
        <select value={sortKey} onChange={(event) => setSortKey(event.target.value)}>
          {sortOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        <button
          type="button"
          className="ghost-button"
          onClick={() =>
            setSortDirection((current) => (current === "asc" ? "desc" : "asc"))
          }
        >
          {sortDirection === "asc" ? "Asc" : "Desc"}
        </button>
        <select
          value={pageSize}
          onChange={(event) => {
            setPageSize(Number(event.target.value));
            setCurrentPage(1);
          }}
        >
          <option value={8}>8 / page</option>
          <option value={12}>12 / page</option>
          <option value={20}>20 / page</option>
        </select>
      </div>
    );
  }

  function renderSectionMeta(totalItems, itemLabel) {
    const from = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
    const to = Math.min(currentPage * pageSize, totalItems);

    return (
      <div className="section-meta">
        <p>
          Showing {from}-{to} of {totalItems} {itemLabel}
        </p>
        <div className="pager">
          <button
            type="button"
            className="ghost-button"
            onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
            disabled={currentPage === 1}
          >
            Prev
          </button>
          <small>
            Page {currentPage} / {totalPages}
          </small>
          <button
            type="button"
            className="ghost-button"
            onClick={() =>
              setCurrentPage((page) => Math.min(totalPages, page + 1))
            }
            disabled={currentPage === totalPages}
          >
            Next
          </button>
        </div>
      </div>
    );
  }

  function renderSectionCard() {
    if (activeSection === "Residents") {
      return (
        <section className="card">
          <h3>Residents</h3>
          {renderSectionTools([
            { value: "name", label: "Sort: Name" },
            { value: "room", label: "Sort: Room" }
          ])}
          {renderSectionMeta(displayedResidents.length, "residents")}
          {displayedResidents.length === 0 && (
            <p className="empty-state">No residents match this view.</p>
          )}
          {pagedResidents.map((resident) => {
            return (
              <div className="list-row" key={resident.id}>
                <span>{resident._name}</span>
                <small>Room {resident._room || "N/A"}</small>
              </div>
            );
          })}
        </section>
      );
    }

    if (activeSection === "Medications") {
      return (
        <section className="card">
          <h3>Medications</h3>
          {renderSectionTools([
            { value: "name", label: "Sort: Name" },
            { value: "stock", label: "Sort: Stock" }
          ])}
          {renderSectionMeta(displayedMedications.length, "medications")}
          {displayedMedications.length === 0 && (
            <p className="empty-state">No medications match this view.</p>
          )}
          {pagedMedications.map((med) => (
            <div className="list-row" key={med.id}>
              <span>{med._name}</span>
              <small>
                {med._stock} in stock
                {med.reorderLevel != null ? ` | Reorder at ${med._reorder}` : ""}
              </small>
            </div>
          ))}
        </section>
      );
    }

    if (activeSection === "Observations") {
      return (
        <section className="card">
          <h3>Observations</h3>
          {renderSectionTools([
            { value: "date", label: "Sort: Date" },
            { value: "summary", label: "Sort: Summary" }
          ])}
          {renderSectionMeta(displayedObservations.length, "observations")}
          {displayedObservations.length === 0 && (
            <p className="empty-state">No observations match this view.</p>
          )}
          {pagedObservations.map((obs) => (
            <div className="list-row" key={obs.id}>
              <span>{obs._summary}</span>
              <small>{obs._timestamp}</small>
            </div>
          ))}
        </section>
      );
    }

    return (
      <section className="card">
        <h3>Staff</h3>
        <p>This section is planned next. Dashboard data is already live.</p>
      </section>
    );
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <h1>CareHub</h1>
        <p>Retirement medication operations</p>
        <nav>
          {SECTIONS.map((section) => (
            <button
              key={section}
              className={activeSection === section ? "active" : ""}
              onClick={() => setActiveSection(section)}
            >
              {section}
            </button>
          ))}
        </nav>
      </aside>

      <main className="content">
        <header className="topbar">
          <h2>{activeSection}</h2>
          <button onClick={() => window.location.reload()}>Refresh</button>
        </header>

        {activeSection !== "Dashboard" && !loading && !error && renderSectionCard()}
        {activeSection !== "Dashboard" && loading && (
          <section className="card">
            <p>Loading {activeSection.toLowerCase()}...</p>
          </section>
        )}
        {activeSection !== "Dashboard" && error && (
          <section className="card error">
            <p>{error}</p>
          </section>
        )}

        {activeSection === "Dashboard" && (
          <section className="dashboard-grid">
            {loading && <article className="card">Loading dashboard...</article>}
            {error && <article className="card error">{error}</article>}

            {!loading && !error && (
              <>
                <article className="card metric">
                  <h3>Total Residents</h3>
                  <strong>{residents.length}</strong>
                </article>
                <article className="card metric">
                  <h3>Total Medications</h3>
                  <strong>{medications.length}</strong>
                </article>
                <article className="card metric">
                  <h3>Observations Logged</h3>
                  <strong>{observations.length}</strong>
                </article>
                <article className="card metric warning">
                  <h3>Low Stock Alerts</h3>
                  <strong>{lowStock.length}</strong>
                </article>
                <article className="card">
                  <h3>Inventory Reorder List</h3>
                  {lowStock.length === 0 && <p>No low-stock items.</p>}
                  {lowStock.slice(0, 6).map((m) => (
                    <div className="list-row" key={m.id}>
                      <span>{m.medName}</span>
                      <small>
                        {m.stockQuantity} / {m.reorderLevel}
                      </small>
                    </div>
                  ))}
                </article>
              </>
            )}
          </section>
        )}
      </main>
    </div>
  );
}

export default App;
