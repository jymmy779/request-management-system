import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router'
import type { UserQuery, UserRole } from '../api/users'
import { useDepartments } from '../hooks/useDepartments'
import { useUsers } from '../hooks/useUsers'

type SortOption =
  | 'fullName:asc'
  | 'fullName:desc'
  | 'email:asc'
  | 'email:desc'

const roleTabs: Array<{ label: string; value: UserRole | '' }> = [
  { label: 'All Roles', value: '' },
  { label: 'Employees', value: 'Employee' },
  { label: 'Managers', value: 'Manager' },
  { label: 'Admins', value: 'Admin' },
]

export function UsersPage() {
  const {
    users,
    currentPage,
    totalCount,
    totalPages,
    isLoading,
    errorMessage,
    retryLoadUsers,
    goToPage,
    applyQuery,
  } = useUsers()

  const {
    departments,
    isLoading: isDepartmentsLoading,
    isUnavailable: departmentsUnavailable,
  } = useDepartments()

  const [search, setSearch] = useState('')
  const [role, setRole] = useState<UserRole | ''>('')
  const [departmentId, setDepartmentId] = useState('')
  const [sortOption, setSortOption] = useState<SortOption>('fullName:asc')

  function triggerApply(
    nextSearch: string,
    nextRole: UserRole | '',
    nextDepartmentId: string,
    nextSort: SortOption,
  ) {
    const [sortBy, sortDirection] = nextSort.split(':') as [
      'fullName' | 'email',
      'asc' | 'desc',
    ]

    const query: UserQuery = {
      sortBy,
      sortDirection,
    }

    const trimmedSearch = nextSearch.trim()
    if (trimmedSearch) {
      query.search = trimmedSearch
    }

    if (nextRole) {
      query.role = nextRole
    }

    if (nextDepartmentId) {
      query.departmentId = Number(nextDepartmentId)
    }

    applyQuery(query)
  }

  function handleFormSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    triggerApply(search, role, departmentId, sortOption)
  }

  function handleRoleTabClick(selectedRole: UserRole | '') {
    setRole(selectedRole)
    triggerApply(search, selectedRole, departmentId, sortOption)
  }

  function clearSearch() {
    setSearch('')
    triggerApply('', role, departmentId, sortOption)
  }

  function clearAllFilters() {
    setSearch('')
    setRole('')
    setDepartmentId('')
    setSortOption('fullName:asc')
    applyQuery({ sortBy: 'fullName', sortDirection: 'asc' })
  }

  const hasActiveFilters = Boolean(search.trim() || role || departmentId)
  const activeDepartmentName = departmentId
    ? departments.find((d) => String(d.id) === departmentId)?.name
    : null

  return (
    <div className="adminhub-page-wrapper">
      <div className="adminhub-page-header">
        <div className="page-title-group">
          <div className="title-row">
            <h1>Users</h1>
            {!isLoading && !errorMessage && (
              <span className="count-pill">{totalCount} total</span>
            )}
          </div>
          <p className="page-subtitle">
            View employee identities, roles, and department assignments.
          </p>
        </div>
        <div className="page-header-actions">
          <Link to="/users/new" className="btn-primary">
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.5"
              strokeLinecap="round"
              strokeLinejoin="round"
              style={{ width: 15, height: 15 }}
              aria-hidden="true"
            >
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            <span>New user</span>
          </Link>
        </div>
      </div>

      <section className="adminhub-card table-panel-card">
        <div className="filter-container">
          <div className="filters-wrapper">
            {/* 1. Quick Role Tabs */}
            <div className="status-tabs-bar" role="tablist" aria-label="Role filters">
              {roleTabs.map((tab) => {
                const isActive = role === tab.value
                return (
                  <button
                    key={tab.label}
                    type="button"
                    role="tab"
                    aria-selected={isActive}
                    className={`status-tab-btn ${isActive ? 'tab-active' : ''}`}
                    onClick={() => handleRoleTabClick(tab.value)}
                    disabled={isLoading}
                  >
                    <span>{tab.label}</span>
                  </button>
                )
              })}
            </div>

            {/* 2. Controls Row */}
            <form className="filters-controls-row" onSubmit={handleFormSubmit}>
              <div className="filter-search-box">
                <svg className="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                  <circle cx="11" cy="11" r="8" />
                  <line x1="21" y1="21" x2="16.65" y2="16.65" />
                </svg>
                <input
                  type="text"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Search name or email..."
                  disabled={isLoading}
                  maxLength={100}
                  aria-label="Search users"
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

              {/* Department Dropdown */}
              <div className="filter-select-group">
                <label htmlFor="user-filter-department">Department</label>
                <select
                  id="user-filter-department"
                  value={departmentId}
                  disabled={isLoading || isDepartmentsLoading || departmentsUnavailable}
                  onChange={(e) => {
                    const nextId = e.target.value
                    setDepartmentId(nextId)
                    triggerApply(search, role, nextId, sortOption)
                  }}
                >
                  <option value="">
                    {departmentsUnavailable ? 'Unavailable' : 'All departments'}
                  </option>
                  {departments.map((department) => (
                    <option key={department.id} value={department.id}>
                      {department.name}
                    </option>
                  ))}
                </select>
              </div>

              {/* Sort by Dropdown */}
              <div className="filter-select-group">
                <label htmlFor="user-filter-sort">Sort by</label>
                <select
                  id="user-filter-sort"
                  value={sortOption}
                  disabled={isLoading}
                  onChange={(e) => {
                    const nextSort = e.target.value as SortOption
                    setSortOption(nextSort)
                    triggerApply(search, role, departmentId, nextSort)
                  }}
                >
                  <option value="fullName:asc">Full name (A–Z)</option>
                  <option value="fullName:desc">Full name (Z–A)</option>
                  <option value="email:asc">Email (A–Z)</option>
                  <option value="email:desc">Email (Z–A)</option>
                </select>
              </div>

              {/* Actions */}
              <div className="filter-buttons-group">
                <button type="submit" className="btn-primary filter-submit-btn" disabled={isLoading}>
                  Search
                </button>
                {hasActiveFilters && (
                  <button
                    type="button"
                    className="btn-secondary filter-reset-btn"
                    onClick={clearAllFilters}
                    disabled={isLoading}
                  >
                    Reset
                  </button>
                )}
              </div>
            </form>

            {/* 3. Active Chips Bar */}
            {hasActiveFilters && (
              <div className="active-chips-bar" aria-label="Active filters">
                <span className="chips-label">Active filters:</span>
                {role && (
                  <span className="filter-chip">
                    Role: <strong>{role}</strong>
                    <button
                      type="button"
                      onClick={() => handleRoleTabClick('')}
                      aria-label={`Remove role filter ${role}`}
                    >
                      ✕
                    </button>
                  </span>
                )}
                {departmentId && activeDepartmentName && (
                  <span className="filter-chip">
                    Department: <strong>{activeDepartmentName}</strong>
                    <button
                      type="button"
                      onClick={() => {
                        setDepartmentId('')
                        triggerApply(search, role, '', sortOption)
                      }}
                      aria-label="Remove department filter"
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
        </div>

        {isLoading && (
          <div className="adminhub-state-card" role="status">
            <div className="spinner" />
            <p>Loading users...</p>
          </div>
        )}

        {!isLoading && errorMessage && (
          <div className="adminhub-state-card adminhub-error-card" role="alert">
            <p>{errorMessage}</p>
            <button
              type="button"
              className="btn-secondary"
              onClick={retryLoadUsers}
            >
              Try again
            </button>
          </div>
        )}

        {!isLoading && !errorMessage && users.length === 0 && (
          <div className="adminhub-empty-state">
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
              <path d="M16 3.13a4 4 0 0 1 0 7.75" />
            </svg>
            <h3>No users found</h3>
            <p>No users matched the current filters.</p>
          </div>
        )}

        {!isLoading && !errorMessage && users.length > 0 && (
          <>
            <div className="adminhub-table-wrapper">
              <table className="adminhub-table">
                <thead>
                  <tr>
                    <th scope="col">User</th>
                    <th scope="col">Role</th>
                    <th scope="col">Department</th>
                    <th scope="col">User ID</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.id}>
                      <td>
                        <div className="table-user-cell">
                          <span className="table-avatar" aria-hidden="true">
                            {user.fullName
                              ? user.fullName.charAt(0).toUpperCase()
                              : 'U'}
                          </span>
                          <div>
                            <strong className="table-user-name">
                              {user.fullName || 'Unnamed user'}
                            </strong>
                            <span className="table-user-email">{user.email}</span>
                          </div>
                        </div>
                      </td>
                      <td>
                        <span className={`role-badge role-${(user.role || 'employee').toLowerCase()}`}>
                          {user.role || 'Unassigned'}
                        </span>
                      </td>
                      <td className="cell-muted">
                        {user.departmentName ?? 'Unassigned'}
                      </td>
                      <td className="cell-muted">{user.id}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <nav className="adminhub-pagination" aria-label="User pages">
              <button
                type="button"
                className="btn-pagination"
                onClick={() => goToPage(currentPage - 1)}
                disabled={currentPage === 1 || isLoading}
              >
                ← Previous
              </button>
              <span className="pagination-info">
                Page <strong>{currentPage}</strong> of{' '}
                <strong>{totalPages}</strong>
              </span>
              <button
                type="button"
                className="btn-pagination"
                onClick={() => goToPage(currentPage + 1)}
                disabled={currentPage === totalPages || isLoading}
              >
                Next →
              </button>
            </nav>
          </>
        )}
      </section>
    </div>
  )
}
