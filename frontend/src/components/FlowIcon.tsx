export type FlowIconName =
  | 'logo' | 'logo-footer' | 'location' | 'unit' | 'size' | 'info'
  | 'overview' | 'visits' | 'reservations' | 'invoices' | 'logout'
  | 'add' | 'clock' | 'paid' | 'calendar' | 'complete' | 'cancel'
  | 'visit-confirm' | 'visit-type' | 'close' | 'calendar-prev' | 'calendar-next'
  | 'payment-start' | 'calendar-action'

export function FlowIcon({ name }: { name: FlowIconName }) {
  return (
    <span className="flow-icon" aria-hidden="true">
      <img src={`/figma/flow1/${name}.svg`} alt="" />
    </span>
  )
}
