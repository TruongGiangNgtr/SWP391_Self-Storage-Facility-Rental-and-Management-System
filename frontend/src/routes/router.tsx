import { Navigate, Route, Routes, useLocation, type Location } from 'react-router-dom'
import { RequireAuth } from '../auth/RequireAuth'
import { RequireRole } from '../auth/RequireRole'
import { RootLayout } from '../layouts/RootLayout'
import { CustomerLoginPage } from '../pages/CustomerLoginPage'
import { CustomerRegisterPage } from '../pages/CustomerRegisterPage'
import { CustomerPortalPage } from '../pages/CustomerPortalPage'
import { EmployeeLoginPage } from '../pages/EmployeeLoginPage'
import { ForbiddenPage } from '../pages/ForbiddenPage'
import { HomePage } from '../pages/HomePage'
import { InvoiceDetailPage } from '../pages/InvoiceDetailPage'
import { InvoiceListPage } from '../pages/InvoiceListPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { PaymentResultPage } from '../pages/PaymentResultPage'
import { PortalPlaceholderPage } from '../pages/PortalPlaceholderPage'
import { BusinessOperationsLayout } from '../layouts/BusinessOperationsLayout'
import { FacilityManagementPage } from '../features/facility-management/FacilityManagementPage'
import { UnitTypePricingPage } from '../features/unit-type-pricing/UnitTypePricingPage'
import { PolicyVersionManagementPage } from '../features/policy-version-management/PolicyVersionManagementPage'
import { ReservationConfirmPage } from '../pages/ReservationConfirmPage'
import { ReservationCreatePage } from '../pages/ReservationCreatePage'
import { ReservationDetailPage } from '../pages/ReservationDetailPage'
import { ReservationListPage } from '../pages/ReservationListPage'
import { StorageSearchPage } from '../pages/StorageSearchPage'
import { VisitDetailPage } from '../pages/VisitDetailPage'
import { VisitListPage } from '../pages/VisitListPage'

export function AppRouter() {
  const location = useLocation()
  const state = location.state as { backgroundLocation?: Location } | null
  const isDialog = location.pathname === '/customer/reservations/new' ||
    /^\/customer\/reservations\/[^/]+\/confirm$/.test(location.pathname)
  const backgroundLocation = isDialog ? state?.backgroundLocation : undefined

  return (
    <>
    <Routes location={backgroundLocation ?? location}>
  {/* ROOT LAYOUT + CUSTOMER */}
  <Route element={<RootLayout />}>
    <Route index element={<HomePage />} />

    <Route
      path="auth/customer/login"
      element={<CustomerLoginPage />}
    />

    <Route
      path="auth/customer/register"
      element={<CustomerRegisterPage />}
    />

    <Route
      path="auth/employee/login"
      element={<EmployeeLoginPage />}
    />

    <Route
      path="forbidden"
      element={<ForbiddenPage />}
    />

    <Route element={<RequireAuth />}>
      <Route
        element={
          <RequireRole allowedRoles={['CUSTOMER']} />
        }
      >
        <Route
          path="customer"
          element={<CustomerPortalPage />}
        />

        <Route
          path="customer/storage-search"
          element={<StorageSearchPage />}
        />

        <Route
          path="customer/reservations/new"
          element={<ReservationCreatePage />}
        />

        <Route
          path="customer/reservations"
          element={<ReservationListPage />}
        />

        <Route
          path="customer/reservations/:reservationId"
          element={<ReservationDetailPage />}
        />

        <Route
          path="customer/reservations/:reservationId/confirm"
          element={<ReservationConfirmPage />}
        />

        <Route
          path="customer/invoices"
          element={<InvoiceListPage />}
        />

        <Route
          path="customer/invoices/:invoiceId"
          element={<InvoiceDetailPage />}
        />

        <Route
          path="customer/payments/result"
          element={<PaymentResultPage />}
        />

        <Route
          path="customer/visits"
          element={<VisitListPage />}
        />

        <Route
          path="customer/visits/:visitId"
          element={<VisitDetailPage />}
        />
      </Route>
    </Route>

    <Route path="404" element={<NotFoundPage />} />
  </Route>

  {/* FACILITY STAFF */}
  <Route element={<RequireAuth />}>
    <Route
      element={
        <RequireRole
          allowedRoles={['FACILITY_STAFF']}
        />
      }
    >
      <Route
        path="staff"
        element={
          <PortalPlaceholderPage
            title="Facility Staff Portal"
          />
        }
      />
    </Route>
  </Route>

  {/* FACILITY MANAGER */}
  <Route element={<RequireAuth />}>
    <Route
      element={
        <RequireRole
          allowedRoles={['FACILITY_MANAGER']}
        />
      }
    >
      <Route
        path="manager"
        element={
          <PortalPlaceholderPage
            title="Facility Manager Portal"
          />
        }
      />
    </Route>
  </Route>

  {/* BUSINESS OPERATIONS MANAGER */}
  <Route element={<RequireAuth />}>
    <Route
      element={
        <RequireRole
          allowedRoles={[
            'BUSINESS_OPERATIONS_MANAGER',
          ]}
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
            <Navigate to="facilities" replace />
          }
        />

        <Route
          path="facilities"
          element={<FacilityManagementPage />}
        />

        <Route
          path="unit-types"
          element={<UnitTypePricingPage />}
        />

        <Route
          path="policies"
          element={<PolicyVersionManagementPage />}
        />
      </Route>
    </Route>
  </Route>

  {/* SYSTEM ADMINISTRATOR */}
  <Route element={<RequireAuth />}>
    <Route
      element={
        <RequireRole
          allowedRoles={[
            'SYSTEM_ADMINISTRATOR',
          ]}
        />
      }
    >
      <Route
        path="admin"
        element={
          <PortalPlaceholderPage
            title="System Administrator Portal"
          />
        }
      />
    </Route>
  </Route>

  <Route
    path="*"
    element={<Navigate to="/404" replace />}
  />
</Routes>
    {backgroundLocation && (
      <Routes>
        <Route element={<RequireAuth />}>
          <Route element={<RequireRole allowedRoles={['CUSTOMER']} />}>
            <Route path="customer/reservations/new" element={<ReservationCreatePage />} />
            <Route path="customer/reservations/:reservationId/confirm" element={<ReservationConfirmPage />} />
          </Route>
        </Route>
      </Routes>
    )}
    </>
  )
}
