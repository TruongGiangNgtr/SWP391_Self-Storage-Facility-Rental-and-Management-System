export interface ApiResponse<T> {
  data: T
  message?: string
}

export interface Pagination {
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface ApiCollectionResponse<T> {
  data: T[]
  pagination: Pagination
}

export interface ApiErrorPayload {
  code: string
  message: string
  traceId?: string
  errors?: Record<string, string[]>
}
