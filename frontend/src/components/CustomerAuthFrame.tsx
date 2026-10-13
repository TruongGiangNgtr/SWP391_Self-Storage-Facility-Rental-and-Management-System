import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

export function CustomerAuthFrame({ register = false, children }: { register?: boolean; children: ReactNode }) {
  return <main className="page-container customer-auth-page customer-auth-feature"><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">{register ? 'Customer Registration' : 'Sign In'}</span></nav><section className="auth-card"><div className="customer-auth-heading"><p className="eyebrow">{register ? 'Customer Registration' : 'Customer Access'}</p><h1>{register ? 'Create your customer account' : 'Sign in to your account'}</h1><p className="muted">{register ? 'Register to reserve storage, manage your rentals and access support.' : 'Manage your reservations, visits, contracts and support requests.'}</p></div>{children}</section></main>
}
