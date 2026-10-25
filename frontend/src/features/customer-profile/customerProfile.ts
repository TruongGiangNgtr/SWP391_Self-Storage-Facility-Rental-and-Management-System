import type { CurrentAccount, UpdateCustomerProfileRequest } from '../../auth/auth.types'

export const CCCD_INPUT_PATTERN = '[0-9]{12}'
export const CCCD_FORMAT_MESSAGE = 'Enter exactly 12 digits (0–9) for your National ID.'

export function isValidCccd(value: string): boolean {
  return value.length === 12 && /^[0-9]{12}$/.test(value)
}

export function cccdValidationMessage(value: string, required = false): string | null {
  const normalized = value.trim()
  return (!normalized && !required) || isValidCccd(normalized) ? null : CCCD_FORMAT_MESSAGE
}

export interface CustomerProfileSnapshot {
  fullName: string
  email: string
  phoneNumber: string
  cccd: string | null | undefined
}

export function readCustomerProfile(account: CurrentAccount, userAccountId: string): CustomerProfileSnapshot | null {
  if (!account || account.userAccountId !== userAccountId || account.role !== 'CUSTOMER' || account.status !== 'ACTIVE' ||
    !account.profile?.customerId || typeof account.profile.fullName !== 'string' ||
    typeof account.email !== 'string' || typeof account.phoneNumber !== 'string') return null
  const cccd = account.profile.cccd
  return {
    fullName: account.profile.fullName,
    email: account.email,
    phoneNumber: account.phoneNumber,
    // Keep legacy stored strings readable for correction; storage capacity is not format validity.
    // Missing/malformed response data must never be treated as a known null value.
    cccd: cccd === null || (typeof cccd === 'string' && cccd.length <= 20) ? cccd : undefined,
  }
}

export function buildCustomerProfilePatch(snapshot: CustomerProfileSnapshot, fullName: string, cccd: string): UpdateCustomerProfileRequest {
  const patch: UpdateCustomerProfileRequest = {}
  if (fullName.trim() !== snapshot.fullName.trim()) patch.fullName = fullName.trim()
  const nationalId = cccd.trim() || null
  // Do not rewrite an unchanged legacy value while editing an unrelated profile field.
  if (snapshot.cccd !== undefined && cccd !== (snapshot.cccd ?? '') && nationalId !== snapshot.cccd) patch.cccd = nationalId
  return patch
}
