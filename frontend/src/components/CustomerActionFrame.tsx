import type { ReactNode } from 'react'
import { Link, useLocation, useNavigate, type Location } from 'react-router-dom'
import { FlowDialog } from './FlowDialog'

export function CustomerActionFrame({ contractId, returnTo, backLabel = 'View Contract', title, category, subtitle, busy = false, children }: {
  contractId?: string; returnTo?: string; backLabel?: string; title: string; category: string; subtitle: string; busy?: boolean; children: ReactNode
}) {
  const location = useLocation()
  const navigate = useNavigate()
  const background = (location.state as { backgroundLocation?: Location } | null)?.backgroundLocation
  const backUrl = returnTo ?? `/customer/contracts/${encodeURIComponent(contractId ?? '')}`
  if (background && (background.pathname === '/customer' || background.pathname.startsWith('/customer/'))) {
    return <FlowDialog trapFocus title={title} category={category} subtitle={subtitle} busy={busy} onClose={() => navigate(`${background.pathname}${background.search}${background.hash}`, { replace: true, state: background.state })}>
      <div className="flow-page customer-action-content">{children}</div>
    </FlowDialog>
  }
  return <main className="page-container flow-page narrow-page">
    <section className="page-heading page-heading-actions"><div><p className="eyebrow">{category}</p><h1>{title}</h1><p className="muted">{subtitle}</p></div><Link className="button button-secondary" to={backUrl}>{backLabel}</Link></section>
    {children}
  </main>
}
