import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
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

export type EmployeeLabelKind = 'facility' | 'unitType' | 'customer' | 'employee' | 'account' | 'unit' | 'contract' | 'discount' | 'contractUnit' | 'policy'
type Kind = EmployeeLabelKind
type Entry = { id: string; label: string }
type Page = { entries: Entry[]; totalPages: number }
type Reader = (page: number) => Promise<Page>
type LabelResult = { label?: string; more?: boolean }
type Resolver = (kind: Kind, id: string, scope?: string, advance?: boolean) => Promise<LabelResult>
type LabelStore = {
  resolve: Resolver
  getCached: (kind: Kind, id: string, scope?: string) => string | undefined
  subscribe: (listener: () => void) => () => void
}
const Labels = createContext<LabelStore | null>(null)
const MAX_DIRECTORY_PAGES = 50

// Automatically advance through directory pages while sharing in-flight requests
// across concurrent cells; known detail endpoints remain bounded fallbacks.
function directory(
  read: Reader,
  lookup?: (id: string) => Promise<string | undefined>,
  onEntry?: (id: string, label: string) => void,
) {
  const entries = new Map<string, string>()
  let nextPage = 1
  let totalPages = Infinity
  let pending: Promise<void> | undefined
  return async (id: string): Promise<LabelResult> => {
    while (!entries.has(id) && nextPage <= totalPages && nextPage <= MAX_DIRECTORY_PAGES) {
      if (!pending) {
        const pageToRead = nextPage
        pending = read(pageToRead).then(result => {
          result.entries.forEach(entry => {
            const label = employeeText(entry.label, '')
            if (label) {
              entries.set(entry.id, label)
              onEntry?.(entry.id, label)
            }
          })
          totalPages = result.entries.length === 0 ? pageToRead : result.totalPages
          nextPage = pageToRead + 1
        }).finally(() => { pending = undefined })
      }
      await pending
    }
    const label = entries.get(id)
    if (!label && lookup) {
      const resolved = employeeText(await lookup(id), '')
      if (resolved) {
        entries.set(id, resolved)
        onEntry?.(id, resolved)
      }
      return { label: resolved || undefined }
    }
    return { label, more: false }
  }
}

function collection<T>(read: (page: number) => Promise<ApiCollectionResponse<T>>, entry: (item: T) => Entry): Reader {
  return async page => { const result = await read(page); return { entries: result.data.map(entry), totalPages: result.pagination.totalPages } }
}

function createLabelStore(user: AuthUser): LabelStore {
  const route = window.location.pathname
  const sessionToken = authToken.get()
  const sameSession = () => authToken.get() === sessionToken && window.location.pathname === route
  const facility = user.facilityId
  const manager = user.role === 'FACILITY_MANAGER'
  const business = user.role === 'BUSINESS_OPERATIONS_MANAGER'
  const admin = user.role === 'SYSTEM_ADMINISTRATOR'
  const cache = new Map<string, string>()
  const listeners = new Set<() => void>()
  let notifyScheduled = false

  const notify = () => {
    if (notifyScheduled) return
    notifyScheduled = true
    queueMicrotask(() => {
      notifyScheduled = false
      listeners.forEach(listener => listener())
    })
  }

  const recordCache = (kind: Kind, scope: string | undefined, id: string, label: string | undefined) => {
    const clean = employeeText(label, '')
    if (!clean) return
    const key = `${kind}:${scope ?? ''}:${id}`
    if (cache.get(key) !== clean) {
      cache.set(key, clean)
      notify()
    }
  }

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
    if (facility) readers.employee = collection(
      page => httpClient.get<ApiCollectionResponse<{ employeeId: string; fullName: string }>>(`/facilities/${encodeURIComponent(facility)}/staff/lookup?page=${page}&pageSize=100`),
      row => ({ id: row.employeeId, label: row.fullName }),
    )
    readers.contract = collection(page => contractApi.list(page, 100), row => {
      const unitLabel = employeeUnitLabel(row)
      if (unitLabel !== '—') recordCache('contractUnit', undefined, row.contractId, unitLabel)
      return { id: row.contractId, label: employeeContractLabel(row) }
    })
    readers.contractUnit = collection(page => contractApi.list(page, 100), row => {
      const contractLabel = employeeContractLabel(row)
      if (contractLabel !== '—') recordCache('contract', undefined, row.contractId, contractLabel)
      return { id: row.contractId, label: employeeUnitLabel(row) }
    })
    if (business) readers.policy = collection(page => businessApi.listPolicies(page, 100), row => ({ id: row.policyId, label: `Policy version ${row.version}` }))
    if (manager && facility) readers.unit = collection(page => storageUnitApi.listByFacility(facility, page, 100), row => ({ id: row.storageUnitId, label: row.unitCode }))
  }
  if (admin) readers.account = collection(page => listAdminUsers({ page, pageSize: 100 }), row => ({ id: row.userAccountId, label: row.profile?.fullName?.trim() || row.email || row.phoneNumber || '' }))
  const directories = new Map<string, ReturnType<typeof directory>>()
  const details = new Map<string, Promise<HandoverContractDetail>>()
  const accounts = new Map<string, Promise<string>>()
  const units = new Map<string, Promise<string>>()
  const resolve: Resolver = async (kind, id, scope) => {
    if (!sameSession()) return {}
    const cached = cache.get(`${kind}:${scope ?? ''}:${id}`)
    if (cached) return { label: cached }
    if (kind === 'employee' && id === user.employeeId && user.fullName?.trim()) {
      recordCache(kind, scope, id, user.fullName)
      return { label: user.fullName }
    }
    // Detail promises are deduplicated for records absent from the shared directory pages.
    const lookup = kind === 'account' && admin ? async (targetId: string) => {
      if (!accounts.has(targetId)) accounts.set(targetId, getAdminUser(targetId).then(({ data }) =>
        data.profile?.fullName?.trim() || data.email || data.phoneNumber || '').catch(error => { accounts.delete(targetId); throw error }))
      const label = await accounts.get(targetId)!
      return sameSession() ? label : undefined
    } : (kind === 'contract' || kind === 'contractUnit') && !admin ? async (targetId: string) => {
      if (!details.has(targetId)) details.set(targetId, contractApi.get(targetId).then(result => result.data).catch(error => { details.delete(targetId); throw error }))
      const contract = await details.get(targetId)!
      return sameSession() ? (kind === 'contractUnit' ? employeeUnitLabel(contract) : employeeContractLabel(contract)) : undefined
    } : kind === 'unit' && manager ? async (targetId: string) => {
      if (!units.has(targetId)) units.set(targetId, storageUnitApi.get(targetId).then(result => result.data.unitCode).catch(error => { units.delete(targetId); throw error }))
      const label = await units.get(targetId)!
      return sameSession() ? label : undefined
    } : undefined
    if (lookup && !readers[kind]) {
      const resolved = await lookup(id)
      recordCache(kind, scope, id, resolved)
      return { label: resolved }
    }
    const cacheKey = `${kind}:${scope ?? ''}`
    const read = kind === 'discount' && scope && !admin
      ? collection(page => businessApi.listCustomerDiscounts(scope, page), row => ({ id: row.discountId, label: `${row.name} · ${row.percentage}% · ${row.effectiveFrom.slice(0, 10)}${row.effectiveTo ? ` – ${row.effectiveTo.slice(0, 10)}` : ''}` }))
      : readers[kind]
    if (!read || (kind === 'unitType' && !business && !facility)) return {}
    if (!directories.has(cacheKey)) directories.set(cacheKey, directory(
      page => {
        if (!sameSession()) return Promise.reject(new Error('Session changed'))
        return read(page)
      },
      lookup,
      (entryId, entryLabel) => recordCache(kind, scope, entryId, entryLabel),
    ))
    const result = await directories.get(cacheKey)!(id)
    if (result.label) recordCache(kind, scope, id, result.label)
    return sameSession() ? result : {}
  }

  return {
    resolve,
    getCached: (kind, id, scope) => cache.get(`${kind}:${scope ?? ''}:${id}`),
    subscribe: listener => {
      listeners.add(listener)
      return () => { listeners.delete(listener) }
    },
  }
}

function ScopedLabels({ children, user }: { children: ReactNode; user: AuthUser }) {
  const store = useMemo(() => createLabelStore(user), [user])
  return <Labels.Provider value={store}>{children}</Labels.Provider>
}

export function EmployeeLabelsProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const location = useLocation()
  // Discard directory names on navigation, account, role or Facility changes.
  return user ? <ScopedLabels key={`${user.userAccountId}:${user.role}:${user.facilityId}:${location.pathname}`} user={user}>{children}</ScopedLabels> : children
}

export function useEmployeeLabelLookup() {
  const store = useContext(Labels)
  const [, setVersion] = useState(0)
  const requestedRef = useRef(new Set<string>())

  useEffect(() => {
    requestedRef.current.clear()
    if (!store) return undefined
    return store.subscribe(() => setVersion(value => value + 1))
  }, [store])

  return useCallback((kind: Kind, id?: string | null, scope?: string): string => {
    if (!id || !store) return ''
    const cached = store.getCached(kind, id, scope)
    if (cached) return cached
    const requestKey = `${kind}:${scope ?? ''}:${id}`
    if (!requestedRef.current.has(requestKey)) {
      requestedRef.current.add(requestKey)
      void store.resolve(kind, id, scope).catch(() => {
        requestedRef.current.delete(requestKey)
      })
    }
    return ''
  }, [store])
}

export function EmployeeEntityLabel({ kind, id, scope, empty = 'Not assigned', fallback = 'Name unavailable' }: { kind: Kind; id?: string | null; scope?: string; empty?: string; fallback?: string }) {
  const store = useContext(Labels)
  const resolve = store?.resolve
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
  const cached = store?.getCached(kind, id, scope)
  if (cached) return <>{cached}</>
  if (resolve && result?.key !== key) return <span role="status">Loading name...</span>
  if (result?.key === key && employeeText(result.label, '')) return <>{employeeText(result.label)}</>
  if (resolve && result?.key === key && result.failed) return <span className="employee-label-continuation">
    <span>Name could not be loaded.</span>
    <button type="button" className="employee-label-load" aria-label={`Retry loading ${kind} name`}
      onClick={() => setRequested(current => ({ identity, round: current.identity === identity ? current.round + 1 : 1 }))}>
      Retry name
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
