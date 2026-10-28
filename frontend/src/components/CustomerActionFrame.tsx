import type { ReactNode } from 'react'
import { Link, useLocation, useNavigate, type Location } from 'react-router-dom'
import { FlowDialog } from './FlowDialog'

export function CustomerActionFrame({ contractId, returnTo, backLabel = 'View Contract', title, category, subtitle, className = '', headingIcon, busy = false, onClose, children }: {
  contractId?: string; returnTo?: string; backLabel?: string; title: string; category: string; subtitle: string; className?: string; headingIcon?: ReactNode; busy?: boolean; onClose?: () => void; children: ReactNode | ((close: () => void) => ReactNode)
}) {
  const location = useLocation()
  const navigate = useNavigate()
  const background = (location.state as { backgroundLocation?: Location } | null)?.backgroundLocation
  const backUrl = returnTo ?? `/customer/contracts/${encodeURIComponent(contractId ?? '')}`
  const hasBackground = background && (background.pathname === '/customer' || background.pathname.startsWith('/customer/'))
  const close = () => {
    if (busy) return
    if (onClose) { onClose(); return }
    if (hasBackground) navigate(`${background.pathname}${background.search}${background.hash}`, { replace: true, state: background.state })
    else navigate(backUrl)
  }
  const content = typeof children === 'function' ? children(close) : children
  if (hasBackground) {
    return <FlowDialog trapFocus title={title} category={category} subtitle={subtitle} className={className} headingIcon={headingIcon} busy={busy} onClose={close}>
      <div className="flow-page customer-action-content">{content}</div>
    </FlowDialog>
  }
  return <main className={`page-container flow-page narrow-page ${className}`}>
    <section className="page-heading page-heading-actions"><div>{headingIcon && <span className="flow-dialog-heading-icon" aria-hidden="true">{headingIcon}</span>}{category && <p className="eyebrow">{category}</p>}<h1>{title}</h1><p className="muted">{subtitle}</p></div><Link className="button button-secondary" to={backUrl} aria-disabled={busy || undefined} onClick={event => { if (busy) event.preventDefault() }}>{backLabel}</Link></section>
    {content}
  </main>
}
