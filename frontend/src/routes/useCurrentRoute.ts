import { useEffect, useState } from 'react'

export type FoundationRoute = 'foundation' | 'not-found'

export function useCurrentRoute(): FoundationRoute {
  const resolve = () => window.location.pathname === '/' ? 'foundation' : 'not-found'
  const [route, setRoute] = useState<FoundationRoute>(resolve)
  useEffect(() => { const listener = () => setRoute(resolve()); window.addEventListener('popstate', listener); return () => window.removeEventListener('popstate', listener) }, [])
  return route
}
