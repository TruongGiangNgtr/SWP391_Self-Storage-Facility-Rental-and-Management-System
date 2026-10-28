// OD-06 / IMPL-CUSACT-01: in-app activity, independent of email delivery.
export interface CustomerNotification {
  notificationId: string
  notificationType: string
  content: string
  createdAt: string
}
