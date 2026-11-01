import { FlowIcon, type FlowIconName } from './FlowIcon'

const STATUS_ICONS: Record<string, FlowIconName> = {
  PAID: 'paid',
  SUCCESS: 'paid',
  COMPLETED: 'complete',
  CANCELLED: 'cancel',
  CONFIRMED: 'calendar',
  SCHEDULED: 'calendar',
}

const DEFAULT_LABELS: Record<string, string> = {
  TERMINATED: 'Closed',
}

export interface StatusBadgeProps {
  status: string
  label?: string
  /**
   * @deprecated Shiny text sweep was distilled out to eliminate text invisibility
   * and meet WCAG 2.1 AA contrast requirements. Maintained for interface compatibility.
   */
  shiny?: boolean
}

export function StatusBadge({ status, label }: StatusBadgeProps) {
  const normalized = status.toLowerCase().replaceAll('_', '-')
  const icon = STATUS_ICONS[status]
  const displayText = label ?? DEFAULT_LABELS[status] ?? status

  return (
    <span className={`status-badge status-${normalized}`}>
      {icon && <FlowIcon name={icon} />}
      {displayText}
    </span>
  )
}

export default StatusBadge
