import { useEffect, useId, useMemo, useRef, type KeyboardEvent } from 'react'
import { Link, useLocation, useNavigate, useNavigationType } from 'react-router-dom'
import { Box } from 'lucide-react'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { SizeGuideUnitTypeCard } from '../components/SizeGuideUnitTypeCard'
import { SizeGuideAssistant } from '../components/SizeGuideAssistant'
import { presentValidationError } from '../api/apiErrorPresentation'
import { usePublicUnitTypes } from '../features/catalog/usePublicCatalog'
import { readSizeGuideSearch, sizeGuideUrl, type SizeGuideSort } from '../features/catalog/sizeGuideSearch'
import type { UnitMode } from '../features/catalog/catalog.types'
import '../styles/size-guide.css'

const MODES: readonly UnitMode[] = ['PRIVATE', 'PUBLIC']
const modeName = (mode: UnitMode) => mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'

export function SizeGuidePage() {
  const { search } = useLocation()
  const navigate = useNavigate()
  const navigationType = useNavigationType()
  const previousSearch = useRef(search)
  const query = useMemo(() => readSizeGuideSearch(search), [search])
  const id = useId()
  const tabs = useRef<Array<HTMLButtonElement | null>>([])
  const catalog = usePublicUnitTypes({ mode: query.mode, page: query.page, pageSize: 20, sortBy: query.sortBy, sortDirection: query.sortDirection }, !query.error)
  useEffect(() => {
    if (previousSearch.current !== search && navigationType !== 'POP') document.getElementById('storage-sizes')?.scrollIntoView({ block: 'start', behavior: 'instant' })
    previousSearch.current = search
  }, [search, navigationType])
  function changeMode(mode: UnitMode) { navigate(sizeGuideUrl(mode, 1, query.sort)) }
  function tabKey(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    let next: number
    if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') next = (index + 1) % MODES.length
    else if (event.key === 'Home') next = 0
    else if (event.key === 'End') next = MODES.length - 1
    else return
    event.preventDefault(); tabs.current[next]?.focus(); changeMode(MODES[next])
  }
  const content = <>
    <p className="size-guide-mode-description">{query.mode === 'PRIVATE' ? 'Compare dedicated storage types for your belongings. The specific unit is selected during handover.' : 'Compare storage types offered within shared storage space. Choose a facility and rental months before checking availability.'}</p>
    <div className="size-guide-toolbar"><p className="muted" aria-live="polite">{catalog.loading ? 'Loading offered storage types…' : catalog.data ? `${catalog.data.pagination.totalItems} offered ${catalog.data.pagination.totalItems === 1 ? 'storage type' : 'storage types'}` : 'Storage types from the public catalog'}</p><div className="form-field"><label htmlFor={`${id}-sort`}>Sort Storage Types</label><select id={`${id}-sort`} value={query.sort} onChange={event => navigate(sizeGuideUrl(query.mode, 1, event.target.value as SizeGuideSort))}><option value="name:asc">Name A–Z</option><option value="name:desc">Name Z–A</option><option value="rentalPrice:asc">Monthly Rent: Low to High</option><option value="rentalPrice:desc">Monthly Rent: High to Low</option></select></div></div>
    <ApiErrorAlert error={query.error ? presentValidationError(query.error) : catalog.error} />
    {!query.error && catalog.error && <button className="button button-secondary" type="button" onClick={catalog.refresh}>Retry Storage Types</button>}
    {catalog.loading ? <LoadingState label="Loading the Size Guide…" /> : catalog.data && <>
      {catalog.data.data.length === 0 ? <EmptyState message={`No ${modeName(query.mode).toLowerCase()} types were returned for this page. Try the other storage type or return to the first page.`} /> : <div className="size-guide-grid">{catalog.data.data.map(item => <SizeGuideUnitTypeCard item={item} key={item.unitTypeId} />)}</div>}
      <PaginationControls pagination={catalog.data.pagination} disabled={catalog.loading} onPageChange={page => navigate(sizeGuideUrl(query.mode, page, query.sort))} />
      {query.page > 1 && catalog.data.data.length === 0 && <Link className="button button-secondary" to={sizeGuideUrl(query.mode, 1, query.sort)}>Back to First Page</Link>}
    </>}
    <p className="size-guide-catalog-note">Sizes and listed prices come from the offered catalog. This guide does not confirm stock for a location or rental period and does not reserve a storage unit.</p>
  </>

  return <main className="page-container public-size-guide">
    <section className="size-guide-hero"><div className="gwp-container"><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">Size Guide</span></nav><div className="size-guide-hero-copy"><span className="size-guide-icon"><Box size={24} aria-hidden="true" /></span><p className="eyebrow">Storage Size Guide</p><h1>Find the right space for your belongings.</h1><p className="muted">Compare Private and Public Storage using the offered sizes, descriptions and current monthly prices.</p></div></div></section>
    <section className="size-guide-catalog" id="storage-sizes"><div className="gwp-container"><div className="size-guide-catalog-heading"><h2>Choose the size that fits your needs</h2><div className="size-guide-tabs" role="tablist" aria-label="Storage type">{MODES.map((mode, index) => <button key={mode} id={`${id}-tab-${mode}`} ref={element => { tabs.current[index] = element }} role="tab" type="button" aria-selected={mode === query.mode} aria-controls={`${id}-panel-${mode}`} tabIndex={mode === query.mode ? 0 : -1} onClick={() => changeMode(mode)} onKeyDown={event => tabKey(event, index)}>{modeName(mode)}</button>)}</div></div>
      {MODES.map(mode => <section key={mode} role="tabpanel" id={`${id}-panel-${mode}`} aria-labelledby={`${id}-tab-${mode}`} hidden={query.mode !== mode} tabIndex={0}>{query.mode === mode && content}</section>)}
    </div></section>
    <div className="size-guide-assistant-section"><div className="gwp-container"><SizeGuideAssistant key={query.mode} mode={query.mode} enabled={!query.error} /></div></div>
  </main>
}
