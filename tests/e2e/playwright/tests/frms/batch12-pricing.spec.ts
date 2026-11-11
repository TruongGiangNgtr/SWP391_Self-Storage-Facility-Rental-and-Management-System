import { expect, test, type Page } from '@playwright/test'
import { capturePosts, fixture, visitAs, waitForHeading } from './batch12-helpers'

test.setTimeout(120_000)
test.use({ trace:'off' })

async function pricing(page:Page){
  await visitAs(page,'BUSINESS_OPERATIONS_MANAGER','/business/unit-types')
  await waitForHeading(page,'Unit Type Pricing')
  await expect(page.getByRole('table',{name:'Unit type pricing registry'})).toBeVisible({timeout:45000})
}
async function editPrice(page:Page){
  const unitName=fixture('FRMS_TEST_PRICING_UNIT_TYPE_NAME')
  await pricing(page)
  await page.getByRole('searchbox',{name:'Search unit types by name, size or description'}).fill(unitName)
  const row=page.getByRole('table',{name:'Unit type pricing registry'}).getByRole('row').filter({hasText:unitName})
  await expect(row).toHaveCount(1)
  await row.getByRole('button',{name:'Update Price'}).click()
  await expect(page.getByRole('heading',{name:'Update Rental Price'})).toBeVisible()
  const input=page.locator('#unit-type-rental-price')
  return {row,input,unitName}
}

/** Save is limited to disposable pricing fixtures on an ISOLATED DB, and always restores original price. */



test('FRMS-ST-0143 - Price 99999 is not a frontend minimum violation',async({page})=>{
  const {input}=await editPrice(page)
  await input.fill('99999')
  await expect(input).toHaveValue('99999')
  await expect(page.getByText('Rental price must be greater than or equal to 0.')).toHaveCount(0)
  await page.getByRole('button',{name:'Cancel'}).click()
})
test('FRMS-ST-0181 - Unit Type Pricing shows registry and editable price dialog',async({page})=>{
  const {input}=await editPrice(page)
  await expect(input).toHaveAttribute('type','number')
  await expect(input).toHaveAttribute('min','0')
  await expect(page.getByText('New Monthly Rental Price')).toBeVisible()
  await expect(page.getByRole('button',{name:'Save Price'})).toBeVisible()
  await page.getByRole('button',{name:'Cancel'}).click()
})
test('FRMS-ST-0183 - Nonnumeric price cannot be accepted by a numeric input',async({page})=>{
  const {input}=await editPrice(page)
  const before=capturePosts(page,'/unit-types')
  await input.fill('')
  await input.pressSequentially('unavailable')
  await expect(input).not.toHaveValue('unavailable')
  expect(before()).toBe(0)
  await page.getByRole('button',{name:'Cancel'}).click()
})

test('FRMS-ST-0183-C - Blank price shows required validation without calling API',async({page})=>{
  const {input}=await editPrice(page)
  await input.fill('')
  const posts=capturePosts(page,'/unit-types')
  await page.getByRole('button',{name:'Save Price'}).click()
  await expect(page.getByText('Rental price is required.')).toBeVisible()
  expect(posts()).toBe(0)
  await page.getByRole('button',{name:'Cancel'}).click()
})


test('FRMS-ST-0187-B - Staff without pricing permission cannot reach pricing editor',async({page})=>{
  await visitAs(page,'FACILITY_STAFF','/business/unit-types')
  await expect(page).not.toHaveURL(/\/business\/unit-types(?:[?#]|$)/,{timeout:45000})
  await expect(page.getByRole('heading',{name:'Update Rental Price'})).toHaveCount(0)
  await expect(page.getByRole('table',{name:'Unit type pricing registry'})).toHaveCount(0)
})
