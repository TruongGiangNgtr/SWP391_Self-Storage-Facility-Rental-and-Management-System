import { isUuid } from '../handover/handoverValidation'

const STORAGE_KEY = 'frms.pendingPayment'

// Preserve existing deposit return records; Invoice checkout carries its own context.
export type PendingPaymentReturn =
  | { paymentId: string; reservationId: string; invoiceId?: never; userAccountId?: never }
  | { paymentId: string; invoiceId: string; userAccountId: string; reservationId?: never }

export const paymentReturnState = {
  get(): PendingPaymentReturn | null {
    try {
      const raw = window.sessionStorage.getItem(STORAGE_KEY)
      if (!raw) return null
      const value = JSON.parse(raw) as Partial<PendingPaymentReturn>
      if (!value || typeof value.paymentId !== 'string') return null
      if (typeof value.invoiceId === 'string') {
        if (!isUuid(value.paymentId) || !isUuid(value.invoiceId) ||
          typeof value.userAccountId !== 'string' || !isUuid(value.userAccountId)) return null
        return { paymentId: value.paymentId, invoiceId: value.invoiceId, userAccountId: value.userAccountId }
      }
      if (typeof value.reservationId !== 'string') return null
      return { paymentId: value.paymentId, reservationId: value.reservationId }
    } catch {
      return null
    }
  },

  set(value: PendingPaymentReturn): void {
    window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(value))
  },

  clear(): void {
    window.sessionStorage.removeItem(STORAGE_KEY)
  },
}
