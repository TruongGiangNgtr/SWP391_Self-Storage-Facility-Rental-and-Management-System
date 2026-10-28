import { Check } from 'lucide-react'
import '../styles/booking-flow.css'

const steps = ['Find storage', 'Review selection', 'Pay deposit', 'Schedule handover']

export function BookingJourney({ step }: { step: 0 | 1 | 2 | 3 | 4 }) {
  return (
    <nav className="booking-journey" aria-label="Reservation steps">
      <ol>
        {steps.map((label, index) => (
          <li key={label} className={index === step ? 'is-current' : index < step ? 'is-previous' : ''}
            aria-current={index === step ? 'step' : undefined}>
            <span className="booking-step-number" aria-hidden="true">{index < step ? <Check size={16} /> : index + 1}</span>
            <span>{label}{index < step && <span className="booking-step-sr"> — completed</span>}</span>
          </li>
        ))}
      </ol>
    </nav>
  )
}
