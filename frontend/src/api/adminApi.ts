import type {
  AdminApiErrorShape,
  AdminUserAccount,
  AdminUserListQuery,
} from '../models/adminUser'
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
