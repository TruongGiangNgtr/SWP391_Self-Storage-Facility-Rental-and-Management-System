import {expect,test} from '@playwright/test'
import {capturePosts,fixture,getAs,visitAs} from './batch12-helpers'
import {list,precondition,read,readStaffReservation,staffHandover,type Contract} from './batch13-helpers'

/** Batch 14: no Handover confirmation, Support creation or employee creation requests. */
test.setTimeout(120_000)
test.use({trace:'off'})

test('FRMS-ST-0061 - first-month rent is acknowledged offline before staff handover',async({request,page})=>{
  const {reservation,visitId}=await readStaffReservation(request)
  precondition(reservation.status==='CONFIRMED'&&reservation.reservationVisit?.status==='CHECKED_IN',
    'a genuine CONFIRMED CHECKED_IN F01 Reservation Visit')
  const assignment=await read<{reservationId:string,status:string,storageUnitId:string}>(
    request,'FACILITY_MANAGER','/reservations/'+reservation.reservationId+'/handover-unit-assignment')
  precondition(assignment.reservationId===reservation.reservationId&&assignment.status==='ACTIVE',
    'Manager-confirmed existing ACTIVE assignment')
  const staffVisit=await read<{visitId:string,entityId:string,status:string}>(
    request,'FACILITY_STAFF','/visits/'+visitId)
  precondition(staffVisit.visitId===visitId&&staffVisit.entityId===reservation.reservationId&&
    staffVisit.status==='CHECKED_IN','Staff may view the same checked-in Visit')
  await staffHandover(page,reservation.reservationId,visitId)
  const summary=page.getByRole('region',{name:'Offline payment summary'})
  await expect(summary).toContainText('First-Month Rent (Collect Offline Now)',{timeout:45000})
  await expect(summary).toContainText('Reservation Deposit')
  await expect(summary).toContainText('Later-Month Discount')
  const ack=page.locator('.receipt-confirmation input[type="checkbox"]')
  await expect(ack).toBeEnabled()
  await expect(ack).not.toBeChecked()
  const complete=page.getByRole('button',{name:'Complete Handover',exact:true})
  await expect(complete).toBeDisabled()
  await ack.check()
  await expect(complete).toBeEnabled()
  // Scope to the unique in-form note: FirstMonthHandoverInfo also renders this sentence.
  await expect(page.locator('.handover-operation form > p.muted')).toContainText(
    'No separate first-month invoice or online payment is created.')
  await expect(page.getByRole('button',{name:/Pay with payOS/i})).toHaveCount(0)
  // Never click Complete Handover: this is a shared database.
})

test('FRMS-ST-0134 - another Customer Contract cannot be chosen for a Support Ticket',async({request,page})=>{
  const own=await list<Contract>(request,'CUSTOMER','/contracts')
  precondition(own.length>0,'Customer 1 has an owned Contract')
  const foreignContract=own[0].contractId
  const forbidden=await getAs(request,'CUSTOMER_2','/api/v1/contracts/'+foreignContract)
  expect([403,404],'Customer 2 must not be able to read Customer 1 Contract').toContain(forbidden.status())
  const other=await list<Contract>(request,'CUSTOMER_2','/contracts')
  precondition(other.length>0,'Customer 2 has independent owned Contract(s) for support form')
  await visitAs(page,'CUSTOMER_2','/customer/support-tickets/new')
  await expect(page.getByRole('heading',{name:'Create Support Ticket'})).toBeVisible({timeout:45000})
  const select=page.getByLabel('Contract',{exact:true})
  await expect(select).toBeVisible({timeout:45000})
  const options=await select.locator('option').evaluateAll(nodes=>
    nodes.map(node=>(node as HTMLOptionElement).value).filter(Boolean))
  expect(options).not.toContain(foreignContract)
  expect(options.some(id=>other.some(c=>c.contractId===id)),'selector must offer actual owned Contract(s)').toBe(true)
  const posts=capturePosts(page,'/api/v1/support-tickets')
  expect(posts(),'cross-owner check must not create a ticket').toBe(0)
})

test('FRMS-ST-0194 - employee Facility selector excludes nonexistent facility IDs',async({request,page})=>{
  const facilities=await list<{facilityId:string,name:string,status:string}>(
    request,'SYSTEM_ADMINISTRATOR','/admin/facilities/lookup')
  const active=facilities.find(f=>f.status==='ACTIVE'&&f.name)
  precondition(active,'authoritative Admin facility directory has at least one ACTIVE Facility')
  const invalid='ffffffff-ffff-ffff-ffff-ffffffffffff'
  precondition(!facilities.some(f=>f.facilityId===invalid),'chosen invalid ID must not exist')
  await visitAs(page,'SYSTEM_ADMINISTRATOR','/admin/employees')
  await expect(page.getByRole('heading',{name:'Employee account management'})).toBeVisible({timeout:45000})
  await page.getByRole('button',{name:'Create employee'}).click()
  const dialog=page.getByRole('dialog',{name:'Create employee account'})
  await expect(dialog).toBeVisible({timeout:45000})
  await dialog.getByLabel('Employee role').selectOption('FACILITY_MANAGER')
  const selector=dialog.getByRole('combobox',{name:/^Facility(?:\s|$)/})
  await expect(selector).toBeEnabled({timeout:45000})
  const actual=await selector.locator('option').evaluateAll(nodes=>
    nodes.map(x=>(x as HTMLOptionElement).value).filter(Boolean))
  expect(actual).not.toContain(invalid)
  expect(new Set(actual),'dropdown must contain exactly Admin-returned real Facility IDs').toEqual(
    new Set(facilities.map(f=>f.facilityId)))
  const posts=capturePosts(page,'/admin/employees')
  await selector.selectOption(active!.facilityId)
  await expect(selector).toHaveValue(active!.facilityId)
  await dialog.getByRole('button',{name:'Cancel'}).click()
  await expect(dialog).toHaveCount(0)
  expect(posts(),'opening the form must not create a new Employee').toBe(0)
})
