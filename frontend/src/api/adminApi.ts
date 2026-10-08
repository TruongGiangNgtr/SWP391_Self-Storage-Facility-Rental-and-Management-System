import type {
  AdminAuditLogEntry,
  AdminAuditLogQuery,
} from '../models/adminAuditLog'
import type {
  AdminApiErrorShape,
  AdminCustomerStatusResult,
  AdminUserAccount,
  AdminUserListQuery,
} from '../models/adminUser'
import type {
  AdminLoginHistoryEntry,
  AdminLoginHistoryQuery,
} from '../models/adminLoginHistory'
import type {
  AssignAdminEmployeeRequest,
  CreateAdminEmployeeRequest,
  UpdateAdminEmployeeRequest,
} from '../models/adminEmployee'
import type {
  ApiCollectionResponse,
  ApiResponse,
} from './api.types'
import { ApiRequestError, httpClient } from './httpClient'

/**
 * AWP-01 / SRS V10 FINAL
 * ADM-001 GET /api/v1/admin/users
 * ADM-002 GET /api/v1/admin/users/{userAccountId}
 *
 * httpClient already prefixes VITE_API_BASE_URL (default /api/v1),
 * so the paths below intentionally start at /admin.
 */
export function listAdminUsers(
  query: AdminUserListQuery = {},
): Promise<ApiCollectionResponse<AdminUserAccount>> {
  const page = query.page ?? 1
  const pageSize = query.pageSize ?? 20

  return httpClient.get(
    `/admin/users?page=${encodeURIComponent(String(page))}&pageSize=${encodeURIComponent(String(pageSize))}`,
  )
}

export function getAdminUser(
  userAccountId: string,
): Promise<ApiResponse<AdminUserAccount>> {
  return httpClient.get(
    `/admin/users/${encodeURIComponent(userAccountId)}`,
  )
}

/** AWP-02 / ADM-003 */
export function activateAdminCustomer(
  customerId: string,
): Promise<ApiResponse<AdminCustomerStatusResult>> {
  return httpClient.post(
    `/admin/customers/${encodeURIComponent(customerId)}/activate`,
    {},
  )
}

/** AWP-02 / ADM-004 */
export function deactivateAdminCustomer(
  customerId: string,
): Promise<ApiResponse<AdminCustomerStatusResult>> {
  return httpClient.post(
    `/admin/customers/${encodeURIComponent(customerId)}/deactivate`,
    {},
  )
}

/** AWP-06 / ADM-010 */
export function listAdminLoginHistory(
  query: AdminLoginHistoryQuery = {},
): Promise<ApiCollectionResponse<AdminLoginHistoryEntry>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page ?? 1))
  params.set('pageSize', String(query.pageSize ?? 20))

  if (query.userAccountId) params.set('userAccountId', query.userAccountId)
  if (query.status) params.set('status', query.status)
  if (query.fromUtc) params.set('fromUtc', query.fromUtc)
  if (query.toUtc) params.set('toUtc', query.toUtc)

  return httpClient.get(`/admin/login-history?${params.toString()}`)
}

/** AWP-07 / ADM-011 */
export function listAdminAuditLogs(
  query: AdminAuditLogQuery = {},
): Promise<ApiCollectionResponse<AdminAuditLogEntry>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page ?? 1))
  params.set('pageSize', String(query.pageSize ?? 20))

  if (query.userAccountId) params.set('userAccountId', query.userAccountId)
  if (query.entityType) params.set('entityType', query.entityType)
  if (query.entityId) params.set('entityId', query.entityId)
  if (query.action) params.set('action', query.action)
  if (query.fromUtc) params.set('fromUtc', query.fromUtc)
  if (query.toUtc) params.set('toUtc', query.toUtc)

  return httpClient.get(`/admin/audit-logs?${params.toString()}`)
}

/** AWP-03 / ADM-005 */
export function createAdminEmployee(
  request: CreateAdminEmployeeRequest,
): Promise<ApiResponse<AdminUserAccount>> {
  return httpClient.post('/admin/employees', request)
}

/** AWP-03 / ADM-006 */
export function updateAdminEmployee(
  employeeId: string,
  request: UpdateAdminEmployeeRequest,
): Promise<ApiResponse<AdminUserAccount>> {
  return httpClient.patch(
    `/admin/employees/${encodeURIComponent(employeeId)}`,
    request,
  )
}

/** AWP-03 / ADM-007 */
export function activateAdminEmployee(
  employeeId: string,
): Promise<ApiResponse<AdminUserAccount>> {
  return httpClient.post(
    `/admin/employees/${encodeURIComponent(employeeId)}/activate`,
    {},
  )
}

/** AWP-03 / ADM-008 */
export function deactivateAdminEmployee(
  employeeId: string,
): Promise<ApiResponse<AdminUserAccount>> {
  return httpClient.post(
    `/admin/employees/${encodeURIComponent(employeeId)}/deactivate`,
    {},
  )
}

/** AWP-03 / ADM-012 */
export function resendAdminEmployeeInitialCredential(
  employeeId: string,
): Promise<ApiResponse<AdminUserAccount>> {
  return httpClient.post(
    `/admin/employees/${encodeURIComponent(employeeId)}/resend-initial-credential`,
    {},
  )
}

/** AWP-04 / ADM-009 */
export function assignAdminEmployee(
  employeeId: string,
  request: AssignAdminEmployeeRequest,
): Promise<ApiResponse<AdminUserAccount>> {
  return httpClient.put(
    `/admin/employees/${encodeURIComponent(employeeId)}/assignment`,
    request,
  )
}

export function normalizeAdminApiError(error: unknown): AdminApiErrorShape {
  if (error instanceof ApiRequestError) {
    return {
      code: error.code,
      message: error.message,
      traceId: error.traceId,
      errors: error.errors,
    }
  }

  return {
    code: 'UNEXPECTED_ERROR',
    message: error instanceof Error
      ? error.message
      : 'An unexpected error occurred.',
  }
}
