import { authToken } from './authToken'
import type { ApiErrorPayload } from './api.types'

export class ApiRequestError extends Error {
  readonly status: number
  readonly code: string
  readonly traceId?: string
  readonly errors?: Record<string, string[]>

  constructor(status: number, payload: ApiErrorPayload) {
    super(payload.message)
    this.name = 'ApiRequestError'
    this.status = status
    this.code = payload.code
    this.traceId = payload.traceId
    this.errors = payload.errors
  }
}

function getApiBaseUrl(): string {
  const value = import.meta.env.VITE_API_BASE_URL?.trim() || '/api/v1'

  return value.replace(/\/$/, '')
}

function isApiErrorPayload(value: unknown): value is ApiErrorPayload {
  if (!value || typeof value !== 'object') {
    return false
  }

  const candidate = value as Partial<ApiErrorPayload>
  return typeof candidate.code === 'string' && typeof candidate.message === 'string'
}

async function parseResponse(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return undefined
  }

  const text = await response.text()
  if (!text) {
    return undefined
  }

  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  const token = authToken.get()

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...options,
    headers,
  })
  const body = await parseResponse(response)

  if (!response.ok) {
    const payload = isApiErrorPayload(body)
      ? body
      : {
          code: 'UNEXPECTED_ERROR',
          message: 'An unexpected error occurred.',
        }

    if (response.status === 401 && token) {
      authToken.clear()
      window.dispatchEvent(new Event('frms:unauthorized'))
    }

    throw new ApiRequestError(response.status, payload)
  }

  return body as T
}

export const httpClient = {
  get<T>(path: string): Promise<T> {
    return request<T>(path)
  },

  post<T>(path: string, body?: unknown): Promise<T> {
    return request<T>(path, {
      method: 'POST',
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  },

  postForm<T>(path: string, body: FormData): Promise<T> {
    return request<T>(path, {
      method: 'POST',
      body,
    })
  },

  patch<T>(path: string, body: unknown): Promise<T> {
    return request<T>(path, {
      method: 'PATCH',
      body: JSON.stringify(body),
    })
  },

  put<T>(path: string, body: unknown): Promise<T> {
    return request<T>(path, {
      method: 'PUT',
      body: JSON.stringify(body),
    })
  },
}
