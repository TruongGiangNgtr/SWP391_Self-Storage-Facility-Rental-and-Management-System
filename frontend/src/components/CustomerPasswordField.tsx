import { useId, useState } from 'react'
import { Eye, EyeOff, LockKeyhole } from 'lucide-react'

export function CustomerPasswordField({ name, label = 'Password', autoComplete = 'current-password', describedBy }: {
  name: string; label?: string; autoComplete?: 'current-password' | 'new-password'; describedBy?: string
}) {
  const id = useId()
  const [visible, setVisible] = useState(false)
  return <div className="form-field"><label htmlFor={id}>{label}</label><div className="customer-auth-input"><LockKeyhole size={16} aria-hidden="true" /><input id={id} name={name} type={visible ? 'text' : 'password'} autoComplete={autoComplete} minLength={8} maxLength={64} required aria-describedby={describedBy} placeholder={label === 'Confirm Password' ? 'Re-enter your password' : 'Enter your password'} /><button className="customer-password-toggle" type="button" onClick={() => setVisible(value => !value)} aria-label={`${visible ? 'Hide' : 'Show'} ${label.toLowerCase()}`} aria-pressed={visible}>{visible ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}</button></div></div>
}
