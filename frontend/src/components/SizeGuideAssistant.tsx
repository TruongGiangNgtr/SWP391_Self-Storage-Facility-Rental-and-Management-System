import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { Bot, MessageSquare } from 'lucide-react'
import { Link } from 'react-router-dom'
import { aiRecommendationApi } from '../api/aiRecommendationApi'
import { ApiErrorAlert } from './ApiErrorAlert'
import { AiStoragePolicyPanel } from './AiStoragePolicyPanel'
import { CustomerFieldError } from './CustomerFieldError'
import {
  AI_DESCRIPTION_MAX_LENGTH,
  presentAiError,
  validateAiDescription,
  type ResolvedAiCandidate,
  type ResolvedAiRecommendation,
} from '../features/catalog/aiRecommendation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import type { UnitMode } from '../features/catalog/catalog.types'
import { sizeGuideStorageUrl } from '../features/catalog/sizeGuideSearch'
import { formatMoney } from '../utils/formatters'

type AssistantState =
  | { status: 'idle' }
  | { status: 'loading'; description: string }
  | { status: 'success'; description: string; result: ResolvedAiRecommendation }
  | { status: 'error'; error: ApiErrorPresentation }

function RecommendationCard({ candidate, alternative = false }: { candidate: ResolvedAiCandidate; alternative?: boolean }) {
  const { unitType, reason } = candidate
  return <article className="size-guide-ai-card">
    <div className="size-guide-card-heading"><h4>{unitType.name}</h4><span className="size-guide-size">{unitType.size}</span></div>
    <p className="size-guide-mode">{alternative ? 'Alternative' : 'Recommended Storage Type'}</p>
    <p className="size-guide-mode">{unitType.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'}</p>
    <p className="size-guide-ai-reason">{reason}</p>
    <div className="size-guide-price"><span>Current Monthly Rent</span><strong>{formatMoney(unitType.rentalPrice)}<small>/month</small></strong></div>
    <Link className={`button${alternative ? ' button-secondary' : ''}`} to={sizeGuideStorageUrl(unitType)} aria-label={`Find Storage for ${unitType.name}`}>Find Storage</Link>
  </article>
}

export function SizeGuideAssistant({ mode, enabled = true }: { mode: UnitMode; enabled?: boolean }) {
  const id = useId()
  const [description, setDescription] = useState('')
  const [validation, setValidation] = useState<string | null>(null)
  const [state, setState] = useState<AssistantState>({ status: 'idle' })
  const [canceled, setCanceled] = useState(false)
  const pending = useRef<AbortController | null>(null)
  const input = useRef<HTMLTextAreaElement | null>(null)
  const loading = state.status === 'loading'
  useEffect(() => () => { pending.current?.abort() }, [])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending.current || !enabled) return
    const error = validateAiDescription(description)
    setValidation(error)
    setCanceled(false)
    if (error) { input.current?.focus(); return }
    const controller = new AbortController()
    pending.current = controller
    const submittedDescription = description.trim()
    setState({ status: 'loading', description: submittedDescription })
    try {
      const result = await aiRecommendationApi.recommend({ storageDescription: submittedDescription, preferredMode: mode }, controller.signal)
      if (!controller.signal.aborted) setState({ status: 'success', description: submittedDescription, result })
    } catch (error: unknown) {
      if (!controller.signal.aborted) {
        setState({ status: 'error', error: presentAiError(error) })
        controller.abort()
      }
    } finally {
      if (pending.current === controller) pending.current = null
    }
  }

  function cancel() {
    pending.current?.abort()
    pending.current = null
    setState({ status: 'idle' })
    setCanceled(true)
    input.current?.focus()
  }

  return <section className="size-guide-assistant" id="size-assistant" aria-labelledby={`${id}-heading`}>
    <div className="size-guide-assistant-heading"><span className="size-guide-icon"><Bot size={24} aria-hidden="true" /></span><h2 id={`${id}-heading`}>Tell us what you need to store</h2><p className="muted"><strong>Optional AI Sizing Assistant.</strong> Describe your belongings for a storage type suggestion. Manual selection is always available.</p></div>
    <div className="size-guide-assistant-message"><strong>{mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'} suggestions</strong><p>Suggestions use the storage type selected above. AI does not reserve a unit or confirm availability, fit or a final rental price.</p></div>
    <AiStoragePolicyPanel />
    <form onSubmit={submit} noValidate>
      <div className="size-guide-assistant-controls"><div className="form-field"><label htmlFor={`${id}-description`}>Items you want to store</label><div className="size-guide-assistant-input"><MessageSquare size={18} aria-hidden="true" /><textarea ref={input} id={`${id}-description`} value={description} rows={3} maxLength={AI_DESCRIPTION_MAX_LENGTH} readOnly={loading} disabled={!enabled} required aria-invalid={Boolean(validation)} aria-describedby={`${id}-help ${id}-count${validation ? ` ${id}-error` : ''}`} placeholder="For example: moving boxes, books and two small shelves." onChange={event => { setDescription(event.target.value); setValidation(null); setCanceled(false); setState({ status: 'idle' }) }} /></div><div className="size-guide-ai-input-help"><small id={`${id}-help`}>Only this description and your storage preference are submitted. Do not include personal or sensitive information.</small><small id={`${id}-count`}>{description.length.toLocaleString('en-US')} / 1,000 characters</small></div><CustomerFieldError id={`${id}-error`} message={validation} /></div><button className="button" type="submit" disabled={!enabled || loading}>{loading ? 'Finding a Storage Type…' : 'Get AI Recommendation'}</button></div>
      {loading && <div className="size-guide-ai-progress"><p role="status">Getting your suggestion and checking the current catalog… You can keep browsing while you wait.</p><button className="button button-secondary" type="button" onClick={cancel}>Cancel Request</button></div>}
    </form>
    {canceled && <p className="muted" role="status">Request canceled. You can edit your description or choose a storage type manually.</p>}
    {state.status === 'error' && <div className="size-guide-ai-feedback"><ApiErrorAlert error={state.error} /><p className="muted">Your description has been kept. Submit again when you are ready, or compare storage types manually.</p></div>}
    {state.status === 'success' && <div className="size-guide-ai-results">
      <p className="size-guide-ai-request"><strong>Your request</strong><span>{state.description}</span></p>
      <div role="status"><h3>Here is a storage type to consider</h3><p className="muted">This is guidance, not a guarantee. Choose a facility and rental months to check availability and current terms.</p></div>
      <RecommendationCard candidate={state.result.recommended} />
      {state.result.alternatives.length > 0 && <><h3>Other types to compare</h3><div className="size-guide-ai-alternatives">{state.result.alternatives.map(candidate => <RecommendationCard key={candidate.unitType.unitTypeId} candidate={candidate} alternative />)}</div></>}
    </div>}
    <div className="size-guide-assistant-links"><a href="#storage-sizes">Compare Storage Types</a><Link to="/facilities">Continue to Find Storage →</Link></div>
  </section>
}
