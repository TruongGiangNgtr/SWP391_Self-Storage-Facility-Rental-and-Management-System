import { Navigate, Route, Routes } from 'react-router-dom'
import { RequireAuth } from '../auth/RequireAuth'
import { RequireRole } from '../auth/RequireRole'
import { RootLayout } from '../layouts/RootLayout'
import { CustomerLoginPage } from '../pages/CustomerLoginPage'
import { CustomerRegisterPage } from '../pages/CustomerRegisterPage'
import { EmployeeLoginPage } from '../pages/EmployeeLoginPage'
import { ForbiddenPage } from '../pages/ForbiddenPage'
import { HomePage } from '../pages/HomePage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { PortalPlaceholderPage } from '../pages/PortalPlaceholderPage'
import { BusinessOperationsLayout } from '../layouts/BusinessOperationsLayout'
import { FacilityManagementPage } from '../features/facility-management/FacilityManagementPage'

export function AppRouter() {
  return (
    <Routes>
      {/* TEMPORARY DEVELOPMENT ROUTE */}
      <Route
        path="dev/business"
        element={
          <BusinessOperationsLayout basePath="/dev/business" />
        }
      >
        <Route
          index
          element={
            <div className="facility-page">
              <h1>Business Operations Overview</h1>
            </div>
          }
        />

        <Route
          path="facilities"
          element={<FacilityManagementPage />}
        />
      </Route>

      {/* NORMAL APPLICATION */}
      <Route element={<RootLayout />}>
        <Route index element={<HomePage />} />
        <Route path="auth/customer/login" element={<CustomerLoginPage />} />
        <Route path="auth/customer/register" element={<CustomerRegisterPage />} />
        <Route path="auth/employee/login" element={<EmployeeLoginPage />} />
        <Route path="forbidden" element={<ForbiddenPage />} />
        
        <Route element={<RequireAuth />}>
        {/* Các route thật ở đây */}
          <Route element={<RequireRole allowedRoles={['CUSTOMER']} />}>
            <Route
              path="customer"
              element={<PortalPlaceholderPage title="Customer Portal" />}
            />
          </Route>

          <Route element={<RequireRole allowedRoles={['FACILITY_STAFF']} />}>
            <Route
              path="staff"
              element={<PortalPlaceholderPage title="Facility Staff Portal" />}
            />
          </Route>

          <Route element={<RequireRole allowedRoles={['FACILITY_MANAGER']} />}>
            <Route
              path="manager"
              element={<PortalPlaceholderPage title="Facility Manager Portal" />}
            />
          </Route>

          <Route
              element={
                <RequireRole
                  allowedRoles={['BUSINESS_OPERATIONS_MANAGER']}
                />
              }
            >
              <Route
                path="business"
                element={<BusinessOperationsLayout />}
              >
                <Route
                  index
                  element={
                    <PortalPlaceholderPage
                      title="Business Operations Portal"
                    />
                  }
                />

                <Route
                  path="facilities"
                  element={<FacilityManagementPage />}
                />
              </Route>
            </Route>

          <Route element={<RequireRole allowedRoles={['SYSTEM_ADMINISTRATOR']} />}>
            <Route
              path="admin"
              element={<PortalPlaceholderPage title="System Administrator Portal" />}
            />
          </Route>
        </Route>

        <Route path="404" element={<NotFoundPage />} />
        <Route path="*" element={<Navigate to="/404" replace />} />
      </Route>
    </Routes>
  )
}
