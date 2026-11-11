import {expect,test,type APIRequestContext,type Page} from '@playwright/test'
import {API,bearer,fixture,getAs,visitAs} from './batch12-helpers'

/** Batch 13: authenticated, GET-only fixture discovery. Never seeds shared Supabase. */
export type Actor='CUSTOMER'|'CUSTOMER_2'|'FACILITY_STAFF'|'FACILITY_STAFF_FOREIGN'|'FACILITY_MANAGER'|'BUSINESS_OPERATIONS_MANAGER'|'SYSTEM_ADMINISTRATOR'
export type Unit={storageUnitId:string,facilityId:string,unitTypeId:string,unitCode:string,status:string}
export type Reservation={reservationId:string,facilityId:string,unitTypeId:string,status:string,reservationVisit?:{visitId:string,status:string,visitType:string}|null}
export type Visit={visitId:string,visitType:string,status:string,entityId:string,visitDate:string}
export type Contract={contractId:string,status:string,startMonth:string,endMonth:string}
export type Invoice={invoiceId:string,invoiceType:string,status:string,billingMonth?:string|null,baseAmount:number,discountAmount:number,amountDue:number}

export function precondition(ok:unknown,why:string):asserts ok{
  test.skip(!ok,'FRMS_FIXTURE_MISSING: '+why)
}
export async function read<T>(request:APIRequestContext,actor:Actor,path:string):Promise<T>{
  const r=await getAs(request,actor,'/api/v1'+path)
  if(r.status()>=500)throw new Error('FRMS_INFRA_UNAVAILABLE: '+actor+' GET '+path+' HTTP '+r.status())
  precondition(r.status()===200,actor+' GET '+path+' HTTP '+r.status())
  const data=(await r.json() as {data?:T}).data
  precondition(data!==undefined&&data!==null,'missing API data for '+path)
  return data
}
export async function list<T>(request:APIRequestContext,actor:Actor,path:string,maxPages=50):Promise<T[]>{
  const token=await bearer(request,actor)
  const output:T[]=[]
  const separator=path.includes('?')?'&':'?'
  for(let page=1;page<=maxPages;page++){
    const response=await request.get(API+'/api/v1'+path+separator+'page='+page+'&pageSize=100',{
      headers:{Authorization:'Bearer '+token},timeout:30000
    })
    if(response.status()>=500)throw new Error('FRMS_INFRA_UNAVAILABLE: paginated '+path+' HTTP '+response.status())
    precondition(response.status()===200,'paginated '+path+' HTTP '+response.status())
    const body=await response.json() as {data?:T[],pagination?:{totalPages?:number}}
    const total=body.pagination?.totalPages
    precondition(Array.isArray(body.data)&&Number.isInteger(total)&&Number(total)>=1&&Number(total)<=maxPages,
      'incomplete/invalid pagination on '+path)
    output.push(...body.data!)
    if(page>=total!)return output
  }
  throw new Error('FRMS_FIXTURE_MISSING: page bound exceeded for '+path)
}
export async function managerCandidate(request:APIRequestContext){
  const reservationId=fixture('FRMS_TEST_ASSIGNABLE_RESERVATION_ID')
  const facilityId=fixture('FRMS_TEST_HANOI_FACILITY_ID')
  const reservation=await read<Reservation>(request,'FACILITY_MANAGER','/reservations/'+reservationId)
  precondition(reservation.reservationId===reservationId&&reservation.facilityId===facilityId&&
    reservation.status==='CONFIRMED'&&reservation.reservationVisit?.visitType==='RESERVATION'&&
    reservation.reservationVisit?.status==='CHECKED_IN','F01 CONFIRMED CHECKED_IN reservation')
  return reservation
}
export async function allManagerUnits(request:APIRequestContext,facilityId:string){
  return list<Unit>(request,'FACILITY_MANAGER','/facilities/'+facilityId+'/storage-units',30)
}
export async function showManagerSelection(page:Page,reservation:Reservation){
  await visitAs(page,'FACILITY_MANAGER','/manager/handover-unit-selection')
  await expect(page.getByRole('heading',{name:'Available Matching Units'})).toBeVisible({timeout:45000})
  await page.getByRole('textbox',{name:'Search reservations by customer, unit type, rental period or status'}).fill(reservation.reservationId)
  const cards=page.locator('.handover-reservation-list > button')
  await expect(cards).toHaveCount(1,{timeout:45000})
  await cards.first().click()
  await expect(page.getByRole('textbox',{name:'Search available units by code or location'})).toBeEnabled({timeout:45000})
}
export async function assertNotEligible(page:Page,unit:Unit){
  const input=page.getByRole('textbox',{name:'Search available units by code or location'})
  await input.fill(unit.storageUnitId)
  await expect(page.locator('.handover-unit-card')).toHaveCount(0,{timeout:45000})
}
export async function readStaffReservation(request:APIRequestContext){
  const reservationId=fixture('FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID')
  const visitId=fixture('FRMS_TEST_STAFF_HANDOVER_VISIT_ID')
  const reservation=await read<Reservation>(request,'FACILITY_MANAGER','/reservations/'+reservationId)
  precondition(reservation.reservationId===reservationId&&reservation.reservationVisit?.visitId===visitId,
    'staff handover Reservation/Visit reference pair drifted')
  return {reservation,visitId}
}
export async function staffHandover(page:Page,reservationId:string,visitId:string,actor:Actor='FACILITY_STAFF'){
  await visitAs(page,actor,'/staff/reservations/'+reservationId+'/visits/'+visitId)
  await expect(page.getByRole('heading',{name:'Reservation Check-in & Handover'})).toBeVisible({timeout:45000})
}
export function monthLast(endMonth:string){
  const [y,m]=endMonth.split('-').map(Number)
  precondition(Number.isInteger(y)&&Number.isInteger(m)&&m>=1&&m<=12,'valid endMonth required')
  return endMonth+'-'+String(new Date(Date.UTC(y,m,0)).getUTCDate()).padStart(2,'0')
}
export async function ownActiveContract(request:APIRequestContext){
  const id=fixture('FRMS_TEST_ACTIVE_CONTRACT_ID')
  const contract=await read<Contract>(request,'CUSTOMER','/contracts/'+id)
  precondition(contract.contractId===id&&contract.status==='ACTIVE','own ACTIVE contract')
  return contract
}

/** Optional foreign Manager positive control. No identifier is trusted without a 200 GET in its own scope. */
export async function foreignManagerUnit(request:APIRequestContext){
  const email=process.env.FRMS_TEST_FOREIGN_MANAGER_EMAIL?.trim()
  const password=process.env.FRMS_TEST_FOREIGN_MANAGER_PASSWORD
  precondition(email&&password,'explicit F32 Manager identity/password required to verify cross-facility Unit')
  const login=await request.post(API+'/api/v1/auth/employee/login',{
    data:{email,password},timeout:30000})
  precondition(login.status()===200,'foreign Manager login available')
  const loginData=(await login.json() as {data?:{accessToken?:string}}).data
  const token=loginData?.accessToken
  precondition(token,'foreign Manager login returns access token')
  const headers={Authorization:'Bearer '+token}
  const me=await request.get(API+'/api/v1/auth/me',{headers,timeout:30000})
  precondition(me.status()===200,'foreign Manager profile authorized')
  const user=(await me.json() as {data?:{role?:string,status?:string,profile?:{facilityId?:string}}}).data
  const facilityId=user?.profile?.facilityId
  precondition(user?.role==='FACILITY_MANAGER'&&user.status==='ACTIVE'&&facilityId,
    'foreign actor must be ACTIVE Facility Manager with an assigned Facility')
  precondition(facilityId!==fixture('FRMS_TEST_HANOI_FACILITY_ID'),
    'foreign manager Facility must differ from F01')
  const response=await request.get(API+'/api/v1/facilities/'+facilityId+'/storage-units?page=1&pageSize=100',{
    headers,timeout:30000})
  precondition(response.status()===200,'foreign manager may list own Facility physical units')
  const data=(await response.json() as {data?:Unit[]}).data
  precondition(Array.isArray(data)&&data.length>0,'foreign Facility has real StorageUnits')
  const requested=process.env.FRMS_TEST_FOREIGN_STORAGE_UNIT_ID?.trim()
  const chosen=requested?data.find(x=>x.storageUnitId===requested):data[0]
  precondition(chosen&&chosen.facilityId===facilityId,
    'selected foreign Unit is present in Manager own-Facility API list')
  const detail=await request.get(API+'/api/v1/storage-units/'+chosen.storageUnitId,{
    headers,timeout:30000})
  precondition(detail.status()===200,'foreign Manager has detail access to chosen Unit')
  const verified=(await detail.json() as {data?:Unit}).data
  precondition(verified?.storageUnitId===chosen.storageUnitId&&verified.facilityId===facilityId,
    'foreign Unit detail facility agrees with Manager profile')
  return chosen
}
