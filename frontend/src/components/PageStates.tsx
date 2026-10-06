import type { Pagination } from '../api/api.types'

export function LoadingState({ label = 'Loading...' }: { label?: string }) {
  return (
    <div className="page-state" role="status" aria-live="polite">
      {label}
    </div>
  )
}

export function EmptyState({ message }: { message: string }) {
  return <div className="page-state empty-state">{message}</div>
}

interface PaginationControlsProps {
  pagination: Pagination
  disabled?: boolean
  onPageChange(page: number): void
}

export function PaginationControls({
  pagination,
  disabled = false,
  onPageChange,
}: PaginationControlsProps) {
  if (pagination.totalPages <= 1) {
    return null
  }

  return (
    <nav className="pagination" aria-label="Pagination">
      <button
        className="button button-secondary"
        type="button"
        disabled={disabled || pagination.page <= 1}
        onClick={() => onPageChange(pagination.page - 1)}
      >
        Previous Page
      </button>
      <span>
        Page {pagination.page}/{pagination.totalPages}
      </span>
      <button
        className="button button-secondary"
        type="button"
        disabled={disabled || pagination.page >= pagination.totalPages}
        onClick={() => onPageChange(pagination.page + 1)}
      >
        Next Page
      </button>
    </nav>
  )
}
