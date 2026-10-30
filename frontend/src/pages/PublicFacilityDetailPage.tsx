import { useMemo, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, ArrowRight, Building2, MapPin, MessageCircle } from 'lucide-react'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { BookingJourney } from '../components/BookingJourney'
import { PublicStorageSearchForm } from '../components/PublicStorageSearchForm'
import { FacilityImage } from '../components/FacilityImage'
import { PublicUnitTypeList } from '../components/PublicUnitTypeList'
import { LoadingState } from '../components/PageStates'
import { readPublicSearch, initialPublicFilters, publicSearchQuery } from '../features/catalog/publicSearch'
import { usePublicFacility } from '../features/catalog/usePublicCatalog'
import { isUuid } from '../features/handover/handoverValidation'
import { presentValidationError } from '../api/apiErrorPresentation'
import '../styles/guest-customer-remaining.css'

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
  return <main className="page-container flow-page public-facility-page public-facility-detail-design booking-facility">
    <section className="page-heading"><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><Link to={`/facilities?${publicSearchQuery({ ...query.filters, facilityId: '' })}`}>Find Storage</Link><span aria-hidden="true">/</span><span aria-current="page">Facility Details</span></nav><p className="eyebrow">Storage Facility</p><h1>{facility.data?.data.name ?? 'Facility Details'}</h1><p className="muted">Review the facility and check its storage options for your rental period.</p></section>
    <BookingJourney step={0} />
    <ApiErrorAlert error={!valid ? presentValidationError('This facility link is invalid. Start a new storage search.') : query.error ? presentValidationError(query.error) : facility.error} />
    {facility.loading ? <LoadingState label="Loading facility details…" /> : facility.data && <>
      <div className="guest-facility-overview"><FacilityImage facility={facility.data.data} className="public-facility-cover" eager />
      <section className="panel public-facility-summary">
        <div className="facility-location-card"><span className="remaining-icon"><MapPin size={24} aria-hidden="true" /></span><p className="eyebrow">Facility Location</p><h2>{facility.data.data.name}</h2><p className="facility-location-address">{facility.data.data.address}</p><a className="button" href="#facility-storage-options">View Storage Options<ArrowRight size={16} aria-hidden="true" /></a></div>
        <div className="facility-about"><div className="remaining-card-heading"><span className="remaining-icon"><Building2 size={22} aria-hidden="true" /></span><h2>About This Facility</h2></div>{facility.data.data.description && <p className="public-description">{facility.data.data.description}</p>}<div className="facility-contact"><h3><MessageCircle size={18} aria-hidden="true" />Contact Information</h3><p>{facility.data.data.contactInfo || 'Contact information is not currently provided for this facility.'}</p></div><p className="public-field-note">Availability depends on your selected storage type and rental months.</p></div>
      </section></div>
      <div className="facility-options-heading" id="facility-storage-options"><p className="eyebrow">Find Your Space</p><h2>Choose Your Storage and Rental Period</h2><p className="muted">Review storage options for this facility using the filters below.</p></div>
      <div className="booking-facility-options">
      <aside className="booking-filter-rail" aria-label="Facility search preferences"><PublicStorageSearchForm initialValues={hasPeriod ? filters : { ...filters, startMonth: initialPublicFilters().startMonth, endMonth: initialPublicFilters().endMonth }} pinnedFacility={facility.data.data} embedded onDirtyChange={setDirty} onSearch={next => navigate(`/facilities/${encodeURIComponent(facilityId)}?${publicSearchQuery(next)}`)} /></aside>
      <div className="booking-options-list">
      {dirty && <p className="notice">Filters changed. Select Find Storage to check the updated period.</p>}
      {!query.error && <PublicUnitTypeList key={search} facilityId={facilityId} facilityName={facility.data.data.name} filters={filters} disabled={dirty} detail />}
      </div></div>
    </>}
    {facility.error && <button className="button button-secondary" type="button" onClick={facility.refresh}>Retry Facility</button>}
    <div className="facility-detail-actions"><Link className="button button-secondary" to={`/facilities?${publicSearchQuery({ ...query.filters, facilityId: '' })}`}><ArrowLeft size={16} aria-hidden="true" />Search All Facilities</Link></div>
  </main>
}
export function PublicFacilityDetailPage() {
  const { pathname, search } = useLocation()
  return <FacilityDetail key={`${pathname}${search}`} />
}
