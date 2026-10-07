import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react'
import {
  getAdminUser,
  listAdminUsers,
  normalizeAdminApiError,
} from '../api/adminApi'
import type { Pagination } from '../api/api.types'
import type {
  AdminApiErrorShape,
  AdminUserAccount,
  AdminUserFilters,
} from '../models/adminUser'

const DEFAULT_PAGINATION: Pagination = {
  page: 1,
  pageSize: 20,
  totalItems: 0,
  totalPages: 0,
}

const DEFAULT_FILTERS: AdminUserFilters = {
  status: 'ALL',
  role: 'ALL',
  search: '',
}

export function useAdminUsers() {
  const [accounts, setAccounts] = useState<AdminUserAccount[]>([])
  const [pagination, setPagination] = useState<Pagination>(DEFAULT_PAGINATION)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<AdminApiErrorShape | null>(null)
  const [filters, setFilters] = useState<AdminUserFilters>(DEFAULT_FILTERS)

  const load = useCallback(async (page = 1) => {
    setLoading(true)
    setError(null)

    try {
      const response = await listAdminUsers({ page, pageSize: 20 })
      setAccounts(response.data ?? [])
      setPagination(response.pagination ?? { ...DEFAULT_PAGINATION, page })
    } catch (requestError) {
      setAccounts([])
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
        account.profile?.fullName ?? '',
        account.email,
        account.phoneNumber,
        account.profile?.customerId ?? '',
        account.profile?.employeeId ?? '',
        account.profile?.facilityId ?? '',
      ].some((value) => value.toLowerCase().includes(needle))
    })
  }, [accounts, filters])

  const refresh = useCallback(
    () => load(pagination.page || 1),
    [load, pagination.page],
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
