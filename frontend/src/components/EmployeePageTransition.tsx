import { Component, Suspense, type ReactNode } from 'react'
import { LoadingState } from './PageStates'
import { useLocation, Outlet } from 'react-router-dom'
import { EmployeeLabelsProvider } from './EmployeeEntityLabel'
import FadeContent from './react-bits/FadeContent'

class WorkspaceBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false }

  static getDerivedStateFromError() { return { failed: true } }

  render() {
    if (this.state.failed) return <section role="alert">
      <h1>Unable to load this workspace</h1>
      <p>Please reload the page to try again.</p>
      <button type="button" className="button" onClick={() => window.location.reload()}>Reload workspace</button>
    </section>
    return this.props.children
  }
}

export function EmployeePageTransition() {
  const { pathname } = useLocation()
  return (
    <FadeContent transitionKey={pathname}>
      <WorkspaceBoundary key={pathname}>
        <Suspense fallback={<LoadingState label="Loading workspace..." />}>
          <EmployeeLabelsProvider><Outlet /></EmployeeLabelsProvider>
        </Suspense>
      </WorkspaceBoundary>
    </FadeContent>
  )
}
