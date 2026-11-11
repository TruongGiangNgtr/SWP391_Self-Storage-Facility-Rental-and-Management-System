import {expect,test,type APIRequestContext} from '@playwright/test'
import {API,bearer,fixture,getAs,visitAs,waitForHeading} from './batch12-helpers'

test.setTimeout(120_000)
test.use({trace:'off'})
async function managerGetUnit(request:APIRequestContext,id:string){
  const response=await getAs(request,'FACILITY_MANAGER',`/api/v1/storage-units/${id}`)
  if(response.status()!==200)throw new Error(`FRMS_FIXTURE_MISSING: manager unit detail HTTP ${response.status()}.`)
  return (await response.json() as {data?:{storageUnitId?:string,status?:string,unitTypeId?:string,facilityId?:string,unitCode?:string}}).data!
}

async function openHandover(page:import('@playwright/test').Page){
  const reservationId=fixture('FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID'),visitId=fixture('FRMS_TEST_STAFF_HANDOVER_VISIT_ID')
  await visitAs(page,'FACILITY_STAFF',`/staff/reservations/${reservationId}/visits/${visitId}`)
  await waitForHeading(page,'Reservation Check-in & Handover')
  return {reservationId,visitId}
}

test('FRMS-ST-0057 - Staff work list shows visit rows, statuses and actions',async({page})=>{
  const reference=fixture('FRMS_TEST_STAFF_OWN_REFERENCE_ID')
  const date=fixture('FRMS_TEST_STAFF_SCOPE_DATE')
  await visitAs(page,'FACILITY_STAFF','/staff/work-list')
  const filter=page.getByRole('region',{name:'Work-list filters'})
  await expect(filter).toBeVisible()
  await filter.getByLabel('Business date (GMT+7)').fill(date)
  const row=page.getByRole('row').filter({hasText:reference})
  await expect(row).toBeVisible({timeout:45000})
  for(const column of ['Customer','Scheduled','Status','Related rental'])
    await expect(page.getByRole('columnheader',{name:column})).toBeVisible()
  await expect(row.getByRole('link')).toBeVisible()
})
test('FRMS-ST-0058 - Manager handover assignment page displays relevant lists',async({page})=>{
  await visitAs(page,'FACILITY_MANAGER','/manager/handover-unit-selection')
  await waitForHeading(page,'Select Handover Unit')
  await expect(page.getByRole('heading',{name:'Checked-in Reservations'})).toBeVisible()
  await expect(page.getByRole('heading',{name:'Available Matching Units'})).toBeVisible()
})

test('FRMS-ST-0063 - AVAILABLE physical unit is eligible for handover pool',async({request})=>{
  const id=fixture('FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID')
  const unit=await managerGetUnit(request,id)
  expect(unit.status).toBe('AVAILABLE')
})


test('FRMS-ST-0065 - Matching physical unit and reservation unit type are verified',async({request,page})=>{
  // Read-only: the snapshot candidate already has an ACTIVE HandoverUnitAssignment.
  // GET checks only. Never PUT/DELETE an assignment to manufacture a fixture.
  const reservationId=fixture('FRMS_TEST_ASSIGNABLE_RESERVATION_ID')
  const unitId=fixture('FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID')
  const facilityId=fixture('FRMS_TEST_HANOI_FACILITY_ID')
  const reservation=await getAs(request,'FACILITY_MANAGER',`/api/v1/reservations/${reservationId}`)
  if(reservation.status()!==200)
    throw new Error(`FRMS_FIXTURE_MISSING: Manager F01 reservation detail HTTP ${reservation.status()}.`)
  const data=(await reservation.json() as {data?:{
    reservationId?:string,facilityId?:string,unitTypeId?:string,status?:string,
    reservationVisit?:{visitType?:string,status?:string}|null
  }}).data
  if(!data||data.reservationId!==reservationId||data.facilityId!==facilityId||
      data.status!=='CONFIRMED'||data.reservationVisit?.visitType!=='RESERVATION'||
      data.reservationVisit?.status!=='CHECKED_IN')
    throw new Error('FRMS_FIXTURE_MISSING: Reservation must be CONFIRMED with a CHECKED_IN RESERVATION Visit in Manager F01 facility.')
  const unit=await managerGetUnit(request,unitId)
  if(unit.storageUnitId!==unitId||unit.status!=='AVAILABLE'||
      unit.facilityId!==data.facilityId||!data.unitTypeId||unit.unitTypeId!==data.unitTypeId)
    throw new Error('FRMS_FIXTURE_MISSING: physical unit must be AVAILABLE and match Reservation Facility and UnitType.')
  const assignment=await getAs(request,'FACILITY_MANAGER',
    `/api/v1/reservations/${reservationId}/handover-unit-assignment`)
  if(assignment.status()!==200)
    throw new Error(`FRMS_FIXTURE_MISSING: Manager F01 assignment detail HTTP ${assignment.status()}.`)
  const assigned=(await assignment.json() as {data?:{
    reservationId?:string,storageUnitId?:string,status?:string
  }|null}).data
  if(!assigned||assigned.status!=='ACTIVE'||assigned.reservationId!==reservationId||
      assigned.storageUnitId!==unitId)
    throw new Error('FRMS_FIXTURE_MISSING: expected ACTIVE assignment linking the selected Reservation and AVAILABLE matching Unit.')
  expect(unit.unitTypeId).toBe(data.unitTypeId)
  if(!unit.unitCode) throw new Error('FRMS_FIXTURE_MISSING: matching physical unit has no code for the saved-assignment UI assertion.')
  await visitAs(page,'FACILITY_MANAGER','/manager/handover-unit-selection')
  await expect(page.getByRole('heading',{name:'Available Matching Units'})).toBeVisible()
  // Search the actual reservation, select its UI card and observe the already-saved
  // assignment. Never click "Save assignment" or "Release assignment".
  await page.getByRole('textbox',{name:'Search reservations by customer, unit type, rental period or status'}).fill(reservationId)
  const reservationCards=page.locator('.handover-reservation-list > button')
  await expect(reservationCards).toHaveCount(1,{timeout:45000})
  await reservationCards.click()
  await expect(page.locator('.handover-selection-summary')).toContainText(unit.unitCode,{timeout:45000})
  await expect(page.locator('.handover-unit-card-selected').getByRole('button',{name:'Saved'})).toBeVisible()
})

test('FRMS-ST-0067 - Handover is gated before staff check-in',async({page})=>{
  await openHandover(page)
  await expect(page.getByRole('button',{name:'Check In Customer'})).toBeVisible({timeout:45000})
  await expect(page.getByRole('button',{name:'Complete Handover',exact:true})).toHaveCount(0)
})



test('FRMS-ST-0164 - Staff daily work view contains filters and actions',async({page})=>{
  await visitAs(page,'FACILITY_STAFF','/staff/work-list')
  await expect(page.getByRole('region',{name:'Work-list filters'})).toBeVisible()
  await expect(page.getByPlaceholder('Customer, phone, unit or status…')).toBeVisible()
  await expect(page.getByRole('main')).not.toContainText('Unable to load work items')
})
test('FRMS-ST-0165 - Staff work list excludes references from another facility',async({request})=>{
  const ownRef=fixture('FRMS_TEST_STAFF_OWN_REFERENCE_ID')
  const foreignRef=fixture('FRMS_TEST_OTHER_FACILITY_REFERENCE_ID')
  const ownDate=fixture('FRMS_TEST_STAFF_SCOPE_DATE')
  const foreignDate=fixture('FRMS_TEST_OTHER_FACILITY_SCOPE_DATE')
  const datePattern=/^\d{4}-\d{2}-\d{2}$/
  if(!datePattern.test(ownDate)||!datePattern.test(foreignDate)||ownRef===foreignRef)
    throw new Error('FRMS_FIXTURE_MISSING: distinct references and valid own/foreign dates required.')

  // Verify both actual actors and facility assignments before testing isolation.
  const [ownToken,foreignToken]=await Promise.all([
    bearer(request,'FACILITY_STAFF'),bearer(request,'FACILITY_STAFF_FOREIGN')
  ])
  const account=async(token:string)=>{
    const response=await request.get(`${API}/api/v1/auth/me`,{
      headers:{Authorization:`Bearer ${token}`},timeout:30000})
    if(response.status()!==200)throw new Error('FRMS_FIXTURE_MISSING: Staff /auth/me unavailable.')
    return (await response.json() as {data?:{role?:string,status?:string,profile?:{facilityId?:string}}}).data
  }
  const [ownUser,foreignUser]=await Promise.all([account(ownToken),account(foreignToken)])
  const ownFacility=ownUser?.profile?.facilityId
  const foreignFacility=foreignUser?.profile?.facilityId
  if(ownUser?.role!=='FACILITY_STAFF'||foreignUser?.role!=='FACILITY_STAFF'||
      ownUser.status!=='ACTIVE'||foreignUser.status!=='ACTIVE'||!ownFacility||
      !foreignFacility||ownFacility===foreignFacility)
    throw new Error('FRMS_FIXTURE_MISSING: two ACTIVE Staff actors with distinct assigned Facilities required.')

  // Establish that F32 can read the nominated Visit on its actual business date.
  const positive=await request.get(`${API}/api/v1/visits/${foreignRef}`,{
    headers:{Authorization:`Bearer ${foreignToken}`},timeout:30000})
  if(positive.status()!==200)
    throw new Error(`FRMS_FIXTURE_MISSING: foreign Staff Visit GET returned ${positive.status()}.`)
  const visit=(await positive.json() as {data?:{visitId?:string,visitType?:string,visitDate?:string,status?:string}}).data
  if(visit?.visitId!==foreignRef||visit.visitDate!==foreignDate||
      visit.visitType!=='RESERVATION'||!['SCHEDULED','CHECKED_IN'].includes(visit.status||''))
    throw new Error('FRMS_FIXTURE_MISSING: foreign Visit ID/date/type/status does not match the approved scope.')

  const listIds=async(token:string,date:string)=>{
    const ids:string[]=[]
    for(let page=1;page<=100;page++){
      const response=await request.get(`${API}/api/v1/staff/work-items?date=${date}&page=${page}&pageSize=100`,{
        headers:{Authorization:`Bearer ${token}`},timeout:30000})
      if(response.status()!==200)throw new Error(`FRMS_FIXTURE_MISSING: Staff work-list HTTP ${response.status()} on ${date}.`)
      const payload=await response.json() as {data?:Array<{referenceId?:string}>,pagination?:{totalPages?:number}}
      const pages=payload.pagination?.totalPages
      if(!Array.isArray(payload.data)||!Number.isInteger(pages)||pages!<1||pages!>100)
        throw new Error('FRMS_FIXTURE_MISSING: incomplete or invalid Staff work-list pagination.')
      ids.push(...payload.data.map(x=>x.referenceId||''))
      if(page>=pages!)return ids
    }
    throw new Error('FRMS_FIXTURE_MISSING: Staff work list exceeded 100 pages.')
  }
  const [ownIds,foreignIds]=await Promise.all([
    listIds(ownToken,ownDate),listIds(foreignToken,foreignDate)
  ])
  if(!ownIds.includes(ownRef)||!foreignIds.includes(foreignRef))
    throw new Error('FRMS_FIXTURE_MISSING: own/F32 positive work-item control was not established.')

  // Compare F01 with the confirmed F32 Visit ON THE SAME FOREIGN DATE.
  const filtered=ownDate===foreignDate?ownIds:await listIds(ownToken,foreignDate)
  expect(filtered,'cross-facility Visit must not appear in F01 work list on the F32 Visit date').not.toContain(foreignRef)
  const denied=await request.get(`${API}/api/v1/visits/${foreignRef}`,{
    headers:{Authorization:`Bearer ${ownToken}`},timeout:30000})
  expect([403,404],'F01 Staff must not access F32 Visit details').toContain(denied.status())
})

test('FRMS-ST-0109 - Return inspection has evidence, damage, fee and completion controls',async({page})=>{
  const inspection=fixture('FRMS_TEST_STAFF_IN_PROGRESS_INSPECTION_ID')
  await visitAs(page,'FACILITY_STAFF',`/staff/returns?tab=inspection&inspectionId=${inspection}`)
  await waitForHeading(page,'Handle Returns')
  await expect(page.getByRole('heading',{name:'Inspection findings'})).toBeVisible({timeout:45000})
  await expect(page.getByLabel('Evidence type')).toBeVisible()
  await expect(page.getByLabel('Evidence file')).toBeVisible()
  await expect(page.getByLabel('Damage type',{exact:true})).toBeVisible()
  await expect(page.getByLabel('Extra fee type')).toBeVisible()
  await expect(page.getByLabel('Condition note')).toBeVisible()
  await expect(page.getByRole('button',{name:/Complete Inspection/i})).toBeVisible()
})
