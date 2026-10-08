import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react'
import {
  listAdminUsers,
  normalizeAdminApiError,
} from '../api/adminApi'
import type { Pagination } from '../api/api.types'
import type {
  AdminApiErrorShape,
  AdminUserAccount,
} from '../models/adminUser'
import type { AdminEmployeeFilters } from '../models/adminEmployee'

const PAGE_SIZE = 100

const DEFAULT_PAGINATION: Pagination = {
  page: 1,
  pageSize: PAGE_SIZE,
  totalItems: 0,
  totalPages: 0,
}

const DEFAULT_FILTERS: AdminEmployeeFilters = {
  status: 'ALL',
  role: 'ALL',
  search: '',
}

export function useAdminEmployees() {
  const [accounts, setAccounts] = useState<AdminUserAccount[]>([])
  const [pagination, setPagination] = useState<Pagination>(DEFAULT_PAGINATION)
  const [requestedPage, setRequestedPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<AdminApiErrorShape | null>(null)
  const [filters, setFilters] = useState<AdminEmployeeFilters>(DEFAULT_FILTERS)

  const load = useCallback(async (page = 1) => {
    const targetPage = Number.isFinite(page)
      ? Math.max(1, Math.trunc(page))
      : 1

    // Keep the attempted page independently from the last successful server
    // pagination. If page N fails, Refresh/Try again must retry page N instead
    // of silently falling back to the previous successful page.
    setRequestedPage(targetPage)
    setLoading(true)
    setError(null)

    try {
      const response = await listAdminUsers({
        page: targetPage,
        pageSize: PAGE_SIZE,
      })
      const nextPagination = response.pagination ?? {
        ...DEFAULT_PAGINATION,
        page: targetPage,
      }

      setAccounts((response.data ?? []).filter((account) => Boolean(account.profile?.employeeId)))
      setPagination(nextPagination)
      setRequestedPage(nextPagination.page)
    } catch (requestError) {
      setAccounts([])
      setPagination((current) => ({
        ...current,
        page: targetPage,
      }))
      setError(normalizeAdminApiError(requestError))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load(1)
  }, [load])

  const visibleAccounts = useMemo(() => {
    const needle = filters.search.trim().toLowerCase()

    return accounts.filter((account) => {
      if (filters.status !== 'ALL' && account.status !== filters.status) {
        return false
      }

      if (filters.role !== 'ALL' && account.role !== filters.role) {
        return false
      }

      if (!needle) {
        return true
      }

      return [
        account.userAccountId,
        account.profile?.employeeId ?? '',
        account.profile?.fullName ?? '',
        account.email,
        account.phoneNumber,
        account.profile?.facilityId ?? '',
      ].some((value) => value.toLowerCase().includes(needle))
    })
  }, [accounts, filters])

  const refresh = useCallback(
    () => load(requestedPage),
    [load, requestedPage],
  )

  return {
    accounts: visibleAccounts,
    rawAccounts: accounts,
    pagination,
    loading,
    error,
    filters,
    setFilters,
    loadPage: load,
    refresh,
  }
}
