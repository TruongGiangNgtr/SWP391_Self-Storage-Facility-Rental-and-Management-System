import { useEffect, useRef, useState } from 'react'
import { CalendarDays, ChevronLeft, ChevronRight } from 'lucide-react'
import '../styles/month-picker.css'

const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']

interface MonthPickerProps {
  id: string
  label: string
  value: string
  min: string
  describedBy?: string
  onChange(value: string): void
}

// Calendar navigation is presentation only. The caller still validates the rental range.
export function MonthPicker({ id, label, value, min, describedBy, onChange }: MonthPickerProps) {
  const root = useRef<HTMLDivElement>(null)
  const trigger = useRef<HTMLButtonElement>(null)
  const previousYearButton = useRef<HTMLButtonElement>(null)
  const nextYearButton = useRef<HTMLButtonElement>(null)
  const [open, setOpen] = useState(false)
  const [above, setAbove] = useState(false)
  const minYear = Number(min.slice(0, 4)) || 1
  const selectedYear = /^\d{4}-(0[1-9]|1[0-2])$/.test(value) ? Number(value.slice(0, 4)) : minYear
  const [year, setYear] = useState(Math.max(minYear, Math.min(selectedYear, 9999)))
  const popupId = `${id}-picker`
  const monthValue = (index: number) => `${String(year).padStart(4, '0')}-${String(index + 1).padStart(2, '0')}`
  const firstEnabled = MONTHS.findIndex((_, index) => monthValue(index) >= min)
  const hasSelection = selectedYear === year && value >= min

  function show() {
    const bounds = root.current?.getBoundingClientRect()
    setAbove(Boolean(bounds && window.innerHeight - bounds.bottom < 340 && bounds.top > window.innerHeight - bounds.bottom))
    setYear(Math.max(minYear, Math.min(selectedYear, 9999)))
    setOpen(true)
  }

  function close(restoreFocus = false) {
    setOpen(false)
    if (restoreFocus) trigger.current?.focus()
  }

  function changeYear(nextYear: number) {
    // Keep focus in the popup when the arrow being used becomes disabled.
    if (nextYear <= minYear) nextYearButton.current?.focus()
    else if (nextYear >= 9999) previousYearButton.current?.focus()
    setYear(Math.max(minYear, Math.min(nextYear, 9999)))
  }

  useEffect(() => {
    if (!open) return
    function outside(event: PointerEvent) {
      if (event.target instanceof Node && !root.current?.contains(event.target)) setOpen(false)
    }
    const onResize = () => setOpen(false)
    document.addEventListener('pointerdown', outside)
    window.addEventListener('resize', onResize)
    return () => {
      document.removeEventListener('pointerdown', outside)
      window.removeEventListener('resize', onResize)
    }
  }, [open])

  return <div className="public-month month-picker" ref={root}
    onBlur={event => { if (!event.currentTarget.contains(event.relatedTarget)) close() }}
    onKeyDown={event => { if (open && event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(true) } }}>
    <input id={id} className="month-picker-input" type="month" min={min} value={value} aria-describedby={describedBy}
      onChange={event => onChange(event.target.value)}
      onKeyDown={event => { if ((event.altKey && event.key === 'ArrowDown') || event.key === 'F4') { event.preventDefault(); show() } }} />
    <button ref={trigger} className="month-picker-trigger" type="button" aria-label={`Choose ${label.toLowerCase()}`} aria-haspopup="dialog" aria-expanded={open} aria-controls={open ? popupId : undefined}
      onClick={() => { if (open) close(); else show() }}><CalendarDays size={18} aria-hidden="true" /></button>
    {open && <div id={popupId} className={`month-picker-popup ${above ? 'month-picker-above' : ''}`} role="dialog" aria-label={`${label} picker`}>
      <div className="month-picker-year">
        <button ref={previousYearButton} className="month-picker-year-arrow" type="button" aria-label="Previous year" disabled={year <= minYear} onClick={() => changeYear(year - 1)}><ChevronLeft size={20} aria-hidden="true" /></button>
        <strong aria-live="polite">{year}</strong>
        <button ref={nextYearButton} className="month-picker-year-arrow" type="button" aria-label="Next year" disabled={year >= 9999} onClick={() => changeYear(year + 1)}><ChevronRight size={20} aria-hidden="true" /></button>
      </div>
      <div className="month-picker-months" role="group" aria-label={`Months in ${year}`}>
        {MONTHS.map((month, index) => {
          const candidate = monthValue(index)
          const selected = candidate === value
          return <button key={month} className="month-picker-month" type="button" aria-label={`${month} ${year}`} aria-pressed={selected} disabled={candidate < min}
            autoFocus={candidate >= min && (selected || (!hasSelection && index === firstEnabled))}
            onClick={() => { onChange(candidate); close(true) }}>{month.slice(0, 3)}</button>
        })}
      </div>
      <div className="month-picker-footer">
        <button type="button" onClick={() => { onChange(''); close(true) }}>Clear</button>
        <button type="button" onClick={() => close(true)}>Close</button>
      </div>
    </div>}
  </div>
}
