// Adapted from React Bits' TypeScript/CSS FadeContent (David Haz).
// https://github.com/DavidHDev/react-bits/blob/main/src/ts-default/Animations/FadeContent/FadeContent.tsx
// See LICENSE.md. This employee variant keeps only the mount fade: no scroll
// triggers, filters, hidden defaults, or staggering of operational content.
import { useEffect, useRef, type ReactNode } from 'react'

interface FadeContentProps {
  children: ReactNode
  transitionKey: string
}

export default function FadeContent({ children, transitionKey }: FadeContentProps) {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const element = ref.current
    if (!element) return
    const preference = window.matchMedia('(prefers-reduced-motion: reduce)')
    let active = true
    let context: { revert: () => void } | undefined

    function stop() {
      context?.revert()
      context = undefined
    }

    // Also honor a preference change while a transition is in progress.
    preference.addEventListener('change', stop)
    if (!preference.matches) {
      void import('gsap').then(({ gsap }) => {
        if (!active || preference.matches || document.hidden) return
        context = gsap.context(() => {
          gsap.fromTo(element, { opacity: 0.94 }, {
            opacity: 1, duration: 0.18, ease: 'power3.out', clearProps: 'opacity',
          })
        }, element)
      }).catch(() => { /* The page remains fully visible if motion cannot load. */ })
    }

    return () => {
      active = false
      preference.removeEventListener('change', stop)
      stop()
    }
  }, [transitionKey])

  return <div ref={ref} className="employee-page-content">{children}</div>
}
