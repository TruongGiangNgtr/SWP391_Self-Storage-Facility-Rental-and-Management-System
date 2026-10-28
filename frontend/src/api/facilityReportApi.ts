import type { ApiResponse, Pagination } from './api.types'
import { httpClient } from './httpClient'

/** REP-001 is a server-calculated snapshot, not a client-side occupancy formula. */
export interface FacilityOperationsReport {
  facilityId: string
  asOf: string
  availableUnits: number
  inUseUnits: number
  inspectionUnits: number
  maintenanceUnits: number
  usageRate: number
  overdueContractCount: number
}

export interface ReportUnitCounts {
  availableUnits: number
  inUseUnits: number
  inspectionUnits: number
  maintenanceUnits: number
}

export interface FacilityUnitReport {
  facilityId: string
  month: string
  asOf: string
  inventoryScope: 'CURRENT'
  historicalInventorySupported: false
  unitTypes: (ReportUnitCounts & {
    unitTypeId: string; name: string; size: string; totalUnits: number; usageRate: number
  })[]
  inventory: {
    storageUnitId: string; unitCode: string; unitTypeId: string; unitTypeName: string
    unitTypeSize: string; locationInfo: string | null
    status: 'AVAILABLE' | 'IN_USE' | 'INSPECTION' | 'MAINTENANCE'
    contractId: string | null; customerId: string | null; customerName: string | null; endMonth: string | null
  }[]
  pagination: Pagination
  dailyTrend: (ReportUnitCounts & {
    date: string; knownUnits: number; unknownUnits: number; usageRate: number | null
  })[]
  averageOccupancy: number | null
  peakDay: string | null
}

export interface FacilityRevenueReport {
  facilityId: string
  month: string
  firstMonthRecognizedRevenue: number
  subsequentPaidRentalRevenue: number
  rentalRevenue: number
}

export const facilityReportApi = {
  operations(facilityId: string, signal?: AbortSignal): Promise<ApiResponse<FacilityOperationsReport>> {
    return httpClient.get(`/reports/facilities/${encodeURIComponent(facilityId)}/operations`, signal)
  },
  units(facilityId: string, month: string, page: number, signal?: AbortSignal): Promise<ApiResponse<FacilityUnitReport>> {
    const query = new URLSearchParams({ month, page: String(page), pageSize: '20' })
    return httpClient.get(`/reports/facilities/${encodeURIComponent(facilityId)}/units?${query}`, signal)
  },
  revenue(facilityId: string, month: string, signal?: AbortSignal): Promise<ApiResponse<FacilityRevenueReport>> {
    return httpClient.get(`/reports/facilities/${encodeURIComponent(facilityId)}/revenue?${new URLSearchParams({ month })}`, signal)
  },
}
