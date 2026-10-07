interface BrandLogoProps {
  inverse?: boolean
  size?: 'small' | 'normal'
}

export function BrandLogo({
  inverse = false,
  size = 'normal',
}: BrandLogoProps) {
  return (
    <div
      className={[
        'brand-logo',
        inverse ? 'brand-logo--inverse' : '',
        size === 'small' ? 'brand-logo--small' : '',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <span className="brand-logo__mark">
        <svg
          viewBox="0 0 32 32"
          aria-hidden="true"
          className="brand-logo__cube"
        >
          <path
            d="M16 6.5 24 11v10l-8 4.5L8 21V11l8-4.5Z"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinejoin="round"
          />

          <path
            d="m8 11 8 4.5 8-4.5M16 15.5v10"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinejoin="round"
          />
        </svg>
      </span>

      <span className="brand-logo__wordmark">
        <span className="brand-logo__prefix">FSto</span>
        <span className="brand-logo__suffix">Rent</span>
      </span>
    </div>
  )
}