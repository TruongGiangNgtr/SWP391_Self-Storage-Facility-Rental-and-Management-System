import { ApiRequestError } from './httpClient'

export interface ApiErrorPresentation {
  message: string
  traceId?: string
  details: string[]
}

const ERROR_MESSAGES: Record<string, string> = {
  AUTH_INVALID_CREDENTIALS: 'Incorrect sign-in details.',
  ACCOUNT_INACTIVE: 'Your account is inactive. Please contact support.',
  VALIDATION_ERROR: 'Some information is invalid. Please check your input.',
  UNAUTHORIZED: 'Your session is invalid or has expired.',
  FORBIDDEN: 'You do not have permission to perform this action.',
  ENDPOINT_NOT_IMPLEMENTED: 'This feature is not available yet. Please try again later.',
  RESOURCE_NOT_FOUND: 'The requested resource was not found.',
  FACILITY_INACTIVE: 'This facility is not accepting new reservations.',
  INVALID_MONTH_RANGE: 'The rental month range is invalid.',
  CAPACITY_NOT_AVAILABLE:
    'The selected storage type no longer has capacity for the full rental period. Please search again.',
  DEPOSIT_NOT_PAID: 'The deposit has not been recorded as paid.',
  VISIT_DATE_OUT_OF_POLICY:
    'The visit date is outside the allowed reservation policy window.',
  RESERVATION_INVALID_STATUS: 'The current reservation status does not allow this action.',
  VISIT_INVALID_STATUS: 'The current visit status does not allow this action.',
  VISIT_ENTITY_MISMATCH: 'This visit does not match the reservation. Refresh the work list.',
  UNIT_NOT_AVAILABLE: 'The Manager-selected storage unit is no longer available. Ask the Manager to select another unit.',
  UNIT_FACILITY_TYPE_MISMATCH: 'The selected unit does not match the reservation facility or storage type.',
  DISCOUNT_NOT_OWNED_BY_CUSTOMER: 'The selected discount does not belong to this customer.',
  DISCOUNT_NOT_VALID: 'The selected discount is not valid for this contract. Refresh the discount list.',
  EXTERNAL_PROVIDER_UNAVAILABLE:
    'The external service is temporarily unavailable. Please try again later.',
}

export function presentApiError(error: unknown): ApiErrorPresentation {
  if (!(error instanceof ApiRequestError)) {
    return {
      message: 'Unable to connect to the server. Please try again.',
      details: [],
    }
  }

  return {
    message: ERROR_MESSAGES[error.code] ?? 'The request could not be completed. Please try again.',
    traceId: error.traceId,
    details: error.errors ? Object.values(error.errors).flat() : [],
  }
}

export function presentValidationError(message: string): ApiErrorPresentation {
  return { message, details: [] }
}
