import { useEffect, useState } from 'react'

/**
 * The sample clock of the review page. A real display shows server time (the device's own clock may be wrong), so there it is read
 * from the API. Here the clock starts at 14:18 and runs faster than real time, so a few minutes of a station's day pass while the
 * screens rotate: trains leave and countdowns fall without anyone touching the page.
 */
const START = 14 * 60 + 18
const SPEED = 10 // one real second is ten sample seconds
const SPAN = 75 // the sample day is 75 minutes long, then it starts again
const T0 = Date.now()

/** Minutes since midnight in the sample day. */
export function sampleMinutes() {
  const elapsed = ((Date.now() - T0) / 1000) * (SPEED / 60)
  return START + (elapsed % SPAN)
}

export const at = (hours: number, minutes: number) => hours * 60 + minutes

export function formatTime(minutes: number) {
  const whole = Math.floor(minutes)
  const h = Math.floor(whole / 60)
  const m = whole % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

/** The time of the sample day as `hh:mm`, renewed when the minute changes. */
export function useClock() {
  const [minute, setMinute] = useState(() => Math.floor(sampleMinutes()))

  useEffect(() => {
    const id = window.setInterval(() => {
      const next = Math.floor(sampleMinutes())
      setMinute((current) => (current === next ? current : next))
    }, 500)
    return () => window.clearInterval(id)
  }, [])

  return { minute, label: formatTime(minute) }
}
