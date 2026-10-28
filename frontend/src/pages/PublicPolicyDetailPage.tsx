import { useEffect } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { PublicPolicyMetadata } from '../components/PublicPolicyMetadata'
import { PUBLIC_POLICY_TOPICS } from '../features/public-help/publicPolicy.types'
import { isPublicPolicyKey } from '../features/public-help/publicPolicyValidation'
import { usePublicPolicy } from '../features/public-help/usePublicPolicies'
import '../styles/public-help.css'

export function PublicPolicyDetailPage() {
  const { policyKey = '' } = useParams()
  const { hash } = useLocation()
  const valid = isPublicPolicyKey(policyKey)
  const policy = usePublicPolicy(policyKey)
  const article = policy.data?.data
  const topic = PUBLIC_POLICY_TOPICS.find(item => item.key === policyKey)
  useEffect(() => {
    if (article && /^#policy-section-\d+$/.test(hash)) {
      const section = document.getElementById(hash.slice(1))
      section?.scrollIntoView({ block: 'start', behavior: 'instant' }); section?.focus({ preventScroll: true })
    }
  }, [article, hash])
  return <main className="page-container public-policy-detail">
    <nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><Link to="/help">Help Center</Link><span aria-hidden="true">/</span><span aria-current="page">{article?.title ?? topic?.label ?? 'Policy'}</span></nav>
    <Link className="help-back-link" to="/help"><ArrowLeft size={16} aria-hidden="true" />Back to Help Center</Link>
    {!valid ? <section className="panel stack"><h1>Policy Topic Not Found</h1><p>This is not a public policy topic. Return to Help Center to choose a policy.</p></section> : <>
      {!article && <h1>{topic?.label}</h1>}
      <ApiErrorAlert error={policy.error} />
      {policy.error && <div className="action-row"><button className="button button-secondary" type="button" onClick={policy.refresh}>Retry Policy</button><Link className="button button-secondary" to="/help#contact">Contact a Facility</Link></div>}
      {policy.loading ? <LoadingState label="Loading the current public policy…" /> : article && <>
        <header className="help-article-heading"><h1 id="policy-article-heading">{article.title}</h1><p className="help-article-summary">{article.summary}</p><PublicPolicyMetadata policy={article} /></header>
        <div className="help-article-grid"><nav className="help-contents" aria-label="On this page"><h2>On this page</h2><ol>{article.sections.map((section, index) => <li key={index}><Link to={`/help/policies/${policyKey}#policy-section-${index + 1}`}>{section.heading}</Link></li>)}</ol></nav>
          <article className="help-article" aria-labelledby="policy-article-heading">{article.sections.map((section, index) => <section key={index} aria-labelledby={`policy-section-${index + 1}`}><h2 id={`policy-section-${index + 1}`} tabIndex={-1}>{section.heading}</h2><p className="help-plain-text">{section.body}</p></section>)}</article>
        </div>
        <aside className="help-existing-terms"><h2>Already renting with FStoRent?</h2><p>This article describes the current public policy for new rentals. Existing reservations and contracts continue using their captured policy. Sign in and check your rental details for the terms that apply to you.</p><Link className="help-read-link" to="/customer/contracts">View My Contracts</Link></aside>
        <div className="help-article-actions"><Link className="button button-secondary" to="/help">All Public Policies</Link><Link className="button button-secondary" to="/help#contact">Contact a Facility</Link></div>
      </>}
    </>}
  </main>
}
