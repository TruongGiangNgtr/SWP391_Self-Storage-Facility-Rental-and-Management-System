import { useId, useState } from 'react'
import { Link } from 'react-router-dom'
import { MapPin } from 'lucide-react'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'
import { ApiErrorAlert } from './ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from './PageStates'

// CAT-001 already includes the approved public ContactInfo projection.
// Do not infer telephone/email fields or publish sample support credentials.
export function PublicHelpContact() {
  const id = useId()
  const [page, setPage] = useState(1)
  const [selectedId, setSelectedId] = useState('')
  const facilities = usePublicFacilities({ page, pageSize: 20, sortBy: 'name', sortDirection: 'asc' })
  const selected = facilities.data?.data.find(item => item.facilityId === selectedId)
  return <section className="help-contact" id="contact" aria-labelledby="help-contact-heading"><div className="gwp-container">
    <div className="help-section-heading"><span className="help-icon-tile"><MapPin size={22} aria-hidden="true" /></span><h2 id="help-contact-heading">Contact a facility</h2><p>Choose a facility to see its published contact information.</p></div>
    <div className="help-contact-panel">
      <ApiErrorAlert error={facilities.error} />
      {facilities.error && <button className="button button-secondary" onClick={facilities.refresh} type="button">Retry Facility Contacts</button>}
      {facilities.loading ? <LoadingState label="Loading facility contacts…" /> : facilities.data && <>
        {facilities.data.data.length === 0 ? <EmptyState message="No facilities were returned for this page. Please check again later." /> : <div className="form-field"><label htmlFor={`${id}-facility`}>Select a Facility</label><select id={`${id}-facility`} value={selected?.facilityId ?? ''} onChange={event => setSelectedId(event.target.value)}><option value="">Choose a facility</option>{facilities.data.data.map(item => <option key={item.facilityId} value={item.facilityId}>{item.name}</option>)}</select></div>}
        {selected && <div className="help-facility-contact"><h3>{selected.name}</h3><p>{selected.address}</p><h4>Contact Information</h4><p className="help-plain-text">{selected.contactInfo?.trim() ? selected.contactInfo : 'No public contact information is currently provided for this facility.'}</p><Link className="button button-secondary" to={`/facilities/${selected.facilityId}`}>View Facility</Link></div>}
        <PaginationControls pagination={facilities.data.pagination} disabled={facilities.loading} onPageChange={next => { setSelectedId(''); setPage(next) }} />
        {page > 1 && facilities.data.data.length === 0 && <button className="button button-secondary" type="button" onClick={() => { setSelectedId(''); setPage(1) }}>Back to First Page</button>}
      </>}
      <p className="help-contact-note">For a request about your rental, use the Contract Support option below after signing in to your Customer account.</p>
    </div>
  </div></section>
}
