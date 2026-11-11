import {expect,test} from '@playwright/test'
import {fixture,getAs,visitAs} from './batch12-helpers'
import {allManagerUnits,foreignManagerUnit,assertNotEligible,managerCandidate,precondition,showManagerSelection} from './batch13-helpers'

test.setTimeout(120_000)
test.use({trace:'off'})

test('FRMS-ST-0168 - IN_USE physical unit status action is lifecycle locked',async({request,page})=>{
  const facilityId=fixture('FRMS_TEST_HANOI_FACILITY_ID')
  const units=await allManagerUnits(request,facilityId)
  const inUse=units.find(u=>u.status==='IN_USE')
  precondition(inUse,'F01 IN_USE unit')
  await visitAs(page,'FACILITY_MANAGER','/manager/physical-units')
  await expect(page.getByRole('heading',{name:'Manage Physical Units'})).toBeVisible()
  await page.locator('button[title="Danh sách bảng"]').click()
  await page.getByPlaceholder('Search unit code, location or unit type').fill(inUse.unitCode)
  const row=page.getByRole('table',{name:'Physical storage units'}).getByRole('row').filter({hasText:inUse.unitCode})
  await expect(row).toBeVisible({timeout:45000})
  await expect(row.getByRole('button',{name:'Lifecycle'})).toBeDisabled()
  await expect(row).toContainText('IN USE')
})
test('FRMS-ST-0170 - MAINTENANCE unit is excluded from available handover list',async({request,page})=>{
  const reservation=await managerCandidate(request)
  const units=await allManagerUnits(request,reservation.facilityId)
  const maintenance=units.find(u=>u.status==='MAINTENANCE'&&u.unitTypeId===reservation.unitTypeId)
  precondition(maintenance,'MAINTENANCE physical unit matching selected F01 reservation UnitType')
  await showManagerSelection(page,reservation)
  await assertNotEligible(page,maintenance)
  await expect(page.getByRole('button',{name:'Save assignment'})).toHaveCount(0)
})
test('FRMS-ST-0172 - Facility Staff cannot use manager physical unit editor',async({page})=>{
  await visitAs(page,'FACILITY_STAFF','/manager/physical-units')
  await expect(page).not.toHaveURL(/\/manager\/physical-units(?:[?#]|$)/,{timeout:45000})
  await expect(page.getByRole('heading',{name:'Manage Physical Units'})).toHaveCount(0)
})
test('FRMS-ST-0172-B - F01 Manager cannot read cross-facility physical unit',async({request,page})=>{
  const foreign=await foreignManagerUnit(request)
  const response=await getAs(request,'FACILITY_MANAGER','/api/v1/storage-units/'+foreign.storageUnitId)
  expect([403,404],'F01 manager must not read verified foreign physical unit').toContain(response.status())
  const f01=await allManagerUnits(request,fixture('FRMS_TEST_HANOI_FACILITY_ID'))
  expect(f01.map(unit=>unit.storageUnitId),'F01 scoped inventory must exclude the foreign Unit ID').not.toContain(foreign.storageUnitId)
  precondition(!f01.some(unit=>unit.unitCode===foreign.unitCode),
    'foreign unit code must be distinguishable from codes in F01 for UI-only comparison')
  await visitAs(page,'FACILITY_MANAGER','/manager/physical-units')
  await expect(page.getByRole('heading',{name:'Manage Physical Units'})).toBeVisible()
  await page.locator('button[title="Danh sách bảng"]').click()
  await page.getByPlaceholder('Search unit code, location or unit type').fill(foreign.unitCode)
  await expect(page.getByRole('table',{name:'Physical storage units'}).getByRole('row').filter({hasText:foreign.unitCode})).toHaveCount(0)
})
