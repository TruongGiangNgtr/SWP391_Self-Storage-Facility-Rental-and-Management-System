import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

export function CustomerAuthFrame({ register = false, employee = false, children }: { register?: boolean; employee?: boolean; children: ReactNode }) {
  return <main className="page-container customer-auth-page customer-auth-feature"><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">{register ? 'Customer Registration' : employee ? 'Employee Sign In' : 'Sign In'}</span></nav><section className="auth-card"><div className="customer-auth-heading"><p className="eyebrow">{register ? 'Customer Registration' : employee ? 'Employee Access' : 'Customer Access'}</p><h1>{register ? 'Create your customer account' : employee ? 'Employee Sign In' : 'Sign in to your account'}</h1><p className="muted">{register ? 'Register to reserve storage, manage your rentals and access support.' : employee ? 'For Staff, Manager, Business Operations and Administrator accounts.' : 'Manage your reservations, visits, contracts and support requests.'}</p></div>{children}</section></main>
}
