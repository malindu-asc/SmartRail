import type { Browser, Page } from '@playwright/test'
import { account, type Persona } from './accounts'

/**
 * Opens the console in a fresh browser context (so two people can be signed in at the same time) and signs in through the
 * Keycloak page. The console redirects to Keycloak on its own; the form is Keycloak's own (`#username`, `#password`, `#kc-login`).
 */
export async function signedIn(browser: Browser, persona: Persona): Promise<Page> {
  const { email, password } = account(persona)
  const context = await browser.newContext()
  const page = await context.newPage()

  await page.goto('/')
  await page.locator('#username').fill(email)
  await page.locator('#password').fill(password)
  await page.locator('#kc-login').click()

  // Back in the console: the sidebar is there once the person and their permissions are loaded.
  await page.getByRole('navigation').first().waitFor()
  return page
}
