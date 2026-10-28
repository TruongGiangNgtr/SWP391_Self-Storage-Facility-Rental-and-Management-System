import { Navigate, Route, Routes, useLocation, type Location } from 'react-router-dom'
import { DiscountAndExtraFeePage } from '../features/discount-management/DiscountAndExtraFeePage'
import { RequireAuth } from '../auth/RequireAuth'
import { RequireRole } from '../auth/RequireRole'
import { RootLayout } from '../layouts/RootLayout'
import { CustomerLoginPage } from '../pages/CustomerLoginPage'
import { CustomerRegisterPage } from '../pages/CustomerRegisterPage'
import { CustomerPortalPage } from '../pages/CustomerPortalPage'
import { CustomerUnavailablePage } from '../pages/CustomerUnavailablePage'
import { CustomerNotificationsPage } from '../pages/CustomerNotificationsPage'
import { CustomerAccountSettingsPage } from '../pages/CustomerAccountSettingsPage'
import { CUSTOMER_UNAVAILABLE_PAGES } from '../features/customer-navigation/customerNavigation'
import { CustomerContractListPage } from '../pages/CustomerContractListPage'
import { CustomerContractDetailPage } from '../pages/CustomerContractDetailPage'
import { ContractRenewalPage } from '../pages/ContractRenewalPage'
import { AccessVisitCreatePage } from '../pages/AccessVisitCreatePage'
import { ReturnVisitCreatePage } from '../pages/ReturnVisitCreatePage'
import { CustomerSupportListPage } from '../pages/CustomerSupportListPage'
import { CustomerSupportCreatePage } from '../pages/CustomerSupportCreatePage'
import { CustomerSupportDetailPage } from '../pages/CustomerSupportDetailPage'
import { EmployeeLoginPage } from '../pages/EmployeeLoginPage'
import { ForbiddenPage } from '../pages/ForbiddenPage'
import { HomePage } from '../pages/HomePage'
import { PublicStorageSearchPage } from '../pages/PublicStorageSearchPage'
import { PublicFacilityDetailPage } from '../pages/PublicFacilityDetailPage'
import { HelpCenterPage } from '../pages/HelpCenterPage'
import { PublicPolicyDetailPage } from '../pages/PublicPolicyDetailPage'
import { SizeGuidePage } from '../pages/SizeGuidePage'
import { HowItWorksPage } from '../pages/HowItWorksPage'
import { InvoiceDetailPage } from '../pages/InvoiceDetailPage'
import { InvoiceListPage } from '../pages/InvoiceListPage'
import { CustomerFeeReturnPage } from '../pages/CustomerFeeReturnPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { PaymentResultPage } from '../pages/PaymentResultPage'
import { FacilityReportsPage } from '../features/manager-reports/FacilityReportsPage'
import { BusinessReportsPage } from '../features/business-reports/BusinessReportsPage'
import { BusinessOperationsLayout } from '../layouts/BusinessOperationsLayout'
import { FacilityManagerLayout } from '../layouts/FacilityManagerLayout'
import { SystemAdministratorLayout } from '../layouts/SystemAdministratorLayout'
import { FacilityStaffLayout } from '../layouts/FacilityStaffLayout'
import { FacilityManagementPage } from '../features/facility-management/FacilityManagementPage'
import { MultiFacilityMonitoringPage } from '../features/multi-facility-monitoring/MultiFacilityMonitoringPage'
import { UnitTypePricingPage } from '../features/unit-type-pricing/UnitTypePricingPage'
import { PolicyVersionManagementPage } from '../features/policy-version-management/PolicyVersionManagementPage'
import { HandoverUnitSelectionPage } from '../features/handover-unit-selection/HandoverUnitSelectionPage'
import { PhysicalUnitManagementPage } from '../features/physical-unit-management/PhysicalUnitManagementPage'
import { FacilityOperationsPage } from '../features/manager-monitoring/FacilityOperationsPage'
import { ReturnsInspectionsPage } from '../features/manager-returns/ReturnsInspectionsPage'
import ManagerSupportTicketsPage from '../features/manager-support/pages/ManagerSupportTicketsPage'
import UserAccountMonitoringPage from '../features/admin-user-monitoring/UserAccountMonitoringPage'
import EmployeeAccountManagementPage from '../features/admin-employee-management/EmployeeAccountManagementPage'
import RoleFacilityAssignmentPage from '../features/admin-role-facility-assignment/RoleFacilityAssignmentPage'
import CustomerAccountStatusPage from '../features/admin-customer-status/CustomerAccountStatusPage'
import AccessManagementPage from '../features/admin-access-management/AccessManagementPage'
import LoginHistoryPage from '../features/admin-login-history/LoginHistoryPage'
import ActivityLogManagementPage from '../features/admin-activity-logs/ActivityLogManagementPage'
import { ReservationConfirmPage } from '../pages/ReservationConfirmPage'
import { ReservationCreatePage } from '../pages/ReservationCreatePage'
import { ReservationDetailPage } from '../pages/ReservationDetailPage'
import { ReservationListPage } from '../pages/ReservationListPage'
import { StorageSearchPage } from '../pages/StorageSearchPage'
import { VisitDetailPage } from '../pages/VisitDetailPage'
import { VisitListPage } from '../pages/VisitListPage'
import { StaffWorkItemsPage } from '../pages/StaffWorkItemsPage'
import { StaffHandoverPage } from '../pages/StaffHandoverPage'
import { StaffAccessVisitPage } from '../pages/StaffAccessVisitPage'
import { StaffContractPage } from '../pages/StaffContractPage'
import { StaffContractListPage } from '../pages/StaffContractListPage'
import { StaffReturnsPage } from '../pages/StaffReturnsPage'
import { StaffSupportPage } from '../pages/StaffSupportPage'
import { CustomerErrorAudience } from '../components/ErrorAudienceContext'

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
    </CustomerErrorAudience.Provider>
  )
}
