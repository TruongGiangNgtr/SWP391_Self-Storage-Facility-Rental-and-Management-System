import type { ApiCollectionResponse, ApiResponse } from './api.types'
import { httpClient } from './httpClient'
import type {
  CompleteInspectionRequest,
  DamageRecordDetail,
  DamageTypeDetail,
  EvidenceType,
  ExtraFeeDetail,
  FinalizeReturnRequest,
  FinalizeReturnResult,
  InspectionDetail,
  InspectionEvidenceDetail,
  RecordDamageRequest,
  RecordExtraFeeRequest,
} from '../features/return-management/return.types'

export const inspectionApi = {
  list(page = 1, pageSize = 20): Promise<ApiCollectionResponse<InspectionDetail>> {
    return httpClient.get(`/inspections?page=${page}&pageSize=${pageSize}`)
  },

  get(inspectionId: string): Promise<ApiResponse<InspectionDetail>> {
    return httpClient.get(`/inspections/${encodeURIComponent(inspectionId)}`)
  },

  claim(inspectionId: string): Promise<ApiResponse<InspectionDetail>> {
    return httpClient.post(`/inspections/${encodeURIComponent(inspectionId)}/claim`, {})
  },

  recordDamage(inspectionId: string, request: RecordDamageRequest): Promise<ApiResponse<DamageRecordDetail>> {
    return httpClient.post(`/inspections/${encodeURIComponent(inspectionId)}/damages`, request)
  },

  recordExtraFee(inspectionId: string, request: RecordExtraFeeRequest): Promise<ApiResponse<ExtraFeeDetail>> {
    return httpClient.post(`/inspections/${encodeURIComponent(inspectionId)}/extra-fees`, request)
  },

  uploadEvidence(inspectionId: string, file: File, evidenceType: EvidenceType): Promise<ApiResponse<InspectionEvidenceDetail>> {
    const body = new FormData()
    body.set('file', file)
    body.set('evidenceType', evidenceType)
    return httpClient.postForm(`/inspections/${encodeURIComponent(inspectionId)}/evidence`, body)
  },

  complete(inspectionId: string, request: CompleteInspectionRequest): Promise<ApiResponse<InspectionDetail>> {
    return httpClient.post(`/inspections/${encodeURIComponent(inspectionId)}/complete`, request)
  },

  finalizeReturn(contractId: string, request: FinalizeReturnRequest): Promise<ApiResponse<FinalizeReturnResult>> {
    return httpClient.post(`/contracts/${encodeURIComponent(contractId)}/finalize-return`, request)
  },

  listDamageTypes(): Promise<ApiResponse<DamageTypeDetail[]>> {
    return httpClient.get('/damage-types')
  },
}
