import { useState } from 'react'
import type { FormEvent } from 'react'
import type {
  RequestCategory,
  RequestQuery,
  RequestStatus,
} from '../api/requests'

type SortOption =
  | 'createdAt:desc'
  | 'createdAt:asc'
  | 'title:asc'
  | 'title:desc'
  | 'status:asc'
  | 'status:desc'

type RequestFiltersProps = {
  disabled: boolean
  onApply: (query: RequestQuery) => void
}

const statusTabs: Array<{ label: string; value: RequestStatus | '' }> = [
  { label: 'All', value: '' },
  { label: 'Pending', value: 'Pending' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Rejected', value: 'Rejected' },
  { label: 'Cancelled', value: 'Cancelled' },
]

export function RequestFilters({ disabled, onApply }: RequestFiltersProps) {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<RequestStatus | ''>('')
  const [category, setCategory] = useState<RequestCategory | ''>('')
  const [sortOption, setSortOption] = useState<SortOption>('createdAt:desc')

  function triggerApply(
    nextSearch: string,
    nextStatus: RequestStatus | '',
    nextCategory: RequestCategory | '',
    nextSort: SortOption,
  ) {
    const [sortBy, sortDirection] = nextSort.split(':') as [
      NonNullable<RequestQuery['sortBy']>,
      NonNullable<RequestQuery['sortDirection']>,
    ]

    onApply({
      search: nextSearch.trim() || undefined,
      status: nextStatus || undefined,
      category: nextCategory || undefined,
      sortBy,
      sortDirection,
    })
  }

  function handleFormSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    triggerApply(search, status, category, sortOption)
  }

  function handleStatusTabClick(selectedStatus: RequestStatus | '') {
    setStatus(selectedStatus)
    triggerApply(search, selectedStatus, category, sortOption)
  }

  function clearSearch() {
    setSearch('')
    triggerApply('', status, category, sortOption)
  }

  function clearAllFilters() {
    setSearch('')
    setStatus('')
    setCategory('')
    setSortOption('createdAt:desc')
    onApply({ sortBy: 'createdAt', sortDirection: 'desc' })
  }

  const hasActiveFilters = Boolean(search.trim() || status || category)

  return (
    <div className="filters-wrapper">
      {/* 1. Quick Status Tabs (AdminHub Segmented Control) */}
      <div className="status-tabs-bar" role="tablist" aria-label="Status filters">
        {statusTabs.map((tab) => {
          const isActive = status === tab.value
          return (
            <button
              key={tab.label}
              type="button"
              role="tab"
              aria-selected={isActive}
              className={`status-tab-btn ${isActive ? 'tab-active' : ''} ${
                tab.value ? `tab-${tab.value.toLowerCase()}` : ''
              }`}
              onClick={() => handleStatusTabClick(tab.value)}
              disabled={disabled}
            >
              <span>{tab.label}</span>
            </button>
          )
        })}
      </div>

      {/* 2. Controls Row: Search Input + Category Select + Sort Select + Actions */}
      <form className="filters-controls-row" onSubmit={handleFormSubmit}>
        {/* Search input with icons */}
        <div className="filter-search-box">
          <svg className="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search title, description..."
            disabled={disabled}
            aria-label="Search requests"
          />
          {search && (
            <button
              type="button"
              className="clear-search-btn"
              onClick={clearSearch}
              title="Clear search"
              aria-label="Clear search"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          )}
        </div>

        {/* Category Select */}
        <div className="filter-select-group">
          <label htmlFor="filter-category-select">Category</label>
          <select
            id="filter-category-select"
            value={category}
            onChange={(e) => {
              const val = e.target.value as RequestCategory | ''
              setCategory(val)
              triggerApply(search, status, val, sortOption)
            }}
            disabled={disabled}
          >
            <option value="">All Categories</option>
            <option value="Equipment">Equipment</option>
            <option value="Software">Software</option>
            <option value="Other">Other</option>
          </select>
        </div>

        {/* Sort Select */}
        <div className="filter-select-group">
          <label htmlFor="filter-sort-select">Sort by</label>
          <select
            id="filter-sort-select"
            value={sortOption}
            onChange={(e) => {
              const val = e.target.value as SortOption
              setSortOption(val)
              triggerApply(search, status, category, val)
            }}
            disabled={disabled}
          >
            <option value="createdAt:desc">Newest first</option>
            <option value="createdAt:asc">Oldest first</option>
            <option value="title:asc">Title A–Z</option>
            <option value="title:desc">Title Z–A</option>
            <option value="status:asc">Status A–Z</option>
            <option value="status:desc">Status Z–A</option>
          </select>
        </div>

        {/* Filter Action Buttons */}
        <div className="filter-buttons-group">
          <button type="submit" className="btn-primary filter-submit-btn" disabled={disabled}>
            Search
          </button>
          {hasActiveFilters && (
            <button
              type="button"
              className="btn-secondary filter-reset-btn"
              onClick={clearAllFilters}
              disabled={disabled}
            >
              Reset
            </button>
          )}
        </div>
      </form>

      {/* 3. Active Filter Chips */}
      {hasActiveFilters && (
        <div className="active-chips-bar" aria-label="Active filters">
          <span className="chips-label">Active filters:</span>
          {status && (
            <span className="filter-chip">
              Status: <strong>{status}</strong>
              <button
                type="button"
                onClick={() => handleStatusTabClick('')}
                aria-label={`Remove status filter ${status}`}
              >
                ✕
              </button>
            </span>
          )}
          {category && (
            <span className="filter-chip">
              Category: <strong>{category}</strong>
              <button
                type="button"
                onClick={() => {
                  setCategory('')
                  triggerApply(search, status, '', sortOption)
                }}
                aria-label={`Remove category filter ${category}`}
              >
                ✕
              </button>
            </span>
          )}
          {search.trim() && (
            <span className="filter-chip">
              Query: &ldquo;{search.trim()}&rdquo;
              <button
                type="button"
                onClick={clearSearch}
                aria-label="Remove search query filter"
              >
                ✕
              </button>
            </span>
          )}
        </div>
      )}
    </div>
  )
}
