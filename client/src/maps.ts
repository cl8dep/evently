import { useEffect, useState } from 'react'
import { api } from './api'

type Config = { places: boolean; mapsKey: string | null }

let config: Promise<Config> | undefined
let script: Promise<void> | undefined

const loadConfig = () => (config ??= api<Config>('/api/config').catch(() => ({ places: false, mapsKey: null })))

// Place search is served by the backend. This only tells the UI whether it is available (null while loading).
export const usePlacesEnabled = () => {
  const [on, setOn] = useState<boolean | null>(null)
  useEffect(() => { void loadConfig().then(c => setOn(c.places)) }, [])
  return on
}

// The interactive map needs a browser key, restricted by HTTP referrer in Google Cloud.
export const useGoogleMaps = () => {
  const [ready, setReady] = useState(false)
  useEffect(() => {
    void loadConfig().then(({ mapsKey }) => {
      if (!mapsKey) return
      script ??= new Promise<void>((resolve, reject) => {
        const s = document.createElement('script')
        s.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(mapsKey)}&loading=async&v=weekly`
        s.async = true
        s.onload = () => resolve()
        s.onerror = () => reject(new Error('Google Maps failed to load'))
        document.head.appendChild(s)
      })
      script.then(() => setReady(true), () => {})
    })
  }, [])
  return ready
}
