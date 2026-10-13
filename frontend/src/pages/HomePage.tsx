import { Link } from 'react-router-dom'
import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { PublicStorageSearchForm } from '../components/PublicStorageSearchForm'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'

const BENEFITS = [
  { icon: 'imgIcon2', title: 'Choose with confidence', text: 'Compare storage sizes and find a space that fits your needs.' },
  { icon: 'imgIcon3', title: 'Reserve online', text: 'Create a reservation and keep track of every next step.' },
  { icon: 'imgIcon4', title: 'Stay in control', text: 'Manage your reservations, contracts and invoices in one place.' },
]
const STEPS = [
  { title: 'Find your space', text: 'Choose a location, storage type, size and rental months.' },
  { title: 'Create a reservation', text: 'Sign in, create your reservation and pay the deposit.' },
  { title: 'Arrange handover', text: 'After the deposit is paid, confirm your reservation and choose a handover date.' },
  { title: 'Start your contract', text: 'The Manager selects a unit. Pay your first month offline; Staff then completes handover and your contract becomes active.' },
]
const PURPOSES = [
  { icon: 'imgIcon8', title: 'Moving Home' }, { icon: 'imgIcon9', title: 'Decluttering' },
  { icon: 'imgIcon10', title: 'Seasonal Storage' }, { icon: 'imgIcon11', title: 'Business Supplies' },
]
function DesignIcon({ name }: { name: string }) {
  return <img className="public-design-icon" src={`/figma/gwp-01/${name}.svg`} alt="" aria-hidden="true" />
}
export function HomePage() {
  const { hash } = useLocation()
  const facilities = usePublicFacilities({ page: 1, pageSize: 3, sortBy: 'name', sortDirection: 'asc' })
  useEffect(() => {
    if (hash === '#public-contact') document.getElementById('public-contact')?.scrollIntoView()
  }, [hash, facilities.loading])
  return <main className="page-container public-home">
    <section className="public-hero"><div className="gwp-container">
      <div className="public-hero-grid"><div className="public-hero-copy">
        <p className="public-hero-pill"><span aria-hidden="true" />Self Storage Made Simple</p>
        <h1>Space for what matters.</h1>
        <p className="public-hero-description">Find the right storage space for your belongings, reserve online, and manage your rental with ease.</p>
        <div className="public-hero-benefits"><span><DesignIcon name="imgIcon1" />Monthly rental periods</span><span><DesignIcon name="imgIcon1" />Compare facility locations</span></div>
      </div><div className="public-hero-media" aria-hidden="true" /></div>
      <div className="public-hero-search"><PublicStorageSearchForm embedded suggestions={facilities.data?.data} /></div>
    </div></section>
    <section className="public-benefits-section"><div className="gwp-container public-benefits-grid">{BENEFITS.map(item => <article className="public-benefit-card" key={item.title}><span className="public-icon-tile"><DesignIcon name={item.icon} /></span><div><h2>{item.title}</h2><p>{item.text}</p></div></article>)}</div></section>
    <section className="public-home-section public-home-tint"><div className="gwp-container">
      <div className="public-section-heading"><p className="eyebrow">Choose Your Storage</p><h2>Choose the storage experience that works for you</h2><p className="muted">Compare Public and Private Storage, then check available types and prices for your rental period.</p></div>
      <div className="public-mode-grid">
        <article className="public-mode-card"><div className="public-mode-art"><DesignIcon name="imgIcon5" /></div><div className="public-mode-copy"><h3>Public Storage</h3><p className="eyebrow">Shared Storage Space</p><p>Store belongings in a shared storage area, using the offered storage types and sizes at your chosen facility.</p><strong>Find a size that fits your needs</strong></div><Link className="public-mode-link" to="/facilities?mode=PUBLIC">View Public Storage<DesignIcon name="imgIcon6" /></Link></article>
        <article className="public-mode-card"><div className="public-mode-art"><DesignIcon name="imgIcon7" /></div><div className="public-mode-copy"><h3>Private Storage</h3><p className="eyebrow">Your Rental Space</p><p>Choose a private storage type and rental period. Your specific storage unit is selected during handover, not at reservation.</p><strong>Choose your storage type and period</strong></div><Link className="public-mode-link" to="/facilities?mode=PRIVATE">View Private Storage<DesignIcon name="imgIcon6" /></Link></article>
      </div><div className="public-section-action"><Link className="button button-secondary" to="/size-guide">Explore Size Guide</Link></div>
    </div></section>
    <section className="public-home-section"><div className="gwp-container">
      <div className="public-section-heading"><h2>Storage made simple</h2><p className="muted">Four steps from finding storage to an active rental.</p></div>
      <ol className="public-steps-grid">{STEPS.map((step, index) => <li key={step.title}><span className="public-step-number">{String(index + 1).padStart(2, '0')}</span><h3>{step.title}</h3><p>{step.text}</p></li>)}</ol><div className="public-section-action"><Link className="public-text-link" to="/how-it-works">Learn How It Works<DesignIcon name="imgIcon6" /></Link></div>
    </div></section>
    <section className="public-home-section public-home-tint"><div className="gwp-container public-lifestyle-grid">
      <div className="public-lifestyle-media" aria-hidden="true" /><div><p className="eyebrow">When You Need More Space</p><h2>Make room for life's changes.</h2><p className="public-section-description">Storage can help you keep belongings organized when your home or work needs more space.</p><div className="public-purpose-grid">{PURPOSES.map(item => <div className="public-purpose-card" key={item.title}><span className="public-icon-tile"><DesignIcon name={item.icon} /></span><strong>{item.title}</strong></div>)}</div></div>
    </div></section>
    <section className="public-home-section"><div className="gwp-container">
      <div className="public-section-heading"><h2>Find FStoRent near you</h2><p className="muted">Explore facilities from the current public catalog. Choose rental months to check availability.</p></div>
      <ApiErrorAlert error={facilities.error} />
      {facilities.loading ? <LoadingState label="Loading facilities…" /> : facilities.data && (facilities.data.data.length === 0 ? <EmptyState message="No active facilities have been returned. Please check again later." /> : <div className="public-featured-grid">{facilities.data.data.map(facility => <article className="public-featured-card" key={facility.facilityId}><div className="public-featured-media" aria-hidden="true" /><div className="public-featured-copy"><h3>{facility.name}</h3><p className="flow-facility-address"><DesignIcon name="imgIcon12" />{facility.address}</p><Link className="button button-secondary" to={`/facilities/${encodeURIComponent(facility.facilityId)}`}>View Facility</Link></div></article>)}</div>)}
      {facilities.error && <div className="public-section-action"><button className="button button-secondary" type="button" onClick={facilities.refresh}>Retry Facilities</button></div>}
    </div></section>
    <section className="public-ready-section"><div className="gwp-container"><h2>Ready to find your storage?</h2><p>Compare locations and storage types for your rental period.</p><div className="public-ready-actions"><Link className="button button-secondary" to="/facilities">Find Your Storage<DesignIcon name="imgIcon13" /></Link><Link to="/how-it-works">Learn How It Works →</Link></div></div></section>
    <section className="public-home-section public-home-tint" id="public-contact"><div className="gwp-container"><div className="public-section-heading"><p className="eyebrow">We're Here for You</p><h2>Need help choosing storage?</h2><p className="muted">Review facility details and the available guidance before making your choice.</p></div><div className="public-contact-grid">
      <article className="panel public-contact-card"><span className="public-icon-tile"><DesignIcon name="imgIcon14" /></span><h3>Facility Contacts</h3><p className="muted">Contact information is shown on each facility's detail page when provided.</p><Link className="button button-secondary" to="/facilities">View Facilities</Link></article>
      <article className="panel public-contact-card"><span className="public-icon-tile"><DesignIcon name="imgIcon15" /></span><h3>Help & Policies</h3><p className="muted">The Help Center is planned for a separate Guest feature. No policy or contact details are assumed here.</p><Link className="button button-secondary" to="/help">Open Help Center</Link></article>
    </div></div></section>
  </main>
}
