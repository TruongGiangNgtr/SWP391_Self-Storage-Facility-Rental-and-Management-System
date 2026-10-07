import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
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
import { formatMonthRange } from '../utils/formatters'
import '../styles/staff.css'

interface ContractContext { contract: HandoverContractDetail; reservation: ReservationDetail; visit: VisitDetail | null }

export function StaffContractPage() {
  const { contractId = '' } = useParams()
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
        const { data: reservation } = await reservationApi.get(contract.reservationId)
        if (!active) return
        if (contract.contractId !== contractId || reservation.reservationId !== contract.reservationId ||
          reservation.facilityId !== contract.facilityId || !Number.isFinite(reservation.lockedRentalPrice) || reservation.lockedRentalPrice < 0) {
          setSnapshot({ key, context: null, error: presentValidationError('The server returned inconsistent Contract/Reservation data. Refresh before continuing.') })
          return
        }
        const visit = reservation.reservationVisit ? (await visitApi.get(reservation.reservationVisit.visitId)).data : null
        if (!active) return
        if (visit && (visit.visitId !== reservation.reservationVisit?.visitId || visit.visitType !== 'RESERVATION' || visit.entityId !== reservation.reservationId)) {
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
        <div><p className="eyebrow">Facility Staff</p><h1>Handover Contract</h1></div>
        <Link className="button button-secondary" to="/staff">Work List</Link>
      </section>
      <ApiErrorAlert error={error} />
      <button className="button button-secondary" type="button" disabled={loading} onClick={() => setRetry((value) => value + 1)}>Refresh Contract</button>
      {loading ? <LoadingState label="Loading Contract and settlement record..." /> : context && (
        <>
          <section className="panel stack" aria-label="Contract details">
            <div className="card-heading-row"><h2>Contract Details</h2><StatusBadge status={context.contract.status} /></div>
            <dl className="detail-list">
              <div><dt>Contract ID</dt><dd>{context.contract.contractId}</dd></div>
              <div><dt>Reservation ID</dt><dd>{context.contract.reservationId}</dd></div>
              <div><dt>Storage Unit ID</dt><dd>{context.contract.storageUnitId}</dd></div>
              <div><dt>Facility ID</dt><dd>{context.contract.facilityId}</dd></div>
              <div><dt>Rental Period</dt><dd>{formatMonthRange(context.contract.startMonth, context.contract.endMonth)}</dd></div>
              <div><dt>Captured Contract Discount ID</dt><dd>{context.contract.discountId ?? 'No discount'}</dd></div>
            </dl>
          </section>
          <FirstMonthHandoverInfo lockedRentalPrice={context.reservation.lockedRentalPrice} handoverCompleted audience="staff" />
          {context.visit && <section className="panel stack" aria-label="Handover visit details">
            <div className="card-heading-row"><h2>Handover Visit</h2><StatusBadge status={context.visit.status} /></div>
            <p>Visit Date: {context.visit.visitDate}</p><p>Visit ID: <span className="staff-id">{context.visit.visitId}</span></p>
          </section>}
          <p className="notice">These values are read from the backend. Contract existence records the original first-month settlement, including when the Contract later completes or terminates.</p>
        </>
      )}
    </main>
  )
}
