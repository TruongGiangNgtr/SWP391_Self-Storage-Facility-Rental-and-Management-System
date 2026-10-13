import { PublicStorageSearchForm } from '../components/PublicStorageSearchForm'
import { PublicStorageSearchPage } from './PublicStorageSearchPage'

// Compatibility entry point: Customer Dashboard and the public catalogue share
// the same form, API contracts and results instead of separate search workflows.
export function StorageSearchPage({ embedded = false }: { embedded?: boolean }) {
  return embedded ? <PublicStorageSearchForm embedded /> : <PublicStorageSearchPage />
}
