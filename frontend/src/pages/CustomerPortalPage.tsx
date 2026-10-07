import { Link } from 'react-router-dom'

const PORTAL_ACTIONS = [
  {
    title: 'Find and Reserve Storage',
    description: 'Check availability for your rental period and start a new reservation.',
    to: '/customer/storage-search',
  },
  {
    title: 'My Reservations',
    description: 'Track deposits and confirm or cancel your reservations.',
    to: '/customer/reservations',
  },
  {
    title: 'Invoices',
    description: 'Review your invoices and start a MoMo payment.',
    to: '/customer/invoices',
  },
  {
    title: 'My Contracts',
    description: 'Review your contracts and schedule access visits for an active rental.',
    to: '/customer/contracts',
  },
  {
    title: 'Visits',
    description: 'Review, reschedule or cancel eligible visits.',
    to: '/customer/visits',
  },
]

export function CustomerPortalPage() {
  return (
    <main className="page-container flow-page">
      <section className="page-heading">
        <p className="eyebrow">Customer Portal</p>
        <h1>Manage Your Storage Journey</h1>
        <p className="muted">
          Find available storage, create a reservation, pay your deposit and schedule handover.
        </p>
      </section>

      <section className="card-grid" aria-label="Customer features">
        {PORTAL_ACTIONS.map((action) => (
          <article className="summary-card" key={action.to}>
            <h2>{action.title}</h2>
            <p className="muted">{action.description}</p>
            <Link className="button button-secondary" to={action.to}>
              Open Feature
            </Link>
          </article>
        ))}
      </section>
    </main>
  )
}
