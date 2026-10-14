export function CustomerFieldError({ id, message }: { id: string; message?: string | null }) {
  return message ? <p className="field-error" id={id} role="alert">{message}</p> : null
}
