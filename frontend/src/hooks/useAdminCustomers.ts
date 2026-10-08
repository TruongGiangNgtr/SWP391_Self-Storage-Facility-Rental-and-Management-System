import { useCallback, useEffect, useMemo, useState } from 'react'
import { listAdminUsers, normalizeAdminApiError } from '../api/adminApi'
import type { Pagination } from '../api/api.types'
import type { AdminApiErrorShape, AdminUserAccount, UserAccountStatus } from '../models/adminUser'

const PAGE_SIZE = 100
const EMPTY_PAGINATION: Pagination = { page: 1, pageSize: PAGE_SIZE, totalItems: 0, totalPages: 0 }

export function useAdminCustomers() {
  const [accounts, setAccounts] = useState<AdminUserAccount[]>([])
  const [pagination, setPagination] = useState<Pagination>(EMPTY_PAGINATION)
  const [requestedPage, setRequestedPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<AdminApiErrorShape | null>(null)
  const [status, setStatus] = useState<'ALL' | UserAccountStatus>('ALL')
  const [search, setSearch] = useState('')

  const load = useCallback(async (page = 1) => {
    const targetPage = Math.max(1, Math.trunc(page))
    setRequestedPage(targetPage)
    setLoading(true)
    setError(null)
    try {
      const response = await listAdminUsers({ page: targetPage, pageSize: PAGE_SIZE })
      setAccounts((response.data ?? []).filter((account) => account.role === 'CUSTOMER' && Boolean(account.profile?.customerId)))
      setPagination(response.pagination ?? { ...EMPTY_PAGINATION, page: targetPage })
    } catch (requestError) {
      setAccounts([])
      setError(normalizeAdminApiError(requestError))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void load(1) }, [load])

  const visibleAccounts = useMemo(() => {
    const needle = search.trim().toLowerCase()
    return accounts.filter((account) => {
      if (status !== 'ALL' && account.status !== status) return false
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
  }, [accounts, search, status])

  const refresh = useCallback(() => load(requestedPage), [load, requestedPage])

  return {
    accounts: visibleAccounts,
    rawAccounts: accounts,
    pagination,
    loading,
    error,
    status,
    setStatus,
    search,
    setSearch,
    loadPage: load,
    refresh,
  }
}
