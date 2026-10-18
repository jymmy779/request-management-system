import { useDepartments } from '../hooks/useDepartments'

export function DepartmentsPage() {
  const {
    departments,
    isLoading,
    errorMessage,
    retryLoadDepartments,
  } = useDepartments()

  return (
    <div className="adminhub-page-wrapper">
      <div className="adminhub-page-header">
        <div className="page-title-group">
          <div className="title-row">
            <h1>Departments</h1>
            {!isLoading && !errorMessage && (
              <span className="count-pill">{departments.length} total</span>
            )}
          </div>
          <p className="page-subtitle">
            Reference directory of departments used by employees and requests.
          </p>
        </div>
      </div>

      <section className="adminhub-card table-panel-card">
        {isLoading && (
          <div className="adminhub-state-card" role="status">
            <div className="spinner" />
            <p>Loading departments...</p>
          </div>
        )}

        {!isLoading && errorMessage && (
          <div className="adminhub-state-card adminhub-error-card" role="alert">
            <p>{errorMessage}</p>
            <button
              type="button"
              className="btn-secondary"
              onClick={retryLoadDepartments}
            >
              Try again
            </button>
          </div>
        )}

        {!isLoading && !errorMessage && departments.length === 0 && (
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
              <rect x="3" y="3" width="18" height="18" rx="2" />
              <path d="M8 21V7h8v14" />
              <path d="M9 10h2M13 10h2M9 14h2M13 14h2" />
            </svg>
            <h3>No departments found</h3>
            <p>No department reference data is currently available.</p>
          </div>
        )}

        {!isLoading && !errorMessage && departments.length > 0 && (
          <div className="adminhub-table-wrapper">
            <table className="adminhub-table">
              <thead>
                <tr>
                  <th scope="col">Department</th>
                  <th scope="col">Code</th>
                  <th scope="col">ID</th>
                </tr>
              </thead>
              <tbody>
                {departments.map((department) => (
                  <tr key={department.id}>
                    <td className="cell-primary">
                      <strong className="table-user-name">
                        {department.name}
                      </strong>
                    </td>
                    <td>
                      <span className="attribute-badge">
                        {department.code}
                      </span>
                    </td>
                    <td className="cell-muted">{department.id}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}
