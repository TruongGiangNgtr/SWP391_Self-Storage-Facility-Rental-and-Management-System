const STORAGE_KEY = 'frms.pendingMomoPayment'

export interface PendingPaymentReturn {
  paymentId: string
  reservationId: string
}

export const paymentReturnState = {
  get(): PendingPaymentReturn | null {
    const raw = window.sessionStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return null
    }

    try {
      const value = JSON.parse(raw) as Partial<PendingPaymentReturn>
      if (typeof value.paymentId !== 'string' || typeof value.reservationId !== 'string') {
        return null
      }
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
