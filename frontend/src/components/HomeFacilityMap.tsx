import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, MapPin } from 'lucide-react'
import { FacilityImage } from './FacilityImage'
import type { FacilitySummary } from '../features/catalog/catalog.types'
import { normalizeMapLocation, VIETNAM_MAP_IMAGE, VIETNAM_MAP_LOCATIONS, VIETNAM_MAP_VIEWBOX } from '../features/catalog/vietnamMap'

export function HomeFacilityMap({ facilities }: { facilities: FacilitySummary[] }) {
  const [selectedId, setSelectedId] = useState<string | null>(null)
  // Match only actual public catalogue entries; never fabricate a facility for a pin.
  const points = VIETNAM_MAP_LOCATIONS.flatMap(location => {
    const facility = facilities.find(item => item.status === 'ACTIVE' &&
      ` ${normalizeMapLocation(`${item.name} ${item.address}`)} `.includes(` ${location.key} `))
    return facility ? [{ ...location, facility }] : []
  })
  const selected = points.find(point => point.facility.facilityId === selectedId) ?? points[0]

  return <div className="home-network-grid">
    <div className="home-map-panel">
      <div className="home-vietnam-map">
        <svg viewBox={`${VIETNAM_MAP_VIEWBOX.x} ${VIETNAM_MAP_VIEWBOX.y} ${VIETNAM_MAP_VIEWBOX.width} ${VIETNAM_MAP_VIEWBOX.height}`} role="img" aria-label="Map of Vietnam from the supplied reference, including Hoàng Sa and Trường Sa archipelagos">
          <image href={VIETNAM_MAP_IMAGE.src} width={VIETNAM_MAP_IMAGE.width} height={VIETNAM_MAP_IMAGE.height} />
        </svg>
        {points.map(point => <button key={point.facility.facilityId} type="button"
          className={`home-map-pin ${point === selected ? 'is-selected' : ''}`}
          style={{ left: `${(point.x - VIETNAM_MAP_VIEWBOX.x) / VIETNAM_MAP_VIEWBOX.width * 100}%`, top: `${(point.y - VIETNAM_MAP_VIEWBOX.y) / VIETNAM_MAP_VIEWBOX.height * 100}%` }}
          aria-label={`Explore ${point.facility.name}, ${point.label}`}
          aria-pressed={point === selected} onClick={() => setSelectedId(point.facility.facilityId)}>
          <span className="home-map-pin-core" />
        </button>)}
      </div>
      <p className="home-map-note"><MapPin size={14} aria-hidden="true" /> Markers show approximate city locations.</p>
    </div>
    <div className="home-network-copy">
      <span className="home-kicker">OUR LOCATIONS</span>
      <h2 id="home-facilities-heading">Your next chapter.<br /><span>A space in Vietnam.</span></h2>
      <p>Discover FStoRent facilities and explore the storage options at each location.</p>
      {points.length > 0 ? <>
        <div className="home-region-tabs" role="group" aria-label="Choose a facility region">
          {points.map(point => <button key={point.facility.facilityId} type="button" aria-pressed={point === selected}
            onClick={() => setSelectedId(point.facility.facilityId)}>{point.label}</button>)}
        </div>
        {selected && <article className="home-map-facility" key={selected.facility.facilityId}>
          <FacilityImage facility={selected.facility} className="home-map-facility-photo" />
          <div className="home-map-facility-info"><span className="home-kicker">{selected.label}</span><h3>{selected.facility.name}</h3><p>{selected.facility.address}</p>
            <Link className="home-button" to={`/facilities/${encodeURIComponent(selected.facility.facilityId)}`}>View Facility <ArrowRight size={16} aria-hidden="true" /></Link>
          </div>
        </article>}
      </> : <div className="home-network-unmapped">{facilities.slice(0, 3).map(facility => <Link key={facility.facilityId} to={`/facilities/${encodeURIComponent(facility.facilityId)}`}>{facility.name}<ArrowRight size={16} aria-hidden="true" /></Link>)}</div>}
      <Link className="sage-text-link" to="/facilities">Explore all facilities <ArrowRight size={16} aria-hidden="true" /></Link>
    </div>
  </div>
}
