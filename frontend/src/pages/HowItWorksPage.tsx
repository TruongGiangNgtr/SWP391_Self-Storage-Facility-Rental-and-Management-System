import { ArrowRight, CalendarDays, Check, ClipboardList, CreditCard, FileText, Handshake, Info, MapPin, Package, RotateCcw } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import '../styles/how-it-works.css'

// GWP-03: explanatory content from SRS sections 4.1–4.3, 4.6 and 11.9.
// No policy thresholds, prices, capacity promises or business commands here.
const RENTAL_STEPS = [
  {
    title: 'Find a storage type', icon: MapPin,
    text: 'Choose a location, Private or Public Storage, a size, and your check-in and check-out months. Search results show storage types with available capacity throughout your selected period.',
    note: 'Browsing and signing in do not hold capacity. Availability is checked again when you create a reservation.',
  },
  {
    title: 'Create your reservation', icon: FileText,
    text: 'Sign in to your Customer account, review your selection and explicitly create your reservation. The system holds capacity for your storage type and rental period and creates a separate deposit invoice.',
    note: 'Your reservation records the locked monthly rent and deposit amount. A specific physical unit is not assigned yet.',
  },
  {
    title: 'Pay the deposit and schedule handover', icon: CreditCard,
    text: 'Pay your deposit invoice through payOS. Once the system confirms payment, confirm your reservation and choose an allowed handover date. This creates your scheduled handover visit.',
    note: 'The deposit is separate from first-month rent. The payment return page alone is not proof of payment; check the recorded invoice status.',
  },
  {
    title: 'Complete handover and start your contract', icon: Handshake,
    text: 'Attend your handover visit for Staff check-in. The Manager selects an available physical unit matching your facility and storage type. Pay the full first-month rent offline at the facility; Staff then completes handover and your contract becomes active.',
    note: 'First-month rent uses your locked reservation price without a Contract Discount. Contract creation records its settlement; there is no first-month online payment or invoice.',
  },
]

const PREPARATION = [
  { title: 'Preferred location', text: 'Choose a facility that suits your storage needs.', icon: MapPin },
  { title: 'Storage type & size', text: 'Compare the offered Private and Public storage types.', icon: Package },
  { title: 'Rental months', text: 'Choose a future check-in month and a check-out month on or after it.', icon: CalendarDays },
  { title: 'Account & identification', text: 'Have your Customer account details and bring identification to handover.', icon: ClipboardList },
]

const DURING_RENTAL = [
  { title: 'Arrange access visits', icon: CalendarDays, text: 'With an active contract, schedule an access visit and let Staff handle check-in and check-out. A scheduled or checked-in return visit blocks new access visits and renewal.' },
  { title: 'Manage rent and renewal', icon: CreditCard, text: 'Rental invoices begin with the second rental month. Review recorded invoices in your account. Renewal depends on an active contract, no pending return and available capacity for every extension month; the current price is captured for the extension.' },
  { title: 'Schedule a return', icon: RotateCcw, text: 'Create a return visit from your contract. Booking the visit does not end the contract. After physical return, inspection, any required damage decisions and deposit settlement, the contract is completed or terminated according to the actual return date and its terms.' },
]

export function HowItWorksPage() {
  const { user } = useAuth()
  const customer = user?.role === 'CUSTOMER'

  return <main className="page-container public-how-it-works">
    <section className="how-hero"><div className="gwp-container">
      <nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">How It Works</span></nav>
      <div className="how-hero-grid"><div>
        <p className="eyebrow">How FStoRent Works</p><h1>Storage made simple.</h1>
        <p className="how-intro">Find a space, reserve your storage type and pay the deposit. Arrange handover, pay your first month at the facility and start your contract.</p>
        <Link className="button" to="/facilities">Find Storage<ArrowRight size={16} aria-hidden="true" /></Link>
      </div><div className="how-hero-media" aria-hidden="true"><Package size={72} strokeWidth={1.25} /></div></div>
    </div></section>

    <section className="how-section how-tint" aria-labelledby="how-journey-heading"><div className="gwp-container">
      <div className="how-section-heading"><p className="eyebrow">Your Storage Journey</p><h2 id="how-journey-heading">From finding storage to moving in.</h2></div>
      <ol className="how-steps">{RENTAL_STEPS.map((step, index) => <li className="how-step" key={step.title}>
        <span className="how-step-number" aria-hidden="true">{String(index + 1).padStart(2, '0')}</span>
        <div className="how-step-copy"><h3>{step.title}</h3><p>{step.text}</p><p className="how-step-note">{step.note}</p></div>
        <span className="how-icon-tile"><step.icon size={20} aria-hidden="true" /></span>
      </li>)}</ol>
    </div></section>

    <section className="how-section" aria-labelledby="how-account-heading"><div className="gwp-container how-narrow">
      <div className="how-section-heading"><h2 id="how-account-heading">What happens after you select a storage type?</h2></div>
      <div className="how-account-grid">
        <article className="how-account-card"><span className="how-label">First-Time Customer</span><h3>Create your Customer account</h3><p>Register with your contact details and a password. Sign in using your phone number and password when you are ready to reserve.</p>
          <ul className="how-checklist"><li><Check size={16} aria-hidden="true" />Compare storage before registering</li><li><Check size={16} aria-hidden="true" />Review required details at the relevant step</li><li><Check size={16} aria-hidden="true" />Explicitly create a reservation after signing in</li></ul>
          <Link className="button button-secondary" to={customer ? '/facilities' : '/auth/customer/register'}>{customer ? 'Find Storage' : 'Create Customer Account'}</Link>
        </article>
        <article className="how-account-card"><span className="how-label how-label-returning">Returning Customer</span><h3>Continue with your reservation</h3><p>If you sign in while reserving, your valid selection is restored and checked against current prices and availability. Signing in alone never creates a reservation or payment.</p>
          <ul className="how-checklist"><li><Check size={16} aria-hidden="true" />Review your current reservation details</li><li><Check size={16} aria-hidden="true" />Pay the deposit and check its recorded status</li><li><Check size={16} aria-hidden="true" />Confirm and schedule handover after payment</li></ul>
          <Link className="button" to={customer ? '/customer/reservations' : '/auth/customer/login'}>{customer ? 'My Reservations' : 'Sign In'}</Link>
        </article>
      </div>
      <p className="how-account-note">Signing in normally opens your Customer Overview. Required account information is handled at the step that needs it, not through an automatic verification or payment flow after every sign-in.</p>
    </div></section>

    <section className="how-section how-tint" aria-labelledby="how-preparation-heading"><div className="gwp-container">
      <div className="how-section-heading"><h2 id="how-preparation-heading">What you will need</h2></div>
      <ul className="how-preparation-grid">{PREPARATION.map(item => <li key={item.title}><span className="how-icon-tile"><item.icon size={20} aria-hidden="true" /></span><h3>{item.title}</h3><p>{item.text}</p></li>)}</ul>
      <aside className="how-important" aria-labelledby="how-important-heading"><span className="how-icon-tile"><Info size={22} aria-hidden="true" /></span><div><h3 id="how-important-heading">Important to know</h3><p>You reserve capacity for a storage type first. The Manager selects your physical unit during handover, not while you browse or reserve.</p></div><Link to="/size-guide">View Size Guide<ArrowRight size={16} aria-hidden="true" /></Link></aside>
    </div></section>

    <section className="how-section" aria-labelledby="how-rental-heading"><div className="gwp-container">
      <div className="how-section-heading"><p className="eyebrow">After Handover</p><h2 id="how-rental-heading">Manage your rental, visits and return.</h2></div>
      <div className="how-rental-grid">{DURING_RENTAL.map(item => <article key={item.title}><span className="how-icon-tile"><item.icon size={20} aria-hidden="true" /></span><h3>{item.title}</h3><p>{item.text}</p></article>)}</div>
      <p className="how-rental-note">Only eligible scheduled visits can be rescheduled or cancelled. Allowed dates and financial terms follow the policy captured for your reservation or contract. Review the terms shown in your account before making changes.</p>
    </div></section>

    <section className="how-ready" aria-labelledby="how-ready-heading"><div className="gwp-container"><h2 id="how-ready-heading">Ready to find storage?</h2><p>Start by choosing a location, storage type, size and rental months.</p><Link className="button" to="/facilities">Find Storage<ArrowRight size={16} aria-hidden="true" /></Link></div></section>
  </main>
}
