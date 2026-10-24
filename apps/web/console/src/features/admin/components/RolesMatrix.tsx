import { Check } from 'lucide-react'
import { Card, CardHeader } from '@/shared/components/ui'
import type { RoleMatrix } from '../types'

/** Who can do what. Read only: changes go through a reviewed release, so one account cannot widen its own access. */
export function RolesMatrix({ matrix }: { matrix: RoleMatrix }) {
  const groups = [...new Set(matrix.permissions.map((p) => p.group))]
  const staff = matrix.roles.filter((role) => role.code !== 'passenger')

  return (
    <div className="space-y-4">
      <Card className="space-y-1 px-5 py-4">
        <p className="font-semibold">{matrix.separationOfDuties}</p>
        <p className="text-fg-secondary">{matrix.note}</p>
      </Card>

      {groups.map((group) => (
        <Card key={group} className="overflow-x-auto">
          <CardHeader title={group} />
          <table className="w-full text-left">
            <caption className="sr-only">{group} permissions by role</caption>
            <thead>
              <tr className="border-b border-line bg-soft text-[13px] text-fg-secondary">
                <th scope="col" className="px-5 py-2.5 font-medium">Permission</th>
                {staff.map((role) => <th key={role.code} scope="col" className="px-3 py-2.5 text-center font-medium">{role.name}</th>)}
              </tr>
            </thead>
            <tbody>
              {matrix.permissions.filter((p) => p.group === group).map((permission) => (
                <tr key={permission.key} className="border-b border-line last:border-0">
                  <th scope="row" className="px-5 py-2.5 text-left font-normal">{permission.label}</th>
                  {staff.map((role) => (
                    <td key={role.code} className="px-3 py-2.5 text-center">
                      {permission.roles.includes(role.code) ? <Check aria-label="Allowed" className="mx-auto size-4 text-on-time" /> : <span aria-label="Not allowed" className="text-fg-tertiary">–</span>}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      ))}
    </div>
  )
}
