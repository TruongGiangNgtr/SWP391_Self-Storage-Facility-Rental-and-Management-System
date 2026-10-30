import type { ReactNode } from 'react'
import { EmployeeModal } from '../EmployeeModal'

interface Props {
  children: ReactNode
  className: string
  label: string
  busy?: boolean
  onClose(): void
}

/** Preserve the Admin presentation on the shared employee modal behavior. */
export function AdminModal({ className, ...props }: Props) {
  return <EmployeeModal {...props} className={`admin-modal-frame ${className}`} />
}
