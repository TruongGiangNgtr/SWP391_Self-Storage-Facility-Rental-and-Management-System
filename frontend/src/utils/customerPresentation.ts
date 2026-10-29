import type { HandoverContractDetail } from '../features/handover/contract.types'
import { formatMonthRange } from './formatters'

// Display labels only. Never change API enums, identifiers or lifecycle rules.
export const customerLabel = (value: string) => value === 'TERMINATED' ? 'Closed' : value.toLowerCase().replaceAll('_', ' ').replace(/\b\w/g, letter => letter.toUpperCase())
export const contractTitle = (contract: Pick<HandoverContractDetail, 'unitTypeName' | 'storageUnit'>) => contract.unitTypeName?.trim() || contract.storageUnit?.unitTypeName?.trim() || 'Storage Contract'
export const contractUnit = (contract: Pick<HandoverContractDetail, 'unitCode' | 'storageUnit'>) => contract.unitCode?.trim() || contract.storageUnit?.unitCode?.trim() || 'Not available'
export const contractOption = (contract: HandoverContractDetail) => `${contractTitle(contract)} · ${contractUnit(contract)} · ${formatMonthRange(contract.startMonth, contract.endMonth)} · ${customerLabel(contract.status)}`
