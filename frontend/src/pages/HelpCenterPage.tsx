import { useEffect } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ArrowRight, MessageSquare } from 'lucide-react'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { PublicPolicyCard } from '../components/PublicPolicyCard'
import { PublicHelpContact } from '../components/PublicHelpContact'
import { PUBLIC_POLICY_TOPICS } from '../features/public-help/publicPolicy.types'
import { usePublicPolicies } from '../features/public-help/usePublicPolicies'
import '../styles/public-help.css'

export function HelpCenterPage() {
  const { user } = useAuth()
  const { hash } = useLocation()
  const policies = usePublicPolicies()
  useEffect(() => { if (hash === '#contact') document.getElementById('contact')?.scrollIntoView({ block: 'start', behavior: 'instant' }) }, [hash, policies.loading])
  return <main className="page-container public-help-center">
    <section className="help-hero"><div className="gwp-container"><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">Help Center</span></nav><div className="help-hero-copy"><p className="eyebrow">FStoRent Help Center</p><h1>Help Center</h1><p>Read public policies and find the information you need before reserving storage.</p></div></div></section>
    <section className="help-policies" aria-labelledby="help-policies-heading"><div className="gwp-container">
      <div className="help-section-heading"><p className="eyebrow">Public Information</p><h2 id="help-policies-heading">System Policies</h2><p>Published articles describe the current public policy for new rentals.</p></div>
      <ApiErrorAlert error={policies.error} />
      {policies.error && <button className="button button-secondary" type="button" onClick={policies.refresh}>Retry Public Policies</button>}
      {policies.loading ? <LoadingState label="Loading current public policies…" /> : policies.data && <>
        {policies.data.data.length === 0 && <EmptyState message="No public policy articles are currently published. Please check again later." />}
        <div className="help-policy-grid">{PUBLIC_POLICY_TOPICS.map(topic => <PublicPolicyCard key={topic.key} topic={topic} policy={policies.data!.data.find(item => item.policyKey === topic.key)} />)}</div>
        <p className="help-snapshot-note">For an existing reservation or contract, the policy captured for that rental still applies. Check your own rental details after signing in; these public articles do not replace your captured terms.</p>
      </>}
    </div></section>
    <PublicHelpContact />
    <section className="help-support-section"><div className="gwp-container"><div className="help-support-banner"><span className="help-support-icon"><MessageSquare size={22} aria-hidden="true" /></span><div><h2>Need help with an existing contract?</h2><p>Support tickets are linked to your contract. Sign in to view your requests or create a request for an eligible contract.</p></div><Link className="button" to="/customer/support-tickets">{user?.role === 'CUSTOMER' ? 'Open Contract Support' : 'Sign In for Contract Support'}<ArrowRight size={16} aria-hidden="true" /></Link></div><p className="help-support-note">Public browsing does not create a support ticket or change your rental.</p></div></section>
  </main>
}
