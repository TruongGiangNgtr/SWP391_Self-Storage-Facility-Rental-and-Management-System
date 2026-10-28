import type { ApiResponse, Pagination } from './api.types'
import { httpClient } from './httpClient'

export type BusinessReportView = 'facility' | 'unitType' | 'status'
export type BusinessExportView = BusinessReportView | 'revenue'
export interface BusinessReportFilters {
  facilityId?: string
  unitTypeId?: string
  year: number
  quarter?: number
}
export interface RentalRevenue {
  firstMonthRecognizedRevenue: number
  subsequentPaidRentalRevenue: number
  rentalRevenue: number
}
export interface PhysicalInventory {
  totalUnits: number
  availableUnits: number
  inUseUnits: number
  inspectionUnits: number
  maintenanceUnits: number
  usageRate: number
}
export interface BusinessReportSummary extends PhysicalInventory, RentalRevenue {
  facilityCount: number
  unitTypeCount: number
}
export interface BusinessReportGroup extends PhysicalInventory, RentalRevenue {
  facilityId: string | null
  facilityName: string | null
  unitTypeId: string | null
  unitTypeName: string | null
  unitTypeSize: string | null
}
export interface BusinessReportBreakdown {
  view: BusinessReportView
  year: number
  quarter: number | null
  asOf: string
  summary: BusinessReportSummary
  rows: BusinessReportGroup[]
  pagination: Pagination
}
export interface BusinessRevenueMonth extends RentalRevenue { month: string }
export interface BusinessReportMonthly extends RentalRevenue {
  year: number
  quarter: number | null
  facilityId: string | null
  unitTypeId: string | null
  months: BusinessRevenueMonth[]
}

function query(filters: BusinessReportFilters): URLSearchParams {
  const params = new URLSearchParams({ year: String(filters.year) })
  if (filters.quarter !== undefined) params.set('quarter', String(filters.quarter))
  if (filters.facilityId) params.set('facilityId', filters.facilityId)
  if (filters.unitTypeId) params.set('unitTypeId', filters.unitTypeId)
  return params
}

export const businessReportApi = {
  analysis(view: BusinessReportView, filters: BusinessReportFilters, page: number, signal?: AbortSignal): Promise<ApiResponse<BusinessReportBreakdown>> {
    const params = query(filters)
    params.set('view', view)
    params.set('page', String(page))
    params.set('pageSize', '20')
    return httpClient.get(`/reports/business/analysis?${params}`, signal)
  },
  monthly(filters: BusinessReportFilters, signal?: AbortSignal): Promise<ApiResponse<BusinessReportMonthly>> {
    return httpClient.get(`/reports/business/monthly?${query(filters)}`, signal)
  },
  export(view: BusinessExportView, filters: BusinessReportFilters, signal?: AbortSignal): Promise<Blob> {
    const params = query(filters)
    params.set('view', view)
    // Export covers the complete filtered report, independent of the UI page.
    return httpClient.getFile(`/reports/business/export?${params}`, signal)
  },
}
