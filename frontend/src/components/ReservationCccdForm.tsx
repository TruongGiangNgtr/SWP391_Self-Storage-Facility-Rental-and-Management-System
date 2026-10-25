import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { ArrowRight, RefreshCw, Shield } from 'lucide-react'
import { ApiErrorAlert } from './ApiErrorAlert'
import { CustomerFieldError } from './CustomerFieldError'
import type { useReservationProfile } from '../features/reservations/useReservationProfile'
import { CCCD_INPUT_PATTERN } from '../features/customer-profile/customerProfile'

export function ReservationCccdForm({ profile, onSaveStart, onContinue, onLater }: {
  profile: ReturnType<typeof useReservationProfile>
  onSaveStart(): void
  onContinue(): void
  onLater(): void
}) {
  const [cccd, setCccd] = useState('')
  const input = useRef<HTMLInputElement>(null)
  const title = useRef<HTMLHeadingElement>(null)
  const id = useId()
  useEffect(() => { title.current?.focus() }, [])
  const busy = profile.saving || profile.phase === 'checking'

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSaveStart()
    if (await profile.save(cccd)) { setCccd(''); onContinue() }
    else input.current?.focus()
  }

  async function checkSavedProfile() {
    if (await profile.recheck() === 'ready') { setCccd(''); onContinue() }
  }

  return <section className="reservation-cccd-step" aria-labelledby={`${id}-title`}>
    <div className="reservation-cccd-intro"><span className="reservation-cccd-icon"><Shield size={24} aria-hidden="true" /></span><div>
      <h2 id={`${id}-title`} ref={title} tabIndex={-1}>Add your National ID to continue reserving storage</h2>
      <p>Your selection will be kept while you complete this step. Saving does not create a reservation or hold capacity.</p>
    </div></div>
    <form className="form-grid" onSubmit={event => void save(event)} noValidate aria-busy={busy}>
      <div className="form-field"><label htmlFor={`${id}-cccd`}>National ID / CCCD</label>
        <input ref={input} id={`${id}-cccd`} name="cccd" value={cccd} autoComplete="off" inputMode="numeric" pattern={CCCD_INPUT_PATTERN} required maxLength={20}
          disabled={busy || profile.phase !== 'missing'} aria-invalid={Boolean(profile.fieldError)}
          aria-describedby={`${id}-help${profile.fieldError ? ` ${id}-error` : ''}`}
          onChange={event => { setCccd(event.target.value); profile.clearFieldError() }} />
        <CustomerFieldError id={`${id}-error`} message={profile.fieldError} />
        <small id={`${id}-help`}>Enter exactly 12 digits, including any leading zeroes. Providing this number does not verify your identity.</small>
      </div>
      <ApiErrorAlert error={profile.error} />
      {profile.error && <button className="button button-secondary" type="button" disabled={busy} onClick={() => void checkSavedProfile()}><RefreshCw size={16} aria-hidden="true" />Check Saved Profile</button>}
      <p className="reservation-cccd-note">After saving, you will return to review the current price and availability. You must confirm the reservation separately.</p>
      <div className="reservation-cccd-actions"><button className="button button-secondary" type="button" disabled={busy} onClick={() => { setCccd(''); onLater() }}>Not Now</button>
        <button className="button" type="submit" disabled={busy || profile.phase !== 'missing'}>{profile.saving ? 'Saving and checking…' : 'Save and Continue'}<ArrowRight size={16} aria-hidden="true" /></button>
      </div>
    </form>
  </section>
}
