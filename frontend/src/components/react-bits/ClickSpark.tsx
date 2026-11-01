// Adapted from React Bits' TypeScript/CSS ClickSpark (David Haz).
// https://github.com/DavidHDev/react-bits/blob/main/Animations/ClickSpark/ClickSpark.tsx
// See LICENSE.md.
import {
  forwardRef,
  useCallback,
  useEffect,
  useImperativeHandle,
  useRef,
  useState,
  type CSSProperties,
  type HTMLAttributes,
  type MouseEvent,
  type ReactNode,
} from 'react'
import './ClickSpark.css'

export type ClickSparkEasing = 'linear' | 'ease-in' | 'ease-out' | 'ease-in-out'

export interface ClickSparkHandle {
  spark: (x?: number, y?: number) => void
}

export interface ClickSparkProps extends Omit<HTMLAttributes<HTMLDivElement>, 'children'> {
  children?: ReactNode
  sparkColor?: string | string[]
  sparkSize?: number
  sparkRadius?: number
  sparkCount?: number
  duration?: number
  easing?: ClickSparkEasing
  extraScale?: number
  ring?: boolean
  overflowPadding?: number
  block?: boolean
  className?: string
  style?: CSSProperties
}

interface Spark {
  x: number
  y: number
  angle: number
  startTime: number
  color: string
}

interface SparkRing {
  x: number
  y: number
  startTime: number
  color: string
}

const easeFuncs: Record<ClickSparkEasing, (t: number) => number> = {
  linear: (t) => t,
  'ease-in': (t) => t * t,
  'ease-out': (t) => t * (2 - t),
  'ease-in-out': (t) => (t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t),
}

export const ClickSpark = forwardRef<ClickSparkHandle, ClickSparkProps>(function ClickSpark(
  {
    children,
    sparkColor = '#38bdf8',
    sparkSize = 10,
    sparkRadius = 18,
    sparkCount = 8,
    duration = 400,
    easing = 'ease-out',
    extraScale = 1.0,
    ring = false,
    overflowPadding,
    block = false,
    className = '',
    style,
    onClick,
    ...rest
  },
  ref
) {
  const containerRef = useRef<HTMLDivElement>(null)
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const sparksRef = useRef<Spark[]>([])
  const ringsRef = useRef<SparkRing[]>([])
  const isAnimatingRef = useRef(false)
  const animationFrameIdRef = useRef<number | null>(null)
  const [reducedMotion, setReducedMotion] = useState(false)

  const padding = overflowPadding ?? Math.max(20, Math.round(sparkRadius * extraScale + sparkSize + 6))

  useEffect(() => {
    const media = window.matchMedia('(prefers-reduced-motion: reduce)')
    setReducedMotion(media.matches)
    const listener = (event: MediaQueryListEvent) => setReducedMotion(event.matches)
    media.addEventListener('change', listener)
    return () => media.removeEventListener('change', listener)
  }, [])

  const animate = useCallback(() => {
    const canvas = canvasRef.current
    if (!canvas) {
      isAnimatingRef.current = false
      return
    }

    const ctx = canvas.getContext('2d')
    if (!ctx) {
      isAnimatingRef.current = false
      return
    }

    const dpr = window.devicePixelRatio || 1
    const width = canvas.width / dpr
    const height = canvas.height / dpr

    ctx.clearRect(0, 0, width, height)

    const now = performance.now()
    const ease = easeFuncs[easing] || easeFuncs['ease-out']

    // Filter and draw active sparks
    sparksRef.current = sparksRef.current.filter((spark) => {
      const elapsed = now - spark.startTime
      if (elapsed >= duration) {
        return false
      }

      const progress = elapsed / duration
      const eased = ease(progress)
      const distance = eased * sparkRadius * extraScale
      const lineLength = sparkSize * (1 - eased)

      const x1 = spark.x + distance * Math.cos(spark.angle)
      const y1 = spark.y + distance * Math.sin(spark.angle)
      const x2 = spark.x + (distance + lineLength) * Math.cos(spark.angle)
      const y2 = spark.y + (distance + lineLength) * Math.sin(spark.angle)

      const alpha = Math.max(0, 1 - progress)

      ctx.save()
      ctx.beginPath()
      ctx.moveTo(x1, y1)
      ctx.lineTo(x2, y2)
      ctx.strokeStyle = spark.color
      ctx.globalAlpha = alpha
      ctx.lineWidth = 2
      ctx.lineCap = 'round'
      ctx.shadowColor = spark.color
      ctx.shadowBlur = 4
      ctx.stroke()
      ctx.restore()

      return true
    })

    // Filter and draw rings
    if (ring) {
      ringsRef.current = ringsRef.current.filter((item) => {
        const elapsed = now - item.startTime
        if (elapsed >= duration) {
          return false
        }

        const progress = elapsed / duration
        const eased = ease(progress)
        const currentRadius = eased * sparkRadius * extraScale * 0.95
        const alpha = Math.max(0, 1 - progress) * 0.45

        ctx.save()
        ctx.beginPath()
        ctx.arc(item.x, item.y, Math.max(0.1, currentRadius), 0, Math.PI * 2)
        ctx.strokeStyle = item.color
        ctx.globalAlpha = alpha
        ctx.lineWidth = 1.5
        ctx.stroke()
        ctx.restore()

        return true
      })
    }

    if (sparksRef.current.length > 0 || (ring && ringsRef.current.length > 0)) {
      animationFrameIdRef.current = requestAnimationFrame(animate)
    } else {
      isAnimatingRef.current = false
      ctx.clearRect(0, 0, width, height)
    }
  }, [duration, easing, extraScale, ring, sparkRadius, sparkSize])

  const burst = useCallback(
    (originX: number, originY: number) => {
      if (reducedMotion) return

      const colors = Array.isArray(sparkColor) ? sparkColor : [sparkColor]
      const now = performance.now()
      const newSparks: Spark[] = []

      for (let i = 0; i < sparkCount; i++) {
        const baseAngle = (i * 2 * Math.PI) / sparkCount
        const jitter = (Math.random() - 0.5) * (Math.PI / sparkCount) * 0.4
        const angle = baseAngle + jitter
        const color = colors[Math.floor(Math.random() * colors.length)]

        newSparks.push({
          x: originX,
          y: originY,
          angle,
          startTime: now,
          color,
        })
      }

      sparksRef.current.push(...newSparks)

      if (ring) {
        const ringColor = colors[0] ?? '#38bdf8'
        ringsRef.current.push({
          x: originX,
          y: originY,
          startTime: now,
          color: ringColor,
        })
      }

      if (!isAnimatingRef.current) {
        isAnimatingRef.current = true
        animationFrameIdRef.current = requestAnimationFrame(animate)
      }
    },
    [animate, reducedMotion, ring, sparkColor, sparkCount]
  )

  useImperativeHandle(
    ref,
    () => ({
      spark: (x, y) => {
        const canvas = canvasRef.current
        if (!canvas) return
        const rect = canvas.getBoundingClientRect()
        const sparkX = typeof x === 'number' ? x : rect.width / 2
        const sparkY = typeof y === 'number' ? y : rect.height / 2
        burst(sparkX, sparkY)
      },
    }),
    [burst]
  )

  useEffect(() => {
    const container = containerRef.current
    const canvas = canvasRef.current
    if (!container || !canvas) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    const updateDimensions = () => {
      const rect = container.getBoundingClientRect()
      const dpr = window.devicePixelRatio || 1
      const width = rect.width + padding * 2
      const height = rect.height + padding * 2

      canvas.width = Math.max(1, Math.round(width * dpr))
      canvas.height = Math.max(1, Math.round(height * dpr))
      canvas.style.width = `${width}px`
      canvas.style.height = `${height}px`
      canvas.style.top = `-${padding}px`
      canvas.style.left = `-${padding}px`

      ctx.setTransform(dpr, 0, 0, dpr, 0, 0)
    }

    updateDimensions()
    const resizeObserver = new ResizeObserver(updateDimensions)
    resizeObserver.observe(container)

    return () => {
      resizeObserver.disconnect()
      if (animationFrameIdRef.current) {
        cancelAnimationFrame(animationFrameIdRef.current)
      }
    }
  }, [padding])

  const handleClick = (event: MouseEvent<HTMLDivElement>) => {
    onClick?.(event)

    if (reducedMotion) return

    // Do not trigger sparks when clicking on an explicitly disabled button
    const target = event.target as HTMLElement | null
    const btn = target?.closest('button')
    if (btn?.disabled) {
      return
    }

    const canvas = canvasRef.current
    if (!canvas) return

    const rect = canvas.getBoundingClientRect()
    let clickX = event.clientX - rect.left
    let clickY = event.clientY - rect.top

    // Synthetic keyboard click fallback (detail === 0 or coordinates 0,0)
    if (event.detail === 0 || (event.clientX === 0 && event.clientY === 0)) {
      const targetRect = (btn || containerRef.current)?.getBoundingClientRect()
      if (targetRect) {
        clickX = targetRect.left + targetRect.width / 2 - rect.left
        clickY = targetRect.top + targetRect.height / 2 - rect.top
      } else {
        const dpr = window.devicePixelRatio || 1
        clickX = canvas.width / (2 * dpr)
        clickY = canvas.height / (2 * dpr)
      }
    }

    burst(clickX, clickY)
  }

  const wrapperClass = `react-bits-click-spark ${block ? 'react-bits-click-spark--block' : ''} ${className}`.trim()

  return (
    <div
      ref={containerRef}
      className={wrapperClass}
      style={style}
      onClick={handleClick}
      {...rest}
    >
      <canvas ref={canvasRef} className="react-bits-click-spark-canvas" aria-hidden="true" />
      {children}
    </div>
  )
})

export default ClickSpark
