import { useState } from 'react'
import { Button, Card, CardHeader, ConfirmModal } from '@/shared/components/ui'
import { useAuth } from '@/shared/auth'
import type { ButtonVariants } from '@/shared/components/ui/buttonVariants'
import { useResume } from '../hooks/useJourneys'
import type { JourneyAction, JourneyDetail } from '../types'
import { CancelJourneyModal } from './CancelJourneyModal'
import { ChangePlatformModal } from './ChangePlatformModal'
import { InterruptModal } from './InterruptModal'
import { ReinstateModal } from './ReinstateModal'
import { TerminateShortModal } from './TerminateShortModal'

interface ActionStyle {
  label: string
  variant: NonNullable<ButtonVariants['variant']>
}

// The order and wording of the buttons. The server decides whether each is allowed and which permission it needs.
const ACTIONS: { code: string; style: ActionStyle }[] = [
  { code: 'change_platform', style: { label: 'Change platform', variant: 'secondary' } },
  { code: 'assign', style: { label: 'Assign driver and unit', variant: 'secondary' } },
  { code: 'message_driver', style: { label: 'Message driver', variant: 'secondary' } },
  { code: 'interrupt', style: { label: 'Interrupt', variant: 'secondary' } },
  { code: 'resume', style: { label: 'Resume', variant: 'primary' } },
  { code: 'reinstate', style: { label: 'Reinstate', variant: 'primary' } },
  { code: 'terminate_short', style: { label: 'Terminate short', variant: 'danger' } },
  { code: 'cancel', style: { label: 'Cancel journey', variant: 'danger' } },
  { code: 'emergency_terminate', style: { label: 'Emergency terminate', variant: 'danger' } },
]

/** Codes whose dialogs live in other features: the page opens them. */
const EXTERNAL = new Set(['assign', 'message_driver', 'emergency_terminate'])

interface JourneyActionsProps {
  detail: JourneyDetail
  /** Called for assign, message and emergency termination. */
  onExternal: (code: string) => void
}

/**
 * The actions a person may take on this journey. A button appears only when the role holds the permission the server named
 * for it; it is disabled, with the reason, when the journey's state does not allow it. The API enforces both again.
 */
export function JourneyActions({ detail, onExternal }: JourneyActionsProps) {
  const { can } = useAuth()
  const [open, setOpen] = useState<string | null>(null)
  const resume = useResume(detail.journey.journeyId)

  const byCode = new Map<string, JourneyAction>(detail.actions.map((action) => [action.code, action]))
  const visible = ACTIONS.filter(({ code }) => {
    const action = byCode.get(code)
    return action !== undefined && can(action.permission)
  })

  if (visible.length === 0) {
    return (
      <Card>
        <CardHeader title="Actions" />
        <p className="px-5 py-4 text-fg-secondary">Your role can view this journey but not act on it.</p>
      </Card>
    )
  }

  function run(code: string) {
    if (EXTERNAL.has(code)) onExternal(code)
    else setOpen(code)
  }

  return (
    <Card>
      <CardHeader title="Actions" />
      <div className="flex flex-col gap-2 p-5">
        {visible.map(({ code, style }) => {
          const action = byCode.get(code) as JourneyAction
          return (
            <div key={code}>
              <Button
                variant={style.variant}
                className="w-full"
                disabled={!action.allowed}
                title={action.allowed ? undefined : (action.whyNot ?? undefined)}
                onClick={() => run(code)}
              >
                {style.label}
              </Button>
              {!action.allowed && action.whyNot && <p className="mt-1 text-xs text-fg-tertiary">{action.whyNot}</p>}
            </div>
          )
        })}
      </div>

      <CancelJourneyModal journey={detail.journey} open={open === 'cancel'} onClose={() => setOpen(null)} />
      <TerminateShortModal detail={detail} open={open === 'terminate_short'} onClose={() => setOpen(null)} />
      <InterruptModal journey={detail.journey} open={open === 'interrupt'} onClose={() => setOpen(null)} />
      <ReinstateModal journey={detail.journey} open={open === 'reinstate'} onClose={() => setOpen(null)} />
      <ChangePlatformModal detail={detail} open={open === 'change_platform'} onClose={() => setOpen(null)} />
      <ConfirmModal
        open={open === 'resume'}
        onClose={() => setOpen(null)}
        title={`Resume ${detail.journey.trainNumber}`}
        description="The live estimate comes back as soon as the train reports."
        confirmLabel="Resume journey"
        pending={resume.isPending}
        onConfirm={() => resume.mutate({ expectedVersion: detail.journey.version }, { onSuccess: () => setOpen(null) })}
      />
    </Card>
  )
}
