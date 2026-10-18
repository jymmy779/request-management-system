import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import type { CurrentUser } from '../api/auth'
import {
  approveRequest,
  cancelRequest,
  completeRequest,
  getRequestById,
  getRequestHistory,
  rejectRequest,
} from '../api/requests'
import type { RequestHistoryItem, RequestItem } from '../api/requests'
import { useToast } from '../contexts/toast'
import { formatEstimatedCost } from '../utils/formatCurrency'

type RequestDetailContentProps = {
  requestId: number
  currentUser: CurrentUser
}

function RequestDetailContent({
  requestId,
  currentUser,
}: RequestDetailContentProps) {
  const [request, setRequest] = useState<RequestItem | null>(null)
  const [history, setHistory] = useState<RequestHistoryItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const { showToast } = useToast()

  // Decision form state
  const [decision, setDecision] = useState<'Approve' | 'Reject'>('Approve')
  const [comments, setComments] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    let isActive = true

    Promise.all([
      getRequestById(requestId),
      getRequestHistory(requestId).catch(() => [] as RequestHistoryItem[]),
    ])
      .then(([reqResult, historyResult]) => {
        if (isActive) {
          setRequest(reqResult)
          setHistory(historyResult)
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setErrorMessage(
            error instanceof Error
              ? error.message
              : 'Unable to load the request.',
          )
        }
      })
      .finally(() => {
        if (isActive) {
          setIsLoading(false)
        }
      })

    return () => {
      isActive = false
    }
  }, [requestId, reloadKey])

  function reloadAll() {
    setIsLoading(true)
    setErrorMessage('')
    setReloadKey((k) => k + 1)
  }

  async function handleDecisionSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!request) return

    if (decision === 'Reject' && comments.trim().length < 5) {
      showToast(
        'A rejection reason of at least 5 characters is required.',
        'error',
      )
      return
    }

    setIsSubmitting(true)
    try {
      if (decision === 'Approve') {
        const updated = await approveRequest(request.id)
        setRequest(updated)
        showToast('Request successfully approved.')
      } else {
        const updated = await rejectRequest(request.id, comments.trim())
        setRequest(updated)
        showToast('Request has been rejected.')
      }
      // Refresh history
      const freshHistory = await getRequestHistory(request.id)
      setHistory(freshHistory)
      setComments('')
    } catch (err) {
      showToast(
        err instanceof Error ? err.message : 'Action failed.',
        'error',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleComplete() {
    if (!request) return
    setIsSubmitting(true)

    try {
      const updated = await completeRequest(request.id)
      setRequest(updated)
      showToast('Request marked as completed.')
      const freshHistory = await getRequestHistory(request.id)
      setHistory(freshHistory)
    } catch (err) {
      showToast(
        err instanceof Error
          ? err.message
          : 'Failed to complete request.',
        'error',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleCancel() {
    if (!request) return
    if (!window.confirm('Are you sure you want to cancel this request?')) return

    setIsSubmitting(true)

    try {
      const updated = await cancelRequest(request.id)
      setRequest(updated)
      showToast('Request has been cancelled.')
      const freshHistory = await getRequestHistory(request.id)
      setHistory(freshHistory)
    } catch (err) {
      showToast(
        err instanceof Error
          ? err.message
          : 'Failed to cancel request.',
        'error',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return (
      <div className="adminhub-page-wrapper">
        <div className="adminhub-state-card" role="status">
          <div className="spinner" />
          <p>Loading request details & workflow...</p>
        </div>
      </div>
    )
  }

  if (errorMessage || !request) {
    return (
      <div className="adminhub-page-wrapper">
        <Link className="adminhub-back-btn" to="/requests">
          ← Back to requests
        </Link>
        <div className="adminhub-state-card adminhub-error-card" role="alert">
          <p>{errorMessage || 'Request not found.'}</p>
          <button type="button" className="btn-secondary" onClick={reloadAll}>
            Try again
          </button>
        </div>
      </div>
    )
  }

  const isOwner = currentUser.id === request.requesterId
  const isManagerOrAdmin = currentUser.role === 'Manager' || currentUser.role === 'Admin'
  const canApprove = isManagerOrAdmin && request.status === 'Pending' && !isOwner
  const canSelfApprovalWarning = isManagerOrAdmin && request.status === 'Pending' && isOwner
  const canComplete = isManagerOrAdmin && request.status === 'Approved'
  const canCancel = isOwner && request.status === 'Pending'

  // Calculate workflow progress
  let progressPercentage = 33
  let currentStepName = 'Initiated'
  if (request.status === 'Pending') {
    progressPercentage = 50
    currentStepName = 'Manager Review'
  } else if (request.status === 'Approved') {
    progressPercentage = 75
    currentStepName = 'Execution / Final'
  } else if (request.status === 'Completed') {
    progressPercentage = 100
    currentStepName = 'Completed'
  } else if (request.status === 'Rejected' || request.status === 'Cancelled') {
    progressPercentage = 100
    currentStepName = request.status
  }

  return (
    <div className="adminhub-page-wrapper">
      {/* Page Header Bar */}
      <div className="adminhub-page-header">
        <div className="page-title-group">
          <div className="title-row">
            <h1>{request.title}</h1>
            <span className={`status-pill status-${request.status.toLowerCase()}`}>
              {request.status}
            </span>
          </div>
          <p className="page-subtitle">
            Multi-tier approval process for internal {request.category.toLowerCase()} request.
          </p>
        </div>

        <div className="page-header-actions">
          <Link className="btn-secondary" to="/requests">
            Back to list
          </Link>
          {canCancel && (
            <>
              <Link className="btn-secondary" to={`/requests/${request.id}/edit`}>
                Edit Request
              </Link>
              <button
                type="button"
                className="btn-danger-outline"
                onClick={handleCancel}
                disabled={isSubmitting}
              >
                Cancel Request
              </button>
            </>
          )}
        </div>
      </div>

      {/* Workflow Horizontal Stepper (Like AdminHub Image 2) */}
      <div className="workflow-stepper-card">
        {/* Step 1: Initiated */}
        <div className="stepper-step stepper-completed">
          <div className="step-marker" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="20 6 9 17 4 12" />
            </svg>
          </div>
          <div className="step-info">
            <strong className="step-title">1. Initiated</strong>
            <span className="step-desc">Request Created</span>
            <span className="step-meta">{request.requesterFullName}</span>
            <span className="step-date">{new Date(request.createdAt).toLocaleDateString()}</span>
          </div>
        </div>

        <div className="stepper-divider" />

        {/* Step 2: Manager Review */}
        <div
          className={`stepper-step ${
            request.status === 'Pending'
              ? 'stepper-active'
              : request.status === 'Approved' || request.status === 'Completed'
              ? 'stepper-completed'
              : request.status === 'Rejected'
              ? 'stepper-rejected'
              : 'stepper-idle'
          }`}
        >
          <div className="step-marker" aria-hidden="true">
            {request.status === 'Approved' || request.status === 'Completed' ? (
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="20 6 9 17 4 12" />
              </svg>
            ) : request.status === 'Rejected' ? (
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            ) : (
              <span>2</span>
            )}
          </div>
          <div className="step-info">
            <strong className="step-title">2. Manager Review</strong>
            <span className="step-desc">
              {request.status === 'Pending'
                ? 'Review In Progress'
                : request.status === 'Approved' || request.status === 'Completed'
                ? 'Approved'
                : request.status === 'Rejected'
                ? 'Rejected'
                : 'Cancelled'}
            </span>
            <span className="step-meta">Department Manager</span>
          </div>
        </div>

        <div className="stepper-divider" />

        {/* Step 3: Final Completion */}
        <div
          className={`stepper-step ${
            request.status === 'Completed'
              ? 'stepper-completed'
              : 'stepper-idle'
          }`}
        >
          <div className="step-marker" aria-hidden="true">
            {request.status === 'Completed' ? (
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="20 6 9 17 4 12" />
              </svg>
            ) : (
              <span>3</span>
            )}
          </div>
          <div className="step-info">
            <strong className="step-title">3. Final Completion</strong>
            <span className="step-desc">
              {request.status === 'Completed' ? 'Completed & Closed' : 'Pending Completion'}
            </span>
            <span className="step-meta">Operations Team</span>
          </div>
        </div>
      </div>

      {/* Main 2-Column Content Grid */}
      <div className="adminhub-grid-layout">
        {/* Left Column: Details & Decision */}
        <div className="grid-main-column">
          {/* Section 1: Request Details Card */}
          <section className="adminhub-card" aria-labelledby="details-heading">
            <div className="card-header">
              <div className="card-header-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                  <polyline points="14 2 14 8 20 8" />
                  <line x1="16" y1="13" x2="8" y2="13" />
                  <line x1="16" y1="17" x2="8" y2="17" />
                </svg>
              </div>
              <div>
                <h2 id="details-heading">Request Details</h2>
                <p className="card-subtitle">Detailed specifications of the submitted internal request.</p>
              </div>
            </div>

            <div className="details-attribute-grid">
              <div className="attribute-item">
                <span className="attribute-label">Request ID</span>
                <strong className="attribute-value">PR-{String(request.id).padStart(5, '0')}</strong>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Request Type</span>
                <span className={`category-badge category-${request.category.toLowerCase()}`}>
                  {request.category}
                </span>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Priority</span>
                <span className={`priority-badge priority-${request.priority.toLowerCase()}`}>
                  {request.priority}
                </span>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Quantity</span>
                <strong className="attribute-value">{request.quantity}</strong>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Estimated Cost</span>
                <strong className="attribute-value">
                  {formatEstimatedCost(request.estimatedCost)}
                </strong>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Department</span>
                <strong className="attribute-value">
                  {request.requesterDepartmentName ?? 'Not assigned'}
                </strong>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Requested By</span>
                <strong className="attribute-value">{request.requesterFullName}</strong>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Contact Email</span>
                <strong className="attribute-value">{request.requesterEmail}</strong>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Request Date</span>
                <strong className="attribute-value">
                  {new Date(request.createdAt).toLocaleDateString()}
                </strong>
              </div>
              <div className="attribute-item">
                <span className="attribute-label">Last Updated</span>
                <strong className="attribute-value">
                  {request.updatedAt
                    ? new Date(request.updatedAt).toLocaleString()
                    : 'Not updated yet'}
                </strong>
              </div>
            </div>

            <div className="details-justification-section">
              <h3>Justification & Scope</h3>
              <div className="justification-box">
                <p>{request.description}</p>
              </div>
            </div>
          </section>

          {/* Section 2: Approval Decision Card (Interactive) */}
          {canApprove && (
            <section className="adminhub-card approval-decision-card" aria-labelledby="decision-heading">
              <div className="card-header">
                <div className="card-header-icon icon-decision" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
                    <polyline points="22 4 12 14.01 9 11.01" />
                  </svg>
                </div>
                <div>
                  <h2 id="decision-heading">Approval Decision</h2>
                  <p className="card-subtitle">Review all details and submit your review decision.</p>
                </div>
              </div>

              <form onSubmit={handleDecisionSubmit}>
                <div className="decision-radio-group">
                  <label className={`decision-radio-label ${decision === 'Approve' ? 'selected-approve' : ''}`}>
                    <input
                      type="radio"
                      name="decision"
                      value="Approve"
                      checked={decision === 'Approve'}
                      onChange={() => setDecision('Approve')}
                      disabled={isSubmitting}
                    />
                    <div className="decision-radio-content">
                      <span className="decision-marker marker-approve" />
                      <div>
                        <strong>Approve Request</strong>
                        <span>Accept request and proceed to fulfillment</span>
                      </div>
                    </div>
                  </label>

                  <label className={`decision-radio-label ${decision === 'Reject' ? 'selected-reject' : ''}`}>
                    <input
                      type="radio"
                      name="decision"
                      value="Reject"
                      checked={decision === 'Reject'}
                      onChange={() => setDecision('Reject')}
                      disabled={isSubmitting}
                    />
                    <div className="decision-radio-content">
                      <span className="decision-marker marker-reject" />
                      <div>
                        <strong>Reject Request</strong>
                        <span>Decline request with an explanation</span>
                      </div>
                    </div>
                  </label>
                </div>

                <div className="decision-comments-group">
                  <label htmlFor="decision-comments">
                    {decision === 'Reject' ? (
                      <>
                        Rejection Reason <span className="required-star">*</span> (5-500 chars)
                      </>
                    ) : (
                      'Comments (Optional)'
                    )}
                  </label>
                  <textarea
                    id="decision-comments"
                    rows={3}
                    placeholder={
                      decision === 'Reject'
                        ? 'Explain why this request is being rejected...'
                        : 'Add optional approval notes or instructions...'
                    }
                    value={comments}
                    onChange={(e) => setComments(e.target.value)}
                    disabled={isSubmitting}
                    required={decision === 'Reject'}
                  />
                </div>

                <div className="decision-actions-row">
                  <button
                    type="submit"
                    className={decision === 'Approve' ? 'btn-primary' : 'btn-danger'}
                    disabled={isSubmitting}
                  >
                    {isSubmitting
                      ? 'Submitting decision...'
                      : decision === 'Approve'
                      ? 'Submit Approval'
                      : 'Submit Rejection'}
                  </button>
                </div>
              </form>
            </section>
          )}

          {canSelfApprovalWarning && (
            <section className="adminhub-card adminhub-info-banner">
              <div className="banner-content">
                <strong>Self-Approval Restriction</strong>
                <p>You created this request. As per policy, you cannot approve or reject your own request.</p>
              </div>
            </section>
          )}

          {canComplete && (
            <section className="adminhub-card completion-card">
              <div className="card-header">
                <div className="card-header-icon icon-complete" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                </div>
                <div>
                  <h2>Final Fulfillment</h2>
                  <p className="card-subtitle">This request has been approved. Mark as completed once processed.</p>
                </div>
              </div>
              <div className="completion-action">
                <button
                  type="button"
                  className="btn-primary"
                  onClick={handleComplete}
                  disabled={isSubmitting}
                >
                  {isSubmitting ? 'Processing...' : 'Mark as Completed'}
                </button>
              </div>
            </section>
          )}
        </div>

        {/* Right Sidebar: Summary, History & Quick Actions */}
        <div className="grid-side-column">
          {/* Card 1: Request Summary */}
          <section className="adminhub-card sidebar-card" aria-labelledby="summary-heading">
            <h3 id="summary-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                <line x1="3" y1="9" x2="21" y2="9" />
                <line x1="9" y1="21" x2="9" y2="9" />
              </svg>
              Request Summary
            </h3>

            <div className="summary-list">
              <div className="summary-row">
                <span>Request ID</span>
                <strong>PR-{String(request.id).padStart(5, '0')}</strong>
              </div>
              <div className="summary-row">
                <span>Category</span>
                <span className={`category-badge category-${request.category.toLowerCase()}`}>
                  {request.category}
                </span>
              </div>
              <div className="summary-row">
                <span>Priority</span>
                <span className={`priority-badge priority-${request.priority.toLowerCase()}`}>
                  {request.priority}
                </span>
              </div>
              <div className="summary-row">
                <span>Quantity</span>
                <strong>{request.quantity}</strong>
              </div>
              <div className="summary-row">
                <span>Estimated Cost</span>
                <strong>{formatEstimatedCost(request.estimatedCost)}</strong>
              </div>
              <div className="summary-row">
                <span>Current Step</span>
                <strong className="summary-step-highlight">{currentStepName}</strong>
              </div>
              <div className="summary-row">
                <span>Status</span>
                <span className={`status-pill status-${request.status.toLowerCase()}`}>
                  {request.status}
                </span>
              </div>
              <div className="summary-progress-wrapper">
                <div className="progress-labels">
                  <span>Progress</span>
                  <strong>{progressPercentage}%</strong>
                </div>
                <div className="progress-bar-track">
                  <div
                    className="progress-bar-fill"
                    style={{ width: `${progressPercentage}%` }}
                  />
                </div>
              </div>
            </div>
          </section>

          {/* Card 2: Approval History (Audit Trail like AdminHub Image 2) */}
          <section className="adminhub-card sidebar-card" aria-labelledby="history-heading">
            <h3 id="history-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <polyline points="12 6 12 12 14 14" />
              </svg>
              Approval History
            </h3>

            <div className="history-timeline">
              {history.length === 0 ? (
                <div className="timeline-empty">No status changes recorded yet.</div>
              ) : (
                history.map((item, index) => {
                  const isLast = index === history.length - 1
                  return (
                    <div key={item.id} className={`timeline-item ${isLast ? 'timeline-item-last' : ''}`}>
                      <div className="timeline-marker-col">
                        <span className={`timeline-dot dot-${item.toStatus.toLowerCase()}`} />
                        {!isLast && <span className="timeline-line" />}
                      </div>
                      <div className="timeline-content">
                        <div className="timeline-top">
                          <strong className="timeline-event-name">
                            {item.fromStatus ? `${item.toStatus}` : 'Request Initiated'}
                          </strong>
                          <span className={`history-pill pill-${item.toStatus.toLowerCase()}`}>
                            {item.toStatus}
                          </span>
                        </div>
                        <div className="timeline-actor">{item.changedByFullName}</div>
                        <div className="timeline-email">{item.changedByEmail}</div>
                        {item.note && (
                          <div className="timeline-note">
                            &ldquo;{item.note}&rdquo;
                          </div>
                        )}
                        <time className="timeline-time">
                          {new Date(item.changedAt).toLocaleString()}
                        </time>
                      </div>
                    </div>
                  )
                })
              )}
            </div>
          </section>

          {/* Card 3: Request Actions */}
          <section className="adminhub-card sidebar-card" aria-labelledby="actions-heading">
            <h3 id="actions-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
              </svg>
              Request Actions
            </h3>

            <nav className="sidebar-quick-actions" aria-label="Quick actions">
              <Link className="quick-action-link" to="/requests">
                <span>View All Requests</span>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <polyline points="9 18 15 12 9 6" />
                </svg>
              </Link>
              <Link className="quick-action-link" to="/requests/new">
                <span>Create New Request</span>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <polyline points="9 18 15 12 9 6" />
                </svg>
              </Link>
              {isOwner && request.status === 'Pending' && (
                <Link className="quick-action-link" to={`/requests/${request.id}/edit`}>
                  <span>Edit Request Details</span>
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="9 18 15 12 9 6" />
                  </svg>
                </Link>
              )}
            </nav>
          </section>
        </div>
      </div>
    </div>
  )
}

type RequestDetailPageProps = {
  currentUser: CurrentUser
}

export function RequestDetailPage({ currentUser }: RequestDetailPageProps) {
  const { requestId: requestIdParameter } = useParams<{
    requestId: string
  }>()
  const requestId = Number(requestIdParameter)

  if (!Number.isInteger(requestId) || requestId <= 0) {
    return (
      <div className="adminhub-page-wrapper">
        <Link className="adminhub-back-btn" to="/requests">
          ← Back to requests
        </Link>
        <div className="adminhub-state-card adminhub-error-card">
          Invalid request ID.
        </div>
      </div>
    )
  }

  return (
    <RequestDetailContent
      key={requestId}
      requestId={requestId}
      currentUser={currentUser}
    />
  )
}

