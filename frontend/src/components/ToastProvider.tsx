import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import type { ReactNode } from 'react'
import { ToastContext } from '../contexts/toast'
import type { ToastTone } from '../contexts/toast'

type Toast = {
  id: number
  message: string
  tone: ToastTone
}

type ToastProviderProps = {
  children: ReactNode
}

type ToastItemProps = {
  toast: Toast
  onDismiss: (id: number) => void
}

function ToastItem({ toast, onDismiss }: ToastItemProps) {
  const [isClosing, setIsClosing] = useState(false)

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      if (isClosing) {
        onDismiss(toast.id)
      } else {
        setIsClosing(true)
      }
    }, isClosing ? 250 : 3750)

    return () => window.clearTimeout(timeoutId)
  }, [isClosing, onDismiss, toast.id])

  return (
    <div
      className={`app-toast toast-${toast.tone}${isClosing ? ' toast-exit' : ''}`}
      role={toast.tone === 'error' ? 'alert' : 'status'}
      aria-live={toast.tone === 'error' ? 'assertive' : 'polite'}
    >
      <span className="toast-icon" aria-hidden="true">
        {toast.tone === 'success' ? (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <polyline points="20 6 9 17 4 12" />
          </svg>
        ) : (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <line x1="12" y1="7" x2="12" y2="13" />
            <circle cx="12" cy="17" r="0.5" fill="currentColor" />
          </svg>
        )}
      </span>
      <span className="toast-message">{toast.message}</span>
      <button
        type="button"
        className="toast-close"
        aria-label="Dismiss notification"
        onClick={() => setIsClosing(true)}
        disabled={isClosing}
      >
        ×
      </button>
    </div>
  )
}

export function ToastProvider({ children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const nextToastId = useRef(0)

  const dismissToast = useCallback((id: number) => {
    setToasts((currentToasts) =>
      currentToasts.filter((toast) => toast.id !== id),
    )
  }, [])

  const showToast = useCallback(
    (message: string, tone: ToastTone = 'success') => {
      nextToastId.current += 1

      const newToast = {
        id: nextToastId.current,
        message,
        tone,
      }

      setToasts((currentToasts) =>
        [...currentToasts, newToast].slice(-5),
      )
    },
    [],
  )

  const contextValue = useMemo(
    () => ({ showToast }),
    [showToast],
  )

  return (
    <ToastContext.Provider value={contextValue}>
      {children}
      <div className="toast-region" aria-label="Notifications">
        {toasts.map((toast) => (
          <ToastItem
            key={toast.id}
            toast={toast}
            onDismiss={dismissToast}
          />
        ))}
      </div>
    </ToastContext.Provider>
  )
}
