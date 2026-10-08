export type LoginHistoryStatus = 'SUCCESS' | 'FAILED'

export interface AdminLoginHistoryEntry {
  loginHistoryId: string
  userAccountId: string
  loginAt: string
  ipAddress: string | null
  deviceInfo: string | null
  status: LoginHistoryStatus
}

export interface AdminLoginHistoryQuery {
  userAccountId?: string
  status?: LoginHistoryStatus
  fromUtc?: string
  toUtc?: string
  page?: number
  pageSize?: number
}

export interface AdminLoginHistoryFilters {
  userAccountId: string
  status: 'ALL' | LoginHistoryStatus
  fromLocal: string
  toLocal: string
}
