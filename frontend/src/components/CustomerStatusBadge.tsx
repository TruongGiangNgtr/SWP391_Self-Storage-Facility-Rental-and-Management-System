import { StatusBadge } from './StatusBadge'
import { customerLabel } from '../utils/customerPresentation'

// Keep employee portals and authoritative API status values unchanged.
export function CustomerStatusBadge({ status, label }: { status: string; label?: string }) {
  return <StatusBadge status={status} label={label ?? customerLabel(status)} />
}
