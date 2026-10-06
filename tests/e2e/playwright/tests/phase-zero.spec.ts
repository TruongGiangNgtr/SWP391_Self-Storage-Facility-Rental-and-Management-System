import { expect, test } from '@playwright/test'

test('E2E-P0-001 technical React shell loads without a business workflow', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'FRMS Frontend Foundation' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Customer Sign In' })).toBeVisible()
})
