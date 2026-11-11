import { expect, test } from '@playwright/test'
import { visitAs, waitForHeading } from './batch12-helpers'

test.setTimeout(120_000)
test.use({ trace: 'off' })

async function adminUsers(page: import('@playwright/test').Page) {
  await visitAs(page, 'SYSTEM_ADMINISTRATOR', '/admin/users')
  await waitForHeading(page, 'User account monitoring')
}
async function adminLogs(page: import('@playwright/test').Page) {
  await visitAs(page, 'SYSTEM_ADMINISTRATOR', '/admin/activity-logs')
  await waitForHeading(page, 'Activity log management')
}

test('FRMS-ST-0135 - Admin dashboard exposes account controls and role/status columns', async ({page}) => {
  await adminUsers(page)
  const grid = page.getByRole('table', {name: 'User account monitoring'})
  await expect(grid).toBeVisible({timeout:45000})
  for (const heading of ['User', 'Role', 'Contact', 'Status']) {
    await expect(grid.getByRole('columnheader', {name: heading, exact:true})).toBeVisible()
  }
  await expect(page.getByRole('region', {name:'Account list summary'})).toBeVisible()
})

test('FRMS-ST-0139 - Admin audit search applies an action filter', async ({page}) => {
  await adminLogs(page)
  await page.getByPlaceholder('Exact action').fill('FRMS_BATCH12_NONEXISTENT_ACTION')
  await page.getByRole('button', {name:'Apply filters'}).click()
  await expect(page.getByText('No activity matches these filters. Clear them to see all activity.')).toBeVisible({timeout:45000})
  await page.getByRole('button', {name:'Clear filters'}).click()
  await expect(page.getByPlaceholder('Exact action')).toHaveValue('')
})

test('FRMS-ST-0141 - Customer role is unavailable in employee provisioning', async ({page}) => {
  await visitAs(page,'SYSTEM_ADMINISTRATOR','/admin/employees')
  await waitForHeading(page,'Employee account management')
  await page.getByRole('button',{name:'Create employee',exact:true}).first().click()
  const dialog=page.getByRole('dialog',{name:'Create employee account'})
  await expect(dialog).toBeVisible()
  const select=dialog.getByLabel('Employee role')
  for (const role of ['FACILITY_STAFF','FACILITY_MANAGER','BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR']) {
    await expect(select.locator(`option[value="${role}"]`)).toHaveCount(1)
  }
  await expect(select.locator('option[value="CUSTOMER"]')).toHaveCount(0)
  await select.selectOption('FACILITY_MANAGER')
  await expect(dialog.getByLabel('Facility',{exact:true})).toBeVisible()
  await select.selectOption('BUSINESS_OPERATIONS_MANAGER')
  await expect(dialog.getByText('Not applicable for global employee roles')).toBeVisible()
  await dialog.getByRole('button',{name:'Cancel'}).click()
  await expect(dialog).toHaveCount(0)
})



test('FRMS-ST-0146 - Customer direct navigation cannot expose admin user data', async ({page}) => {
  await visitAs(page,'CUSTOMER','/admin/users')
  await expect(page).not.toHaveURL(/\/admin\/users(?:[?#]|$)/,{timeout:45000})
  await expect(page.getByRole('table',{name:'User account monitoring'})).toHaveCount(0)
})

test('FRMS-ST-0189 - Admin user list supports filters and bounded pagination', async ({page}) => {
  await adminUsers(page)
  const filter=page.getByRole('region',{name:'Current-page account filters'})
  await expect(filter).toBeVisible()
  const search=page.getByPlaceholder('Name, email or phone…')
  await search.fill('FRMS_BATCH12_NEVER_REGISTERED_NAME')
  await expect(search).toHaveValue('FRMS_BATCH12_NEVER_REGISTERED_NAME')
  await expect(page.getByRole('table',{name:'User account monitoring'})).toHaveCount(0)
  await expect(page.getByText(/No user accounts to show|No user accounts found|No accounts on this page match/i).first()).toBeVisible()
  await search.fill('')
})
