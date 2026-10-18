import { useEffect, useState } from 'react'
import { getUsers } from '../api/users'
import type { UserItem, UserQuery } from '../api/users'

const defaultQuery: UserQuery = {
  sortBy: 'fullName',
  sortDirection: 'asc',
}

export function useUsers(pageSize = 20) {
  const [users, setUsers] = useState<UserItem[]>([])
  const [currentPage, setCurrentPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [query, setQuery] = useState<UserQuery>(defaultQuery)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    let isActive = true

    getUsers({ ...query, page: currentPage, pageSize })
      .then((result) => {
        if (isActive) {
          setUsers(result.items)
          setTotalCount(result.totalCount)
          setTotalPages(result.totalPages)
          setErrorMessage('')
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setErrorMessage(
            error instanceof Error ? error.message : 'Unable to load users.',
          )
        }
      })
      .finally(() => {
        if (isActive) {
          setIsLoading(false)
        }
      })

    return () => {
      isActive = false
    }
  }, [currentPage, pageSize, query, reloadKey])

  function retryLoadUsers() {
    setIsLoading(true)
    setErrorMessage('')
    setReloadKey((currentKey) => currentKey + 1)
  }

  function goToPage(page: number) {
    setIsLoading(true)
    setErrorMessage('')
    setCurrentPage(page)
  }

  function applyQuery(nextQuery: UserQuery) {
    setIsLoading(true)
    setErrorMessage('')
    setCurrentPage(1)
    setQuery(nextQuery)
  }

  return {
    users,
    currentPage,
    totalCount,
    totalPages,
    query,
    isLoading,
    errorMessage,
    retryLoadUsers,
    goToPage,
    applyQuery,
  }
}
