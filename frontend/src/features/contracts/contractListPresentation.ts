import type { ApiCollectionResponse } from '../../api/api.types'
import type { HandoverContractDetail } from '../handover/contract.types'
import { isUuid } from '../handover/handoverValidation'
import { isRentalMonth } from '../renewal/renewalValidation'
import { isSupportPagination } from '../customer-support/supportValidation'

// CON-001 currently returns the BE summary projection; CON-002 supplies the
// original Reservation reference. Also accept canonical detail-shaped list rows.
export type ContractListItem = Pick<HandoverContractDetail, 'contractId' | 'customerId' | 'facilityId' | 'storageUnitId' | 'startMonth' | 'endMonth' | 'status'> &
  Partial<Pick<HandoverContractDetail, 'reservationId' | 'unitCode' | 'unitTypeName' | 'storageUnit'>> & { unitTypeId?: string }
export const CONTRACT_TABS = ['Active Contracts', 'Contract History'] as const
export interface ContractFilters { query: string; facility: string; status: ContractListItem['status'] | '' }

function validUnitDisplay(unit: ContractListItem['storageUnit'], storageUnitId: string): boolean {
  return unit === undefined || Boolean(unit && unit.storageUnitId === storageUnitId && isUuid(unit.unitTypeId) &&
    typeof unit.unitCode === 'string' && typeof unit.unitTypeName === 'string')
}
export function isContractListResponse(value: unknown, page: number, expectedPageSize = 20): value is ApiCollectionResponse<ContractListItem> {
  const response = value as ApiCollectionResponse<ContractListItem> | null
  return Boolean(response && Array.isArray(response.data) && isSupportPagination(response.pagination) &&
    response.pagination.page === page && response.pagination.pageSize === expectedPageSize && response.data.length <= expectedPageSize &&
    response.pagination.totalItems >= response.data.length && response.pagination.totalPages === Math.ceil(response.pagination.totalItems / expectedPageSize) &&
    new Set(response.data.map(item => item?.contractId)).size === response.data.length && response.data.every(item => item &&
      isUuid(item.contractId) && isUuid(item.customerId) && isUuid(item.facilityId) && isUuid(item.storageUnitId) &&
      isRentalMonth(item.startMonth) && isRentalMonth(item.endMonth) && item.endMonth >= item.startMonth &&
      ['ACTIVE', 'COMPLETED', 'TERMINATED'].includes(item.status) &&
      (item.reservationId === undefined || isUuid(item.reservationId)) &&
      (item.unitTypeId === undefined || isUuid(item.unitTypeId)) &&
      (item.unitCode === undefined || typeof item.unitCode === 'string') &&
      (item.unitTypeName === undefined || typeof item.unitTypeName === 'string') && validUnitDisplay(item.storageUnit, item.storageUnitId)))
}
