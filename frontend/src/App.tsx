import { useEffect, useState } from 'react'
import { getCurrentUser, login, logout } from './api/auth'
import type { CurrentUser, LoginRequest } from './api/auth'
import { AppRoutes } from './routes/AppRoutes'
import './App.css'

function App() {
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null)
  const [isCheckingSession, setIsCheckingSession] = useState(true)
  const [sessionError, setSessionError] = useState('')

  useEffect(() => {
    let isActive = true

    getCurrentUser()
      .then((user) => {
        if (isActive) {
          setCurrentUser(user)
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setSessionError(
            error instanceof Error
              ? error.message
              : 'Unable to check the current session.',
          )
        }
      })
      .finally(() => {
        if (isActive) {
          setIsCheckingSession(false)
        }
      })

    return () => {
      isActive = false
    }
  }, [])

  async function handleLogin(loginRequest: LoginRequest) {
    await login(loginRequest)

    const user = await getCurrentUser()

    if (user === null) {
      throw new Error('The session could not be loaded after sign in.')
    }

    setSessionError('')
    setCurrentUser(user)
  }

  async function handleLogout() {
    await logout()
    setSessionError('')
    setCurrentUser(null)
  }

  if (isCheckingSession) {
    return (
      <main className="app-shell">
        <section className="loading-card" role="status">
          Checking your session...
        </section>
      </main>
    )
  }

  return (
    <AppRoutes
      currentUser={currentUser}
      sessionError={sessionError}
      onLogin={handleLogin}
      onLogout={handleLogout}
    />
  )
}

export default App
