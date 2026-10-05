import { AuthProvider } from '../auth/AuthProvider'
import { AppRouter } from '../routes/router'

export function App() {
  return (
    <AuthProvider>
      <AppRouter />
    </AuthProvider>
  )
}
