import { useState } from "react";
import PageTabs from "../components/PageTabs";

const STAFF_TABS = [
  { key: "directory", label: "Directory" },
  { key: "shifts", label: "Shifts" },
  { key: "assignments", label: "Assignments" }
];

function StaffPage({
  loading,
  error,
  authRole,
  currentResident,
  canEditStaff,
  onSaveStaff,
  staffMembers = []
}) {
  const [activeTab, setActiveTab] = useState("directory");
  const [editingUsername, setEditingUsername] = useState("");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [editForm, setEditForm] = useState({
    displayName: "",
    role: "",
    password: ""
  });

  if (loading) {
    return (
      <section className="card">
        <p>Loading staff workspace...</p>
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
    return (
      <section className="card">
        <h3>My Assigned Care Team</h3>
        <div className="list-row">
          <span>Primary Doctor / Nurse Practitioner</span>
          <small>{currentResident?.doctorName || "Not assigned"}</small>
        </div>
        <div className="list-row">
          <span>Contact</span>
          <small>{currentResident?.doctorContact || "Not available"}</small>
        </div>
      </section>
    );
  }

  const editingMember = staffMembers.find((member) => member.username === editingUsername);

  function startEdit(member) {
    setSaveError("");
    setEditingUsername(member.username);
    setEditForm({
      displayName: member.displayName || "",
      role: member.role || "",
      password: ""
    });
  }

  async function handleSave(event) {
    event.preventDefault();
    if (!editingMember || !onSaveStaff) {
      return;
    }

    setSaving(true);
    setSaveError("");

    try {
      const payload = {
        displayName: editForm.displayName,
        role: editForm.role,
        password: editForm.password
      };
      await onSaveStaff(editingMember.username, payload);
      setEditingUsername("");
    } catch (err) {
      setSaveError(err?.message || "Failed to save staff.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="page-shell">
      <PageTabs tabs={STAFF_TABS} activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === "directory" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Staff Directory</h3>
            {staffMembers.length === 0 && <p>No staff accounts available.</p>}
            {staffMembers.map((member) => (
              <div className="list-row" key={member.username}>
                <span>{member.displayName || member.username}</span>
                <span className="list-row-actions">
                  <small>{member.role}</small>
                  {canEditStaff && (
                    <button type="button" className="ghost-button" onClick={() => startEdit(member)}>
                      Edit
                    </button>
                  )}
                </span>
              </div>
            ))}
            {canEditStaff && editingMember && (
              <form className="resident-edit-form" onSubmit={handleSave}>
                <h4>Edit Staff Account</h4>
                <label>
                  Display Name
                  <input
                    value={editForm.displayName}
                    onChange={(event) =>
                      setEditForm((current) => ({ ...current, displayName: event.target.value }))
                    }
                  />
                </label>
                <label>
                  Role
                  <select
                    value={editForm.role}
                    onChange={(event) =>
                      setEditForm((current) => ({ ...current, role: event.target.value }))
                    }
                  >
                    <option value="Admin">Admin</option>
                    <option value="Staff">Staff</option>
                    <option value="Observer">Observer</option>
                  </select>
                </label>
                <label>
                  New Password (optional)
                  <input
                    type="password"
                    value={editForm.password}
                    onChange={(event) =>
                      setEditForm((current) => ({ ...current, password: event.target.value }))
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
                    onClick={() => setEditingUsername("")}
                    disabled={saving}
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}
          </article>
        </section>
      )}

      {activeTab === "shifts" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Morning Shift</h3>
            <p>Planned: med rounds, vitals checks, and breakfast observation tasks.</p>
          </article>
          <article className="card">
            <h3>Evening Shift</h3>
            <p>Planned: administration checks, follow-ups, and end-of-day logs.</p>
          </article>
          <article className="card">
            <h3>Night Shift</h3>
            <p>Planned: incident handling and overnight monitoring coverage.</p>
          </article>
        </section>
      )}

      {activeTab === "assignments" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Medication Tasks</h3>
            <p>Planned: who is assigned to each medication route and pass window.</p>
          </article>
          <article className="card">
            <h3>Follow-Up Tasks</h3>
            <p>Planned: flagged observations and required follow-through owners.</p>
          </article>
          <article className="card">
            <h3>Escalation Tasks</h3>
            <p>Planned: unresolved alerts and SLA tracking by role.</p>
          </article>
        </section>
      )}
    </section>
  );
}

export default StaffPage;
