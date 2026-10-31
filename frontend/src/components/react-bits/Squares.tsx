// Adapted from React Bits' TypeScript/CSS Squares (David Haz).
// https://github.com/DavidHDev/react-bits/blob/main/src/ts-default/Backgrounds/Squares/Squares.tsx
// See LICENSE.md.
import { useEffect, useRef, useState, type CSSProperties } from 'react'
import './Squares.css'

export interface SquaresProps {
  direction?: 'diagonal' | 'up' | 'right' | 'down' | 'left'
  speed?: number
  borderColor?: string
  squareSize?: number
  hoverFillColor?: string
  className?: string
  style?: CSSProperties
}

export function Squares({
  direction = 'diagonal',
  speed = 0.5,
  borderColor = 'rgba(56, 189, 248, 0.1)',
  squareSize = 40,
  hoverFillColor = 'rgba(56, 189, 248, 0.18)',
  className = '',
  style,
}: SquaresProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const hoveredSquareRef = useRef<{ x: number; y: number } | null>(null)
  const [reducedMotion, setReducedMotion] = useState(false)

  useEffect(() => {
    const media = window.matchMedia('(prefers-reduced-motion: reduce)')
    setReducedMotion(media.matches)
    const listener = (event: MediaQueryListEvent) => setReducedMotion(event.matches)
    media.addEventListener('change', listener)
    return () => media.removeEventListener('change', listener)
  }, [])

  useEffect(() => {
    const canvas = canvasRef.current
    const container = containerRef.current
    if (!canvas || !container) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    let animationFrameId: number
    let gridOffset = { x: 0, y: 0 }

    const updateDimensions = () => {
      const rect = container.getBoundingClientRect()
      const dpr = window.devicePixelRatio || 1
      canvas.width = rect.width * dpr
      canvas.height = rect.height * dpr
      canvas.style.width = `${rect.width}px`
      canvas.style.height = `${rect.height}px`
      ctx.scale(dpr, dpr)
    }

    updateDimensions()
    const resizeObserver = new ResizeObserver(updateDimensions)
    resizeObserver.observe(container)

    const handleMouseMove = (event: MouseEvent) => {
      const rect = canvas.getBoundingClientRect()
      const mouseX = event.clientX - rect.left
      const mouseY = event.clientY - rect.top

      const startX = Math.floor(gridOffset.x / squareSize) * squareSize
      const startY = Math.floor(gridOffset.y / squareSize) * squareSize

      const hoveredSquareX = Math.floor((mouseX + (gridOffset.x - startX)) / squareSize)
      const hoveredSquareY = Math.floor((mouseY + (gridOffset.y - startY)) / squareSize)

      hoveredSquareRef.current = { x: hoveredSquareX, y: hoveredSquareY }
    }

    const handleMouseLeave = () => {
      hoveredSquareRef.current = null
    }

    container.addEventListener('mousemove', handleMouseMove)
    container.addEventListener('mouseleave', handleMouseLeave)

    const draw = () => {
      const width = container.clientWidth
      const height = container.clientHeight

      ctx.clearRect(0, 0, width, height)

      const startX = Math.floor(gridOffset.x / squareSize) * squareSize
      const startY = Math.floor(gridOffset.y / squareSize) * squareSize

      ctx.lineWidth = 0.5
      ctx.strokeStyle = borderColor

      for (let x = startX; x < width + squareSize; x += squareSize) {
        for (let y = startY; y < height + squareSize; y += squareSize) {
          const squareX = x - (gridOffset.x % squareSize)
          const squareY = y - (gridOffset.y % squareSize)

          if (hoveredSquareRef.current) {
            const currentSquareX = Math.floor((x - startX) / squareSize)
            const currentSquareY = Math.floor((y - startY) / squareSize)

            if (
              hoveredSquareRef.current.x === currentSquareX &&
              hoveredSquareRef.current.y === currentSquareY
            ) {
              ctx.fillStyle = hoverFillColor
              ctx.fillRect(squareX, squareY, squareSize, squareSize)
            }
          }

          ctx.strokeRect(squareX, squareY, squareSize, squareSize)
        }
      }

      if (!reducedMotion) {
        const effectiveSpeed = Math.max(speed, 0.1)
        switch (direction) {
          case 'right':
            gridOffset.x = (gridOffset.x - effectiveSpeed + squareSize) % squareSize
            break
          case 'left':
            gridOffset.x = (gridOffset.x + effectiveSpeed + squareSize) % squareSize
            break
          case 'up':
            gridOffset.y = (gridOffset.y + effectiveSpeed + squareSize) % squareSize
            break
          case 'down':
            gridOffset.y = (gridOffset.y - effectiveSpeed + squareSize) % squareSize
            break
          case 'diagonal':
            gridOffset.x = (gridOffset.x - effectiveSpeed + squareSize) % squareSize
            gridOffset.y = (gridOffset.y - effectiveSpeed + squareSize) % squareSize
            break
        }

        animationFrameId = requestAnimationFrame(draw)
      }
    }

    draw()

    return () => {
      resizeObserver.disconnect()
      container.removeEventListener('mousemove', handleMouseMove)
      container.removeEventListener('mouseleave', handleMouseLeave)
      cancelAnimationFrame(animationFrameId)
    }
  }, [borderColor, direction, hoverFillColor, reducedMotion, speed, squareSize])

  return (
    <div
      ref={containerRef}
      className={`react-bits-squares-container ${className}`.trim()}
      style={style}
      aria-hidden="true"
    >
      <canvas ref={canvasRef} className="react-bits-squares-canvas" />
    </div>
  )
}

export default Squares
