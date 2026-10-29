import { FlowIcon, type FlowIconName } from './FlowIcon'

const STATUS_ICONS: Record<string, FlowIconName> = {
  PAID: 'paid', SUCCESS: 'paid', COMPLETED: 'complete', CANCELLED: 'cancel',
  CONFIRMED: 'calendar', SCHEDULED: 'calendar',
}

const DEFAULT_LABELS: Record<string, string> = {
  TERMINATED: 'Closed',
}

export function StatusBadge({ status, label }: { status: string; label?: string }) {
  const normalized = status.toLowerCase().replaceAll('_', '-')
  const icon = STATUS_ICONS[status]
  return <span className={`status-badge status-${normalized}`}>{icon && <FlowIcon name={icon} />}{label ?? DEFAULT_LABELS[status] ?? status}</span>
}
