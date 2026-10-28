import { ArrowRight } from 'lucide-react'
import { Link } from 'react-router-dom'

export function StorageReady({ guide = false }: { guide?: boolean }) {
  return <section className="storage-ready"><div className="gwp-container storage-ready-inner">
    <div><h2>Ready to find your storage?</h2><p>Compare locations and storage types for your rental period.</p></div>
    <div className="storage-ready-actions"><Link className="button" to="/facilities">Find Your Storage<ArrowRight size={16} aria-hidden="true" /></Link>
      {guide ? <a href="#how-journey-heading">Learn How It Works<ArrowRight size={16} aria-hidden="true" /></a> : <Link to="/how-it-works">Learn How It Works<ArrowRight size={16} aria-hidden="true" /></Link>}
    </div>
  </div></section>
}
