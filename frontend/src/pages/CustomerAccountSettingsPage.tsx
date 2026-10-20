import { useEffect, useRef, useState, type FormEvent } from 'react'
import { CheckCircle2, Info, LockKeyhole, ShieldCheck } from 'lucide-react'
import { authApi } from '../api/authApi'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerFieldError } from '../components/CustomerFieldError'
import { LoadingState } from '../components/PageStates'
import { buildCustomerProfilePatch, readCustomerProfile, type CustomerProfileSnapshot } from '../features/customer-profile/customerProfile'
import '../styles/customer-account-settings.css'

export function CustomerAccountSettingsPage() {
  const { user, updateCustomerProfile } = useAuth()
  const [snapshot, setSnapshot] = useState<CustomerProfileSnapshot | null>(null)
  const [fullName, setFullName] = useState('')
  const [cccd, setCccd] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [saved, setSaved] = useState(false)
  const [reload, setReload] = useState(0)
  const busy = useRef(false)
  const alive = useRef(false)

  useEffect(() => {
    alive.current = true
    return () => { alive.current = false }
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    async function load() {
      try {
        const response = await authApi.getCurrentAccount(controller.signal)
        if (controller.signal.aborted) return
        const profile = readCustomerProfile(response?.data, user?.userAccountId ?? '')
        if (!profile) {
          setError(presentValidationError('Your profile could not be verified. Reload your profile before making changes.'))
          return
        }
        setSnapshot(profile); setFullName(profile.fullName); setCccd(profile.cccd ?? '')
      } catch (caughtError) {
        if (!controller.signal.aborted) setError(presentApiError(caughtError))
      } finally {
        if (!controller.signal.aborted) setLoading(false)
      }
    }
    void load()
    return () => controller.abort()
  }, [user?.userAccountId, reload])

  const patch = snapshot ? buildCustomerProfilePatch(snapshot, fullName, cccd) : {}
  const dirty = Object.keys(patch).length > 0

  function resetChanges() {
    if (!snapshot || busy.current) return
    setFullName(snapshot.fullName); setCccd(snapshot.cccd ?? ''); setFieldErrors({}); setError(null); setSaved(false)
  }

  async function saveChanges(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!snapshot || busy.current || !dirty) return
    const errors: Record<string, string> = {}
    if (!fullName.trim() || fullName.trim().length > 150) errors.fullName = 'Enter your full name (1–150 characters).'
    if (snapshot.cccd !== undefined && cccd.trim().length > 20) errors.cccd = 'Use no more than 20 characters for your National ID.'
    setFieldErrors(errors); setError(null); setSaved(false)
    if (Object.keys(errors).length) {
      event.currentTarget.querySelector<HTMLInputElement>(`[name="${Object.keys(errors)[0]}"]`)?.focus()
      return
    }
    busy.current = true; setSaving(true)
    try {
      const response = await updateCustomerProfile(patch)
      if (!alive.current) return
      const updated = { ...snapshot, fullName: response.fullName, cccd: 'cccd' in patch ? patch.cccd : snapshot.cccd }
      setSnapshot(updated); setFullName(updated.fullName); setCccd(updated.cccd ?? ''); setSaved(true)
    } catch (caughtError) {
      if (alive.current) setError(presentApiError(caughtError))
    } finally {
      busy.current = false
      if (alive.current) setSaving(false)
    }
  }

  return <main className="page-container flow-page customer-account-settings">
    <header className="account-page-heading"><p className="eyebrow">Customer Portal</p><h1>Account Settings</h1><p className="muted">Manage your personal information and account security.</p></header>
    <section className="panel account-profile-panel" aria-labelledby="profile-heading" aria-busy={loading || saving}>
      <div className="account-section-heading"><h2 id="profile-heading">Profile Information</h2><p className="muted">Keep your name and optional National ID up to date.</p></div>
      {loading && <LoadingState label="Loading your profile…" />}
      <ApiErrorAlert error={error} />
      {!loading && !snapshot && <button className="button button-secondary" type="button" onClick={() => { setLoading(true); setError(null); setReload(value => value + 1) }}>Reload Profile</button>}
      {!loading && snapshot && <form className="account-profile-form" onSubmit={saveChanges} noValidate>
        <div className="account-profile-grid">
          <div className="form-field"><label htmlFor="settings-full-name">Full Name</label><input id="settings-full-name" name="fullName" autoComplete="name" value={fullName} required maxLength={150} disabled={saving} aria-invalid={Boolean(fieldErrors.fullName)} aria-describedby={fieldErrors.fullName ? 'settings-name-error' : undefined} onChange={event => { setFullName(event.target.value); setSaved(false); setFieldErrors(current => ({ ...current, fullName: '' })) }} /><CustomerFieldError id="settings-name-error" message={fieldErrors.fullName} /></div>
          <div className="form-field"><label htmlFor="settings-phone">Phone Number <span className="account-readonly-label" aria-hidden="true">Read-only</span></label><div className="account-readonly-input"><input id="settings-phone" type="tel" autoComplete="tel" value={snapshot.phoneNumber} readOnly aria-describedby="settings-phone-help" /><LockKeyhole size={16} aria-hidden="true" /></div><small className="account-field-help" id="settings-phone-help">Used to sign in. Your phone number cannot be changed here.</small></div>
          <div className="form-field account-profile-wide"><label htmlFor="settings-email">Email Address <span className="account-readonly-label" aria-hidden="true">Read-only</span></label><div className="account-readonly-input"><input id="settings-email" type="email" autoComplete="email" value={snapshot.email} readOnly aria-describedby="settings-email-help" /><LockKeyhole size={16} aria-hidden="true" /></div><small className="account-field-help" id="settings-email-help"><Info size={14} aria-hidden="true" />Your account email cannot be changed here. Customer sign-in uses your phone number.</small></div>
          <div className="form-field account-profile-wide"><label htmlFor="settings-cccd">National ID / CCCD <span className="muted">(optional)</span></label><input id="settings-cccd" name="cccd" autoComplete="off" value={cccd} maxLength={20} disabled={saving || snapshot.cccd === undefined} placeholder={snapshot.cccd === undefined ? 'Not available yet' : 'Enter your National ID'} aria-invalid={Boolean(fieldErrors.cccd)} aria-describedby={`settings-cccd-help${fieldErrors.cccd ? ' settings-cccd-error' : ''}`} onChange={event => { setCccd(event.target.value); setSaved(false); setFieldErrors(current => ({ ...current, cccd: '' })) }} /><CustomerFieldError id="settings-cccd-error" message={fieldErrors.cccd} /><small className="muted" id="settings-cccd-help">{snapshot.cccd === undefined ? 'Your National ID is not available from the profile service yet. It will not be changed when you save your name.' : 'Optional, up to 20 characters. Updating this field does not verify your identity. Clearing it removes the saved National ID.'}</small></div>
        </div>
        {snapshot.cccd === undefined && <div className="account-information"><Info size={18} aria-hidden="true" /><p>National ID editing will be available when the profile service supports it. You can still update your full name.</p></div>}
        {saved && <p className="account-save-success" role="status"><CheckCircle2 size={18} aria-hidden="true" />Your profile changes have been saved.</p>}
        <div className="account-form-actions"><span className="muted" role="status">{dirty ? 'You have unsaved changes.' : 'No unsaved changes.'}</span><div className="action-row"><button className="button button-secondary" type="button" onClick={resetChanges} disabled={saving || !dirty}>Discard Changes</button><button className="button" type="submit" disabled={saving || !dirty}>{saving ? 'Saving Changes…' : 'Save Changes'}</button></div></div>
      </form>}
    </section>
    <section className="panel account-security-panel" aria-labelledby="security-heading"><div className="account-section-heading"><h2 id="security-heading">Password &amp; Security</h2><p className="muted">Review the sign-in method used for your customer account.</p></div>
      <div className="account-security-method"><span className="account-security-icon"><ShieldCheck size={23} aria-hidden="true" /></span><div><h3>Phone Number &amp; Password</h3><p>Use your registered phone number and current password to sign in.</p></div></div>
      <div className="account-information" id="settings-password-help"><LockKeyhole size={18} aria-hidden="true" /><p>Password changes are not available yet. Please continue using your current sign-in details. No password change request can be submitted from this page.</p></div>
    </section>
  </main>
}
