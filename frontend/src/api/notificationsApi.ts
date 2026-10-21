import { httpClient } from './httpClient'
import { readNotificationCollection } from '../features/customer-notifications/notificationValidation'

export const notificationsApi = {
  async list(page = 1, pageSize = 20, signal?: AbortSignal) {
    const response = await httpClient.get<unknown>(`/customers/me/notifications?page=${page}&pageSize=${pageSize}`, signal)
    return readNotificationCollection(response, page, pageSize)
  },
}
