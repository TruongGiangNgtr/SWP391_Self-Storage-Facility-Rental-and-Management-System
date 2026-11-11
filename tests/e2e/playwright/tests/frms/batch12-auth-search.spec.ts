import {expect,test} from '@playwright/test'
import {fixture,month} from './batch12-helpers'

test.setTimeout(120_000)
test.use({trace:'off'})





test('FRMS-ST-0025 - Staff identifier cannot authenticate through customer portal',async({page})=>{
  await page.goto('/auth/customer/login')
  await page.getByLabel('Phone Number').fill(process.env.FRMS_TEST_STAFF_01_EMAIL||'staff.f01@example.com')
  await page.getByLabel('Password',{exact:true}).fill(process.env.FRMS_TEST_STAFF_PASSWORD||'11111111')
  await page.getByRole('button',{name:'Sign In',exact:true}).click()
  await expect(page).toHaveURL(/\/auth\/customer\/login/)
  await expect(page.getByRole('alert')).toContainText('The phone number or password is incorrect.')
})
test('FRMS-ST-0199 - Unknown customer phone cannot sign in',async({page})=>{
  const absent=fixture('FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE')
  await page.goto('/auth/customer/login')
  await page.getByLabel('Phone Number').fill(absent)
  await page.getByLabel('Password',{exact:true}).fill('11111111')
  await page.getByRole('button',{name:'Sign In',exact:true}).click()
  await expect(page).toHaveURL(/\/auth\/customer\/login/)
  await expect(page.getByRole('alert')).toContainText('The phone number or password is incorrect.')
})
test('FRMS-ST-0203 - Unknown employee email cannot sign in',async({page})=>{
  const email=fixture('FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL')
  await page.goto('/auth/employee/login')
  await page.getByLabel('Email',{exact:true}).fill(email)
  await page.getByLabel('Password',{exact:true}).fill('11111111')
  await page.getByRole('button',{name:'Sign In',exact:true}).click()
  await expect(page).toHaveURL(/\/auth\/employee\/login/)
  await expect(page.getByRole('alert')).toContainText('Incorrect sign-in details.')
})

test('FRMS-ST-0031 - AI guide shows a recoverable fallback on service outage',async({page})=>{
  await page.route('**/api/v1/ai/**',route=>route.abort('failed'))
  await page.goto('/size-guide')
  await page.getByLabel('Items you want to store').fill('Boxes and chairs')
  await page.getByRole('button',{name:'Get AI Recommendation'}).click()
  await expect(page.getByText('Your description has been kept. Submit again when you are ready, or compare storage types manually.')).toBeVisible({timeout:45000})
  await expect(page.getByLabel('Items you want to store')).toHaveValue('Boxes and chairs')
})

test('FRMS-ST-0207 - Ha Noi active facility is searchable',async({page})=>{
  const facility=fixture('FRMS_TEST_HANOI_FACILITY_ID')
  const name=fixture('FRMS_TEST_HANOI_FACILITY_NAME')
  await page.goto(`/facilities?startMonth=${month(1)}&endMonth=${month(1)}&facilityId=${facility}`)
  await expect(page.getByRole('region',{name:'Storage search results'})).toContainText(name,{timeout:60000})
})
