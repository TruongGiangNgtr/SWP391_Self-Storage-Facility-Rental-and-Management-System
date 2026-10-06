import { type FormEvent, useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { presentApiError, presentValidationError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { CalendarDateField } from '../components/CalendarDateField'
import type { VisitDetail } from '../features/visits/visit.types'

const VISIT_NAMES: Record<VisitDetail['visitType'], string> = {
  RESERVATION: 'Handover',
  ACCESS: 'Access',
  RETURN: 'Return',
}

export function VisitDetailPage() {
  const { visitId = '' } = useParams()
  const [visit, setVisit] = useState<VisitDetail | null>(null)
  const [newVisitDate, setNewVisitDate] = useState('')
  const [cancelReason, setCancelReason] = useState('')
  const [mode, setMode] = useState<'reschedule' | 'cancel' | null>(null)
  const [loading, setLoading] = useState(true)
  const [working, setWorking] = useState(false)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)

  const loadVisit = useCallback(async () => {
    try {
      const response = await visitApi.get(visitId)
      setVisit(response.data)
      setNewVisitDate(response.data.visitDate)
    } catch (requestError) {
      setVisit(null)
      setError(presentApiError(requestError))
    } finally {
      setLoading(false)
    }
  }, [visitId])

  useEffect(() => {
    let active = true
    visitApi
      .get(visitId)
      .then((response) => {
        if (active) {
          setVisit(response.data)
          setNewVisitDate(response.data.visitDate)
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
  }, [visitId])

  async function handleReschedule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!newVisitDate) {
      setError(presentValidationError('Please select a new visit date.'))
      return
    }

    setWorking(true)
    setError(null)
    try {
      await visitApi.reschedule(visitId, { visitDate: newVisitDate })
      setMode(null)
      await loadVisit()
    } catch (requestError) {
      setError(presentApiError(requestError))
    } finally {
      setWorking(false)
    }
  }

  async function handleCancel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!cancelReason.trim()) {
      setError(presentValidationError('Please enter a reason for cancelling the visit.'))
      return
    }

    setWorking(true)
    setError(null)
    try {
      await visitApi.cancel(visitId, { reason: cancelReason.trim() })
      setMode(null)
      setCancelReason('')
      await loadVisit()
    } catch (requestError) {
      setError(presentApiError(requestError))
    } finally {
      setWorking(false)
    }
  }

  if (loading && !visit) {
    return (
      <main className="page-container flow-page">
        <LoadingState label="Loading visit..." />
      </main>
    )
  }

  return (
    <main className="page-container flow-page narrow-page">
      <section className="page-heading page-heading-actions">
        <div>
          <p className="eyebrow">My Visits</p>
          <h1>Visit Details</h1>
        </div>
        <Link className="button button-secondary" to="/customer/visits">
          My Visits
        </Link>
      </section>

      <ApiErrorAlert error={error} />
      {visit && (
        <section className="panel stack">
          <div className="card-heading-row">
            <h2>{VISIT_NAMES[visit.visitType]}</h2>
            <StatusBadge status={visit.status} />
          </div>
          <dl className="detail-list compact">
            <div>
              <dt>Visit Date</dt>
              <dd>{visit.visitDate}</dd>
            </div>
            <div>
              <dt>Actual Return Date</dt>
              <dd>{visit.actualReturnDate ?? 'Not Available'}</dd>
            </div>
            <div>
              <dt>Assigned Employee</dt>
              <dd>{visit.employeeId ?? 'Not Assigned'}</dd>
            </div>
          </dl>

          {visit.visitType === 'RESERVATION' && (
            <Link className="button button-secondary" to={`/customer/reservations/${visit.entityId}`}>
              View Reservation
            </Link>
          )}

          {visit.status === 'SCHEDULED' && mode === null && (
            <div className="action-row">
              <button className="button" onClick={() => setMode('reschedule')} type="button">
                Reschedule
              </button>
              <button
                className="button button-danger"
                onClick={() => setMode('cancel')}
                type="button"
              >
                Cancel Visit
              </button>
            </div>
          )}

          {visit.status === 'SCHEDULED' && mode === 'reschedule' && (
            <form className="form-grid" onSubmit={handleReschedule}>
              <CalendarDateField id="newVisitDate" label="New Visit Date" value={newVisitDate} disabled={working} onChange={setNewVisitDate} />
              <small className="muted">The new date must follow the policy for this visit.</small>
              <div className="action-row">
                <button className="button button-secondary" disabled={working} onClick={() => setMode(null)} type="button">
                  Close
                </button>
                <button className="button" disabled={working} type="submit">
                  {working ? 'Updating...' : 'Save New Date'}
                </button>
              </div>
            </form>
          )}

          {visit.status === 'SCHEDULED' && mode === 'cancel' && (
            <form className="form-grid" onSubmit={handleCancel}>
              <div className="form-field">
                <label htmlFor="visitCancelReason">Cancellation Reason</label>
                <textarea
                  id="visitCancelReason"
                  rows={3}
                  value={cancelReason}
                  onChange={(event) => setCancelReason(event.target.value)}
                />
              </div>
              <div className="action-row">
                <button className="button button-secondary" disabled={working} onClick={() => setMode(null)} type="button">
                  Close
                </button>
                <button className="button button-danger" disabled={working} type="submit">
                  {working ? 'Cancelling...' : 'Confirm Visit Cancellation'}
                </button>
              </div>
            </form>
          )}
        </section>
      )}
    </main>
  )
}
