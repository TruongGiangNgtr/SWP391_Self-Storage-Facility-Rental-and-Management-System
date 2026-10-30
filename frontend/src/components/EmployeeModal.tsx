import { useEffect, useRef, useState, type ReactNode } from 'react'
import '../styles/employeeModal.css'

let openModals = 0
let previousOverflow = ''

interface Props {
  children: ReactNode
  className?: string
  label?: string
  labelledBy?: string
  busy?: boolean
  role?: 'dialog' | 'alertdialog'
  onClose(): void
}

/** Shared native modal behavior for employee dialogs and drawers. */
export function EmployeeModal({ children, className = '', label, labelledBy, busy = false, role = 'dialog', onClose }: Props) {
  const ref = useRef<HTMLDialogElement>(null)
  const [opener] = useState(() => document.activeElement)

  useEffect(() => {
    const dialog = ref.current
    dialog?.showModal()
    const initial = dialog?.querySelector<HTMLElement>('[autofocus]:not(:disabled), button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled)')
    initial?.focus()
    if (openModals === 0) previousOverflow = document.body.style.overflow
    openModals += 1
    document.body.style.overflow = 'hidden'
    return () => {
      dialog?.close()
      openModals -= 1
      if (openModals === 0) document.body.style.overflow = previousOverflow
      requestAnimationFrame(() => {
        if (opener instanceof HTMLElement && opener !== document.body && opener.isConnected && !opener.closest('[inert]')) {
          opener.focus()
        } else if (!document.querySelector('dialog[open]')) {
          document.querySelector<HTMLElement>('main[tabindex="-1"]')?.focus()
        }
      })
    }
  }, [opener])

  return <dialog ref={ref} role={role} className={`employee-modal-frame ${className}`} aria-label={label}
    aria-labelledby={labelledBy} aria-busy={busy} tabIndex={-1}
    onCancel={event => { event.preventDefault(); if (!busy) onClose() }}
    onMouseDown={event => {
      if (!busy && (event.target === event.currentTarget ||
        (event.target instanceof HTMLElement && event.target.hasAttribute('data-admin-backdrop')))) onClose()
    }}
    onKeyDown={event => {
      if (event.key !== 'Tab') return
      const controls = Array.from(event.currentTarget.querySelectorAll<HTMLElement>('button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]'))
        .filter(element => element.tabIndex >= 0 && !element.matches(':disabled') && element.getClientRects().length > 0)
      const first = controls[0], last = controls.at(-1)
      if (!first) { event.preventDefault(); event.currentTarget.focus() }
      else if (event.shiftKey && (document.activeElement === first || document.activeElement === event.currentTarget)) {
        event.preventDefault(); last?.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault(); first.focus()
      }
    }}
  >{children}</dialog>
}
