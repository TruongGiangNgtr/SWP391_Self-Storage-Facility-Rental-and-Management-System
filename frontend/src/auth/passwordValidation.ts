const encoder = new TextEncoder()

export function getPasswordValidationMessage(password: string): string | null {
  if (password.length < 8 || password.length > 64) {
    return 'Mật khẩu phải có từ 8 đến 64 ký tự.'
  }

  if (encoder.encode(password).length > 72) {
    return 'Mật khẩu không được vượt quá 72 byte UTF-8.'
  }

  return null
}
