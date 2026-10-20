import { Field, Select, TextArea } from '@/shared/components/ui'
import type { ReasonCode } from '../types'

interface ReasonFieldsProps {
  reasons: ReasonCode[]
  reasonCode: string
  note: string
  onReasonCode: (value: string) => void
  onNote: (value: string) => void
  /** Shown beside the reason when the person tried to submit without one. */
  error?: string | null
  noteLabel?: string
}

/** A reason from the approved list, and an optional note. Every controller action needs a reason (REQ-OPS-004). */
export function ReasonFields({ reasons, reasonCode, note, onReasonCode, onNote, error, noteLabel = 'Note (optional)' }: ReasonFieldsProps) {
  return (
    <div className="space-y-4">
      <Field label="Reason" error={error}>
        {(props) => (
          <Select {...props} value={reasonCode} onChange={(event) => onReasonCode(event.target.value)}>
            <option value="">Choose a reason</option>
            {reasons.map((reason) => (
              <option key={reason.code} value={reason.code}>
                {reason.label}
              </option>
            ))}
          </Select>
        )}
      </Field>
      <Field label={noteLabel}>
        {(props) => <TextArea {...props} value={note} maxLength={300} onChange={(event) => onNote(event.target.value)} />}
      </Field>
    </div>
  )
}
