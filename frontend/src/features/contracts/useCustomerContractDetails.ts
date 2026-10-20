import { useState } from 'react'
import { presentValidationError } from '../../api/apiErrorPresentation'
import { reservationApi } from '../../api/reservationApi'
import { useAuth } from '../../auth/auth.context'
import { isOverviewContract, isOverviewReservation } from '../customer-overview/overviewValidation'
import { useOverviewDetail } from '../customer-overview/useOverviewCollection'
import type { HandoverContractDetail } from '../handover/contract.types'
import { isUuid } from '../handover/handoverValidation'
import { useReservationFacilities } from '../reservations/useReservationUnitTypes'
import { useAccessContract } from '../visits/useAccessContract'

// Presentation checks only; permissions and command eligibility remain server-owned.
function isDisplayContract(value: HandoverContractDetail): boolean {
  const unit = value?.storageUnit
  return isOverviewContract(value) && isUuid(value.customerId) && value.endMonth >= value.startMonth &&
    (unit === undefined || Boolean(unit && unit.storageUnitId === value.storageUnitId && isUuid(unit.unitTypeId) &&
      typeof unit.unitCode === 'string' && typeof unit.unitTypeName === 'string' &&
      ['PRIVATE', 'PUBLIC'].includes(unit.unitTypeMode) && typeof unit.unitTypeSize === 'string' &&
      (unit.locationInfo === null || typeof unit.locationInfo === 'string')))
}

export function useCustomerContractDetails(contractId: string) {
  const { user } = useAuth()
  const root = useAccessContract(contractId)
  const [revision, setRevision] = useState(0)
  const contract = root.contract && isDisplayContract(root.contract) && root.contract.customerId === user?.customerId ? root.contract : null
  const error = root.error ?? (root.contract && !contract ? presentValidationError('Contract details could not be verified. Refresh before choosing an action.') : null)
  const original = useOverviewDetail(contract?.reservationId ?? '', revision, reservationApi.get, isOverviewReservation)
  const matches = !original.data || Boolean(contract && original.data.customerId === contract.customerId && original.data.facilityId === contract.facilityId)
  const facilities = useReservationFacilities(contract ? [contract.facilityId] : [])
  return {
    contract, error, loading: root.loading,
    reservation: matches ? original.data : null,
    originalLoading: original.loading,
    originalError: original.error ?? (!matches ? presentValidationError('The original reservation does not match this contract. Refresh to verify the rental and deposit snapshot.') : null),
    facility: contract ? facilities.reads[contract.facilityId]?.data ?? null : null,
    facilityLoading: facilities.loading,
    facilityError: facilities.error,
    refresh: () => { root.refresh(); setRevision(value => value + 1); facilities.retry() },
  }
}
