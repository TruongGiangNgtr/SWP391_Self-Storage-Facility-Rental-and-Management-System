import { useCallback, useEffect, useMemo, useState, type Dispatch, type SetStateAction } from 'react'
import { listAdminUsers, normalizeAdminApiError } from '../api/adminApi'
import type { Pagination } from '../api/api.types'
import type { AdminApiErrorShape, AdminUserAccount, UserAccountStatus } from '../models/adminUser'
import { fetchAllEmployeePages } from '../utils/employeePresentation'

const PAGE_SIZE = 20

export function useAdminCustomers() {
  const [accounts, setAccounts] = useState<AdminUserAccount[]>([])
  const [requestedPage, setRequestedPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<AdminApiErrorShape | null>(null)
  const [statusState, setStatusState] = useState<'ALL' | UserAccountStatus>('ALL')
  const [searchState, setSearchState] = useState('')

  const setStatus: Dispatch<SetStateAction<'ALL' | UserAccountStatus>> = useCallback((next) => {
    setRequestedPage(1)
    setStatusState(next)
  }, [])

  const setSearch: Dispatch<SetStateAction<string>> = useCallback((next) => {
    setRequestedPage(1)
    setSearchState(next)
  }, [])

  const fetchAll = useCallback(async (page = 1) => {
    const targetPage = Math.max(1, Math.trunc(page))
    setRequestedPage(targetPage)
    setLoading(true)
    setError(null)
    try {
      const allAccounts = await fetchAllEmployeePages((nextPage, pageSize) =>
        listAdminUsers({ page: nextPage, pageSize }),
      )
      setAccounts(allAccounts.items.filter((account) => account.role === 'CUSTOMER' && Boolean(account.profile?.customerId)))
    } catch (requestError) {
      setAccounts([])
      setError(normalizeAdminApiError(requestError))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void fetchAll(1) }, [fetchAll])

  const filteredAccounts = useMemo(() => {
    const needle = searchState.trim().toLowerCase()
    return accounts.filter((account) => {
      if (statusState !== 'ALL' && account.status !== statusState) return false
      if (!needle) return true
      return [
        account.userAccountId,
        account.profile?.customerId ?? '',
        account.profile?.fullName ?? '',
        account.email,
        account.phoneNumber,
        account.profile?.cccd ?? '',
      ].some((value) => value.toLowerCase().includes(needle))
    })
  }, [accounts, searchState, statusState])

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
    setRequestedPage(Math.max(1, Math.trunc(page)))
  }, [])

  const refresh = useCallback(() => fetchAll(safePage || 1), [fetchAll, safePage])

  return {
    accounts: visibleAccounts,
    rawAccounts: accounts,
    pagination,
    loading,
    error,
    status: statusState,
    setStatus,
    search: searchState,
    setSearch,
    loadPage,
    refresh,
  }
}
