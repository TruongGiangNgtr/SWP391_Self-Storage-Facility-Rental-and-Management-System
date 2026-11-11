import {expect,test,type APIRequestContext,type Page} from '@playwright/test'
import {capturePosts,monthOffset,visitAs} from './batch12-helpers'
import {list,ownActiveContract,precondition,read,type Contract,type Invoice} from './batch13-helpers'

/** Batch 14: existing, owner-visible records only. Never send a payment or renew a contract. */
test.setTimeout(120_000)
test.use({trace:'off'})
type BillingInvoice=Invoice & {entityId:string,paidAt:string|null,dueDate:string}
type Fee={lateFeeId:string,invoiceId:string,overdueDays:number,amount:number,calculatedAt:string}
type Billing={contractId:string,invoices:BillingInvoice[],lateFees:Fee[]}
const money=(value:number)=>new Intl.NumberFormat('en-GB',{
  style:'currency',currency:'VND',currencyDisplay:'code',maximumFractionDigits:0
}).format(value)

async function findBilling(request:APIRequestContext,check:(s:Billing)=>boolean){
  for(const actor of ['CUSTOMER','CUSTOMER_2'] as const){
    const contracts=await list<Contract>(request,actor,'/contracts')
    for(const contract of contracts){
      const summary=await read<Billing>(request,actor,'/contracts/'+contract.contractId+'/billing')
      expect(summary.contractId).toBe(contract.contractId)
      if(check(summary))return {actor,contract,summary}
    }
  }
  precondition(false,'owner-visible Contract billing data satisfying the case partition')
  throw new Error('unreachable')
}
async function lateFee(page:Page,request:APIRequestContext,partition:(days:number)=>boolean){
  const found=await findBilling(request,s=>s.lateFees.some(f=>partition(f.overdueDays)))
  const fee=found.summary.lateFees.find(f=>partition(f.overdueDays))!
  precondition(found.summary.invoices.some(x=>x.invoiceId===fee.invoiceId&&x.invoiceType==='RENTAL_FEE'),
    'late fee must refer to an existing owner-visible RENTAL_FEE invoice')
  await visitAs(page,found.actor,'/customer/contracts/'+found.contract.contractId+'/fees-return')
  await expect(page.getByRole('heading',{name:'Fees & Return Tracking'})).toBeVisible({timeout:45000})
  const section=page.locator('.tracking-late-fees')
  await expect(section.getByRole('heading',{name:'Late Fee Records'})).toBeVisible({timeout:45000})
  const record=section.locator('.tracking-record').filter({
    has:page.locator('a[href="/customer/invoices/'+fee.invoiceId+'"]')
  })
  await expect(record).toHaveCount(1,{timeout:45000})
  await expect(record.getByText(String(fee.overdueDays),{exact:true})).toBeVisible()
  await expect(record).toContainText(money(fee.amount))
  return fee
}
async function findInvoice(request:APIRequestContext,predicate:(x:BillingInvoice)=>boolean){
  for(const actor of ['CUSTOMER','CUSTOMER_2'] as const){
    const invoices=await list<BillingInvoice>(request,actor,'/invoices')
    const item=invoices.find(predicate)
    if(item){
      const detail=await read<BillingInvoice>(request,actor,'/invoices/'+item.invoiceId)
      precondition(detail.invoiceId===item.invoiceId&&detail.status===item.status,
        'invoice detail must match the owner-visible list record')
      return {actor,item:detail}
    }
  }
  precondition(false,'an owner-visible invoice matching this recorded state')
  throw new Error('unreachable')
}

test('FRMS-ST-0091 - no recorded late fee is shown as no late-fee record',async({page,request})=>{
  // Source UI has no seven-day fee calculator. Require an actual billing summary
  // with no LateFee records, rather than inventing a zero-fee day-seven transaction.
  const found=await findBilling(request,s=>s.lateFees.length===0&&s.invoices.some(x=>x.invoiceType==='RENTAL_FEE'))
  await visitAs(page,found.actor,'/customer/contracts/'+found.contract.contractId+'/fees-return')
  await expect(page.getByRole('heading',{name:'Fees & Return Tracking'})).toBeVisible({timeout:45000})
  const region=page.locator('.tracking-late-fees')
  await expect(region.getByRole('heading',{name:'Late Fee Records'})).toBeVisible({timeout:45000})
  await expect(region).toContainText('No late-fee records have been returned.')
  await expect(region.locator('.tracking-record')).toHaveCount(0)
})
test('FRMS-ST-0092 - recorded late fee above seven overdue days uses authoritative amount',async({page,request})=>{
  const fee=await lateFee(page,request,n=>n>=8)
  expect(fee.overdueDays).toBeGreaterThanOrEqual(8)
  await expect(page.locator('.tracking-late-fees')).toContainText(money(fee.amount))
})
test('FRMS-ST-0093 - paid invoice status and payment record come from the service',async({request,page})=>{
  const {actor,item}=await findInvoice(request,x=>x.status==='PAID'&&Boolean(x.paidAt))
  await visitAs(page,actor,'/customer/invoices/'+item.invoiceId)
  const detail=page.getByRole('region',{name:'Invoice information'})
  await expect(detail).toBeVisible({timeout:45000})
  await expect(detail).toContainText('Paid')
  await expect(detail).toContainText('Paid At')
  await expect(detail).toContainText('Amount Due')
  await expect(page.locator('.invoice-detail-recorded-state')).toContainText('recorded as paid')
  await expect(page.getByRole('button',{name:'Pay with payOS'})).toHaveCount(0)
})
test('FRMS-ST-0094 - overdue rental invoice does not create an OVERDUE contract state',async({request,page})=>{
  const {actor,item}=await findInvoice(request,x=>x.invoiceType==='RENTAL_FEE'&&x.status==='OVERDUE')
  const contract=await read<Contract>(request,actor,'/contracts/'+item.entityId)
  precondition(contract.contractId===item.entityId&&['ACTIVE','COMPLETED','TERMINATED'].includes(contract.status),
    'linked Contract has a supported state')
  await visitAs(page,actor,'/customer/invoices/'+item.invoiceId)
  await expect(page.getByRole('region',{name:'Invoice information'})).toContainText('Overdue',{timeout:45000})
  await visitAs(page,actor,'/customer/contracts/'+contract.contractId)
  const banner=page.getByRole('region',{name:'Recorded contract status'})
  await expect(banner).toContainText(contract.status.toLowerCase(),{timeout:45000})
  await expect(banner).not.toContainText('contract is overdue')
})

const monthNames=['January','February','March','April','May','June','July','August','September','October','November','December']
async function previewExtension(page:Page,request:APIRequestContext,months:5|6){
  const contract=await ownActiveContract(request)
  const next=monthOffset(contract.endMonth,1),target=monthOffset(contract.endMonth,months)
  precondition(/^\d{4}-(0[1-9]|1[0-2])$/.test(next)&&target>contract.endMonth&&
    Number(target.slice(0,4))<=9999,'ACTIVE Contract with valid extension period')
  await visitAs(page,'CUSTOMER','/customer/contracts/'+contract.contractId+'/renew')
  await expect(page.getByRole('heading',{name:'Extend Contract Duration'})).toBeVisible({timeout:45000})
  const posts=capturePosts(page,'/contracts/'+contract.contractId+'/renew')
  const picker=page.getByRole('button',{name:'Choose new check-out month'})
  await picker.click()
  const dialog=page.getByRole('dialog',{name:'New check-out month picker'})
  await expect(dialog).toBeVisible()
  if(target.slice(0,4)!==next.slice(0,4))
    await dialog.getByRole('button',{name:'Next year'}).click()
  const [y,m]=target.split('-').map(Number)
  await dialog.getByRole('button',{name:monthNames[m-1]+' '+y,exact:true}).click()
  const summary=page.getByRole('region',{name:'Extension summary'})
  await expect(summary.getByText(months+' months',{exact:true})).toBeVisible()
  await expect(page.locator('#newEndMonth')).toHaveValue(monthNames[m-1]+' '+y)
  await expect(page.getByRole('heading',{name:'Extension Confirmed'})).toHaveCount(0)
  expect(posts(),'preview must never renew or change the shared database').toBe(0)
}
test('FRMS-ST-0103 - five-month extension preview shows a five-month estimate period',async({page,request})=>{
  await previewExtension(page,request,5)
})
test('FRMS-ST-0104 - six-month extension preview displays six months without promised 10-percent discount',async({page,request})=>{
  await previewExtension(page,request,6)
  await expect(page.getByRole('region',{name:'Extension summary'})).toContainText('Confirmed after submission')
})
