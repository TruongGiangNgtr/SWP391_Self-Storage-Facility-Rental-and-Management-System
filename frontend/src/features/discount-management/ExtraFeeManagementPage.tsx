import { useEffect, useState, type SyntheticEvent } from 'react'
import { businessApi } from '../../api/businessApi'
import type { ApiCollectionResponse } from '../../api/api.types'
import { presentApiError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { ApiErrorAlert } from '../../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../../components/PageStates'
import type { ExtraFeeTypeDetail } from '../return-management/return.types'

export function ExtraFeeManagementPage() {
  const [page, setPage] = useState(1)
  const [revision, setRevision] = useState(0)
  const [response, setResponse] = useState<ApiCollectionResponse<ExtraFeeTypeDetail> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [editing, setEditing] = useState<ExtraFeeTypeDetail | null>(null)
  const [amount, setAmount] = useState('')
  const [status, setStatus] = useState<ExtraFeeTypeDetail['status']>('ACTIVE')
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<ApiErrorPresentation | null>(null)
  const [notice, setNotice] = useState('')

  useEffect(() => {
    let active = true
    setLoading(true)
    setError(null)
    businessApi.listExtraFeeTypes(page, 20).then(data => {
      if (active) setResponse(data)
    }).catch(reason => {
      if (active) { setResponse(null); setError(presentApiError(reason)) }
    }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [page, revision])

  async function save(event: SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!editing || saving) return
    const value = Number(amount)
    if (!amount.trim() || !Number.isFinite(value) || value < 0) {
      setFormError({ message: 'Enter a valid non-negative default amount.', details: [] })
      return
    }
    setSaving(true)
    setFormError(null)
    try {
      await businessApi.updateExtraFeeType(editing.extraFeeTypeId, { defaultAmount: value, status })
      setEditing(null)
      setNotice('Extra fee configuration saved.')
      setRevision(current => current + 1)
    } catch (reason) { setFormError(presentApiError(reason)) }
    finally { setSaving(false) }
  }

  return <div>
    <div className="facility-page-heading"><div><h2>Extra Fee Configuration</h2>
      <p>Update default amounts and availability for return inspection fees.</p></div></div>
    {notice && <p role="status" className="fee-success">{notice}</p>}
    <ApiErrorAlert error={error} />
    {error && <button className="facility-secondary-button" onClick={() => setRevision(current => current + 1)}>Retry</button>}
    {loading && <LoadingState label="Loading extra fee configurations..." />}
    {!loading && !error && response && <div className="facility-table-card">
      <div className="facility-table-scroll"><table className="facility-table">
        <thead><tr><th>Fee type</th><th>Default amount</th><th>Status</th><th>Actions</th></tr></thead>
        <tbody>{response.data.map(fee => <tr key={fee.extraFeeTypeId}>
          <td>{fee.name.replaceAll('_', ' ')}</td>
          <td>{new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 }).format(fee.defaultAmount)}</td>
          <td><span className={`facility-status facility-status--${fee.status === 'ACTIVE' ? 'active' : 'inactive'}`}>{fee.status}</span></td>
          <td><button className="facility-secondary-button" onClick={() => {
            setEditing(fee); setAmount(String(fee.defaultAmount)); setStatus(fee.status); setFormError(null); setNotice('')
          }}>Edit</button></td>
        </tr>)}</tbody>
      </table></div>
      {response.data.length === 0 && <EmptyState message="No extra fee types are configured. Contact the deployment owner to provision the approved catalogue." />}
      <div className="facility-table-footer"><span>{response.pagination.totalItems} fee types</span>
        <PaginationControls pagination={response.pagination} onPageChange={setPage} /></div>
    </div>}
    {editing && <div className="facility-modal-backdrop" onKeyDown={event => { if (event.key === 'Escape' && !saving) setEditing(null) }}>
      <div className="facility-modal discount-modal" role="dialog" aria-modal="true" aria-labelledby="fee-edit-title">
        <div className="facility-modal-header"><h2 id="fee-edit-title">Edit Extra Fee</h2>
          <button className="facility-modal-close" disabled={saving} aria-label="Close extra fee dialog" onClick={() => setEditing(null)}>×</button></div>
        <form className="facility-form" onSubmit={save}>
          <div className="facility-form-field"><label htmlFor="fee-name">Fee type</label><input id="fee-name" value={editing.name.replaceAll('_', ' ')} readOnly /></div>
          <div className="facility-form-field"><label htmlFor="fee-amount">Default amount</label>
            <input autoFocus id="fee-amount" type="number" min="0" step="0.01" required value={amount} disabled={saving} onChange={event => setAmount(event.target.value)} /></div>
          <div className="facility-form-field"><label htmlFor="fee-status">Status</label>
            <select id="fee-status" value={status} disabled={saving} onChange={event => setStatus(event.target.value as ExtraFeeTypeDetail['status'])}>
              <option value="ACTIVE">ACTIVE</option><option value="INACTIVE">INACTIVE</option></select></div>
          <ApiErrorAlert error={formError} />
          <div className="facility-modal-actions"><button type="button" className="facility-secondary-button" disabled={saving} onClick={() => setEditing(null)}>Cancel</button>
            <button className="facility-primary-button" disabled={saving}>{saving ? 'Saving...' : 'Save Changes'}</button></div>
        </form>
      </div>
    </div>}
  </div>
}
