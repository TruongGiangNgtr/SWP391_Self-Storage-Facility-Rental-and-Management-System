export type FrmsRole = 'CUSTOMER' | 'FACILITY_STAFF' | 'FACILITY_MANAGER' | 'BUSINESS_OPERATIONS_MANAGER' | 'SYSTEM_ADMINISTRATOR'
export type AccountStatus = 'ACTIVE' | 'INACTIVE'
export type ApiResponse<T> = { data: T; message?: string }
export type ApiError = { code: string; message: string; traceId: string; errors?: Record<string, string[]> }
export type AuthenticatedUser = { userAccountId: string; role: FrmsRole; status: AccountStatus }
export type AuthTokenData = { accessToken: string; tokenType: 'Bearer'; user: AuthenticatedUser }
