const encoder = new TextEncoder()

export function getPasswordValidationMessage(password: string): string | null {
  if (password.length < 8 || password.length > 64) {
    return 'Password must contain 8 to 64 characters.'
  }

  if (encoder.encode(password).length > 72) {
    return 'Password must not exceed 72 UTF-8 bytes.'
  }

  return null
}
