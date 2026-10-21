import { Bell } from 'lucide-react'
import type { CustomerNotification } from '../features/customer-notifications/notification.types'
import { customerLabel } from '../utils/customerPresentation'
import { formatUtcDateTime, getCurrentBusinessDate } from '../utils/formatters'
import '../styles/customer-notifications.css'

function NotificationCard({ item }: { item: CustomerNotification }) {
  return <article className="notification-card">
    <span className="notification-icon"><Bell size={20} aria-hidden="true" /></span>
    <div className="notification-copy">
      <div className="notification-metadata"><span className="notification-type">{customerLabel(item.notificationType)}</span><span>Created <time dateTime={item.createdAt}>{formatUtcDateTime(item.createdAt)}</time></span></div>
      <p className="notification-message">{item.content}</p>
      <p className="notification-sent">Sent <time dateTime={item.sentAt}>{formatUtcDateTime(item.sentAt)}</time></p>
    </div>
  </article>
}

export function CustomerNotificationList({ items, compact = false }: { items: readonly CustomerNotification[]; compact?: boolean }) {
  if (compact) return <div className="customer-notification-list notification-list-compact">{items.map(item => <NotificationCard key={item.notificationId} item={item} />)}</div>
  const groups = new Map<string, CustomerNotification[]>()
  for (const item of items) {
    const day = getCurrentBusinessDate(new Date(item.createdAt))
    const group = groups.get(day) ?? []
    group.push(item); groups.set(day, group)
  }
  const today = getCurrentBusinessDate()
  return <div className="customer-notification-list">{Array.from(groups, ([day, notifications]) => <section className="notification-group" key={day} aria-label={day === today ? 'Today' : `Created ${day}`}>
    <h2>{day === today ? 'Today' : day}</h2>
    {notifications.map(item => <NotificationCard key={item.notificationId} item={item} />)}
  </section>)}</div>
}
