import { useEffect, useRef } from 'react'
import { X } from 'lucide-react'
import { StaffContractPage } from '../pages/StaffContractPage'
import '../styles/staff-contracts.css'

export function StaffContractDrawer({ contractId, onClose }: { contractId: string; onClose(): void }) {
  const ref = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const dialog = ref.current
    const previousFocus = document.activeElement
    const overflow = document.body.style.overflow
    dialog?.showModal()
    document.body.style.overflow = 'hidden'
    return () => {
      dialog?.close()
      document.body.style.overflow = overflow
      if (previousFocus instanceof HTMLElement) previousFocus.focus()
    }
  }, [])

  return <dialog ref={ref} className="staff-contract-drawer" aria-label="Contract details" onCancel={event => {
    event.preventDefault()
    onClose()
  }} onClick={event => {
    if (event.target === event.currentTarget) {
      const bounds = event.currentTarget.getBoundingClientRect()
      if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) onClose()
    }
  }}>
    <div className="staff-contract-drawer-header">
      <span>Facility contracts · Read only</span>
      <button className="button button-secondary" type="button" onClick={onClose} aria-label="Close contract details" autoFocus><X size={20} aria-hidden="true" /></button>
    </div>
    <StaffContractPage key={contractId} selectedContractId={contractId} />
  </dialog>
}
