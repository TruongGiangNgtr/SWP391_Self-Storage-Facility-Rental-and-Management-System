import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { presentApiError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import type { Pagination } from '../api/api.types'
import { reservationApi } from '../api/reservationApi'
import { CustomerListToolbar } from '../components/CustomerListToolbar'
import { useCustomerListFilter } from '../features/customer-list/useCustomerListFilter'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { FlowIcon } from '../components/FlowIcon'
import type { ReservationDetail } from '../features/reservations/reservation.types'
import { getReservationStatusLabel, RESERVATION_FILTER_LABELS } from '../features/reservations/reservationPresentation'
import { useReservationUnitTypes } from '../features/reservations/useReservationUnitTypes'
import { formatMoney, formatMonthRange, formatUtcDateTime } from '../utils/formatters'

const EMPTY_PAGINATION: Pagination = {
  page: 1,
  pageSize: 20,
  totalItems: 0,
  totalPages: 0,
}

export function ReservationListPage() {
  const filter = useCustomerListFilter()
  const location = useLocation()
  const [page, setPage] = useState(1)
  const [reservations, setReservations] = useState<ReservationDetail[]>([])
  const [pagination, setPagination] = useState(EMPTY_PAGINATION)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const unitTypes = useReservationUnitTypes(reservations.map(reservation => reservation.unitTypeId))
  const visibleReservations = reservations.filter(reservation => filter.matches(
    `${unitTypes.reads[reservation.unitTypeId]?.data?.name ?? ''} ${reservation.startMonth} ${reservation.endMonth}`,
    reservation.status,
  ))

  useEffect(() => {
    let active = true

    reservationApi
      .list(page)
      .then((response) => {
        if (active) {
          setReservations(response.data)
          setPagination(response.pagination)
        }
      })
      .catch((requestError: unknown) => {
        if (active) {
          setError(presentApiError(requestError))
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false)
        }
      })

    return () => {
      active = false
    }
  }, [page])

  return (
    <main className="page-container flow-page">
      <section className="page-heading page-heading-actions">
        <div>
          <p className="eyebrow">Reservation Management</p>
          <h1>My Reservations</h1>
          <p className="muted">Track your reservations, settle deposits and manage handover visits.</p>
        </div>
        <Link className="button" to="/customer/storage-search">
          <FlowIcon name="add" />
          New Storage Reservation
        </Link>
      </section>

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={unitTypes.error} />
      {unitTypes.error && <button className="button button-secondary" type="button" disabled={unitTypes.loading} onClick={unitTypes.retry}>Retry Unit Type Names</button>}
      {loading ? (
        <LoadingState />
      ) : error ? null : reservations.length === 0 ? (
        <EmptyState message="You do not have any reservations yet." />
      ) : (
        <><CustomerListToolbar {...filter.controls} statuses={["PENDING_DEPOSIT","CONFIRMED","COMPLETED","CANCELLED"]} statusLabels={RESERVATION_FILTER_LABELS} searchPlaceholder="Search by unit type or rental period" visibleCount={visibleReservations.length} /><div className="reservation-list">
          {visibleReservations.map((reservation) => (
            <article className={`reservation-card reservation-card-${reservation.status.toLowerCase().replaceAll('_', '-')}`} key={reservation.reservationId}>
              <div className="card-heading-row">
                <h2>Storage Reservation</h2>
                <StatusBadge status={reservation.status} label={getReservationStatusLabel(reservation)} />
              </div>
              <p className="reservation-meta">Created {formatUtcDateTime(reservation.createdAt)}</p>
              <dl className="detail-list">
                <div><dt>Unit Type</dt><dd>{unitTypes.reads[reservation.unitTypeId]?.data?.name ?? (unitTypes.reads[reservation.unitTypeId]?.error ? 'Unit type unavailable' : 'Loading unit type…')}</dd></div>
                <div><dt>Rental Period</dt><dd>{formatMonthRange(reservation.startMonth, reservation.endMonth)}</dd></div>
                <div><dt>Locked Monthly Rent</dt><dd>{formatMoney(reservation.lockedRentalPrice)}/month</dd></div>
                <div><dt>Deposit Invoice · {reservation.depositInvoice.status}</dt><dd>{formatMoney(reservation.depositInvoice.amountDue)}</dd></div>
              </dl>
              <div className="notice">
                <FlowIcon name={reservation.status === 'COMPLETED' ? 'complete' : reservation.status === 'CANCELLED' ? 'cancel' : reservation.status === 'CONFIRMED' ? 'calendar' : 'clock'} />
                {reservation.status === 'PENDING_DEPOSIT' && (reservation.depositInvoice.status === 'PAID'
                  ? 'Your deposit is paid. You can confirm the reservation and select a handover date.'
                  : `Deposit Payment Deadline: ${formatUtcDateTime(reservation.depositInvoice.dueDate)}.`)}
                {reservation.status === 'CONFIRMED' && `Your reservation is confirmed.${reservation.reservationVisit ? ` Handover date: ${reservation.reservationVisit.visitDate}.` : ''}`}
                {reservation.status === 'COMPLETED' && 'Handover for this reservation is complete.'}
                {reservation.status === 'CANCELLED' && 'Your reservation is cancelled.'}
              </div>
              <div className="action-row">
                <Link className="button button-secondary" to={reservation.reservationId}>View Details</Link>
                {reservation.status === 'PENDING_DEPOSIT' && reservation.depositInvoice.status === 'PAID' && (
                  <Link className="button" to={`/customer/reservations/${reservation.reservationId}/confirm`} state={{ backgroundLocation: location }}><FlowIcon name="calendar-action" />Schedule Handover</Link>
                )}
                {reservation.status === 'PENDING_DEPOSIT' && reservation.depositInvoice.status === 'UNPAID' && (
                  <Link className="button" to={reservation.reservationId}><FlowIcon name="payment-start" />Pay Deposit</Link>
                )}
                {reservation.status === 'CONFIRMED' && reservation.reservationVisit && (
                  <Link className="button" to={`/customer/visits/${reservation.reservationVisit.visitId}`}><FlowIcon name="calendar-action" />Manage Handover Visit</Link>
                )}
              </div>
            </article>
          ))}
        </div></>
      )}

      <PaginationControls
        disabled={loading}
        pagination={pagination}
        onPageChange={(nextPage) => {
          setLoading(true)
          setError(null)
          setPage(nextPage)
        }}
      />
    </main>
  )
}
