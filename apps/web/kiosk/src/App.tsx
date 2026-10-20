import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { FullScreen, Gallery } from './features/display/Gallery'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Gallery />} />
        <Route path="/display/:id" element={<FullScreen />} />
        <Route path="/:id" element={<Gallery />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
