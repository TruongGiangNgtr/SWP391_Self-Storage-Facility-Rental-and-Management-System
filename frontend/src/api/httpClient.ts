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
    // OD-09/10: customer copy is driven by stable code+field, not raw server messages.
    this.errors = status === 400 && payload.code === 'VALIDATION_ERROR' &&
      payload.errors && Object.hasOwn(payload.errors, 'cccd')
      ? { ...payload.errors, cccd: ['Enter a valid 12-digit National ID / CCCD (0–9) that is not already in use.'] }
      : payload.errors
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

async function request<T>(path: string, options: RequestInit = {}, authenticated = true, asFile = false): Promise<T> {
  const headers = new Headers(options.headers)
  const token = authenticated ? authToken.get() : null

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
  // Failed downloads still use the standard JSON error/401 handling below.
  if (response.ok && asFile) return await response.blob() as T
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
  getFile(path: string, signal?: AbortSignal): Promise<Blob> {
    return request<Blob>(path, { signal }, true, true)
  },
  // Public projections are independent of the session; a public 401 must not
  // clear a Customer's valid private session while BE alignment is pending.
  getPublic<T>(path: string, signal?: AbortSignal): Promise<T> {
    return request<T>(path, { signal }, false)
  },

  postPublic<T>(path: string, body: unknown, signal?: AbortSignal): Promise<T> {
    return request<T>(path, {
      method: 'POST',
      body: JSON.stringify(body),
      signal,
    }, false)
  },

  get<T>(path: string, signal?: AbortSignal): Promise<T> {
    return request<T>(path, { signal })
  },

  post<T>(path: string, body?: unknown, headers?: HeadersInit): Promise<T> {
    return request<T>(path, {
      method: 'POST',
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  },

  postForm<T>(path: string, body: FormData): Promise<T> {
    return request<T>(path, {
      method: 'POST',
      body,
    })
  },

  delete<T = void>(path: string): Promise<T> {
    return request<T>(path, { method: 'DELETE' })
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
