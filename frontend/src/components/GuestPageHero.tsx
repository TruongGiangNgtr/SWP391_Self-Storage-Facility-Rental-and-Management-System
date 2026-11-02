import { useState, type ReactNode } from 'react'

export function GuestPageHero({ eyebrow, title, description, image, breadcrumb, children, compact = false }: {
  eyebrow: string
  title: ReactNode
  description: string
  image: string
  breadcrumb: ReactNode
  children?: ReactNode
  compact?: boolean
}) {
  const [failed, setFailed] = useState(false)
  return <section className={`guest-hero${compact ? ' guest-hero-compact' : ''}`}>
    {!failed && <img className="guest-hero-image" src={image} alt="" width={1672} height={941}
      fetchPriority="high" decoding="async" onError={() => setFailed(true)} />}
    <div className="gwp-container guest-hero-inner">
      {breadcrumb}
      <div className="guest-hero-copy">
        <p className="guest-kicker"><span />{eyebrow}</p>
        <h1>{title}</h1>
        <p className="guest-hero-description">{description}</p>
        {children && <div className="guest-hero-actions">{children}</div>}
      </div>
    </div>
  </section>
}
