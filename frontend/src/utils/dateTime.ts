export const BUSINESS_TIME_ZONE = 'Asia/Ho_Chi_Minh'
export function formatBusinessTimestamp(utcIsoTimestamp: string, locale = 'vi-VN') { return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short', timeZone: BUSINESS_TIME_ZONE }).format(new Date(utcIsoTimestamp)) }
export function assertMonthValue(value: string) { if (!/^\d{4}-(0[1-9]|1[0-2])$/.test(value)) throw new Error('Month value must use YYYY-MM.'); return value }
