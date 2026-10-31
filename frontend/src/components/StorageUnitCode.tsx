import { DecryptedText } from './react-bits/DecryptedText'
import './react-bits/DecryptedText.css'

export interface StorageUnitCodeProps {
  code?: string | null
  className?: string
  badge?: boolean
  speed?: number
  maxIterations?: number
  sequential?: boolean
}

export function StorageUnitCode({
  code,
  className = '',
  badge = true,
  speed = 35,
  maxIterations = 8,
  sequential = true,
}: StorageUnitCodeProps) {
  if (!code || !code.trim()) {
    return <span className="muted">—</span>
  }

  const cleanCode = code.trim()

  return (
    <span
      className={`${badge ? 'storage-unit-code-badge' : 'storage-unit-code-text'} ${className}`.trim()}
      title={`Storage Unit: ${cleanCode}`}
    >
      <DecryptedText
        text={cleanCode}
        speed={speed}
        maxIterations={maxIterations}
        sequential={sequential}
        animateOn="view"
      />
    </span>
  )
}

export default StorageUnitCode
