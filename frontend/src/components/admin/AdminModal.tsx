import { useEffect, useRef, useState, type ReactNode } from 'react'

interface Props {
  children: ReactNode
  className: string
  label: string
  busy?: boolean
  onClose(): void
}

/** Native modal behavior, shared by Admin dialogs and detail drawers. */
export function AdminModal({ children, className, label, busy = false, onClose }: Props) {
  const ref = useRef<HTMLDialogElement>(null)
  const [opener] = useState(() => document.activeElement)

  useEffect(() => {
    const dialog = ref.current
    const overflow = document.body.style.overflow
    dialog?.showModal()
    document.body.style.overflow = 'hidden'
    return () => {
      dialog?.close()
      document.body.style.overflow = overflow
      requestAnimationFrame(() => {
        if (opener instanceof HTMLElement && opener.isConnected) {
          opener.focus()
        } else if (!document.querySelector('dialog[open]')) {
          document.getElementById('admin-main')?.focus()
        }
      })
    }
  }, [opener])

  return (
    <dialog
      ref={ref}
      className={`admin-modal-frame ${className}`}
      aria-label={label}
      aria-busy={busy}
      tabIndex={-1}
      onCancel={event => {
        event.preventDefault()
        if (!busy) onClose()
      }}
      onMouseDown={event => {
        const backdrop = event.target === event.currentTarget || (
          event.target instanceof HTMLElement && event.target.hasAttribute('data-admin-backdrop')
        )
        if (!busy && backdrop) onClose()
      }}
      onKeyDown={event => {
        if (event.key !== 'Tab') return
        const controls = Array.from(event.currentTarget.querySelectorAll<HTMLElement>('button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]'))
          .filter(element => element.tabIndex >= 0 && element.getClientRects().length > 0)
        const first = controls[0]
        const last = controls.at(-1)
        if (!first) {
          event.preventDefault()
          event.currentTarget.focus()
        } else if (event.shiftKey && (document.activeElement === first || document.activeElement === event.currentTarget)) {
          event.preventDefault()
          last?.focus()
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault()
          first.focus()
        }
      }}
    >
      {children}
    </dialog>
  )
}
