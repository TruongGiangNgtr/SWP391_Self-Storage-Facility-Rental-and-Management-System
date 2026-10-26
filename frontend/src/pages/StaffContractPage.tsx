import { employeeContractLabel, employeeMonthRange, employeeText, employeeUnitLabel } from '../utils/employeePresentation'
import { EmployeeEntityLabel, EmployeeReservationLabel } from '../components/EmployeeEntityLabel'
import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { contractApi } from '../api/contractApi'
import { reservationApi } from '../api/reservationApi'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { FirstMonthHandoverInfo } from '../components/FirstMonthHandoverInfo'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import type { HandoverContractDetail } from '../features/handover/contract.types'
import type { ReservationDetail } from '../features/reservations/reservation.types'
import type { VisitDetail } from '../features/visits/visit.types'
import '../styles/staff.css'

interface ContractContext { contract: HandoverContractDetail; reservation: ReservationDetail | null; visit: VisitDetail | null }

export function StaffContractPage({ selectedContractId }: { selectedContractId?: string } = {}) {
  const { contractId: routeContractId = '' } = useParams()
  const contractId = selectedContractId ?? routeContractId
  const [params] = useSearchParams()
  const listPage = Number(params.get('page'))
  const contractsPath = Number.isSafeInteger(listPage) && listPage > 1 ? `/staff/contracts?page=${listPage}` : '/staff/contracts'
  const [snapshot, setSnapshot] = useState<{ key: string; context: ContractContext | null; error: ApiErrorPresentation | null } | null>(null)
  const [retry, setRetry] = useState(0)
  const key = `${contractId}:${retry}`
  const loading = snapshot?.key !== key
  const context = loading ? null : snapshot.context
  const error = loading ? null : snapshot.error

  useEffect(() => {
    let active = true
    async function load() {
      try {
        const { data: contract } = await contractApi.get(contractId)
        if (!active) return
        if (!contract || contract.contractId !== contractId) {
          setSnapshot({ key, context: null, error: presentValidationError('The contract details are unavailable. Refresh before continuing.') })
          return
        }
        const reservation = contract.reservationId ? (await reservationApi.get(contract.reservationId)).data : null
        if (!active) return
        if (reservation && (reservation.reservationId !== contract.reservationId ||
          reservation.facilityId !== contract.facilityId || reservation.customerId !== contract.customerId ||
          !Number.isFinite(reservation.lockedRentalPrice) || reservation.lockedRentalPrice < 0)) {
          setSnapshot({ key, context: null, error: presentValidationError('The server returned inconsistent Contract/Reservation data. Refresh before continuing.') })
          return
        }
        const visit = reservation?.reservationVisit ? (await visitApi.get(reservation.reservationVisit.visitId)).data : null
        if (!active) return
        if (visit && (visit.visitId !== reservation?.reservationVisit?.visitId || visit.visitType !== 'RESERVATION' || visit.entityId !== reservation?.reservationId)) {
          setSnapshot({ key, context: null, error: presentValidationError('The handover visit does not match the Contract reservation.') })
          return
        }
        setSnapshot({ key, context: { contract, reservation, visit }, error: null })
      } catch (caughtError) {
        if (active) setSnapshot({ key, context: null, error: presentApiError(caughtError) })
      }
    }
    void load()
    return () => { active = false }
  }, [contractId, key])

  return (
    <main className="page-container flow-page narrow-page staff-flow">
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">Facility Staff</p><h1>Contract Details</h1></div>
        <div className="employee-page-actions">
          {!selectedContractId && <Link className="button button-secondary" to={contractsPath}>View Contracts</Link>}
          {!selectedContractId && <Link className="button button-secondary" to="/staff">Work List</Link>}
          <button className="button button-secondary" type="button" disabled={loading} onClick={() => setRetry((value) => value + 1)}>Refresh Contract</button>
        </div>
      </section>
      <ApiErrorAlert error={error} />
      {loading ? <LoadingState label="Loading Contract and settlement record..." /> : context && (
        <>
          <section className="panel stack" aria-label="Contract details">
            <div className="card-heading-row"><h2>Contract Details</h2><StatusBadge status={employeeText(context.contract.status)} /></div>
            <dl className="detail-list">
              <div><dt>Contract</dt><dd>{employeeContractLabel(context.contract)}</dd></div>
              <div><dt>Reservation</dt><dd><EmployeeReservationLabel customerId={context.reservation?.customerId} startMonth={context.reservation?.startMonth} endMonth={context.reservation?.endMonth} visitDate={context.reservation?.reservationVisit?.visitDate} /></dd></div>
              <div><dt>Storage Unit</dt><dd>{employeeText(context.contract.unitTypeName ?? context.contract.storageUnit?.unitTypeName)} · {employeeUnitLabel(context.contract)}</dd></div>
              <div><dt>Facility</dt><dd><EmployeeEntityLabel kind="facility" id={context.contract.facilityId} /></dd></div>
              <div><dt>Rental Period</dt><dd>{employeeMonthRange(context.contract.startMonth, context.contract.endMonth)}</dd></div>
              <div><dt>Captured Contract Discount</dt><dd><EmployeeEntityLabel kind="discount" id={context.contract.discountId} scope={context.contract.customerId} empty="No discount" /></dd></div>
            </dl>
          </section>
          {context.reservation
            ? <FirstMonthHandoverInfo lockedRentalPrice={context.reservation.lockedRentalPrice} handoverCompleted audience="staff" />
            : <p className="notice">Original reservation details are not available.</p>}
          {context.visit && <section className="panel stack" aria-label="Handover visit details">
            <div className="card-heading-row"><h2>Handover Visit</h2><StatusBadge status={employeeText(context.visit.status)} /></div>
            <p>Visit Date: {employeeText(context.visit.visitDate)}</p>
          </section>}
          <p className="notice">First-month settlement remains recorded here after the contract completes or terminates.</p>
        </>
      )}
    </main>
  )
}
