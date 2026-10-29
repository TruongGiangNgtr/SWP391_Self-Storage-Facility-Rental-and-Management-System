import { StorageReady } from '../components/StorageReady'
import { Link } from 'react-router-dom'
import { useEffect, useRef, useState } from 'react'
import { useLocation } from 'react-router-dom'
import { ArrowRight, Box, ImageOff } from 'lucide-react'
import { PublicStorageSearchForm } from '../components/PublicStorageSearchForm'
import { FacilityImage } from '../components/FacilityImage'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'
import '../styles/home-depth.css'

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
function HomeIllustration({ className, src, alt, width, height, priority = false }: {
  className: string; src: string; alt: string; width: number; height: number; priority?: boolean
}) {
  const [failed, setFailed] = useState(false)
  return <figure className={`public-home-illustration ${className}`}>
    {failed ? <div className="public-illustration-fallback" role="img" aria-label={alt}><ImageOff size={28} aria-hidden="true" /><span>Illustration unavailable</span></div>
      : <img src={src} alt={alt} width={width} height={height} loading={priority ? 'eager' : 'lazy'} fetchPriority={priority ? 'high' : undefined} decoding="async" onError={() => setFailed(true)} />}
  </figure>
}
export function HomePage() {
  const homeRef = useRef<HTMLElement>(null)
  const { hash } = useLocation()
  const facilities = usePublicFacilities({ page: 1, pageSize: 3, sortBy: 'name', sortDirection: 'asc' })
  useEffect(() => {
    if (hash === '#public-contact') document.getElementById('public-contact')?.scrollIntoView()
  }, [hash, facilities.loading])
  useEffect(() => {
    const root = homeRef.current
    if (!root) return
    const media = window.matchMedia('(min-width: 1024px) and (hover: hover) and (pointer: fine) and (prefers-reduced-motion: no-preference)')
    const selector = '.home-depth-scene, .public-mode-card, .public-featured-card, .public-contact-card, .public-lifestyle-media'
    let active: HTMLElement | null = null, bounds: DOMRect | null = null, frame = 0
    let scrolling = false, scrollTimer: ReturnType<typeof setTimeout> | undefined
    const reset = () => {
      cancelAnimationFrame(frame); frame = 0
      active?.style.removeProperty('--home-rotate-x')
      active?.style.removeProperty('--home-rotate-y')
      active = null; bounds = null
    }
    // Keep hover geometry stable and avoid layout reads on every pointer frame.
    const scroll = () => {
      if (!media.matches) return
      if (!scrolling) { scrolling = true; reset(); root.classList.add('is-scrolling') }
      clearTimeout(scrollTimer)
      scrollTimer = setTimeout(() => { scrolling = false; root.classList.remove('is-scrolling') }, 140)
    }
    const move = (event: PointerEvent) => {
      if (!media.matches || scrolling || event.pointerType !== 'mouse') { reset(); return }
      const surface = event.target instanceof Element ? event.target.closest<HTMLElement>(selector) : null
      if (!surface || !root.contains(surface)) { reset(); return }
      if (active !== surface) { reset(); active = surface; bounds = surface.getBoundingClientRect() }
      const surfaceBounds = bounds
      if (!surfaceBounds) return
      cancelAnimationFrame(frame)
      const { clientX, clientY } = event
      frame = requestAnimationFrame(() => {
        frame = 0
        const x = Math.max(-1, Math.min(1, (clientX - surfaceBounds.x) / surfaceBounds.width * 2 - 1))
        const y = Math.max(-1, Math.min(1, (clientY - surfaceBounds.y) / surfaceBounds.height * 2 - 1))
        surface.style.setProperty('--home-rotate-x', `${(-y * 1.8).toFixed(2)}deg`)
        surface.style.setProperty('--home-rotate-y', `${(x * 1.8).toFixed(2)}deg`)
      })
    }
    root.addEventListener('pointermove', move)
    root.addEventListener('pointerleave', reset)
    window.addEventListener('blur', reset)
    window.addEventListener('scroll', scroll, { passive: true })
    window.addEventListener('resize', reset)
    media.addEventListener('change', reset)
    return () => {
      reset(); root.removeEventListener('pointermove', move)
      root.removeEventListener('pointerleave', reset)
      window.removeEventListener('blur', reset); media.removeEventListener('change', reset)
      window.removeEventListener('scroll', scroll); window.removeEventListener('resize', reset)
      clearTimeout(scrollTimer); root.classList.remove('is-scrolling')
    }
  }, [])
  return <main ref={homeRef} className="page-container public-home home-depth">
    <div className="home-ambient-scene" aria-hidden="true"><span /></div>
    <section className="public-hero"><div className="gwp-container">
      <div className="public-hero-grid"><div className="public-hero-copy">
        <h1>Space for<br /><span>what matters.</span></h1>
        <p className="public-hero-description">Find the right storage space for your belongings, reserve online, and manage your rental with ease.</p>
        <div className="public-hero-actions"><a className="button home-nav-button" href="#home-storage-search">Find Your Storage<ArrowRight size={18} aria-hidden="true" /></a><Link className="home-nav-button" to="/size-guide">Explore Storage Sizes<ArrowRight size={18} aria-hidden="true" /></Link></div><div className="public-hero-benefits"><span><DesignIcon name="imgIcon1" />Monthly rental periods</span><span><DesignIcon name="imgIcon1" />Compare facility locations</span></div>
      </div><div className="home-depth-scene"><HomeIllustration className="public-hero-media" src="/images/home/fstorent-facility.webp" alt="Illustration of the FStoRent reception building and storage area entrance." width={1536} height={1024} priority /><div className="home-depth-caption"><span><Box size={24} aria-hidden="true" /></span><div><strong>Public &amp; Private Storage</strong><p>Compare types and sizes for your belongings.</p></div><Link className="home-nav-button" to="/size-guide" aria-label="Compare storage sizes">Size Guide<ArrowRight size={18} aria-hidden="true" /></Link></div></div></div>
      <div className="public-hero-search" id="home-storage-search"><PublicStorageSearchForm embedded /></div>
    </div></section>
    <section className="public-benefits-section"><div className="gwp-container public-benefits-grid">{BENEFITS.map(item => <article className="public-benefit-card" key={item.title}><span className="public-icon-tile"><DesignIcon name={item.icon} /></span><div><h2>{item.title}</h2><p>{item.text}</p></div></article>)}</div></section>
    <section className="public-home-section public-home-tint"><div className="gwp-container">
      <div className="public-section-heading"><h2>Choose the storage experience that works for you</h2><p className="muted">Compare Public and Private Storage, then check available types and prices for your rental period.</p></div>
      <div className="public-mode-grid">
        <article className="public-mode-card"><HomeIllustration className="public-mode-art" src="/images/home/public-storage.webp" alt="Illustration of three separate groups of belongings in a shared storage area." width={2172} height={724} /><div className="public-mode-copy"><h3>Public Storage</h3><p>Store belongings in a shared storage area, using the offered storage types and sizes at your chosen facility.</p><strong>Find a size that fits your needs</strong></div><Link className="public-mode-link home-nav-button" to="/facilities?mode=PUBLIC">View Public Storage<ArrowRight size={18} aria-hidden="true" /></Link></article>
        <article className="public-mode-card"><HomeIllustration className="public-mode-art" src="/images/home/private-storage.webp" alt="Illustration of belongings inside a private storage unit beside a closed, locked unit." width={2172} height={724} /><div className="public-mode-copy"><h3>Private Storage</h3><p>Choose a private storage type and rental period. Your specific storage unit is selected during handover, not at reservation.</p><strong>Choose your storage type and period</strong></div><Link className="public-mode-link home-nav-button" to="/facilities?mode=PRIVATE">View Private Storage<ArrowRight size={18} aria-hidden="true" /></Link></article>
      </div><p className="public-section-action home-size-guidance"><span>Not sure which size fits?</span><Link className="home-nav-button" to="/size-guide">Compare storage sizes<ArrowRight size={18} aria-hidden="true" /></Link></p>
    </div></section>
    <section className="public-home-section"><div className="gwp-container">
      <div className="public-section-heading"><h2>Storage made simple</h2><p className="muted">Four steps from finding storage to an active rental.</p></div>
      <ol className="public-steps-grid">{STEPS.map((step, index) => <li key={step.title}><span className="public-step-number">{String(index + 1).padStart(2, '0')}</span><h3>{step.title}</h3><p>{step.text}</p></li>)}</ol><div className="public-section-action"><Link className="home-nav-button" to="/how-it-works">Learn How It Works<ArrowRight size={18} aria-hidden="true" /></Link></div>
    </div></section>
    <section className="public-home-section public-home-tint"><div className="gwp-container public-lifestyle-grid">
      <HomeIllustration className="public-lifestyle-media" src="/images/home/fstorent-customer-assistance.webp" alt="Illustration of a FStoRent employee helping a customer with boxes at the reception forecourt." width={1421} height={1107} /><div><h2>Make room for life's changes.</h2><p className="public-section-description">Storage can help you keep belongings organized when your home or work needs more space.</p><div className="public-purpose-grid">{PURPOSES.map(item => <div className="public-purpose-card" key={item.title}><span className="public-icon-tile"><DesignIcon name={item.icon} /></span><strong>{item.title}</strong></div>)}</div></div>
    </div></section>
    <section className="public-home-section"><div className="gwp-container">
      <div className="public-section-heading"><h2>Find FStoRent near you</h2><p className="muted">Explore facilities from the current public catalog. Choose rental months to check availability.</p></div>
      <ApiErrorAlert error={facilities.error} />
      {facilities.loading ? <LoadingState label="Loading facilities…" /> : facilities.data && (facilities.data.data.length === 0 ? <EmptyState message="No active facilities have been returned. Please check again later." /> : <div className="public-featured-grid">{facilities.data.data.map(facility => <article className="public-featured-card" key={facility.facilityId}><FacilityImage facility={facility} className="public-featured-media" /><div className="public-featured-copy"><h3>{facility.name}</h3><p className="flow-facility-address"><DesignIcon name="imgIcon12" />{facility.address}</p><Link className="button button-secondary home-nav-button" to={`/facilities/${encodeURIComponent(facility.facilityId)}`}>View Facility<ArrowRight size={18} aria-hidden="true" /></Link></div></article>)}</div>)}
      {facilities.error && <div className="public-section-action"><button className="button button-secondary" type="button" onClick={facilities.refresh}>Retry Facilities</button></div>}
    </div></section>
    <section className="public-home-section public-home-tint" id="public-contact"><div className="gwp-container"><div className="public-section-heading"><h2>Need help choosing storage?</h2><p className="muted">Review facility details and the available guidance before making your choice.</p></div><div className="public-contact-grid">
      <article className="panel public-contact-card"><span className="public-icon-tile"><DesignIcon name="imgIcon14" /></span><h3>Facility Contacts</h3><p className="muted">Contact information is shown on each facility's detail page when provided.</p><Link className="button button-secondary home-nav-button" to="/facilities">View Facilities<ArrowRight size={18} aria-hidden="true" /></Link></article>
      <article className="panel public-contact-card"><span className="public-icon-tile"><DesignIcon name="imgIcon15" /></span><h3>Help & Policies</h3><p className="muted">Read the published system policies before reserving your storage.</p><Link className="button button-secondary home-nav-button" to="/help">Open Help Center<ArrowRight size={18} aria-hidden="true" /></Link></article>
    </div></div></section>
    <StorageReady />
  </main>
}
