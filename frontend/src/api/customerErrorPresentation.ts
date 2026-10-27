import type { ApiErrorPresentation } from './apiErrorPresentation'

// Customer copy is selected by stable codes/field names, never server prose.
// Unknown errors must not expose SQL, provider responses or submitted values.
const MESSAGES: Record<string, string> = {
  AUTH_INVALID_CREDENTIALS: 'The phone number or password is incorrect. Check both and try signing in again.',
  EMAIL_ALREADY_EXISTS: 'This email address is already registered. Sign in to your account or use another email address.',
  PHONE_NUMBER_ALREADY_EXISTS: 'This phone number is already registered. Sign in to your existing account.',
  ACCOUNT_INACTIVE: 'Your account is currently inactive. Please contact support for help signing in or using your account.',
  VALIDATION_ERROR: 'Please review the information below and try again.',
  UNAUTHORIZED: 'Please sign in again to continue. Your session may have expired.',
  FORBIDDEN: 'You cannot access this item with your account. Return to your account and choose another item, or contact support for help.',
  RESOURCE_NOT_FOUND: 'We could not find this item. Return to the list and select it again, or contact support if you still need help.',
  FACILITY_INACTIVE: 'This facility is not accepting new reservations right now. Please choose another facility.',
  INVALID_MONTH_RANGE: 'Choose a check-in month after the current month. The check-out month must be the same month or later.',
  CAPACITY_NOT_AVAILABLE: 'This storage type is no longer available for your full rental period. Search again with different months, a different size or another facility.',
  DEPOSIT_NOT_PAID: 'Your deposit is not confirmed as paid yet. Check the deposit invoice before continuing. If you have already paid, check its status before making another payment.',
  VISIT_DATE_OUT_OF_POLICY: 'This date is not available for your visit. Please choose a date within the allowed period shown on this page.',
  RESERVATION_INVALID_STATUS: 'Your reservation has changed and this action is no longer available. Refresh the reservation to see what you can do next.',
  CONTRACT_NOT_ACTIVE: 'This action is only available for an active contract. Open My Contracts to check the latest status.',
  RETURN_VISIT_PENDING: 'You already have a return visit scheduled or in progress for this contract. Review it in My Visits before booking an access visit or renewing.',
  RENEWAL_NOT_CONTIGUOUS: 'Your extension must start immediately after your current rental ends. Refresh the contract and choose a later end month.',
  RENEWAL_CAPACITY_NOT_AVAILABLE: 'This storage type is not available for every month of your extension. Choose a different end month and try again.',
  VISIT_INVALID_STATUS: 'This visit has changed and the selected action is no longer available. Refresh My Visits to check its latest status.',
  VISIT_ENTITY_MISMATCH: 'We could not match this visit to your rental. Return to My Visits and open the visit again.',
  SUPPORT_TICKET_INVALID_STATUS: 'This support request has changed and the selected action is no longer available. Refresh the request to check its latest status.',
  RETURN_ALREADY_FINALIZED: 'Your return has already been completed. Refresh Fees & Return Tracking to see the latest details.',
  DAMAGE_DECISION_PENDING: 'Your return is still being reviewed. Check Fees & Return Tracking for updates or contact the facility for help.',
  INVOICE_ALREADY_EXISTS: 'An invoice already exists for this charge. Open Deposit & Invoices to check it before continuing.',
  INVOICE_NOT_PAYABLE: 'This invoice cannot be paid right now. Open the invoice to check its latest status or contact the facility for help.',
  PAYMENT_IDEMPOTENCY_CONFLICT: 'The payment details have changed. Return to your invoice and check its payment status before starting another payment.',
  PAYMENT_SESSION_EXPIRED: 'This payment link has expired. Check your invoice to see whether payment was received before requesting a new link.',
  PAYMENT_SESSION_UNAVAILABLE: 'We could not open this payment link. Check your invoice and its payment status before trying again.',
  PAYMENT_AMOUNT_UNSUPPORTED: 'Online payment is not available for this invoice amount. Please contact the facility for help.',
  PAYMENT_CALLBACK_DUPLICATE: 'A payment update has already been received. Check your invoice for the latest payment status before paying again.',
  RATE_LIMIT_EXCEEDED: 'You have made several requests in a short time. Please wait a moment before trying again.',
  EXTERNAL_PROVIDER_UNAVAILABLE: 'This service is temporarily unavailable. Please try again later. If you have just paid, check your invoice before making another payment.',
  EXTERNAL_PROVIDER_ERROR: 'We could not confirm the result with this service. Check the latest details before trying again, or contact support for help.',
}

const FIELD_MESSAGES: Record<string, string> = {
  fullName: 'Check your full name.',
  phoneNumber: 'Check your phone number.',
  email: 'Enter a valid email address.',
  password: 'Check your password. Use 8–64 characters; if it contains many symbols or accented characters, try a shorter password.',
  confirmPassword: 'Re-enter the same password in both password fields.',
  cccd: 'Enter a valid, unused 12-digit National ID / CCCD (0–9).',
  startMonth: 'Choose a check-in month after the current month.',
  endMonth: 'Choose a check-out month that is the same as or later than your check-in month.',
  newEndMonth: 'Choose an end month after your current contract ends.',
  visitDate: 'Check the visit date and choose one within the allowed period.',
  reservationVisitDate: 'Choose a handover date within the allowed period for your reservation.',
  reason: 'Enter a reason before continuing.',
  category: 'Choose a support category from the available options.',
  description: 'Check your request description. Use no more than 1,000 characters.',
  storageDescription: 'Describe the items you want to store using no more than 1,000 characters.',
  preferredMode: 'Choose Private Storage or Public Storage.',
  facilityId: 'Choose a facility from the available options.',
  unitTypeId: 'Choose a storage type from the available options.',
  contractId: 'Return to My Contracts and select the contract again.',
}

export function isUnavailableError(error: ApiErrorPresentation): boolean {
  return error.unavailable === true || error.api?.code === 'ENDPOINT_NOT_IMPLEMENTED' || error.api?.status === 501
}

export function customerErrorFeedback(error: ApiErrorPresentation) {
  const code = error.api?.code
  const status = error.api?.status
  let title = 'Unable to continue'
  let message = error.message
  let details = error.details

  if (error.connectionFailure) {
    title = 'Connection interrupted'
    message = 'We could not confirm the result. Check your connection and the latest details before trying again.'
    details = []
  } else if (error.api) {
    title = code === 'AUTH_INVALID_CREDENTIALS' ? 'We could not sign you in' : status === 401 ? 'Please sign in again' :
      status === 403 ? 'Access unavailable' : status === 404 ? 'Item not found' :
        code === 'VALIDATION_ERROR' || status === 400 ? 'Please check your information' :
          status === 409 ? 'Please check the latest details' : 'Something went wrong'
    const fallback = status === 401 ? MESSAGES.UNAUTHORIZED : status === 403 ? MESSAGES.FORBIDDEN :
      status === 404 ? MESSAGES.RESOURCE_NOT_FOUND : status === 400 ? 'We could not use this information. Please check your entries and try again.' :
        status === 409 ? 'These details have changed. Refresh the page and check the latest information before trying again.' :
          status === 429 ? MESSAGES.RATE_LIMIT_EXCEEDED : 'We could not complete this step. Check the latest details before trying again, or contact support if the problem continues.'
    message = error.customerMessage ?? (code && Object.hasOwn(MESSAGES, code) ? MESSAGES[code] : undefined) ?? fallback
    // Field keys are matched case-insensitively for the documented camelCase
    // DTOs and ASP.NET model-state keys. Raw messages/unknown paths stay private.
    details = code === 'VALIDATION_ERROR' ? [...new Set(error.api.fields.flatMap(field => {
      const key = Object.keys(FIELD_MESSAGES).find(name => name.toLowerCase() === field.toLowerCase())
      return key ? [FIELD_MESSAGES[key]] : []
    }))] : []
    if (code === 'VALIDATION_ERROR' && details.length === 0) message = 'Please check the information you entered and try again.'
  }
  return { title, message, details: [...new Set(details)] }
}
