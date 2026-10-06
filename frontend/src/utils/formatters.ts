const moneyFormatter = new Intl.NumberFormat('en-GB', {
  style: 'currency',
  currency: 'VND',
  currencyDisplay: 'code',
  maximumFractionDigits: 0,
})

const dateTimeFormatter = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Asia/Ho_Chi_Minh',
  dateStyle: 'short',
  timeStyle: 'short',
})

export function formatMoney(value: number): string {
  return moneyFormatter.format(value)
}

export function formatUtcDateTime(value: string): string {
  return dateTimeFormatter.format(new Date(value))
}

export function formatMonthRange(startMonth: string, endMonth: string): string {
  return `${startMonth} to ${endMonth}`
}
