import { useCurrentRoute } from '../routes/useCurrentRoute'
import { FoundationPage } from '../pages/FoundationPage'
import { NotFoundPage } from '../pages/NotFoundPage'

export function App() {
  const route = useCurrentRoute()
  return route === 'foundation' ? <FoundationPage /> : <NotFoundPage />
}
