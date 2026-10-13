import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { usePublicFacilityTypes } from '../features/catalog/usePublicCatalog'
import type { PublicCatalogFilters, UnitTypeAvailability } from '../features/catalog/catalog.types'
import { formatMoney, formatMonthRange } from '../utils/formatters'
import { ApiErrorAlert } from './ApiErrorAlert'
import { FlowIcon } from './FlowIcon'
import { EmptyState, LoadingState, PaginationControls } from './PageStates'

function ReservationChoice({ item, facilityId, filters, disabled }: { item: UnitTypeAvailability; facilityId: string; filters: PublicCatalogFilters; disabled: boolean }) {
  const { user, isInitializing } = useAuth()
  const period = Boolean(filters.startMonth && filters.endMonth)
  const available = period && item.availableCapacity !== null && item.availableCapacity > 0
  if (!available || disabled || isInitializing) return <><button className="button" type="button" disabled>Create Reservation</button><small className="muted">{disabled ? 'Apply your updated search first.' : !period ? 'Choose rental months to check availability.' : !available ? 'No availability has been confirmed for this period.' : 'Checking your session…'}</small></>
  if (user && user.role !== 'CUSTOMER') return <><p className="public-field-note">Reservations require a Customer account.</p><Link className="button button-secondary" to="/auth/customer/login">Customer Sign In</Link></>
  const query = new URLSearchParams({ facilityId, unitTypeId: item.unitTypeId, startMonth: filters.startMonth, endMonth: filters.endMonth })
  // The protected destination keeps the existing auth boundary. GWP-05 owns
  // the sign-in gate and restoration after login; viewing never submits RES-001.
  return <Link className="button" to={`/customer/reservations/new?${query}`}>{user ? 'Create Reservation' : 'Sign In to Reserve'}</Link>
}

export function PublicUnitTypeList({ facilityId, filters, disabled = false, detail = false }: { facilityId: string; filters: PublicCatalogFilters; disabled?: boolean; detail?: boolean }) {
  const [page, setPage] = useState(1)
  const [sort, setSort] = useState<'name' | 'rentalPrice'>('name')
  const hasPeriod = Boolean(filters.startMonth && filters.endMonth)
  const result = usePublicFacilityTypes(facilityId, { mode: filters.mode, size: filters.size, unitTypeId: filters.unitTypeId, startMonth: filters.startMonth, endMonth: filters.endMonth, availableOnly: hasPeriod, page, pageSize: 20, sortBy: sort, sortDirection: 'asc' })
  return <section className="public-unit-section" aria-label="Facility storage options">
    <div className="section-heading"><h3>{hasPeriod ? 'Available Storage Options' : 'Storage Types Offered'}</h3>{detail && <div className="form-field"><label htmlFor="facility-unit-sort">Sort Storage Types</label><select id="facility-unit-sort" value={sort} onChange={event => { setSort(event.target.value as 'name' | 'rentalPrice'); setPage(1) }}><option value="name">Name A–Z</option><option value="rentalPrice">Monthly Rent: Low to High</option></select></div>}<button className="button button-secondary" type="button" disabled={result.loading} onClick={result.refresh}>Refresh Options</button></div>
    <ApiErrorAlert error={result.error} />
    {result.loading ? <LoadingState label="Checking storage options…" /> : result.data && <>
      {result.data.data.length === 0 ? <EmptyState message={hasPeriod ? 'No matching storage is available for this period. Availability may have changed; refresh or try different search criteria.' : 'No storage types have been returned for this facility.'} /> : <div className="flow-unit-list">
        {result.data.data.map(item => <article className="flow-unit-card public-unit-card" key={item.unitTypeId}>
          <div><div className="unit-card-heading"><span className="unit-icon-tile"><FlowIcon name="unit" /></span><h4>{item.name}</h4><span className="unit-size">{item.size}</span></div><p className="muted">{item.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'}</p>{item.description && <p className="public-description">{item.description}</p>}
            {hasPeriod ? <span className="unit-capacity">Available capacity for {formatMonthRange(filters.startMonth, filters.endMonth)}: <strong>{item.availableCapacity}</strong></span> : <p className="public-field-note">Choose a rental period to check availability.</p>}
          </div><div className="unit-price"><small>Current Monthly Rent</small><strong>{formatMoney(item.rentalPrice)}</strong><span>per month</span><ReservationChoice item={item} facilityId={facilityId} filters={filters} disabled={disabled} /></div>
        </article>)}
      </div>}
      <PaginationControls pagination={result.data.pagination} disabled={result.loading} onPageChange={setPage} />
    </>}
  </section>
}
