import { useEffect, useRef, useState } from 'react'
import { diffRows, LEAVE_MS, type Row } from './logic.ts'

/**
 * The rows of a board, with the movement a station board has. A train that has gone fades away first and the rows close up; a train
 * that comes on fades in. Nothing moves the first time the board is drawn (the whole screen fades up then), and a row whose words
 * change (late, then on time) just changes in place.
 */
export function useSmoothRows(target: Row[]) {
  const [shown, setShown] = useState<Row[]>(target)
  const [going, setGoing] = useState<ReadonlySet<string>>(new Set())
  const [fresh, setFresh] = useState<ReadonlySet<string>>(new Set())

  // The latest rows and what is on screen, for the timers below. The effect itself only runs when the rows really change.
  const latest = useRef(target)
  const onScreen = useRef(shown)
  // Kept up to date after every draw. This effect is declared first, so it runs before the one below.
  useEffect(() => {
    latest.current = target
    onScreen.current = shown
  })

  const signature = target.map((r) => `${r.key}|${r.time}|${r.platform}|${r.code}|${r.at ?? ''}`).join(';')

  useEffect(() => {
    let leaveTimer: ReturnType<typeof setTimeout> | undefined

    const settle = () => {
      const { added } = diffRows(onScreen.current, latest.current)
      setFresh(new Set(added))
      setShown(latest.current)
      setGoing(new Set())
    }

    // Not in the effect's own turn, so the board is not redrawn while it is still being drawn.
    const start = setTimeout(() => {
      const { removed } = diffRows(onScreen.current, latest.current)
      if (removed.length === 0) {
        settle()
        return
      }
      setGoing(new Set(removed))
      leaveTimer = setTimeout(settle, LEAVE_MS)
    }, 0)

    return () => {
      clearTimeout(start)
      clearTimeout(leaveTimer)
    }
  }, [signature])

  return { shown, going, fresh }
}
