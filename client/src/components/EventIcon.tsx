import { Box } from '@mui/material'
import { useState } from 'react'

// True when the visible pixels are nearly white, so the logo would vanish on a light background.
function isMostlyWhite(img: HTMLImageElement) {
  try {
    const canvas = document.createElement('canvas')
    canvas.width = canvas.height = 24
    const ctx = canvas.getContext('2d', { willReadFrequently: true })!
    ctx.drawImage(img, 0, 0, 24, 24)
    const { data } = ctx.getImageData(0, 0, 24, 24)
    let sum = 0, n = 0
    for (let i = 0; i < data.length; i += 4) {
      if (data[i + 3] < 32) continue
      sum += (0.2126 * data[i] + 0.7152 * data[i + 1] + 0.0722 * data[i + 2]) / 255
      n++
    }
    return n > 0 && sum / n > 0.9
  } catch {
    return false
  }
}

type Props = { id: number; title: string; iconUpdatedAt?: string | null; size?: number }

export default function EventIcon({ id, title, iconUpdatedAt, size = 20 }: Props) {
  const [failed, setFailed] = useState(false)
  const [light, setLight] = useState(false)
  const box = { width: size, height: size, flex: '0 0 auto', borderRadius: `${Math.round(size / 4)}px`, overflow: 'hidden' }
  if (iconUpdatedAt && !failed)
    return (
      <Box
        component="img"
        src={`/api/events/${id}/icon?v=${encodeURIComponent(iconUpdatedAt)}`}
        alt=""
        onError={() => setFailed(true)}
        onLoad={e => setLight(isMostlyWhite(e.currentTarget))}
        sx={{ ...box, objectFit: 'contain', boxSizing: 'border-box', ...(light && { bgcolor: 'var(--ink)', p: '2px' }) }}
      />
    )
  return (
    <Box
      aria-hidden
      sx={{
        ...box, display: 'grid', placeItems: 'center', bgcolor: 'var(--fill)', color: 'text.secondary',
        fontSize: Math.round(size * 0.55), fontWeight: 700,
      }}
    >
      {title.trim().charAt(0).toUpperCase()}
    </Box>
  )
}
