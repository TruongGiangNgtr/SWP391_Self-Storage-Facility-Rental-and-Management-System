import type { ApiCollectionResponse } from '../../api/api.types'
import { isSupportPagination } from '../customer-support/supportValidation'
import { isUuid } from '../handover/handoverValidation'
import type { CustomerNotification } from './notification.types'

export class NotificationContractError extends Error {
  constructor() { super('We could not load your notifications. Refresh the page to check for updates.'); this.name = 'NotificationContractError' }
}

function isUtcTimestamp(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|\+00:00)$/.test(value) &&
    Number.isFinite(Date.parse(value)) && new Date(value).toISOString().slice(0, 19) === value.slice(0, 19)
}

function isNotification(value: unknown): value is CustomerNotification {
  if (!value || typeof value !== 'object') return false
  const item = value as CustomerNotification
  return isUuid(item.notificationId) && typeof item.notificationType === 'string' && Boolean(item.notificationType.trim()) &&
    typeof item.content === 'string' && isUtcTimestamp(item.createdAt) && isUtcTimestamp(item.sentAt)
}

export function readNotificationCollection(value: unknown, page: number, pageSize: number): ApiCollectionResponse<CustomerNotification> {
  const response = value as ApiCollectionResponse<CustomerNotification> | null
  if (!response || !Array.isArray(response.data) || !response.data.every(isNotification) ||
    !isSupportPagination(response.pagination) || response.pagination.page !== page || response.pagination.pageSize !== pageSize ||
    response.data.length > pageSize || response.pagination.totalItems < response.data.length ||
    response.pagination.totalPages !== Math.ceil(response.pagination.totalItems / pageSize) ||
    new Set(response.data.map(item => item.notificationId.toLowerCase())).size !== response.data.length) throw new NotificationContractError()
  return response
}
