import { useState } from 'react'
import { Button, Card, CardHeader, Field, TextInput } from '@/shared/components/ui'
import { formatDate } from '@/shared/utils'
import { useCompleteAccessReview, useSaveSecurity } from '../hooks/useAdmin'
import type { SecuritySettings, SecurityView } from '../types'

interface NumberField {
  key: keyof SecuritySettings
  label: string
  hint: string
  min: number
  max: number
}

const GROUPS: { title: string; fields: NumberField[] }[] = [
  {
    title: 'Sign-in protection',
    fields: [
      { key: 'lockAfterFailures', label: 'Lock after failed attempts', hint: '3 to 20', min: 3, max: 20 },
      { key: 'lockMinutes', label: 'Lock for (minutes)', hint: '1 to 1440', min: 1, max: 1440 },
    ],
  },
  {
    title: 'Sessions',
    fields: [
      { key: 'consoleIdleMinutes', label: 'Console idle timeout (minutes)', hint: '5 to 120', min: 5, max: 120 },
      { key: 'driverAppLockMinutes', label: 'Driver app lock (minutes)', hint: '1 to 60', min: 1, max: 60 },
    ],
  },
  {
    title: 'Access review and devices',
    fields: [
      { key: 'accessReviewDays', label: 'Review every (days)', hint: '30 to 365', min: 30, max: 365 },
      { key: 'deviceLateAfterSeconds', label: 'Device late after (seconds)', hint: '30 to 600', min: 30, max: 600 },
      { key: 'deviceOfflineAfterSeconds', label: 'Device offline after (seconds)', hint: 'Later than late, up to 1800', min: 31, max: 1800 },
    ],
  },
]

/** The security numbers the sign-in service and the devices follow. The server validates them again. */
export function SecurityForm({ view }: { view: SecurityView }) {
  const save = useSaveSecurity()
  const review = useCompleteAccessReview()
  const [settings, setSettings] = useState<SecuritySettings>(view.settings)
  const [submitted, setSubmitted] = useState(false)

  const problem = (field: NumberField) => {
    const value = settings[field.key]
    if (typeof value !== 'number' || Number.isNaN(value) || value < field.min || value > field.max) return `Between ${field.min} and ${field.max}.`
    if (field.key === 'deviceOfflineAfterSeconds' && value <= settings.deviceLateAfterSeconds) return 'Must be later than "late after".'
    return null
  }
  const invalid = GROUPS.some((group) => group.fields.some(problem))

  return (
    <form
      className="space-y-4"
      onSubmit={(event) => {
        event.preventDefault()
        setSubmitted(true)
        if (!invalid) save.mutate(settings)
      }}
    >
      {GROUPS.map((group) => (
        <Card key={group.title}>
          <CardHeader title={group.title} />
          <div className="grid gap-4 px-5 py-4 md:grid-cols-2">
            {group.fields.map((field) => (
              <Field key={field.key} label={field.label} hint={field.hint} error={submitted ? problem(field) : null}>
                {(props) => (
                  <TextInput {...props} type="number" min={field.min} max={field.max} value={String(settings[field.key])} onChange={(event) => setSettings((s) => ({ ...s, [field.key]: Number(event.target.value) }))} />
                )}
              </Field>
            ))}
            {group.title === 'Sign-in protection' && (
              <>
                <Toggle label="Two-step sign-in required" checked={settings.twoStepRequired} onChange={(value) => setSettings((s) => ({ ...s, twoStepRequired: value }))} />
                <Toggle label="Flag leavers who still have access" checked={settings.flagLeavers} onChange={(value) => setSettings((s) => ({ ...s, flagLeavers: value }))} />
              </>
            )}
          </div>
        </Card>
      ))}

      <div className="flex justify-end"><Button type="submit" loading={save.isPending}>Save settings</Button></div>

      <Card>
        <CardHeader title="Access review" />
        <div className="flex flex-wrap items-center justify-between gap-4 px-5 py-4">
          <p className="text-fg-secondary">
            Last review: <strong className="text-fg">{view.lastAccessReview ? formatDate(view.lastAccessReview) : 'never'}</strong> · Next due: <strong className="text-fg">{view.nextAccessReview ? formatDate(view.nextAccessReview) : 'now'}</strong>
          </p>
          <Button variant="secondary" onClick={() => review.mutate()} loading={review.isPending}>Record that every account was reviewed</Button>
        </div>
      </Card>
    </form>
  )
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="flex h-10 items-center gap-3 self-end font-semibold">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} />
      {label}
    </label>
  )
}
