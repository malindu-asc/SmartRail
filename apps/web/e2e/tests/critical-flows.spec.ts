import { expect, test, type Locator } from '@playwright/test'
import { api, authorised, publicDisruptions, tokenFor } from '../helpers/api'
import { signedIn } from '../helpers/session'

/**
 * The flows of the demo, in a real browser against the running stack. They are written to be repeatable: every run uses its
 * own unique text, so an earlier run does not get in the way. They add real rows (a user, a notice, a message) and do not clean up.
 *
 * Before running: `docker compose up` (API 5080, Keycloak 8080, console 3000) and `npx playwright install chromium` once.
 */

const run = Date.now().toString(36)

/** Picks the option of a <select> whose text matches, because the visible words are the console's and may be reworded. */
async function chooseOption(select: Locator, text: RegExp) {
  const value = await select.locator('option').filter({ hasText: text }).first().getAttribute('value')
  if (!value) throw new Error(`No option matching ${text}`)
  await select.selectOption(value)
}

test.describe('SmartRail demo flows', () => {
  // Flow 1: the administrator brings a driver in.
  test('flow 1: an administrator invites a driver and the account is listed', async ({ browser }) => {
    const email = `e2e.driver.${run}@gmail.com`
    const page = await signedIn(browser, 'admin')

    await page.goto('/admin/users')
    await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible()

    await page.getByRole('button', { name: 'Invite user' }).click()
    const form = page.getByRole('dialog')
    await form.getByLabel('Full name').fill('E2E Driver')
    await form.getByLabel('Email').fill(email)
    await chooseOption(form.getByLabel('Role'), /driver/i)
    await form.getByRole('button', { name: 'Send invitation' }).click()

    await expect(page.getByText(email)).toBeVisible()
  })

  // Flow 2: who can see what. The administrator cannot run trains, and the controller cannot manage people.
  test('flow 2: the administrator and the controller see different consoles', async ({ browser }) => {
    const admin = await signedIn(browser, 'admin')
    await expect(admin.getByRole('link', { name: 'Users' })).toBeVisible()
    await expect(admin.getByRole('link', { name: 'Live board' })).toHaveCount(0)

    const controller = await signedIn(browser, 'controller')
    await expect(controller.getByRole('link', { name: 'Live board' })).toBeVisible()
    await expect(controller.getByRole('link', { name: 'Users' })).toHaveCount(0)

    // Even by typing the address, the controller does not get the administration screens.
    await controller.goto('/admin/users')
    await expect(controller.getByRole('heading', { name: 'You do not have access to this' })).toBeVisible()
  })

  // Flow 3: the controller tells passengers about trouble, and it becomes public.
  test('flow 3: a controller publishes a disruption and passengers can read it', async ({ browser, request }) => {
    const title = `E2E signal fault ${run}`
    const page = await signedIn(browser, 'controller')

    await page.goto('/disruptions')
    await page.getByRole('button', { name: 'New disruption' }).click()

    const form = page.getByRole('dialog')
    await form.getByLabel('Title').fill(title)
    await form.getByLabel('Message to passengers').fill('Expect delays of up to 15 minutes while the fault is repaired.')
    // One station, not network-wide: a network-wide notice waits for a second controller.
    await form.getByRole('checkbox', { name: /Central/ }).check()
    await form.getByRole('button', { name: 'Publish now' }).click()

    // It is in the controller's Active list...
    await page.goto('/disruptions')
    await expect(page.getByText(title)).toBeVisible()

    // ...and on the public list the passenger app and the station displays read, with no sign-in.
    await expect.poll(async () => (await publicDisruptions(request)).some((d) => d.title === title)).toBe(true)
  })

  // Flow 4: the driver tells control, and control decides what passengers are told.
  test('flow 4: a driver report reaches the controller, who publishes it to passengers', async ({ browser, request }) => {
    const note = `Delay reason: Signal fault (${run})`

    // The driver's phone sends a report. The app calls this same endpoint.
    const driver = await tokenFor(request, 'driver')
    const sent = await request.post(api('/api/driver/assistance'), { ...authorised(driver), data: { note } })
    expect(sent.ok()).toBeTruthy()

    const page = await signedIn(browser, 'controller')
    await page.goto('/driver-messages?tab=assistance')

    const report = page.locator('div.flex.items-center.gap-4').filter({ hasText: note })
    await expect(report).toBeVisible()
    await report.getByRole('button', { name: 'Publish to passengers' }).click()

    // The notice form opens already filled from the driver's note; the controller edits and publishes it.
    const form = page.getByRole('dialog')
    await expect(form.getByLabel('Title')).toHaveValue(/Delay reason/)
    await form.getByRole('checkbox', { name: /Central/ }).check()
    await form.getByRole('button', { name: 'Publish now' }).click()

    await expect.poll(async () => (await publicDisruptions(request)).some((d) => d.title.includes(note))).toBe(true)
  })

  // Flow 5: the controller writes to one driver, the driver acknowledges, and the controller sees it was read.
  test('flow 5: a message to a driver is acknowledged and the controller sees that', async ({ browser, request }) => {
    const text = `Hold at the next station until the signal clears (${run})`
    const page = await signedIn(browser, 'controller')

    await page.goto('/driver-messages')
    await page.getByRole('button', { name: 'New message' }).click()

    const form = page.getByRole('dialog')
    await chooseOption(form.getByLabel('Driver'), /driver/i)
    await form.getByLabel('Message').fill(text)
    await form.getByRole('button', { name: 'Send message' }).click()
    await expect(page.getByText(text)).toBeVisible()

    // The driver's phone fetches its messages, finds this one and acknowledges it.
    const driver = await tokenFor(request, 'driver')
    let id = ''
    await expect
      .poll(async () => {
        const messages = (await (await request.get(api('/api/driver/messages'), authorised(driver))).json()) as { id: string; body: string }[]
        id = messages.find((m) => m.body.includes(text))?.id ?? ''
        return id
      })
      .not.toBe('')
    const acknowledged = await request.post(api(`/api/driver/messages/${id}/acknowledge`), authorised(driver))
    expect(acknowledged.ok()).toBeTruthy()

    // Back in the console the message now says Acknowledged.
    await page.reload()
    await expect(page.getByText(text)).toBeVisible()
    await expect(page.getByText('Acknowledged').first()).toBeVisible()
  })

  // Flow 6: the scheduler generates the week's plan, then approves it, and the driver's duties appear.
  test('flow 6: a scheduler generates and approves the week plan, and the driver sees a duty', async ({ browser, request }) => {
    const page = await signedIn(browser, 'scheduler')

    await page.goto('/planning')
    await expect(page.getByRole('heading', { name: 'Planning' })).toBeVisible()

    await page.getByRole('button', { name: 'Generate plan' }).click()
    await expect(page.getByText(/Plan generated/)).toBeVisible()

    await page.getByRole('button', { name: 'Approve plan' }).click()
    await expect(page.getByText(/Plan approved/)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Approve plan' })).toBeDisabled()

    // The driver's phone asks for today's duty: it is there once the plan is approved.
    const driver = await tokenFor(request, 'driver')
    const duty = (await (await request.get(api('/api/driver/duty'), authorised(driver))).json()) as { duties: unknown[] }
    expect(duty.duties.length).toBeGreaterThan(0)
  })
})
