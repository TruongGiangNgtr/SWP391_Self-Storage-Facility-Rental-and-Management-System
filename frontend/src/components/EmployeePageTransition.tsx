import { useLocation, Outlet } from 'react-router-dom'
import { EmployeeLabelsProvider } from './EmployeeEntityLabel'
import FadeContent from './react-bits/FadeContent'

export function EmployeePageTransition() {
  const { pathname } = useLocation()
  return (
    <FadeContent transitionKey={pathname}>
      <EmployeeLabelsProvider><Outlet /></EmployeeLabelsProvider>
    </FadeContent>
  )
}
