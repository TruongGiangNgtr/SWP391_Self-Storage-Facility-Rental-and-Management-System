import type { Pagination } from '../../api/api.types'

export function AdminListNavigation({ pagination, loading, onPage }: {
  pagination: Pagination
  loading: boolean
  onPage(page: number): void
}) {
  if (pagination.totalItems === 0) return null
  return (
    <nav className="awp01-pagination" aria-label="Account directory pages">
      <span>Directory page {pagination.page} of {Math.max(pagination.totalPages, 1)} · {pagination.totalItems} total accounts</span>
      <div>
        <button type="button" className="awp01-button awp01-button--secondary" disabled={loading || pagination.page <= 1} onClick={() => onPage(pagination.page - 1)}>Previous</button>
        <button type="button" className="awp01-button awp01-button--secondary" disabled={loading || pagination.page >= pagination.totalPages} onClick={() => onPage(pagination.page + 1)}>Next</button>
      </div>
    </nav>
  )
}

export function AdminFilterScope({ loading, page, loaded, matching, onClear }: {
  loading: boolean
  page: number
  loaded: number
  matching: number
  onClear(): void
}) {
  return (
    <div className="admin-filter-scope">
      <div role="status">
        <strong>{loading ? 'Loading accounts…' : `${matching} of ${loaded} loaded accounts match`}</strong>
        <p>Search and filters cover directory page {page} only. Other pages may contain more matches.</p>
      </div>
      <button type="button" className="awp01-button awp01-button--secondary" onClick={onClear} disabled={loading}>Clear filters</button>
    </div>
  )
}
