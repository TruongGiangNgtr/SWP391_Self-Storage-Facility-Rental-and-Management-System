import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiRequestError } from '../api/httpClient'
import {
  presentApiError,
  presentValidationError,
  type ApiErrorPresentation,
} from '../api/apiErrorPresentation'
import { catalogApi } from '../api/catalogApi'
import { reservationApi } from '../api/reservationApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import type {
  FacilitySummary,
  UnitMode,
  UnitTypeAvailability,
} from '../features/catalog/catalog.types'
import type {
  CreateReservationRequest,
  ReservationDetail,
} from '../features/reservations/reservation.types'
import {
  getNextBusinessMonth,
  validateCreateReservation,
  validateMonthRange,
} from '../features/reservations/reservationValidation'

const currencyFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
})

const dateTimeFormatter = new Intl.DateTimeFormat('vi-VN', {
  timeZone: 'Asia/Ho_Chi_Minh',
  dateStyle: 'short',
  timeStyle: 'short',
})

const MODE_LABELS: Record<UnitMode, string> = {
  PUBLIC: 'Kho chung',
  PRIVATE: 'Kho riêng',
}

function formatMoney(value: number): string {
  return currencyFormatter.format(value)
}

function formatDeadline(value: string): string {
  return dateTimeFormatter.format(new Date(value))
}

export function CustomerReservationCreatePage() {
  const minimumMonth = useMemo(() => getNextBusinessMonth(), [])
  const [facilities, setFacilities] = useState<FacilitySummary[]>([])
  const [unitTypes, setUnitTypes] = useState<UnitTypeAvailability[]>([])
  const [facilityId, setFacilityId] = useState('')
  const [unitMode, setUnitMode] = useState<UnitMode | ''>('')
  const [unitTypeId, setUnitTypeId] = useState('')
  const [startMonth, setStartMonth] = useState('')
  const [endMonth, setEndMonth] = useState('')
  const [isLoadingFacilities, setIsLoadingFacilities] = useState(true)
  const [isLoadingAvailability, setIsLoadingAvailability] = useState(false)
  const [availabilityRevision, setAvailabilityRevision] = useState(0)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [catalogError, setCatalogError] = useState<ApiErrorPresentation | null>(null)
  const [submitError, setSubmitError] = useState<ApiErrorPresentation | null>(null)
  const [createdReservation, setCreatedReservation] =
    useState<ReservationDetail | null>(null)

  useEffect(() => {
    let isCurrent = true

    async function loadFacilities() {
      try {
        setIsLoadingFacilities(true)
        setCatalogError(null)
        const response = await catalogApi.listFacilities({ page: 1, pageSize: 100 })
        if (isCurrent) {
          setFacilities(response.data)
        }
      } catch (error) {
        if (isCurrent) {
          setCatalogError(presentApiError(error))
        }
      } finally {
        if (isCurrent) {
          setIsLoadingFacilities(false)
        }
      }
    }

    void loadFacilities()
    return () => {
      isCurrent = false
    }
  }, [])

  const monthError = validateMonthRange(startMonth, endMonth)
  const canLoadAvailability = Boolean(facilityId && !monthError)

  useEffect(() => {
    if (!canLoadAvailability) {
      return
    }

    let isCurrent = true

    catalogApi
      .listFacilityUnitTypes(facilityId, {
        startMonth,
        endMonth,
        page: 1,
        pageSize: 100,
      })
      .then((response) => {
        if (isCurrent) {
          setUnitTypes(response.data)
        }
      })
      .catch((error: unknown) => {
        if (isCurrent) {
          setUnitTypes([])
          setCatalogError(presentApiError(error))
        }
      })
      .finally(() => {
        if (isCurrent) {
          setIsLoadingAvailability(false)
        }
      })

    return () => {
      isCurrent = false
    }
  }, [availabilityRevision, canLoadAvailability, endMonth, facilityId, startMonth])

  const modes = useMemo(
    () => Array.from(new Set(unitTypes.map((unitType) => unitType.mode))),
    [unitTypes],
  )

  const sizeOptions = useMemo(
    () =>
      unitMode
        ? unitTypes.filter((unitType) => unitType.mode === unitMode)
        : [],
    [unitMode, unitTypes],
  )

  const selectedFacility = facilities.find(
    (facility) => facility.facilityId === facilityId,
  )
  const selectedUnitType = unitTypes.find(
    (unitType) => unitType.unitTypeId === unitTypeId,
  )

  function resetUnitSelection() {
    setUnitMode('')
    setUnitTypeId('')
    setUnitTypes([])
  }

  function prepareAvailabilityLoad(
    nextFacilityId: string,
    nextStartMonth: string,
    nextEndMonth: string,
  ) {
    const shouldLoad = Boolean(
      nextFacilityId && !validateMonthRange(nextStartMonth, nextEndMonth),
    )
    setCatalogError(null)
    setIsLoadingAvailability(shouldLoad)
  }

  function handleFacilityChange(value: string) {
    setFacilityId(value)
    resetUnitSelection()
    prepareAvailabilityLoad(value, startMonth, endMonth)
  }

  function handleStartMonthChange(value: string) {
    setStartMonth(value)
    resetUnitSelection()
    prepareAvailabilityLoad(facilityId, value, endMonth)
  }

  function handleEndMonthChange(value: string) {
    setEndMonth(value)
    resetUnitSelection()
    prepareAvailabilityLoad(facilityId, startMonth, value)
  }

  function handleModeChange(value: string) {
    setUnitMode(value as UnitMode | '')
    setUnitTypeId('')
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const request: CreateReservationRequest = {
      facilityId,
      unitTypeId,
      startMonth,
      endMonth,
    }
    const validationError = validateCreateReservation(request)

    if (validationError) {
      setSubmitError(presentValidationError(validationError))
      return
    }

    try {
      setIsSubmitting(true)
      setSubmitError(null)
      const response = await reservationApi.create(request)
      setCreatedReservation(response.data)
    } catch (error) {
      setSubmitError(presentApiError(error))

      if (error instanceof ApiRequestError && error.code === 'CAPACITY_NOT_AVAILABLE') {
        resetUnitSelection()
        setIsLoadingAvailability(true)
        setAvailabilityRevision((revision) => revision + 1)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  if (createdReservation) {
    return (
      <main className="page-container">
        <section className="panel reservation-result" aria-live="polite">
          <p className="eyebrow">CWP-03 · Reservation đã được tạo</p>
          <div className="result-heading">
            <div>
              <h1>Đặt kho thành công</h1>
              <p className="muted">
                Reservation đang chờ thanh toán Deposit và chưa phải hợp đồng thuê.
              </p>
            </div>
            <span className="status-badge status-pending">
              {createdReservation.status}
            </span>
          </div>

          <dl className="summary-grid">
            <div>
              <dt>Địa điểm</dt>
              <dd>{selectedFacility?.name ?? createdReservation.facilityId}</dd>
            </div>
            <div>
              <dt>Loại kho</dt>
              <dd>{selectedUnitType?.name ?? createdReservation.unitTypeId}</dd>
            </div>
            <div>
              <dt>Thời hạn thuê</dt>
              <dd>
                {createdReservation.startMonth} đến {createdReservation.endMonth}
              </dd>
            </div>
            <div>
              <dt>Giá thuê đã khóa</dt>
              <dd>{formatMoney(createdReservation.lockedRentalPrice)}</dd>
            </div>
            <div>
              <dt>Deposit cần thanh toán</dt>
              <dd>{formatMoney(createdReservation.depositAmount)}</dd>
            </div>
            <div>
              <dt>Hạn thanh toán</dt>
              <dd>{formatDeadline(createdReservation.depositInvoice.dueDate)}</dd>
            </div>
          </dl>

          <div className="notice">
            Chức năng thanh toán Deposit qua MoMo sẽ được thực hiện trong CWP-04.
          </div>
          <button
            className="button button-secondary"
            type="button"
            onClick={() => setCreatedReservation(null)}
          >
            Tạo Reservation khác
          </button>
        </section>
      </main>
    )
  }

  return (
    <main className="page-container">
      <section className="panel reservation-page">
        <p className="eyebrow">CWP-03 · Storage Reservation</p>
        <h1>Tạo Reservation</h1>
        <p className="muted">
          Chọn địa điểm, loại kho và khoảng tháng cần thuê. Sức chứa, giá thuê và
          Deposit đều do hệ thống xác nhận.
        </p>

        <ApiErrorAlert error={catalogError} />

        <form className="form-grid reservation-form" onSubmit={handleSubmit} noValidate>
          <div className="form-field form-field-wide">
            <label htmlFor="facilityId">Địa điểm lưu trữ</label>
            <select
              id="facilityId"
              value={facilityId}
              onChange={(event) => handleFacilityChange(event.target.value)}
              disabled={isLoadingFacilities}
              required
            >
              <option value="">
                {isLoadingFacilities ? 'Đang tải địa điểm...' : 'Chọn địa điểm'}
              </option>
              {facilities.map((facility) => (
                <option key={facility.facilityId} value={facility.facilityId}>
                  {facility.name} — {facility.address}
                </option>
              ))}
            </select>
            {!isLoadingFacilities && facilities.length === 0 && !catalogError && (
              <small>Hiện chưa có Facility đang hoạt động.</small>
            )}
          </div>

          <div className="form-field">
            <label htmlFor="startMonth">Tháng bắt đầu thuê</label>
            <input
              id="startMonth"
              type="month"
              min={minimumMonth}
              value={startMonth}
              onChange={(event) => handleStartMonthChange(event.target.value)}
              required
            />
          </div>

          <div className="form-field">
            <label htmlFor="endMonth">Tháng kết thúc thuê</label>
            <input
              id="endMonth"
              type="month"
              min={startMonth || minimumMonth}
              value={endMonth}
              onChange={(event) => handleEndMonthChange(event.target.value)}
              required
            />
          </div>

          <div className="form-field">
            <label htmlFor="unitMode">Loại kho</label>
            <select
              id="unitMode"
              value={unitMode}
              onChange={(event) => handleModeChange(event.target.value)}
              disabled={!canLoadAvailability || isLoadingAvailability}
              required
            >
              <option value="">
                {isLoadingAvailability ? 'Đang kiểm tra sức chứa...' : 'Chọn loại kho'}
              </option>
              {modes.map((mode) => (
                <option key={mode} value={mode}>
                  {MODE_LABELS[mode]}
                </option>
              ))}
            </select>
          </div>

          <div className="form-field">
            <label htmlFor="unitTypeId">Kích thước kho</label>
            <select
              id="unitTypeId"
              value={unitTypeId}
              onChange={(event) => setUnitTypeId(event.target.value)}
              disabled={!unitMode || isLoadingAvailability}
              required
            >
              <option value="">Chọn kích thước</option>
              {sizeOptions.map((unitType) => {
                const unavailable = (unitType.availableCapacity ?? 0) <= 0
                return (
                  <option
                    key={unitType.unitTypeId}
                    value={unitType.unitTypeId}
                    disabled={unavailable}
                  >
                    {unitType.size} — {unitType.name} — còn{' '}
                    {unitType.availableCapacity ?? 0}
                  </option>
                )
              })}
            </select>
          </div>

          {canLoadAvailability &&
            !isLoadingAvailability &&
            unitTypes.length === 0 &&
            !catalogError && (
              <div className="notice form-field-wide">
                Không có loại kho phù hợp trong khoảng tháng đã chọn.
              </div>
            )}

          {selectedUnitType && (
            <aside className="selection-summary form-field-wide">
              <div>
                <span>Loại kho đã chọn</span>
                <strong>{selectedUnitType.name}</strong>
              </div>
              <div>
                <span>Kích thước</span>
                <strong>{selectedUnitType.size}</strong>
              </div>
              <div>
                <span>Sức chứa còn lại</span>
                <strong>{selectedUnitType.availableCapacity ?? 0}</strong>
              </div>
              <div>
                <span>Giá hiện tại tham khảo</span>
                <strong>{formatMoney(selectedUnitType.rentalPrice)}</strong>
              </div>
            </aside>
          )}

          <div className="form-field-wide">
            <ApiErrorAlert error={submitError} />
          </div>

          <button
            className="button form-field-wide"
            type="submit"
            disabled={isSubmitting || isLoadingAvailability}
          >
            {isSubmitting ? 'Đang tạo Reservation...' : 'Xác nhận đặt kho'}
          </button>
        </form>
      </section>
    </main>
  )
}
