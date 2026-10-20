import { useEffect, useLayoutEffect, useRef, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { screens } from './registry'

/** Scales the 1280 by 720 stage to the space it is given, so the design looks the same on a laptop and on a wall screen. */
function Fit({ children }: { children: ReactNode }) {
  const box = useRef<HTMLDivElement>(null)
  const [scale, setScale] = useState(1)

  useLayoutEffect(() => {
    const el = box.current
    if (!el) return
    const measure = () => setScale(Math.min(el.clientWidth / 1280, el.clientHeight / 720))
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  return (
    <div ref={box} className="fit">
      <div className="fit-inner" style={{ transform: `scale(${scale})` }}>
        {children}
      </div>
    </div>
  )
}

/**
 * Walks through the screens one after another, as a real display does, and wraps round. `index` is the screen showing now.
 * While `playing` is false it stays where it is.
 */
function useRotation(playing: boolean, start = 0) {
  const [index, setIndex] = useState(start)

  useEffect(() => {
    if (!playing) return
    const timer = window.setTimeout(
      () => setIndex((i) => (i + 1) % screens.length),
      screens[index].seconds * 1000,
    )
    return () => window.clearTimeout(timer)
  }, [playing, index])

  return [index, setIndex] as const
}

/** The screen that is up. Its key changes with the screen, so each one slides in and its rows flip into place. */
function Current({ index }: { index: number }) {
  return (
    <div key={screens[index].id} className="d-enter">
      {screens[index].node}
    </div>
  )
}

/** The review page: the three screens in the list, and the display cycling through them. */
export function Gallery() {
  const [playing, setPlaying] = useState(true)
  const [index, setIndex] = useRotation(playing)
  const current = screens[index]

  useEffect(() => {
    document.title = `${current.title} · SmartRail displays`
  }, [current])

  const groups = [...new Set(screens.map((s) => s.group))]

  return (
    <div className="gallery">
      <nav className="gallery-nav">
        <h1>Station displays</h1>
        <p>High fidelity. Sample data.</p>
        <button type="button" className="gallery-play" onClick={() => setPlaying((p) => !p)}>
          {playing ? 'Pause rotation' : 'Resume rotation'}
        </button>
        {groups.map((group) => (
          <div key={group}>
            <h2>{group}</h2>
            {screens.map((s, i) =>
              s.group === group ? (
                <button
                  key={s.id}
                  type="button"
                  className={i === index ? 'on' : ''}
                  onClick={() => {
                    setPlaying(false)
                    setIndex(i)
                  }}
                >
                  {s.title}
                  <small>{s.board}</small>
                </button>
              ) : null,
            )}
          </div>
        ))}
        <Link to="/display" className="gallery-full">
          Open full screen
        </Link>
      </nav>
      <main className="gallery-stage">
        <Fit>
          <Current index={index} />
        </Fit>
      </main>
    </div>
  )
}

/** A display on its own: cycles through every screen, or shows one when the address names it. */
export function FullScreen() {
  const { id } = useParams()
  const named = screens.findIndex((s) => s.id === id)
  const [index] = useRotation(named < 0)

  return (
    <div className="fullscreen">
      <Fit>
        <Current index={named >= 0 ? named : index} />
      </Fit>
    </div>
  )
}
