import { useState } from "react";
import PageTabs from "../components/PageTabs";

const INVENTORY_TABS = [
  { key: "catalog", label: "Catalog" },
  { key: "reorder", label: "Reorder Queue" },
  { key: "audit", label: "Audit" }
];

function InventoryPage({
  loading,
  error,
  canEditInventory,
  onSaveMedication,
  displayedInventory,
  pagedInventory,
  lowStock,
  currentPage,
  pageSize,
  renderSectionTools,
  renderSectionMeta
}) {
  const [activeTab, setActiveTab] = useState("catalog");
  const [editingMedicationId, setEditingMedicationId] = useState("");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [editForm, setEditForm] = useState({
    medName: "",
    dosage: "",
    usage: "",
    stockQuantity: 0,
    reorderLevel: 0
  });

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

  const editingMedication = displayedInventory.find(
    (med) => (med.id || med.Id) === editingMedicationId
  );

  function startEditingMedication(med) {
    setSaveError("");
    setEditingMedicationId(med.id || med.Id);
    setEditForm({
      medName: med.medName || med.MedName || "",
      dosage: med.dosage || med.Dosage || "",
      usage: med.usage || med.Usage || "",
      stockQuantity: Number(med.stockQuantity ?? med.StockQuantity ?? 0),
      reorderLevel: Number(med.reorderLevel ?? med.ReorderLevel ?? 0)
    });
  }

  async function handleSave(event) {
    event.preventDefault();
    if (!editingMedication || !onSaveMedication) {
      return;
    }

    setSaving(true);
    setSaveError("");
    try {
      const payload = {
        ...editingMedication,
        id: editingMedication.id || editingMedication.Id,
        medName: editForm.medName,
        dosage: editForm.dosage,
        usage: editForm.usage,
        stockQuantity: Number(editForm.stockQuantity),
        reorderLevel: Number(editForm.reorderLevel)
      };
      await onSaveMedication(payload);
      setEditingMedicationId("");
    } catch (err) {
      setSaveError(err?.message || "Failed to save medication.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="page-shell">
      <PageTabs tabs={INVENTORY_TABS} activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === "catalog" && (
        <section className="card">
          <h3>Inventory Catalog</h3>
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
              <span className="list-row-actions">
                <small>
                  {med._stock} in stock
                  {med.reorderLevel != null ? ` | Reorder at ${med._reorder}` : ""}
                  {med._isLow ? " | LOW" : ""}
                </small>
                {canEditInventory && (
                  <button type="button" className="ghost-button" onClick={() => startEditingMedication(med)}>
                    Edit
                  </button>
                )}
              </span>
            </div>
          ))}
          {canEditInventory && editingMedication && (
            <form className="resident-edit-form" onSubmit={handleSave}>
              <h4>Edit Inventory Item</h4>
              <label>
                Medication
                <input
                  value={editForm.medName}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, medName: event.target.value }))
                  }
                  required
                />
              </label>
              <label>
                Dosage
                <input
                  value={editForm.dosage}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, dosage: event.target.value }))
                  }
                />
              </label>
              <label>
                Usage
                <input
                  value={editForm.usage}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, usage: event.target.value }))
                  }
                />
              </label>
              <label>
                Stock Quantity
                <input
                  type="number"
                  value={editForm.stockQuantity}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, stockQuantity: event.target.value }))
                  }
                />
              </label>
              <label>
                Reorder Level
                <input
                  type="number"
                  value={editForm.reorderLevel}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, reorderLevel: event.target.value }))
                  }
                />
              </label>
              {saveError ? <p className="auth-error">{saveError}</p> : null}
              <div className="action-row">
                <button type="submit" className="ghost-button" disabled={saving}>
                  {saving ? "Saving..." : "Save Changes"}
                </button>
                <button
                  type="button"
                  className="ghost-button"
                  onClick={() => setEditingMedicationId("")}
                  disabled={saving}
                >
                  Cancel
                </button>
              </div>
            </form>
          )}
        </section>
      )}

      {activeTab === "reorder" && (
        <section className="card">
          <h3>Reorder Queue</h3>
          {lowStock.length === 0 && <p>No low-stock items pending reorder.</p>}
          {lowStock.map((med, index) => (
            <div className="list-row row-alert" key={med.id}>
              <span className="list-primary">
                <b className="row-index">{index + 1}</b>
                {med.medName || med.name || "Unnamed medication"}
              </span>
              <small>
                {med.stockQuantity ?? 0} / {med.reorderLevel ?? 0}
              </small>
            </div>
          ))}
        </section>
      )}

      {activeTab === "audit" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Stock Movement</h3>
            <p>Planned: inbound/outbound transaction log and adjustment reasons.</p>
          </article>
          <article className="card">
            <h3>Expiry Tracking</h3>
            <p>Planned: expiry windows, batch visibility, and early-disposal flags.</p>
          </article>
          <article className="card">
            <h3>Compliance Checks</h3>
            <p>Planned: reconciliation reminders and discrepancy reporting.</p>
          </article>
        </section>
      )}
    </section>
  );
}

export default InventoryPage;
