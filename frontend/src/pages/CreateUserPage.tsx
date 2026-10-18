import { Link, useNavigate } from 'react-router'
import { createUser } from '../api/users'
import type { CreateUserInput } from '../api/users'
import { CreateUserForm } from '../components/CreateUserForm'
import { useToast } from '../contexts/toast'
import { useDepartments } from '../hooks/useDepartments'

export function CreateUserPage() {
  const navigate = useNavigate()
  const { showToast } = useToast()
  const {
    departments,
    isUnavailable: departmentsUnavailable,
  } = useDepartments()

  async function handleCreateUser(input: CreateUserInput) {
    try {
      const createdUser = await createUser(input)
      showToast(`${createdUser.fullName} was created successfully.`)
      navigate('/users')
    } catch (error) {
      showToast(
        error instanceof Error ? error.message : 'Unable to create the user.',
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
            <h1>Create New User</h1>
            <span className="count-pill">Internal Account</span>
          </div>
          <p className="page-subtitle">
            Add a new employee identity, assign their system role, and map their department.
          </p>
        </div>

        <div className="page-header-actions">
          <Link className="btn-secondary" to="/users">
            ← Back to Users
          </Link>
        </div>
      </div>

      <div className="adminhub-grid-layout">
        {/* Main Column: The Form */}
        <div className="grid-main-column">
          <section className="adminhub-card" aria-labelledby="create-user-heading">
            <div className="card-header">
              <div className="card-header-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
                  <circle cx="9" cy="7" r="4" />
                  <line x1="19" y1="8" x2="19" y2="14" />
                  <line x1="22" y1="11" x2="16" y2="11" />
                </svg>
              </div>
              <div>
                <h2 id="create-user-heading">User Account Information</h2>
                <p className="card-subtitle">
                  Configure identity credentials and security access permissions.
                </p>
              </div>
            </div>

            <CreateUserForm
              departments={departments}
              departmentsUnavailable={departmentsUnavailable}
              onSubmit={handleCreateUser}
              onCancel={() => navigate('/users')}
            />
          </section>
        </div>

        {/* Right Sidebar: Guidelines & Security Policy */}
        <div className="grid-side-column">
          {/* Card 1: Role Permissions Overview */}
          <section className="adminhub-card sidebar-card" aria-labelledby="roles-overview-heading">
            <h3 id="roles-overview-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
              </svg>
              Role Privileges
            </h3>

            <ul className="guidelines-list">
              <li>
                <strong>Employee:</strong> Can create requests, edit their own draft submissions, and cancel them.
              </li>
              <li>
                <strong>Manager:</strong> Has access to approval queues, can review, approve, reject, and complete requests.
              </li>
              <li>
                <strong>Admin:</strong> Global governance, user lifecycle management, department settings, and system audits.
              </li>
            </ul>
          </section>

          {/* Card 2: Security Guidelines */}
          <section className="adminhub-card sidebar-card" aria-labelledby="security-tips-heading">
            <h3 id="security-tips-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                <path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
              Security Policy
            </h3>

            <ul className="guidelines-list">
              <li>
                <strong>Temporary password:</strong> The user should change their password upon initial sign-in.
              </li>
              <li>
                <strong>Department mapping:</strong> Assigning a department enables automatic routing in approval workflows.
              </li>
            </ul>
          </section>
        </div>
      </div>
    </div>
  )
}
