import { Navigate, Route, Routes } from 'react-router'
import type { CurrentUser, LoginRequest } from '../api/auth'
import { AppLayout } from '../layouts/AppLayout'
import { ApprovalHistoryPage } from '../pages/ApprovalHistoryPage'
import { ChangePasswordPage } from '../pages/ChangePasswordPage'
import { CreateRequestPage } from '../pages/CreateRequestPage'
import { CreateUserPage } from '../pages/CreateUserPage'
import { DepartmentsPage } from '../pages/DepartmentsPage'
import { EditRequestPage } from '../pages/EditRequestPage'
import { LoginPage } from '../pages/LoginPage'
import { MyApprovalsPage } from '../pages/MyApprovalsPage'
import { ProfilePage } from '../pages/ProfilePage'
import { RequestDetailPage } from '../pages/RequestDetailPage'
import { RequestsPage } from '../pages/RequestsPage'
import { UsersPage } from '../pages/UsersPage'

type AppRoutesProps = {
  currentUser: CurrentUser | null
  sessionError: string
  onLogin: (loginRequest: LoginRequest) => Promise<void>
  onLogout: () => Promise<void>
  onSessionInvalidated: () => void
}

export function AppRoutes({
  currentUser,
  sessionError,
  onLogin,
  onLogout,
  onSessionInvalidated,
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
          path="/profile"
          element={<ProfilePage currentUser={currentUser!} />}
        />
        <Route
          path="/profile/security"
          element={
            <ChangePasswordPage onPasswordChanged={onSessionInvalidated} />
          }
        />
        <Route path="/departments" element={<DepartmentsPage />} />
        <Route
          path="/users"
          element={
            currentUser?.role === 'Admin' ? (
              <UsersPage />
            ) : (
              <Navigate to="/requests" replace />
            )
          }
        />
        <Route
          path="/users/new"
          element={
            currentUser?.role === 'Admin' ? (
              <CreateUserPage />
            ) : (
              <Navigate to="/requests" replace />
            )
          }
        />
        <Route
          path="/my-approvals"
          element={
            currentUser?.role === 'Manager' || currentUser?.role === 'Admin' ? (
              <MyApprovalsPage />
            ) : (
              <Navigate to="/requests" replace />
            )
          }
        />
        <Route
          path="/approval-history"
          element={
            currentUser?.role === 'Manager' || currentUser?.role === 'Admin' ? (
              <ApprovalHistoryPage />
            ) : (
              <Navigate to="/requests" replace />
            )
          }
        />
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
