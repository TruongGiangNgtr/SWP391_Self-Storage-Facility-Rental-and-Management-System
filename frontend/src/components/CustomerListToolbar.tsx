import { useId } from 'react'
import { EmptyState } from './PageStates'

interface Props {
  query: string
  status: string
  statuses: readonly string[]
  statusLabels?: Partial<Record<string, string>>
  searchPlaceholder?: string
  visibleCount: number
  onQueryChange(value: string): void
  onStatusChange(value: string): void
}

export function CustomerListToolbar({ query, status, statuses, statusLabels, searchPlaceholder = 'Search by reference or details', visibleCount, onQueryChange, onStatusChange }: Props) {
  const id = useId()
  return <>
    <section className="panel customer-list-toolbar" aria-label="Filter current page">
      <div className="form-field"><label htmlFor={`${id}-search`}>Search this page</label><input id={`${id}-search`} type="search" value={query} placeholder={searchPlaceholder} onChange={event => onQueryChange(event.target.value)} /></div>
      <div className="form-field"><label htmlFor={`${id}-status`}>Status</label><select id={`${id}-status`} value={status} onChange={event => onStatusChange(event.target.value)}><option value="">All statuses</option>{statuses.map(value => <option value={value} key={value}>{statusLabels?.[value] ?? value.replaceAll('_', ' ')}</option>)}</select></div>
      <button className="button button-secondary" type="button" onClick={() => { onQueryChange(''); onStatusChange('') }}>Clear Filters</button>
      <p className="muted customer-filter-note" aria-live="polite">{visibleCount} matching {visibleCount === 1 ? 'record' : 'records'} on this page. Filters do not search other pages.</p>
    </section>
    {visibleCount === 0 && <EmptyState message="No records on this page match these filters. Clear filters or review another page." />}
  </>
}
