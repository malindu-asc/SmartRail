import { defineConfig, devices } from '@playwright/test'

/**
 * End-to-end flows for the SmartRail consoles. They drive a real browser against the running Docker Compose stack
 * (`docker compose up`: API on 5080, Keycloak on 8080, console on 3000), so nothing is mocked.
 *
 * Run by hand, not in CI: each flow signs in through Keycloak and changes real data (it adds a user, a notice, a message).
 */
export default defineConfig({
  testDir: './tests',
  // The flows share one database and one set of seeded accounts, so they run one after another.
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 90_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.CONSOLE_URL ?? 'http://localhost:3000',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
})
