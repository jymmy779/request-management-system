import type { LoginRequest } from '../api/auth'
import { LoginForm } from '../components/LoginForm'

type LoginPageProps = {
  onLogin: (loginRequest: LoginRequest) => Promise<void>
  initialError?: string
}

export function LoginPage({ onLogin, initialError = '' }: LoginPageProps) {
  return (
    <div className="adminhub-login-page">
      <div className="login-ambient-glow" aria-hidden="true" />

      <main className="adminhub-login-card" aria-labelledby="login-title">
        <header className="login-brand-header">
          <div className="login-brand-logo" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M12 2L2 7l10 5 10-5-10-5z" />
              <path d="M2 17l10 5 10-5" />
              <path d="M2 12l10 5 10-5" />
            </svg>
          </div>
          <h1>AdminHub</h1>
          <p className="login-subtitle">Request Management & Workflow System</p>
        </header>

        <div className="login-welcome-text">
          <h2 id="login-title">Sign in to your account</h2>
          <p>Enter your internal credentials to access the workspace</p>
        </div>

        <LoginForm onLogin={onLogin} initialError={initialError} />

        <footer className="login-card-footer">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
          <span>Internal workspace • Role-based access control</span>
        </footer>
      </main>
    </div>
  )
}

