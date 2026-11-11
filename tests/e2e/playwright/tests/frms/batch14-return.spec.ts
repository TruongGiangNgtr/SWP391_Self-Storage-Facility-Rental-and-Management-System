import {expect,test,type APIRequestContext,type Page} from '@playwright/test'
import {getAs,visitAs} from './batch12-helpers'
import {list,precondition,read,type Contract} from './batch13-helpers'

/** Batch 14: return/settlement *recorded* states only, no state-mutating finalization. */
test.setTimeout(120_000)
test.use({trace:'off'})
type Actor='CUSTOMER'|'CUSTOMER_2'
type Damage={damageRecordId:string,status:'PENDING'|'APPROVED'|'REJECTED',damageAmount:number}
type ReturnRecord={
  contractId:string,contractStatus:string,
  inspection:{inspectionId:string,status:'PENDING'|'IN_PROGRESS'|'COMPLETED',completedAt:string|null}|null,
  damages:Damage[],settlement:{status:string,totalDeduction:number,refundAmount:number,additionalAmountDue:number}|null
}
const money=(value:number)=>new Intl.NumberFormat('en-GB',{
  style:'currency',currency:'VND',currencyDisplay:'code',maximumFractionDigits:0
}).format(value)
async function matchingReturn(request:APIRequestContext,predicate:(record:ReturnRecord)=>boolean){
  for(const actor of ['CUSTOMER','CUSTOMER_2'] as const){
    const contracts=await list<Contract>(request,actor,'/contracts')
    for(const contract of contracts){
      const summary=await read<ReturnRecord>(request,actor,'/contracts/'+contract.contractId+'/return-summary')
      expect(summary.contractId).toBe(contract.contractId)
      if(predicate(summary))return {actor,contract,summary}
    }
  }
  precondition(false,'owner-visible return/settlement record matching the required state')
  throw new Error('unreachable')
}
async function openReturn(page:Page,actor:Actor,contractId:string){
  await visitAs(page,actor,'/customer/contracts/'+contractId+'/fees-return')
  await expect(page.getByRole('heading',{name:'Fees & Return Tracking'})).toBeVisible({timeout:45_000})
  await page.getByRole('tab',{name:'Return & Settlement',exact:true}).click()
  const panel=page.getByRole('tabpanel')
  await expect(panel.getByRole('heading',{name:'Deposit Settlement'})).toBeVisible({timeout:45_000})
  return panel
}
async function staffPending(request:APIRequestContext){
  const match=await matchingReturn(request,s=>s.inspection?.status==='COMPLETED'&&
    s.damages.some(x=>x.status==='PENDING')&&s.settlement===null)
  const id=match.summary.inspection!.inspectionId
  const allowed=await getAs(request,'FACILITY_STAFF','/api/v1/inspections/'+id)
  if(allowed.status()>=500)throw new Error('FRMS_INFRA_UNAVAILABLE: F01 Staff Inspection GET HTTP '+allowed.status())
  precondition(allowed.status()===200,'F01 Staff must access this genuine completed Inspection')
  const detail=(await allowed.json() as {data?:{inspectionId:string,status:string,
    damages:Array<{status:string}>}}).data
  precondition(detail?.inspectionId===id&&detail.status==='COMPLETED'&&
    detail.damages.some(x=>x.status==='PENDING'),'Staff detail has a manager-PENDING Damage decision')
  return {...match,id}
}
async function staffFinalizePanel(page:Page,id:string){
  await visitAs(page,'FACILITY_STAFF','/staff/returns?tab=inspection&inspectionId='+id)
  await expect(page.getByRole('heading',{name:'Handle Returns'})).toBeVisible({timeout:45_000})
  const finalize=page.locator('.fwp-finalize-card')
  await expect(finalize).toBeVisible({timeout:45_000})
  return finalize
}

test('FRMS-ST-0116 - pending Damage decision blocks return finalization',async({request,page})=>{
  const found=await staffPending(request)
  const area=await staffFinalizePanel(page,found.id)
  await expect(area).toContainText('Manager resolution is required.',{timeout:45_000})
  await expect(area.getByRole('button',{name:'Finalize Return'})).toBeDisabled()
  await expect(page.getByRole('region',{name:'Authoritative settlement result'})).toHaveCount(0)
})
test('FRMS-ST-0117 - positive refund is shown as a recorded settlement obligation',async({request,page})=>{
  const found=await matchingReturn(request,s=>s.settlement?.status==='FINALIZED'&&s.settlement.refundAmount>0)
  const panel=await openReturn(page,found.actor,found.contract.contractId)
  const result=panel.locator('.tracking-settlement')
  await expect(result).toContainText('Total Deduction')
  await expect(result).toContainText('Refund Amount')
  await expect(result.locator('.tracking-refund dd')).toHaveText(money(found.summary.settlement!.refundAmount))
  await expect(result).toContainText('not confirmation that a refund transfer')
})
test('FRMS-ST-0118 - additional amount due is displayed without claiming collection',async({request,page})=>{
  const found=await matchingReturn(request,s=>s.settlement?.status==='FINALIZED'&&s.settlement.additionalAmountDue>0)
  const panel=await openReturn(page,found.actor,found.contract.contractId)
  const result=panel.locator('.tracking-settlement')
  await expect(result.locator('.tracking-additional dd')).toHaveText(money(found.summary.settlement!.additionalAmountDue))
  await expect(result).toContainText('not confirmation that a refund transfer or debt collection')
})
test('FRMS-ST-0122 - unresolved manager Damage decision leaves Finalize Return disabled',async({request,page})=>{
  const found=await staffPending(request)
  const area=await staffFinalizePanel(page,found.id)
  const count=found.summary.damages.filter(d=>d.status==='PENDING').length
  await expect(area).toContainText(count+' Damage decision(s) are still PENDING.')
  await expect(area.getByRole('button',{name:'Finalize Return'})).toBeDisabled()
  await expect(page.getByRole('heading',{name:'Return Finalized'})).toHaveCount(0)
})
test('FRMS-ST-0243 - completed inspection without settlement does not imply finalized return',async({request,page})=>{
  const found=await matchingReturn(request,s=>s.inspection?.status==='COMPLETED'&&s.settlement===null)
  const panel=await openReturn(page,found.actor,found.contract.contractId)
  const inspection=panel.locator('.tracking-inspection')
  await expect(inspection).toContainText('Completed',{timeout:45_000})
  await expect(inspection).toContainText('Inspection completion alone does not finalize a return.')
  await expect(panel.locator('.tracking-settlement')).toContainText('Settlement has not been recorded yet.')
})
test('FRMS-ST-0244 - customer and Staff see unresolved Damage decision warning',async({request,page})=>{
  const found=await staffPending(request)
  const panel=await openReturn(page,found.actor,found.contract.contractId)
  await expect(panel.locator('.tracking-damages')).toContainText(
    'Your return cannot be finalized until the Facility Manager has resolved the pending damage decisions.')
  const area=await staffFinalizePanel(page,found.id)
  await expect(area.getByRole('button',{name:'Finalize Return'})).toBeDisabled()
  await expect(area).toContainText('Manager resolution is required.')
})
