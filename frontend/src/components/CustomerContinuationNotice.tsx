import { Link, useLocation } from 'react-router-dom'
import { clearCustomerContinuation, readCustomerContinuation } from '../auth/customerContinuation'

export function CustomerContinuationNotice() {
  const { state } = useLocation()
  const pending = readCustomerContinuation(state)
  if (!pending) return null
  const reservation = pending.destination.startsWith('/customer/reservations/new?')
  const params = new URL(pending.destination, 'https://frms.invalid').searchParams
  const back = reservation ? `/facilities?${new URLSearchParams({ facilityId: params.get('facilityId')!, unitTypeId: params.get('unitTypeId')!, startMonth: params.get('startMonth')!, endMonth: params.get('endMonth')! })}` : '/'
  return <aside className="notice customer-continuation-notice"><div><strong>{reservation ? 'Your storage selection is saved for this sign-in.' : 'Sign in to continue your request.'}</strong><p>We will check current information after sign-in. Nothing is reserved or submitted automatically.</p><Link to={back} onClick={clearCustomerContinuation}>{reservation ? 'Cancel and return to storage options' : 'Cancel and return to Home'}</Link></div></aside>
}
