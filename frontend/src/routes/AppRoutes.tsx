import { Navigate, Route, Routes } from 'react-router'
import type { CurrentUser, LoginRequest } from '../api/auth'
import { AppLayout } from '../layouts/AppLayout'
import { CreateRequestPage } from '../pages/CreateRequestPage'
import { EditRequestPage } from '../pages/EditRequestPage'
import { LoginPage } from '../pages/LoginPage'
import { RequestDetailPage } from '../pages/RequestDetailPage'
import { RequestsPage } from '../pages/RequestsPage'

type AppRoutesProps = {
  currentUser: CurrentUser | null
  sessionError: string
  onLogin: (loginRequest: LoginRequest) => Promise<void>
  onLogout: () => Promise<void>
}

export function AppRoutes({
  currentUser,
  sessionError,
  onLogin,
  onLogout,
}: AppRoutesProps) {
  const defaultPath = currentUser ? '/requests' : '/login'

  return (
    <Routes>
      <Route
        path="/login"
        element={
          currentUser ? (
            <Navigate to="/requests" replace />
          ) : (
            <LoginPage onLogin={onLogin} initialError={sessionError} />
          )
        }
      />
      <Route
        element={
          currentUser ? (
            <AppLayout currentUser={currentUser} onLogout={onLogout} />
          ) : (
            <Navigate to="/login" replace />
          )
        }
      >
        <Route path="/requests" element={<RequestsPage />} />
        <Route
          path="/requests/new"
          element={<CreateRequestPage currentUser={currentUser!} />}
        />
        <Route
          path="/requests/:requestId/edit"
          element={<EditRequestPage currentUser={currentUser!} />}
        />
        <Route
          path="/requests/:requestId"
          element={<RequestDetailPage currentUser={currentUser!} />}
        />
      </Route>
      <Route path="/" element={<Navigate to={defaultPath} replace />} />
      <Route path="*" element={<Navigate to={defaultPath} replace />} />
    </Routes>
  )
}
