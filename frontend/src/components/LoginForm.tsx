import { useState } from 'react'
import type { FormEvent } from 'react'
import type { LoginRequest } from '../api/auth'

type LoginFormProps = {
  onLogin: (loginRequest: LoginRequest) => Promise<void>
  initialError?: string
}

export function LoginForm({ onLogin, initialError = '' }: LoginFormProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState(initialError)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setErrorMessage('')

    try {
      await onLogin({ email, password })
      setPassword('')
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Unable to sign in.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className="adminhub-login-form" onSubmit={handleSubmit}>
      <div className="login-field-group">
        <label htmlFor="email">Email Address</label>
        <div className="input-with-icon">
          <svg className="field-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
            <polyline points="22,6 12,13 2,6" />
          </svg>
          <input
            id="email"
            name="email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="name@example.com"
            autoComplete="username"
            disabled={isSubmitting}
            required
          />
        </div>
      </div>

      <div className="login-field-group">
        <label htmlFor="password">Password</label>
        <div className="input-with-icon">
          <svg className="field-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
          <input
            id="password"
            name="password"
            type={showPassword ? 'text' : 'password'}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder="••••••••"
            autoComplete="current-password"
            disabled={isSubmitting}
            required
          />
          <button
            className="password-toggle"
            type="button"
            onClick={() => setShowPassword((isVisible) => !isVisible)}
            aria-label={showPassword ? 'Hide password' : 'Show password'}
            aria-pressed={showPassword}
            tabIndex={-1}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M2.2 12s3.5-6 9.8-6 9.8 6 9.8 6-3.5 6-9.8 6-9.8-6Z" />
              <circle cx="12" cy="12" r="2.5" />
              {!showPassword && <path d="m4 4 16 16" />}
            </svg>
          </button>
        </div>
      </div>

      {errorMessage && (
        <div className="adminhub-alert alert-error" role="alert" style={{ margin: '4px 0 0' }}>
          <span>{errorMessage}</span>
        </div>
      )}

      <button
        className="btn-primary login-submit-btn"
        type="submit"
        disabled={isSubmitting}
      >
        {isSubmitting ? (
          <>
            <div className="spinner-sm" aria-hidden="true" />
            <span>Signing in...</span>
          </>
        ) : (
          <span>Sign In</span>
        )}
      </button>
    </form>
  )
}
