function StatCard({ title, value, tone = "", caption = "" }) {
  return (
    <article className={`card metric ${tone}`.trim()}>
      <h3>{title}</h3>
      <strong>{value}</strong>
      {caption ? <p className="metric-caption">{caption}</p> : null}
    </article>
  );
}

export default StatCard;
