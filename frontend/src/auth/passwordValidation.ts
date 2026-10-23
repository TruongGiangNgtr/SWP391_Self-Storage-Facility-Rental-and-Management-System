const encoder = new TextEncoder()

export function getPasswordValidationMessage(password: string, plainLanguage = false): string | null {
  if (password.length < 8 || password.length > 64) {
    return 'Password must contain 8 to 64 characters.'
  }

  if (encoder.encode(password).length > 72) {
    return plainLanguage ? 'Please use a shorter password. Emoji and accented letters can reach the password length limit sooner.' : 'Password must not exceed 72 UTF-8 bytes.'
  }

  return null
}
