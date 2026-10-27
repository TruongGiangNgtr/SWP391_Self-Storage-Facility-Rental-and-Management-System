import { useState } from 'react'
import { ImageOff } from 'lucide-react'
import type { FacilitySummary } from '../features/catalog/catalog.types'
import { facilityIllustration } from '../features/catalog/facilityIllustrations'
import '../styles/facility-images.css'

export function FacilityImage({ facility, className = '', eager = false }: {
  facility: Pick<FacilitySummary, 'facilityId' | 'name'>
  className?: string
  eager?: boolean
}) {
  const src = facilityIllustration(facility)
  return <FacilityArtwork key={src} src={src} className={className} eager={eager} />
}

function FacilityArtwork({ src, className, eager }: { src: string; className: string; eager: boolean }) {
  const [failed, setFailed] = useState(false)
  return <figure className={`facility-illustration ${className}`}>
    {failed ? <div className="facility-image-fallback" role="img" aria-label="Facility illustration unavailable"><ImageOff size={28} aria-hidden="true" /><span>Image unavailable</span></div>
      : <img src={src} alt="Illustrative FStoRent storage facility entrance." width={1536} height={1024} loading={eager ? 'eager' : 'lazy'} decoding="async" onError={() => setFailed(true)} />}
  </figure>
}
