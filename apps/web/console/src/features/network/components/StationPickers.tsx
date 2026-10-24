import { Select } from '@/shared/components/ui'
import { cn } from '@/shared/utils'
import { useStations } from '../hooks/useStations'

interface StationSelectProps {
  value: string
  onChange: (code: string) => void
  /** Text of the empty choice: "All stations" for a filter, "Choose a station" for a form. */
  placeholder: string
  pill?: boolean
  id?: string
  'aria-label'?: string
}

export function StationSelect({ value, onChange, placeholder, pill, ...rest }: StationSelectProps) {
  const stations = useStations()
  return (
    <Select pill={pill} value={value} onChange={(event) => onChange(event.target.value)} disabled={stations.isPending} {...rest}>
      <option value="">{placeholder}</option>
      {(stations.data ?? []).map((station) => (
        <option key={station.code} value={station.code}>
          {station.name}
        </option>
      ))}
    </Select>
  )
}

interface StationChecklistProps {
  selected: string[]
  onChange: (codes: string[]) => void
  disabled?: boolean
}

/** Pick several stations (a notice's scope, a station master's stations). */
export function StationChecklist({ selected, onChange, disabled }: StationChecklistProps) {
  const stations = useStations()

  if (stations.isPending) return <p className="text-fg-tertiary">Loading stations…</p>
  if (stations.isError) return <p className="text-cancelled">Stations could not be loaded.</p>

  function toggle(code: string) {
    onChange(selected.includes(code) ? selected.filter((c) => c !== code) : [...selected, code])
  }

  const all = (stations.data ?? []).map((station) => station.code)
  const everyone = all.length > 0 && all.every((code) => selected.includes(code))

  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between">
        <span className="text-fg-secondary">{selected.length} of {all.length} selected</span>
        <span className="flex gap-3">
          <button type="button" className="font-semibold text-primary hover:underline disabled:opacity-50" disabled={disabled || everyone} onClick={() => onChange(all)}>
            Select all
          </button>
          <button type="button" className="font-semibold text-primary hover:underline disabled:opacity-50" disabled={disabled || selected.length === 0} onClick={() => onChange([])}>
            Clear
          </button>
        </span>
      </div>
      <div role="group" aria-label="Stations" className="grid max-h-44 grid-cols-2 gap-x-4 gap-y-1 overflow-y-auto rounded-control border border-line-strong p-3">
        {(stations.data ?? []).map((station) => (
          <label key={station.code} className={cn('flex items-center gap-2 py-0.5', disabled && 'opacity-50')}>
            <input type="checkbox" checked={selected.includes(station.code)} disabled={disabled} onChange={() => toggle(station.code)} />
            {station.name}
          </label>
        ))}
      </div>
    </div>
  )
}
