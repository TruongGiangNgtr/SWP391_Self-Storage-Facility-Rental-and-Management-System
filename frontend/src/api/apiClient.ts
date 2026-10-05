import type { ApiError } from '../models/api'

const API_BASE_URL = '/api/v1'

export class FrmsApiError extends Error { constructor(public readonly error: ApiError, public readonly status: number) { super(error.message) } }

export async function apiRequest<T>(path: string, init: RequestInit = {}, accessToken?: string, signal?: AbortSignal): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers, signal })
  if (!response.ok) throw new FrmsApiError(await response.json() as ApiError, response.status)
  return response.json() as Promise<T>
}
