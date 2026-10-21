// IMPL-CUSACT-01: sent delivery history, not a read/unread inbox.
export interface CustomerNotification {
  notificationId: string
  notificationType: string
  content: string
  createdAt: string
  sentAt: string
}
