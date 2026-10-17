import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router'
import type { CurrentUser } from '../api/auth'

type AppLayoutProps = {
  currentUser: CurrentUser
  onLogout: () => Promise<void>
}

export function AppLayout({ currentUser, onLogout }: AppLayoutProps) {
  const [isSigningOut, setIsSigningOut] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')
  const location = useLocation()

  async function handleLogout() {
    setIsSigningOut(true)
    setErrorMessage('')

    try {
      await onLogout()
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Unable to sign out.',
      )
      setIsSigningOut(false)
    }
  }

  // Generate simple breadcrumb context based on current path
  const isDetail = location.pathname.startsWith('/requests/') && !location.pathname.endsWith('/new') && !location.pathname.endsWith('/edit')
  const isNew = location.pathname === '/requests/new'
  const isEdit = location.pathname.endsWith('/edit')

  return (
    <div className="adminhub-shell">
      {/* Dark Navy Sidebar */}
      <aside className="adminhub-sidebar">
        <div className="adminhub-brand">
          <div className="adminhub-brand-logo" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M12 2L2 7l10 5 10-5-10-5z" />
              <path d="M2 17l10 5 10-5" />
              <path d="M2 12l10 5 10-5" />
            </svg>
          </div>
          <div className="adminhub-brand-text">
            <strong>AdminHub</strong>
            <span>Workflow & Approvals</span>
          </div>
        </div>

        <nav className="adminhub-nav" aria-label="Main navigation">
          <div className="nav-section-title">REQUESTS</div>
          <NavLink
            to="/requests"
            end
            className={({ isActive }) =>
              isActive
                ? 'adminhub-nav-link adminhub-nav-link-active'
                : 'adminhub-nav-link'
            }
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
              <line x1="16" y1="13" x2="8" y2="13" />
              <line x1="16" y1="17" x2="8" y2="17" />
              <polyline points="10 9 9 9 8 9" />
            </svg>
            <span>All Requests</span>
          </NavLink>

          <NavLink
            to="/requests/new"
            className={({ isActive }) =>
              isActive
                ? 'adminhub-nav-link adminhub-nav-link-active'
                : 'adminhub-nav-link'
            }
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="8" x2="12" y2="16" />
              <line x1="8" y1="12" x2="16" y2="12" />
            </svg>
            <span>New Request</span>
          </NavLink>
        </nav>

        {/* Sidebar Footer Secondary Navigation (AdminHub Image 2 style) */}
        <div className="adminhub-sidebar-footer">
          <div className="sidebar-secondary-nav">
            <div className="secondary-nav-item" title="Settings">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="3" />
                <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
              </svg>
              <span>Settings</span>
            </div>
            <div className="secondary-nav-item" title="Help & Support">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" />
                <line x1="12" y1="17" x2="12.01" y2="17" />
              </svg>
              <span>Help & Support</span>
            </div>
          </div>
        </div>
      </aside>

      {/* Main App Content Area */}
      <div className="adminhub-main">
        {/* Top Navbar */}
        <header className="adminhub-topbar">
          <nav className="topbar-breadcrumbs" aria-label="Breadcrumb">
            <span className="breadcrumb-muted">Approvals</span>
            <span className="breadcrumb-separator">/</span>
            <span className="breadcrumb-muted">Purchase Request</span>
            {isDetail && (
              <>
                <span className="breadcrumb-separator">/</span>
                <span className="breadcrumb-active">Details</span>
              </>
            )}
            {isNew && (
              <>
                <span className="breadcrumb-separator">/</span>
                <span className="breadcrumb-active">New</span>
              </>
            )}
            {isEdit && (
              <>
                <span className="breadcrumb-separator">/</span>
                <span className="breadcrumb-active">Edit</span>
              </>
            )}
          </nav>

          <div className="topbar-right">
            <div className="topbar-user">
              <span className="topbar-avatar" aria-hidden="true">
                {currentUser.email.charAt(0).toUpperCase()}
              </span>
              <div className="topbar-user-info">
                <strong>{currentUser.email}</strong>
                <span className="role-tag">{currentUser.role}</span>
              </div>
            </div>

            <button
              className="topbar-logout-btn"
              type="button"
              onClick={handleLogout}
              disabled={isSigningOut}
              title="Sign out"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                <polyline points="16 17 21 12 16 7" />
                <line x1="21" y1="12" x2="9" y2="12" />
              </svg>
              <span>{isSigningOut ? 'Signing out...' : 'Sign out'}</span>
            </button>
          </div>
        </header>

        {errorMessage && (
          <div className="adminhub-alert alert-error" style={{ margin: '16px 36px 0' }} role="alert">
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Dynamic Route Pages */}
        <main className="adminhub-content-container">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

