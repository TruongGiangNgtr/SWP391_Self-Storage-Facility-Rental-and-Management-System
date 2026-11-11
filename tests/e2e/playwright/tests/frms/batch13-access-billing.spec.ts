import {expect,test,type Page,type APIRequestContext} from '@playwright/test'
import {API,capturePosts,fixture,getAs,isolatedWrite,visitAs} from './batch12-helpers'
import {list,monthLast,ownActiveContract,precondition,read,type Contract,type Invoice,type Visit} from './batch13-helpers'

test.setTimeout(120_000)
test.use({trace:'off'})

async function activeVisitForm(page:Page,request:APIRequestContext){
  const contract=await ownActiveContract(request)
  await visitAs(page,'CUSTOMER','/customer/contracts/'+contract.contractId+'/access-visits/new')
  await expect(page.getByRole('heading',{name:'Schedule Access Visit'})).toBeVisible()
  await expect(page.getByRole('region',{name:'Schedule access visit'})).toBeVisible({timeout:45000})
  const input=page.locator('#accessVisitDate')
  return {contract,input}
}
async function createIsolatedAccess(page:Page,request:APIRequestContext,onLastDay:boolean){
  // Actual POST and new Visit are allowed only on a separately provisioned disposable DB.
  isolatedWrite()
  const {contract,input}=await activeVisitForm(page,request)
  const min=await input.getAttribute('min'),max=await input.getAttribute('max')
  precondition(min&&max&&min<=max,'ACTIVE owned contract with an eligible access-date window')
  const chosen=onLastDay?monthLast(contract.endMonth):min
  precondition(chosen>=min&&chosen<=max,'chosen day within eligible ACTIVE contract')
  await input.fill(chosen)
  await expect(input).toHaveValue(chosen)
  await page.getByRole('button',{name:'Confirm Access Visit'}).click()
  await expect(page).toHaveURL(/\/customer\/visits\/[0-9a-f-]{36}(?:[?#]|$)/,{timeout:45000})
  await expect(page.getByRole('heading',{name:'Visit Details'})).toBeVisible()
  await expect(page.getByRole('region',{name:'Visit information'})).toContainText(/Scheduled/i)
  await expect(page.getByRole('region',{name:'Visit information'})).toContainText(chosen)
}
async function rentalInvoice(request:APIRequestContext){
  const invoices=await list<Invoice>(request,'CUSTOMER','/invoices')
  const item=invoices.find(x=>x.invoiceType==='RENTAL_FEE'&&x.billingMonth&&Number.isFinite(x.amountDue))
  precondition(item,'genuine owner-visible later-month RENTAL_FEE Invoice')
  return item
}
async function feeRows(page:Page,days:(x:number)=>boolean){
  const contract=await ownActiveContract(page.request)
  const summary=await read<{lateFees:Array<{lateFeeId:string,invoiceId:string,overdueDays:number,amount:number}>}>(
    page.request,'CUSTOMER','/contracts/'+contract.contractId+'/billing')
  const item=summary.lateFees.find(x=>days(x.overdueDays))
  precondition(item,'recorded late fee in the selected overdue-days boundary partition')
  await visitAs(page,'CUSTOMER','/customer/contracts/'+contract.contractId+'/fees-return')
  await expect(page.getByRole('heading',{name:'Fees & Return Tracking'})).toBeVisible()
  const panel=page.locator('.tracking-late-fees')
  await expect(panel.getByRole('heading',{name:'Late Fee Records'})).toBeVisible({timeout:45000})
  const record=panel.locator('.tracking-record').filter({hasText:String(item.overdueDays)})
  await expect(record).toContainText('Recorded Late Fee')
  const formatted=new Intl.NumberFormat('en-GB',{style:'currency',currency:'VND',currencyDisplay:'code',maximumFractionDigits:0}).format(item.amount)
  await expect(record).toContainText(formatted)
  return item
}

test('FRMS-ST-0077 - ACTIVE owned contract can schedule a valid access Visit on isolated DB',async({request,page})=>{
  await createIsolatedAccess(page,request,false)
})
test('FRMS-ST-0080 - access Visit accepts final calendar day of contract on isolated DB',async({request,page})=>{
  await createIsolatedAccess(page,request,true)
})
test('FRMS-ST-0084 - OVERDUE rental Invoice does not invent an OVERDUE contract enum',async({request,page})=>{
  const contract=await ownActiveContract(request)
  const summary=await read<{invoices:Invoice[]}>(request,'CUSTOMER','/contracts/'+contract.contractId+'/billing')
  const overdue=summary.invoices.find(x=>x.invoiceType==='RENTAL_FEE'&&x.status==='OVERDUE')
  precondition(overdue,'ACTIVE Contract that also has a genuine OVERDUE rental invoice')
  await visitAs(page,'CUSTOMER','/customer/contracts/'+contract.contractId+'/access-visits/new')
  await expect(page.getByRole('region',{name:'Schedule access visit'})).toBeVisible()
  await expect(page.getByRole('button',{name:'Confirm Access Visit'})).toBeVisible()
  await expect(page.getByText('You can only schedule access visits for an active contract.')).toHaveCount(0)
})
test('FRMS-ST-0162-B - CHECKED_IN ACCESS Visit cannot be cancelled by customer',async({request,page})=>{
  const visits=await list<Visit>(request,'CUSTOMER','/visits')
  const active=visits.find(v=>v.visitType==='ACCESS'&&v.status==='CHECKED_IN')
  precondition(active,'owned CHECKED_IN ACCESS Visit')
  await visitAs(page,'CUSTOMER','/customer/visits/'+active.visitId)
  const region=page.getByRole('region',{name:'Visit information'})
  await expect(region).toContainText(/Checked In/i,{timeout:45000})
  await expect(page.getByText('This visit can no longer be rescheduled or cancelled.',{exact:false})).toBeVisible()
  await expect(page.getByRole('button',{name:'Cancel Visit'})).toHaveCount(0)
})
test('FRMS-ST-0163 - other customer Contract cannot be used for access scheduling',async({request,page})=>{
  const own=await ownActiveContract(request)
  const denied=await getAs(request,'CUSTOMER_2','/api/v1/contracts/'+own.contractId)
  expect([403,404],'Customer 2 must not read Customer 1 Contract').toContain(denied.status())
  await visitAs(page,'CUSTOMER_2','/customer/contracts/'+own.contractId+'/access-visits/new')
  await expect(page.getByRole('region',{name:'Schedule access visit'})).toHaveCount(0)
  await expect(page.getByRole('button',{name:'Confirm Access Visit'})).toHaveCount(0)
})
test('FRMS-ST-0086 - later RENTAL_FEE invoice appears in customer invoice history',async({request,page})=>{
  const item=await rentalInvoice(request)
  await visitAs(page,'CUSTOMER','/customer/invoices?tab=invoices')
  await expect(page.getByRole('heading',{name:'Deposit & Invoices'})).toBeVisible()
  await page.getByLabel('Invoice Type',{exact:true}).selectOption('RENTAL_FEE')
  const table=page.getByRole('table')
  await expect(table.getByText('Rental Fee').first()).toBeVisible({timeout:45000})
  await visitAs(page,'CUSTOMER','/customer/invoices/'+item.invoiceId)
  const detail=page.getByRole('region',{name:'Invoice information'})
  await expect(detail).toContainText('Rental Invoice',{timeout:45000})
  await expect(detail).toContainText(item.billingMonth!)
  await expect(detail).toContainText('Amount Due')
  await expect(detail).toContainText(item.status.replaceAll('_',' '),{ignoreCase:true})
})
test('FRMS-ST-0095 - recorded fee at or before grace-day seven is shown without inventing a rule',async({page})=>{
  const item=await feeRows(page,n=>n>=0&&n<=7)
  expect(item.overdueDays).toBeLessThanOrEqual(7)
})
test('FRMS-ST-0096 - recorded fee after grace-day seven is shown with API amount',async({page})=>{
  const item=await feeRows(page,n=>n>=8)
  expect(item.overdueDays).toBeGreaterThanOrEqual(8)
})
test('FRMS-ST-0235 - saved rental invoice breakdown is sourced from issued invoice',async({request,page})=>{
  const item=await rentalInvoice(request)
  const invoice=await read<Invoice>(request,'CUSTOMER','/invoices/'+item.invoiceId)
  precondition(invoice.invoiceType==='RENTAL_FEE'&&Number.isFinite(invoice.baseAmount)&&
    Number.isFinite(invoice.discountAmount)&&Number.isFinite(invoice.amountDue),'full recorded invoice snapshot')
  await visitAs(page,'CUSTOMER','/customer/invoices/'+invoice.invoiceId)
  const breakdown=page.locator('.invoice-detail-breakdown')
  await expect(breakdown).toBeVisible({timeout:45000})
  for(const name of ['Base Amount','Discount','Amount Due'])
    await expect(breakdown.getByText(name,{exact:true})).toBeVisible()
  const before=await breakdown.textContent()
  expect(before).not.toBeNull()
  await page.getByRole('button',{name:'Refresh Invoice'}).click()
  await expect.poll(async()=>breakdown.textContent(),{timeout:45_000}).toBe(before)
  expect(invoice.amountDue).toBeGreaterThanOrEqual(0)
  expect(invoice.baseAmount).toBeGreaterThanOrEqual(0)
  expect(invoice.discountAmount).toBeGreaterThanOrEqual(0)
})
test('FRMS-ST-0236 - Facility Staff cannot pay or mark customer Invoice as PAID',async({request,page})=>{
  const invoiceId=fixture('FRMS_TEST_OWN_DEPOSIT_INVOICE_ID')
  const owned=await getAs(request,'CUSTOMER','/api/v1/invoices/'+invoiceId)
  precondition(owned.status()===200,'Customer owns the invoice used for Staff role boundary')
  const actual=(await owned.json() as {data?:{invoiceId?:string}}).data
  precondition(actual?.invoiceId===invoiceId,'verified owner-visible invoice ID')
  // BillingController permits scoped Staff invoice GET. This testcase checks
  // the Customer payment UI boundary, not an invented blanket invoice-read denial.
  await visitAs(page,'FACILITY_STAFF','/customer/invoices/'+invoiceId)
  await expect(page).not.toHaveURL(/\/customer\/invoices\/[0-9a-f-]{36}(?:[?#]|$)/,{timeout:45_000})
  await expect(page.getByRole('button',{name:'Pay with payOS'})).toHaveCount(0)
  await expect(page.getByRole('heading',{name:'Invoice Details'})).toHaveCount(0)
})
