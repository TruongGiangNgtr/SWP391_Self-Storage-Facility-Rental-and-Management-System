import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type Dispatch,
  type SetStateAction,
} from 'react'
import {
  getAdminUser,
  listAdminUsers,
  normalizeAdminApiError,
} from '../api/adminApi'
import type { Pagination } from '../api/api.types'
import { useEmployeeLabelLookup } from '../components/EmployeeEntityLabel'
import {
  USER_ROLE_LABELS,
  type AdminApiErrorShape,
  type AdminUserAccount,
  type AdminUserFilters,
} from '../models/adminUser'
import { fetchAllEmployeePages } from '../utils/employeePresentation'

const PAGE_SIZE = 20

const DEFAULT_FILTERS: AdminUserFilters = {
  status: 'ALL',
  role: 'ALL',
  search: '',
}

export function useAdminUsers() {
  const lookupLabel = useEmployeeLabelLookup()
  const [accounts, setAccounts] = useState<AdminUserAccount[]>([])
  const [requestedPage, setRequestedPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<AdminApiErrorShape | null>(null)
  const [filters, setFiltersState] = useState<AdminUserFilters>(DEFAULT_FILTERS)

  const setFilters: Dispatch<SetStateAction<AdminUserFilters>> = useCallback((next) => {
    setRequestedPage(1)
    setFiltersState(next)
  }, [])

  const fetchAll = useCallback(async (page = 1) => {
    const targetPage = Number.isFinite(page)
      ? Math.max(1, Math.trunc(page))
      : 1
    setRequestedPage(targetPage)
    setLoading(true)
    setError(null)

    try {
      const allAccounts = await fetchAllEmployeePages((nextPage, pageSize) =>
        listAdminUsers({ page: nextPage, pageSize }),
      )
      setAccounts(allAccounts.items)
    } catch (requestError) {
      setAccounts([])
      setError(normalizeAdminApiError(requestError))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchAll(1)
  }, [fetchAll])

  const filteredAccounts = useMemo(() => {
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
        account.profile?.fullName ?? '',
        account.email,
        account.phoneNumber,
        account.role,
        USER_ROLE_LABELS[account.role] ?? '',
        account.profile?.customerId ?? '',
        account.profile?.employeeId ?? '',
        account.profile?.facilityId ?? '',
        lookupLabel('facility', account.profile?.facilityId),
      ].some((value) => value.toLowerCase().includes(needle))
    })
  }, [accounts, filters, lookupLabel])

  const totalPages = Math.max(1, Math.ceil(filteredAccounts.length / PAGE_SIZE))
  const safePage = Math.min(requestedPage, totalPages)
  const visibleAccounts = useMemo(
    () => filteredAccounts.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [filteredAccounts, safePage],
  )
  const pagination: Pagination = useMemo(
    () => ({
      page: safePage,
      pageSize: PAGE_SIZE,
      totalItems: filteredAccounts.length,
      totalPages: filteredAccounts.length === 0 ? 0 : totalPages,
    }),
    [filteredAccounts.length, safePage, totalPages],
  )

  const loadPage = useCallback(async (page = 1) => {
    const targetPage = Number.isFinite(page)
      ? Math.max(1, Math.trunc(page))
      : 1
    setRequestedPage(targetPage)
  }, [])

  const refresh = useCallback(
    () => fetchAll(safePage || 1),
    [fetchAll, safePage],
  )

  return {
    accounts: visibleAccounts,
    rawAccounts: accounts,
    pagination,
    loading,
    error,
    filters,
    setFilters,
    loadPage,
    refresh,
  }
}

export function useAdminUserDetail(userAccountId: string | null) {
  const [account, setAccount] = useState<AdminUserAccount | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<AdminApiErrorShape | null>(null)

  const refresh = useCallback(async () => {
    if (!userAccountId) {
      setAccount(null)
      setError(null)
      return
    }

    setLoading(true)
    setAccount(null)
    setError(null)

    try {
      const response = await getAdminUser(userAccountId)
      setAccount(response.data)
    } catch (requestError) {
      setAccount(null)
      setError(normalizeAdminApiError(requestError))
    } finally {
      setLoading(false)
    }
  }, [userAccountId])

  useEffect(() => {
    void refresh()
  }, [refresh])

  return {
    account,
    loading,
    error,
    refresh,
  }
}
