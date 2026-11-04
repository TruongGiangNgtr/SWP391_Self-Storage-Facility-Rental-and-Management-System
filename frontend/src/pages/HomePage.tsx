import { useEffect, useRef, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ArrowRight, ImageOff } from 'lucide-react'
import { HomeFacilityMap } from '../components/HomeFacilityMap'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'
import '../styles/home-depth.css'
import '../styles/home-sage.css'

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

function HomeHeroBackground() {
  const sceneRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)

  useEffect(() => {
    const scene = sceneRef.current
    const video = videoRef.current
    if (!scene || !video) return
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)')
    let visible = false
    let disposed = false

    function showPoster() { video!.style.opacity = '0' }
    function playFromStart() {
      if (disposed || !visible || motion.matches || document.hidden || video!.error) return
      if (!video!.getAttribute('src')) {
        video!.src = '/images/home/fstorent-campus-arrival-4s.mp4'
        video!.load()
        return
      }
      if (video!.readyState < 2) return
      video!.currentTime = 0
      video!.muted = true
      void video!.play().catch(() => { if (!disposed) showPoster() })
    }
    function playing() {
      if (disposed || !visible || motion.matches || document.hidden) video!.pause()
      else video!.style.opacity = '1'
    }
    function motionChanged() {
      if (motion.matches) {
        video!.pause()
        showPoster()
        video!.removeAttribute('src')
        video!.load()
      } else playFromStart()
    }
    function visibilityChanged() {
      if (document.hidden) video!.pause()
      else if (visible && !motion.matches && !video!.ended && video!.readyState >= 2) {
        void video!.play().catch(() => { if (!disposed) showPoster() })
      }
    }

    video.addEventListener('loadeddata', playFromStart)
    video.addEventListener('playing', playing)
    video.addEventListener('error', showPoster)
    motion.addEventListener('change', motionChanged)
    document.addEventListener('visibilitychange', visibilityChanged)
    const observer = typeof IntersectionObserver === 'undefined' ? null : new IntersectionObserver(entries => {
      if (disposed) return
      const entered = entries.some(entry => entry.isIntersecting && entry.intersectionRatio >= 0.5)
      if (entered && !visible) { visible = true; playFromStart() }
      else if (!entered && visible) { visible = false; video.pause() }
    }, { threshold: [0, 0.5] })
    if (observer) observer.observe(scene)
    else { visible = true; playFromStart() }

    return () => {
      disposed = true
      observer?.disconnect()
      video.removeEventListener('loadeddata', playFromStart)
      video.removeEventListener('playing', playing)
      video.removeEventListener('error', showPoster)
      motion.removeEventListener('change', motionChanged)
      document.removeEventListener('visibilitychange', visibilityChanged)
      video.pause()
    }
  }, [])

  return <div className="sage-scene" ref={sceneRef} aria-hidden="true">
    <HomeImage className="sage-scene-photo" src="/images/home/fstorent-campus-arrival-poster.png" alt="" width={1280} height={720} priority />
    <video className="sage-scene-video" ref={videoRef} muted playsInline preload="none" poster="/images/home/fstorent-campus-arrival-poster.png" tabIndex={-1} />
  </div>
}

export function HomePage() {
  const homeRef = useRef<HTMLElement>(null)
  const { hash } = useLocation()
  const facilities = usePublicFacilities({ page: 1, pageSize: 100, sortBy: 'name', sortDirection: 'asc' })

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

  return <main ref={homeRef} className="page-container public-home home-editorial home-sage">
    <section className="home-hero sage-hero" aria-labelledby="home-heading">
      <HomeHeroBackground />
      <div className="home-hero-copy">
        <div className="sage-eyebrow"><span /> A little space. A new beginning.</div>
        <h1 id="home-heading">Space for<br /><span>what matters.</span></h1>
        <p>Room for your belongings.<br />Freedom for your next chapter. Find, reserve<br className="home-desktop-break" /> and manage your storage with FStoRent.</p>
        <div className="home-actions">
          <Link className="home-button home-button-primary" to="/facilities">Find Your Storage<ArrowRight size={17} aria-hidden="true" /></Link>
          <Link className="sage-text-link" to="/size-guide">Explore Storage Sizes <ArrowRight size={16} aria-hidden="true" /></Link>
        </div>
      </div>
      <div className="sage-hero-bottom"><span>SELF-STORAGE, WITH ROOM FOR LIFE.</span></div>
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
      <HomeImage className="home-lifestyle-photo" src="/images/home/fstorent-next-chapter.png" alt="Illustration of thoughtfully stored belongings inside a sunlit FStoRent storage space." width={1672} height={941} />
      <div className="home-lifestyle-copy">
        <span className="home-kicker">ROOM TO MOVE FORWARD</span>
        <h2 id="home-lifestyle-heading">More room for<br /><span>life’s next chapter.</span></h2>
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
      <ApiErrorAlert error={facilities.error} />
      <HomeFacilityMap facilities={facilities.data?.data ?? []} />
    </section>

    <section className="home-ready"><div className="home-container home-section-heading"><h2>Ready to find your space?</h2><Link className="home-button home-button-primary" to="/facilities">Find Storage<ArrowRight size={17} aria-hidden="true" /></Link></div></section>
  </main>
}
