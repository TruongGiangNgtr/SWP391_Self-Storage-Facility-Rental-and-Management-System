import { lazy, Suspense } from 'react'
import { LoadingState } from '../components/PageStates'
import { Navigate, Route, Routes, useLocation, type Location } from 'react-router-dom'
import { RequireAuth } from '../auth/RequireAuth'
import { RequireRole } from '../auth/RequireRole'
import { CUSTOMER_UNAVAILABLE_PAGES } from '../features/customer-navigation/customerNavigation'
import { CustomerErrorAudience } from '../components/ErrorAudienceContext'

const RootLayout = lazy(() => import('../layouts/RootLayout').then(module => ({ default: module.RootLayout })))
const CustomerLoginPage = lazy(() => import('../pages/CustomerLoginPage').then(module => ({ default: module.CustomerLoginPage })))
const CustomerRegisterPage = lazy(() => import('../pages/CustomerRegisterPage').then(module => ({ default: module.CustomerRegisterPage })))
const CustomerPortalPage = lazy(() => import('../pages/CustomerPortalPage').then(module => ({ default: module.CustomerPortalPage })))
const CustomerUnavailablePage = lazy(() => import('../pages/CustomerUnavailablePage').then(module => ({ default: module.CustomerUnavailablePage })))
const CustomerNotificationsPage = lazy(() => import('../pages/CustomerNotificationsPage').then(module => ({ default: module.CustomerNotificationsPage })))
const CustomerAccountSettingsPage = lazy(() => import('../pages/CustomerAccountSettingsPage').then(module => ({ default: module.CustomerAccountSettingsPage })))
const CustomerContractListPage = lazy(() => import('../pages/CustomerContractListPage').then(module => ({ default: module.CustomerContractListPage })))
const CustomerContractDetailPage = lazy(() => import('../pages/CustomerContractDetailPage').then(module => ({ default: module.CustomerContractDetailPage })))
const ContractRenewalPage = lazy(() => import('../pages/ContractRenewalPage').then(module => ({ default: module.ContractRenewalPage })))
const AccessVisitCreatePage = lazy(() => import('../pages/AccessVisitCreatePage').then(module => ({ default: module.AccessVisitCreatePage })))
const ReturnVisitCreatePage = lazy(() => import('../pages/ReturnVisitCreatePage').then(module => ({ default: module.ReturnVisitCreatePage })))
const CustomerSupportListPage = lazy(() => import('../pages/CustomerSupportListPage').then(module => ({ default: module.CustomerSupportListPage })))
const CustomerSupportCreatePage = lazy(() => import('../pages/CustomerSupportCreatePage').then(module => ({ default: module.CustomerSupportCreatePage })))
const CustomerSupportDetailPage = lazy(() => import('../pages/CustomerSupportDetailPage').then(module => ({ default: module.CustomerSupportDetailPage })))
const EmployeeLoginPage = lazy(() => import('../pages/EmployeeLoginPage').then(module => ({ default: module.EmployeeLoginPage })))
const ForbiddenPage = lazy(() => import('../pages/ForbiddenPage').then(module => ({ default: module.ForbiddenPage })))
const HomePage = lazy(() => import('../pages/HomePage').then(module => ({ default: module.HomePage })))
const PublicStorageSearchPage = lazy(() => import('../pages/PublicStorageSearchPage').then(module => ({ default: module.PublicStorageSearchPage })))
const PublicFacilityDetailPage = lazy(() => import('../pages/PublicFacilityDetailPage').then(module => ({ default: module.PublicFacilityDetailPage })))
const HelpCenterPage = lazy(() => import('../pages/HelpCenterPage').then(module => ({ default: module.HelpCenterPage })))
const PublicPolicyDetailPage = lazy(() => import('../pages/PublicPolicyDetailPage').then(module => ({ default: module.PublicPolicyDetailPage })))
const SizeGuidePage = lazy(() => import('../pages/SizeGuidePage').then(module => ({ default: module.SizeGuidePage })))
const HowItWorksPage = lazy(() => import('../pages/HowItWorksPage').then(module => ({ default: module.HowItWorksPage })))
const InvoiceDetailPage = lazy(() => import('../pages/InvoiceDetailPage').then(module => ({ default: module.InvoiceDetailPage })))
const InvoiceListPage = lazy(() => import('../pages/InvoiceListPage').then(module => ({ default: module.InvoiceListPage })))
const CustomerFeeReturnPage = lazy(() => import('../pages/CustomerFeeReturnPage').then(module => ({ default: module.CustomerFeeReturnPage })))
const NotFoundPage = lazy(() => import('../pages/NotFoundPage').then(module => ({ default: module.NotFoundPage })))
const PaymentResultPage = lazy(() => import('../pages/PaymentResultPage').then(module => ({ default: module.PaymentResultPage })))
const BusinessOperationsLayout = lazy(() => import('../layouts/BusinessOperationsLayout').then(module => ({ default: module.BusinessOperationsLayout })))
const FacilityManagerLayout = lazy(() => import('../layouts/FacilityManagerLayout').then(module => ({ default: module.FacilityManagerLayout })))
const SystemAdministratorLayout = lazy(() => import('../layouts/SystemAdministratorLayout').then(module => ({ default: module.SystemAdministratorLayout })))
const FacilityStaffLayout = lazy(() => import('../layouts/FacilityStaffLayout').then(module => ({ default: module.FacilityStaffLayout })))
const ReservationConfirmPage = lazy(() => import('../pages/ReservationConfirmPage').then(module => ({ default: module.ReservationConfirmPage })))
const ReservationCreatePage = lazy(() => import('../pages/ReservationCreatePage').then(module => ({ default: module.ReservationCreatePage })))
const ReservationDetailPage = lazy(() => import('../pages/ReservationDetailPage').then(module => ({ default: module.ReservationDetailPage })))
const ReservationListPage = lazy(() => import('../pages/ReservationListPage').then(module => ({ default: module.ReservationListPage })))
const StorageSearchPage = lazy(() => import('../pages/StorageSearchPage').then(module => ({ default: module.StorageSearchPage })))
const VisitDetailPage = lazy(() => import('../pages/VisitDetailPage').then(module => ({ default: module.VisitDetailPage })))
const VisitListPage = lazy(() => import('../pages/VisitListPage').then(module => ({ default: module.VisitListPage })))

const FacilityReportsPage = lazy(() => import('../features/manager-reports/FacilityReportsPage').then(module => ({ default: module.FacilityReportsPage })))
const BusinessReportsPage = lazy(() => import('../features/business-reports/BusinessReportsPage').then(module => ({ default: module.BusinessReportsPage })))
const FacilityManagementPage = lazy(() => import('../features/facility-management/FacilityManagementPage').then(module => ({ default: module.FacilityManagementPage })))
const MultiFacilityMonitoringPage = lazy(() => import('../features/multi-facility-monitoring/MultiFacilityMonitoringPage').then(module => ({ default: module.MultiFacilityMonitoringPage })))
const UnitTypePricingPage = lazy(() => import('../features/unit-type-pricing/UnitTypePricingPage').then(module => ({ default: module.UnitTypePricingPage })))
const PolicyVersionManagementPage = lazy(() => import('../features/policy-version-management/PolicyVersionManagementPage').then(module => ({ default: module.PolicyVersionManagementPage })))
const HandoverUnitSelectionPage = lazy(() => import('../features/handover-unit-selection/HandoverUnitSelectionPage').then(module => ({ default: module.HandoverUnitSelectionPage })))
const PhysicalUnitManagementPage = lazy(() => import('../features/physical-unit-management/PhysicalUnitManagementPage').then(module => ({ default: module.PhysicalUnitManagementPage })))
const FacilityOperationsPage = lazy(() => import('../features/manager-monitoring/FacilityOperationsPage').then(module => ({ default: module.FacilityOperationsPage })))
const ReturnsInspectionsPage = lazy(() => import('../features/manager-returns/ReturnsInspectionsPage').then(module => ({ default: module.ReturnsInspectionsPage })))
const ManagerSupportTicketsPage = lazy(() => import('../features/manager-support/pages/ManagerSupportTicketsPage'))
const UserAccountMonitoringPage = lazy(() => import('../features/admin-user-monitoring/UserAccountMonitoringPage'))
const EmployeeAccountManagementPage = lazy(() => import('../features/admin-employee-management/EmployeeAccountManagementPage'))
const RoleFacilityAssignmentPage = lazy(() => import('../features/admin-role-facility-assignment/RoleFacilityAssignmentPage'))
const CustomerAccountStatusPage = lazy(() => import('../features/admin-customer-status/CustomerAccountStatusPage'))
const AccessManagementPage = lazy(() => import('../features/admin-access-management/AccessManagementPage'))
const LoginHistoryPage = lazy(() => import('../features/admin-login-history/LoginHistoryPage'))
const ActivityLogManagementPage = lazy(() => import('../features/admin-activity-logs/ActivityLogManagementPage'))
const DiscountAndExtraFeePage = lazy(() => import('../features/discount-management/DiscountAndExtraFeePage').then(module => ({ default: module.DiscountAndExtraFeePage })))
const StaffWorkItemsPage = lazy(() => import('../pages/StaffWorkItemsPage').then(module => ({ default: module.StaffWorkItemsPage })))
const StaffHandoverPage = lazy(() => import('../pages/StaffHandoverPage').then(module => ({ default: module.StaffHandoverPage })))
const StaffAccessVisitPage = lazy(() => import('../pages/StaffAccessVisitPage').then(module => ({ default: module.StaffAccessVisitPage })))
const StaffContractPage = lazy(() => import('../pages/StaffContractPage').then(module => ({ default: module.StaffContractPage })))
const StaffContractListPage = lazy(() => import('../pages/StaffContractListPage').then(module => ({ default: module.StaffContractListPage })))
const StaffReturnsPage = lazy(() => import('../pages/StaffReturnsPage').then(module => ({ default: module.StaffReturnsPage })))
const StaffSupportPage = lazy(() => import('../pages/StaffSupportPage').then(module => ({ default: module.StaffSupportPage })))

export function AppRouter() {
  const location = useLocation()
  const state = location.state as { backgroundLocation?: Location } | null
  const isDialog = location.pathname === '/customer/reservations/new' ||
    /^\/customer\/reservations\/[^/]+\/confirm$/.test(location.pathname) ||
    /^\/customer\/contracts\/[^/]+\/(renew|return-visits\/new)$/.test(location.pathname)
  const backgroundLocation = isDialog ? state?.backgroundLocation : undefined
  const customerAudience = location.pathname === '/' || location.pathname === '/customer' ||
    ['/customer/', '/auth/customer/', '/facilities/', '/help/'].some(path => location.pathname.startsWith(path)) ||
    ['/facilities', '/help', '/size-guide', '/how-it-works'].includes(location.pathname)

  return (
    <CustomerErrorAudience.Provider value={customerAudience}>
    <Suspense fallback={<LoadingState label="Loading page..." />}>
    <Routes location={backgroundLocation ?? location}>
  {/* ROOT LAYOUT + CUSTOMER */}
  <Route element={<RootLayout />}>
    <Route index element={<HomePage />} />
    <Route path="facilities" element={<PublicStorageSearchPage />} />
    <Route path="facilities/:facilityId" element={<PublicFacilityDetailPage />} />
    <Route path="customer/storage-search" element={<StorageSearchPage />} />
    <Route path="how-it-works" element={<HowItWorksPage />} />
    <Route path="size-guide" element={<SizeGuidePage />} />
    <Route path="help" element={<HelpCenterPage />} />
    <Route path="help/policies/identity-verification" element={<Navigate to="/help/policies/reservation-cancellation" replace />} />
    <Route path="help/policies/account-privacy" element={<Navigate to="/help/policies/reservation-cancellation" replace />} />
    <Route path="help/policies/:policyKey" element={<PublicPolicyDetailPage />} />

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
          path="customer/contracts"
          element={<CustomerContractListPage />}
        />

        <Route
          path="customer/contracts/:contractId"
          element={<CustomerContractDetailPage />}
        />

        <Route
          path="customer/contracts/:contractId/renew"
          element={<ContractRenewalPage key={location.pathname} />}
        />

        <Route path="customer/contracts/:contractId/fees-return" element={<CustomerFeeReturnPage key={location.pathname} />} />

        <Route
          path="customer/contracts/:contractId/access-visits/new"
          element={<AccessVisitCreatePage />}
        />

        <Route
          path="customer/contracts/:contractId/return-visits/new"
          element={<ReturnVisitCreatePage key={location.pathname} />}
        />

        <Route path="customer/support-tickets" element={<CustomerSupportListPage />} />
        <Route path="customer/support-tickets/new" element={<CustomerSupportCreatePage />} />
        <Route path="customer/support-tickets/:ticketId" element={<CustomerSupportDetailPage key={location.pathname} />} />

        <Route path="customer/account-settings" element={<CustomerAccountSettingsPage />} />
        <Route path="customer/identity-verification" element={<Navigate to="/customer/account-settings" replace />} />
        <Route path="customer/notifications" element={<CustomerNotificationsPage />} />
        {CUSTOMER_UNAVAILABLE_PAGES.map(page => <Route key={page.to} path={page.to.slice(1)} element={<CustomerUnavailablePage title={page.title} historyTabs={page.historyTabs} />} />)}

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
      <Route path="staff" element={<FacilityStaffLayout />}>
        <Route index element={<Navigate to="work-list" replace />} />
        <Route path="work-list" element={<StaffWorkItemsPage mode="daily" />} />
        <Route path="contracts" element={<StaffContractListPage />} />
        <Route path="handle-handover" element={<StaffWorkItemsPage mode="handle-handover" />} />
        <Route path="reservation-check-in" element={<Navigate to="/staff/handle-handover?tab=check-in" replace />} />
        <Route path="handover-processing" element={<Navigate to="/staff/handle-handover?tab=handover" replace />} />
        <Route path="access-visits" element={<StaffWorkItemsPage mode="access" />} />
        <Route path="returns" element={<StaffReturnsPage />} />
        <Route path="support" element={<StaffSupportPage />} />
        <Route path="reservations/:reservationId/visits/:visitId" element={<StaffHandoverPage />} />
        <Route path="contracts/:contractId/access-visits/:visitId" element={<StaffAccessVisitPage />} />
        <Route path="contracts/:contractId" element={<StaffContractPage />} />
      </Route>
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
        element={<FacilityManagerLayout />}
      >
        <Route
          index
          element={
            <Navigate
              to="handover-unit-selection"
              replace
            />
          }
        />

        <Route
          path="handover-unit-selection"
          element={<HandoverUnitSelectionPage />}
        />

        <Route
          path="physical-units"
          element={<PhysicalUnitManagementPage />}
        />

        <Route
          path="operations"
          element={<FacilityOperationsPage />}
        />

        <Route
          path="returns-inspections"
          element={<ReturnsInspectionsPage />}
        />

        <Route
          path="support-assignment"
          element={<ManagerSupportTicketsPage />}
        />

        <Route
          path="reports"
          element={
            <FacilityReportsPage />
          }
        />
      </Route>
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
          path="monitoring"
          element={<MultiFacilityMonitoringPage />}
        />

        <Route
          path="unit-types"
          element={<UnitTypePricingPage />}
        />

        <Route
          path="policies"
          element={<PolicyVersionManagementPage />}
        />
        <Route path="discounts" element={<DiscountAndExtraFeePage />} />
        <Route path="extra-fees" element={<DiscountAndExtraFeePage />} />
        <Route path="reports" element={<BusinessReportsPage />} />
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
        element={<SystemAdministratorLayout />}
      >
        <Route
          index
          element={<Navigate to="users" replace />}
        />

        <Route
          path="users"
          element={<UserAccountMonitoringPage />}
        />

        <Route
          path="customer-status"
          element={<CustomerAccountStatusPage />}
        />

        <Route
          path="employees"
          element={<EmployeeAccountManagementPage />}
        />

        <Route
          path="assignment"
          element={<RoleFacilityAssignmentPage />}
        />

        <Route
          path="access-management"
          element={<AccessManagementPage />}
        />

        <Route
          path="login-history"
          element={<LoginHistoryPage />}
        />

        <Route
          path="activity-logs"
          element={<ActivityLogManagementPage />}
        />
      </Route>
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
            <Route path="customer/contracts/:contractId/renew" element={<ContractRenewalPage key={location.pathname} />} />
            <Route path="customer/contracts/:contractId/return-visits/new" element={<ReturnVisitCreatePage key={location.pathname} />} />
          </Route>
        </Route>
      </Routes>
    )}
    </Suspense>
    </CustomerErrorAudience.Provider>
  )
}
