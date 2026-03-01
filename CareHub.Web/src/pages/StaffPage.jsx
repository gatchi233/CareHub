function StaffPage({ loading, error }) {
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

  return (
    <section className="staff-grid">
      <article className="card">
        <h3>Staff Directory</h3>
        <p>Staff roster page is now separated and ready for API integration.</p>
      </article>
      <article className="card">
        <h3>Shift Board</h3>
        <p>Planned: upcoming shifts, role coverage, and handoff notes.</p>
      </article>
      <article className="card">
        <h3>Task Assignments</h3>
        <p>Planned: medication rounds, follow-ups, and escalation ownership.</p>
      </article>
    </section>
  );
}

export default StaffPage;
