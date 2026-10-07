import { useState } from 'react'
import { getCurrentBusinessMonth } from '../features/reservations/reservationValidation'
import { FlowIcon } from './FlowIcon'

interface CalendarDateFieldProps {
  id: string
  label: string
  value: string
  initialMonth?: string
  disabled?: boolean
  onChange(value: string): void
}

function moveMonth(month: string, step: number): string {
  const [year, monthNumber] = month.split('-').map(Number)
  const date = new Date(Date.UTC(year, monthNumber - 1 + step, 1))
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}`
}

export function CalendarDateField({ id, label, value, initialMonth, disabled = false, onChange }: CalendarDateFieldProps) {
  const [month, setMonth] = useState(() => value.slice(0, 7) || initialMonth || getCurrentBusinessMonth())
  const [year, monthNumber] = month.split('-').map(Number)
  const offset = (new Date(Date.UTC(year, monthNumber - 1, 1)).getUTCDay() + 6) % 7
  const days = new Date(Date.UTC(year, monthNumber, 0)).getUTCDate()
  const monthLabel = new Intl.DateTimeFormat('en-GB', {
    month: 'long', year: 'numeric', timeZone: 'UTC',
  }).format(new Date(Date.UTC(year, monthNumber - 1, 1)))

  return (
    <div className="flow-date-card">
      <div className="form-field">
        <label htmlFor={id}>{label}</label>
        <input
          id={id}
          type="date"
          value={value}
          disabled={disabled}
          onChange={(event) => {
            onChange(event.target.value)
            if (event.target.value) setMonth(event.target.value.slice(0, 7))
          }}
        />
      </div>
      <div className="calendar-heading">
        <button type="button" disabled={disabled} aria-label="Previous Month" onClick={() => setMonth(moveMonth(month, -1))}><FlowIcon name="calendar-prev" /></button>
        <strong>{monthLabel}</strong>
        <button type="button" disabled={disabled} aria-label="Next Month" onClick={() => setMonth(moveMonth(month, 1))}><FlowIcon name="calendar-next" /></button>
      </div>
      <div className="calendar-grid" aria-label={`Calendar for ${monthLabel}`}>
        {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map((day) => <span key={day}>{day}</span>)}
        {Array.from({ length: offset }, (_, index) => <span key={`blank-${index}`} />)}
        {Array.from({ length: days }, (_, index) => {
          const date = `${month}-${String(index + 1).padStart(2, '0')}`
          return <button key={date} type="button" disabled={disabled} aria-label={`Select ${date}`} aria-pressed={value === date} onClick={() => onChange(date)}>{index + 1}</button>
        })}
      </div>
      <p className="calendar-selected">Selected Date: <strong>{value || 'Not Selected'}</strong></p>
    </div>
  )
}
