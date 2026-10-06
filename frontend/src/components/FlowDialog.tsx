import { useEffect, useRef, type ReactNode } from 'react'
import { FlowIcon } from './FlowIcon'
import '../styles/customer.css'

interface FlowDialogProps {
  title: string
  subtitle: string
  category: string
  busy?: boolean
  onClose(): void
  children: ReactNode
}

export function FlowDialog({ title, subtitle, category, busy = false, onClose, children }: FlowDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const dialog = dialogRef.current
    dialog?.showModal()
    return () => dialog?.close()
  }, [])

  return (
    <dialog
      className="frms-customer flow-dialog"
      ref={dialogRef}
      aria-labelledby="flow-dialog-title"
      onCancel={(event) => {
        event.preventDefault()
        if (!busy) onClose()
      }}
    >
      <div className="flow-dialog-heading">
        <div><p className="eyebrow">{category}</p><h1 id="flow-dialog-title">{title}</h1></div>
        <button className="dialog-close" type="button" disabled={busy} onClick={onClose} aria-label="Close">
          <FlowIcon name="close" />
        </button>
      </div>
      <p className="muted dialog-subtitle">{subtitle}</p>
      {children}
    </dialog>
  )
}
