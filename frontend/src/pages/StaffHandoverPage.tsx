import { employeeText } from '../utils/employeePresentation'
import { EmployeeEntityLabel, EmployeeReservationLabel } from '../components/EmployeeEntityLabel'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { FirstMonthHandoverInfo } from '../components/FirstMonthHandoverInfo'
import { LoadingState, PaginationControls } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { getHandoverInputError, isActiveHandoverAssignment } from '../features/handover/handoverValidation'
import { useCustomerDiscounts } from '../features/handover/useCustomerDiscounts'
import { useReservationHandover } from '../features/handover/useReservationHandover'
import { formatMonthRange, formatMoney } from '../utils/formatters'
import '../styles/staff.css'

export function StaffHandoverPage() {
  const { reservationId = '', visitId = '' } = useParams()
  const [searchParams] = useSearchParams()
  const requestedPage = Number(searchParams.get('workPage') ?? 1)
  const workPage = Number.isSafeInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const { context, loading, action, error, refresh, checkIn, complete, confirmationKey } = useReservationHandover(reservationId, visitId, workPage)
  const [discountId, setDiscountId] = useState('')
  const [discountPage, setDiscountPage] = useState(1)
  const [acknowledgedKey, setAcknowledgedKey] = useState('')
  const receiptAcknowledged = acknowledgedKey === confirmationKey
  const assignment = context?.assignment ?? null
  const validAssignment = isActiveHandoverAssignment(assignment, reservationId)
  const [validationError, setValidationError] = useState<ApiErrorPresentation | null>(null)
  const customer = context?.workItem?.customer
  const discounts = useCustomerDiscounts(context?.reservation.status === 'CONFIRMED' ? customer?.customerId : undefined, discountPage)
  const navigate = useNavigate()
  const busy = loading || action !== null

  function resetAcknowledgment() {
    setAcknowledgedKey('')
    setValidationError(null)
    setDiscountId('')
    setDiscountPage(1)
  }

  async function handleComplete(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setValidationError(null)
    const inputError = getHandoverInputError(validAssignment ? assignment.storageUnitId : '', receiptAcknowledged)
    if (inputError) { setValidationError(presentValidationError(inputError)); return }
    if (discountId && !discounts.response?.data.some((item) =>
      item.discountId === discountId && item.customerId === customer?.customerId && item.status === 'ACTIVE')) {
      setValidationError(presentValidationError('Refresh and select a discount from this customer’s current list, or choose no discount.'))
      return
    }
    setAcknowledgedKey('')
    const result = await complete({ visitId, discountId: discountId || null })
    if (result) navigate(`/staff/contracts/${encodeURIComponent(result.contract.contractId)}`)
  }

  return (
    <main className="page-container flow-page narrow-page staff-flow">
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">Facility Staff</p><h1>Reservation Check-in &amp; Handover</h1></div>
        <Link className="button button-secondary" to="/staff">Work List</Link>
      </section>
      <ApiErrorAlert error={validationError ?? error} />
      <button className="button button-secondary" type="button" disabled={busy} onClick={() => { resetAcknowledgment(); void refresh() }}>Refresh Details</button>
      {loading ? <LoadingState label="Loading reservation and visit..." /> : context && (
        <>
          <section className="panel stack" aria-label="Reservation and customer">
            <div className="card-heading-row"><h2>{employeeText(customer?.fullName, 'Reservation Details')}</h2><StatusBadge status={context.reservation.status} /></div>
            {customer ? <p className="muted">{customer.phoneNumber}</p> : <p className="notice">Customer identification is not in the current work-list page. Return to Work List to review the customer; a missing work item is not evidence of a different reservation.</p>}
            <dl className="detail-list">
              <div><dt>Reservation</dt><dd><EmployeeReservationLabel customerId={context.reservation.customerId} customerName={customer?.fullName} /></dd></div>
              <div><dt>Rental Period</dt><dd>{formatMonthRange(context.reservation.startMonth, context.reservation.endMonth)}</dd></div>
              <div><dt>Facility</dt><dd><EmployeeEntityLabel kind="facility" id={context.reservation.facilityId} /></dd></div>
              <div><dt>Unit Type</dt><dd><EmployeeEntityLabel kind="unitType" id={context.reservation.unitTypeId} /></dd></div>
            </dl>
          </section>
          <section className="panel stack" aria-label="Reservation visit">
            <div className="card-heading-row"><h2>Reservation Visit</h2><StatusBadge status={context.visit.status} /></div>
            <dl className="detail-list">
              <div><dt>Visit</dt><dd>Reservation visit · {context.visit.visitDate}</dd></div>
              <div><dt>Visit Date</dt><dd>{context.visit.visitDate}</dd></div>
            </dl>
            {context.reservation.status === 'CONFIRMED' && context.visit.status === 'SCHEDULED' ? (
              <>
                <p className="muted">Verify the arriving customer before check-in. The backend checks the visit date, Facility and current state.</p>
                <button className="button" type="button" disabled={busy} onClick={() => { resetAcknowledgment(); void checkIn() }}>{action === 'check-in' ? 'Checking In...' : 'Check In Customer'}</button>
              </>
            ) : <p className="muted">{context.visit.status === 'CHECKED_IN' ? 'The customer is checked in. Continue with the Manager-selected unit and offline receipt.' : 'Check-in is not available in the current reservation/visit state.'}</p>}
          </section>
          {(context.reservation.status === 'CONFIRMED' || context.reservation.status === 'COMPLETED') && (
            <FirstMonthHandoverInfo lockedRentalPrice={context.reservation.lockedRentalPrice} handoverCompleted={context.reservation.status === 'COMPLETED'} audience="staff" />
          )}
          {context.reservation.status === 'CONFIRMED' && context.visit.status === 'CHECKED_IN' && (
            <section className="panel stack" aria-label="Complete handover">
              <h2>Complete Handover</h2>
              <form className="form-grid" onSubmit={handleComplete}>
                <section className="stack" aria-label="Assigned storage unit">
                  <h3>Manager-assigned Unit</h3>
                  {validAssignment ? (
                    <dl className="detail-list">
                      <div><dt>Unit Code</dt><dd>{employeeText(assignment.unitCode)}</dd></div>
                      <div><dt>Location</dt><dd>{employeeText(assignment.locationInfo)}</dd></div>
                      <div><dt>Status</dt><dd>{assignment.status}</dd></div>
                      <div><dt>Assigned At</dt><dd>{new Date(assignment.assignedAt).toLocaleString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' })} (GMT+7)</dd></div>
                    </dl>
                  ) : <p className="notice">No valid active assignment. Ask the Manager to save a unit, then refresh details.</p>}
                </section>
                <section className="stack" aria-label="Contract discount">
                  <div className="form-field">
                    <label htmlFor="discountId">Contract Discount (Optional, Later Months Only)</label>
                    <select id="discountId" value={discountId} disabled={busy || discounts.loading || !discounts.response} onChange={(event) => { setDiscountId(event.target.value); setAcknowledgedKey('') }}>
                      <option value="">No discount</option>
                      {discounts.response?.data.filter((item) => item.customerId === customer?.customerId).map((item) => (
                        <option key={item.discountId} value={item.discountId} disabled={item.status !== 'ACTIVE'}>{item.name} — {item.percentage}% ({item.status})</option>
                      ))}
                    </select>
                    <small className="muted">First-month rent remains {formatMoney(context.reservation.lockedRentalPrice)}. The backend validates discount ownership and effective dates for eligible later invoices.</small>
                  </div>
                  {discounts.loading && <LoadingState label="Loading customer discounts..." />}
                  <ApiErrorAlert error={discounts.error} />
                  {discounts.error && <><p className="muted">Discounts could not be loaded. You may explicitly continue with no discount or retry the read.</p><button className="button button-secondary" type="button" disabled={busy} onClick={() => { setDiscountId(''); discounts.refresh() }}>Retry Discounts</button></>}
                  {!customer && <p className="muted">Return to the matching Work List page to load this customer's discount catalogue. No discount is selected.</p>}
                  {discounts.response?.data.length === 0 && <p className="muted">No customer discounts.</p>}
                  {discounts.response && <PaginationControls pagination={discounts.response.pagination} disabled={busy || discounts.loading} onPageChange={(nextPage) => { setDiscountId(''); setAcknowledgedKey(''); setDiscountPage(nextPage) }} />}
                </section>
                <label className="receipt-confirmation">
                  <input type="checkbox" checked={receiptAcknowledged} disabled={busy || !validAssignment} onChange={(event) => setAcknowledgedKey(event.target.checked ? confirmationKey : '')} />
                  <span>I confirm that I have received the full first-month rent of {formatMoney(context.reservation.lockedRentalPrice)} offline and completed the physical handover.</span>
                </label>
                <p className="muted">This command records receipt through Contract creation. It does not create a first-month Invoice or Payment. If a retry is needed, do not collect the same rent twice.</p>
                <button className="button" type="submit" disabled={busy || !validAssignment || !receiptAcknowledged}>{action === 'handover' ? 'Completing Handover...' : 'Complete Handover'}</button>
              </form>
            </section>
          )}
          {context.reservation.status === 'COMPLETED' && <p className="notice">This reservation is already completed. No additional first-month payment or handover action is required.</p>}
        </>
      )}
    </main>
  )
}
