import { httpClient } from './httpClient'
import { readTransactionCollection } from '../features/customer-transactions/transactionValidation'

export const transactionsApi = {
  async list(page = 1, pageSize = 20, signal?: AbortSignal) {
    const response = await httpClient.get<unknown>(`/customers/me/transactions?page=${page}&pageSize=${pageSize}`, signal)
    return readTransactionCollection(response, page, pageSize)
  },
}
