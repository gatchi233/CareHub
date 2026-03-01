import { useEffect, useMemo, useRef, useState } from "react";
import { API_BASE, api } from "./api";
import ListToolbar from "./components/ListToolbar";
import SectionMetaPager from "./components/SectionMetaPager";
import DashboardPage from "./pages/DashboardPage";
import InventoryPage from "./pages/InventoryPage";
import ObservationsPage from "./pages/ObservationsPage";
import ResidentsPage from "./pages/ResidentsPage";
import StaffPage from "./pages/StaffPage";

const SECTIONS = [
  { key: "Dashboard", label: "Dashboard" },
  { key: "Residents", label: "Residents" },
  { key: "Inventory", label: "Inventory" },
  { key: "Observations", label: "Observations" },
  { key: "Staff", label: "Staff" }
];
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
      const unassigned = !m.residentId || m.residentId === EMPTY_GUID;
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

  const displayedInventory = useMemo(() => {
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

  const pagedInventory = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return displayedInventory.slice(start, start + pageSize);
  }, [displayedInventory, currentPage, pageSize]);

  const pagedObservations = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return displayedObservations.slice(start, start + pageSize);
  }, [displayedObservations, currentPage, pageSize]);

  const activeTotalItems =
    activeSection === "Residents"
      ? displayedResidents.length
      : activeSection === "Inventory"
        ? displayedInventory.length
        : activeSection === "Observations"
          ? displayedObservations.length
          : 0;

  const totalPages = Math.max(1, Math.ceil(activeTotalItems / pageSize));
  const lowStockRate =
    medications.length === 0 ? 0 : Math.round((lowStock.length / medications.length) * 100);
  const sectionSummary =
    activeSection === "Residents"
      ? `${displayedResidents.length} residents in current view`
      : activeSection === "Inventory"
        ? `${displayedInventory.length} inventory items in current view`
        : activeSection === "Observations"
          ? `${displayedObservations.length} observations in current view`
          : activeSection === "Dashboard"
            ? `${lowStock.length} low stock alerts right now`
            : "Staff directory and planning workspace";
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
    if (activeSection === "Inventory") {
      downloadCsv(
        "inventory.csv",
        ["Medication", "Stock", "Reorder Level", "Low Stock"],
        displayedInventory.map((med) => [
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

  async function handleCopySummary() {
    try {
      await navigator.clipboard.writeText(`${activeSection}: ${sectionSummary}`);
    } catch {
      // Ignore clipboard errors in restricted environments.
    }
  }

  function renderActivePage() {
    if (activeSection === "Dashboard") {
      return (
        <DashboardPage
          loading={loading}
          error={error}
          residentsCount={residents.length}
          medicationsCount={medications.length}
          observationsCount={observations.length}
          lowStock={lowStock}
          lowStockRate={lowStockRate}
          occupiedRooms={occupiedRooms}
          showAllReorders={showAllReorders}
          onToggleReorders={() => setShowAllReorders((isOpen) => !isOpen)}
          recentObservations={displayedObservations}
          onNavigate={setActiveSection}
        />
      );
    }

    if (activeSection === "Residents") {
      return (
        <ResidentsPage
          loading={loading}
          error={error}
          displayedResidents={displayedResidents}
          pagedResidents={pagedResidents}
          currentPage={currentPage}
          pageSize={pageSize}
          renderSectionTools={renderSectionTools}
          renderSectionMeta={renderSectionMeta}
        />
      );
    }

    if (activeSection === "Inventory") {
      return (
        <InventoryPage
          loading={loading}
          error={error}
          displayedInventory={displayedInventory}
          pagedInventory={pagedInventory}
          currentPage={currentPage}
          pageSize={pageSize}
          renderSectionTools={renderSectionTools}
          renderSectionMeta={renderSectionMeta}
        />
      );
    }

    if (activeSection === "Observations") {
      return (
        <ObservationsPage
          loading={loading}
          error={error}
          displayedObservations={displayedObservations}
          pagedObservations={pagedObservations}
          currentPage={currentPage}
          pageSize={pageSize}
          renderSectionTools={renderSectionTools}
          renderSectionMeta={renderSectionMeta}
        />
      );
    }

    return <StaffPage loading={loading} error={error} />;
  }

  return (
    <div className="app-shell">
      <aside className={`sidebar ${sidebarOpen ? "open" : ""}`}>
        <h1>CareHub</h1>
        <p>Retirement medication operations</p>
        <nav>
          {SECTIONS.map((section) => (
            <button
              key={section.key}
              className={activeSection === section.key ? "active" : ""}
              onClick={() => {
                setActiveSection(section.key);
                setSidebarOpen(false);
              }}
            >
              <span>{section.label}</span>
              {section.key === "Residents" && <small>{residents.length}</small>}
              {section.key === "Inventory" && <small>{medications.length}</small>}
              {section.key === "Observations" && <small>{observations.length}</small>}
              {section.key === "Dashboard" && lowStock.length > 0 && (
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
        {renderActivePage()}
      </main>
    </div>
  );
}

export default App;
