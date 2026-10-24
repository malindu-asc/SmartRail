import { Link } from 'react-router-dom'
import { Button, Modal, Skeleton } from '@/shared/components/ui'
import { Permissions, useAuth } from '@/shared/auth'
import { useSignOutCheck } from '@/features/incidents'

interface Props {
  open: boolean
  onClose: () => void
}

/**
 * Sign out. A controller who still owns an open incident must hand it over first, so an incident is never left without an owner
 * (UC-OPS-07). Everyone else just confirms.
 */
export function SignOutModal({ open, onClose }: Props) {
  const { can, signOut } = useAuth()
  const owner = can(Permissions.IncidentDeclare)
  const check = useSignOutCheck(open && owner)
  const blocked = check.data?.mustHandOver ?? false
  const checking = owner && check.isPending

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="sm"
      title="Sign out"
      description="You will need to sign in again to use the console."
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Stay signed in</Button>
          <Button onClick={signOut} disabled={blocked || checking}>Sign out</Button>
        </>
      }
    >
      {checking && <Skeleton className="h-4 w-56" />}
      {blocked && check.data && (
        <div role="alert" className="rounded-card bg-delayed-bg p-4 text-delayed">
          <p className="font-semibold">You still own {check.data.ownedIncidents.length === 1 ? 'an open incident' : `${check.data.ownedIncidents.length} open incidents`}</p>
          <p className="mt-1">Hand {check.data.ownedIncidents.length === 1 ? 'it' : 'them'} over before you leave, so someone is responsible.</p>
          <ul className="mt-2 list-disc pl-5">
            {check.data.ownedIncidents.map((incident) => (
              <li key={incident.id}><Link to={`/incidents/${incident.id}`} onClick={onClose} className="font-semibold underline">{incident.title}</Link></li>
            ))}
          </ul>
        </div>
      )}
      {check.data && !blocked && check.data.draftNotices > 0 && (
        <p className="text-fg-secondary">You have {check.data.draftNotices} draft {check.data.draftNotices === 1 ? 'notice' : 'notices'} that nobody else can publish.</p>
      )}
    </Modal>
  )
}
