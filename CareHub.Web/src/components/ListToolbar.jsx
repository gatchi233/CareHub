function ListToolbar({
  searchInputRef,
  query,
  onQueryChange,
  sortKey,
  onSortKeyChange,
  sortOptions,
  sortDirection,
  onToggleSortDirection,
  pageSize,
  onPageSizeChange,
  onReset
}) {
  return (
    <div className="section-tools">
      <input
        ref={searchInputRef}
        type="search"
        value={query}
        placeholder="Filter list... (Ctrl/Cmd+K)"
        onChange={(event) => onQueryChange(event.target.value)}
      />
      {query ? (
        <button type="button" className="ghost-button" onClick={() => onQueryChange("")}>
          Clear
        </button>
      ) : null}
      <select value={sortKey} onChange={(event) => onSortKeyChange(event.target.value)}>
        {sortOptions.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      <button type="button" className="ghost-button" onClick={onToggleSortDirection}>
        {sortDirection === "asc" ? "Asc" : "Desc"}
      </button>
      <select value={pageSize} onChange={(event) => onPageSizeChange(Number(event.target.value))}>
        <option value={8}>8 / page</option>
        <option value={12}>12 / page</option>
        <option value={20}>20 / page</option>
      </select>
      <button type="button" className="ghost-button" onClick={onReset}>
        Reset
      </button>
    </div>
  );
}

export default ListToolbar;
