const ACCESS_TOKEN_KEY = 'frms.accessToken'

export const authToken = {
  get(): string | null {
    return sessionStorage.getItem(ACCESS_TOKEN_KEY)
  },

  set(token: string): void {
    sessionStorage.setItem(ACCESS_TOKEN_KEY, token)
  },

  clear(): void {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY)
  },
}
