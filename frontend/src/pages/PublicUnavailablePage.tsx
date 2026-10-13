import { Link } from 'react-router-dom'
import { ApiErrorAlert } from '../components/ApiErrorAlert'

export function PublicUnavailablePage({ title }: { title: string }) {
  return <main className="page-container flow-page"><section className="page-heading"><p className="eyebrow">FStoRent</p><h1>{title}</h1></section><section className="panel stack"><h2>Not Available Yet</h2><ApiErrorAlert error={{ message: 'This feature is not available yet. Please try again later.', details: [] }} /><p className="muted">This section will be completed in a separate Guest feature.</p></section><div className="action-row"><Link className="button" to="/facilities">Find Storage</Link><Link className="button button-secondary" to="/">Back to Home</Link></div></main>
}
