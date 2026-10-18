import { useEffect, useState } from 'react'
import { getDepartments } from '../api/departments'
import type { DepartmentItem } from '../api/departments'

export function useDepartments() {
  const [departments, setDepartments] = useState<DepartmentItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let isActive = true

    getDepartments()
      .then((result) => {
        if (isActive) {
          setDepartments(result)
          setErrorMessage('')
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setErrorMessage(
            error instanceof Error
              ? error.message
              : 'Unable to load departments.',
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
  }, [reloadKey])

  function retryLoadDepartments() {
    setIsLoading(true)
    setErrorMessage('')
    setReloadKey((currentKey) => currentKey + 1)
  }

  return {
    departments,
    isLoading,
    errorMessage,
    isUnavailable: errorMessage !== '',
    retryLoadDepartments,
  }
}
