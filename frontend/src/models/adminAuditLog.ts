export interface AdminAuditLogEntry {
  auditLogId: string
  userAccountId: string | null
  action: string
  entityType: string
  entityId: string | null
  oldValue: string | null
  newValue: string | null
  createdAt: string
}

export interface AdminAuditLogQuery {
  userAccountId?: string
  entityType?: string
  entityId?: string
  action?: string
  fromUtc?: string
  toUtc?: string
  page?: number
  pageSize?: number
}

export interface AdminAuditLogFilters {
  userAccountId: string
  entityType: string
  entityId: string
  action: string
  fromLocal: string
  toLocal: string
}
