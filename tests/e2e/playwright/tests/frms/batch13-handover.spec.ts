import {expect,test,type APIRequestContext,type Page} from '@playwright/test'
import {API,bearer,fixture,getAs,visitAs} from './batch12-helpers'
import {allManagerUnits,foreignManagerUnit,assertNotEligible,list,managerCandidate,precondition,read,readStaffReservation,
  showManagerSelection,staffHandover,type Reservation,type Unit} from './batch13-helpers'

test.setTimeout(120_000)
test.use({trace:'off'})

type InvalidClass='IN_USE'|'MAINTENANCE'|'WRONG_TYPE'
async function invalidUnit(request:APIRequestContext,cls:InvalidClass){
  const reservation=await managerCandidate(request)
  const units=await allManagerUnits(request,reservation.facilityId)
  const eligible=units.find(u=>u.status==='AVAILABLE'&&u.unitTypeId===reservation.unitTypeId)
  precondition(eligible,'positive AVAILABLE same-type F01 unit is required before a negative partition check')
  const unit=units.find(u=>u.storageUnitId!==eligible.storageUnitId&&
    (cls==='WRONG_TYPE'?u.status==='AVAILABLE'&&u.unitTypeId!==reservation.unitTypeId:
      u.status===cls&&u.unitTypeId===reservation.unitTypeId))
  precondition(unit,cls+' unit in same Facility/appropriate UnitType')
  return {reservation,unit,eligible}
}
async function absentInManager(request:APIRequestContext,page:Page,cls:InvalidClass){
  const {reservation,unit,eligible}=await invalidUnit(request,cls)
  await showManagerSelection(page,reservation)
  await expect(page.locator('.handover-unit-card').filter({hasText:eligible.unitCode})).toBeVisible()
  await assertNotEligible(page,unit)
  return {reservation,unit}
}
async function foreignDenied(request:APIRequestContext,page:Page){
  const reservationId=fixture('FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID')
  const visitId=fixture('FRMS_TEST_STAFF_HANDOVER_VISIT_ID')
  const [f01,f32]=await Promise.all([bearer(request,'FACILITY_STAFF'),bearer(request,'FACILITY_STAFF_FOREIGN')])
  const auth=async(token:string)=> {
    const r=await request.get(API+'/api/v1/auth/me',{headers:{Authorization:'Bearer '+token}})
    precondition(r.status()===200,'Staff authenticated profile GET')
    return (await r.json() as {data?:{status?:string,profile?:{facilityId?:string}}}).data
  }
  const [a,b]=await Promise.all([auth(f01),auth(f32)])
  precondition(a?.status==='ACTIVE'&&b?.status==='ACTIVE'&&a.profile?.facilityId&&b.profile?.facilityId&&
    a.profile.facilityId!==b.profile.facilityId,'different ACTIVE assigned Facilities')
  const own=await request.get(API+'/api/v1/visits/'+visitId,{headers:{Authorization:'Bearer '+f01}})
  precondition(own.status()===200,'F01 has legitimate reservation Visit access')
  const v=(await own.json() as {data?:{visitId?:string,entityId?:string}}).data
  precondition(v?.visitId===visitId&&v.entityId===reservationId,'F01 Visit links target Reservation')
  const denied=await request.get(API+'/api/v1/visits/'+visitId,{headers:{Authorization:'Bearer '+f32}})
  expect([403,404],'F32 cannot read F01 Visit').toContain(denied.status())
  await visitAs(page,'FACILITY_STAFF_FOREIGN','/staff/reservations/'+reservationId+'/visits/'+visitId)
  await expect(page.getByRole('button',{name:'Check In Customer'})).toHaveCount(0)
  await expect(page.getByRole('button',{name:'Complete Handover',exact:true})).toHaveCount(0)
}
async function completedHandover(request:APIRequestContext,page:Page){
  const all=await list<Reservation>(request,'FACILITY_MANAGER','/reservations')
  const facility=fixture('FRMS_TEST_HANOI_FACILITY_ID')
  const complete=all.find(r=>r.facilityId===facility&&r.status==='COMPLETED'&&r.reservationVisit?.visitId)
  precondition(complete,'COMPLETED reservation with Visit owned by F01')
  await staffHandover(page,complete.reservationId,complete.reservationVisit!.visitId)
  await expect(page.getByText('This reservation is already completed. No additional first-month payment or handover action is required.')).toBeVisible({timeout:45_000})
  await expect(page.getByRole('button',{name:'Complete Handover',exact:true})).toHaveCount(0)
}

test('FRMS-ST-0064 - IN_USE matching unit is excluded from manager assignment',async({request,page})=>{
  await absentInManager(request,page,'IN_USE')
  await expect(page.getByRole('button',{name:'Save assignment'})).toHaveCount(0)
})
test('FRMS-ST-0064-B - MAINTENANCE matching unit cannot be selected',async({request,page})=>{
  await absentInManager(request,page,'MAINTENANCE')
  await expect(page.getByRole('button',{name:'Save assignment'})).toHaveCount(0)
})
test('FRMS-ST-0066 - mismatched UnitType is excluded from assignment',async({request,page})=>{
  const {unit,reservation}=await absentInManager(request,page,'WRONG_TYPE')
  expect(unit.unitTypeId).not.toBe(reservation.unitTypeId)
  await expect(page.getByRole('button',{name:'Save assignment'})).toHaveCount(0)
})
test('FRMS-ST-0071 - checked in reservation without ACTIVE assignment cannot hand over',async({request,page})=>{
  const all=await list<Reservation>(request,'FACILITY_MANAGER','/reservations')
  const facility=fixture('FRMS_TEST_HANOI_FACILITY_ID')
  let found:Reservation|undefined
  for(const candidate of all.filter(r=>r.facilityId===facility&&r.status==='CONFIRMED'&&
    r.reservationVisit?.visitType==='RESERVATION'&&r.reservationVisit.status==='CHECKED_IN')){
    const response=await getAs(request,'FACILITY_MANAGER','/api/v1/reservations/'+candidate.reservationId+'/handover-unit-assignment')
    if(response.status()!==200)continue
    const assignment=(await response.json() as {data?:{status?:string}|null}).data
    if(!assignment||assignment.status!=='ACTIVE'){found=candidate;break}
  }
  precondition(found,'genuine checked-in CONFIRMED F01 Reservation lacking ACTIVE assignment')
  const staffVisit=await getAs(request,'FACILITY_STAFF','/api/v1/visits/'+found.reservationVisit!.visitId)
  precondition(staffVisit.status()===200,'selected CHECKED_IN unassigned Visit is readable by F01 Staff')
  const current=(await staffVisit.json() as {data?:{visitId?:string,status?:string,entityId?:string}}).data
  precondition(current?.visitId===found.reservationVisit!.visitId&&current.status==='CHECKED_IN'&&
    current.entityId===found.reservationId,'Staff Visit detail matches unassigned reservation')
  await staffHandover(page,found.reservationId,found.reservationVisit!.visitId)
  await expect(page.getByRole('region',{name:'Reservation visit'})).toContainText(/CHECKED_IN|Checked In/i,{timeout:45_000})
  await expect(page.getByText('No valid active assignment. Ask the Manager to save a unit, then refresh details.')).toBeVisible({timeout:45_000})
  await expect(page.getByRole('button',{name:'Complete Handover',exact:true})).toBeDisabled()
})
test('FRMS-ST-0220 - checked in Visit has no duplicate check-in action',async({request,page})=>{
  const {reservation,visitId}=await readStaffReservation(request)
  precondition(reservation.status==='CONFIRMED'&&reservation.reservationVisit?.status==='CHECKED_IN',
    'F01 Reservation Visit must be CHECKED_IN')
  const staffVisit=await getAs(request,'FACILITY_STAFF','/api/v1/visits/'+visitId)
  precondition(staffVisit.status()===200,'F01 can read the target CHECKED_IN Visit')
  const current=(await staffVisit.json() as {data?:{visitId?:string,status?:string,entityId?:string}}).data
  precondition(current?.visitId===visitId&&current.status==='CHECKED_IN'&&current.entityId===reservation.reservationId,
    'Staff Visit remains CHECKED_IN and belongs to nominated F01 reservation')
  await staffHandover(page,reservation.reservationId,visitId)
  await expect(page.getByRole('region',{name:'Reservation visit'})).toContainText(/CHECKED_IN|Checked In/i,{timeout:45_000})
  await expect(page.getByText('The customer is checked in. Continue with the Manager-selected unit and offline receipt.')).toBeVisible({timeout:45_000})
  await expect(page.getByRole('button',{name:'Check In Customer'})).toHaveCount(0)
})
test('FRMS-ST-0223 - IN_USE unit cannot be used to start manager handover selection',async({request,page})=>{
  const {unit}=await absentInManager(request,page,'IN_USE')
  await expect(page.locator('.handover-unit-list').getByText(unit.unitCode,{exact:true})).toHaveCount(0)
})
test('FRMS-ST-0223-B - wrong UnitType cannot be assigned for handover',async({request,page})=>{
  const {unit,reservation}=await absentInManager(request,page,'WRONG_TYPE')
  expect(unit.unitTypeId).not.toBe(reservation.unitTypeId)
  await expect(page.locator('.handover-unit-card-selected')).toHaveCount(0)
})
test('FRMS-ST-0223-C - unit from another facility cannot enter F01 selection',async({request,page})=>{
  const reservation=await managerCandidate(request)
  const foreign=await foreignManagerUnit(request)
  const response=await getAs(request,'FACILITY_MANAGER','/api/v1/storage-units/'+foreign.storageUnitId)
  expect([403,404],'F01 manager may not read confirmed foreign physical unit details').toContain(response.status())
  await showManagerSelection(page,reservation)
  const search=page.getByRole('textbox',{name:'Search available units by code or location'})
  await search.fill(foreign.storageUnitId)
  await expect(page.locator('.handover-unit-card')).toHaveCount(0)
})
test('FRMS-ST-0224 - F32 Staff cannot check in F01 reservation Visit',async({request,page})=>{
  await foreignDenied(request,page)
  await expect(page.getByRole('button',{name:'Check In Customer'})).toHaveCount(0)
})
test('FRMS-ST-0224-B - F32 Staff cannot complete F01 handover',async({request,page})=>{
  await foreignDenied(request,page)
  await expect(page.getByRole('button',{name:'Complete Handover',exact:true})).toHaveCount(0)
})
test('FRMS-ST-0225 - completed handover has no second completion action',async({request,page})=>{
  await completedHandover(request,page)
})
test('FRMS-ST-0226 - Customer role cannot use Staff handover route',async({page})=>{
  const reservationId=fixture('FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID')
  const visitId=fixture('FRMS_TEST_STAFF_HANDOVER_VISIT_ID')
  await visitAs(page,'CUSTOMER','/staff/reservations/'+reservationId+'/visits/'+visitId)
  await expect(page).not.toHaveURL(/\/staff\/reservations\/[^/]+\/visits\/[^/?]+(?:[?#]|$)/,{timeout:45000})
  await expect(page.getByRole('button',{name:'Complete Handover',exact:true})).toHaveCount(0)
})
