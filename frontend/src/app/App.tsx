import { AuthProvider } from '../auth/AuthProvider'
import { AppRouter } from '../routes/router'
import { ContinuationNavigationGuard } from '../auth/ContinuationNavigationGuard'

export function App() {
  return (
    <AuthProvider>
      <ContinuationNavigationGuard />
      <AppRouter />
    </AuthProvider>
  )
}
