import { useEffect, useRef, useState } from 'react'
import { authApi } from '../../api/authApi'
import { authToken } from '../../api/authToken'
import { ApiRequestError } from '../../api/httpClient'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { useAuth } from '../../auth/auth.context'
import { cccdValidationMessage, isValidCccd, readCustomerProfile } from '../customer-profile/customerProfile'

type ProfilePhase = 'checking' | 'ready' | 'missing' | 'unavailable'
class ProfileUnavailable extends Error {}

async function readOwnProfile(userAccountId: string, customerId: string, signal?: AbortSignal): Promise<'ready' | 'missing'> {
  const { data } = await authApi.getCurrentAccount(signal)
  const profile = readCustomerProfile(data, userAccountId)
  if (!profile || data.profile?.customerId !== customerId || profile.cccd === undefined) throw new ProfileUnavailable()
  return isValidCccd(profile.cccd ?? '') ? 'ready' : 'missing'
}

const profileError = (error: unknown) => error instanceof ProfileUnavailable
  ? presentValidationError('Your saved National ID could not be checked. Reload your profile before continuing; no reservation has been submitted.')
  : presentApiError(error)

// OD-09: profile data stays in memory; only a fresh own-profile read establishes readiness.
export function useReservationProfile(enabled: boolean) {
  const { user, updateCustomerProfile } = useAuth()
  const userAccountId = user?.userAccountId ?? ''
  const customerId = user?.customerId ?? ''
  const [phase, setPhase] = useState<ProfilePhase>('checking')
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [fieldError, setFieldError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const active = useRef(false)
  const reading = useRef(false)
  const writing = useRef(false)

  useEffect(() => {
    active.current = true
    if (!enabled) return () => { active.current = false }
    const controller = new AbortController()
    const token = authToken.get()
    readOwnProfile(userAccountId, customerId, controller.signal)
      .then(result => { if (!controller.signal.aborted && authToken.get() === token) setPhase(result) })
      .catch((caught: unknown) => {
        if (!controller.signal.aborted && authToken.get() === token) { setPhase('unavailable'); setError(profileError(caught)) }
      })
    return () => { active.current = false; controller.abort() }
  }, [enabled, userAccountId, customerId])

  async function recheck(): Promise<'ready' | 'missing' | null> {
    if (!enabled || reading.current || writing.current) return null
    reading.current = true; setPhase('checking'); setError(null)
    const token = authToken.get()
    try {
      const result = await readOwnProfile(userAccountId, customerId)
      if (!active.current || authToken.get() !== token) return null
      setPhase(result)
      return result
    } catch (caught) {
      if (active.current && authToken.get() === token) { setPhase('unavailable'); setError(profileError(caught)) }
      return null
    } finally { reading.current = false }
  }

  async function save(cccd: string): Promise<boolean> {
    if (!enabled || phase !== 'missing' || writing.current || reading.current) return false
    const value = cccd.trim()
    const invalid = cccdValidationMessage(value, true)
    if (invalid) { setFieldError(invalid); return false }
    writing.current = true; setSaving(true); setError(null); setFieldError(null)
    const token = authToken.get()
    try {
      let saveError: unknown
      try {
        // Omit name/address: this completion step updates CCCD only.
        await updateCustomerProfile({ cccd: value })
      } catch (caught) { saveError = caught }
      if (!active.current || authToken.get() !== token) return false
      // Also recover uncertain PATCH outcomes by reading, never by replaying the save.
      const result = await readOwnProfile(userAccountId, customerId)
      if (!active.current || authToken.get() !== token) return false
      setPhase(result)
      if (result === 'ready') return true
      if (saveError instanceof ApiRequestError && saveError.errors?.cccd?.length) setFieldError(saveError.errors.cccd.join(' '))
      setError(saveError ? presentApiError(saveError) : presentValidationError('Your profile still has no valid 12-digit National ID. Check your entry and try again.'))
      return false
    } catch (caught) {
      if (active.current && authToken.get() === token) { setPhase('unavailable'); setError(profileError(caught)) }
      return false
    } finally {
      writing.current = false
      if (active.current) setSaving(false)
    }
  }

  function requireCompletion(message: string) {
    setPhase('missing'); setFieldError(message); setError(null)
  }

  return { phase, error, fieldError, saving, recheck, save, requireCompletion,
    clearFieldError: () => setFieldError(null) }
}
