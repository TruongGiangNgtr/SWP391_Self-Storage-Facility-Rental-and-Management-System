// Adapted from React Bits' TypeScript/CSS ShinyText (David Haz).
// https://github.com/DavidHDev/react-bits/blob/main/src/ts-default/TextAnimations/ShinyText/ShinyText.tsx
// See LICENSE.md.
import type { CSSProperties, ReactNode } from 'react'
import './ShinyText.css'

export interface ShinyTextProps {
  text?: string
  children?: ReactNode
  disabled?: boolean
  speed?: number
  className?: string
  style?: CSSProperties
}

export function ShinyText({
  text,
  children,
  disabled = false,
  speed = 3.5,
  className = '',
  style,
}: ShinyTextProps) {
  const content = text ?? children

  return (
    <span
      className={`shiny-text ${disabled ? 'disabled' : ''} ${className}`.trim()}
      style={{
        animationDuration: `${speed}s`,
        ...style,
      }}
    >
      {content}
    </span>
  )
}

export default ShinyText
