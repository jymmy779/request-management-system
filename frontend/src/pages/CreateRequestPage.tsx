import { Link, useNavigate } from 'react-router'
import type { CurrentUser } from '../api/auth'
import { createRequest } from '../api/requests'
import type { CreateRequestInput } from '../api/requests'
import { RequestForm } from '../components/RequestForm'
import { useToast } from '../contexts/toast'

type CreateRequestPageProps = {
  currentUser: CurrentUser
}

export function CreateRequestPage({ currentUser }: CreateRequestPageProps) {
  const navigate = useNavigate()
  const { showToast } = useToast()

  async function handleCreate(input: CreateRequestInput) {
    try {
      const request = await createRequest(input)
      showToast('Request created successfully.')
      navigate(`/requests/${request.id}`)
    } catch (error) {
      showToast(
        error instanceof Error
          ? error.message
          : 'Unable to create the request.',
        'error',
      )
      throw error
    }
  }

  return (
    <div className="adminhub-page-wrapper">
      <div className="adminhub-page-header">
        <div className="page-title-group">
          <div className="title-row">
            <h1>Create New Request</h1>
            <span className="count-pill">Drafting</span>
          </div>
          <p className="page-subtitle">
            Initiate a multi-tier review request for equipment, software, or workspace resources.
          </p>
        </div>

        <div className="page-header-actions">
          <Link className="btn-secondary" to="/requests">
            ← Back to Requests
          </Link>
        </div>
      </div>

      <div className="adminhub-grid-layout">
        {/* Main Column: The Form */}
        <div className="grid-main-column">
          <section className="adminhub-card" aria-labelledby="form-heading">
            <div className="card-header">
              <div className="card-header-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                  <polyline points="14 2 14 8 20 8" />
                  <line x1="12" y1="18" x2="12" y2="12" />
                  <line x1="9" y1="15" x2="15" y2="15" />
                </svg>
              </div>
              <div>
                <h2 id="form-heading">Request Specifications</h2>
                <p className="card-subtitle">
                  Fill in all required fields. Your request will enter the approval queue upon submission.
                </p>
              </div>
            </div>

            <RequestForm
              submitLabel="Submit Request"
              submittingLabel="Submitting request..."
              cancelTo="/requests"
              onSubmit={handleCreate}
            />
          </section>
        </div>

        {/* Right Sidebar: Preview, Tips & Requester Identity */}
        <div className="grid-side-column">
          {/* Card 1: Workflow Preview */}
          <section className="adminhub-card sidebar-card" aria-labelledby="workflow-heading">
            <h3 id="workflow-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
              </svg>
              Approval Workflow
            </h3>

            <div className="preview-stepper">
              <div className="preview-step">
                <div className="preview-marker active-marker">1</div>
                <div className="preview-info">
                  <strong>Submit Request</strong>
                  <span>Enters queue in Pending status</span>
                </div>
              </div>
              <div className="preview-step-line" />
              <div className="preview-step">
                <div className="preview-marker">2</div>
                <div className="preview-info">
                  <strong>Manager Review</strong>
                  <span>Approval decision with audit logging</span>
                </div>
              </div>
              <div className="preview-step-line" />
              <div className="preview-step">
                <div className="preview-marker">3</div>
                <div className="preview-info">
                  <strong>Execution & Close</strong>
                  <span>Operations fulfills approved items</span>
                </div>
              </div>
            </div>
          </section>

          {/* Card 2: Submission Guidelines */}
          <section className="adminhub-card sidebar-card" aria-labelledby="guidelines-heading">
            <h3 id="guidelines-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="16" x2="12" y2="12" />
                <line x1="12" y1="8" x2="12.01" y2="8" />
              </svg>
              Submission Tips
            </h3>

            <ul className="guidelines-list">
              <li>
                <strong>Be specific:</strong> Include exact model, license quantity, or project context in justification.
              </li>
              <li>
                <strong>Editable status:</strong> You can edit or cancel this request anytime while it remains <em>Pending</em>.
              </li>
              <li>
                <strong>Review turnaround:</strong> Department reviews are normally completed within 24–48 hours.
              </li>
            </ul>
          </section>

          {/* Card 3: Requester Identity */}
          <section className="adminhub-card sidebar-card" aria-labelledby="requester-heading">
            <h3 id="requester-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
              Submitting As
            </h3>

            <div className="requester-profile-snippet">
              <span className="profile-avatar-circle" aria-hidden="true">
                {currentUser.email.charAt(0).toUpperCase()}
              </span>
              <div className="profile-snippet-info">
                <strong>{currentUser.email}</strong>
                <span className="profile-role-badge">{currentUser.role}</span>
                <span className="profile-dept-sub">
                  {currentUser.departmentName ?? 'Department not assigned'}
                </span>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
