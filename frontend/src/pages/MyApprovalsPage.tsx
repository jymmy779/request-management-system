import { Link } from 'react-router'
import { RequestFilters } from '../components/RequestFilters'
import { useDepartments } from '../hooks/useDepartments'
import { useMyApprovals } from '../hooks/useRequestList'
import { formatEstimatedCost } from '../utils/formatCurrency'

export function MyApprovalsPage() {
  const {
    requests,
    currentPage,
    totalCount,
    totalPages,
    isLoading,
    errorMessage,
    retryLoadRequests,
    goToPage,
    applyQuery,
  } = useMyApprovals()

  const {
    departments,
    isLoading: isDepartmentsLoading,
    isUnavailable: departmentsUnavailable,
  } = useDepartments()

  return (
    <div className="adminhub-page-wrapper">
      <div className="adminhub-page-header">
        <div className="page-title-group">
          <div className="title-row">
            <h1>My Approvals</h1>
            {!isLoading && !errorMessage && (
              <span className="count-pill">{totalCount} pending</span>
            )}
          </div>
          <p className="page-subtitle">
            Pending requests that you can review and decide.
          </p>
        </div>
      </div>

      <section
        className="adminhub-card table-panel-card"
        aria-labelledby="approval-list-title"
      >
        <div className="filter-container">
          <RequestFilters
            disabled={isLoading}
            departments={departments}
            isDepartmentsLoading={isDepartmentsLoading}
            departmentsUnavailable={departmentsUnavailable}
            onApply={applyQuery}
          />
        </div>

        {isLoading && (
          <div className="adminhub-state-card" role="status">
            <div className="spinner" />
            <p>Loading approvals...</p>
          </div>
        )}

        {!isLoading && errorMessage && (
          <div className="adminhub-state-card adminhub-error-card" role="alert">
            <p>{errorMessage}</p>
            <button
              type="button"
              className="btn-secondary"
              onClick={retryLoadRequests}
            >
              Try again
            </button>
          </div>
        )}

        {!isLoading && !errorMessage && requests.length === 0 && (
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
              <path d="M9 11l3 3L22 4" />
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
            </svg>
            <h3>No pending approvals</h3>
            <p>You have reviewed every request currently available to you.</p>
          </div>
        )}

        {!isLoading && !errorMessage && requests.length > 0 && (
          <>
            <div className="adminhub-table-wrapper">
              <table className="adminhub-table">
                <thead>
                  <tr>
                    <th scope="col">Request ID &amp; Title</th>
                    <th scope="col">Category</th>
                    <th scope="col">Priority</th>
                    <th scope="col">Quantity</th>
                    <th scope="col">Estimated Cost</th>
                    <th scope="col">Requested By</th>
                    <th scope="col">Created</th>
                    <th scope="col" className="text-right">Action</th>
                  </tr>
                </thead>
                <tbody>
                  {requests.map((request) => (
                    <tr key={request.id}>
                      <td className="cell-primary">
                        <div className="cell-id">
                          PR-{String(request.id).padStart(5, '0')}
                        </div>
                        <Link
                          className="table-title-link"
                          to={`/requests/${request.id}`}
                        >
                          {request.title}
                        </Link>
                        <span className="table-desc-preview">
                          {request.description}
                        </span>
                      </td>
                      <td>
                        <span className={`category-badge category-${request.category.toLowerCase()}`}>
                          {request.category}
                        </span>
                      </td>
                      <td>
                        <span className={`priority-badge priority-${request.priority.toLowerCase()}`}>
                          {request.priority}
                        </span>
                      </td>
                      <td className="cell-muted">{request.quantity}</td>
                      <td className="cell-muted">
                        {formatEstimatedCost(request.estimatedCost)}
                      </td>
                      <td>
                        <div className="table-user-cell">
                          <span className="table-avatar" aria-hidden="true">
                            {request.requesterFullName
                              ? request.requesterFullName.charAt(0).toUpperCase()
                              : 'U'}
                          </span>
                          <div>
                            <strong className="table-user-name">
                              {request.requesterFullName}
                            </strong>
                            <span className="table-user-email">
                              {request.requesterEmail}
                            </span>
                          </div>
                        </div>
                      </td>
                      <td className="cell-muted">
                        {new Date(request.createdAt).toLocaleDateString()}
                      </td>
                      <td className="text-right">
                        <Link
                          className="table-action-btn"
                          to={`/requests/${request.id}`}
                        >
                          Review
                          <svg
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            strokeWidth="2"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            aria-hidden="true"
                          >
                            <polyline points="9 18 15 12 9 6" />
                          </svg>
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <nav className="adminhub-pagination" aria-label="Approval pages">
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
