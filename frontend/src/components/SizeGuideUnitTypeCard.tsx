import { useId } from 'react'
import { Link } from 'react-router-dom'
import type { PublicUnitTypeSummary } from '../features/catalog/catalog.types'
import { sizeGuideStorageUrl } from '../features/catalog/sizeGuideSearch'
import { formatMoney } from '../utils/formatters'
import { SizeGuideUnitTypeMedia } from './SizeGuideUnitTypeMedia'

export function SizeGuideUnitTypeCard({ item }: { item: PublicUnitTypeSummary }) {
  const headingId = useId()
  return <article className="size-guide-card" aria-labelledby={headingId}>
    <SizeGuideUnitTypeMedia name={item.name} imageUrl={item.imageUrl} videoUrl={item.videoUrl} />
    <div className="size-guide-card-content">
      <div className="size-guide-card-heading"><h3 id={headingId}>{item.name}</h3><span className="size-guide-size">{item.size}</span></div>
      <p className="size-guide-mode">{item.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'}</p>
      <div className="size-guide-description"><h4>Storage Description</h4><p>{item.description?.trim() || 'Additional sizing information has not been provided for this storage type.'}</p></div>
      <div className="size-guide-price"><span>Current Monthly Rent</span><strong>{formatMoney(item.rentalPrice)}<small>/month</small></strong></div>
      <p className="public-field-note">Choose a facility and rental months to check availability.</p>
      <Link className="button button-secondary" to={sizeGuideStorageUrl(item)} aria-label={`Find Storage for ${item.name}`}>Find Storage</Link>
    </div>
  </article>
}
