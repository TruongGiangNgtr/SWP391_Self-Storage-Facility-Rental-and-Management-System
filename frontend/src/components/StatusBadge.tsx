import { FlowIcon, type FlowIconName } from './FlowIcon'

const STATUS_ICONS: Record<string, FlowIconName> = {
  PAID: 'paid', SUCCESS: 'paid', COMPLETED: 'complete', CANCELLED: 'cancel',
  CONFIRMED: 'calendar', SCHEDULED: 'calendar',
}

export function StatusBadge({ status }: { status: string }) {
  const normalized = status.toLowerCase().replaceAll('_', '-')
  const icon = STATUS_ICONS[status]
  return <span className={`status-badge status-${normalized}`}>{icon && <FlowIcon name={icon} />}{status}</span>
}
