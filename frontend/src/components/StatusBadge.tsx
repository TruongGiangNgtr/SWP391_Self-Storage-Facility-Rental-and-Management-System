import { FlowIcon, type FlowIconName } from './FlowIcon'
import { ShinyText } from './react-bits/ShinyText'

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
  shiny?: boolean
}

export function StatusBadge({ status, label, shiny = true }: StatusBadgeProps) {
  const normalized = status.toLowerCase().replaceAll('_', '-')
  const icon = STATUS_ICONS[status]
  const displayText = label ?? DEFAULT_LABELS[status] ?? status

  return (
    <span className={`status-badge status-${normalized}`}>
      {icon && <FlowIcon name={icon} />}
      {shiny ? (
        <ShinyText speed={3.5} className="status-badge-shiny">
          {displayText}
        </ShinyText>
      ) : (
        displayText
      )}
    </span>
  )
}

export default StatusBadge
