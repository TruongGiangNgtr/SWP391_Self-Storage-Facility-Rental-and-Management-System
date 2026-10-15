import { NavLink, useLocation } from 'react-router-dom'
import { DiscountManagementPage } from './DiscountManagementPage'
import { ExtraFeeManagementPage } from './ExtraFeeManagementPage'
import './discount-and-fees.css'

export function DiscountAndExtraFeePage() {
  const { pathname } = useLocation()
  return <section className="facility-page">
    <div className="facility-breadcrumb">Business Operations / Discount &amp; Extra Fee</div>
    <div className="facility-page-heading"><div>
      <span className="facility-eyebrow">BUSINESS CONFIGURATION</span>
      <h1>Discount &amp; Extra Fee</h1>
      <p>Manage customer discounts and extra fee configurations.</p>
    </div></div>
    <nav className="discount-fee-tabs" aria-label="Discount and fee sections">
      <NavLink to="/business/discounts">Discounts</NavLink>
      <NavLink to="/business/extra-fees">Extra Fees</NavLink>
    </nav>
    {pathname.endsWith('/extra-fees') ? <ExtraFeeManagementPage /> : <DiscountManagementPage />}
  </section>
}
