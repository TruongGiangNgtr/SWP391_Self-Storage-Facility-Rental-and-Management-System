import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, MapPin } from 'lucide-react'
import { FacilityImage } from './FacilityImage'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'
import type { FacilitySummary } from '../features/catalog/catalog.types'
import { normalizeMapLocation, VIETNAM_MAP_IMAGE, VIETNAM_MAP_LOCATIONS, VIETNAM_MAP_VIEWBOX } from '../features/catalog/vietnamMap'

export function HomeFacilityMap({ facilities }: { facilities: FacilitySummary[] }) {
  const [selected, setSelected] = useState<(typeof VIETNAM_MAP_LOCATIONS)[number]>(VIETNAM_MAP_LOCATIONS[3])
  const matchesRegion = (item: FacilitySummary) => item.status === 'ACTIVE' &&
    ` ${normalizeMapLocation(`${item.name} ${item.address}`)} `.includes(` ${selected.key} `)
  const localMatch = facilities.find(matchesRegion)
  const region = usePublicFacilities(
    { location: selected.key, page: 1, pageSize: 20, sortBy: 'name', sortDirection: 'asc' },
    !localMatch,
  )
  const facility = localMatch ?? region.data?.data.find(matchesRegion)
  return <div className="home-network-grid">
    <div className="home-map-panel">
      <div className="home-vietnam-map">
        <svg viewBox={`${VIETNAM_MAP_VIEWBOX.x} ${VIETNAM_MAP_VIEWBOX.y} ${VIETNAM_MAP_VIEWBOX.width} ${VIETNAM_MAP_VIEWBOX.height}`} role="img" aria-label="Map of Vietnam from the supplied reference, including Hoàng Sa and Trường Sa archipelagos">
          <image href={VIETNAM_MAP_IMAGE.src} width={VIETNAM_MAP_IMAGE.width} height={VIETNAM_MAP_IMAGE.height} />
        </svg>
        {VIETNAM_MAP_LOCATIONS.map(point => <button key={point.key} type="button"
          className={`home-map-pin ${point === selected ? 'is-selected' : ''}`}
          style={{ left: `${(point.x - VIETNAM_MAP_VIEWBOX.x) / VIETNAM_MAP_VIEWBOX.width * 100}%`, top: `${(point.y - VIETNAM_MAP_VIEWBOX.y) / VIETNAM_MAP_VIEWBOX.height * 100}%` }}
          aria-label={`Explore ${point.label}`}
          aria-pressed={point === selected} onClick={() => setSelected(point)}>
          <span className="home-map-pin-core" />
        </button>)}
      </div>
      <p className="home-map-note"><MapPin size={14} aria-hidden="true" /> Markers show approximate city locations.</p>
    </div>
    <div className="home-network-copy">
      <span className="home-kicker">OUR LOCATIONS</span>
      <h2 id="home-facilities-heading">Your next chapter.<br /><span>A space in Vietnam.</span></h2>
      <p>Discover FStoRent facilities and explore the storage options at each location.</p>

      <div className="home-region-tabs" role="group" aria-label="Choose a facility region">
        {VIETNAM_MAP_LOCATIONS.map(point => <button key={point.key} type="button" aria-pressed={point === selected}
          onClick={() => setSelected(point)}>{point.label}</button>)}
      </div>
      {facility ? <article className="home-map-facility" key={facility.facilityId}>
        <FacilityImage facility={facility} className="home-map-facility-photo" />
        <div className="home-map-facility-info">
          <span className="home-kicker">{selected.label}</span>
          <h3>{facility.name}</h3>
          <p>{facility.address}</p>
          <Link className="home-button" to={`/facilities/${encodeURIComponent(facility.facilityId)}`}>View Facility <ArrowRight size={16} aria-hidden="true" /></Link>
        </div>
      </article> : <article className="home-map-facility">
        <div className="home-map-facility-info">
          <h3>{selected.label}</h3>
          <p role="status">{region.loading ? 'Looking for facilities…' : region.error ? 'Could not load facilities for this region.' : 'No matching facility in the public catalogue.'}</p>
        </div>
      </article>}
      <Link className="sage-text-link" to="/facilities">Explore all facilities <ArrowRight size={16} aria-hidden="true" /></Link>
    </div>
  </div>
}
