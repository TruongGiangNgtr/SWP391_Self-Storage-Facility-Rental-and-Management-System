import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useLocation } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import type { AuthUser } from '../auth/auth.types'
import { httpClient } from '../api/httpClient'
import type { ApiCollectionResponse } from '../api/api.types'
import { catalogApi } from '../api/catalogApi'
import { contractApi } from '../api/contractApi'
import { businessApi } from '../api/businessApi'
import { listAdminUsers, listAllAdminFacilities } from '../api/adminApi'
import { storageUnitApi } from '../api/storageUnitApi'
import { authToken } from '../api/authToken'
import type { HandoverContractDetail } from '../features/handover/contract.types'
import { employeeContractLabel, employeeText, employeeUnitLabel } from '../utils/employeePresentation'

type Kind = 'facility' | 'unitType' | 'customer' | 'employee' | 'account' | 'unit' | 'contract' | 'discount' | 'contractUnit' | 'policy'
type Entry = { id: string; label: string }
type Page = { entries: Entry[]; totalPages: number }
type Reader = (page: number) => Promise<Page>
type Resolver = (kind: Kind, id: string, scope?: string) => Promise<string | undefined>
const Labels = createContext<Resolver | null>(null)

// Page through a directory only as far as requested names require. Concurrent
// cells share each page read, rather than issuing a detail request per row.
function directory(read: Reader) {
  const entries = new Map<string, string>()
  let nextPage = 1
  let totalPages = Infinity
  let pending: Promise<void> | undefined
  return async (id: string) => {
    while (!entries.has(id) && nextPage <= totalPages) {
      if (!pending) {
        pending = read(nextPage).then(result => {
          result.entries.forEach(entry => { const label = employeeText(entry.label, ''); if (label) entries.set(entry.id, label) })
          totalPages = result.totalPages
          nextPage += 1
        }).finally(() => { pending = undefined })
      }
      await pending
    }
    return entries.get(id)
  }
}

function collection<T>(read: (page: number) => Promise<ApiCollectionResponse<T>>, entry: (item: T) => Entry): Reader {
  return async page => { const result = await read(page); return { entries: result.data.map(entry), totalPages: result.pagination.totalPages } }
}

function createResolver(user: AuthUser): Resolver {
  const sessionToken = authToken.get()
  const sameSession = () => authToken.get() === sessionToken
  const facility = user.facilityId
  const manager = user.role === 'FACILITY_MANAGER'
  const business = user.role === 'BUSINESS_OPERATIONS_MANAGER'
  const admin = user.role === 'SYSTEM_ADMINISTRATOR'
  const readers: Partial<Record<Kind, Reader>> = {
    facility: admin
      ? async () => ({ entries: (await listAllAdminFacilities()).map(row => ({ id: row.facilityId, label: row.name })), totalPages: 1 })
      : collection(page => business ? businessApi.listFacilities(page, 100) : catalogApi.listFacilities({ page, pageSize: 100 }), row => ({ id: row.facilityId, label: row.name })),
  }
  if (!admin) {
    readers.unitType = collection(page => business ? businessApi.listUnitTypes(page, 100)
      : catalogApi.listFacilityUnitTypes(facility!, { page, pageSize: 100 }), row => ({ id: row.unitTypeId, label: row.name }))
    if (business || facility) readers.customer = collection(
      page => httpClient.get<ApiCollectionResponse<{ customerId: string; fullName: string }>>(
        business ? `/business/customers?page=${page}&pageSize=100` : `/facilities/${encodeURIComponent(facility!)}/customers?page=${page}&pageSize=100`),
      row => ({ id: row.customerId, label: row.fullName }),
    )
    if (manager && facility) readers.employee = collection(
      page => httpClient.get<ApiCollectionResponse<{ employeeId: string; fullName: string }>>(`/facilities/${encodeURIComponent(facility)}/staff/lookup?page=${page}&pageSize=100`),
      row => ({ id: row.employeeId, label: row.fullName }),
    )
    if (manager || business) readers.contract = collection(page => contractApi.list(page, 100), row => ({ id: row.contractId, label: employeeContractLabel(row) }))
    if (business) readers.policy = collection(page => businessApi.listPolicies(page, 100), row => ({ id: row.policyId, label: `Policy version ${row.version}` }))
    if (manager && facility) readers.unit = collection(page => storageUnitApi.listByFacility(facility, page, 100), row => ({ id: row.storageUnitId, label: row.unitCode }))
  } else {
    readers.account = collection(page => listAdminUsers({ page, pageSize: 100 }), row => ({ id: row.userAccountId, label: row.profile?.fullName?.trim() || row.email || row.phoneNumber || '' }))
  }
  const directories = new Map<string, ReturnType<typeof directory>>()
  const details = new Map<string, Promise<HandoverContractDetail>>()
  return async (kind, id, scope) => {
    if (!sameSession()) return undefined
    if (kind === 'employee' && id === user.employeeId && user.fullName?.trim()) return user.fullName
    // Staff can read a linked Contract, but cannot list Contracts or other Staff.
    if ((kind === 'contract' || kind === 'contractUnit') && user.role === 'FACILITY_STAFF') {
      if (!details.has(id)) details.set(id, contractApi.get(id).then(result => result.data).catch(error => { details.delete(id); throw error }))
      const contract = await details.get(id)!
      return sameSession() ? (kind === 'contractUnit' ? employeeUnitLabel(contract) : employeeContractLabel(contract)) : undefined
    }
    const cacheKey = `${kind}:${scope ?? ''}`
    const read = kind === 'discount' && scope && !admin
      ? collection(page => businessApi.listCustomerDiscounts(scope, page), row => ({ id: row.discountId, label: `${row.name} · ${row.percentage}% · ${row.effectiveFrom.slice(0, 10)}${row.effectiveTo ? ` – ${row.effectiveTo.slice(0, 10)}` : ''}` }))
      : readers[kind]
    if (!read || (kind === 'unitType' && !business && !facility)) return undefined
    if (!directories.has(cacheKey)) directories.set(cacheKey, directory(page => {
      if (!sameSession()) return Promise.reject(new Error('Session changed'))
      return read(page)
    }))
    return directories.get(cacheKey)!(id)
  }
}

function ScopedLabels({ children, user }: { children: ReactNode; user: AuthUser }) {
  const resolve = useMemo(() => createResolver(user), [user])
  return <Labels.Provider value={resolve}>{children}</Labels.Provider>
}

export function EmployeeLabelsProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const location = useLocation()
  // Discard directory names on navigation, account, role or Facility changes.
  return user ? <ScopedLabels key={`${user.userAccountId}:${user.role}:${user.facilityId}:${location.pathname}`} user={user}>{children}</ScopedLabels> : children
}

export function EmployeeEntityLabel({ kind, id, scope, empty = 'Not assigned', fallback = 'Name unavailable' }: { kind: Kind; id?: string | null; scope?: string; empty?: string; fallback?: string }) {
  const resolve = useContext(Labels)
  const key = `${kind}:${scope}:${id}`
  const [result, setResult] = useState<{ key: string; label?: string } | null>(null)
  useEffect(() => {
    if (!id || !resolve) return
    let active = true
    resolve(kind, id, scope).then(label => { if (active) setResult({ key, label }) })
      .catch(() => { if (active) setResult({ key }) })
    return () => { active = false }
  }, [resolve, kind, id, scope, key])
  if (!id) return <>{empty}</>
  if (resolve && result?.key !== key) return <span role="status">Loading name…</span>
  if (result?.key === key && employeeText(result.label, '')) return <>{employeeText(result.label)}</>
  return <span title="Reload the page to retry loading this name">{fallback}</span>
}

export function EmployeeReservationLabel({ customerId, customerName }: { customerId?: string | null; customerName?: string | null }) {
  const name = employeeText(customerName, '')
  return name ? <>{name}</> : <EmployeeEntityLabel kind="customer" id={customerId} empty="—" fallback="—" />
}
