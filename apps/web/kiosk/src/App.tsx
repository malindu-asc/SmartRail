import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { FullScreen, Gallery } from './features/display/Gallery'
import { StationDisplay, StationPicker } from './features/display/live/StationDisplay'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* The real displays: one address per station, reading the live board. */}
        <Route path="/" element={<StationPicker />} />
        <Route path="/display/:station" element={<StationDisplay />} />
        <Route path="/display/:station/:platform" element={<StationDisplay />} />
        {/* The design review with sample data. */}
        <Route path="/gallery" element={<Gallery />} />
        <Route path="/gallery/play" element={<FullScreen />} />
        <Route path="/gallery/play/:id" element={<FullScreen />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
