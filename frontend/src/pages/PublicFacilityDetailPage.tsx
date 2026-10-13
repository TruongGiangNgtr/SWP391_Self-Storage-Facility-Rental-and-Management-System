import { useMemo, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { PublicStorageSearchForm } from '../components/PublicStorageSearchForm'
import { PublicUnitTypeList } from '../components/PublicUnitTypeList'
import { LoadingState } from '../components/PageStates'
import { readPublicSearch, initialPublicFilters, publicSearchQuery } from '../features/catalog/publicSearch'
import { usePublicFacility } from '../features/catalog/usePublicCatalog'
import { isUuid } from '../features/handover/handoverValidation'
import { presentValidationError } from '../api/apiErrorPresentation'

function FacilityDetail() {
  const { facilityId = '' } = useParams()
  const { search } = useLocation()
  const navigate = useNavigate()
  const query = useMemo(() => readPublicSearch(search), [search])
  const [dirty, setDirty] = useState(false)
  const valid = isUuid(facilityId)
  const facility = usePublicFacility(facilityId, valid)
  const filters = { ...query.filters, facilityId }
  const hasPeriod = Boolean(filters.startMonth && filters.endMonth)
  return <main className="page-container flow-page public-facility-page">
    <section className="page-heading"><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><Link to={`/facilities?${publicSearchQuery({ ...query.filters, facilityId: '' })}`}>Find Storage</Link><span aria-hidden="true">/</span><span aria-current="page">Facility Details</span></nav><p className="eyebrow">Storage Facility</p><h1>{facility.data?.data.name ?? 'Facility Details'}</h1><p className="muted">Review the facility and check its storage options for your rental period.</p></section>
    <ApiErrorAlert error={!valid ? presentValidationError('This facility link is invalid. Start a new storage search.') : query.error ? presentValidationError(query.error) : facility.error} />
    {facility.loading ? <LoadingState label="Loading facility details…" /> : facility.data && <>
      <section className="panel public-facility-summary"><div className="public-facility-image" aria-hidden="true" /><div><h2>{facility.data.data.name}</h2><p className="flow-facility-address"><img src="/figma/gwp-01/imgIcon12.svg" alt="" aria-hidden="true" />{facility.data.data.address}</p>{facility.data.data.description && <p className="public-description">{facility.data.data.description}</p>}<h3>Contact Information</h3><p>{facility.data.data.contactInfo || 'Contact information is not currently provided for this facility.'}</p><p className="public-field-note">Availability depends on your selected storage type and rental months.</p></div></section>
      <PublicStorageSearchForm initialValues={hasPeriod ? filters : { ...filters, startMonth: initialPublicFilters().startMonth, endMonth: initialPublicFilters().endMonth }} pinnedFacility={facility.data.data} onDirtyChange={setDirty} onSearch={next => navigate(`/facilities/${encodeURIComponent(facilityId)}?${publicSearchQuery(next)}`)} />
      {dirty && <p className="notice">Filters changed. Select Find Storage to check the updated period.</p>}
      {!query.error && <PublicUnitTypeList key={search} facilityId={facilityId} filters={filters} disabled={dirty} detail />}
    </>}
    {facility.error && <button className="button button-secondary" type="button" onClick={facility.refresh}>Retry Facility</button>}
    <Link className="button button-secondary" to={`/facilities?${publicSearchQuery({ ...query.filters, facilityId: '' })}`}>Search All Facilities</Link>
  </main>
}
export function PublicFacilityDetailPage() {
  const { pathname, search } = useLocation()
  return <FacilityDetail key={`${pathname}${search}`} />
}
