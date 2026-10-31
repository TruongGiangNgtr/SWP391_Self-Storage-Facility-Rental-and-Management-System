// Adapted from React Bits' TypeScript/CSS DecryptedText (David Haz).
// https://github.com/DavidHDev/react-bits/blob/main/src/ts-default/TextAnimations/DecryptedText/DecryptedText.tsx
// See LICENSE.md.
import { useEffect, useRef, useState, type HTMLAttributes } from 'react'
import './DecryptedText.css'

export interface DecryptedTextProps extends HTMLAttributes<HTMLSpanElement> {
  text: string
  speed?: number
  maxIterations?: number
  sequential?: boolean
  revealDirection?: 'start' | 'end' | 'center'
  useOriginalCharsOnly?: boolean
  characters?: string
  className?: string
  encryptedClassName?: string
  parentClassName?: string
  animateOn?: 'view' | 'hover'
}

const DEFAULT_CHARS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-#'

export function DecryptedText({
  text,
  speed = 40,
  maxIterations = 10,
  sequential = true,
  revealDirection = 'start',
  useOriginalCharsOnly = false,
  characters = DEFAULT_CHARS,
  className = '',
  encryptedClassName = 'decrypted-scramble',
  parentClassName = 'decrypted-container',
  animateOn = 'view',
  ...rest
}: DecryptedTextProps) {
  const [displayText, setDisplayText] = useState(text)
  const [isScrambling, setIsScrambling] = useState(false)
  const [revealedIndices, setRevealedIndices] = useState<Set<number>>(new Set())
  const [hasAnimated, setHasAnimated] = useState(false)
  const containerRef = useRef<HTMLSpanElement>(null)
  const intervalRef = useRef<number | null>(null)
  const [reducedMotion, setReducedMotion] = useState(false)

  useEffect(() => {
    const media = window.matchMedia('(prefers-reduced-motion: reduce)')
    setReducedMotion(media.matches)
    const listener = (event: MediaQueryListEvent) => setReducedMotion(event.matches)
    media.addEventListener('change', listener)
    return () => media.removeEventListener('change', listener)
  }, [])

  const availableChars = useOriginalCharsOnly
    ? Array.from(new Set(text.split(''))).filter((char) => char !== ' ')
    : characters.split('')

  const getNextChar = (originalChar: string) => {
    if (originalChar === ' ') return ' '
    const randomIndex = Math.floor(Math.random() * availableChars.length)
    return availableChars[randomIndex] || originalChar
  }

  const startScramble = () => {
    if (reducedMotion || !text) {
      setDisplayText(text)
      return
    }

    if (intervalRef.current !== null) {
      clearInterval(intervalRef.current)
    }

    setIsScrambling(true)
    let currentIteration = 0
    const targetRevealed = new Set<number>()

    const order: number[] = []
    const length = text.length

    if (revealDirection === 'end') {
      for (let i = length - 1; i >= 0; i--) order.push(i)
    } else if (revealDirection === 'center') {
      const mid = Math.floor(length / 2)
      let offset = 0
      while (mid - offset >= 0 || mid + offset < length) {
        if (mid + offset < length) order.push(mid + offset)
        if (offset > 0 && mid - offset >= 0) order.push(mid - offset)
        offset++
      }
    } else {
      for (let i = 0; i < length; i++) order.push(i)
    }

    intervalRef.current = window.setInterval(() => {
      currentIteration++

      if (sequential) {
        const charsToReveal = Math.floor((currentIteration / maxIterations) * length)
        for (let i = 0; i < charsToReveal && i < length; i++) {
          targetRevealed.add(order[i])
        }
      } else {
        if (currentIteration >= maxIterations) {
          for (let i = 0; i < length; i++) targetRevealed.add(i)
        }
      }

      setRevealedIndices(new Set(targetRevealed))

      const scrambled = text
        .split('')
        .map((char, index) => {
          if (char === ' ') return ' '
          if (targetRevealed.has(index)) return char
          return getNextChar(char)
        })
        .join('')

      setDisplayText(scrambled)

      if (targetRevealed.size >= length || currentIteration >= maxIterations * 1.5) {
        if (intervalRef.current !== null) {
          clearInterval(intervalRef.current)
          intervalRef.current = null
        }
        setDisplayText(text)
        setIsScrambling(false)
        setRevealedIndices(new Set(Array.from({ length }, (_, i) => i)))
      }
    }, speed)
  }

  useEffect(() => {
    if (reducedMotion) {
      setDisplayText(text)
      return
    }

    if (animateOn === 'view' && !hasAnimated) {
      const observer = new IntersectionObserver(
        (entries) => {
          if (entries[0]?.isIntersecting) {
            setHasAnimated(true)
            startScramble()
            observer.disconnect()
          }
        },
        { threshold: 0.1 },
      )

      if (containerRef.current) {
        observer.observe(containerRef.current)
      }

      return () => {
        observer.disconnect()
        if (intervalRef.current !== null) clearInterval(intervalRef.current)
      }
    }
  }, [animateOn, hasAnimated, reducedMotion, text])

  useEffect(() => {
    return () => {
      if (intervalRef.current !== null) {
        clearInterval(intervalRef.current)
      }
    }
  }, [])

  const handleMouseEnter = () => {
    if (animateOn === 'hover' || !isScrambling) {
      startScramble()
    }
  }

  if (reducedMotion) {
    return <span className={`decrypted-text ${className}`.trim()} {...rest}>{text}</span>
  }

  return (
    <span
      ref={containerRef}
      className={`decrypted-text ${parentClassName}`.trim()}
      aria-label={text}
      onMouseEnter={handleMouseEnter}
      {...rest}
    >
      <span aria-hidden="true" className="decrypted-chars">
        {displayText.split('').map((char, index) => {
          const isRevealed = revealedIndices.has(index)
          return (
            <span
              key={index}
              className={isRevealed ? className : encryptedClassName}
            >
              {char}
            </span>
          )
        })}
      </span>
    </span>
  )
}

export default DecryptedText
