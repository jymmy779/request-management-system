import { apiUrl } from './config'
import type { PagedResult } from './requests'

export type UserRole = 'Employee' | 'Manager' | 'Admin'

export type UserItem = {
  id: string
  fullName: string
  email: string
  role: UserRole | ''
  departmentId: number | null
  departmentName: string | null
}

export type UserQuery = {
  page?: number
  pageSize?: number
  search?: string
  role?: UserRole
  departmentId?: number
  sortBy?: 'fullName' | 'email'
  sortDirection?: 'asc' | 'desc'
}

export type CreateUserInput = {
  fullName: string
  email: string
  password: string
  role: UserRole
  departmentId: number | null
}

type ApiProblem = {
  detail?: string
  title?: string
  errors?: Record<string, string[]>
}

export async function getUsers(
  query: UserQuery = {},
): Promise<PagedResult<UserItem>> {
  const searchParameters = new URLSearchParams()

  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== '') {
      searchParameters.set(key, String(value))
    }
  }

  const queryString = searchParameters.toString()
  const path = queryString ? `/users?${queryString}` : '/users'

  const response = await fetch(apiUrl(path), {
    credentials: 'include',
  })

  if (response.status === 401) {
    throw new Error('Your session has expired. Please sign in again.')
  }

  if (response.status === 403) {
    throw new Error('Only administrators can view users.')
  }

  if (!response.ok) {
    throw new Error('Unable to load users.')
  }

  return (await response.json()) as PagedResult<UserItem>
}

export async function createUser(input: CreateUserInput): Promise<UserItem> {
  const response = await fetch(apiUrl('/users'), {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(input),
  })

  if (response.status === 401) {
    throw new Error('Your session has expired. Please sign in again.')
  }

  if (response.status === 403) {
    throw new Error('Only administrators can create users.')
  }

  if (!response.ok) {
    const problem = (await response
      .json()
      .catch(() => null)) as ApiProblem | null
    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat()[0]
      : undefined

    throw new Error(
      validationMessage ??
        problem?.detail ??
        problem?.title ??
        'Unable to create the user.',
    )
  }

  return (await response.json()) as UserItem
}
