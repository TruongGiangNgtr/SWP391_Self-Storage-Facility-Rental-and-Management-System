import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import type { ApiCollectionResponse } from '../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { contractApi } from '../api/contractApi'
import { createSupportTicket } from '../api/supportTicketApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { isSupportPagination, isSupportTicket } from '../features/customer-support/supportValidation'
import { isUuid } from '../features/handover/handoverValidation'
import type { HandoverContractDetail } from '../features/handover/contract.types'
import { SUPPORT_TICKET_CATEGORIES, type SupportTicketCategory } from '../features/manager-support/models/supportTicket'
import { contractOption, customerLabel } from '../utils/customerPresentation'
import { CustomerFieldError } from '../components/CustomerFieldError'

export function CustomerSupportCreatePage() {
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const [contractId, setContractId] = useState('')
  const [category, setCategory] = useState<SupportTicketCategory | ''>('')
  const [description, setDescription] = useState('')
  const [working, setWorking] = useState(false)
  const [actionError, setActionError] = useState<ApiErrorPresentation | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const key = `${page}:${retry}`
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<HandoverContractDetail> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = snapshot?.key !== key
  const response = loading ? null : snapshot.response
  const readError = loading ? null : snapshot.error
  const busy = useRef(false)
  const mounted = useRef(false)
  const navigate = useNavigate()
  useEffect(() => { mounted.current = true; return () => { mounted.current = false } }, [])
  useEffect(() => {
    let active = true
    contractApi.list(page).then(response => {
      if (!active) return
      if (!Array.isArray(response.data) || !response.data.every(contract => isUuid(contract.contractId)) || !isSupportPagination(response.pagination)) {
        setSnapshot({ key, response: null, error: presentValidationError('The server returned an unexpected contract list. Refresh before continuing.') })
      } else setSnapshot({ key, response, error: null })
    })
      .catch((error: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [page, key])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy.current || loading || !response) return
    const text = description.trim()
    const nextErrors: Record<string, string> = {}
    if (!response.data.some(contract => contract.contractId === contractId)) nextErrors.contract = 'Choose a contract from this page.'
    if (!category || !SUPPORT_TICKET_CATEGORIES.includes(category)) nextErrors.category = 'Choose a category.'
    if (!text) nextErrors.description = 'Describe the issue.'
    setFieldErrors(nextErrors)
    if (Object.keys(nextErrors).length || !category) { setActionError(null); return }
    busy.current = true; setWorking(true); setActionError(null)
    try {
      const { data } = await createSupportTicket({ contractId, category, description: text })
      if (!mounted.current) return
      if (!isSupportTicket(data) || data.contractId !== contractId || data.category !== category ||
        data.status !== 'OPEN' || data.assignedEmployeeId !== null || data.completedAt !== null || data.resultNote !== null) {
        setActionError(presentValidationError('The server returned an unexpected ticket. Check My Support Tickets before submitting again.'))
        return
      }
      navigate(`/customer/support-tickets/${encodeURIComponent(data.supportTicketId)}`)
    } catch (error) { if (mounted.current) setActionError(presentApiError(error)) }
    finally { busy.current = false; if (mounted.current) setWorking(false) }
  }

  return <main className="page-container flow-page narrow-page">
    <section className="page-heading page-heading-actions"><div><p className="eyebrow">Customer Support</p><h1>Create Support Ticket</h1></div><Link className="button button-secondary" to="/customer/support-tickets">My Support Tickets</Link></section>
    <ApiErrorAlert error={actionError ?? readError} />
    <button className="button button-secondary" type="button" disabled={loading || working} onClick={() => { setContractId(''); setRetry(value => value + 1) }}>Refresh Contracts</button>
    {loading ? <LoadingState label="Loading your contracts..." /> : response && (response.data.length === 0 ? <EmptyState message="A support ticket must reference one of your contracts. You do not have any contracts on this page." /> : <section className="panel stack">
      <form className="form-grid" onSubmit={submit} noValidate>
        <div className="form-field"><label htmlFor="supportContract">Contract</label><select id="supportContract" value={contractId} disabled={working} aria-invalid={Boolean(fieldErrors.contract)} aria-describedby={fieldErrors.contract ? 'supportContract-error' : undefined} onChange={event => { setContractId(event.target.value); setFieldErrors(current => ({ ...current, contract: '' })) }}><option value="">Choose a contract</option>{response.data.map(contract => <option key={contract.contractId} value={contract.contractId}>{contractOption(contract)}</option>)}</select><CustomerFieldError id="supportContract-error" message={fieldErrors.contract} /></div>
        <div className="form-field"><label htmlFor="supportCategory">Category</label><select id="supportCategory" value={category} disabled={working} aria-invalid={Boolean(fieldErrors.category)} aria-describedby={fieldErrors.category ? 'supportCategory-error' : undefined} onChange={event => { setCategory(event.target.value as SupportTicketCategory | ''); setFieldErrors(current => ({ ...current, category: '' })) }}><option value="">Choose a category</option>{SUPPORT_TICKET_CATEGORIES.map(value => <option key={value} value={value}>{customerLabel(value)}</option>)}</select><CustomerFieldError id="supportCategory-error" message={fieldErrors.category} /></div>
        <div className="form-field"><label htmlFor="supportDescription">Description</label><textarea id="supportDescription" value={description} disabled={working} aria-invalid={Boolean(fieldErrors.description)} aria-describedby={fieldErrors.description ? 'supportDescription-error' : undefined} onChange={event => { setDescription(event.target.value); setFieldErrors(current => ({ ...current, description: '' })) }} /><CustomerFieldError id="supportDescription-error" message={fieldErrors.description} /></div>
        {category === 'OTHER' && <p className="notice">Describe the issue clearly so the Facility team can understand what needs help.</p>}
        <p className="muted">The Manager assigns Staff. Creating a ticket does not change your Contract, Payment or Storage Unit, and does not schedule a return.</p>
        <button className="button" type="submit" disabled={working}>{working ? 'Creating...' : 'Submit Support Ticket'}</button>
      </form>
    </section>)}
    {response && <PaginationControls pagination={response.pagination} disabled={working || loading} onPageChange={next => { setContractId(''); setPage(next) }} />}
    {actionError && <p className="notice">If the response was interrupted, check My Support Tickets before retrying. This page never automatically repeats the create request.</p>}
  </main>
}
