import { expect, test } from '@playwright/test'

test('FRMS frontend foundation loads', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'FRMS Frontend Foundation' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Đăng nhập khách hàng' })).toBeVisible()
})
