import { useEffect, useRef, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ArrowRight, ImageOff } from 'lucide-react'
import { PublicStorageSearchForm } from '../components/PublicStorageSearchForm'
import { FacilityImage } from '../components/FacilityImage'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'
import '../styles/home-depth.css'

const STEPS = [
  { title: 'Find your storage', text: 'Choose a facility, storage type, size and rental months.' },
  { title: 'Reserve and pay your deposit', text: 'Sign in, review your selection and pay the reservation deposit.' },
  { title: 'Arrange your handover', text: 'Confirm your reservation after payment, then choose a handover date. Your unit is selected at the facility.' },
]

function HomeImage({ className, src, alt, width, height, priority = false }: {
  className: string; src: string; alt: string; width: number; height: number; priority?: boolean
}) {
  const [failed, setFailed] = useState(false)
  return <figure className={`home-photo ${className}`}>
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
    if (!root || !('IntersectionObserver' in window)) return
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    if (motion.matches) return
    // Animate once on entry; no pointer tracking, scroll loop or hidden content.
    const observer = new IntersectionObserver(entries => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-visible')
          observer.unobserve(entry.target)
        }
      }
    }, { threshold: 0.08 })
    root.querySelectorAll('[data-home-reveal]').forEach(element => observer.observe(element))
    return () => observer.disconnect()
  }, [])

  return <main ref={homeRef} className="page-container public-home home-editorial">
    <section className="home-hero" aria-labelledby="home-heading">
      <div className="home-hero-copy">
        <h1 id="home-heading">Space for<br /><span>what matters.</span></h1>
        <p>Find the space you need. Reserve online<br className="home-desktop-break" /> and manage your rental with FStoRent.</p>
        <div className="home-actions">
          <a className="home-button home-button-primary" href="#home-storage-search">Find Your Storage<ArrowRight size={17} aria-hidden="true" /></a>
          <Link className="home-button home-button-on-dark" to="/size-guide">Explore Storage Sizes</Link>
        </div>
      </div>
      <HomeImage className="home-hero-photo" src="/images/home/fstorent-facility.webp" alt="Illustration of the FStoRent reception building and storage area entrance." width={1536} height={1024} priority />
    </section>

    <section className="home-search" id="home-storage-search" aria-label="Find a storage space">
      <div className="home-container home-search-container"><PublicStorageSearchForm embedded /></div>
    </section>

    <section className="home-storage home-container" aria-labelledby="home-storage-heading" data-home-reveal>
      <div className="home-storage-heading-row"><h2 id="home-storage-heading">Storage that fits your life.</h2><div className="home-size-guidance"><span>Not sure which size fits?</span><Link className="home-button" to="/size-guide">Compare Storage Sizes<ArrowRight size={16} aria-hidden="true" /></Link></div></div>
      <div className="home-storage-grid">
        <article className="home-storage-option">
          <HomeImage className="home-storage-photo" src="/images/home/public-storage.webp" alt="Illustration of separate groups of belongings in a shared storage area." width={2172} height={724} />
          <h3>Public Storage</h3>
          <p>Shared storage options, with offered types and sizes at your chosen facility.</p>
          <Link className="home-button" to="/facilities?mode=PUBLIC">View Public Storage<ArrowRight size={16} aria-hidden="true" /></Link>
        </article>
        <article className="home-storage-option">
          <HomeImage className="home-storage-photo" src="/images/home/private-storage.webp" alt="Illustration of belongings inside a private storage unit beside a closed unit." width={2172} height={724} />
          <h3>Private Storage</h3>
          <p>A private space for your belongings. Your specific unit is chosen at handover.</p>
          <Link className="home-button" to="/facilities?mode=PRIVATE">View Private Storage<ArrowRight size={16} aria-hidden="true" /></Link>
        </article>
      </div>
    </section>

    <section className="home-lifestyle" aria-labelledby="home-lifestyle-heading" data-home-reveal>
      <HomeImage className="home-lifestyle-photo" src="/images/home/fstorent-customer-assistance.webp" alt="Illustration of a FStoRent employee helping a customer with boxes at reception." width={1421} height={1107} />
      <div className="home-lifestyle-copy">
        <h2 id="home-lifestyle-heading">More room for life’s next chapter.</h2>
        <p>Moving, making space at home or storing business supplies? Start by comparing storage options.</p>
        <div className="home-purpose-grid">
          <div><h3>For your home</h3><p>Create space for what matters, whether you’re moving, renovating or simply need more room.</p></div>
          <div><h3>For your business</h3><p>Store inventory, equipment or documents so you can focus on what’s next.</p></div>
        </div>
        <Link className="home-button" to="/facilities">Explore Storage<ArrowRight size={16} aria-hidden="true" /></Link>
      </div>
    </section>

    <section className="home-process" aria-labelledby="home-process-heading">
      <div className="home-container" data-home-reveal>
        <div className="home-section-heading"><h2 id="home-process-heading">A simple way to make room.</h2><Link className="home-button" to="/how-it-works">How It Works<ArrowRight size={16} aria-hidden="true" /></Link></div>
        <ol className="home-process-grid">{STEPS.map(step => <li key={step.title}><h3>{step.title}</h3><p>{step.text}</p></li>)}</ol>
        <p className="home-process-note">At handover, pay the first month offline. Staff completes handover to activate your contract.</p>
      </div>
    </section>

    <section className="home-facilities home-container" id="public-contact" aria-labelledby="home-facilities-heading" data-home-reveal>
      <h2 id="home-facilities-heading">Find FStoRent near you</h2>
      <ApiErrorAlert error={facilities.error} />
      {facilities.loading ? <LoadingState label="Loading facilities…" /> : facilities.data && (facilities.data.data.length === 0
        ? <EmptyState message="No active facilities have been returned. Please check again later." />
        : <div className="home-facility-list">{facilities.data.data.map(facility => <article className="home-facility" key={facility.facilityId}>
          <FacilityImage facility={facility} className="home-facility-photo" />
          <div className="home-facility-copy"><h3>{facility.name}</h3><p>{facility.address}</p></div>
          <Link className="home-button" to={`/facilities/${encodeURIComponent(facility.facilityId)}`}>View Facility<ArrowRight size={16} aria-hidden="true" /></Link>
        </article>)}</div>)}
    </section>

    <section className="home-ready"><div className="home-container home-section-heading"><h2>Ready to find your space?</h2><Link className="home-button home-button-primary" to="/facilities">Find Storage<ArrowRight size={17} aria-hidden="true" /></Link></div></section>
  </main>
}
