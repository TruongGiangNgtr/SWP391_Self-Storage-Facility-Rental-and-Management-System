import { useEffect, useMemo, useState } from 'react'
import { RefreshCw } from 'lucide-react'
import { Link, useSearchParams } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { contractApi } from '../api/contractApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { StaffContractDrawer } from '../components/StaffContractDrawer'
import { isContractListResponse, type ContractListItem } from '../features/contracts/contractListPresentation'
import { employeeMonthRange, employeeText, employeeUnitLabel, fetchAllEmployeePages } from '../utils/employeePresentation'

const PAGE_SIZE = 20

export function StaffContractListPage() {
  const [params, setParams] = useSearchParams()
  const rawPage = Number(params.get('page') ?? '1')
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1
  const [revision, setRevision] = useState(0)
  const [selectedContractId, setSelectedContractId] = useState<string | null>(null)
  const [quarter, setQuarter] = useState('')
  const [year, setYear] = useState(() => new Intl.DateTimeFormat('en', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric' }).format(new Date()))
  const [activeOnly, setActiveOnly] = useState(false)
  const key = String(revision)
  const [snapshot, setSnapshot] = useState<{
    key: string
    contracts: ContractListItem[] | null
    error: ApiErrorPresentation | null
  } | null>(null)
  const loading = snapshot?.key !== key
  const allContracts = loading ? null : snapshot?.contracts ?? null
  const error = loading ? null : snapshot?.error ?? null
  const validYear = /^\d{1,4}$/.test(year) && Number(year) >= 1 && Number(year) <= 9999
  const quarterStart = `${year.padStart(4, '0')}-${String(Number(quarter) * 3 - 2).padStart(2, '0')}`
  const quarterEnd = `${year.padStart(4, '0')}-${String(Number(quarter) * 3).padStart(2, '0')}`

  const filteredContracts = useMemo(
    () => (allContracts ?? []).filter(contract =>
      (!activeOnly || contract.status === 'ACTIVE') &&
      (!quarter || (validYear && contract.startMonth <= quarterEnd && contract.endMonth >= quarterStart))),
    [activeOnly, allContracts, quarter, quarterEnd, quarterStart, validYear],
  )
  const totalPages = Math.max(1, Math.ceil(filteredContracts.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const visibleContracts = useMemo(
    () => filteredContracts.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [filteredContracts, safePage],
  )

  function changePage(next: number) {
    setParams(next === 1 ? {} : { page: String(next) })
  }

  useEffect(() => {
    let active = true
    fetchAllEmployeePages(async (nextPage, pageSize) => {
      const result = await contractApi.list(nextPage, pageSize)
      if (!isContractListResponse(result, nextPage)) {
        throw new Error('INVALID_CONTRACT_LIST_RESPONSE')
      }
      return result
    }).then(result => {
      if (active) setSnapshot({ key, contracts: result.items as ContractListItem[], error: null })
    }).catch((caught: unknown) => {
      if (!active) return
      if (caught instanceof Error && caught.message === 'INVALID_CONTRACT_LIST_RESPONSE') {
        setSnapshot({ key, contracts: null, error: presentValidationError('The contract list could not be loaded. Please refresh and try again.') })
        return
      }
      setSnapshot({ key, contracts: null, error: presentApiError(caught) })
    })
    return () => { active = false }
  }, [key])

  return (
    <div className="page-container flow-page staff-flow">
      <section className="page-heading page-heading-actions">
        <div>
          <p className="eyebrow">Facility Staff</p>
          <h1>View Contracts</h1>
          <p className="muted">View rental contracts in your assigned facility. Open a contract to review its details.</p>
        </div>
        <button className="button button-secondary" type="button" disabled={loading} onClick={() => setRevision(value => value + 1)}>
          <RefreshCw size={16} aria-hidden="true" /> Refresh
        </button>
      </section>
      <section className="fwp-toolbar" aria-label="Contract filters">
        <label>
          <span>Quarter</span>
          <select value={quarter} onChange={event => { setQuarter(event.target.value); changePage(1) }}>
            <option value="">All quarters</option>
            <option value="1">Q1 · January–March</option>
            <option value="2">Q2 · April–June</option>
            <option value="3">Q3 · July–September</option>
            <option value="4">Q4 · October–December</option>
          </select>
        </label>
        <label>
          <span>Year</span>
          <input type="number" min="1" max="9999" step="1" value={year} disabled={!quarter} aria-invalid={Boolean(quarter) && !validYear} aria-describedby={quarter && !validYear ? 'contract-year-error' : undefined} onChange={event => { setYear(event.target.value); changePage(1) }} />
        </label>
        <label>
          <span>Status</span>
          <select value={activeOnly ? 'ACTIVE' : ''} onChange={event => { setActiveOnly(event.target.value === 'ACTIVE'); changePage(1) }}>
            <option value="">All statuses</option>
            <option value="ACTIVE">Active only</option>
          </select>
        </label>
        <button className="button button-secondary" type="button" onClick={() => { setQuarter(''); setActiveOnly(false); changePage(1) }}>Clear Filters</button>
        {quarter && !validYear && <p id="contract-year-error" className="fwp-toolbar-note" role="alert">Enter a year from 1 to 9999.</p>}
        <p className="fwp-toolbar-note">Filters apply across all contracts in your facility. A contract matches if its rental period overlaps the selected quarter.</p>
      </section>
      <ApiErrorAlert error={error} />
      {loading && <LoadingState label="Loading contracts…" />}
      {allContracts && <>
        <p className="muted" role="status">{filteredContracts.length} matching contracts out of {allContracts.length} total in your facility · Read only</p>
        {visibleContracts.length === 0
          ? <EmptyState message={allContracts.length ? 'No contracts in your facility match your filters.' : 'No contracts in your facility.'} />
          : <section className="fwp-table-card" aria-label="Facility contracts">
            <div className="fwp-table-wrap">
              <table className="fwp-table">
                <thead><tr><th>Storage unit</th><th>Storage type</th><th>Rental period</th><th>Status</th><th>Details</th></tr></thead>
                <tbody>{visibleContracts.map(contract => (
                  <tr key={contract.contractId}>
                    <td><strong>{employeeText(contract.unitTypeName ?? contract.storageUnit?.unitTypeName)}</strong><div className="muted">{employeeUnitLabel(contract)}</div></td>
                    <td>{employeeText(contract.unitTypeName ?? contract.storageUnit?.unitTypeName)}</td>
                    <td>{employeeMonthRange(contract.startMonth, contract.endMonth)}</td>
                    <td><StatusBadge status={contract.status} /></td>
                    <td><button type="button" className="fwp-link-button" aria-label={`View contract · ${employeeUnitLabel(contract)} · ${employeeMonthRange(contract.startMonth, contract.startMonth ? contract.endMonth : contract.endMonth)}`} onClick={() => setSelectedContractId(contract.contractId)}>View Contract</button></td>
                  </tr>
                ))}</tbody>
              </table>
            </div>
          </section>}
        <PaginationControls
          pagination={{ page: safePage, pageSize: PAGE_SIZE, totalItems: filteredContracts.length, totalPages }}
          onPageChange={changePage}
        />
        {visibleContracts.length === 0 && page > 1 && <Link className="button button-secondary" to="/staff/contracts">Back to First Page</Link>}
      </>}
      {selectedContractId && <StaffContractDrawer contractId={selectedContractId} onClose={() => setSelectedContractId(null)} />}
    </div>
  )
}
