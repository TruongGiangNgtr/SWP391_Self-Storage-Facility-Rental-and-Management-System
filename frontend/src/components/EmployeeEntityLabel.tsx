import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useLocation } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import type { AuthUser } from '../auth/auth.types'
import { httpClient } from '../api/httpClient'
import type { ApiCollectionResponse } from '../api/api.types'
import { catalogApi } from '../api/catalogApi'
import { contractApi } from '../api/contractApi'
import { businessApi } from '../api/businessApi'
import { getAdminUser, listAdminUsers, type AdminFacilityLookup } from '../api/adminApi'
import { storageUnitApi } from '../api/storageUnitApi'
import { authToken } from '../api/authToken'
import type { HandoverContractDetail } from '../features/handover/contract.types'
import { employeeContractLabel, employeeText, employeeUnitLabel } from '../utils/employeePresentation'

type Kind = 'facility' | 'unitType' | 'customer' | 'employee' | 'account' | 'unit' | 'contract' | 'discount' | 'contractUnit' | 'policy'
type Entry = { id: string; label: string }
type Page = { entries: Entry[]; totalPages: number }
type Reader = (page: number) => Promise<Page>
type LabelResult = { label?: string; more?: boolean }
type Resolver = (kind: Kind, id: string, scope?: string, advance?: boolean) => Promise<LabelResult>
const Labels = createContext<Resolver | null>(null)

// Share a single automatic page across cells. Further directory pages require
// explicit user continuation; known detail endpoints remain bounded fallbacks.
function directory(read: Reader, lookup?: (id: string) => Promise<string | undefined>) {
  const entries = new Map<string, string>()
  let nextPage = 1
  let totalPages = Infinity
  let pending: Promise<void> | undefined
  return async (id: string, advance = false): Promise<LabelResult> => {
    if (!entries.has(id) && nextPage <= totalPages && (nextPage === 1 || (advance && !lookup))) {
      if (!pending) {
        pending = read(nextPage).then(result => {
          result.entries.forEach(entry => { const label = employeeText(entry.label, ''); if (label) entries.set(entry.id, label) })
          totalPages = result.totalPages
          nextPage += 1
        }).finally(() => { pending = undefined })
      }
      await pending
    }
    const label = entries.get(id)
    if (!label && lookup) return { label: await lookup(id) }
    return { label, more: !label && nextPage <= totalPages }
  }
}

function collection<T>(read: (page: number) => Promise<ApiCollectionResponse<T>>, entry: (item: T) => Entry): Reader {
  return async page => { const result = await read(page); return { entries: result.data.map(entry), totalPages: result.pagination.totalPages } }
}

function createResolver(user: AuthUser): Resolver {
  const route = window.location.pathname
  const sessionToken = authToken.get()
  const sameSession = () => authToken.get() === sessionToken && window.location.pathname === route
  const facility = user.facilityId
  const manager = user.role === 'FACILITY_MANAGER'
  const business = user.role === 'BUSINESS_OPERATIONS_MANAGER'
  const admin = user.role === 'SYSTEM_ADMINISTRATOR'
  const readers: Partial<Record<Kind, Reader>> = {
    facility: admin
      ? collection(page => httpClient.get<ApiCollectionResponse<AdminFacilityLookup>>(`/admin/facilities/lookup?page=${page}&pageSize=100`), row => ({ id: row.facilityId, label: row.name }))
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
  }
  if (admin) readers.account = collection(page => listAdminUsers({ page, pageSize: 100 }), row => ({ id: row.userAccountId, label: row.profile?.fullName?.trim() || row.email || row.phoneNumber || '' }))
  const directories = new Map<string, ReturnType<typeof directory>>()
  const details = new Map<string, Promise<HandoverContractDetail>>()
  const accounts = new Map<string, Promise<string>>()
  const units = new Map<string, Promise<string>>()
  return async (kind, id, scope, advance) => {
    if (!sameSession()) return {}
    if (kind === 'employee' && id === user.employeeId && user.fullName?.trim()) return { label: user.fullName }
    // Detail promises are deduplicated for records absent from the shared page.
    const lookup = kind === 'account' && admin ? async (id: string) => {
      if (!accounts.has(id)) accounts.set(id, getAdminUser(id).then(({ data }) =>
        data.profile?.fullName?.trim() || data.email || data.phoneNumber || '').catch(error => { accounts.delete(id); throw error }))
      const label = await accounts.get(id)!
      return sameSession() ? label : undefined
    } : (kind === 'contract' || kind === 'contractUnit') && !admin ? async (id: string) => {
      if (!details.has(id)) details.set(id, contractApi.get(id).then(result => result.data).catch(error => { details.delete(id); throw error }))
      const contract = await details.get(id)!
      return sameSession() ? (kind === 'contractUnit' ? employeeUnitLabel(contract) : employeeContractLabel(contract)) : undefined
    } : kind === 'unit' && manager ? async (id: string) => {
      if (!units.has(id)) units.set(id, storageUnitApi.get(id).then(result => result.data.unitCode).catch(error => { units.delete(id); throw error }))
      const label = await units.get(id)!
      return sameSession() ? label : undefined
    } : undefined
    if (lookup && !readers[kind]) return { label: await lookup(id) }
    const cacheKey = `${kind}:${scope ?? ''}`
    const read = kind === 'discount' && scope && !admin
      ? collection(page => businessApi.listCustomerDiscounts(scope, page), row => ({ id: row.discountId, label: `${row.name} · ${row.percentage}% · ${row.effectiveFrom.slice(0, 10)}${row.effectiveTo ? ` – ${row.effectiveTo.slice(0, 10)}` : ''}` }))
      : readers[kind]
    if (!read || (kind === 'unitType' && !business && !facility)) return {}
    if (!directories.has(cacheKey)) directories.set(cacheKey, directory(page => {
      if (!sameSession()) return Promise.reject(new Error('Session changed'))
      return read(page)
    }, lookup))
    const result = await directories.get(cacheKey)!(id, advance)
    return sameSession() ? result : {}
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
  const identity = `${kind}:${scope}:${id}`
  const [requested, setRequested] = useState({ identity: '', round: 0 })
  const round = requested.identity === identity ? requested.round : 0
  const key = `${identity}:${round}`
  const [result, setResult] = useState<(LabelResult & { key: string; failed?: boolean }) | null>(null)
  useEffect(() => {
    if (!id || !resolve) return
    let active = true
    resolve(kind, id, scope, round > 0).then(value => { if (active) setResult({ key, ...value }) })
      .catch(() => { if (active) setResult({ key, failed: true }) })
    return () => { active = false }
  }, [resolve, kind, id, scope, key, round])
  if (!id) return <>{empty}</>
  if (resolve && result?.key !== key) return <span role="status">Loading name...</span>
  if (result?.key === key && employeeText(result.label, '')) return <>{employeeText(result.label)}</>
  if (resolve && result?.key === key && (result.more || result.failed)) return <span className="employee-label-continuation">
    <span>{result.failed ? 'Name could not be loaded.' : 'Name not loaded.'}</span>
    <button type="button" className="employee-label-load" aria-label={`${result.failed ? 'Retry loading' : 'Load next page of'} ${kind} names`}
      onClick={() => setRequested(current => ({ identity, round: current.identity === identity ? current.round + 1 : 1 }))}>
      {result.failed ? 'Retry name' : 'Load more names'}
    </button>
  </span>
  return <span>{fallback}</span>
}

export function EmployeeReservationLabel({ customerId, customerName, startMonth, endMonth, visitDate }: { customerId?: string | null; customerName?: string | null; startMonth?: string | null; endMonth?: string | null; visitDate?: string | null }) {
  const name = employeeText(customerName, '')
  return <span className="employee-reservation-label">
    {name || <EmployeeEntityLabel kind="customer" id={customerId} empty="—" fallback="—" />}
    {startMonth && endMonth && <> · {startMonth} → {endMonth}</>}
    {visitDate && <> · Visit {visitDate}</>}
  </span>
}
