import { useMemo, useState } from "react";
import PageTabs from "../components/PageTabs";

const RESIDENT_TABS = [
  { key: "directory", label: "Directory" },
  { key: "rooms", label: "Room Map" },
  { key: "care", label: "Care Plans" }
];

function ResidentsPage({
  loading,
  error,
  authSession,
  authRole,
  canEditResidents,
  onSaveResident,
  currentResident,
  displayedResidents,
  pagedResidents,
  currentPage,
  pageSize,
  renderSectionTools,
  renderSectionMeta
}) {
  const [activeTab, setActiveTab] = useState("directory");
  const [editingResidentId, setEditingResidentId] = useState("");
  const [saveError, setSaveError] = useState("");
  const [saving, setSaving] = useState(false);
  const [editForm, setEditForm] = useState({
    residentFName: "",
    residentLName: "",
    roomNumber: "",
    dateOfBirth: "",
    doctorName: "",
    doctorContact: "",
    emergencyContactName1: "",
    emergencyContactPhone1: "",
    emergencyRelationship1: ""
  });

  const roomGroups = useMemo(() => {
    const map = new Map();
    displayedResidents.forEach((resident) => {
      const room = resident._room || "Unassigned";
      if (!map.has(room)) {
        map.set(room, []);
      }
      map.get(room).push(resident);
    });
    return Array.from(map.entries()).sort((a, b) => String(a[0]).localeCompare(String(b[0])));
  }, [displayedResidents]);

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

  if (authRole === "Resident") {
    const resident = currentResident;
    if (!resident) {
      return (
        <section className="card">
          <h3>My Resident Profile</h3>
          <p className="empty-state">
            No resident profile is linked to this account yet. Linked resident ID:{" "}
            {authSession?.residentId || "not provided"}.
          </p>
        </section>
      );
    }

    const fullName =
      resident.fullName ||
      `${resident.ResidentFName || ""} ${resident.ResidentLName || ""}`.trim() ||
      `${resident.firstName || resident.residentFName || ""} ${resident.lastName || resident.residentLName || ""}`.trim() ||
      "Unnamed resident";

    return (
      <section className="card">
        <h3>My Resident Profile</h3>
        <div className="list-row">
          <span>Name</span>
          <small>{fullName}</small>
        </div>
        <div className="list-row">
          <span>Room</span>
          <small>{resident.roomNumber || resident.room || "N/A"}</small>
        </div>
        <div className="list-row">
          <span>Date of Birth</span>
          <small>{resident.dateOfBirth || "N/A"}</small>
        </div>
        <div className="list-row">
          <span>Primary Contact</span>
          <small>{resident.emergencyContactName1 || "N/A"}</small>
        </div>
        <div className="list-row">
          <span>Contact Phone</span>
          <small>{resident.emergencyContactPhone1 || "N/A"}</small>
        </div>
      </section>
    );
  }

  const editingResident = displayedResidents.find(
    (resident) => (resident.id || resident.Id) === editingResidentId
  );

  function startEditing(resident) {
    setSaveError("");
    setEditingResidentId(resident.id || resident.Id);
    setEditForm({
      residentFName: resident.residentFName || resident.ResidentFName || "",
      residentLName: resident.residentLName || resident.ResidentLName || "",
      roomNumber: resident.roomNumber || resident.RoomNumber || "",
      dateOfBirth: resident.dateOfBirth || resident.DateOfBirth || "",
      doctorName: resident.doctorName || resident.DoctorName || "",
      doctorContact: resident.doctorContact || resident.DoctorContact || "",
      emergencyContactName1:
        resident.emergencyContactName1 || resident.EmergencyContactName1 || "",
      emergencyContactPhone1:
        resident.emergencyContactPhone1 || resident.EmergencyContactPhone1 || "",
      emergencyRelationship1:
        resident.emergencyRelationship1 || resident.EmergencyRelationship1 || ""
    });
  }

  async function handleSave(event) {
    event.preventDefault();
    if (!editingResident || !onSaveResident) {
      return;
    }

    setSaving(true);
    setSaveError("");

    try {
      const payload = {
        ...editingResident,
        id: editingResident.id || editingResident.Id,
        residentFName: editForm.residentFName,
        residentLName: editForm.residentLName,
        roomNumber: editForm.roomNumber,
        dateOfBirth: editForm.dateOfBirth,
        doctorName: editForm.doctorName,
        doctorContact: editForm.doctorContact,
        emergencyContactName1: editForm.emergencyContactName1,
        emergencyContactPhone1: editForm.emergencyContactPhone1,
        emergencyRelationship1: editForm.emergencyRelationship1
      };

      await onSaveResident(payload);
      setEditingResidentId("");
    } catch (err) {
      setSaveError(err?.message || "Failed to save resident.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="page-shell">
      <PageTabs tabs={RESIDENT_TABS} activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === "directory" && (
        <section className="card">
          <h3>Residents Directory</h3>
          {renderSectionTools([
            { value: "name", label: "Sort: Name" },
            { value: "room", label: "Sort: Room" }
          ])}
          {renderSectionMeta(displayedResidents.length, "residents")}
          {displayedResidents.length === 0 && <p className="empty-state">No residents match this view.</p>}
          {pagedResidents.map((resident, index) => (
            <div className="list-row" key={resident.id || resident.Id}>
              <span className="list-primary">
                <b className="row-index">{(currentPage - 1) * pageSize + index + 1}</b>
                {resident._name}
              </span>
              <span className="list-row-actions">
                <small>Room {resident._room || "N/A"}</small>
                {canEditResidents && (
                  <button type="button" className="ghost-button" onClick={() => startEditing(resident)}>
                    Edit
                  </button>
                )}
              </span>
            </div>
          ))}
          {canEditResidents && editingResident && (
            <form className="resident-edit-form" onSubmit={handleSave}>
              <h4>Edit Resident</h4>
              <label>
                First Name
                <input
                  value={editForm.residentFName}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, residentFName: event.target.value }))
                  }
                  required
                />
              </label>
              <label>
                Last Name
                <input
                  value={editForm.residentLName}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, residentLName: event.target.value }))
                  }
                  required
                />
              </label>
              <label>
                Room Number
                <input
                  value={editForm.roomNumber}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, roomNumber: event.target.value }))
                  }
                />
              </label>
              <label>
                Date of Birth
                <input
                  value={editForm.dateOfBirth}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, dateOfBirth: event.target.value }))
                  }
                />
              </label>
              <label>
                Doctor / NP
                <input
                  value={editForm.doctorName}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, doctorName: event.target.value }))
                  }
                />
              </label>
              <label>
                Doctor Contact
                <input
                  value={editForm.doctorContact}
                  onChange={(event) =>
                    setEditForm((current) => ({ ...current, doctorContact: event.target.value }))
                  }
                />
              </label>
              <label>
                Primary Contact
                <input
                  value={editForm.emergencyContactName1}
                  onChange={(event) =>
                    setEditForm((current) => ({
                      ...current,
                      emergencyContactName1: event.target.value
                    }))
                  }
                />
              </label>
              <label>
                Contact Phone
                <input
                  value={editForm.emergencyContactPhone1}
                  onChange={(event) =>
                    setEditForm((current) => ({
                      ...current,
                      emergencyContactPhone1: event.target.value
                    }))
                  }
                />
              </label>
              <label>
                Relationship
                <input
                  value={editForm.emergencyRelationship1}
                  onChange={(event) =>
                    setEditForm((current) => ({
                      ...current,
                      emergencyRelationship1: event.target.value
                    }))
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
                  onClick={() => setEditingResidentId("")}
                  disabled={saving}
                >
                  Cancel
                </button>
              </div>
            </form>
          )}
        </section>
      )}

      {activeTab === "rooms" && (
        <section className="card">
          <h3>Room Map</h3>
          {roomGroups.length === 0 && <p>No room assignments available.</p>}
          <section className="room-plan-grid">
            {roomGroups.map(([room, residents]) => (
              <article key={room} className="room-card">
                <header>
                  <strong>Room {room}</strong>
                  <small>{residents.length} resident(s)</small>
                </header>
                <div className="room-occupants">
                  {residents.map((resident) => (
                    <span key={resident.id || resident.Id} className="room-occupant-pill">
                      {resident._name}
                    </span>
                  ))}
                </div>
              </article>
            ))}
          </section>
        </section>
      )}

      {activeTab === "care" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Care Plan Status</h3>
            <p>Planned: care plan review cycles and completion flags.</p>
          </article>
          <article className="card">
            <h3>Escalations</h3>
            <p>Planned: medication adherence and vitals escalation tracking.</p>
          </article>
          <article className="card">
            <h3>Family Notes</h3>
            <p>Planned: contact updates and communication history.</p>
          </article>
        </section>
      )}
    </section>
  );
}

export default ResidentsPage;
