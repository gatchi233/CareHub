import { useEffect, useMemo, useRef, useState } from "react";
import { API_BASE, api } from "./api";
import ListToolbar from "./components/ListToolbar";
import SectionMetaPager from "./components/SectionMetaPager";
import StatCard from "./components/StatCard";

const SECTIONS = ["Dashboard", "Residents", "Medications", "Observations", "Staff"];
const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";
const DEFAULT_PAGE_SIZE = 8;

function App() {
  const [activeSection, setActiveSection] = useState("Dashboard");
  const [sidebarOpen, setSidebarOpen] = useState(false);
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
  const [showAllReorders, setShowAllReorders] = useState(false);
  const searchInputRef = useRef(null);

  function resetSectionView() {
    setQuery("");
    setCurrentPage(1);
    setPageSize(DEFAULT_PAGE_SIZE);
    if (activeSection === "Observations") {
      setSortKey("date");
      setSortDirection("desc");
      return;
    }
    setSortKey("name");
    setSortDirection("asc");
  }

  function formatObservationTime(rawValue) {
    if (!rawValue) {
      return "No timestamp";
    }
    const parsed = Date.parse(rawValue);
    if (Number.isNaN(parsed)) {
      return rawValue;
    }
    return new Date(parsed).toLocaleString();
  }

  function csvCell(value) {
    const text = String(value ?? "");
    return `"${text.replaceAll('"', '""')}"`;
  }

  function downloadCsv(filename, headers, rows) {
    const headerLine = headers.map(csvCell).join(",");
    const rowLines = rows.map((row) => row.map(csvCell).join(","));
    const csv = [headerLine, ...rowLines].join("\n");
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
  }

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
  const occupiedRooms = useMemo(() => {
    const roomSet = new Set(
      residents
        .map((resident) => resident.roomNumber || resident.room)
        .filter((room) => room !== undefined && room !== null && String(room).trim() !== "")
        .map((room) => String(room).trim())
    );
    return roomSet.size;
  }, [residents]);

  useEffect(() => {
    resetSectionView();
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
        return {
          ...med,
          _name: name,
          _stock: stock,
          _reorder: reorder,
          _isLow: stock <= reorder
        };
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
          _timestamp: formatObservationTime(timestamp),
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
  const lowStockRate =
    medications.length === 0 ? 0 : Math.round((lowStock.length / medications.length) * 100);
  const sectionSummary =
    activeSection === "Residents"
      ? `${displayedResidents.length} residents in current view`
      : activeSection === "Medications"
        ? `${displayedMedications.length} medications in current view`
        : activeSection === "Observations"
          ? `${displayedObservations.length} observations in current view`
          : activeSection === "Dashboard"
            ? `${lowStock.length} low stock alerts right now`
            : "Staff directory coming next";
  const canExport =
    activeSection !== "Dashboard" &&
    activeSection !== "Staff" &&
    !loading &&
    !error &&
    activeTotalItems > 0;

  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [currentPage, totalPages]);

  useEffect(() => {
    if (activeSection !== "Dashboard") {
      setShowAllReorders(false);
    }
  }, [activeSection]);

  useEffect(() => {
    function handleFocusShortcut(event) {
      if (activeSection === "Dashboard" || activeSection === "Staff") {
        return;
      }
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        searchInputRef.current?.focus();
      }
      if (event.key === "Escape") {
        setQuery("");
      }
    }

    window.addEventListener("keydown", handleFocusShortcut);
    return () => window.removeEventListener("keydown", handleFocusShortcut);
  }, [activeSection]);

  function handleExport() {
    if (activeSection === "Residents") {
      downloadCsv(
        "residents.csv",
        ["Name", "Room"],
        displayedResidents.map((resident) => [resident._name, resident._room || "N/A"])
      );
      return;
    }
    if (activeSection === "Medications") {
      downloadCsv(
        "medications.csv",
        ["Medication", "Stock", "Reorder Level", "Low Stock"],
        displayedMedications.map((med) => [
          med._name,
          med._stock,
          med._reorder,
          med._isLow ? "Yes" : "No"
        ])
      );
      return;
    }
    if (activeSection === "Observations") {
      downloadCsv(
        "observations.csv",
        ["Summary", "Timestamp"],
        displayedObservations.map((obs) => [obs._summary, obs._timestamp])
      );
    }
  }

  function renderSectionTools(sortOptions) {
    return (
      <ListToolbar
        searchInputRef={searchInputRef}
        query={query}
        onQueryChange={setQuery}
        sortKey={sortKey}
        onSortKeyChange={setSortKey}
        sortOptions={sortOptions}
        sortDirection={sortDirection}
        onToggleSortDirection={() =>
          setSortDirection((current) => (current === "asc" ? "desc" : "asc"))
        }
        pageSize={pageSize}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setCurrentPage(1);
        }}
        onReset={resetSectionView}
      />
    );
  }

  function renderSectionMeta(totalItems, itemLabel) {
    const from = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
    const to = Math.min(currentPage * pageSize, totalItems);

    return (
      <SectionMetaPager
        from={from}
        to={to}
        totalItems={totalItems}
        itemLabel={itemLabel}
        currentPage={currentPage}
        totalPages={totalPages}
        onPrev={() => setCurrentPage((page) => Math.max(1, page - 1))}
        onNext={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
        onJump={setCurrentPage}
      />
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
          {pagedResidents.map((resident, index) => {
            return (
              <div className="list-row" key={resident.id}>
                <span className="list-primary">
                  <b className="row-index">{(currentPage - 1) * pageSize + index + 1}</b>
                  {resident._name}
                </span>
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
          {pagedMedications.map((med, index) => (
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

    return (
      <section className="card">
        <h3>Staff</h3>
        <p>This section is planned next. Dashboard data is already live.</p>
      </section>
    );
  }

  async function handleCopySummary() {
    try {
      await navigator.clipboard.writeText(`${activeSection}: ${sectionSummary}`);
    } catch {
      // Ignore clipboard errors in restricted environments.
    }
  }

  return (
    <div className="app-shell">
      <aside className={`sidebar ${sidebarOpen ? "open" : ""}`}>
        <h1>CareHub</h1>
        <p>Retirement medication operations</p>
        <nav>
          {SECTIONS.map((section) => (
            <button
              key={section}
              className={activeSection === section ? "active" : ""}
              onClick={() => {
                setActiveSection(section);
                setSidebarOpen(false);
              }}
            >
              <span>{section}</span>
              {section === "Residents" && <small>{residents.length}</small>}
              {section === "Medications" && <small>{medications.length}</small>}
              {section === "Observations" && <small>{observations.length}</small>}
              {section === "Dashboard" && lowStock.length > 0 && (
                <small className="alert-pill">{lowStock.length}</small>
              )}
            </button>
          ))}
        </nav>
      </aside>
      {sidebarOpen && <button className="backdrop" onClick={() => setSidebarOpen(false)} />}

      <main className="content">
        <header className="topbar">
          <div className="topbar-title">
            <button
              type="button"
              className="menu-toggle"
              onClick={() => setSidebarOpen((isOpen) => !isOpen)}
            >
              Menu
            </button>
            <h2>{activeSection}</h2>
          </div>
          <p className="topbar-meta">{sectionSummary}</p>
          <button type="button" className="secondary-button" onClick={handleCopySummary}>
            Copy Summary
          </button>
          {canExport && (
            <button type="button" className="secondary-button" onClick={handleExport}>
              Export CSV
            </button>
          )}
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
                <StatCard title="Total Residents" value={residents.length} />
                <StatCard title="Total Medications" value={medications.length} />
                <StatCard title="Observations Logged" value={observations.length} />
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
                    <button type="button" className="ghost-button" onClick={() => setActiveSection("Residents")}>
                      View Residents
                    </button>
                    <button type="button" className="ghost-button" onClick={() => setActiveSection("Medications")}>
                      View Medications
                    </button>
                    <button
                      type="button"
                      className="ghost-button"
                      onClick={() => setActiveSection("Observations")}
                    >
                      View Observations
                    </button>
                  </div>
                </article>
                <article className="card">
                  <h3>Inventory Reorder List</h3>
                  {lowStock.length === 0 && <p>No low-stock items.</p>}
                  {lowStock
                    .slice(0, showAllReorders ? lowStock.length : 6)
                    .map((m, index) => (
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
                    <button
                      type="button"
                      className="ghost-button"
                      onClick={() => setShowAllReorders((isOpen) => !isOpen)}
                    >
                      {showAllReorders ? "Show less" : `Show all (${lowStock.length})`}
                    </button>
                  )}
                </article>
                <article className="card">
                  <h3>Recent Observations</h3>
                  {displayedObservations.slice(0, 5).map((obs) => (
                    <div key={obs.id} className="recent-row">
                      <span>{obs._summary}</span>
                      <small>{obs._timestamp}</small>
                    </div>
                  ))}
                  {displayedObservations.length === 0 && <p>No observations available.</p>}
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
