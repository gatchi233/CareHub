import { useEffect, useMemo, useState } from "react";
import { apiService } from "../api";
import PageTabs from "../components/PageTabs";

const AI_TABS = [
  { key: "resident-tools", label: "Resident Tools" },
  { key: "facility-tools", label: "Facility Tools" },
  { key: "response-center", label: "Response Center" }
];

const RESIDENT_TOOL_CARDS = [
  {
    key: "shift-summary",
    title: "Shift Summary",
    description: "Summarize the last 24 hours of observations, MAR activity, and follow-up items."
  },
  {
    key: "detect-trends",
    title: "Detect Trends",
    description: "Review recent vitals and medication events to surface trend signals."
  },
  {
    key: "care-query",
    title: "Care Query",
    description: "Ask a resident-specific or facility-wide question using current care data."
  },
  {
    key: "report-draft",
    title: "Report Draft",
    description: "Draft a structured resident report for staff review and editing."
  },
  {
    key: "medication-explain",
    title: "Medication Explain",
    description: "Translate a medication name and dosage into plain-language staff guidance."
  },
  {
    key: "trend-explain",
    title: "Trend Explain",
    description: "Explain short-window trend movement over a selected 3- or 7-day period."
  }
];

const FACILITY_TOOL_CARDS = [
  {
    key: "shift-handoff",
    title: "Shift Handoff",
    description: "Generate a facility-wide handoff note for the incoming team."
  }
];

const TOOL_LABELS = new Map(
  [...RESIDENT_TOOL_CARDS, ...FACILITY_TOOL_CARDS].map((tool) => [tool.key, tool.title])
);

function AiDashboardPage({ loading, error, residents = [], authRole }) {
  const [activeTab, setActiveTab] = useState("resident-tools");
  const [selectedResidentId, setSelectedResidentId] = useState("");
  const [trendDays, setTrendDays] = useState("7");
  const [careQuery, setCareQuery] = useState("");
  const [medicationName, setMedicationName] = useState("");
  const [dosage, setDosage] = useState("");
  const [responseState, setResponseState] = useState("empty");
  const [response, setResponse] = useState(null);
  const [requestError, setRequestError] = useState("");
  const [activeTool, setActiveTool] = useState("");
  const [copyState, setCopyState] = useState("idle");

  const residentOptions = useMemo(() => {
    return residents
      .map((resident) => {
        const id = String(resident.id || resident.Id || "");
        if (!id) {
          return null;
        }

        const firstName = resident.firstName || resident.residentFName || resident.ResidentFName || "";
        const lastName = resident.lastName || resident.residentLName || resident.ResidentLName || "";
        const room = resident.roomNumber || resident.RoomNumber || resident.room || "";

        return {
          id,
          label: `${`${firstName} ${lastName}`.trim() || "Unnamed resident"}${room ? ` • Room ${room}` : ""}`
        };
      })
      .filter(Boolean);
  }, [residents]);

  useEffect(() => {
    if (residentOptions.length === 0) {
      setSelectedResidentId("");
      return;
    }

    const hasSelectedResident = residentOptions.some((resident) => resident.id === selectedResidentId);
    if (!hasSelectedResident) {
      setSelectedResidentId(residentOptions[0].id);
    }
  }, [residentOptions, selectedResidentId]);

  async function runTool(toolKey, request) {
    setActiveTab("response-center");
    setResponseState("loading");
    setResponse(null);

    try {
      setActiveTool(toolKey);
      setRequestError("");
      const result = await request();
      const title = TOOL_LABELS.get(toolKey) || "AI Response";
      const content = result?.content || result?.Content || "";
      const disclaimer =
        result?.disclaimer ||
        result?.Disclaimer ||
        "AI-generated output is informational only and must be reviewed by qualified staff.";

      setResponse({
        tool: toolKey,
        title,
        content,
        residentName: result?.residentName || result?.ResidentName || "",
        disclaimer
      });
      setResponseState(content ? "ready" : "empty");
    } catch (err) {
      const nextError = err?.message || "AI request failed.";
      setRequestError(nextError);
      setResponseState("error");
    } finally {
      setActiveTool("");
    }
  }

  const canRunShiftSummary = Boolean(selectedResidentId) && !loading && !error;
  const canRunDetectTrends = Boolean(selectedResidentId) && !loading && !error;
  const canRunCareQuery = Boolean(careQuery.trim()) && !loading && !error;
  const canRunMedicationExplain = Boolean(medicationName.trim()) && !loading && !error;
  const canRunReportDraft = Boolean(selectedResidentId) && !loading && !error;
  const canRunTrendExplain = Boolean(selectedResidentId) && !loading && !error;
  const canRunShiftHandoff = !loading && !error;

  if (!["Admin", "Nurse"].includes(authRole || "")) {
    return <article className="card error">AI Dashboard is restricted to Admin and Nurse roles.</article>;
  }

  function renderToolButton(toolKey) {
    if (toolKey === "shift-summary") {
      return (
        <button
          type="button"
          className="ghost-button"
          onClick={() => runTool(toolKey, () => apiService.aiShiftSummary(selectedResidentId))}
          disabled={!canRunShiftSummary || activeTool === toolKey}
        >
          {activeTool === toolKey ? "Running..." : "Run Tool"}
        </button>
      );
    }

    if (toolKey === "detect-trends") {
      return (
        <button
          type="button"
          className="ghost-button"
          onClick={() => runTool(toolKey, () => apiService.aiDetectTrends(selectedResidentId))}
          disabled={!canRunDetectTrends || activeTool === toolKey}
        >
          {activeTool === toolKey ? "Running..." : "Run Tool"}
        </button>
      );
    }

    if (toolKey === "care-query") {
      return (
        <button
          type="button"
          className="ghost-button"
          onClick={() =>
            runTool(toolKey, () => apiService.aiCareQuery(careQuery.trim(), selectedResidentId || null))
          }
          disabled={!canRunCareQuery || activeTool === toolKey}
        >
          {activeTool === toolKey ? "Running..." : "Run Tool"}
        </button>
      );
    }

    if (toolKey === "report-draft") {
      return (
        <button
          type="button"
          className="ghost-button"
          onClick={() => runTool(toolKey, () => apiService.aiReportDraft(selectedResidentId))}
          disabled={!canRunReportDraft || activeTool === toolKey}
        >
          {activeTool === toolKey ? "Running..." : "Run Tool"}
        </button>
      );
    }

    if (toolKey === "medication-explain") {
      return (
        <button
          type="button"
          className="ghost-button"
          onClick={() =>
            runTool(toolKey, () =>
              apiService.aiMedicationExplain(medicationName.trim(), dosage.trim() || null)
            )
          }
          disabled={!canRunMedicationExplain || activeTool === toolKey}
        >
          {activeTool === toolKey ? "Running..." : "Run Tool"}
        </button>
      );
    }

    if (toolKey === "trend-explain") {
      return (
        <button
          type="button"
          className="ghost-button"
          onClick={() =>
            runTool(toolKey, () => apiService.aiTrendExplain(selectedResidentId, Number(trendDays)))
          }
          disabled={!canRunTrendExplain || activeTool === toolKey}
        >
          {activeTool === toolKey ? "Running..." : "Run Tool"}
        </button>
      );
    }

    if (toolKey === "shift-handoff") {
      return (
        <button
          type="button"
          className="ghost-button"
          onClick={() => runTool(toolKey, () => apiService.aiShiftHandoff())}
          disabled={!canRunShiftHandoff || activeTool === toolKey}
        >
          {activeTool === toolKey ? "Running..." : "Run Tool"}
        </button>
      );
    }

    return (
      <button type="button" className="ghost-button" disabled>
        Available Soon
      </button>
    );
  }

  async function handleCopyResponse() {
    if (!response?.content) {
      return;
    }

    try {
      await navigator.clipboard.writeText(response.content);
      setCopyState("copied");
      window.setTimeout(() => setCopyState("idle"), 1800);
    } catch {
      setCopyState("error");
      window.setTimeout(() => setCopyState("idle"), 1800);
    }
  }

  return (
    <section className="page-shell">
      <PageTabs tabs={AI_TABS} activeTab={activeTab} onChange={setActiveTab} />

      {(activeTab === "resident-tools" || activeTab === "facility-tools") && (
        <section className="dashboard-grid">
          <article className="card ai-context-card">
            <h3>Resident Context</h3>
            <p>Select a resident, choose an AI tool, and review each result before acting on it.</p>
            <div className="ai-field-grid">
              <label>
                Resident
                <select value={selectedResidentId} onChange={(event) => setSelectedResidentId(event.target.value)}>
                  <option value="">Choose resident</option>
                  {residentOptions.map((resident) => (
                    <option key={resident.id} value={resident.id}>
                      {resident.label}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Trend Window
                <select value={trendDays} onChange={(event) => setTrendDays(event.target.value)}>
                  <option value="3">3 days</option>
                  <option value="7">7 days</option>
                </select>
              </label>
            </div>
            {loading ? <p className="empty-state">Loading resident context...</p> : null}
            {!loading && !error && residentOptions.length === 0 ? (
              <p className="empty-state">No residents are available for AI resident-specific tools yet.</p>
            ) : null}
            {error ? <p className="auth-error">{error}</p> : null}
            <label>
              Care Query
              <textarea
                rows="4"
                value={careQuery}
                onChange={(event) => setCareQuery(event.target.value)}
                placeholder="Ask about symptoms, refusals, follow-up items, or recent care activity."
              />
            </label>
            <div className="ai-field-grid">
              <label>
                Medication Name
                <input
                  value={medicationName}
                  onChange={(event) => setMedicationName(event.target.value)}
                  placeholder="Medication name"
                />
              </label>
              <label>
                Dosage
                <input
                  value={dosage}
                  onChange={(event) => setDosage(event.target.value)}
                  placeholder="Dosage (optional)"
                />
              </label>
            </div>
            {requestError ? <p className="auth-error">{requestError}</p> : null}
          </article>

          {activeTab === "resident-tools" &&
            RESIDENT_TOOL_CARDS.map((tool) => (
              <article key={tool.key} className="card ai-tool-card">
                <div className="ai-tool-header">
                  <h3>{tool.title}</h3>
                  <span className="row-index">AI</span>
                </div>
                <p>{tool.description}</p>
                <div className="action-row">
                  {renderToolButton(tool.key)}
                </div>
              </article>
            ))}

          {activeTab === "facility-tools" &&
            FACILITY_TOOL_CARDS.map((tool) => (
              <article key={tool.key} className="card ai-tool-card">
                <div className="ai-tool-header">
                  <h3>{tool.title}</h3>
                  <span className="row-index">AI</span>
                </div>
                <p>{tool.description}</p>
                <div className="action-row">{renderToolButton(tool.key)}</div>
              </article>
            ))}
        </section>
      )}

      {activeTab === "response-center" && (
        <section className="dashboard-grid">
          <article className="card ai-response-card">
            <div className="ai-tool-header">
              <h3>Latest AI Response</h3>
              <button
                type="button"
                className="ghost-button"
                onClick={handleCopyResponse}
                disabled={responseState !== "ready" || !response?.content}
              >
                {copyState === "copied" ? "Copied" : copyState === "error" ? "Retry Copy" : "Copy"}
              </button>
            </div>
            {responseState === "loading" ? (
              <p className="empty-state">Generating AI output for {TOOL_LABELS.get(activeTool) || "selected tool"}...</p>
            ) : null}
            {responseState === "error" ? (
              <article className="card error ai-inline-state">
                {requestError || "AI output could not be generated for this request."}
              </article>
            ) : null}
            {responseState === "ready" && response ? (
              <>
                <div className="ai-response-meta">
                  <span className="row-index">AI</span>
                  <strong>{response.title}</strong>
                  <small>{response.residentName || "Facility context"}</small>
                </div>
                <pre className="ai-response-body">{response.content}</pre>
                <p className="topbar-meta">{response.disclaimer}</p>
              </>
            ) : null}
            {responseState === "empty" ? (
              <p className="empty-state">
                No AI response yet. Run one of the resident or facility tools to populate this panel.
              </p>
            ) : null}
          </article>

          <article className="card">
            <h3>How This Fits The Shift</h3>
            <div className="list-row">
              <span>Review resident context first</span>
              <small>Choose resident and timeframe</small>
            </div>
            <div className="list-row">
              <span>Run only one tool at a time</span>
              <small>Keep outputs scoped and easy to verify</small>
            </div>
            <div className="list-row">
              <span>Copy or hand off result</span>
              <small>Use the response center after review</small>
            </div>
          </article>
        </section>
      )}
    </section>
  );
}

export default AiDashboardPage;
