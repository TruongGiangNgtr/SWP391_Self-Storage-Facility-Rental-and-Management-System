import { expect, test, type Page, type APIRequestContext } from '@playwright/test'

/** Batch 12: source-backed, not mock-backed. No demo secrets in logs or traces. */
export type Actor = 'CUSTOMER' | 'CUSTOMER_2' | 'FACILITY_STAFF' | 'FACILITY_STAFF_FOREIGN' | 'FACILITY_MANAGER' | 'BUSINESS_OPERATIONS_MANAGER' | 'SYSTEM_ADMINISTRATOR'
export const API = (process.env.FRMS_API_URL ?? 'http://127.0.0.1:5164').replace(/\/$/, '')

export function fixture(name: string): string {
  const value = process.env[name]?.trim()
  test.skip(!value, `FRMS_FIXTURE_MISSING: configure ${name} for this test (see BATCH12_ENVIRONMENT.md).`)
  return value || ''
}
export function isolatedWrite(): void {
  // Both explicit opts are required. Testing ASP.NET environment is NOT database isolation.
  test.skip(process.env.FRMS_TEST_DB_ISOLATED !== 'true' || process.env.FRMS_ALLOW_STATE_CHANGES !== 'YES',
    'FRMS_FIXTURE_MISSING: writes disabled. Require separate disposable Testing DB and both FRMS_TEST_DB_ISOLATED=true, FRMS_ALLOW_STATE_CHANGES=YES.')
}
export function allowAi(): void {
  test.skip(process.env.FRMS_ALLOW_AI_CALLS !== 'YES', 'FRMS_FIXTURE_MISSING: AI external calls require FRMS_ALLOW_AI_CALLS=YES.')
}
export function month(add = 0): string {
  const parts = new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Ho_Chi_Minh',year:'numeric',month:'2-digit'}).formatToParts(new Date())
  const year = Number(parts.find(p=>p.type==='year')?.value)
  const mon = Number(parts.find(p=>p.type==='month')?.value)
  const date = new Date(Date.UTC(year, mon - 1 + add, 1))
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth()+1).padStart(2,'0')}`
}
export function vnToday(addDays = 0): string {
  const parts = new Intl.DateTimeFormat('en-CA',{timeZone:'Asia/Ho_Chi_Minh',year:'numeric',month:'2-digit',day:'2-digit'}).formatToParts(new Date())
  const y=Number(parts.find(p=>p.type==='year')?.value),m=Number(parts.find(p=>p.type==='month')?.value),d=Number(parts.find(p=>p.type==='day')?.value)
  const date = new Date(Date.UTC(y,m-1,d+addDays))
  return date.toISOString().slice(0,10)
}
export function monthOffset(s: string, n: number): string {
  const [y,m] = s.split('-').map(Number)
  const date = new Date(Date.UTC(y,m-1+n,1))
  return date.toISOString().slice(0,7)
}
const actors: Record<Actor,{login:'customer'|'employee', key:string, fallback:string, password:string}> = {
  CUSTOMER:{login:'customer',key:'FRMS_TEST_CUSTOMER_1_PHONE',fallback:'0901000001',password:'FRMS_TEST_CUSTOMER_1_PASSWORD'},
  CUSTOMER_2:{login:'customer',key:'FRMS_TEST_CUSTOMER_2_PHONE',fallback:'0901000002',password:'FRMS_TEST_CUSTOMER_2_PASSWORD'},
  FACILITY_STAFF:{login:'employee',key:'FRMS_TEST_STAFF_01_EMAIL',fallback:'staff.f01@example.com',password:'FRMS_TEST_STAFF_PASSWORD'},
  FACILITY_STAFF_FOREIGN:{login:'employee',key:'FRMS_TEST_FOREIGN_STAFF_EMAIL',fallback:'',password:'FRMS_TEST_FOREIGN_STAFF_PASSWORD'},
  FACILITY_MANAGER:{login:'employee',key:'FRMS_TEST_MANAGER_01_EMAIL',fallback:'manager.f01@example.com',password:'FRMS_TEST_MANAGER_PASSWORD'},
  BUSINESS_OPERATIONS_MANAGER:{login:'employee',key:'FRMS_TEST_BOM_EMAIL',fallback:'bom@example.com',password:'FRMS_TEST_BOM_PASSWORD'},
  SYSTEM_ADMINISTRATOR:{login:'employee',key:'FRMS_TEST_ADMIN_EMAIL',fallback:'admin@example.com',password:'FRMS_TEST_ADMIN_PASSWORD'},
}
export async function bearer(request: APIRequestContext, actor: Actor): Promise<string> {
  const config = actors[actor]
  // Only Customer 1 and Staff 01 have documented demo identities in profile.md.
  // Do not guess Manager/BOM/Admin accounts or assume a shared password.
  const explicitRole = ['FACILITY_MANAGER','BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR','CUSTOMER_2','FACILITY_STAFF_FOREIGN'].includes(actor)
  const identifier = process.env[config.key]?.trim() || (explicitRole ? '' : config.fallback)
  const password = process.env[config.password] || process.env.FRMS_TEST_DEMO_PASSWORD ||
    (!explicitRole && process.env.FRMS_ENVIRONMENT === 'Testing' ? '11111111' : '')
  test.skip(!identifier, `FRMS_FIXTURE_MISSING: ${config.key} required for ${actor}; no account identity is assumed.`)
  test.skip(!password, `FRMS_FIXTURE_MISSING: ${config.password} or FRMS_TEST_DEMO_PASSWORD required for ${actor}.`)
  const data = config.login === 'customer' ? {phoneNumber:identifier,password} : {email:identifier,password}
  const response = await request.post(`${API}/api/v1/auth/${config.login}/login`, {data, timeout:30_000})
  if (response.status() !== 200) throw new Error(`FRMS_FIXTURE_MISSING: ${actor} demo login returned HTTP ${response.status()} (check seeded ACTIVE identity).`)
  const result = await response.json() as {data?:{accessToken?:string}}
  const token = result.data?.accessToken
  if (!token || token.split('.').length !== 3) throw new Error(`FRMS_FIXTURE_MISSING: ${actor} login response has no valid JWT.`)
  const me = await request.get(`${API}/api/v1/auth/me`,{headers:{Authorization:`Bearer ${token}`},timeout:30_000})
  if (me.status() !== 200) throw new Error(`FRMS_FIXTURE_MISSING: ${actor} /auth/me returned ${me.status()}.`)
  const account = await me.json() as {data?:{role?:string,status?:string}}
  const expected = actor === 'CUSTOMER_2' ? 'CUSTOMER' : actor === 'FACILITY_STAFF_FOREIGN' ? 'FACILITY_STAFF' : actor
  if (account.data?.role !== expected || account.data?.status !== 'ACTIVE') throw new Error(`FRMS_FIXTURE_MISSING: required ACTIVE ${expected} actor, received role=${account.data?.role || 'unknown'} status=${account.data?.status || 'unknown'}.`)
  return token
}
export async function visitAs(page: Page, actor: Actor, path: string): Promise<void> {
  const token = await bearer(page.request,actor)
  await page.goto(actor==='CUSTOMER'||actor==='CUSTOMER_2'?'/auth/customer/login':'/auth/employee/login')
  await page.evaluate(value => sessionStorage.setItem('frms.accessToken',value),token)
  await page.goto(path)
}
export async function getAs(request: APIRequestContext, actor:Actor, endpoint:string) {
  const token=await bearer(request,actor)
  return request.get(`${API}${endpoint}`,{headers:{Authorization:`Bearer ${token}`},timeout:30_000})
}
export function assertAuthDenied(status:number):void { expect([401,403,404], 'resource must not be exposed across role/ownership boundary').toContain(status) }
export function capturePosts(page:Page, urlPart:string):()=>number {
  let count=0
  page.on('request',req=>{ if(req.method()==='POST'&&new URL(req.url()).pathname.includes(urlPart))count++ })
  return ()=>count
}
export async function waitForHeading(page:Page,title:string) {
  await expect(page.getByRole('heading',{name:title,exact:true})).toBeVisible({timeout:45_000})
}

/** Dedicated missing-CCCD account, never modify a real seeded customer's identity just to test a form. */
export async function visitNoCccd(page:Page,path:string):Promise<void>{
  const phone=fixture('FRMS_TEST_NO_CCCD_PHONE'), password=fixture('FRMS_TEST_NO_CCCD_PASSWORD')
  const login=await page.request.post(`${API}/api/v1/auth/customer/login`,{data:{phoneNumber:phone,password}})
  if(login.status()!==200)throw new Error(`FRMS_FIXTURE_MISSING: no-CCCD customer login HTTP ${login.status()}.`)
  const result=await login.json() as {data?:{accessToken?:string}}
  const token=result.data?.accessToken
  if(!token)throw new Error('FRMS_FIXTURE_MISSING: no-CCCD account token missing.')
  await page.goto('/auth/customer/login')
  await page.evaluate(value=>sessionStorage.setItem('frms.accessToken',value),token)
  await page.goto(path)
}
export function reservationUrl():string {
  const u=new URLSearchParams({facilityId:fixture('FRMS_TEST_ACTIVE_FACILITY_ID'),unitTypeId:fixture('FRMS_TEST_AVAILABLE_UNIT_TYPE_ID'),startMonth:month(1),endMonth:month(1)})
  return `/customer/reservations/new?${u}`
}
export async function contractStatus(request:APIRequestContext,actor:Actor,id:string):Promise<{status:string,startMonth:string,endMonth:string}> {
  const response=await getAs(request,actor,`/api/v1/contracts/${encodeURIComponent(id)}`)
  if(response.status()!==200)throw new Error(`FRMS_FIXTURE_MISSING: contract details HTTP ${response.status()} for actor ${actor}.`)
  const data=(await response.json() as {data?:{status:string,startMonth:string,endMonth:string}}).data
  if(!data?.status||!data.startMonth||!data.endMonth)throw new Error('FRMS_FIXTURE_MISSING: contract state/period missing.')
  return data
}
