import { useState } from 'react'
import { createPortal } from 'react-dom'
import { useNavigate } from 'react-router-dom'
import { LogIn } from 'lucide-react'
import { beginCustomerContinuation } from '../auth/customerContinuation'
import type { PublicCatalogFilters, UnitTypeAvailability } from '../features/catalog/catalog.types'
import { formatMoney, formatMonthRange } from '../utils/formatters'
import { FlowDialog } from './FlowDialog'
import { FlowIcon } from './FlowIcon'
import { ApiErrorAlert } from './ApiErrorAlert'
import { presentValidationError } from '../api/apiErrorPresentation'
import '../styles/guest-customer-remaining.css'

export function CustomerSignInGate({ item, facilityId, facilityName, filters, onClose }: {
  item: UnitTypeAvailability; facilityId: string; facilityName: string; filters: PublicCatalogFilters; onClose(): void
}) {
  const navigate = useNavigate()
  const [failed, setFailed] = useState(false)
  function continueTo(register: boolean) {
    const query = new URLSearchParams({ facilityId, unitTypeId: item.unitTypeId, startMonth: filters.startMonth, endMonth: filters.endMonth })
    const state = beginCustomerContinuation(`/customer/reservations/new?${query}`, true)
    if (!state) { setFailed(true); return }
    navigate(register ? '/auth/customer/register' : '/auth/customer/login', { state })
  }
  return createPortal(<FlowDialog className="reservation-sign-in-design" headingIcon={<LogIn size={24} />} trapFocus title="Sign in to continue" subtitle="Sign in or create a Customer account to continue your reservation." category="Create Reservation" onClose={onClose}>
    <div className="customer-sign-in-gate"><div className="panel"><div className="dialog-selection"><span className="unit-icon-tile"><FlowIcon name="unit" /></span><div><strong>{item.name}</strong><small>{facilityName}</small></div></div><dl className="detail-list"><div><dt>Storage Type / Size</dt><dd>{item.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'} · {item.size}</dd></div><div><dt>Rental Period</dt><dd>{formatMonthRange(filters.startMonth, filters.endMonth)}</dd></div><div><dt>Current Monthly Rent</dt><dd>{formatMoney(item.rentalPrice)}/month</dd></div></dl></div>
      <ApiErrorAlert error={failed ? presentValidationError('Your selection could not be saved in this tab. Enable browser storage and try again, or sign in from the header and search again.') : null} />
      <button className="button" type="button" onClick={() => continueTo(false)}>Sign In to Continue</button><div className="customer-auth-divider"><span>New to FStoRent?</span></div><button className="button button-secondary" type="button" onClick={() => continueTo(true)}>Create Customer Account</button><p className="muted customer-auth-small">This selection does not hold capacity or create a deposit. Availability and price will be checked again after sign-in.</p><button className="button button-secondary" type="button" onClick={onClose}>Back to Storage Options</button>
    </div>
  </FlowDialog>, document.body)
}
