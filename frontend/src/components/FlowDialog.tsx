import { useEffect, useId, useRef, type ReactNode } from 'react'
import { FlowIcon } from './FlowIcon'
import '../styles/customer.css'

interface FlowDialogProps {
  className?: string
  headingIcon?: ReactNode
  title: string
  subtitle: string
  category: string
  busy?: boolean
  trapFocus?: boolean
  onClose(): void
  children: ReactNode
}

export function FlowDialog({ title, subtitle, category, className = '', headingIcon, busy = false, trapFocus = false, onClose, children }: FlowDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const restoreTarget = useRef<{ element: HTMLElement; href: string | null } | null>(null)
  const titleId = useId()
  useEffect(() => {
    const dialog = dialogRef.current
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null
    if (opener && !dialog?.contains(opener)) {
      restoreTarget.current = { element: opener, href: opener instanceof HTMLAnchorElement ? opener.getAttribute('href') : null }
    }
    dialog?.showModal()
    return () => {
      dialog?.close()
      // A remounted background may need its GET to finish before its opener exists.
      requestAnimationFrame(() => {
        if (document.querySelector('dialog[open]')) return
        const target = restoreTarget.current
        const restore = () => {
          const element = target?.element.isConnected ? target.element : target?.href
            ? Array.from(document.querySelectorAll<HTMLAnchorElement>('a[href]')).find(link => link.getAttribute('href') === target.href)
            : null
          if (!element) return false
          element.focus()
          return true
        }
        if (restore() || !target?.href) return
        const observer = new MutationObserver(() => { if (restore()) observer.disconnect() })
        observer.observe(document.body, { childList: true, subtree: true })
        window.setTimeout(() => observer.disconnect(), 2000)
      })
    }
  }, [])

  return (
    <dialog
      className={`frms-customer flow-dialog ${className}`}
      ref={dialogRef}
      aria-labelledby={titleId}
      onKeyDown={(event) => {
        if (!trapFocus || event.key !== 'Tab') return
        const dialog = event.currentTarget
        const controls = Array.from(dialog.querySelectorAll<HTMLElement>('button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]'))
          .filter(element => element.tabIndex >= 0 && element.getClientRects().length > 0)
        const first = controls[0], last = controls.at(-1)
        if (!first) { event.preventDefault(); dialog.focus(); return }
        if (event.shiftKey && (document.activeElement === first || document.activeElement === dialog)) {
          event.preventDefault(); last?.focus()
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault(); first.focus()
        }
      }}
      onCancel={(event) => {
        event.preventDefault()
        if (!busy) onClose()
      }}
    >
      <div className="flow-dialog-heading">
        {headingIcon && <span className="flow-dialog-heading-icon" aria-hidden="true">{headingIcon}</span>}
        <div>{category && <p className="eyebrow">{category}</p>}<h1 id={titleId}>{title}</h1></div>
        <button className="dialog-close" type="button" disabled={busy} onClick={onClose} aria-label="Close">
          <FlowIcon name="close" />
        </button>
      </div>
      <p className="muted dialog-subtitle">{subtitle}</p>
      {children}
    </dialog>
  )
}
