import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router'
import type {
  CreateRequestInput,
  RequestCategory,
  RequestPriority,
} from '../api/requests'

type RequestFormProps = {
  initialValues?: CreateRequestInput
  submitLabel: string
  submittingLabel: string
  cancelTo: string
  onSubmit: (input: CreateRequestInput) => Promise<void>
}

export function RequestForm({
  initialValues,
  submitLabel,
  submittingLabel,
  cancelTo,
  onSubmit,
}: RequestFormProps) {
  const [title, setTitle] = useState(initialValues?.title ?? '')
  const [description, setDescription] = useState(
    initialValues?.description ?? '',
  )
  const [category, setCategory] = useState<RequestCategory | ''>(
    initialValues?.category ?? '',
  )
  const [priority, setPriority] = useState<RequestPriority>(
    initialValues?.priority ?? 'Medium',
  )
  const [quantity, setQuantity] = useState(initialValues?.quantity ?? 1)
  const [estimatedCost, setEstimatedCost] = useState(
    initialValues?.estimatedCost?.toString() ?? '',
  )
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!category) {
      setErrorMessage('Please select a category.')
      return
    }

    if (!Number.isInteger(quantity) || quantity < 1 || quantity > 1000) {
      setErrorMessage('Quantity must be a whole number from 1 to 1,000.')
      return
    }

    const parsedEstimatedCost =
      estimatedCost.trim() === '' ? null : Number(estimatedCost)

    if (
      parsedEstimatedCost !== null &&
      (!Number.isFinite(parsedEstimatedCost) ||
        parsedEstimatedCost < 0.01 ||
        parsedEstimatedCost > 999999999999.99)
    ) {
      setErrorMessage('Estimated cost must be greater than 0.')
      return
    }

    setIsSubmitting(true)
    setErrorMessage('')

    try {
      await onSubmit({
        title: title.trim(),
        description: description.trim(),
        category,
        priority,
        quantity,
        estimatedCost: parsedEstimatedCost,
      })
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : 'Unable to save the request.',
      )
      setIsSubmitting(false)
    }
  }

  return (
    <form className="create-request-form" onSubmit={handleSubmit}>
      <div className="form-group">
        <div className="label-with-counter">
          <label htmlFor="request-title">
            Request Title <span className="required-star">*</span>
          </label>
          <span className="char-counter">{title.length} / 150</span>
        </div>
        <input
          id="request-title"
          type="text"
          value={title}
          onChange={(event) => setTitle(event.target.value)}
          placeholder="e.g. Dual 27-inch 4K Monitor for Engineering Setup"
          minLength={5}
          maxLength={150}
          required
          disabled={isSubmitting}
        />
        <span className="field-hint">Minimum 5 characters, clear and concise.</span>
      </div>

      <div className="form-group">
        <label htmlFor="request-priority">
          Priority <span className="required-star">*</span>
        </label>
        <select
          id="request-priority"
          value={priority}
          onChange={(event) =>
            setPriority(event.target.value as RequestPriority)
          }
          required
          disabled={isSubmitting}
        >
          <option value="Low">Low</option>
          <option value="Medium">Medium</option>
          <option value="High">High</option>
        </select>
        <span className="field-hint">
          Choose how urgently this request needs attention.
        </span>
      </div>

      <div className="form-group">
        <label htmlFor="request-quantity">
          Quantity <span className="required-star">*</span>
        </label>
        <input
          id="request-quantity"
          type="number"
          min={1}
          max={1000}
          step={1}
          value={quantity}
          onChange={(event) => setQuantity(Number(event.target.value))}
          required
          disabled={isSubmitting}
        />
        <span className="field-hint">
          Enter the number of items, licenses, or units requested.
        </span>
      </div>

      <div className="form-group">
        <label htmlFor="request-estimated-cost">Estimated Cost (VND)</label>
        <input
          id="request-estimated-cost"
          type="number"
          min="0.01"
          max="999999999999.99"
          step="0.01"
          value={estimatedCost}
          onChange={(event) => setEstimatedCost(event.target.value)}
          placeholder="e.g. 18000000"
          disabled={isSubmitting}
        />
        <span className="field-hint">
          Optional total estimated cost for the entire request.
        </span>
      </div>

      <div className="form-group">
        <label>
          Resource Category <span className="required-star">*</span>
        </label>
        <div className="category-card-selector" role="radiogroup" aria-label="Resource Category">
          <button
            type="button"
            className={`category-option-card ${category === 'Equipment' ? 'category-card-active' : ''}`}
            onClick={() => setCategory('Equipment')}
            disabled={isSubmitting}
          >
            <div className="cat-icon-wrap cat-equipment">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
                <line x1="8" y1="21" x2="16" y2="21" />
                <line x1="12" y1="17" x2="12" y2="21" />
              </svg>
            </div>
            <div className="cat-text-wrap">
              <strong>Equipment</strong>
              <span>Hardware, screens, peripherals</span>
            </div>
          </button>

          <button
            type="button"
            className={`category-option-card ${category === 'Software' ? 'category-card-active' : ''}`}
            onClick={() => setCategory('Software')}
            disabled={isSubmitting}
          >
            <div className="cat-icon-wrap cat-software">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <polyline points="16 18 22 12 16 6" />
                <polyline points="8 6 2 12 8 18" />
              </svg>
            </div>
            <div className="cat-text-wrap">
              <strong>Software</strong>
              <span>Licenses, IDEs, subscriptions</span>
            </div>
          </button>

          <button
            type="button"
            className={`category-option-card ${category === 'Other' ? 'category-card-active' : ''}`}
            onClick={() => setCategory('Other')}
            disabled={isSubmitting}
          >
            <div className="cat-icon-wrap cat-other">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
                <polyline points="3.27 6.96 12 12.01 20.73 6.96" />
                <line x1="12" y1="22.08" x2="12" y2="12" />
              </svg>
            </div>
            <div className="cat-text-wrap">
              <strong>Other</strong>
              <span>Office supplies, accessories</span>
            </div>
          </button>
        </div>
      </div>

      <div className="form-group">
        <div className="label-with-counter">
          <label htmlFor="request-description">
            Justification & Description <span className="required-star">*</span>
          </label>
          <span className="char-counter">{description.length} / 2,000</span>
        </div>
        <textarea
          id="request-description"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          placeholder="Describe why this resource is required, business impact, and any specific preferences or links..."
          minLength={10}
          maxLength={2000}
          rows={7}
          required
          disabled={isSubmitting}
        />
        <span className="field-hint">Minimum 10 characters. Detailed justification speeds up manager approval.</span>
      </div>

      {errorMessage && (
        <div className="adminhub-alert alert-error" role="alert">
          <span>{errorMessage}</span>
        </div>
      )}

      <div className="create-form-actions">
        <button className="btn-primary" type="submit" disabled={isSubmitting}>
          {isSubmitting ? (
            <>
              <div className="spinner-sm" aria-hidden="true" />
              <span>{submittingLabel}</span>
            </>
          ) : (
            submitLabel
          )}
        </button>
        <Link className="btn-secondary" to={cancelTo}>
          Cancel
        </Link>
      </div>
    </form>
  )
}
