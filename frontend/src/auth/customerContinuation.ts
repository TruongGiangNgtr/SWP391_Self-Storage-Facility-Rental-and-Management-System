import { isUuid } from '../features/handover/handoverValidation'
import { isRentalMonth } from '../features/renewal/renewalValidation'

const KEY = 'frms.customerContinuation'
const AUTH_PATHS = ['/auth/customer/login', '/auth/customer/register']
const LISTS = ['reservations', 'contracts', 'visits', 'invoices', 'support-tickets']
interface PendingOperation { version: 1; id: string; destination: string }
let signOutInProgress = false

// Keep only registered Customer routes and approved non-sensitive parameters.
// A raw Location/state, account payload, price or capacity is never retained.
export function safeCustomerDestination(value: unknown): string | null {
  if (typeof value !== 'string' || !value.startsWith('/customer') || value.includes('\\') || /\s/.test(value) ||
    Array.from(value).some(character => character.charCodeAt(0) < 32)) return null
  let url: URL
  try { url = new URL(value, 'https://frms.invalid') } catch { return null }
  if (url.origin !== 'https://frms.invalid' || url.hash || url.pathname.includes('%') || value.split('?')[0] !== url.pathname) return null
  const parts = url.pathname.split('/').slice(1)
  if (parts[0] !== 'customer') return null
  const params = url.searchParams
  if (new Set(params.keys()).size !== [...params.keys()].length) return null
  if (url.pathname === '/customer/reservations/new') {
    const keys = ['facilityId', 'unitTypeId', 'startMonth', 'endMonth']
    if ([...params.keys()].some(key => !keys.includes(key)) || !isUuid(params.get('facilityId') ?? '') || !isUuid(params.get('unitTypeId') ?? '') ||
      !isRentalMonth(params.get('startMonth') ?? '') || !isRentalMonth(params.get('endMonth') ?? '') || params.get('endMonth')! < params.get('startMonth')!) return null
  } else {
    const list = parts.length === 2 && LISTS.includes(parts[1])
    const staticPage = parts.length === 1 || (parts.length === 2 && ['identity-verification', 'account-settings', 'notifications'].includes(parts[1]))
    const detail = parts.length === 3 && LISTS.includes(parts[1]) && isUuid(parts[2])
    const action = (parts.length === 4 && isUuid(parts[2]) && ((parts[1] === 'reservations' && parts[3] === 'confirm') ||
      (parts[1] === 'contracts' && ['renew', 'fees-return'].includes(parts[3])))) ||
      (parts.length === 5 && parts[1] === 'contracts' && isUuid(parts[2]) && ['access-visits', 'return-visits'].includes(parts[3]) && parts[4] === 'new') ||
      url.pathname === '/customer/support-tickets/new'
    if (!list && !staticPage && !detail && !action) return null
    for (const [key, content] of params) {
      if (list && ['page', 'pageSize'].includes(key) && /^\d+$/.test(content) && Number.isSafeInteger(Number(content)) && Number(content) >= 1 &&
        (key !== 'pageSize' || Number(content) <= 100)) continue
      if (url.pathname === '/customer/notifications' && key === 'tab' && ['notifications', 'history'].includes(content)) continue
      return null
    }
  }
  return `${url.pathname}${params.size ? `?${params}` : ''}`
}
export function clearCustomerContinuation(): void {
  try { sessionStorage.removeItem(KEY) } catch { /* Storage may be disabled. */ }
}
export function abandonCustomerContinuation(): void {
  signOutInProgress = true
  clearCustomerContinuation()
}
export function finishCustomerSignOutNavigation(): void { signOutInProgress = false }
export function beginCustomerContinuation(destination: string, explicitSelection = false): { continuationId: string } | null {
  if (explicitSelection) signOutInProgress = false
  if (signOutInProgress) return null
  clearCustomerContinuation()
  const safe = safeCustomerDestination(destination)
  if (!safe) return null
  const operation: PendingOperation = { version: 1, id: crypto.randomUUID(), destination: safe }
  try { sessionStorage.setItem(KEY, JSON.stringify(operation)); return { continuationId: operation.id } } catch { return null }
}
export function readCustomerContinuation(state: unknown): PendingOperation | null {
  if (!state || typeof state !== 'object') return null
  const continuationId = (state as { continuationId?: unknown }).continuationId
  if (typeof continuationId !== 'string' || !isUuid(continuationId)) return null
  try {
    const raw = sessionStorage.getItem(KEY)
    if (!raw) return null
    const item = JSON.parse(raw) as PendingOperation
    if (!item || item.version !== 1 || typeof item.id !== 'string' || !isUuid(item.id) || Object.keys(item).sort().join(',') !== 'destination,id,version' ||
      item.id !== (state as { continuationId: string }).continuationId || !safeCustomerDestination(item.destination)) {
      clearCustomerContinuation(); return null
    }
    return { version: 1, id: item.id, destination: safeCustomerDestination(item.destination)! }
  } catch { clearCustomerContinuation(); return null }
}
export function consumeCustomerContinuation(state: unknown): string | null {
  const item = readCustomerContinuation(state)
  clearCustomerContinuation()
  return item?.destination ?? null
}
export const isCustomerAuthPath = (pathname: string) => AUTH_PATHS.includes(pathname)
