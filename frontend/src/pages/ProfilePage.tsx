import type { CurrentUser } from '../api/auth'
import { Link } from 'react-router'

type ProfilePageProps = {
  currentUser: CurrentUser
}

export function ProfilePage({ currentUser }: ProfilePageProps) {
  const displayName = currentUser.fullName?.trim() || currentUser.email
  const avatarLabel = displayName.charAt(0).toUpperCase()
  const initials =
    currentUser.fullName
      ?.trim()
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((w) => w[0].toUpperCase())
      .join('') || avatarLabel

  const roleConfig: Record<string, { label: string; color: string; description: string }> = {
    Admin: {
      label: 'Administrator',
      color: '#7c3aed',
      description: 'Full system governance, user management, and audit access.',
    },
    Manager: {
      label: 'Manager',
      color: '#0891b2',
      description: 'Approval queues, review decisions, and team oversight.',
    },
    Employee: {
      label: 'Employee',
      color: '#059669',
      description: 'Submit requests, track status, and manage own submissions.',
    },
  }

  const role = roleConfig[currentUser.role] ?? {
    label: currentUser.role,
    color: '#64748b',
    description: '',
  }

  return (
    <div className="adminhub-page-wrapper">
      <div className="adminhub-page-header">
        <div className="page-title-group">
          <div className="title-row">
            <h1>My Profile</h1>
          </div>
          <p className="page-subtitle">
            View your account identity, role, and organization assignment.
          </p>
        </div>
        <div className="page-header-actions">
          <Link className="btn-secondary" to="/profile/security">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" style={{ width: 14, height: 14 }}>
              <rect x="3" y="11" width="18" height="10" rx="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
            Change Password
          </Link>
        </div>
      </div>

      <div className="profile-layout">
        {/* Left: Identity Card */}
        <div className="profile-main-col">
          <section className="adminhub-card profile-identity-card" aria-labelledby="profile-heading">
            {/* Hero Banner */}
            <div className="profile-hero-banner">
              <div className="profile-hero-avatar" aria-hidden="true">
                {initials}
              </div>
              <div className="profile-hero-info">
                <h2 id="profile-heading">{displayName}</h2>
                <span
                  className="profile-role-badge"
                  style={{ '--role-color': role.color } as React.CSSProperties}
                >
                  {role.label}
                </span>
                <p className="profile-role-desc">{role.description}</p>
              </div>
            </div>

            {/* Details Grid */}
            <div className="profile-details-section">
              <h3 className="profile-section-title">Account Details</h3>
              <dl className="profile-details-grid">
                <div className="profile-detail-item">
                  <dt>
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                      <circle cx="12" cy="7" r="4" />
                    </svg>
                    Full Name
                  </dt>
                  <dd>{currentUser.fullName || <span className="profile-empty-value">Not provided</span>}</dd>
                </div>

                <div className="profile-detail-item">
                  <dt>
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                      <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
                      <polyline points="22,6 12,13 2,6" />
                    </svg>
                    Email Address
                  </dt>
                  <dd>{currentUser.email}</dd>
                </div>

                <div className="profile-detail-item">
                  <dt>
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                    </svg>
                    System Role
                  </dt>
                  <dd>
                    <span
                      className="profile-role-badge"
                      style={{ '--role-color': role.color } as React.CSSProperties}
                    >
                      {role.label}
                    </span>
                  </dd>
                </div>

                <div className="profile-detail-item">
                  <dt>
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                      <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                      <polyline points="9 22 9 12 15 12 15 22" />
                    </svg>
                    Department
                  </dt>
                  <dd>
                    {currentUser.departmentName ?? (
                      <span className="profile-empty-value">Not assigned</span>
                    )}
                  </dd>
                </div>
              </dl>
            </div>
          </section>
        </div>

        {/* Right: Sidebar */}
        <div className="profile-side-col">
          {/* Quick Actions */}
          <section className="adminhub-card sidebar-card" aria-labelledby="quick-actions-heading">
            <h3 id="quick-actions-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="3" />
                <path d="M12 1v4M12 19v4M4.22 4.22l2.83 2.83M16.95 16.95l2.83 2.83M1 12h4M19 12h4M4.22 19.78l2.83-2.83M16.95 7.05l2.83-2.83" />
              </svg>
              Quick Actions
            </h3>
            <ul className="profile-action-list">
              <li>
                <Link to="/profile/security" className="profile-action-link">
                  <span className="profile-action-icon" aria-hidden="true">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <rect x="3" y="11" width="18" height="10" rx="2" />
                      <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                    </svg>
                  </span>
                  <span>
                    <strong>Change Password</strong>
                    <span>Update your sign-in credentials</span>
                  </span>
                  <svg className="profile-action-chevron" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                    <polyline points="9 18 15 12 9 6" />
                  </svg>
                </Link>
              </li>
              <li>
                <Link to="/requests" className="profile-action-link">
                  <span className="profile-action-icon" aria-hidden="true">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                      <polyline points="14 2 14 8 20 8" />
                      <line x1="16" y1="13" x2="8" y2="13" />
                      <line x1="16" y1="17" x2="8" y2="17" />
                      <polyline points="10 9 9 9 8 9" />
                    </svg>
                  </span>
                  <span>
                    <strong>My Requests</strong>
                    <span>View all submitted requests</span>
                  </span>
                  <svg className="profile-action-chevron" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                    <polyline points="9 18 15 12 9 6" />
                  </svg>
                </Link>
              </li>
            </ul>
          </section>

          {/* Role Permissions */}
          <section className="adminhub-card sidebar-card" aria-labelledby="permissions-heading">
            <h3 id="permissions-heading" className="sidebar-card-title">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                <polyline points="9 12 11 14 15 10" />
              </svg>
              Access Permissions
            </h3>
            <ul className="guidelines-list">
              {currentUser.role === 'Employee' && (
                <>
                  <li>Submit and manage your own requests</li>
                  <li>Track status updates in real time</li>
                  <li>Cancel pending drafts</li>
                </>
              )}
              {currentUser.role === 'Manager' && (
                <>
                  <li>Review and action approval queues</li>
                  <li>Approve, reject, or complete requests</li>
                  <li>View all requests in your scope</li>
                </>
              )}
              {currentUser.role === 'Admin' && (
                <>
                  <li>Full governance over all requests</li>
                  <li>User lifecycle and role management</li>
                  <li>Department configuration and audits</li>
                </>
              )}
            </ul>
          </section>
        </div>
      </div>
    </div>
  )
}
