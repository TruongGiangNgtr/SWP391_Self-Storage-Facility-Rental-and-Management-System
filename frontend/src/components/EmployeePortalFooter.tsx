import { Link } from 'react-router-dom'
import { BrandLogo } from './BrandLogo'
import '../styles/employeePortalFooter.css'

interface FooterLink {
  to: string
  label: string
}

interface FooterSection {
  title: string
  links: readonly FooterLink[]
}

interface EmployeePortalFooterProps {
  variant: 'manager' | 'staff'
  portalName: string
  homePath: string
  displayName: string
  description: string
  trustMessage: string
  sections: readonly [FooterSection, FooterSection]
  onSignOut: () => void
}

/** Shared employee-portal footer: Staff and Manager use the same layout and responsive rules. */
export function EmployeePortalFooter({
  variant,
  portalName,
  homePath,
  displayName,
  description,
  trustMessage,
  sections,
  onSignOut,
}: EmployeePortalFooterProps) {
  return (
    <footer className={`${variant}-footer employee-footer`}>
      <div className={`${variant}-footer-main employee-footer-main`}>
        <div className={`${variant}-footer-brand employee-footer-brand`}>
          <BrandLogo inverse />
          <p>{description}</p>
          <span className={`${variant}-footer-trust employee-footer-trust`}>{trustMessage}</span>
        </div>

        {sections.map((section) => (
          <div className={`${variant}-footer-column employee-footer-column`} key={section.title}>
            <strong>{section.title}</strong>
            {section.links.map((link) => (
              <Link
                className={`${variant}-footer-link employee-footer-link`}
                key={link.to}
                to={link.to}
              >
                {link.label}
              </Link>
            ))}
          </div>
        ))}

        <div className={`${variant}-footer-column employee-footer-column`}>
          <strong>ACCOUNT</strong>
          <span>{displayName}</span>
          <span>{portalName}</span>
          <button
            type="button"
            className={`${variant}-footer-logout employee-footer-logout`}
            onClick={onSignOut}
          >
            Sign Out
          </button>
        </div>
      </div>

      <div className={`${variant}-footer-bottom employee-footer-bottom`}>
        <span>© 2026 FStoRent. All rights reserved.</span>
        <div>
          <span>Privacy Policy</span>
          <span>Terms of Service</span>
          <Link
            className={`${variant}-footer-bottom-link employee-footer-bottom-link`}
            to={homePath}
          >
            {portalName} Portal
          </Link>
        </div>
      </div>
    </footer>
  )
}
