import type {
  AdminApiErrorShape,
  AdminUserAccount,
  AdminUserListQuery,
} from '../models/adminUser'
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
