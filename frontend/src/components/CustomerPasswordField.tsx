import { useId, useState } from 'react'
import { Eye, EyeOff, LockKeyhole } from 'lucide-react'
import { CustomerFieldError } from './CustomerFieldError'

export function CustomerPasswordField({ name, label = 'Password', autoComplete = 'current-password', describedBy, error }: {
  name: string; label?: string; autoComplete?: 'current-password' | 'new-password'; describedBy?: string; error?: string
}) {
  const id = useId()
  const [visible, setVisible] = useState(false)
  const errorId = `${id}-error`
  const description = [describedBy, error ? errorId : null].filter(Boolean).join(' ') || undefined
  return <div className="form-field"><label htmlFor={id}>{label}</label><div className="customer-auth-input"><LockKeyhole size={16} aria-hidden="true" /><input id={id} name={name} type={visible ? 'text' : 'password'} autoComplete={autoComplete} minLength={8} maxLength={64} required aria-invalid={Boolean(error)} aria-describedby={description} placeholder={label === 'Confirm Password' ? 'Re-enter your password' : 'Enter your password'} /><button className="customer-password-toggle" type="button" onClick={() => setVisible(value => !value)} aria-label={`${visible ? 'Hide' : 'Show'} ${label.toLowerCase()}`} aria-pressed={visible}>{visible ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}</button></div><CustomerFieldError id={errorId} message={error} /></div>
}
