import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router'
import { changePassword } from '../api/auth'
import { useToast } from '../contexts/toast'

type ChangePasswordPageProps = {
  onPasswordChanged: () => void
}

type PasswordFieldProps = {
  id: string
  label: string
  value: string
  autoComplete: string
  isVisible: boolean
  isDisabled: boolean
  onChange: (value: string) => void
  onToggleVisibility: () => void
}

function PasswordField({
  id,
  label,
  value,
  autoComplete,
  isVisible,
  isDisabled,
  onChange,
  onToggleVisibility,
}: PasswordFieldProps) {
  return (
    <div className="form-field">
      <label htmlFor={id}>{label}</label>
      <div className="password-input-wrap">
        <input
          id={id}
          type={isVisible ? 'text' : 'password'}
          value={value}
          required
          maxLength={100}
          autoComplete={autoComplete}
          disabled={isDisabled}
          onChange={(event) => onChange(event.target.value)}
        />
        <button
          type="button"
          className="password-toggle"
          aria-label={isVisible ? `Hide ${label.toLowerCase()}` : `Show ${label.toLowerCase()}`}
          disabled={isDisabled}
          onClick={onToggleVisibility}
        >
          <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z" />
            <circle cx="12" cy="12" r="3" />
            {!isVisible && <line x1="2" y1="2" x2="22" y2="22" />}
          </svg>
        </button>
      </div>
    </div>
  )
}

export function ChangePasswordPage({
  onPasswordChanged,
}: ChangePasswordPageProps) {
  const { showToast } = useToast()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [visibleField, setVisibleField] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrorMessage('')

    if (newPassword !== confirmPassword) {
      setErrorMessage('The new password and confirmation do not match.')
      return
    }

    if (currentPassword === newPassword) {
      setErrorMessage('The new password must differ from the current password.')
      return
    }

    setIsSubmitting(true)

    try {
      await changePassword({ currentPassword, newPassword })
      showToast('Password changed. Sign in again with your new password.')
      onPasswordChanged()
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : 'Unable to change the password.',
      )
      setIsSubmitting(false)
    }
  }

  function toggleVisibility(fieldName: string) {
    setVisibleField((currentField) =>
      currentField === fieldName ? null : fieldName,
    )
  }

  return (
    <div className="adminhub-page-wrapper">
      <div className="adminhub-page-header">
        <div className="page-title-group">
          <div className="title-row">
            <h1>Change Password</h1>
          </div>
          <p className="page-subtitle">
            Update your password. You will need to sign in again afterward.
          </p>
        </div>
        <div className="page-header-actions">
          <Link className="btn-secondary" to="/profile">
            ← Back to Profile
          </Link>
        </div>
      </div>

      <section className="adminhub-card form-container-card" aria-labelledby="security-heading">
        <div className="card-header">
          <div className="card-header-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="11" width="18" height="10" rx="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
          </div>
          <div>
            <h2 id="security-heading">Password security</h2>
            <p className="card-subtitle">Use a password that is not shared with another account.</p>
          </div>
        </div>

        <form className="request-form" onSubmit={handleSubmit}>
          {errorMessage && (
            <div className="adminhub-alert alert-error" role="alert">
              {errorMessage}
            </div>
          )}

          <PasswordField
            id="current-password"
            label="Current password"
            value={currentPassword}
            autoComplete="current-password"
            isVisible={visibleField === 'current'}
            isDisabled={isSubmitting}
            onChange={setCurrentPassword}
            onToggleVisibility={() => toggleVisibility('current')}
          />
          <PasswordField
            id="new-password"
            label="New password"
            value={newPassword}
            autoComplete="new-password"
            isVisible={visibleField === 'new'}
            isDisabled={isSubmitting}
            onChange={setNewPassword}
            onToggleVisibility={() => toggleVisibility('new')}
          />
          <PasswordField
            id="confirm-password"
            label="Confirm new password"
            value={confirmPassword}
            autoComplete="new-password"
            isVisible={visibleField === 'confirm'}
            isDisabled={isSubmitting}
            onChange={setConfirmPassword}
            onToggleVisibility={() => toggleVisibility('confirm')}
          />

          <span className="field-hint">
            Min 6 chars, uppercase, lowercase, digit.
          </span>

          <div className="form-actions">
            <button type="submit" className="btn-primary" disabled={isSubmitting}>
              {isSubmitting ? 'Changing password...' : 'Change password'}
            </button>
            <Link className="btn-secondary" to="/profile">
              Cancel
            </Link>
          </div>
        </form>
      </section>
    </div>
  )
}
