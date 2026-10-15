import { useEffect, useState } from 'react'
import { businessApi } from '../../api/businessApi'
import type { ApiCollectionResponse } from '../../api/api.types'
import { presentApiError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { ApiErrorAlert } from '../../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../../components/PageStates'

interface CustomerOption { customerId: string; fullName: string }

export function CustomerPicker({ disabled, selectedCustomer, onSelect }: {
  disabled: boolean
  selectedCustomer: CustomerOption | null
  onSelect(customer: CustomerOption): void
}) {
  const [page, setPage] = useState(1)
  const [revision, setRevision] = useState(0)
  const [data, setData] = useState<ApiCollectionResponse<{ customerId: string; fullName: string }> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  useEffect(() => {
    let active = true
    setLoading(true); setError(null)
    businessApi.listCustomers(page).then(result => { if (active) setData(result) })
      .catch(reason => { if (active) { setData(null); setError(presentApiError(reason)) } })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [page, revision])
  return <div className="customer-picker">
    <label htmlFor="discount-customer-picker">Customer name</label>
    <ApiErrorAlert error={error} />
    {error && <button type="button" className="facility-secondary-button" onClick={() => setRevision(value => value + 1)}>Retry customers</button>}
    {loading && <LoadingState label="Loading customers..." />}
    {!loading && data && <>
      <select id="discount-customer-picker" disabled={disabled} value={selectedCustomer?.customerId ?? ''} onChange={event => {
        const customer = data.data.find(item => item.customerId === event.target.value)
        if (customer) onSelect(customer)
      }}>
        <option value="" disabled>Choose a customer</option>
        {selectedCustomer && !data.data.some(customer => customer.customerId === selectedCustomer.customerId) &&
          <option value={selectedCustomer.customerId}>{selectedCustomer.fullName}</option>}
        {data.data.map(customer => <option key={customer.customerId} value={customer.customerId}>{customer.fullName}</option>)}
      </select>
      {data.data.length === 0 && <EmptyState message="No customers found." />}
      <PaginationControls pagination={data.pagination} disabled={disabled} onPageChange={setPage} />
    </>}
  </div>
}
