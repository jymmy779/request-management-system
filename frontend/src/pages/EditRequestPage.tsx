import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import type { CurrentUser } from '../api/auth'
import { getRequestById, updateRequest } from '../api/requests'
import type { CreateRequestInput, RequestItem } from '../api/requests'
import { RequestForm } from '../components/RequestForm'
import { useToast } from '../contexts/toast'

type EditRequestPageProps = {
  currentUser: CurrentUser
}

type EditRequestContentProps = EditRequestPageProps & {
  requestId: number
}

function EditRequestContent({
  requestId,
  currentUser,
}: EditRequestContentProps) {
  const navigate = useNavigate()
  const { showToast } = useToast()
  const [request, setRequest] = useState<RequestItem | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let isActive = true

    getRequestById(requestId)
      .then((result) => {
        if (isActive) {
          setRequest(result)
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

  function retryLoadRequest() {
    setIsLoading(true)
    setErrorMessage('')
    setReloadKey((currentKey) => currentKey + 1)
  }

  async function handleUpdate(input: CreateRequestInput) {
    try {
      const updatedRequest = await updateRequest(requestId, input)
      showToast('Request updated successfully.')
      navigate(`/requests/${updatedRequest.id}`)
    } catch (error) {
      showToast(
        error instanceof Error
          ? error.message
          : 'Unable to update the request.',
        'error',
      )
      throw error
    }
  }

  if (isLoading) {
    return (
      <div className="adminhub-page-wrapper">
        <div className="adminhub-state-card" role="status">
          <div className="spinner" />
          <p>Loading request...</p>
        </div>
      </div>
    )
  }

  if (errorMessage || !request) {
    return (
      <div className="adminhub-page-wrapper">
        <Link className="adminhub-back-btn" to={`/requests/${requestId}`}>
          ← Back to request
        </Link>
        <div className="adminhub-state-card adminhub-error-card" role="alert">
          <p>{errorMessage || 'Unable to load the request.'}</p>
          <button type="button" className="btn-secondary" onClick={retryLoadRequest}>
            Try again
          </button>
        </div>
      </div>
    )
  }

  if (request.requesterId !== currentUser.id) {
    return (
      <div className="adminhub-page-wrapper">
        <Link className="adminhub-back-btn" to={`/requests/${requestId}`}>
          ← Back to request
        </Link>
        <div className="adminhub-state-card adminhub-error-card">
          Only the request owner can edit this request.
        </div>
      </div>
    )
  }

  if (request.status !== 'Pending') {
    return (
      <div className="adminhub-page-wrapper">
        <Link className="adminhub-back-btn" to={`/requests/${requestId}`}>
          ← Back to request
        </Link>
        <div className="adminhub-state-card adminhub-error-card">
          Only a Pending request can be edited.
        </div>
      </div>
    )
  }

  return (
    <div className="adminhub-page-wrapper">
      <div className="adminhub-page-header">
        <div className="page-title-group">
          <div className="title-row">
            <h1>Edit Request #PR-{String(request.id).padStart(5, '0')}</h1>
            <span className={`status-pill status-${request.status.toLowerCase()}`}>
              {request.status}
            </span>
          </div>
          <p className="page-subtitle">
            Update request information before review decisions are taken.
          </p>
        </div>

        <div className="page-header-actions">
          <Link className="btn-secondary" to={`/requests/${request.id}`}>
            ← Back to Request
          </Link>
        </div>
      </div>

      <div className="adminhub-grid-layout">
        {/* Main Column: Edit Form */}
        <div className="grid-main-column">
          <section className="adminhub-card" aria-labelledby="edit-form-heading">
            <div className="card-header">
              <div className="card-header-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                </svg>
              </div>
              <div>
                <h2 id="edit-form-heading">Modify Request Details</h2>
                <p className="card-subtitle">
                  Make corrections to title, category, or justification before your manager approves.
                </p>
              </div>
            </div>

            <RequestForm
              initialValues={{
                title: request.title,
                description: request.description,
                category: request.category,
                priority: request.priority,
                quantity: request.quantity,
                estimatedCost: request.estimatedCost,
              }}
              submitLabel="Save Changes"
              submittingLabel="Saving changes..."
              cancelTo={`/requests/${request.id}`}
              onSubmit={handleUpdate}
            />
          </section>
        </div>

        {/* Right Sidebar: Rules & Original Metadata */}
        <div className="grid-side-column">
          {/* Card 1: Editing Rules */}
          <section className="adminhub-card sidebar-card" aria-labelledby="rules-heading">
            <h3 id="rules-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="16" x2="12" y2="12" />
                <line x1="12" y1="8" x2="12.01" y2="8" />
              </svg>
              Editing Policy
            </h3>

            <ul className="guidelines-list">
              <li>
                <strong>Pending state:</strong> Requests can only be edited while they are still in <em>Pending</em> status.
              </li>
              <li>
                <strong>Audit preserved:</strong> Any prior history entries and initial creation logs remain permanently preserved.
              </li>
              <li>
                <strong>Immediate effect:</strong> The updated details will be immediately visible to reviewing managers.
              </li>
            </ul>
          </section>

          {/* Card 2: Original Metadata */}
          <section className="adminhub-card sidebar-card" aria-labelledby="metadata-heading">
            <h3 id="metadata-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                <line x1="3" y1="9" x2="21" y2="9" />
                <line x1="9" y1="21" x2="9" y2="9" />
              </svg>
              Original Submission
            </h3>

            <div className="summary-list">
              <div className="summary-row">
                <span>Request ID</span>
                <strong>PR-{String(request.id).padStart(5, '0')}</strong>
              </div>
              <div className="summary-row">
                <span>Current Status</span>
                <span className={`status-pill status-${request.status.toLowerCase()}`}>
                  {request.status}
                </span>
              </div>
              <div className="summary-row">
                <span>Created On</span>
                <strong>{new Date(request.createdAt).toLocaleDateString()}</strong>
              </div>
              <div className="summary-row">
                <span>Submitted By</span>
                <strong>{request.requesterFullName}</strong>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}

export function EditRequestPage({ currentUser }: EditRequestPageProps) {
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
    <EditRequestContent
      key={requestId}
      requestId={requestId}
      currentUser={currentUser}
    />
  )
}
