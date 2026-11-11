#!/usr/bin/env node
/**
 * FRMS negative-login fixture preflight.
 * Discovers NONEXISTENT identifiers only after an exhaustive, authenticated,
 * read-only listing of Admin users. Does not disclose account records or token.
 * POST /auth/employee/login may append a normal LoginHistory row.
 */
import { randomInt, randomUUID } from 'node:crypto';

const API = (process.env.FRMS_API_URL || 'http://127.0.0.1:5164').replace(/\/+$/, '');
const ADMIN_EMAIL = process.env.FRMS_TEST_ADMIN_EMAIL || 'admin@example.com';
const ADMIN_PASSWORD = process.env.FRMS_TEST_ADMIN_PASSWORD || process.env.FRMS_TEST_DEMO_PASSWORD;
const OUT_KEYS = ['FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE', 'FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL'];
const done = (verified, fixtures = {}, reason = '', count = null) => {
  console.log(JSON.stringify({ verified, fixtures, accountCount: count, reason }));
  process.exit(0); // Missing negative-fixture preconditions cause SKIP, not test failure.
};

async function request(path, { token, method = 'GET', body } = {}) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 12_000);
  try {
    const response = await fetch(`${API}/api/v1${path}`, {
      method,
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: controller.signal,
    });
    const payload = await response.json().catch(() => null);
    return { status: response.status, payload };
  } catch {
    return { status: 0, payload: null };
  } finally {
    clearTimeout(timeout);
  }
}

if (!ADMIN_PASSWORD) done(false, {}, 'Admin password not configured');
const login = await request('/auth/employee/login', {
  method: 'POST', body: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
});
const token = login.payload?.data?.accessToken;
if (login.status !== 200 || typeof token !== 'string' || !token)
  done(false, {}, 'Admin login could not be verified');
const me = await request('/auth/me', { token });
if (me.status !== 200 || me.payload?.data?.role !== 'SYSTEM_ADMINISTRATOR' || me.payload?.data?.status !== 'ACTIVE')
  done(false, {}, 'Admin role or ACTIVE status not verified');

const all = [];
let expectedTotal = null;
let expectedPages = null;
for (let page = 1; page <= 500; page++) {
  const rsp = await request(`/admin/users?page=${page}&pageSize=100`, { token });
  const data = rsp.payload?.data;
  const pagination = rsp.payload?.pagination;
  if (rsp.status !== 200 || !Array.isArray(data) || !pagination ||
      !Number.isInteger(pagination.totalItems) || !Number.isInteger(pagination.totalPages) ||
      pagination.totalItems < 0 || pagination.totalPages < 0 ||
      pagination.page !== page)
    done(false, {}, 'Admin users query or pagination is incomplete');
  if (expectedTotal === null) {
    expectedTotal = pagination.totalItems;
    expectedPages = Math.max(1, pagination.totalPages);
    if (expectedPages > 500) done(false, {}, 'Admin user list exceeds bounded scan');
  }
  if (pagination.totalItems !== expectedTotal || Math.max(1, pagination.totalPages) !== expectedPages)
    done(false, {}, 'User directory changed during scan; retry');
  if (data.some(x => !x || !Object.hasOwn(x, 'email') || !Object.hasOwn(x, 'phoneNumber') ||
      !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(x.userAccountId ?? '')))
    done(false, {}, 'User directory does not expose email and phone fields');
  all.push(...data);
  if (page >= expectedPages) break;
}
if (all.length !== expectedTotal ||
    new Set(all.map(x => x.userAccountId.toLowerCase())).size !== all.length)
  done(false, {}, 'Admin account list count did not match totalItems');

const registeredPhones = new Set(all.map(x => String(x.phoneNumber ?? '').trim()));
const registeredEmails = new Set(all.map(x => String(x.email ?? '').trim().toLowerCase()));
const candidatePhones = ['0999999999','0988888888','0977777777','0966666666'];
for (let i = 0; i < 100 && candidatePhones.every(x => registeredPhones.has(x)); i++) {
  candidatePhones.push(`09${String(randomInt(100_000_000)).padStart(8, '0')}`);
}
const requestedPhone = (process.env.FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE ?? '').trim();
const phone = requestedPhone || candidatePhones.find(x => !registeredPhones.has(x));
const candidateEmails = ['frms-batch12-unknown@example.invalid',
  `frms-batch12-${randomUUID()}@example.invalid`];
const requestedEmail = (process.env.FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL ?? '').trim().toLowerCase();
const email = requestedEmail || candidateEmails.find(x => !registeredEmails.has(x.toLowerCase()));
if (!phone || !/^0[0-9]{9}$/.test(phone) || registeredPhones.has(phone) ||
    !email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) || registeredEmails.has(email))
  done(false, {}, 'Negative identifiers invalid, registered, or not verifiably absent');
const fixtures = {
  [OUT_KEYS[0]]: phone,
  [OUT_KEYS[1]]: email,
};
done(true, fixtures, 'Verified absent from complete Admin user list', all.length);
