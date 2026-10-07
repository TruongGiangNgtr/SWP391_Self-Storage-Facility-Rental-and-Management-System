import type {
  ApiCollectionResponse,
  ApiResponse,
} from './api.types'
import { httpClient } from './httpClient'

export type InspectionStatus = 'PENDING' | 'IN_PROGRESS' | 'COMPLETED'
export type DamageRecordStatus = 'PENDING' | 'APPROVED' | 'REJECTED'
export type DamageDecision = 'APPROVED' | 'REJECTED'
export type EvidenceType = 'IMAGE' | 'VIDEO' | 'DOCUMENT'

export interface DamageType {
  damageTypeId: string
  name: string
  defaultAmount: number | null
  status: 'ACTIVE' | 'INACTIVE'
}

export interface DamageRecord {
  damageRecordId: string
  inspectionId?: string
  damageTypeId: string
  damageAmount: number
  note: string | null
  status: DamageRecordStatus
  createdAt?: string
}

export interface InspectionEvidence {
  inspectionEvidenceId: string
  inspectionId?: string
  evidenceType: EvidenceType
  createdAt?: string
}

export interface ExtraFee {
  extraFeeId: string
  inspectionId?: string
  extraFeeTypeId: string
  amount: number
  reason: string
  createdAt?: string
}

export interface InspectionListItem {
  inspectionId: string
  contractId: string
  storageUnitId: string
  visitId?: string | null
  employeeId?: string | null
  status: InspectionStatus
  conditionNote?: string | null
  completedAt?: string | null
}

export interface InspectionDetail extends InspectionListItem {
  visitId: string | null
  employeeId: string | null
  conditionNote: string | null
  completedAt: string | null
  damages: DamageRecord[]
  extraFees: ExtraFee[]
  evidence: InspectionEvidence[]
}

export interface DecideDamageRequest {
  decision: DamageDecision
}

async function readAllPages(): Promise<InspectionListItem[]> {
  const firstPage = await inspectionApi.list(1, 100)
  const items = [...firstPage.data]

  for (
    let page = 2;
    page <= firstPage.pagination.totalPages;
    page += 1
  ) {
    const response = await inspectionApi.list(page, 100)
    items.push(...response.data)
  }

  return items
}

export const inspectionApi = {
  list(
    page = 1,
    pageSize = 20,
  ): Promise<ApiCollectionResponse<InspectionListItem>> {
    return httpClient.get(
      `/inspections?page=${page}&pageSize=${pageSize}`,
    )
  },

  listAll(): Promise<InspectionListItem[]> {
    return readAllPages()
  },

  get(
    inspectionId: string,
  ): Promise<ApiResponse<InspectionDetail>> {
    return httpClient.get(
      `/inspections/${encodeURIComponent(inspectionId)}`,
    )
  },

  decideDamage(
    damageRecordId: string,
    request: DecideDamageRequest,
  ): Promise<ApiResponse<unknown>> {
    return httpClient.post(
      `/damage-records/${encodeURIComponent(damageRecordId)}/decision`,
      request,
    )
  },

  listDamageTypes(): Promise<ApiResponse<DamageType[]>> {
    return httpClient.get('/damage-types')
  },
}
