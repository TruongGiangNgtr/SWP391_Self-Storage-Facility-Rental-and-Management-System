import type { CurrentAccount, UpdateCustomerProfileRequest } from '../../auth/auth.types'

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
    // Missing/malformed data must never be treated as a known null value.
    cccd: cccd === null || (typeof cccd === 'string' && cccd.length <= 20) ? cccd : undefined,
  }
}

export function buildCustomerProfilePatch(snapshot: CustomerProfileSnapshot, fullName: string, cccd: string): UpdateCustomerProfileRequest {
  const patch: UpdateCustomerProfileRequest = {}
  if (fullName.trim() !== snapshot.fullName.trim()) patch.fullName = fullName.trim()
  const nationalId = cccd.trim() || null
  if (snapshot.cccd !== undefined && nationalId !== (snapshot.cccd?.trim() || null)) patch.cccd = nationalId
  return patch
}
