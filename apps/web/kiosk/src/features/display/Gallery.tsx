import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { screens } from './registry'

/** Scales the 1280 by 720 stage to the space it is given, so the design looks the same on a laptop and on a wall screen. */
function Fit({ children }: { children: React.ReactNode }) {
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

/** Every display state on one page, for review. `/display/<id>` shows one on its own, full screen, as a real display would. */
export function Gallery() {
  const { id } = useParams()
  const current = screens.find((s) => s.id === id) ?? screens[0]

  useEffect(() => {
    document.title = `${current.title} · SmartRail displays`
  }, [current])

  const groups = [...new Set(screens.map((s) => s.group))]

  return (
    <div className="gallery">
      <nav className="gallery-nav">
        <h1>Station displays</h1>
        <p>High fidelity. Sample data.</p>
        {groups.map((group) => (
          <div key={group}>
            <h2>{group}</h2>
            {screens
              .filter((s) => s.group === group)
              .map((s) => (
                <Link key={s.id} to={`/${s.id}`} className={s.id === current.id ? 'on' : ''}>
                  {s.title}
                  <small>{s.board}</small>
                </Link>
              ))}
          </div>
        ))}
        <Link to={`/display/${current.id}`} className="gallery-full">
          Open full screen
        </Link>
      </nav>
      <main className="gallery-stage">
        <Fit>{current.node}</Fit>
      </main>
    </div>
  )
}

export function FullScreen() {
  const { id } = useParams()
  const current = screens.find((s) => s.id === id) ?? screens[0]
  return (
    <div className="fullscreen">
      <Fit>{current.node}</Fit>
    </div>
  )
}
