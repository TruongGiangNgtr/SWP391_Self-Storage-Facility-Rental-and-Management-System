import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight } from 'lucide-react'

export function CustomerAuthFrame({ register = false, employee = false, children }: { register?: boolean; employee?: boolean; children: ReactNode }) {
  if (employee) {
  return <main className="page-container customer-auth-page customer-auth-feature"><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">{register ? 'Customer Registration' : employee ? 'Employee Sign In' : 'Sign In'}</span></nav><section className="auth-card"><div className="customer-auth-heading"><p className="eyebrow">{register ? 'Customer Registration' : employee ? 'Employee Access' : 'Customer Access'}</p><h1>{register ? 'Create your customer account' : employee ? 'Employee Sign In' : 'Sign in to your account'}</h1><p className="muted">{register ? 'Register to reserve storage, manage your rentals and access support.' : employee ? 'For Staff, Manager, Business Operations and Administrator accounts.' : 'Manage your reservations, visits, contracts and support requests.'}</p></div>{children}</section></main>
  }
  return <main className={`page-container customer-auth-page customer-auth-feature${register ? '' : ' customer-sign-in-scene'}`}>
    <nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">{register ? 'Customer Registration' : employee ? 'Employee Sign In' : 'Sign In'}</span></nav>
    <div className="support-auth-layout">
      <aside className="support-auth-story">
        <div><h2>{employee ? 'Your facility workspace.' : 'Space for what matters.'}</h2><p>{employee ? 'Access the tools for your assigned role and facility.' : 'Find, reserve and manage your self-storage with FStoRent.'}</p></div>
        <img src="/images/how-it-works/fstorent-reception.webp" alt="" width={1672} height={941} decoding="async" />
        <Link to={employee ? '/auth/customer/login' : '/how-it-works'}>{employee ? 'Looking for Customer Sign In?' : 'See how storage rental works'}<ArrowRight size={18} aria-hidden="true" /></Link>
      </aside>
      <section className="auth-card"><div className="customer-auth-heading"><h1>{register ? 'Create your customer account' : employee ? 'Employee Sign In' : 'Sign in to your account'}</h1><p className="muted">{register ? 'Register to reserve storage, manage your rentals and access support.' : employee ? 'For Staff, Manager, Business Operations and Administrator accounts.' : 'Manage your reservations, visits, contracts and support requests.'}</p></div>{children}</section>
    </div>
  </main>
}
