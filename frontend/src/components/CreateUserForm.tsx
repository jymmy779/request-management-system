import { useState } from 'react'
import type { FormEvent } from 'react'
import type { DepartmentItem } from '../api/departments'
import type { CreateUserInput, UserRole } from '../api/users'

type CreateUserFormProps = {
  departments: DepartmentItem[]
  departmentsUnavailable: boolean
  onSubmit: (input: CreateUserInput) => Promise<void>
  onCancel: () => void
}

export function CreateUserForm({
  departments,
  departmentsUnavailable,
  onSubmit,
  onCancel,
}: CreateUserFormProps) {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<UserRole>('Employee')
  const [departmentId, setDepartmentId] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setErrorMessage('')

    try {
      await onSubmit({
        fullName: fullName.trim(),
        email: email.trim(),
        password,
        role,
        departmentId: departmentId ? Number(departmentId) : null,
      })
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Unable to create the user.',
      )
      setIsSubmitting(false)
    }
  }

  return (
    <form className="create-request-form" onSubmit={handleSubmit}>
      {errorMessage && (
        <div className="adminhub-alert alert-error" role="alert">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          <span>{errorMessage}</span>
        </div>
      )}

      <div className="form-group">
        <label htmlFor="new-user-full-name">
          Full Name <span className="required-star">*</span>
        </label>
        <input
          id="new-user-full-name"
          type="text"
          value={fullName}
          required
          minLength={2}
          maxLength={150}
          placeholder="e.g. Alex Morgan"
          disabled={isSubmitting}
          onChange={(event) => setFullName(event.target.value)}
        />
        <span className="field-hint">Between 2 and 150 characters.</span>
      </div>

      <div className="form-group">
        <label htmlFor="new-user-email">
          Email Address <span className="required-star">*</span>
        </label>
        <input
          id="new-user-email"
          type="email"
          value={email}
          required
          maxLength={256}
          placeholder="name@company.internal"
          autoComplete="off"
          disabled={isSubmitting}
          onChange={(event) => setEmail(event.target.value)}
        />
        <span className="field-hint">Used as their sign-in username.</span>
      </div>

      <div className="form-group">
        <label htmlFor="new-user-password">
          Temporary Password <span className="required-star">*</span>
        </label>
        <div className="password-input-wrap">
          <input
            id="new-user-password"
            type={showPassword ? 'text' : 'password'}
            value={password}
            required
            maxLength={100}
            placeholder="••••••••"
            autoComplete="new-password"
            disabled={isSubmitting}
            onChange={(event) => setPassword(event.target.value)}
          />
          <button
            type="button"
            className="password-toggle"
            aria-label={showPassword ? 'Hide password' : 'Show password'}
            disabled={isSubmitting}
            onClick={() => setShowPassword((current) => !current)}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              {showPassword ? (
                <>
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94" />
                  <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19" />
                  <line x1="1" y1="1" x2="23" y2="23" />
                </>
              ) : (
                <>
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z" />
                  <circle cx="12" cy="12" r="3" />
                </>
              )}
            </svg>
          </button>
        </div>
        <span className="field-hint">Min 6 chars, uppercase, lowercase, digit.</span>
      </div>

      <div className="form-group">
        <label>
          Assigned System Role <span className="required-star">*</span>
        </label>
        <div className="role-card-selector" role="radiogroup" aria-label="System Role">
          <button
            type="button"
            className={`role-option-card ${role === 'Employee' ? 'role-card-active' : ''}`}
            onClick={() => setRole('Employee')}
            disabled={isSubmitting}
          >
            <div className="role-icon-wrap role-icon-employee">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
            </div>
            <div className="role-text-wrap">
              <strong>Employee</strong>
              <span>Create and manage personal requests</span>
            </div>
          </button>

          <button
            type="button"
            className={`role-option-card ${role === 'Manager' ? 'role-card-active' : ''}`}
            onClick={() => setRole('Manager')}
            disabled={isSubmitting}
          >
            <div className="role-icon-wrap role-icon-manager">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                <polyline points="9 12 11 14 15 10" />
              </svg>
            </div>
            <div className="role-text-wrap">
              <strong>Manager</strong>
              <span>Review queues and make approval decisions</span>
            </div>
          </button>

          <button
            type="button"
            className={`role-option-card ${role === 'Admin' ? 'role-card-active' : ''}`}
            onClick={() => setRole('Admin')}
            disabled={isSubmitting}
          >
            <div className="role-icon-wrap role-icon-admin">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="3" />
                <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
              </svg>
            </div>
            <div className="role-text-wrap">
              <strong>Admin</strong>
              <span>Full governance, users, and system audits</span>
            </div>
          </button>
        </div>
      </div>

      <div className="form-group">
        <label htmlFor="new-user-department">Department Assignment</label>
        <select
          id="new-user-department"
          value={departmentId}
          disabled={isSubmitting || departmentsUnavailable}
          onChange={(event) => setDepartmentId(event.target.value)}
        >
          <option value="">Unassigned (No Department)</option>
          {departments.map((department) => (
            <option key={department.id} value={department.id}>
              {department.name} ({department.code})
            </option>
          ))}
        </select>
        <span className="field-hint">Associates user with internal routing and visibility.</span>
      </div>

      <div className="create-form-actions">
        <button
          type="submit"
          className="btn-primary"
          disabled={isSubmitting}
        >
          {isSubmitting ? (
            <>
              <div className="spinner-sm" aria-hidden="true" />
              <span>Creating user...</span>
            </>
          ) : (
            <span>Create User</span>
          )}
        </button>
        <button
          type="button"
          className="btn-secondary"
          disabled={isSubmitting}
          onClick={onCancel}
        >
          Cancel
        </button>
      </div>
    </form>
  )
}
