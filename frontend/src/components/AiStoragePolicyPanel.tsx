import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { aiRecommendationApi, type AiStoragePolicyView } from '../api/aiRecommendationApi'

export function AiStoragePolicyPanel() {
  const [policy, setPolicy] = useState<AiStoragePolicyView | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [copyStatus, setCopyStatus] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    aiRecommendationApi.storagePolicy(controller.signal)
      .then(result => { if (!controller.signal.aborted) setPolicy(result.data) })
      .catch(() => { if (!controller.signal.aborted) setLoadError(true) })
    return () => controller.abort()
  }, [])

  const text = policy ? [
    policy.title,
    policy.summary,
    `Approval status: ${policy.approvalStatus}`,
    'PROHIBITED',
    ...policy.prohibited.map(x => `${x.title}: ${x.examples.join(', ')}`),
    'ALLOWED',
    ...policy.allowed.map(x => `${x.title}: ${x.examples.join(', ')}`),
    'References',
    ...policy.legalReferences.map(x => `${x.documentNumber} - ${x.title}: ${x.officialUrl}`),
  ].join('\n') : ''

  async function copyPolicy() {
    if (!policy) return
    try {
      await navigator.clipboard.writeText(text)
      setCopyStatus('Copied policy preview.')
    } catch {
      setCopyStatus('Could not copy. Select the text above manually.')
    }
  }

  return <aside className="size-guide-assistant-message" aria-label="Storage item acceptance rules">
    <strong>Storage item acceptance rules</strong>
    {loadError ? <p>Could not load the storage rules. Please review the published policy before requesting a recommendation.</p>
      : !policy ? <p>Loading prohibited and allowed item groups…</p> : <>
      <p>{policy.summary}</p>
      {policy.approvalStatus !== 'APPROVED' && <p role="note"><strong>Draft for review:</strong> these are proposed FRMS acceptance rules, not an officially approved or exhaustive legal list.</p>}
      <details>
        <summary>View PROHIBITED / ALLOWED lists</summary>
        <h4>PROHIBITED — not accepted for storage</h4>
        <ul>{policy.prohibited.map(item => <li key={item.id}><strong>{item.title}</strong>: {item.examples.join(', ')}</li>)}</ul>
        <h4>ALLOWED — subject to safe storage conditions</h4>
        <ul>{policy.allowed.map(item => <li key={item.id}><strong>{item.title}</strong>: {item.examples.join(', ')}</li>)}</ul>
        <p>Legal references are included in copied text. See the separately published policy for approved terms.</p>
      </details>
      <button type="button" className="button button-secondary" onClick={() => void copyPolicy()}>Copy policy preview</button>
      {copyStatus && <p role="status">{copyStatus}</p>}
    </>}
    {policy?.approvalStatus === 'APPROVED'
      ? <p><Link to="/help/policies/facility-rules-prohibited-items">View published Facility Rules &amp; Prohibited Items Policy</Link></p>
      : <p>Official policy publication is pending legal and business approval.</p>}
  </aside>
}
