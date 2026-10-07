import { formatMoney } from '../utils/formatters'
import { FlowIcon } from './FlowIcon'

interface FirstMonthHandoverInfoProps {
  lockedRentalPrice: number
  handoverCompleted: boolean
  audience?: 'customer' | 'staff'
}

export function FirstMonthHandoverInfo({ lockedRentalPrice, handoverCompleted, audience = 'customer' }: FirstMonthHandoverInfoProps) {
  return (
    <section className="panel stack" aria-label="First-month rent and handover">
      <h2>{handoverCompleted ? 'Initial Rent Settled' : 'First-Month Rent'}</h2>
      <dl className="detail-list compact">
        <div><dt>First-month rental amount</dt><dd>{formatMoney(lockedRentalPrice)}</dd></div>
        <div><dt>Contract Discount</dt><dd>Not applied to the first month</dd></div>
      </dl>
      <div className="notice">
        <FlowIcon name="info" />
        {handoverCompleted
          ? 'Handover is complete. The contract records settlement of the first month. Rental invoices begin with the second rental month.'
          : audience === 'staff'
            ? 'Collect the full first-month rent offline at the facility. Completing handover acknowledges receipt through Contract creation. There is no first-month Invoice or Payment.'
            : 'Pay the first month offline at the facility. Staff acknowledges receipt when completing handover. There is no online payment or invoice for the first rental month.'}
      </div>
      {!handoverCompleted && <p className="muted">{audience === 'staff'
        ? 'The deposit is separate from the first-month rent. Verify the arriving customer and use the locked Reservation amount above.'
        : 'Your deposit is separate from the first-month rent. Bring identification to your scheduled handover visit.'}</p>}
    </section>
  )
}
