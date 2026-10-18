import { apiUrl } from './config'

export type DepartmentItem = {
  id: number
  code: string
  name: string
}

export async function getDepartments(): Promise<DepartmentItem[]> {
  const response = await fetch(apiUrl('/departments'), {
    credentials: 'include',
  })

  if (response.status === 401) {
    throw new Error('Your session has expired. Please sign in again.')
  }

  if (!response.ok) {
    throw new Error('Unable to load departments.')
  }

  return (await response.json()) as DepartmentItem[]
}
