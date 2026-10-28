import { useEffect, useState, type ReactNode } from 'react'

export function RentalListFilters({ children }: { children: ReactNode }) {
  const [compact, setCompact] = useState(() => window.matchMedia('(max-width: 760px)').matches)
  const [expanded, setExpanded] = useState(false)
  useEffect(() => {
    const media = window.matchMedia('(max-width: 760px)')
    const update = () => setCompact(media.matches)
    media.addEventListener('change', update)
    return () => media.removeEventListener('change', update)
  }, [])
  return <details className="rental-list-filters" open={!compact || expanded} onToggle={event => { if (compact) setExpanded(event.currentTarget.open) }}>
    <summary>Search &amp; filters<span>Applies to this page</span></summary>
    {children}
  </details>
}
