import { Bot, MessageSquare } from 'lucide-react'
import { Link } from 'react-router-dom'

// Owner decision: keep AI visible but do not enable/send AI-001 until BE has
// documented the description limit. No fake answers or retained conversation.
export function SizeGuideAssistantPreview() {
  return <section className="size-guide-assistant" id="size-assistant" aria-labelledby="size-assistant-heading">
    <div className="size-guide-assistant-heading"><span className="size-guide-icon"><Bot size={24} aria-hidden="true" /></span><p className="eyebrow">Optional AI Sizing Assistant</p><h2 id="size-assistant-heading">Tell us what you need to store</h2><p className="muted">AI sizing suggestions will help you compare storage types. Manual selection is always available.</p></div>
    <div className="size-guide-assistant-message" role="status"><strong>Not Available Yet</strong><p id="size-assistant-status">This feature is not available yet. Please try again later.</p><p className="muted">No description is sent and no recommendation is generated. You can still compare the storage types above.</p></div>
    <div className="size-guide-assistant-controls"><div className="form-field"><label htmlFor="size-assistant-description">Items you want to store</label><div className="size-guide-assistant-input"><MessageSquare size={18} aria-hidden="true" /><textarea id="size-assistant-description" disabled rows={2} aria-describedby="size-assistant-status" placeholder="AI sizing suggestions are not available yet." /></div></div><button className="button" type="button" disabled>Get AI Recommendation</button></div>
    <div className="size-guide-assistant-links"><a href="#storage-sizes">Compare Storage Types</a><Link to="/facilities">Continue to Find Storage →</Link></div>
  </section>
}
