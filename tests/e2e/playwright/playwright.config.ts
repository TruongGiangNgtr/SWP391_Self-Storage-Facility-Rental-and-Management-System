import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './tests',
  use: { baseURL: 'http://127.0.0.1:4173', trace: 'retain-on-failure' },
  webServer: { command: 'pnpm --dir ../../../frontend build && pnpm --dir ../../../frontend preview --host 127.0.0.1', url: 'http://127.0.0.1:4173', reuseExistingServer: !process.env.CI }
})
