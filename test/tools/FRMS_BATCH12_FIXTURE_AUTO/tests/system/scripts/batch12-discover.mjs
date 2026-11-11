#!/usr/bin/env node
/** FRMS Batch 12 fixture discovery. Only POST login and GET calls; NEVER edits the DB. */
const env = process.env;
const base = (env.FRMS_API_URL || 'http://127.0.0.1:5164').replace(/\/+$/, '');
const fixtures = {};
const roles = {};
const notes = [];
const uuid = x => typeof x === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(x);
const string = x => typeof x === 'string' && Boolean(x.trim()) && !/[\r\n]/.test(x);
const set = (key, value) => {
  if (!env[key] && ((key.endsWith('_ID') && uuid(value)) || (!key.endsWith('_ID') && string(value)))) fixtures[key] = value;
};
const err = msg => notes.push(msg);
const ymd = (now = new Date()) => {
  const p = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(now);
  const part = type => p.find(item => item.type === type)?.value;
  return `${part('year')}-${part('month')}-${part('day')}`;
};
const addDate = (s, days) => {
  const d = new Date(`${s}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
};
const nextMonth = () => {
  const d = new Date(`${ymd().slice(0, 7)}-01T12:00:00Z`);
  d.setUTCMonth(d.getUTCMonth() + 1);
  return d.toISOString().slice(0, 7);
};
async function http(path, token, method = 'GET', body) {
  const ctl = new AbortController();
  const timer = setTimeout(() => ctl.abort(), 12000);
  try {
    const response = await fetch(`${base}/api/v1${path}`, {
      method, headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      }, body: body === undefined ? undefined : JSON.stringify(body), signal: ctl.signal,
    });
    const payload = response.ok ? await response.json().catch(() => null) : null;
    return { status: response.status, payload };
  } catch { return { status: 0, payload: null }; }
  finally { clearTimeout(timer); }
}
async function login(role, id, password) {
  if (!id || !password) { err(`${role}: credentials not configured`); return null; }
  const kind = role === 'CUSTOMER' ? 'customer' : 'employee';
  const body = kind === 'customer' ? { phoneNumber: id, password } : { email: id, password };
  const result = await http(`/auth/${kind}/login`, null, 'POST', body);
  const token = result.payload?.data?.accessToken;
  if (result.status !== 200 || !string(token)) { err(`${role}: demo login did not succeed`); return null; }
  const verified = await http('/auth/me', token);
  const user = verified.payload?.data;
  if (verified.status !== 200 || user?.role !== role || user?.status !== 'ACTIVE' || !uuid(user.userAccountId)) {
    err(`${role}: /auth/me role, status or identity not verified`); return null;
  }
  return { token, user };
}
/** A null result means incomplete/unverifiable pagination; never assume it's an empty list. */
async function list(path, token, limit = 30) {
  const data = [];
  for (let page = 1; page <= limit; page++) {
    const separator = path.includes('?') ? '&' : '?';
    const response = await http(`${path}${separator}page=${page}&pageSize=20`, token);
    const items = response.payload?.data;
    const pager = response.payload?.pagination;
    if (response.status !== 200 || !Array.isArray(items) || !Number.isInteger(pager?.totalPages)) return null;
    data.push(...items);
    if (page >= pager.totalPages) return data;
  }
  return null;
}
// Do not spend minutes attempting six logins if the backend is not running.
try {
  const ctl = new AbortController();
  const timer = setTimeout(() => ctl.abort(), 3500);
  const live = await fetch(`${base}/health`, { signal: ctl.signal }).catch(() => null);
  clearTimeout(timer);
  if (!live || live.status !== 200) {
    console.log(JSON.stringify({ fixtures, roles, notes: ['API /health is unavailable; start the launcher/backend first'] }));
    process.exit(0);
  }
} catch {
  console.log(JSON.stringify({ fixtures, roles, notes: ['API /health is unavailable'] }));
  process.exit(0);
}
const defaultPass = env.FRMS_TEST_DEMO_PASSWORD || '11111111';
const c1 = await login('CUSTOMER', env.FRMS_TEST_CUSTOMER_1_PHONE || '0901000001', env.FRMS_TEST_CUSTOMER_1_PASSWORD || defaultPass);
const c2 = await login('CUSTOMER', env.FRMS_TEST_CUSTOMER_2_PHONE || '0901000002', env.FRMS_TEST_CUSTOMER_2_PASSWORD || defaultPass);
const staff = await login('FACILITY_STAFF', env.FRMS_TEST_STAFF_01_EMAIL || 'staff.f01@example.com', env.FRMS_TEST_STAFF_PASSWORD || defaultPass);
const manager = await login('FACILITY_MANAGER', env.FRMS_TEST_MANAGER_01_EMAIL || 'manager.f01@example.com', env.FRMS_TEST_MANAGER_PASSWORD || defaultPass);
const bom = await login('BUSINESS_OPERATIONS_MANAGER', env.FRMS_TEST_BOM_EMAIL || 'bom@example.com', env.FRMS_TEST_BOM_PASSWORD || defaultPass);
const admin = await login('SYSTEM_ADMINISTRATOR', env.FRMS_TEST_ADMIN_EMAIL || 'admin@example.com', env.FRMS_TEST_ADMIN_PASSWORD || defaultPass);
Object.assign(roles, { CUSTOMER:!!c1, CUSTOMER_2:!!c2, FACILITY_STAFF:!!staff, FACILITY_MANAGER:!!manager, BUSINESS_OPERATIONS_MANAGER:!!bom, SYSTEM_ADMINISTRATOR:!!admin });

let reservations1 = null;
if (c1) {
  reservations1 = await list('/reservations', c1.token);
  const suitable = reservations1?.find(x => uuid(x.reservationId) &&
    Number.isFinite(x.depositAmount) && Number.isFinite(x.lockedRentalPrice) &&
    x.depositAmount === x.lockedRentalPrice);
  if (suitable) set('FRMS_TEST_OWN_RESERVATION_ID', suitable.reservationId);
  const contracts = await list('/contracts', c1.token);
  if (contracts) {
    for (const [status, key] of [
      ['ACTIVE', 'FRMS_TEST_ACTIVE_CONTRACT_ID'], ['COMPLETED', 'FRMS_TEST_COMPLETED_CONTRACT_ID'],
      ['TERMINATED', 'FRMS_TEST_TERMINATED_CONTRACT_ID'],
    ]) { const x = contracts.find(x => x.status === status && uuid(x.contractId)); if (x) set(key, x.contractId); }
  }
  const invoices = await list('/invoices', c1.token);
  if (invoices) {
    const validReservationIds = new Set((reservations1 || []).map(x => x.reservationId));
    const deposit = invoices.find(x => x.invoiceType === 'DEPOSIT' && uuid(x.invoiceId) && validReservationIds.has(x.entityId));
    const paid = invoices.find(x => x.status === 'PAID' && uuid(x.invoiceId));
    if (deposit) set('FRMS_TEST_OWN_DEPOSIT_INVOICE_ID', deposit.invoiceId);
    if (paid) set('FRMS_TEST_OWN_PAID_INVOICE_ID', paid.invoiceId);
  }
}
if (c1 && c2 && c1.user.userAccountId !== c2.user.userAccountId) {
  const reservations = await list('/reservations', c2.token);
  const invoices = await list('/invoices', c2.token);
  const tickets = await list('/support-tickets', c2.token);
  const otherReservation = reservations?.find(x => uuid(x.reservationId));
  const otherInvoice = invoices?.find(x => uuid(x.invoiceId));
  const otherTicket = tickets?.find(x => uuid(x.supportTicketId));
  if (otherReservation) set('FRMS_TEST_CUSTOMER_2_RESERVATION_ID', otherReservation.reservationId);
  if (otherInvoice) set('FRMS_TEST_CUSTOMER_2_INVOICE_ID', otherInvoice.invoiceId);
  if (otherTicket) set('FRMS_TEST_CUSTOMER_2_SUPPORT_TICKET_ID', otherTicket.supportTicketId);
} else err('Customer 2: distinct ACTIVE owner not verified');

// The uploaded API snapshot identified Hanoi as ACTIVE; verify current availability
// rather than turning a historical ACTIVE facility into a false search assertion.
const hanoiId = 'f0000000-0000-0000-0000-000000000001';
const fac = await http(`/facilities/${hanoiId}`, null);
if (fac.status === 200 && fac.payload?.data?.facilityId === hanoiId && fac.payload.data.status === 'ACTIVE') {
  const month = nextMonth();
  const available = await list(`/facilities/${hanoiId}/unit-types?view=public&startMonth=${month}&endMonth=${month}&availableOnly=true`, null, 8);
  if (available?.some(x => Number(x.availableCapacity) > 0)) {
    set('FRMS_TEST_HANOI_FACILITY_ID', hanoiId);
    set('FRMS_TEST_HANOI_FACILITY_NAME', fac.payload.data.name);
  } else err('Hanoi: ACTIVE facility present but next-month available capacity not confirmed');
} else err('Hanoi: current public facility detail not verified');

const checkedDates = [0, 1, -1, 2, -2, 3, -3, 7, -7, 14, -14, 21, -21, 30, -30, 45, -45, 60, -60];
let workDate = null, workList = null;
if (staff) {
  const today = ymd();
  for (const offset of checkedDates) {
    const date = addDate(today, offset);
    const rows = await list(`/staff/work-items?date=${date}`, staff.token, 15);
    if (!rows) continue;
    const own = rows.find(x => uuid(x.referenceId));
    if (own && !workDate) {
      workDate = date; workList = rows;
      set('FRMS_TEST_STAFF_SCOPE_DATE', date);
      set('FRMS_TEST_STAFF_OWN_REFERENCE_ID', own.referenceId);
    }
    const scheduled = rows.find(x => x.workType === 'RESERVATION_VISIT' && x.status === 'SCHEDULED' && uuid(x.entityId) && uuid(x.referenceId));
    if (scheduled) {
      set('FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID', scheduled.entityId);
      set('FRMS_TEST_STAFF_HANDOVER_VISIT_ID', scheduled.referenceId);
    }
    if (workDate && fixtures.FRMS_TEST_STAFF_HANDOVER_VISIT_ID) break;
  }
  const inspections = await list('/inspections', staff.token, 15);
  const owned = inspections?.find(x => x.status === 'IN_PROGRESS' && uuid(x.inspectionId) &&
    x.employeeId === staff.user.profile?.employeeId);
  if (owned) set('FRMS_TEST_STAFF_IN_PROGRESS_INSPECTION_ID', owned.inspectionId);
}
if (staff && workDate && workList) {
  const otherStaff = await login('FACILITY_STAFF', env.FRMS_TEST_STAFF_02_EMAIL || 'staff.f02@example.com', env.FRMS_TEST_STAFF_02_PASSWORD || env.FRMS_TEST_STAFF_PASSWORD || defaultPass);
  if (otherStaff && otherStaff.user.profile?.facilityId !== staff.user.profile?.facilityId) {
    const otherRows = await list(`/staff/work-items?date=${workDate}`, otherStaff.token, 15);
    const ownIds = new Set(workList.map(x => x.referenceId));
    const foreign = otherRows?.find(x => uuid(x.referenceId) && !ownIds.has(x.referenceId));
    if (foreign) set('FRMS_TEST_OTHER_FACILITY_REFERENCE_ID', foreign.referenceId);
  }
}
if (bom) {
  const types = await list('/business/unit-types', bom.token, 20);
  const count = new Map();
  for (const item of types || []) if (string(item.name)) count.set(item.name, (count.get(item.name) || 0) + 1);
  const unique = types?.find(x => string(x.name) && count.get(x.name) === 1);
  if (unique) set('FRMS_TEST_PRICING_UNIT_TYPE_NAME', unique.name);
}
if (manager && uuid(manager.user.profile?.facilityId)) {
  const fid = manager.user.profile.facilityId;
  const units = await list(`/facilities/${fid}/storage-units`, manager.token, 25);
  const availableUnits = units?.filter(x => x.status === 'AVAILABLE' && uuid(x.storageUnitId) && x.facilityId === fid) || [];
  if (availableUnits[0]) set('FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID', availableUnits[0].storageUnitId);
  const reservations = await list('/reservations', manager.token, 25);
  const matching = reservations?.find(x => x.facilityId === fid && x.status === 'CONFIRMED' &&
    x.reservationVisit?.status === 'CHECKED_IN' && uuid(x.reservationId) &&
    availableUnits.some(u => u.unitTypeId === x.unitTypeId));
  if (matching) {
    set('FRMS_TEST_ASSIGNABLE_RESERVATION_ID', matching.reservationId);
    const unit = availableUnits.find(x => x.unitTypeId === matching.unitTypeId);
    if (unit) fixtures.FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID = unit.storageUnitId;
  } else err('Manager: no checked-in CONFIRMED reservation matched an AVAILABLE physical unit');
}
// Check nonexistent login identifiers against an *exhaustive* Admin account listing.
if (admin) {
  const users = await list('/admin/users', admin.token, 50);
  if (users) {
    const phones = new Set(users.map(x => x.phoneNumber));
    const emails = new Set(users.map(x => x.email?.toLowerCase()));
    const candidatePhone = ['0999999999', '0988888888', '0977777777'].find(x => !phones.has(x));
    const candidateEmail = ['frms-batch12-unknown@example.invalid', 'frms-batch12-unknown2@example.invalid'].find(x => !emails.has(x));
    if (candidatePhone) set('FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE', candidatePhone);
    if (candidateEmail) set('FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL', candidateEmail);
  } else err('Admin: full user directory not available; negative identifiers not verified');
}
console.log(JSON.stringify({ fixtures, roles, notes }));
