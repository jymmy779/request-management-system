import { useEffect, useState } from 'react'
import {
  getApprovalHistory,
  getMyApprovals,
  getRequests,
} from '../api/requests'
import type { RequestItem, RequestQuery } from '../api/requests'

export type RequestListMode =
  | 'allRequests'
  | 'myApprovals'
  | 'approvalHistory'

const defaultQuery: RequestQuery = {
  sortBy: 'createdAt',
  sortDirection: 'desc',
}

export function useRequestList(
  mode: RequestListMode,
  pageSize = 20,
) {
  const [requests, setRequests] = useState<RequestItem[]>([])
  const [currentPage, setCurrentPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [query, setQuery] = useState<RequestQuery>(defaultQuery)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    let isActive = true
    const loadPage =
      mode === 'myApprovals'
        ? getMyApprovals
        : mode === 'approvalHistory'
          ? getApprovalHistory
          : getRequests

    loadPage({ ...query, page: currentPage, pageSize })
      .then((result) => {
        if (isActive) {
          setRequests(result.items)
          setTotalCount(result.totalCount)
          setTotalPages(result.totalPages)
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setErrorMessage(
            error instanceof Error
              ? error.message
              : mode === 'myApprovals'
                ? 'Unable to load approvals.'
                : mode === 'approvalHistory'
                  ? 'Unable to load approval history.'
                  : 'Unable to load requests.',
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
  }, [currentPage, mode, pageSize, query, reloadKey])

  function retryLoadRequests() {
    setIsLoading(true)
    setErrorMessage('')
    setReloadKey((currentKey) => currentKey + 1)
  }

  function goToPage(page: number) {
    setIsLoading(true)
    setErrorMessage('')
    setCurrentPage(page)
  }

  function applyQuery(nextQuery: RequestQuery) {
    setIsLoading(true)
    setErrorMessage('')
    setCurrentPage(1)
    setQuery(nextQuery)
  }

  return {
    requests,
    currentPage,
    totalCount,
    totalPages,
    isLoading,
    errorMessage,
    retryLoadRequests,
    goToPage,
    applyQuery,
  }
}

export function useAllRequests(pageSize = 20) {
  return useRequestList('allRequests', pageSize)
}

export function useMyApprovals(pageSize = 20) {
  return useRequestList('myApprovals', pageSize)
}

export function useApprovalHistory(pageSize = 20) {
  return useRequestList('approvalHistory', pageSize)
}
