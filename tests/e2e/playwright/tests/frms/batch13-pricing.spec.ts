import {expect,test,type Page} from '@playwright/test'
import {capturePosts,fixture,visitAs} from './batch12-helpers'
import {precondition,read} from './batch13-helpers'

test.setTimeout(120_000)
test.use({trace:'off'})

async function priceEditor(page:Page){
  const name=fixture('FRMS_TEST_PRICING_UNIT_TYPE_NAME')
  await visitAs(page,'BUSINESS_OPERATIONS_MANAGER','/business/unit-types')
  await expect(page.getByRole('heading',{name:'Unit Type Pricing'})).toBeVisible({timeout:45000})
  const table=page.getByRole('table',{name:'Unit type pricing registry'})
  await expect(table).toBeVisible()
  await page.getByRole('searchbox',{name:'Search unit types by name, size or description'}).fill(name)
  const row=table.getByRole('row').filter({hasText:name})
  await expect(row).toHaveCount(1)
  await row.getByRole('button',{name:'Update Price'}).click()
  await expect(page.getByRole('heading',{name:'Update Rental Price'})).toBeVisible()
  const input=page.locator('#unit-type-rental-price')
  await expect(input).toHaveAttribute('min','0')
  await expect(input).toHaveAttribute('type','number')
  return input
}
async function assertPriceFrontend(page:Page,value:string){
  const price=await priceEditor(page)
  const sent=capturePosts(page,'/unit-types')
  await price.fill(value)
  await expect(price).toHaveValue(value)
  expect(await price.evaluate((node)=> (node as HTMLInputElement).validity.rangeUnderflow)).toBe(false)
  await expect(page.getByText('Rental price must be greater than or equal to 0.')).toHaveCount(0)
  expect(sent()).toBe(0)
  await page.getByRole('button',{name:'Cancel'}).click()
  expect(sent()).toBe(0)
}
test('FRMS-ST-0183-B - 99999 is valid under actual nonnegative pricing check',async({page})=>{
  await assertPriceFrontend(page,'99999')
})
test('FRMS-ST-0184 - legacy 100000 floor is not enforced in price editor',async({page})=>{
  const input=await priceEditor(page)
  for(const value of ['99999','100000']){
    await input.fill(value)
    await expect(input).toHaveValue(value)
    expect(await input.evaluate(node=>(node as HTMLInputElement).validity.rangeUnderflow)).toBe(false)
  }
  await page.getByRole('button',{name:'Cancel'}).click()
})
test('FRMS-ST-0184-B - actual zero rental price lower bound is valid in frontend',async({page})=>{
  await assertPriceFrontend(page,'0')
})
test('FRMS-ST-0187 - business editor permission is not implicitly System Admin permission',async({page})=>{
  await visitAs(page,'SYSTEM_ADMINISTRATOR','/business/unit-types')
  await expect(page).not.toHaveURL(/\/business\/unit-types(?:[?#]|$)/,{timeout:45000})
  await expect(page.getByRole('heading',{name:'Update Rental Price'})).toHaveCount(0)
  await visitAs(page,'BUSINESS_OPERATIONS_MANAGER','/business/unit-types')
  await expect(page.getByRole('table',{name:'Unit type pricing registry'})).toBeVisible({timeout:45000})
})
test('FRMS-ST-0188 - Staff inspection Damage Type dropdown reflects ACTIVE API types',async({request,page})=>{
  const id=fixture('FRMS_TEST_STAFF_IN_PROGRESS_INSPECTION_ID')
  const inspection=await read<{inspectionId:string,status:string}>(request,'FACILITY_STAFF','/inspections/'+id)
  precondition(inspection.inspectionId===id&&inspection.status==='IN_PROGRESS',
    'assigned IN_PROGRESS inspection for Staff')
  const types=await read<Array<{damageTypeId:string,name:string,status:string}>>(
    request,'FACILITY_STAFF','/damage-types')
  const active=types.filter(x=>x.status==='ACTIVE')
  precondition(active.length>0,'nonempty active DamageType catalogue')
  await visitAs(page,'FACILITY_STAFF','/staff/returns?tab=inspection&inspectionId='+id)
  await expect(page.getByRole('heading',{name:'Handle Returns'})).toBeVisible()
  const select=page.getByLabel('Damage type',{exact:true})
  await expect(select).toBeVisible({timeout:45000})
  const options=await select.locator('option').evaluateAll(nodes=>
    nodes.map(node=>({value:(node as HTMLOptionElement).value,label:node.textContent?.trim()||''})))
  const actual=options.filter(x=>x.value)
  expect(new Set(actual.map(x=>x.value))).toEqual(new Set(active.map(x=>x.damageTypeId)))
  for(const item of active)
    expect(actual.find(x=>x.value===item.damageTypeId)?.label).toBe(item.name)
})
