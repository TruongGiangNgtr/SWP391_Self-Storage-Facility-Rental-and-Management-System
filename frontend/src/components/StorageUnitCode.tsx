import './StorageUnitCode.css'

export interface StorageUnitCodeProps {
  code?: string | null
  className?: string
  badge?: boolean
  /** @deprecated Unit codes are always static; animation options are ignored. */
  speed?: number
  /** @deprecated Unit codes are always static; animation options are ignored. */
  maxIterations?: number
  /** @deprecated Unit codes are always static; animation options are ignored. */
  sequential?: boolean
}

export function StorageUnitCode({
  code,
  className = '',
  badge = true,
}: StorageUnitCodeProps) {
  if (!code || !code.trim()) {
    return <span className="muted">—</span>
  }

  const cleanCode = code.trim()

  return (
    <span
      className={`storage-unit-code${badge ? ' storage-unit-code--badge' : ''} ${className}`.trim()}
      title={`Storage Unit: ${cleanCode}`}
    >
      {cleanCode}
    </span>
  )
}

export default StorageUnitCode
