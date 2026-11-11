import {expect,test,type Page} from '@playwright/test'
import {API,assertAuthDenied,bearer,capturePosts,contractStatus,fixture,getAs,monthOffset,reservationUrl,visitAs,visitNoCccd,vnToday,waitForHeading} from './batch12-helpers'

test.setTimeout(120_000)
test.use({trace:'off'})

async function activeContractPage(page:Page,suffix:string) {
  const id=fixture('FRMS_TEST_ACTIVE_CONTRACT_ID')
  const contract=await contractStatus(page.request,'CUSTOMER',id)
  if(contract.status!=='ACTIVE')throw new Error('FRMS_FIXTURE_MISSING: FRMS_TEST_ACTIVE_CONTRACT_ID must be ACTIVE.')
  await visitAs(page,'CUSTOMER',`/customer/contracts/${id}${suffix}`)
  return {id,...contract}
}
async function statusContractPage(page:Page, envId:string, expected:string, suffix:string){
  const id=fixture(envId)
  const contract=await contractStatus(page.request,'CUSTOMER',id)
  if(contract.status!==expected)throw new Error(`FRMS_FIXTURE_MISSING: ${envId} must be ${expected} (observed ${contract.status}).`)
  await visitAs(page,'CUSTOMER',`/customer/contracts/${id}${suffix}`)
  return contract
}
async function reservationReview(page:Page){const path=reservationUrl();await visitAs(page,'CUSTOMER',path);await waitForHeading(page,'Confirm Your Reservation')}
async function supportCreate(page:Page){
  const response=await getAs(page.request,'CUSTOMER','/api/v1/contracts?page=1&pageSize=20')
  if(response.status()!==200)throw new Error(`FRMS_FIXTURE_MISSING: cannot read Customer 1 contracts (HTTP ${response.status()}).`)
  const rows=(await response.json() as {data?:Array<{contractId?:string}>}).data
  test.skip(!Array.isArray(rows)||rows.length===0,'FRMS_FIXTURE_MISSING: Customer 1 needs at least one owned Contract to show Support Ticket form.')
  await visitAs(page,'CUSTOMER','/customer/support-tickets/new')
  await waitForHeading(page,'Create Support Ticket')
  await expect(page.locator('#supportDescription')).toBeVisible({timeout:45000})
}

async function invoicePage(page:Page,key:string){const id=fixture(key);await visitAs(page,'CUSTOMER',`/customer/invoices/${id}`);await waitForHeading(page,'Invoice Details');return id}

// Reservation: no writes until a dedicated DB/fixtures are supplied.
test('FRMS-ST-0041 - Reservation review shows selected terms and actions',async({page})=>{
  await reservationReview(page)
  const detail=page.locator('.detail-list')
  for(const name of ['Facility','Address','Unit Type','Storage Type / Size','Rental Period','Current Listed Rent'])await expect(detail).toContainText(name)
  await expect(page.getByText('Your locked rental price and deposit amount will be shown after the reservation is created.')).toBeVisible()
})
test('FRMS-ST-0047 - Saved valid National ID permits reservation review',async({page})=>{
  await reservationReview(page)
  await expect(page.getByRole('button',{name:'Confirm Reservation',exact:true})).toBeVisible({timeout:45000})
  await expect(page.getByRole('button',{name:'Add National ID to Continue'})).toHaveCount(0)
})
test('FRMS-ST-0048 - Missing saved National ID requires profile completion',async({page})=>{
  const url=reservationUrl();await visitNoCccd(page,url)
  await expect(page.getByRole('button',{name:'Add National ID to Continue'})).toBeVisible({timeout:45000})
  await expect(page.getByRole('button',{name:'Confirm Reservation',exact:true})).toHaveCount(0)
})
test('FRMS-ST-0049 - Deposit equals the captured monthly rent on existing reservation',async({page})=>{
  const id=fixture('FRMS_TEST_OWN_RESERVATION_ID')
  const response=await getAs(page.request,'CUSTOMER',`/api/v1/reservations/${id}`)
  expect(response.status()).toBe(200)
  const data=(await response.json() as {data?:{depositAmount:number,lockedRentalPrice:number}}).data
  expect(data?.depositAmount).toBeDefined();expect(data?.lockedRentalPrice).toBeDefined()
  expect(data!.depositAmount,'one-month deposit formula').toBe(data!.lockedRentalPrice)
  await visitAs(page,'CUSTOMER',`/customer/reservations/${id}`)
  await expect(page.getByRole('main')).toContainText(/deposit/i)
})

test('FRMS-ST-0214 - Reservation does not create capacity hold with missing CCCD',async({page})=>{
  const path=reservationUrl(),posts=capturePosts(page,'/api/v1/reservations')
  await visitNoCccd(page,path)
  await page.getByRole('button',{name:'Add National ID to Continue'}).click()
  await expect(page.getByRole('heading',{name:'Complete Your Profile'})).toBeVisible()
  expect(posts(),'profile gate must not POST reservation').toBe(0)
})
test('FRMS-ST-0216 - Customer cannot read a different customer reservation',async({page})=>{
  const id=fixture('FRMS_TEST_CUSTOMER_2_RESERVATION_ID')
  const response=await getAs(page.request,'CUSTOMER',`/api/v1/reservations/${id}`)
  assertAuthDenied(response.status())
  await visitAs(page,'CUSTOMER',`/customer/reservations/${id}`)
  await expect(page.getByRole('main')).not.toContainText('Confirm Reservation and Visit')
})

// Customer Access Visits. No visit is created by boundary assertions.
test('FRMS-ST-0072 - Access booking page exposes contract and date-only controls',async({page})=>{
  await activeContractPage(page,'/access-visits/new')
  await waitForHeading(page,'Schedule Access Visit')
  await expect(page.getByRole('heading',{name:'Selected Contract'})).toBeVisible()
  await expect(page.getByLabel('Access Visit Date (GMT+7)')).toHaveAttribute('type','date')
  await expect(page.getByRole('button',{name:'Confirm Access Visit'})).toBeVisible()
})

test('FRMS-ST-0078 - Completed contract cannot schedule an access visit',async({page})=>{
  await statusContractPage(page,'FRMS_TEST_COMPLETED_CONTRACT_ID','COMPLETED','/access-visits/new')
  await expect(page.getByText('You can only schedule access visits for an active contract.')).toBeVisible({timeout:45000})
  await expect(page.getByRole('button',{name:'Confirm Access Visit'})).toHaveCount(0)
})
test('FRMS-ST-0078-B - Terminated contract cannot schedule access',async({page})=>{
  await statusContractPage(page,'FRMS_TEST_TERMINATED_CONTRACT_ID','TERMINATED','/access-visits/new')
  await expect(page.getByText('You can only schedule access visits for an active contract.')).toBeVisible({timeout:45000})
  await expect(page.getByRole('button',{name:'Confirm Access Visit'})).toHaveCount(0)
})
test('FRMS-ST-0079 - Earliest access date respects today and contract start',async({page})=>{
  const contract=await activeContractPage(page,'/access-visits/new')
  const minimum=await page.locator('#accessVisitDate').getAttribute('min')
  const expected=contract.startMonth+'-01'
  expect(minimum).toBe(expected>vnToday()?expected:vnToday())
})

test('FRMS-ST-0081 - Date after contract end cannot be selected',async({page})=>{
  const contract=await activeContractPage(page,'/access-visits/new')
  const date=monthOffset(contract.endMonth,1)+'-01'
  const max=await page.locator('#accessVisitDate').getAttribute('max')
  expect(max,'access date range must end with contract period').toBeLessThan(date)
  // CalendarDateField renders month buttons and disables dates beyond max.
  // Move to the first day AFTER the contract instead of conditionally ignoring an absent button.
  const initialMonth=(await page.locator('#accessVisitDate').getAttribute('min'))!.slice(0,7)
  const toMonth=date.slice(0,7)
  const [y0,m0]=initialMonth.split('-').map(Number),[y1,m1]=toMonth.split('-').map(Number)
  const steps=(y1-y0)*12+(m1-m0)
  test.skip(steps<0||steps>120,'FRMS_FIXTURE_MISSING: contract period outside supported calendar navigation window.')
  for(let i=0;i<steps;i++) await page.getByRole('button',{name:'Next Month'}).click()
  await expect(page.getByRole('button',{name:`Select ${date}`})).toBeDisabled()
})
test('FRMS-ST-0156 - Access form has date picker and no timeslot',async({page})=>{
  await activeContractPage(page,'/access-visits/new')
  await expect(page.getByText('Storage Unit',{exact:true})).toBeVisible({timeout:45000})
  await expect(page.getByText('Rental Period',{exact:true})).toBeVisible()
  await expect(page.locator('#accessVisitDate')).toHaveAttribute('type','date')
  await expect(page.getByRole('link',{name:'My Visits'})).toBeVisible()
  await expect(page.getByLabel(/time slot/i)).toHaveCount(0)
})
test('FRMS-ST-0159 - Non-active contract shows access prohibition notice',async({page})=>{
  await statusContractPage(page,'FRMS_TEST_COMPLETED_CONTRACT_ID','COMPLETED','/access-visits/new')
  await expect(page.locator('.notice')).toContainText('only schedule access visits for an active contract')
})
test('FRMS-ST-0160 - Past access dates are excluded by Vietnam-date bound',async({page})=>{
  await activeContractPage(page,'/access-visits/new')
  const minimum=await page.locator('#accessVisitDate').getAttribute('min')
  expect(minimum).toBeTruthy();expect(minimum!).toBeGreaterThanOrEqual(vnToday())
})

// Billing read paths and role protections.
test('FRMS-ST-0085 - Customer invoices list shows filters and columns',async({page})=>{
  await visitAs(page,'CUSTOMER','/customer/invoices?tab=invoices')
  await waitForHeading(page,'Deposit & Invoices')
  await expect(page.getByRole('tab',{name:'All Invoices'})).toBeVisible()
  for(const label of ['Invoice Type','Status'])await expect(page.getByLabel(label,{exact:true})).toBeVisible()
  const id=fixture('FRMS_TEST_OWN_DEPOSIT_INVOICE_ID')
  await expect(page.getByRole('table',{name:'Invoice records'})).toBeVisible({timeout:45000})
  for(const column of ['Invoice','Payment Deadline','Amount Due','Status'])await expect(page.getByRole('columnheader',{name:column})).toBeVisible()
  expect(id).toBeTruthy()
})
test('FRMS-ST-0090 - Paid invoice has no payment action',async({page})=>{
  const id=fixture('FRMS_TEST_OWN_PAID_INVOICE_ID')
  const response=await getAs(page.request,'CUSTOMER',`/api/v1/invoices/${id}`)
  if(response.status()!==200)throw new Error(`FRMS_FIXTURE_MISSING: paid invoice read HTTP ${response.status()}.`)
  const invoice=(await response.json() as {data?:{status?:string}}).data
  if(invoice?.status!=='PAID')throw new Error('FRMS_FIXTURE_MISSING: FRMS_TEST_OWN_PAID_INVOICE_ID must be PAID.')
  await invoicePage(page,'FRMS_TEST_OWN_PAID_INVOICE_ID')
  await expect(page.getByRole('region',{name:'Invoice information'})).toContainText(/paid/i,{timeout:45000})
  await expect(page.getByRole('button',{name:/Pay Now|Pay Invoice|Pay with PayOS/i})).toHaveCount(0)
})
test('FRMS-ST-0227 - Customer can view their reservation deposit invoice',async({page})=>{
  const id=fixture('FRMS_TEST_OWN_DEPOSIT_INVOICE_ID')
  const response=await getAs(page.request,'CUSTOMER',`/api/v1/invoices/${id}`)
  if(response.status()!==200)throw new Error(`FRMS_FIXTURE_MISSING: deposit invoice read HTTP ${response.status()}.`)
  const invoice=(await response.json() as {data?:{invoiceType?:string}}).data
  if(invoice?.invoiceType!=='DEPOSIT')throw new Error('FRMS_FIXTURE_MISSING: FRMS_TEST_OWN_DEPOSIT_INVOICE_ID must be a DEPOSIT invoice.')
  await invoicePage(page,'FRMS_TEST_OWN_DEPOSIT_INVOICE_ID')
  await expect(page.getByRole('heading',{name:'Deposit Invoice'})).toBeVisible({timeout:45000})
  await expect(page.getByRole('heading',{name:'Amount Breakdown'})).toBeVisible()
})
test('FRMS-ST-0228 - One customer cannot read the other customer invoice',async({request})=>{
  const id=fixture('FRMS_TEST_CUSTOMER_2_INVOICE_ID')
  const response=await getAs(request,'CUSTOMER',`/api/v1/invoices/${id}`)
  assertAuthDenied(response.status())
})
test('FRMS-ST-0234 - First rental month has no separately payable online invoice',async({request})=>{
  const contractId=fixture('FRMS_TEST_ACTIVE_CONTRACT_ID')
  const contract=await contractStatus(request,'CUSTOMER',contractId)
  const token=await bearer(request,'CUSTOMER')
  const items:Array<{invoiceType?:string,billingMonth?:string,entityId?:string}>=[]
  for(let page=1;page<=100;page++){
    const response=await request.get(`${API}/api/v1/invoices?page=${page}&pageSize=100`,{
      headers:{Authorization:`Bearer ${token}`},timeout:30000})
    expect(response.status()).toBe(200)
    const payload=await response.json() as {data?:Array<{invoiceType?:string,billingMonth?:string,entityId?:string}>,pagination?:{totalPages:number}}
    expect(Array.isArray(payload.data)).toBe(true)
    items.push(...payload.data!)
    if(page>=(payload.pagination?.totalPages||1))break
    if(page===100)throw new Error('FRMS_FIXTURE_MISSING: invoice pagination exceeded safety cap.')
  }
  // InvoiceDetailResponse uses `entityId` for the Contract; NOT `contractId`.
  const rentalInvoices=items.filter(x=>x.entityId===contractId&&x.invoiceType==='RENTAL_FEE')
  test.skip(rentalInvoices.length===0,'FRMS_FIXTURE_MISSING: need a contract with at least one later-month RENTAL_FEE invoice to prove the first month is excluded.')
  expect(rentalInvoices.some(x=>x.billingMonth===contract.startMonth),
    'the first contract month is paid offline and must not create an online RENTAL_FEE invoice').toBe(false)
})


// Renewal: assert the real month picker and visible summary, not a fictional renewal invoice.
async function renewalPage(page:Page){return activeContractPage(page,'/renew')}

test('FRMS-ST-0097 - Contract renewal page displays extension summary controls',async({page})=>{
  await renewalPage(page);await waitForHeading(page,'Extend Contract Duration')
  for(const item of ['Current End Month','Select New Check-out Month','Extension Summary'])await expect(page.getByText(item,{exact:true}).first()).toBeVisible()
  await expect(page.getByRole('button',{name:'Confirm Extension'})).toBeVisible()
})

test('FRMS-ST-0101 - Renewal cannot choose the current end month',async({page})=>{
  const contract=await renewalPage(page)
  await page.getByRole('button',{name:'Choose new check-out month'}).click()
  const label=new Intl.DateTimeFormat('en-US',{month:'long',year:'numeric',timeZone:'UTC'}).format(new Date(Date.UTC(Number(contract.endMonth.slice(0,4)),Number(contract.endMonth.slice(5,7))-1,1)))
  const year=Number(label.slice(-4))
  const dialog=page.getByRole('dialog',{name:'New check-out month picker'})
  const displayedYear=Number(await dialog.locator('.month-picker-year strong').innerText())
  if(year===displayedYear) await expect(dialog.getByRole('button',{name:label,exact:true})).toBeDisabled()
  else { expect(displayedYear).toBeGreaterThan(year);await expect(dialog.getByRole('button',{name:'Previous year'})).toBeDisabled() }
  await expect(page.locator('#newEndMonth')).toHaveValue('')
})
test('FRMS-ST-0101-B - Renewal cannot choose a month earlier than current end',async({page})=>{
  const contract=await renewalPage(page)
  const earlier=monthOffset(contract.endMonth,-1)
  await page.getByRole('button',{name:'Choose new check-out month'}).click()
  const label=new Intl.DateTimeFormat('en-US',{month:'long',year:'numeric',timeZone:'UTC'}).format(new Date(Date.UTC(Number(earlier.slice(0,4)),Number(earlier.slice(5,7))-1,1)))
  const year=Number(label.slice(-4))
  const dialog=page.getByRole('dialog',{name:'New check-out month picker'})
  const displayedYear=Number(await dialog.locator('.month-picker-year strong').innerText())
  if(year===displayedYear) await expect(dialog.getByRole('button',{name:label,exact:true})).toBeDisabled()
  else { expect(displayedYear).toBeGreaterThan(year);await expect(dialog.getByRole('button',{name:'Previous year'})).toBeDisabled() }
  await expect(page.locator('#newEndMonth')).toHaveValue('')
})




// Return flow read-only review and pending claim gate.
test('FRMS-ST-0108 - Return request shows date and early return policies',async({page})=>{
  await activeContractPage(page,'/return-visits/new')
  await waitForHeading(page,'Request Storage Return')
  await expect(page.getByLabel('Requested Return Date (GMT+7)')).toHaveAttribute('type','date')
  await expect(page.getByRole('heading',{name:'Normal Return'})).toBeVisible()
  await expect(page.getByRole('heading',{name:'Early Return'})).toBeVisible()
  await expect(page.getByRole('button',{name:'Submit Return Request'})).toBeVisible()
})



// Support ticket UI validation and ownership.
test('FRMS-ST-0123 - Support request form has contract, category and description',async({page})=>{
  await supportCreate(page)
  for(const label of ['Contract','Category','Description'])await expect(page.getByLabel(label,{exact:true})).toBeVisible()
  await expect(page.getByLabel('Title',{exact:true})).toHaveCount(0)
  await expect(page.getByRole('button',{name:'Submit Support Ticket'})).toBeVisible()
})

test('FRMS-ST-0129 - Empty support description is rejected inline without POST',async({page})=>{
  await supportCreate(page);const count=capturePosts(page,'/api/v1/support-tickets')
  await page.getByLabel('Contract',{exact:true}).selectOption({index:1})
  await page.getByLabel('Category',{exact:true}).selectOption({index:1})
  await page.getByLabel('Description',{exact:true}).fill('')
  await page.getByRole('button',{name:'Submit Support Ticket'}).click()
  await expect(page.getByText('Describe the issue.',{exact:true})).toBeVisible()
  expect(count()).toBe(0)
})



test('FRMS-ST-0177-B - Whitespace-only ticket description is rejected',async({page})=>{
  await supportCreate(page);const count=capturePosts(page,'/api/v1/support-tickets')
  await page.getByLabel('Contract',{exact:true}).selectOption({index:1})
  await page.getByLabel('Category',{exact:true}).selectOption({index:1})
  await page.getByLabel('Description',{exact:true}).fill('   ')
  await page.getByRole('button',{name:'Submit Support Ticket'}).click()
  await expect(page.getByText('Describe the issue.',{exact:true})).toBeVisible()
  expect(count()).toBe(0)
})
test('FRMS-ST-0180 - Other customer cannot access support ticket by ID',async({request})=>{
  const id=fixture('FRMS_TEST_CUSTOMER_2_SUPPORT_TICKET_ID')
  const response=await getAs(request,'CUSTOMER',`/api/v1/support-tickets/${id}`)
  assertAuthDenied(response.status())
})
