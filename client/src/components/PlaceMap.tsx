import { Box } from '@mui/material'
import type { SxProps } from '@mui/material'
import { useEffect, useRef } from 'react'
import { useGoogleMaps } from '../maps'

type Props = { lat: number; lng: number; title: string; height?: number; sx?: SxProps }

export default function PlaceMap({ lat, lng, title, height = 320, sx }: Props) {
  const ref = useRef<HTMLDivElement>(null)
  const ready = useGoogleMaps()

  useEffect(() => {
    if (!ready || !ref.current) return
    const el = ref.current
    let cancelled = false
    void (async () => {
      const { Map } = await google.maps.importLibrary('maps') as google.maps.MapsLibrary
      const { AdvancedMarkerElement } = await google.maps.importLibrary('marker') as google.maps.MarkerLibrary
      if (cancelled) return
      const position = { lat, lng }
      const map = new Map(el, { center: position, zoom: 15, mapId: 'DEMO_MAP_ID', disableDefaultUI: true, zoomControl: true, gestureHandling: 'cooperative' })
      new AdvancedMarkerElement({ map, position, title })
    })()
    return () => { cancelled = true }
  }, [ready, lat, lng, title])

  if (!ready) return null
  return <Box ref={ref} role="region" aria-label={`Map of ${title}`} sx={{ height, borderRadius: '12px', overflow: 'hidden', bgcolor: 'var(--fill)', ...sx }} />
}
