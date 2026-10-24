import { useLayoutEffect, useRef, useState, type ReactNode } from 'react'

/** Scales the 1280 by 720 stage to the space it is given, so the design looks the same on a laptop and on a wall screen. */
export function Fit({ children }: { children: ReactNode }) {
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
