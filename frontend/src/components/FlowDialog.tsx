import { useEffect, useId, useRef, type ReactNode } from 'react'
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
      className="frms-customer flow-dialog"
      ref={dialogRef}
      aria-labelledby={titleId}
      onCancel={(event) => {
        event.preventDefault()
        if (!busy) onClose()
      }}
    >
      <div className="flow-dialog-heading">
        <div><p className="eyebrow">{category}</p><h1 id={titleId}>{title}</h1></div>
        <button className="dialog-close" type="button" disabled={busy} onClick={onClose} aria-label="Close">
          <FlowIcon name="close" />
        </button>
      </div>
      <p className="muted dialog-subtitle">{subtitle}</p>
      {children}
    </dialog>
  )
}
