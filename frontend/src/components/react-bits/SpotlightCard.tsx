// Adapted from React Bits' TypeScript/CSS SpotlightCard (David Haz).
// https://github.com/DavidHDev/react-bits/blob/main/src/ts-default/Components/SpotlightCard/SpotlightCard.tsx
// See LICENSE.md.
import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type FocusEvent,
  type HTMLAttributes,
  type MouseEvent,
  type ReactNode,
} from 'react'
import './SpotlightCard.css'

export interface SpotlightCardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
  className?: string
  spotlightColor?: string
  spotlightRadius?: number
  style?: CSSProperties
}

export function SpotlightCard({
  children,
  className = '',
  spotlightColor = 'rgba(0, 242, 254, 0.15)',
  spotlightRadius = 350,
  style,
  onMouseMove,
  onMouseEnter,
  onMouseLeave,
  onFocus,
  onBlur,
  ...rest
}: SpotlightCardProps) {
  const divRef = useRef<HTMLDivElement>(null)
  const [position, setPosition] = useState({ x: 0, y: 0 })
  const [opacity, setOpacity] = useState(0)
  const [reducedMotion, setReducedMotion] = useState(false)

  useEffect(() => {
    const media = window.matchMedia('(prefers-reduced-motion: reduce)')
    setReducedMotion(media.matches)
    const listener = (event: MediaQueryListEvent) => setReducedMotion(event.matches)
    media.addEventListener('change', listener)
    return () => media.removeEventListener('change', listener)
  }, [])

  const handleMouseMove = (event: MouseEvent<HTMLDivElement>) => {
    if (!divRef.current || reducedMotion) return
    const rect = divRef.current.getBoundingClientRect()
    setPosition({
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    })
    onMouseMove?.(event)
  }

  const handleMouseEnter = (event: MouseEvent<HTMLDivElement>) => {
    if (!reducedMotion) setOpacity(1)
    onMouseEnter?.(event)
  }

  const handleMouseLeave = (event: MouseEvent<HTMLDivElement>) => {
    setOpacity(0)
    onMouseLeave?.(event)
  }

  const handleFocus = (event: FocusEvent<HTMLDivElement>) => {
    if (!reducedMotion) setOpacity(1)
    onFocus?.(event)
  }

  const handleBlur = (event: FocusEvent<HTMLDivElement>) => {
    setOpacity(0)
    onBlur?.(event)
  }

  return (
    <div
      ref={divRef}
      className={`react-bits-spotlight-card ${className}`.trim()}
      style={style}
      onMouseMove={handleMouseMove}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      onFocus={handleFocus}
      onBlur={handleBlur}
      {...rest}
    >
      <div
        className="react-bits-spotlight-overlay"
        aria-hidden="true"
        style={{
          opacity,
          background: reducedMotion
            ? 'none'
            : `radial-gradient(${spotlightRadius}px circle at ${position.x}px ${position.y}px, ${spotlightColor}, transparent 80%)`,
        }}
      />
      {children}
    </div>
  )
}

export default SpotlightCard
