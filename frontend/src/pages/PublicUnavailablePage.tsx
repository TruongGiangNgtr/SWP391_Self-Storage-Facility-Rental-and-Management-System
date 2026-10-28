import { Link } from 'react-router-dom'
import { CircleHelp } from 'lucide-react'

export function PublicUnavailablePage({ title }: { title: string }) {
  return <main className="page-container flow-page support-unavailable-page"><section className="page-heading"><h1>{title}</h1></section><section className="panel stack"><CircleHelp size={32} aria-hidden="true" /><h2>Not Available Yet</h2><p role="status">This feature is not available yet. Please try again later.</p><p className="muted">You can still browse storage and read the available guidance.</p></section><div className="action-row"><Link className="button" to="/facilities">Find Storage</Link><Link className="button button-secondary" to="/">Back to Home</Link></div></main>
}
